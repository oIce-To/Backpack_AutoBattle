using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
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
    public sealed class PrototypeRunLifecycleTests
    {
        private BackpackPrototypeController _controller;
        private IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            _controller.BattleController.AutoTick = false;
        }

        [UnityTest]
        public IEnumerator ForcedDefeat_CostsOneLife_PreservesHealth_AndRetriesSameRoundWithFreshClock()
        {
            yield return Setup();
            Assert.That(_controller.Lives, Is.EqualTo(3)); Assert.That(_controller.StartingLives, Is.EqualTo(3));
            Assert.That(Text("Run Lives"), Is.EqualTo("라이프 3 / 3"));
            Assert.That(Find("Next Round Button").gameObject.activeInHierarchy, Is.True);
            Assert.That(_controller.ItemCatalog.Any(i => i.ItemId == "healing_potion"), Is.True);
            Capture("RunLivesPreparation.png");
            Assert.That(_controller.AdvanceRound(), Is.True); Assert.That(_controller.Lives, Is.EqualTo(3));
            Assert.That(_controller.TryStartBattle(), Is.True);
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons.Concat(_controller.ActiveCombat.EnemyWeapons)) weapon.Stats.AttackDamage = 0;
            int round = _controller.CurrentRound, gold = _controller.Gold;
            _controller.TickCombat(121);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
            Assert.That(_controller.ActiveCombat.Result, Is.EqualTo(CombatResult.Defeat));
            Assert.That(_controller.ActiveCombat.IsForcedDefeat, Is.True);
            Assert.That(_controller.ActiveCombat.ElapsedSeconds, Is.EqualTo(90));
            Assert.That(_controller.ActiveCombat.PlayerHealth, Is.EqualTo(_controller.ActiveCombat.PlayerMaxHealth));
            Assert.That(Text("Battle Lives"), Is.EqualTo("라이프 2 / 3"));
            Assert.That(Text("Battle Result"), Does.Contain("90초 경과"));
            Assert.That(_controller.Lives, Is.EqualTo(2)); Capture("ForcedDefeat90Seconds.png");
            Assert.That(_controller.BattleController.ShowStatistics(), Is.True);
            Assert.That(_controller.ActiveCombat.CaptureStatistics().Seconds, Is.EqualTo(90));
            _controller.BattleController.HideStatistics();
            _controller.TickCombat(200); Assert.That(_controller.Lives, Is.EqualTo(2));
            Assert.That(_controller.ReturnToPreparation(), Is.True);
            Assert.That(_controller.CurrentRound, Is.EqualTo(round)); Assert.That(_controller.Gold, Is.EqualTo(gold));
            Assert.That(_controller.TryStartBattle(), Is.True);
            Assert.That(_controller.ActiveCombat.EnrageStacks, Is.Zero);
            Assert.That(_controller.ActiveCombat.ElapsedSeconds, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ThreeDefeats_CostOneLifeEach_ThenBlockAllGameplayAndShowGameOverOnce()
        {
            yield return Setup();
            int ended = 0; _controller.RunEnded += _ => ended++;
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            slot.Bind(new ShopOffer(Item("healing_potion"), true)); _controller.TryToggleShopLock(slot);
            var offer = slot.Offer; int gold = _controller.Gold;
            for (int i = 1; i <= 3; i++)
            {
                Lose();
                Assert.That(_controller.Lives, Is.EqualTo(3 - i));
                _controller.TickCombat(200); // The completed battle must not spend another life.
                Assert.That(_controller.Lives, Is.EqualTo(3 - i));
                Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.Gold, Is.EqualTo(gold));
                Assert.That(slot.Offer, Is.SameAs(offer)); Assert.That(offer.IsLocked, Is.True);
                if (i < 3)
                {
                    Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
                    Assert.That(Text("Battle Result"), Does.Contain("라이프 -1"));
                    if (i == 1) Capture("DefeatWithTwoLives.png");
                    Find("Combat Action Button").GetComponent<Button>().onClick.Invoke();
                    Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
                    Assert.That(Text("Run Lives"), Is.EqualTo($"라이프 {3 - i} / 3"));
                    Assert.That(_controller.ReturnToPreparation(), Is.False);
                }
            }
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.GameOver));
            Assert.That(ended, Is.EqualTo(1)); Assert.That(_controller.RunResult.Defeats, Is.EqualTo(3));
            Assert.That(_controller.RunResult.Outcome, Is.EqualTo(PrototypeRunOutcome.GameOver));
            Assert.That(Find("Run Result Screen").gameObject.activeInHierarchy, Is.True);
            Assert.That(Text("Run Result Title"), Is.EqualTo("게임 오버"));
            Assert.That(Text("Run Result Summary"), Does.Contain("남은 라이프  0 / 3").And.Contain("패배 3").And.Not.Contain("골드"));
            Assert.That(_controller.CanEditLoadout, Is.False);
            Assert.That(_controller.TryStartBattle(), Is.False); Assert.That(_controller.ReturnToPreparation(), Is.False);
            Assert.That(_controller.AdvanceRound(), Is.False); Assert.That(_controller.TryRerollShop(), Is.False);
            Assert.That(_controller.TryToggleShopLock(slot), Is.False);
            Assert.That(Find("Combat Action Button").GetComponent<Button>().interactable, Is.False);
            _controller.HandleCombatAction(); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.GameOver));
            Capture("GameOverRunResult.png");
        }

        [UnityTest]
        public IEnumerator GameOverRestart_ResetsStorageLocksOffersInventoryFusionsStatsAndOldBattleViews()
        {
            yield return Setup();
            for (int i = 0; i < 2; i++) { Lose(); _controller.ReturnToPreparation(); }
            Buy("whetstone", Find("Grid Cell 1,2"));
            Buy("whetstone", Find("Freeform Storage"));
            Buy("healing_potion", Find("Freeform Storage")); yield return null;
            string lockedId = _controller.GetComponentsInChildren<PrototypeStorageItemView>().Single(v => v.StoredItem.ItemData.ItemId == "whetstone").StoredItem.InstanceId;
            Assert.That(_controller.TryToggleFusionLock(lockedId), Is.True);
            var shield = _controller.GetComponentsInChildren<BackpackItemView>().Single(v => v.Data.ItemId == "wooden_shield");
            _controller.RotateItem(shield); Assert.That(shield.Rotation, Is.Not.EqualTo(ItemRotation.None));
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            slot.Bind(new ShopOffer(Item("healing_potion"), true)); _controller.TryToggleShopLock(slot);
            var oldOffer = slot.Offer;
            Assert.That(_controller.GetReadyFusions(), Has.Count.EqualTo(1));
            Lose();
            var oldResult = _controller.RunResult;
            Assert.That(_controller.IsRunFinished, Is.True);
            Assert.That(_controller.StoredItemCount, Is.EqualTo(2), "Game over must not apply pending fusions or discard owned materials.");
            Assert.That(_controller.GetComponentsInChildren<BackpackItemView>(true).Count(v => v.Data.ItemId == "whetstone"), Is.EqualTo(1));
            Find("Restart Run Button").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(_controller.Lives, Is.EqualTo(3)); Assert.That(_controller.CurrentRound, Is.EqualTo(1));
            Assert.That(_controller.Gold, Is.EqualTo(20)); Assert.That(_controller.StoredItemCount, Is.Zero);
            Assert.That(_controller.RunResult, Is.Null); Assert.That(_controller.ActiveCombat, Is.Null);
            Assert.That(_controller.IsFusionLocked(lockedId), Is.False); Assert.That(_controller.GetReadyFusions(), Is.Empty);
            Assert.That(_controller.IsFusionPresenting, Is.False); Assert.That(_controller.ItemTooltip.IsVisible, Is.False);
            Assert.That(_controller.GetComponentsInChildren<PrototypeBattleItemView>(true), Is.Empty);
            Assert.That(_controller.GetComponentsInChildren<PrototypeStatusIcon>(true), Is.Empty);
            Assert.That(Find("Run Result Screen").gameObject.activeSelf, Is.False);
            var bag = _controller.GetComponentsInChildren<BackpackItemView>();
            Assert.That(bag, Has.Length.EqualTo(3));
            Assert.That(bag.Select(v => v.Data.ItemId), Is.EquivalentTo(new[] { "training_blade", "wooden_shield", "swift_charm" }));
            Assert.That(bag.All(v => v.Rotation == ItemRotation.None), Is.True);
            Assert.That(bag.Single(v => v.Data.ItemId == "wooden_shield").Origin, Is.EqualTo(new Vector2Int(1, 0)));
            var slots = _controller.GetComponentsInChildren<PrototypeShopSlotView>();
            Assert.That(slots.All(s => s.CanPurchase && !s.Offer.IsLocked), Is.True);
            Assert.That(slot.Offer, Is.Not.SameAs(oldOffer)); Assert.That(_controller.CurrentShopItems, Has.Count.EqualTo(4));
            Assert.That(oldResult.Defeats, Is.EqualTo(3)); Assert.That(oldResult.Lives, Is.Zero);
            Assert.That(_controller.TryRestartRun(), Is.False); Assert.That(_controller.RequestRunRestart(), Is.False);
            Assert.That(_controller.TryStartBattle(), Is.True);
            Assert.That(_controller.ActiveCombat.PlayerHealth, Is.EqualTo(50));
            Assert.That(_controller.ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            Assert.That(_controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator TwelveVictories_ShowClearSummaryAndRestartWithoutExtraRoundIncome()
        {
            yield return Setup();
            int ended = 0; PrototypeRunResult result = null;
            _controller.RunEnded += value => { ended++; result = value; };
            for (int round = 1; round <= 12; round++)
            {
                Win();
                Assert.That(_controller.Lives, Is.EqualTo(3));
                if (round < 12) Assert.That(_controller.AdvanceRound(), Is.True);
            }
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(ended, Is.EqualTo(1)); Assert.That(result, Is.SameAs(_controller.RunResult));
            Assert.That(result.Victories, Is.EqualTo(12)); Assert.That(result.Defeats, Is.Zero);
            Assert.That(result.Gold, Is.EqualTo(75)); Assert.That(result.Outcome, Is.EqualTo(PrototypeRunOutcome.Cleared));
            Assert.That(Text("Run Result Summary"), Does.Contain("완료 라운드  12 / 12").And.Contain("승리 12").And.Not.Contain("골드"));
            Assert.That(_controller.AdvanceRound(), Is.False); Assert.That(_controller.ReturnToPreparation(), Is.False);
            Capture("ClearedRunResult.png");
            Find("Restart Run Button").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.That(_controller.Lives, Is.EqualTo(3)); Assert.That(_controller.Gold, Is.EqualTo(20));
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(ended, Is.EqualTo(1));
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Capture("RestartedRunPreparation.png");
        }

        [UnityTest]
        public IEnumerator NavigationSubscriber_ReceivesOneImmutableResultWithoutAutomaticRestart()
        {
            yield return Setup();
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            Win(); var result = _controller.RunResult;
            int requested = 0;
            _controller.RestartRequested += received => { requested++; Assert.That(received, Is.SameAs(result)); };
            Assert.That(_controller.RequestRunRestart(), Is.True);
            Assert.That(_controller.RequestRunRestart(), Is.False);
            Assert.That(requested, Is.EqualTo(1)); Assert.That(_controller.RunResult, Is.SameAs(result));
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(Find("Restart Run Button").GetComponent<Button>().interactable, Is.False);
            // The future coordinator owns navigation; it can also explicitly request the shared fresh-run reset.
            Set(_controller, "_startingLives", 2);
            Assert.That(_controller.TryRestartRun(), Is.True); yield return null;
            Assert.That(_controller.Lives, Is.EqualTo(2)); Assert.That(_controller.StartingLives, Is.EqualTo(2));
            Assert.That(result.Lives, Is.EqualTo(3)); Assert.That(result.Victories, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Restart_RecapturesExternalCharacterStatsOnlyForTheNewRun()
        {
            yield return Setup();
            var original = (CharacterData)Get(_controller, "_characterData"); var changed = Object.Instantiate(original);
            try
            {
                Set(changed, "_maximumHealth", 100f); Set(changed, "_attack", 20f); Set(_controller, "_characterData", changed);
                Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(10));
                for (int i = 0; i < 11; i++) _controller.AdvanceRound();
                Win(); _controller.TryRestartRun(); yield return null;
                Assert.That(_controller.StageEntryStats.MaximumHealth, Is.EqualTo(100));
                Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(20));
                _controller.TryStartBattle();
                Assert.That(_controller.ActiveCombat.PlayerMaxHealth, Is.EqualTo(100));
                Assert.That(_controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(10));
                Assert.That(_controller.Lives, Is.EqualTo(3));
            }
            finally { Set(_controller, "_characterData", original); Object.Destroy(changed); }
        }

        private void Lose()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons) weapon.Stats.AttackDamage = 0;
            var enemy = _controller.ActiveCombat.EnemyWeapons.First();
            enemy.Stats.AttackDamage = 1000; enemy.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2);
        }
        private void Win()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            var weapon = _controller.ActiveCombat.PlayerWeapons.First(); weapon.Stats.AttackDamage = 1000; weapon.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2);
        }
        private void Buy(string id, Transform destination)
        {
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First(); slot.Bind(new ShopOffer(Item(id), false));
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, slot.transform.position) };
            slot.OnBeginDrag(pointer); pointer.position = RectTransformUtility.WorldToScreenPoint(null, destination.position);
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private ItemData Item(string id) => _controller.ItemCatalog.Single(i => i.ItemId == id);
        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private string Text(string name) => Find(name).GetComponent<Text>().text;
        private void Capture(string name) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), name);
        private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
