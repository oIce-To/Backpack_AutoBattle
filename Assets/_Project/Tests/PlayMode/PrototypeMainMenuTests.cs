using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeMainMenuTests
    {
        private BackpackPrototypeController _controller;
        private IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10;
            do
            {
                yield return null;
                _controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            } while ((_controller == null || !_controller.IsMainMenu) && Time.realtimeSinceStartup < deadline);
            Assert.That(_controller, Is.Not.Null); Assert.That(_controller.IsMainMenu, Is.True);
            _controller.BattleController.AutoTick = false;
        }

        [UnityTest]
        public IEnumerator Entry_ShowsAllenPortraitAndBaseStats_AndStartsFreshPreparationOnlyOnce()
        {
            yield return Setup(); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.MainMenu));
            Assert.That(Text("Main Character Name"), Is.EqualTo("알렌"));
            Assert.That(Text("Main Character Health"), Is.EqualTo("체력  50"));
            Assert.That(Text("Main Character Attack"), Is.EqualTo("공격력  10"));
            Assert.That(Text("Main Stage Summary"), Does.Contain("12라운드"));
            var portrait = Find("Main Character Illustration").GetComponent<Image>();
            Assert.That(portrait.sprite, Is.SameAs(Character().Portrait)); Assert.That(portrait.sprite, Is.Not.Null);
            Assert.That(portrait.sprite.texture.name, Is.EqualTo("allen-standing-approved"));
            Assert.That(portrait.preserveAspect, Is.True);
            Assert.That(Find("Shop Screen").gameObject.activeSelf, Is.False);
            Assert.That(_controller.BattleController.ScreenRoot.gameObject.activeSelf, Is.False);
            Assert.That(_controller.GetComponentsInChildren<BackpackItemView>(true), Is.Empty);
            Assert.That(_controller.CurrentShopItems, Is.Empty);
            Assert.That(_controller.CanEditLoadout, Is.False); Assert.That(_controller.TryStartBattle(), Is.False);
            Assert.That(_controller.TryRerollShop(), Is.False); Assert.That(_controller.AdvanceRound(), Is.False);
            Assert.That(_controller.TryReturnToMainMenu(), Is.False);
            Assert.That(Find("Main Quit Button").GetComponent<Button>().interactable, Is.True);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(Find("Main Start Button")), hits);
            Assert.That(hits.Any(hit => hit.gameObject == Find("Main Start Button").gameObject), Is.True,
                $"Hits: {string.Join(", ", hits.Select(hit => hit.gameObject.name))}; "
                + $"depth: {Find("Main Start Button").GetComponent<Image>().depth}");
            foreach (var label in Find("Main Menu Panel").GetComponentsInChildren<Text>())
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1), label.name);
            Capture("MainMenuAllen.png");
            Click("Main Start Button");
            Assert.That(_controller.IsMainMenu, Is.False); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(_controller.TryStartRun(), Is.False);
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.Lives, Is.EqualTo(3));
            Assert.That(_controller.Gold, Is.EqualTo(20)); Assert.That(_controller.CurrentShopItems, Has.Count.EqualTo(4));
            Assert.That(_controller.GetComponentsInChildren<BackpackItemView>(), Has.Length.EqualTo(3));
            Assert.That(_controller.TryReturnToMainMenu(), Is.False, "An active run cannot be discarded by the result-only action.");
            _controller.TryStartBattle(); Assert.That(_controller.ActiveCombat.Player.DisplayName, Is.EqualTo("알렌"));
        }

        [UnityTest]
        public IEnumerator GameOver_ReturnsToMain_ClearsOldItemsOffersAndStatistics_ThenStartsNewRun()
        {
            yield return Setup(); Click("Main Start Button");
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            slot.Bind(new ShopOffer(_controller.ItemCatalog.Single(i => i.ItemId == "healing_potion"), true));
            _controller.TryToggleShopLock(slot); var previousOffer = slot.Offer;
            var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Freeform Storage")).position; slot.OnDrag(pointer); slot.OnEndDrag(pointer);
            Assert.That(_controller.StoredItemCount, Is.EqualTo(1));
            for (int i = 0; i < 3; i++)
            {
                Lose(); if (i < 2) _controller.ReturnToPreparation();
            }
            var result = _controller.RunResult; Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.GameOver));
            Assert.That(Find("Return To Main Menu Button").gameObject.activeInHierarchy, Is.True);
            Capture("MainMenuGameOverActions.png");
            Click("Run Statistics Button"); _controller.BattleController.HideStatistics();
            Click("Return To Main Menu Button"); yield return null;
            Assert.That(_controller.IsMainMenu, Is.True); Assert.That(_controller.ActiveCombat, Is.Null);
            Assert.That(_controller.BattleController.ShowStatistics(), Is.False);
            Assert.That(_controller.StoredItemCount, Is.Zero); Assert.That(_controller.GetReadyFusions(), Is.Empty);
            Assert.That(_controller.GetComponentsInChildren<BackpackItemView>(true), Is.Empty);
            Assert.That(_controller.GetComponentsInChildren<PrototypeStorageItemView>(true), Is.Empty);
            Assert.That(_controller.GetComponentsInChildren<PrototypeShopSlotView>(true).All(s => s.Offer == null), Is.True);
            Assert.That(_controller.CurrentShopItems, Is.Empty); Assert.That(_controller.ItemTooltip.IsVisible, Is.False);
            Assert.That(_controller.TryRestartRun(), Is.False); Assert.That(_controller.RequestRunRestart(), Is.False);
            Click("Main Start Button");
            Assert.That(_controller.Lives, Is.EqualTo(3)); Assert.That(_controller.Gold, Is.EqualTo(20));
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.RunResult, Is.Null);
            Assert.That(slot.Offer, Is.Not.SameAs(previousOffer)); Assert.That(slot.Offer.IsLocked, Is.False);
            Assert.That(result.Lives, Is.Zero); Assert.That(result.Defeats, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ClearedRun_ReturnsToMainAndRecapturesOutsideStatsOnlyOnEntry()
        {
            yield return Setup(); Click("Main Start Button");
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            _controller.TryStartBattle(); var sword = _controller.ActiveCombat.PlayerWeapons.First();
            sword.Stats.AttackDamage = 10000; sword.Stats.AccuracyPercent = 100; _controller.TickCombat(2);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed)); var result = _controller.RunResult;
            var original = Character(); var upgraded = Object.Instantiate(original);
            try
            {
                Set(upgraded, "_maximumHealth", 80f); Set(upgraded, "_attack", 20f); Set(_controller, "_characterData", upgraded);
                Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(10));
                Click("Return To Main Menu Button");
                Assert.That(Text("Main Character Health"), Is.EqualTo("체력  80"));
                Assert.That(Text("Main Character Attack"), Is.EqualTo("공격력  20"));
                Capture("MainMenuAfterClear.png");
                Click("Main Start Button");
                Assert.That(_controller.StageEntryStats.MaximumHealth, Is.EqualTo(80));
                Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(20));
                Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(result.Round, Is.EqualTo(12));
                _controller.TryStartBattle(); Assert.That(_controller.ActiveCombat.PlayerHealth, Is.EqualTo(80));
                Assert.That(_controller.ActiveCombat.PlayerWeapons.First().Stats.AttackDamage, Is.EqualTo(10));
                Assert.That(_controller.BattleController.PlaybackSpeed, Is.EqualTo(1));
            }
            finally { Set(_controller, "_characterData", original); Object.Destroy(upgraded); }
        }

        [UnityTest]
        public IEnumerator InvalidStage_StaysOnMain_AndDeveloperSceneStillOpensDirectlyInPreparation()
        {
            yield return Setup(); var stage = _controller.Stage;
            Set(_controller, "_stageData", null);
            Assert.That(_controller.TryStartRun(), Is.False); Assert.That(_controller.IsMainMenu, Is.True);
            Assert.That(Text("Main Menu Error"), Is.Not.Empty); Assert.That(_controller.CurrentShopItems, Is.Empty);
            Set(_controller, "_stageData", stage);
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); _controller.BattleController.AutoTick = false;
            Assert.That(_controller.IsMainMenu, Is.False); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(_controller.GetComponentsInChildren<BackpackItemView>(), Has.Length.EqualTo(3));
        }

        private void Lose()
        {
            _controller.TryStartBattle();
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons) weapon.Stats.AttackDamage = 0;
            var enemy = _controller.ActiveCombat.EnemyWeapons.First(); enemy.Stats.AttackDamage = 10000; enemy.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2);
        }
        private CharacterData Character() => (CharacterData)_controller.GetType().GetField("_characterData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_controller);
        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private string Text(string name) => Find(name).GetComponent<Text>().text;
        private void Click(string name) => Find(name).GetComponent<Button>().onClick.Invoke();
        private void Capture(string file) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), file);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center)) };
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
