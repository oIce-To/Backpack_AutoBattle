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
            _controller.AutoTickFusionPresentation = false;
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
            Assert.That(Overlay.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(100), "Curved strokes include feathered edges, a glow and rounded caps.");
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
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Fusion));
            Assert.That(_controller.CanEditLoadout, Is.False);
            Assert.That(_controller.IsFusionPresenting, Is.True);
            Assert.That(_controller.ActiveCombat, Is.Null);
            Assert.That(_controller.TryStartBattle(), Is.False);
            Assert.That(_controller.TryRerollShop(), Is.False);
            Assert.That(_controller.AdvanceRound(), Is.False);
            Assert.That(_controller.ReturnToPreparation(), Is.False);
            Assert.That(_controller.TryToggleShopLock(Slots()[1]), Is.False);
            Assert.That(_controller.RefreshEncounterPreview(), Is.False);
            var before = Slots()[1].Offer;
            var blockedPointer = Pointer(Slots()[1].transform);
            Slots()[1].OnBeginDrag(blockedPointer); blockedPointer.position = Screen(Rect("Freeform Storage"));
            Slots()[1].OnDrag(blockedPointer); Slots()[1].OnEndDrag(blockedPointer);
            Assert.That(Slots()[1].Offer, Is.SameAs(before));
            Assert.That(before.IsPurchased, Is.False, "Dragging cannot purchase during the presentation.");
            yield return null; // Allow newly enabled graphics to register their canvas depths before raycasting.
            var hits = new System.Collections.Generic.List<RaycastResult>();
            blockedPointer.position = Screen(Rect("Reroll Button")); Canvas.ForceUpdateCanvases();
            EventSystem.current.RaycastAll(blockedPointer, hits);
            Assert.That(hits.First().gameObject.name, Is.EqualTo("Fusion Presentation"));
            Assert.That(_controller.StoredItemCount, Is.EqualTo(1));
            _controller.TickFusionPresentation(.24f);
            var ghostStone = Rect("Fusion Material 0:1");
            var ghostCenter = Rect("Fusion Material 0:0");
            Assert.That(Vector2.Distance(ghostStone.anchoredPosition, ghostCenter.anchoredPosition), Is.GreaterThan(1f));
            Assert.That(ghostStone.localScale.x, Is.LessThan(1f));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionGathering.png");
            _controller.TickFusionPresentation(.28f);
            Assert.That(Rect("Fusion Result 0").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionReveal.png");
            _controller.TickFusionPresentation(.35f);
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionFlying.png");
            _controller.TickFusionPresentation(5f);
            yield return null;
            var steel = Stored().Single(view => view.StoredItem.ItemData.ItemId == "steel_sword");
            Assert.That(steel.StoredItem.Rotation, Is.EqualTo(ItemRotation.None));
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(Bag().Any(view => view.Data.ItemId == "training_blade" || view.Data.ItemId == "whetstone"), Is.False);
            Assert.That(_controller.Gold, Is.EqualTo(gold + 5));
            Assert.That(_controller.GetReadyFusions(), Is.Empty);
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionResult.png");
            Assert.That(_controller.ReturnToPreparation(), Is.False);
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(Stored().Count(view => view.StoredItem.ItemData.ItemId == "steel_sword"), Is.EqualTo(1));
            gold = _controller.Gold;
            MoveStorage(steel, Slots()[0].transform);
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
            yield return new WaitForSecondsRealtime(.15f);
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
            var badge = stone.GetComponentInChildren<PrototypeShopLockIcon>();
            Assert.That(badge.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(badge.transform.GetSiblingIndex(), Is.LessThan(stone.transform.Find("Label").GetSiblingIndex()));
            Assert.That(stone.GetComponentsInChildren<UnityEngine.UI.Text>().Single().text, Is.EqualTo("숫돌"));
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
            _controller.TickFusionPresentation(5f);
            yield return null;
            Assert.That(Stored().Count(item => item.StoredItem.ItemData.ItemId == "steel_sword"), Is.EqualTo(1));
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.Gold, Is.EqualTo(gold));
            Assert.That(_controller.ReturnToPreparation(), Is.False);

            yield return Setup();
            BuyStone(Rect("Grid Cell 1,2")); yield return null;
            _controller.TryStartBattle();
            // An earlier explicit test deadline keeps this Draw-only compatibility path separate from the 90s forced defeat.
            var original = _controller.ActiveCombat;
            _controller.BattleController.BeginBattle(original.Player, original.Enemy, _controller.CurrentRound, 5, maximumSeconds: 1f);
            _controller.TickCombat(1f);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Draw));
            _controller.ReturnToPreparation(); _controller.TickFusionPresentation(5f); yield return null;
            Assert.That(Stored().Count(item => item.StoredItem.ItemData.ItemId == "steel_sword"), Is.EqualTo(1));
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
                _controller.TryStartBattle(); Win(); _controller.AdvanceRound(); _controller.TickFusionPresentation(5f); yield return null;
                Assert.That(Bag().Select(view => view.Data.ItemId), Is.EquivalentTo(new[] { "swift_charm" }));
                Assert.That(Stored().Single().StoredItem.ItemData.ItemId, Is.EqualTo("steel_sword"));
            }
            finally { Object.Destroy(recipe); }
        }

        [UnityTest]
        public IEnumerator FullBagAndBiggerOutput_CraftToStorage_InterruptionRevealsExactlyOneResult()
        {
            var recipe = Object.Instantiate(_controller.FusionRecipes.Single());
            var output = Object.Instantiate(recipe.ResultItem);
            try
            {
                Set(output, "_occupiedCells", new System.Collections.Generic.List<Vector2Int>
                    { new(0, 0), new(1, 0), new(0, 1), new(1, 1), new(0, 2), new(1, 2) });
                Set(recipe, "_resultItem", output); Set(_controller, "_fusionRecipes", new[] { recipe });
                BuyStone(Rect("Grid Cell 1,2")); yield return null;
                var grid = (BackpackGrid)Get(_controller, "_grid");
                var create = typeof(BackpackPrototypeController).GetMethod("CreateStartingItem", BindingFlags.NonPublic | BindingFlags.Instance);
                for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
                    if (!grid.TryGetItemAt(new Vector2Int(x, y), out _))
                        create.Invoke(_controller, new object[] { "healing_potion", $"fill_{x}_{y}", new Vector2Int(x, y) });
                Assert.That(_controller.GetReadyFusions().Count, Is.EqualTo(1));
                _controller.TryStartBattle(); Win(); _controller.AdvanceRound();
                Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Fusion));
                Assert.That(Stored().Count(view => view.StoredItem.ItemData == output), Is.EqualTo(1));
                _controller.TickFusionPresentation(.2f);
                _controller.gameObject.SetActive(false); // Interrupted visuals must not undo or repeat the transaction.
                Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
                Assert.That(_controller.IsFusionPresenting, Is.False);
                Assert.That(Stored().Single().gameObject.activeSelf, Is.True);
                _controller.gameObject.SetActive(true); yield return null;
                _controller.TickFusionPresentation(10f);
                Assert.That(Stored().Count(view => view.StoredItem.ItemData == output), Is.EqualTo(1));
                Assert.That(Bag().Any(view => view.Data.ItemId == "training_blade" || view.Data == Stone), Is.False);
            }
            finally { Object.Destroy(recipe); Object.Destroy(output); }
        }

        [UnityTest]
        public IEnumerator MultipleFusions_PlayWithPausedGameTime_AndAvoidExistingStorageItems()
        {
            BuyStone(Rect("Grid Cell 1,2"));
            var slot = Slots()[0]; slot.Bind(new ShopOffer(_controller.ItemCatalog.Single(item => item.ItemId == "training_blade"), false));
            var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer); pointer.position = Screen(Rect("Grid Cell 4,0"));
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
            BuyStone(Rect("Grid Cell 5,2"));
            slot.Bind(new ShopOffer(_controller.ItemCatalog.Single(item => item.ItemId == "healing_potion"), false));
            pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer); pointer.position = Screen(Rect("Freeform Storage"));
            slot.OnDrag(pointer); slot.OnEndDrag(pointer); yield return null;
            var existing = Stored().Single(); Vector2 existingPosition = existing.StoredItem.StoragePosition;
            Assert.That(_controller.GetReadyFusions().Count, Is.EqualTo(2));
            _controller.TryStartBattle();
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons) { weapon.Stats.AttackDamage = 1000; weapon.Stats.AccuracyPercent = 100; }
            _controller.TickCombat(2f); _controller.AdvanceRound();
            float oldScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                _controller.AutoTickFusionPresentation = true;
                yield return new WaitForSecondsRealtime(1.9f);
                Assert.That(_controller.IsFusionPresenting, Is.False, "Presentation uses unscaled delta time.");
                Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            }
            finally { Time.timeScale = oldScale; }
            var results = Stored().Where(view => view.StoredItem.ItemData.ItemId == "steel_sword").ToArray();
            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(existing.StoredItem.StoragePosition, Is.EqualTo(existingPosition));
            var all = Stored();
            for (int i = 0; i < all.Length; i++) for (int j = i + 1; j < all.Length; j++)
            {
                Vector2 sizeA = ((RectTransform)all[i].transform).rect.size, sizeB = ((RectTransform)all[j].transform).rect.size;
                Assert.That(new Rect(all[i].StoredItem.StoragePosition - sizeA * .5f, sizeA)
                    .Overlaps(new Rect(all[j].StoredItem.StoragePosition - sizeB * .5f, sizeB)), Is.False);
            }
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionMultipleResults.png");
        }

        [UnityTest]
        public IEnumerator LockedTooltips_KeepOnlyItemDescriptionsAndEffects_AndHintsFade()
        {
            BuyStone(Rect("Grid Cell 1,2")); yield return null;
            var stone = Bag().Single(view => view.Data == Stone);
            _controller.TryToggleFusionLock(stone.InstanceId);
            stone.OnPointerEnter(Pointer(stone.transform));
            yield return new WaitForSecondsRealtime(.16f);
            var text = _controller.ItemTooltip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text;
            Assert.That(text, Is.EqualTo(ItemTooltipContent.FromItem(Stone).GetDescriptionText()));
            Assert.That(text, Does.Not.Contain("합성"));
            Canvas.ForceUpdateCanvases();
            Assert.That(Overlay.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            stone.OnPointerExit(Pointer(stone.transform));
            yield return new WaitForSecondsRealtime(.18f);
            Canvas.ForceUpdateCanvases();
            Assert.That(Overlay.canvasRenderer.GetMesh().vertexCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CentralLocks_ShareNamePositionAndRenderBehindNames_InBagAndStorage()
        {
            var sword = Sword;
            Assert.That(_controller.TryToggleFusionLock(sword.InstanceId), Is.True);
            _controller.RefreshFusionVisuals(); Canvas.ForceUpdateCanvases();
            AssertLockBelowName(sword.RectTransform);
            MoveBag(sword, Rect("Freeform Storage")); yield return null;
            _controller.RefreshFusionVisuals(); Canvas.ForceUpdateCanvases();
            var stored = Stored().Single();
            Assert.That(_controller.IsFusionLocked(stored.StoredItem.InstanceId), Is.True);
            AssertLockBelowName((RectTransform)stored.transform);
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "FusionLockedSword.png");

            void AssertLockBelowName(RectTransform item)
            {
                var name = item.GetComponentInChildren<UnityEngine.UI.Text>();
                var badge = item.GetComponentInChildren<PrototypeShopLockIcon>();
                Assert.That(name.text, Is.EqualTo("훈련용 검"));
                Assert.That(name.gameObject.activeInHierarchy, Is.True);
                Assert.That(badge.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(Vector3.Distance(badge.transform.position, item.TransformPoint(item.rect.center)), Is.LessThan(.01f));
                Assert.That(badge.transform.GetSiblingIndex(), Is.LessThan(name.transform.GetSiblingIndex()));
                name.gameObject.SetActive(false);
                _controller.RefreshFusionVisuals();
                Assert.That(badge.gameObject.activeSelf, Is.True);
                Assert.That(badge.rectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
                name.gameObject.SetActive(true);
            }
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
        private PrototypeStorageItemView[] Stored() => _controller.GetComponentsInChildren<PrototypeStorageItemView>(true);
        private PrototypeShopSlotView[] Slots() => _controller.GetComponentsInChildren<PrototypeShopSlotView>(true).OrderBy(slot => slot.name).ToArray();
        private RectTransform Rect(string name) => (RectTransform)_controller.GetComponentsInChildren<Transform>(true).Single(transform => transform.name == name);
        private static Vector2 Screen(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current) { position = Screen(target), button = PointerEventData.InputButton.Left };
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
