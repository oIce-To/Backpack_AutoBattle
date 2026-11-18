using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
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
            PrototypeCombatFixture.PrepareLobbyProfile();
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
            Assert.That(Text("Main Character Health"), Is.EqualTo("체력  52.5"));
            Assert.That(Text("Main Character Attack"), Is.EqualTo("공격력  10.5"));
            Assert.That(Text("Main Profile Name"), Is.EqualTo("플레이어"));
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
            Assert.That(Find("Main Inventory Button").GetComponent<Image>().sprite?.name, Is.EqualTo("HomeBackpackIcon"));
            Assert.That(Find("Main Mail Button").GetComponent<Image>().sprite?.name, Is.EqualTo("HomeMailboxIcon"));
            Assert.That(Find("Main Compendium Button").GetComponent<Image>().sprite?.name, Is.EqualTo("HomeCompendiumIcon"));
            var hall = Find("Guild Hall Illustration").GetComponent<Image>();
            Assert.That(hall.raycastTarget, Is.False);
            Assert.That(hall.sprite, Is.Not.Null);
            Assert.That(hall.sprite.texture.name, Is.EqualTo("GuildHallIllustrated"));
            Assert.That(hall.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(Find("Main Inventory Button")), hits);
            Assert.That(hits.First().gameObject, Is.SameAs(Find("Main Inventory Button").gameObject));
            hits.Clear();
            EventSystem.current.RaycastAll(Pointer(Find("Main Inventory Button Label")), hits);
            Assert.That(hits.Any(hit => hit.gameObject == Find("Main Inventory Button").gameObject), Is.False);
            foreach (var label in Find("Main Menu Panel").GetComponentsInChildren<Text>())
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1), label.name);
            Capture("MainMenuAllen.png");
            EnterStage();
            Assert.That(_controller.IsMainMenu, Is.False); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(_controller.TryStartRun(), Is.False);
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.Lives, Is.EqualTo(4));
            Assert.That(_controller.Gold, Is.EqualTo(20)); Assert.That(_controller.CurrentShopItems, Has.Count.EqualTo(4));
            Assert.That(_controller.CaptureCombatSnapshot().Items.Single().ItemId, Is.EqualTo("knight_sword"));
            Assert.That(_controller.TryReturnToMainMenu(), Is.False, "An active run cannot be discarded by the result-only action.");
            _controller.TryStartBattle(); Assert.That(_controller.ActiveCombat.Player.DisplayName, Is.EqualTo("알렌"));
            Assert.That(Find("Battle Stage Illustration").GetComponent<Image>().sprite.name, Is.EqualTo("DungeonSelection"));
            Assert.That(Find("Battle Stage Shade").GetComponent<Image>().color.a, Is.GreaterThanOrEqualTo(.8f));
            Assert.That(Find("Battle Stage Backdrop").GetSiblingIndex(), Is.Zero);
            Capture("DungeonBattleBackdrop.png");
        }

        [UnityTest]
        public IEnumerator QuitConfirmation_CanBeCancelledAndOnlyYesCallsQuit()
        {
            yield return Setup();
            Click("Main Quit Button");
            Assert.That(Find("Main Quit Popup").gameObject.activeInHierarchy, Is.True);
            Assert.That(Text("Main Quit Popup Title"), Is.EqualTo("정말로 종료하시겠습니까?"));
            Assert.That(_controller.ShowStageSelection(), Is.False);
            Capture("MainQuitConfirmation.png");
            Click("Main Quit No");
            Assert.That(Find("Main Quit Popup").gameObject.activeInHierarchy, Is.False);
            Assert.That(_controller.IsMainMenu, Is.True);

            var canvas = _controller.GetComponentInChildren<Canvas>();
            var sandbox = new GameObject("Quit Callback Test", typeof(RectTransform));
            sandbox.transform.SetParent(canvas.transform, false);
            try
            {
                int quitCalls = 0;
                var menu = new PrototypeMainMenuScreen(sandbox.transform, _controller.ItemTooltip, () => quitCalls++);
                menu.Show(_controller.SelectedCharacter, _controller.Progress, _controller.StageEntryStats, _controller.AvailableGear);
                Button Button(string name) => sandbox.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
                Button("Main Quit Yes").onClick.Invoke();
                Assert.That(quitCalls, Is.Zero);
                Button("Main Quit Button").onClick.Invoke();
                Assert.That(quitCalls, Is.Zero);
                Button("Main Quit Yes").onClick.Invoke();
                Assert.That(quitCalls, Is.EqualTo(1));
            }
            finally { Object.Destroy(sandbox); }
        }

        [UnityTest]
        public IEnumerator Compendium_ShowsRecipesAndSilhouettes_AndCraftingUnlocksThroughAbandon()
        {
            const string pathVariable = "BACKPACK_PROFILE_PATH";
            string previousPath = System.Environment.GetEnvironmentVariable(pathVariable);
            string directory = Path.Combine(Path.GetTempPath(), "BackpackCompendium_" + System.Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "profile.json");
            System.Environment.SetEnvironmentVariable(pathVariable, path);
            try
            {
                yield return Setup();
                Click("Main Compendium Button");
                Transform popup = Find("Item Compendium Popup");
                Assert.That(popup.gameObject.activeInHierarchy, Is.True);
                Assert.That(_controller.ShowStageSelection(), Is.False);
                Transform grid = Find("Compendium Grid Content");
                Assert.That(grid.childCount, Is.GreaterThan(8));
                float TileY(int i) => ((RectTransform)grid.GetChild(i)).anchoredPosition.y;
                Assert.That(TileY(7), Is.EqualTo(TileY(0)));
                Assert.That(TileY(8), Is.LessThan(TileY(7)));
                var scroll = Find("Compendium Grid Viewport").GetComponent<ScrollRect>();
                Canvas.ForceUpdateCanvases();
                scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.down });
                Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(1f));
                float scrollPosition = scroll.verticalNormalizedPosition;
                var shopItem = _controller.ItemCatalog.Single(item => item.ItemId == "wooden_sword");
                Click("Compendium Item wooden_sword");
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(scrollPosition).Within(.001f));
                var itemDetails = popup.GetComponentsInChildren<PrototypeItemTooltip>().Single();
                Assert.That(itemDetails.GetComponentInChildren<TMPro.TMP_Text>().text,
                    Does.Contain("가격 : 3G").And.Not.Contain("구매가").And.Not.Contain("판매가"));
                Assert.That(itemDetails.transform.Find("Tooltip Stats/Attack Value").GetComponent<Text>().text,
                    Is.EqualTo($"{shopItem.ResolveWeaponStats(_controller.GetCharacterStats(_controller.SelectedCharacter)).AttackDamage:0.#}"));
                Assert.That(popup.GetComponentsInChildren<Text>().Any(text => text.text == "이 아이템을 재료로 쓰는 조합"), Is.True);
                Capture("CompendiumShopItem.png");

                Click("Compendium Item dragonslayer");
                Assert.That(itemDetails.gameObject.activeSelf, Is.False);
                Assert.That(Text("Compendium Unknown Name"), Is.EqualTo("???"));
                var hiddenMaterial = popup.GetComponentsInChildren<Transform>()
                    .Single(item => item.name == "Compendium Recipe Item war_hero_halberd");
                Assert.That(hiddenMaterial.Find("Compendium Artwork").GetComponent<Image>().color, Is.EqualTo(Color.black));
                Assert.That(hiddenMaterial.Find("Recipe Name").GetComponent<Text>().text, Is.EqualTo("???"));
                Assert.That(popup.GetComponentsInChildren<Text>().Count(text => text.name == "Compendium Recipe Heading"), Is.EqualTo(1));
                Capture("CompendiumUndiscovered.png");

                hiddenMaterial.GetComponent<Button>().onClick.Invoke();
                Assert.That(itemDetails.gameObject.activeSelf, Is.False);
                Assert.That(popup.GetComponentsInChildren<Text>().Where(text => text.name == "Compendium Recipe Heading").Select(text => text.text),
                    Is.EqualTo(new[] { "이 아이템을 만드는 조합", "이 아이템을 재료로 쓰는 조합" }));
                Transform lockedUpgrade = popup.GetComponentsInChildren<Transform>()
                    .Single(item => item.name == "Compendium Recipe Item dragonslayer");
                Assert.That(lockedUpgrade.Find("Compendium Artwork").GetComponent<Image>().color, Is.EqualTo(Color.black));
                Assert.That(lockedUpgrade.Find("Recipe Name").GetComponent<Text>().text, Is.EqualTo("???"));
                Capture("CompendiumUndiscoveredUses.png");

                Click("Compendium Item lunar_eclipse");
                var equation = popup.GetComponentsInChildren<Transform>().Where(item =>
                    item.name.StartsWith("Compendium Recipe Item ") || item.name == "Recipe Connector");
                Assert.That(equation.OrderBy(item => ((RectTransform)item).anchoredPosition.x)
                    .Select(item => item.name == "Recipe Connector" ? item.GetComponent<Text>().text : item.name),
                    Is.EqualTo(new[] { "Compendium Recipe Item great_sword", "+", "Compendium Recipe Item moonflower", "+",
                        "Compendium Recipe Item moonflower", "→", "Compendium Recipe Item lunar_eclipse" }));
                Assert.That(popup.GetComponentsInChildren<Text>().Any(text => text.name == "Recipe Role" || text.text.Contains("×2")), Is.False);
                Capture("CompendiumRepeatedIngredient.png");

                Click("Item Compendium Popup Close");
                EnterStage();

                var recipe = _controller.FusionRecipes.Single(entry => entry.ResultItem.ItemId == "war_hero_halberd");
                Assert.That(PrototypeCombatFixture.PlaceItem(_controller, recipe.CenterItem.ItemId, "center", new Vector2Int(3, 0)), Is.True);
                Assert.That(PrototypeCombatFixture.PlaceItem(_controller, recipe.Ingredients.Single().Item.ItemId, "ingredient", new Vector2Int(4, 1)), Is.True);
                Assert.That(_controller.GetReadyFusions().Single().Result, Is.SameAs(recipe.ResultItem));
                Assert.That(_controller.TryStartBattle(), Is.True);
                foreach (var weapon in _controller.ActiveCombat.PlayerWeapons)
                {
                    weapon.Stats.AttackDamage = 10000;
                    weapon.Stats.AccuracyPercent = 100;
                }
                _controller.TickCombat(6);
                Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
                Assert.That(_controller.Progress.HasCraftedStageItem(recipe.ResultItem.ItemId), Is.False);
                Assert.That(_controller.AdvanceRound(), Is.True);
                _controller.TickFusionPresentation(5f);
                Assert.That(new PlayerProgressStore(path).Load(out _).HasCraftedStageItem(recipe.ResultItem.ItemId), Is.True);
                Click("Abandon Stage Button");
                Click("Confirm Abandon Stage");
                Assert.That(_controller.IsMainMenu, Is.True);
                Click("Main Compendium Button");
                yield return null;
                Click("Compendium Item war_hero_halberd");
                Assert.That(itemDetails.gameObject.activeInHierarchy, Is.True);
                Assert.That(itemDetails.transform.Find("Tooltip Name").GetComponent<Text>().text, Is.EqualTo(recipe.ResultItem.DisplayName));
                Assert.That(itemDetails.GetComponentInChildren<TMPro.TMP_Text>(true).text, Does.Not.Contain("가격"));
                var hiddenResult = popup.GetComponentsInChildren<Transform>()
                    .Single(item => item.name == "Compendium Recipe Item dragonslayer");
                Assert.That(hiddenResult.Find("Recipe Name").GetComponent<Text>().text, Is.EqualTo("???"));
                Capture("CompendiumDiscovered.png");
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(pathVariable, previousPath);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [UnityTest]
        public IEnumerator GameOver_ReturnsToMain_ClearsOldItemsOffersAndStatistics_ThenStartsNewRun()
        {
            yield return Setup(); EnterStage();
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            slot.Bind(new ShopOffer(_controller.ItemCatalog.Single(i => i.ItemId == "healing_potion"), true));
            _controller.TryToggleShopLock(slot); var previousOffer = slot.Offer;
            var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Freeform Storage")).position; slot.OnDrag(pointer); slot.OnEndDrag(pointer);
            Assert.That(_controller.StoredItemCount, Is.EqualTo(1));
            for (int i = 0; i < _controller.StartingLives; i++)
            {
                Lose(); if (i < _controller.StartingLives - 1) _controller.ReturnToPreparation();
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
            EnterStage();
            Assert.That(_controller.Lives, Is.EqualTo(4)); Assert.That(_controller.Gold, Is.EqualTo(20));
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.RunResult, Is.Null);
            Assert.That(slot.Offer, Is.Not.SameAs(previousOffer)); Assert.That(slot.Offer.IsLocked, Is.False);
            Assert.That(result.Lives, Is.Zero); Assert.That(result.Defeats, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator ClearedRun_ReturnsToMainAndRecapturesOutsideStatsOnlyOnEntry()
        {
            yield return Setup(); EnterStage();
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            _controller.TryStartBattle(); var sword = _controller.ActiveCombat.PlayerWeapons.First();
            sword.Stats.AttackDamage = 10000; sword.Stats.AccuracyPercent = 100; _controller.TickCombat(2);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed)); var result = _controller.RunResult;
            var original = Character(); var upgraded = Object.Instantiate(original);
            try
            {
                Set(upgraded, "_maximumHealth", 80f); Set(upgraded, "_attack", 20f); Set(_controller, "_characterData", upgraded);
                Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(10.5));
                Click("Return To Main Menu Button");
                Assert.That(Text("Main Character Health"), Is.EqualTo("체력  84"));
                Assert.That(Text("Main Character Attack"), Is.EqualTo("공격력  21"));
                Capture("MainMenuAfterClear.png");
                EnterStage();
                Assert.That(_controller.StageEntryStats.MaximumHealth, Is.EqualTo(84));
                Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(21));
                Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(result.Round, Is.EqualTo(12));
                _controller.TryStartBattle(); Assert.That(_controller.ActiveCombat.PlayerHealth, Is.EqualTo(84));
                Assert.That(_controller.ActiveCombat.PlayerWeapons.First().Stats.AttackDamage, Is.EqualTo(11.55).Within(.001));
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
            Assert.That(_controller.CaptureCombatSnapshot().Items.Single().ItemId, Is.EqualTo("knight_sword"));
        }

        private void EnterStage()
        {
            Click("Main Tab Stages");
            Assert.That(Text("Stage Confirm Button Label"), Is.EqualTo("스테이지 입장"));
            Click("Stage Confirm Button");
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.MainMenu));
            Click("Stage Entry Start Button");
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
