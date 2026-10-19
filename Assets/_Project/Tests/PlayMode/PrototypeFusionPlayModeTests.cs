using System.Collections;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeFusionPlayModeTests
    {
        private BackpackPrototypeController _controller;
        private ItemData Stone => _controller.ItemCatalog.Single(item => item.ItemId == "whetstone");
        private BackpackItemView Sword => Bag().Single(item => item.Data.ItemId == "training_blade");
        private PrototypeFusionOverlay Overlay => _controller.GetComponentInChildren<PrototypeFusionOverlay>(true);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            _controller.BattleController.AutoTick = false;
            var potion = _controller.ItemCatalog.Single(item => item.ItemId == "healing_potion");
            foreach (var slot in Slots()) slot.Bind(new ShopOffer(potion, false));
        }

        [UnityTest]
        public IEnumerator ReadyPair_FightsAsMaterials_ThenFusesOnceAtReturnAndSellsForThree()
        {
            BuyStone(Rect("Grid Cell 1,2"));
            yield return null;
            Assert.That(_controller.GetReadyFusions().Count, Is.EqualTo(1));
            Assert.That(Overlay.ReadyLinkCount, Is.EqualTo(1));
            Assert.That(Overlay.HintLinkCount, Is.Zero);
            Assert.That(Overlay.raycastTarget, Is.False);
            Assert.That(Overlay.GetComponentsInChildren<UnityEngine.UI.Text>().Single().text, Is.EqualTo("강철 검"));
            Canvas.ForceUpdateCanvases();
            Assert.That(Overlay.canvasRenderer.GetMesh().vertexCount, Is.EqualTo(4));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionReady.png");
            int gold = _controller.Gold;
            string swordId = Sword.InstanceId;
            Assert.That(_controller.TryStartBattle(), Is.True);
            Assert.That(_controller.ActiveCombat.PlayerWeapons.Single().Snapshot.ItemId, Is.EqualTo("training_blade"));
            Assert.That(_controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(6f), "Whetstone still buffs the training blade during this battle.");
            Assert.That(_controller.TryToggleFusionLock(swordId), Is.False);
            Assert.That(Bag().Any(view => view.Data.ItemId == "steel_sword"), Is.False);
            Win();
            Assert.That(_controller.AdvanceRound(), Is.True);
            yield return null;
            var steel = Bag().Single(view => view.Data.ItemId == "steel_sword");
            Assert.That(steel.Origin, Is.EqualTo(Vector2Int.zero));
            Assert.That(steel.Rotation, Is.EqualTo(ItemRotation.None));
            Assert.That(Bag().Any(view => view.Data.ItemId == "training_blade" || view.Data.ItemId == "whetstone"), Is.False);
            Assert.That(_controller.Gold, Is.EqualTo(gold + 5));
            Assert.That(_controller.GetReadyFusions(), Is.Empty);
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionResult.png");
            Assert.That(_controller.ReturnToPreparation(), Is.False);
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(Bag().Count(view => view.Data.ItemId == "steel_sword"), Is.EqualTo(1));
            gold = _controller.Gold;
            MoveBag(steel, Slots()[0].transform);
            yield return null;
            Assert.That(_controller.Gold, Is.EqualTo(gold + 3));
        }

        [UnityTest]
        public IEnumerator HoverHints_ConnectShopStorageAndBag_ExcludeSoldOffersAndDisappearOnExit()
        {
            BuyStone(Rect("Freeform Storage"));
            yield return null;
            Slots()[1].Bind(new ShopOffer(Stone, false));
            var pointer = Pointer(Sword.transform);
            Sword.OnPointerEnter(pointer);
            _controller.RefreshFusionVisuals();
            Assert.That(Overlay.ReadyLinkCount, Is.Zero);
            Assert.That(Overlay.HintLinkCount, Is.EqualTo(2), "Storage and one available shelf offer, not the purchased offer.");
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionHoverHints.png");
            Sword.OnPointerExit(pointer);
            _controller.RefreshFusionVisuals();
            Assert.That(Overlay.HintLinkCount, Is.Zero);
            Assert.That(_controller.GetReadyFusions(), Is.Empty);
            Assert.That(_controller.TryStartBattle(), Is.True); Win(); _controller.AdvanceRound();
            yield return null;
            Assert.That(Bag().Any(item => item.Data.ItemId == "steel_sword"), Is.False);
            Assert.That(_controller.StoredItemCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ShiftRightClickLock_PersistsAcrossStorageAndPreventsFusionButNotPlacementBonus()
        {
            BuyStone(Rect("Grid Cell 1,2")); yield return null;
            var stone = Bag().Single(item => item.Data == Stone);
            string id = stone.InstanceId;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift)); InputSystem.Update();
                var click = Pointer(stone.transform); click.button = PointerEventData.InputButton.Right;
                stone.OnPointerClick(click);
                Assert.That(_controller.IsFusionLocked(id), Is.True);
                Assert.That(stone.Rotation, Is.EqualTo(ItemRotation.None));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            _controller.RefreshFusionVisuals();
            Assert.That(Overlay.ReadyLinkCount, Is.Zero);
            Assert.That(Overlay.HintLinkCount, Is.Zero);
            Assert.That(Overlay.GetComponentsInChildren<PrototypeShopLockIcon>().Length, Is.EqualTo(1));
            var hover = Pointer(stone.transform); stone.OnPointerEnter(hover); _controller.RefreshFusionVisuals();
            Assert.That(Overlay.HintLinkCount, Is.EqualTo(1));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionLocked.png");
            stone.OnPointerExit(hover);
            _controller.RefreshFusionVisuals();
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionLockedNoHover.png");
            MoveBag(stone, Rect("Freeform Storage")); yield return null;
            Assert.That(_controller.IsFusionLocked(id), Is.True);
            var stored = _controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
            MoveStorage(stored, Rect("Grid Cell 1,2")); yield return null;
            Assert.That(_controller.IsFusionLocked(id), Is.True);
            Assert.That(_controller.GetReadyFusions(), Is.Empty);
            _controller.TryStartBattle();
            Assert.That(_controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(6f));
            Win(); _controller.AdvanceRound(); yield return null;
            Assert.That(Bag().Any(item => item.Data.ItemId == "steel_sword"), Is.False);
            Assert.That(_controller.TryToggleFusionLock(id), Is.True);
            Assert.That(_controller.GetReadyFusions().Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RepositionAndCancel_UpdateReadiness_AndDebugAdvanceDoesNotCraft()
        {
            BuyStone(Rect("Grid Cell 1,2")); yield return null;
            var stone = Bag().Single(item => item.Data == Stone);
            var pointer = Pointer(stone.transform);
            stone.OnBeginDrag(pointer); _controller.RefreshFusionVisuals();
            Assert.That(Overlay.ReadyLinkCount, Is.Zero);
            Assert.That(_controller.TryToggleFusionLock(stone.InstanceId), Is.False);
            pointer.position = Screen(Rect("Grid Cell 1,0"));
            stone.OnDrag(pointer); stone.OnEndDrag(pointer); yield return null;
            Assert.That(stone.Origin, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(Overlay.ReadyLinkCount, Is.EqualTo(1));
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(_controller.GetReadyFusions().Count, Is.EqualTo(1));
            Assert.That(Bag().Any(item => item.Data.ItemId == "steel_sword"), Is.False);
            MoveBag(stone, Rect("Grid Cell 5,5")); yield return null;
            Assert.That(_controller.GetReadyFusions(), Is.Empty);
            Assert.That(Overlay.ReadyLinkCount, Is.Zero);
            _controller.TryStartBattle(); Win(); _controller.AdvanceRound(); yield return null;
            Assert.That(Bag().Any(item => item.Data.ItemId == "steel_sword"), Is.False);
        }

        [UnityTest]
        public IEnumerator DefeatAndDraw_ReturnToShopApplyPendingPlansWithoutIncome()
        {
            BuyStone(Rect("Grid Cell 1,2")); yield return null;
            int gold = _controller.Gold;
            _controller.TryStartBattle();
            _controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage = 0;
            _controller.ActiveCombat.EnemyWeapons.Single().Stats.AttackDamage = 100;
            _controller.TickCombat(2f);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
            Assert.That(_controller.ReturnToPreparation(), Is.True);
            yield return null;
            Assert.That(Bag().Count(item => item.Data.ItemId == "steel_sword"), Is.EqualTo(1));
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.Gold, Is.EqualTo(gold));
            Assert.That(_controller.ReturnToPreparation(), Is.False);

            yield return Setup();
            BuyStone(Rect("Grid Cell 1,2")); yield return null;
            Set(_controller, "_maximumBattleSeconds", 1f);
            _controller.TryStartBattle(); _controller.TickCombat(1f);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Draw));
            _controller.ReturnToPreparation(); yield return null;
            Assert.That(Bag().Count(item => item.Data.ItemId == "steel_sword"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ThreeMaterialRecipe_DrawsCenterBranchesAndConsumesAllThreeOnce()
        {
            var recipe = Object.Instantiate(_controller.FusionRecipes.Single());
            try
            {
                var shield = _controller.ItemCatalog.Single(item => item.ItemId == "wooden_shield");
                Set(recipe, "_ingredients", new System.Collections.Generic.List<FusionIngredient>
                    { new(Stone), new(shield) });
                Set(_controller, "_fusionRecipes", new[] { recipe });
                BuyStone(Rect("Grid Cell 1,2")); yield return null;
                Assert.That(_controller.GetReadyFusions().Single().Materials.Count, Is.EqualTo(3));
                Assert.That(Overlay.ReadyLinkCount, Is.EqualTo(2));
                string shieldId = Bag().Single(view => view.Data == shield).InstanceId;
                _controller.TryToggleFusionLock(shieldId);
                Assert.That(_controller.GetReadyFusions(), Is.Empty);
                _controller.TryToggleFusionLock(shieldId);
                _controller.TryStartBattle(); Win(); _controller.AdvanceRound(); yield return null;
                Assert.That(Bag().Select(view => view.Data.ItemId), Is.EquivalentTo(new[] { "steel_sword", "swift_charm" }));
            }
            finally { Object.Destroy(recipe); }
        }

        [UnityTest]
        public IEnumerator FusionOnlyOutput_IsExcludedEvenIfAddedToShopCatalog()
        {
            ItemData output = _controller.FusionRecipes.Single().ResultItem;
            Set(_controller, "_itemCatalog", new[] { output, Stone });
            for (int i = 0; i < 10; i++)
            {
                _controller.RefreshShopForTesting();
                Assert.That(_controller.CurrentShopItems.All(item => item == Stone), Is.True);
            }
            Set(_controller, "_itemCatalog", new[] { output }); _controller.RefreshShopForTesting();
            Assert.That(_controller.CurrentShopItems, Is.Empty);
            Assert.That(Slots().All(slot => !slot.CanPurchase), Is.True);
            yield return null;
        }

        private void BuyStone(Transform destination)
        {
            var slot = Slots()[0]; slot.Bind(new ShopOffer(Stone, false));
            var pointer = Pointer(slot.transform);
            slot.OnBeginDrag(pointer); pointer.position = Screen(destination); slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private void Win()
        {
            var weapon = _controller.ActiveCombat.PlayerWeapons.Single();
            weapon.Stats.AttackDamage = 1000; weapon.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2f); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
        }
        private static void MoveBag(BackpackItemView view, Transform destination)
        { var pointer = Pointer(view.transform); view.OnBeginDrag(pointer); pointer.position = Screen(destination); view.OnDrag(pointer); view.OnEndDrag(pointer); }
        private static void MoveStorage(PrototypeStorageItemView view, Transform destination)
        { var pointer = Pointer(view.transform); view.OnBeginDrag(pointer); pointer.position = Screen(destination); view.OnDrag(pointer); view.OnEndDrag(pointer); }
        private BackpackItemView[] Bag() => _controller.GetComponentsInChildren<BackpackItemView>();
        private PrototypeShopSlotView[] Slots() => _controller.GetComponentsInChildren<PrototypeShopSlotView>(true).OrderBy(slot => slot.name).ToArray();
        private RectTransform Rect(string name) => (RectTransform)_controller.GetComponentsInChildren<Transform>(true).Single(transform => transform.name == name);
        private static Vector2 Screen(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current) { position = Screen(target), button = PointerEventData.InputButton.Left };
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
