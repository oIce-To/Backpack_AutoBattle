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
    public sealed class PrototypePlayModeTests
    {
        [UnityTest]
        public IEnumerator PrototypeScene_SupportsDragPurchaseStoragePlacementAndEconomy()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return loadOperation;
            yield return null;

            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            PrototypeShopSlotView[] shopSlots = Object.FindObjectsByType<PrototypeShopSlotView>(FindObjectsSortMode.None);

            Assert.That(controller, Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None), Has.Length.EqualTo(3));
            Assert.That(controller.ItemCatalog.Count, Is.EqualTo(15));
            Assert.That(shopSlots, Has.Length.EqualTo(4));
            Assert.That(controller.CurrentShopItems, Has.Count.EqualTo(4));
            Assert.That(controller.CurrentShopItems.All(item => item.Rarity != ItemRarity.Epic), Is.True);
            Assert.That(controller.CurrentShopItems.All(item => item.Rarity != ItemRarity.Legendary), Is.True);
            Assert.That(controller.Gold, Is.EqualTo(20));
            Assert.That(controller.CurrentRound, Is.EqualTo(1));
            Assert.That(controller.StoredItemCount, Is.Zero);

            PrototypeShopSlotView purchaseSlot = shopSlots.OrderBy(slot => slot.ItemData.PurchasePrice).First();
            int purchasePrice = purchaseSlot.PurchasePrice;
            Transform storageArea = controller.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "Freeform Storage");
            var pointerEvent = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, purchaseSlot.transform.position)
            };

            purchaseSlot.OnBeginDrag(pointerEvent);
            pointerEvent.position = RectTransformUtility.WorldToScreenPoint(null, storageArea.position);
            purchaseSlot.OnDrag(pointerEvent);
            purchaseSlot.OnEndDrag(pointerEvent);
            yield return null;

            Assert.That(controller.Gold, Is.EqualTo(20 - purchasePrice));
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(purchaseSlot.CanPurchase, Is.False);
            Assert.That(controller.CurrentShopItems, Has.Count.EqualTo(3));

            // The same shelf entry cannot be bought again, even by another drag.
            purchaseSlot.OnBeginDrag(pointerEvent);
            purchaseSlot.OnDrag(pointerEvent);
            purchaseSlot.OnEndDrag(pointerEvent);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(20 - purchasePrice));
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            PrototypeStorageItemView storedView = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            Assert.That(storedView, Is.Not.Null);

            Transform targetCell = controller.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "Grid Cell 0,3");
            pointerEvent.position = RectTransformUtility.WorldToScreenPoint(null, storedView.transform.position);
            storedView.OnBeginDrag(pointerEvent);
            pointerEvent.position = RectTransformUtility.WorldToScreenPoint(null, targetCell.position);
            storedView.OnDrag(pointerEvent);
            storedView.OnEndDrag(pointerEvent);
            yield return null;

            Assert.That(controller.StoredItemCount, Is.Zero);
            BackpackItemView purchasedItem = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(item => item.InstanceId.StartsWith("purchased_"));
            Assert.That(Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None), Has.Length.EqualTo(4));

            int goldBeforeReroll = controller.Gold;
            Assert.That(controller.TryRerollShop(), Is.True);
            Assert.That(controller.Gold, Is.EqualTo(goldBeforeReroll - 1));
            Assert.That(controller.CurrentShopItems.All(item => item.Rarity != ItemRarity.Epic), Is.True);

            Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(controller.CurrentRound, Is.EqualTo(2));
            Assert.That(controller.Gold, Is.EqualTo(goldBeforeReroll - 1 + 5));

            int salePrice = ShopPriceRules.HalfRoundedUp(purchasedItem.Data.PurchasePrice);
            Transform sellArea = controller.GetComponentsInChildren<Transform>(true)
                .Single(child => child.name == "Prototype Shop");
            Text sellPreview = controller.GetComponentsInChildren<Text>(true)
                .Single(text => text.name == "Sell Price");
            int goldBeforeSale = controller.Gold;
            pointerEvent.position = RectTransformUtility.WorldToScreenPoint(null, purchasedItem.transform.position);
            purchasedItem.OnBeginDrag(pointerEvent);
            Assert.That(sellPreview.gameObject.activeInHierarchy, Is.True);
            Assert.That(sellPreview.text, Does.Contain($"+{salePrice} G"));
            pointerEvent.position = RectTransformUtility.WorldToScreenPoint(null, sellArea.position);
            purchasedItem.OnDrag(pointerEvent);
            purchasedItem.OnEndDrag(pointerEvent);
            yield return null;

            Assert.That(controller.Gold, Is.EqualTo(goldBeforeSale + salePrice));
            Assert.That(sellPreview.gameObject.activeInHierarchy, Is.False);
            Assert.That(Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None), Has.Length.EqualTo(3));
            Assert.That(controller.GetComponentsInChildren<Transform>(true).Count(child => child.name.StartsWith("Grid Cell ")), Is.EqualTo(36));
        }

        [UnityTest]
        public IEnumerator DiscountedOffers_AreIndependentAndOwnedItemsCanReappear()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;

            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            ItemData potion = controller.ItemCatalog.Single(item => item.ItemId == "healing_potion");
            SetControllerField(controller, "_itemCatalog", new[] { potion });
            SetControllerField(controller, "_saleChance", 1f);
            controller.RefreshShopForTesting();
            yield return null;

            PrototypeShopSlotView[] slots = Object.FindObjectsByType<PrototypeShopSlotView>(FindObjectsSortMode.None)
                .OrderBy(slot => slot.name).ToArray();
            Assert.That(slots, Has.Length.EqualTo(4));
            Assert.That(slots.All(slot => slot.ItemData == potion && slot.Offer.IsOnSale), Is.True);
            Assert.That(slots.Select(slot => slot.Offer).Distinct().Count(), Is.EqualTo(4));
            Assert.That(slots.All(slot => slot.PurchasePrice == 2), Is.True);
            Text discountedPrice = slots[0].GetComponentsInChildren<Text>().Single(text => text.name == "Purchase Price");
            Assert.That(discountedPrice.text, Is.EqualTo("2 G  Sale!"));
            Assert.That(discountedPrice.color.r, Is.GreaterThan(discountedPrice.color.g));
            Image strike = slots[0].GetComponentsInChildren<Image>().Single(image => image.name == "Price Strike");
            Assert.That(strike.gameObject.activeInHierarchy, Is.True);
            Assert.That(strike.rectTransform.rect.width, Is.GreaterThan(0f));

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Transform storageArea = FindChild(controller, "Freeform Storage");
            var pointer = new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left };
            DragShopSlot(slots[0], pointer, storageArea.position);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(18));
            Assert.That(slots[0].CanPurchase, Is.False);
            Assert.That(slots.Skip(1).All(slot => slot.CanPurchase), Is.True);
            Assert.That(controller.CurrentShopItems, Has.Count.EqualTo(3));

            DragShopSlot(slots[1], pointer, FindChild(controller, "Grid Cell 5,5").position);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(16));
            Assert.That(slots[1].CanPurchase, Is.False);
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None), Has.Length.EqualTo(4));

            // Both purchased/owned copies still allow the item to appear in a new shop.
            SetControllerField(controller, "_saleChance", 0f);
            controller.RefreshShopForTesting();
            yield return null;
            Assert.That(slots.All(slot => slot.CanPurchase && slot.ItemData == potion && !slot.Offer.IsOnSale), Is.True);
            Assert.That(slots.All(slot => slot.PurchasePrice == 3), Is.True);
            Assert.That(strike.gameObject.activeInHierarchy, Is.False);

            PrototypeStorageItemView stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, stored.transform.position);
            stored.OnBeginDrag(pointer);
            Text preview = controller.GetComponentsInChildren<Text>(true).Single(text => text.name == "Sell Price");
            Assert.That(preview.gameObject.activeInHierarchy, Is.True);
            Assert.That(preview.text, Does.Contain("+2 G"));
            // Dropping over a stocked card also sells, since the whole shop is the sale area.
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, slots[2].transform.position);
            stored.OnDrag(pointer);
            Assert.That(preview.text, Does.Contain("놓으면 판매됩니다"));
            stored.OnEndDrag(pointer);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(18));
            Assert.That(controller.StoredItemCount, Is.Zero);
            Assert.That(slots[2].CanPurchase, Is.True);
            Assert.That(preview.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator CancelledOrUnaffordablePurchase_PreservesShelfOfferAndGold()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            ItemData potion = controller.ItemCatalog.Single(item => item.ItemId == "healing_potion");
            SetControllerField(controller, "_itemCatalog", new[] { potion });
            SetControllerField(controller, "_saleChance", 1f);
            controller.RefreshShopForTesting();
            PrototypeShopSlotView slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            ShopOffer offer = slot.Offer;
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>())
            {
                button = PointerEventData.InputButton.Left
            };

            DragShopSlot(slot, pointer, FindChild(controller, "Grid Cell 0,0").position);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(20));
            Assert.That(slot.CanPurchase, Is.True);
            Assert.That(slot.Offer, Is.SameAs(offer));

            DragShopSlot(slot, pointer, FindChild(controller, "Prototype Shop").position);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(20));
            Assert.That(slot.CanPurchase, Is.True);
            Assert.That(controller.StoredItemCount, Is.Zero);

            while (controller.Gold > 0)
            {
                Assert.That(controller.TryRerollShop(), Is.True);
            }

            offer = slot.Offer;
            DragShopSlot(slot, pointer, FindChild(controller, "Freeform Storage").position);
            yield return null;
            Assert.That(controller.Gold, Is.Zero);
            Assert.That(controller.StoredItemCount, Is.Zero);
            Assert.That(slot.CanPurchase, Is.True);
            Assert.That(slot.Offer, Is.SameAs(offer));
            Assert.That(controller.CurrentShopItems, Has.Count.EqualTo(4));
        }

        private static void SetControllerField(BackpackPrototypeController controller, string name, object value)
        {
            typeof(BackpackPrototypeController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, value);
        }

        private static Transform FindChild(BackpackPrototypeController controller, string name)
        {
            return controller.GetComponentsInChildren<Transform>(true).Single(child => child.name == name);
        }

        private static void DragShopSlot(PrototypeShopSlotView slot, PointerEventData pointer, Vector3 destination)
        {
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, slot.transform.position);
            slot.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, destination);
            slot.OnDrag(pointer);
            slot.OnEndDrag(pointer);
        }
    }
}
