using System.Collections;
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

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeShopLockPlayModeTests
    {
        [UnityTest]
        public IEnumerator RerollAndRoundAdvance_PreserveOnlyLockedOffersWithTheirExactSaleAndPrice()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            ItemData potion = Item(controller, "healing_potion");
            ItemData sword = Item(controller, "training_blade");
            Set(controller, "_itemCatalog", new[] { potion });
            Set(controller, "_saleChance", 1f);
            controller.RefreshShopForTesting();
            PrototypeShopSlotView[] slots = Slots(controller);
            slots[1].Bind(new ShopOffer(potion, false));
            ShopOffer sale = slots[0].Offer, fullPrice = slots[1].Offer, previousUnlocked = slots[2].Offer;
            RightClick(slots[0]); RightClick(slots[1]);
            Set(controller, "_itemCatalog", new[] { sword });
            Set(controller, "_saleChance", 0f);
            int gold = controller.Gold;
            Assert.That(controller.TryRerollShop(), Is.True);
            Assert.That(controller.Gold, Is.EqualTo(gold - 1));
            Assert.That(slots[0].Offer, Is.SameAs(sale));
            Assert.That(slots[1].Offer, Is.SameAs(fullPrice));
            Assert.That(slots[0].PurchasePrice, Is.EqualTo(2));
            Assert.That(slots[1].PurchasePrice, Is.EqualTo(3));
            Assert.That(slots[0].Offer.IsOnSale, Is.True);
            Assert.That(slots[1].Offer.IsOnSale, Is.False);
            Assert.That(slots[2].Offer, Is.Not.SameAs(previousUnlocked));
            Assert.That(slots.Skip(2).All(slot => slot.ItemData == sword && !slot.Offer.IsOnSale && !slot.Offer.IsLocked), Is.True);
            Assert.That(controller.CurrentShopItems, Is.EqualTo(new[] { potion, potion, sword, sword }));
            previousUnlocked = slots[2].Offer;
            Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(controller.CurrentRound, Is.EqualTo(2));
            Assert.That(controller.Gold, Is.EqualTo(gold - 1 + 5));
            Assert.That(slots[0].Offer, Is.SameAs(sale));
            Assert.That(slots[1].Offer, Is.SameAs(fullPrice));
            Assert.That(slots[2].Offer, Is.Not.SameAs(previousUnlocked));
            Assert.That(slots[0].GetComponentsInChildren<Text>().Single(text => text.name == "Purchase Price").text, Is.EqualTo("2 G  Sale!"));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "ShopLockedOffers.png");
            RightClick(slots[0]);
            Assert.That(slots[0].Offer.IsLocked, Is.False);
            Assert.That(controller.TryRerollShop(), Is.True);
            Assert.That(slots[0].Offer, Is.Not.SameAs(sale));
            Assert.That(slots[0].ItemData, Is.SameAs(sword));
            Assert.That(slots[1].Offer, Is.SameAs(fullPrice));
            Assert.That(Shade(slots[0]).gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator LockedPurchases_WorkInBagAndStorageAndPurchasedSlotsRefillUnlocked()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Set(controller, "_itemCatalog", new[] { Item(controller, "healing_potion") });
            Set(controller, "_saleChance", 1f);
            controller.RefreshShopForTesting();
            PrototypeShopSlotView[] slots = Slots(controller);
            ShopOffer first = slots[0].Offer, second = slots[1].Offer;
            RightClick(slots[0]); RightClick(slots[1]);
            Assert.That(slots[2].Offer.IsLocked, Is.False);
            Drag(slots[0], Rect(controller, "Grid Cell 5,5"));
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(18));
            Assert.That(first.IsPurchased, Is.True);
            Assert.That(first.IsLocked, Is.False);
            Assert.That(slots[0].CanPurchase, Is.False);
            Assert.That(Shade(slots[0]).gameObject.activeSelf, Is.False);
            Assert.That(Icon(slots[0]).gameObject.activeSelf, Is.False);
            RightClick(slots[0]);
            Assert.That(first.IsLocked, Is.False);
            Assert.That(slots[1].Offer.IsLocked, Is.True);
            Drag(slots[1], Rect(controller, "Freeform Storage"));
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(16));
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(second.IsPurchased, Is.True);
            Assert.That(second.IsLocked, Is.False);
            Assert.That(controller.CurrentShopItems.Count, Is.EqualTo(2));
            Assert.That(controller.TryRerollShop(), Is.True);
            Assert.That(slots[0].Offer, Is.Not.SameAs(first));
            Assert.That(slots[1].Offer, Is.Not.SameAs(second));
            Assert.That(slots.All(slot => slot.CanPurchase && !slot.Offer.IsLocked), Is.True);
            Assert.That(controller.CurrentShopItems.Count, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator RightClickLock_DoesNotBlockHoverOrRaycastsAndWorksWithoutGold()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            PrototypeShopSlotView[] slots = Slots(controller);
            slots[0].Bind(new ShopOffer(Item(controller, "healing_potion"), false));
            var run = (PrototypeRunState)Get(controller, "_run");
            typeof(PrototypeRunState).GetProperty("Gold").SetValue(run, 0);
            var pointer = Pointer(slots[0].transform);
            pointer.button = PointerEventData.InputButton.Left;
            slots[0].OnPointerClick(pointer);
            pointer.button = PointerEventData.InputButton.Middle;
            slots[0].OnPointerClick(pointer);
            Assert.That(slots[0].Offer.IsLocked, Is.False);
            RightClick(slots[0]);
            Assert.That(slots[0].Offer.IsLocked, Is.True);
            Assert.That(controller.Gold, Is.Zero);
            Assert.That(Shade(slots[0]).color.a, Is.GreaterThan(0.3f));
            Assert.That(Icon(slots[0]).raycastTarget, Is.False);
            Assert.That(Shade(slots[0]).raycastTarget, Is.False);
            slots[0].OnPointerEnter(pointer);
            Assert.That(controller.ItemTooltip.IsVisible, Is.True);
            Assert.That(controller.ItemTooltip.Owner, Is.SameAs(slots[0]));
            Canvas.ForceUpdateCanvases();
            Assert.That(Icon(slots[0]).canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            var hits = new System.Collections.Generic.List<RaycastResult>();
            pointer.position = Screen(Icon(slots[0]).transform);
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Any(hit => hit.gameObject == slots[0].gameObject), Is.True);
            Assert.That(hits.Any(hit => hit.gameObject == Icon(slots[0]).gameObject || hit.gameObject == Shade(slots[0]).gameObject), Is.False);
            ShopOffer offer = slots[0].Offer;
            Drag(slots[0], Rect(controller, "Freeform Storage"));
            yield return null;
            Assert.That(controller.StoredItemCount, Is.Zero);
            Assert.That(slots[0].Offer, Is.SameAs(offer));
            Assert.That(offer.IsLocked, Is.True);
            Assert.That(controller.TryRerollShop(), Is.False);
            Assert.That(slots[0].Offer, Is.SameAs(offer));
            RightClick(slots[0]);
            Assert.That(offer.IsLocked, Is.False);
        }

        [UnityTest]
        public IEnumerator CancelAndDragGuards_PreserveLocksAndPreventChangingThemDuringAnotherDrag()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            PrototypeShopSlotView[] slots = Slots(controller);
            slots[0].Bind(new ShopOffer(Item(controller, "healing_potion"), false));
            RightClick(slots[0]);
            var pointer = Pointer(slots[0].transform);
            slots[0].OnBeginDrag(pointer);
            Assert.That(controller.TryToggleShopLock(slots[0]), Is.False);
            RightClick(slots[1]);
            Assert.That(slots[1].Offer.IsLocked, Is.False);
            pointer.position = Screen(Rect(controller, "Grid Cell 0,0"));
            slots[0].OnDrag(pointer); slots[0].OnEndDrag(pointer);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(20));
            Assert.That(slots[0].Offer.IsLocked, Is.True);
            Assert.That(slots[0].CanPurchase, Is.True);
            pointer.button = PointerEventData.InputButton.Right;
            pointer.dragging = true;
            slots[0].OnPointerClick(pointer);
            Assert.That(slots[0].Offer.IsLocked, Is.True, "Dragging with the right mouse button is not a click.");
        }

        [UnityTest]
        public IEnumerator AllLockedOffers_SurviveRerollAndBattleAndCannotToggleOutsidePreparation()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            PrototypeShopSlotView[] slots = Slots(controller);
            foreach (PrototypeShopSlotView slot in slots) RightClick(slot);
            ShopOffer[] before = slots.Select(slot => slot.Offer).ToArray();
            int gold = controller.Gold;
            Assert.That(controller.TryRerollShop(), Is.True);
            Assert.That(controller.Gold, Is.EqualTo(gold - 1), "Existing reroll cost rules remain unchanged even when all slots are locked.");
            Assert.That(slots.Select(slot => slot.Offer), Is.EqualTo(before));
            Assert.That(controller.CurrentShopItems.Count, Is.EqualTo(4));
            Assert.That(controller.TryStartBattle(), Is.True);
            Assert.That(controller.TryToggleShopLock(slots[0]), Is.False);
            RightClick(slots[0]);
            Assert.That(slots[0].Offer.IsLocked, Is.True);
            var weapon = controller.ActiveCombat.PlayerWeapons.Single();
            weapon.Stats.AttackDamage = 100f; weapon.Stats.AccuracyPercent = 100f;
            controller.TickCombat(2f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Assert.That(controller.TryToggleShopLock(slots[0]), Is.False);
            Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(slots.Select(slot => slot.Offer), Is.EqualTo(before));
            Assert.That(slots.All(slot => slot.Offer.IsLocked), Is.True);
        }

        private static ItemData Item(BackpackPrototypeController controller, string id) => controller.ItemCatalog.Single(item => item.ItemId == id);
        private static PrototypeShopSlotView[] Slots(BackpackPrototypeController controller)
            => controller.GetComponentsInChildren<PrototypeShopSlotView>(true).OrderBy(slot => slot.name).ToArray();
        private static PrototypeShopLockIcon Icon(PrototypeShopSlotView slot) => slot.GetComponentInChildren<PrototypeShopLockIcon>(true);
        private static Image Shade(PrototypeShopSlotView slot) => slot.GetComponentsInChildren<Image>(true).Single(image => image.name == "Locked Offer Shade");
        private static RectTransform Rect(BackpackPrototypeController controller, string name)
            => (RectTransform)controller.GetComponentsInChildren<Transform>(true).Single(transform => transform.name == name);
        private static Vector2 Screen(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
            { position = Screen(target), button = PointerEventData.InputButton.Left };
        private static void RightClick(PrototypeShopSlotView slot)
        { var pointer = Pointer(slot.transform); pointer.button = PointerEventData.InputButton.Right; slot.OnPointerClick(pointer); }
        private static void Drag(PrototypeShopSlotView slot, Transform destination)
        {
            var pointer = Pointer(slot.transform);
            slot.OnBeginDrag(pointer); pointer.position = Screen(destination);
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
