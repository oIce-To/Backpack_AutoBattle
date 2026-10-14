using System.Collections;
using System.Linq;
using BackpackAutoBattle.Inventory;
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
    public sealed class PrototypeLayoutPlayModeTests
    {
        [UnityTest]
        public IEnumerator Storage_UsesRotatedOriginalShapeAndKeepsFreePositions()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var pointer = Pointer();
            RectTransform storage = Rect(controller, "Freeform Storage");
            BackpackItemView charm = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.InstanceId == "starter_swift_charm");
            controller.RotateItem(charm);
            Assert.That(charm.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            pointer.position = Screen(charm.transform);
            charm.OnBeginDrag(pointer);
            Vector2 desired = new Vector2(-83.25f, -9.75f);
            Vector2 centerOffset = new Vector2(charm.RectTransform.sizeDelta.x / 2f, -charm.RectTransform.sizeDelta.y / 2f);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, storage.TransformPoint(desired - centerOffset));
            charm.OnDrag(pointer);
            Assert.That(charm.transform.parent.GetSiblingIndex(), Is.GreaterThan(storage.GetSiblingIndex()),
                "Drag previews should not disappear behind the storage background.");
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "StorageDrag.png");
            charm.OnEndDrag(pointer);
            yield return null;

            var stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            Assert.That(stored.StoredItem.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Assert.That(stored.StoredItem.StoragePosition.x, Is.EqualTo(desired.x).Within(0.01f));
            Assert.That(stored.StoredItem.StoragePosition.y, Is.EqualTo(desired.y).Within(0.01f));
            AssertShape(stored);
            Assert.That(stored.GetComponent<Image>(), Is.Null, "No rectangular card should fill the shape's holes.");

            // Buying a differently shaped item into the same area remains possible: there is no grid occupancy.
            var slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(controller.ItemCatalog.Single(item => item.ItemId == "fire_staff"), false));
            pointer.position = Screen(slot.transform);
            slot.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, storage.TransformPoint(desired));
            slot.OnDrag(pointer);
            slot.OnEndDrag(pointer);
            yield return null;
            Assert.That(controller.StoredItemCount, Is.EqualTo(2));
            foreach (PrototypeStorageItemView view in Object.FindObjectsByType<PrototypeStorageItemView>(FindObjectsSortMode.None))
                AssertShape(view);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "ShopLayout.png");

            pointer.position = Screen(stored.transform);
            stored.OnBeginDrag(pointer);
            BackpackItemView proxy = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.InstanceId == "starter_swift_charm");
            Assert.That(proxy.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            pointer.position = Screen(Rect(controller, "Grid Cell 3,3"));
            stored.OnDrag(pointer);
            stored.OnEndDrag(pointer);
            yield return null;
            Assert.That(proxy.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Assert.That(proxy.Origin, Is.EqualTo(new Vector2Int(3, 3)));
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Storage_ClampsFullRotatedShapeAndPreservesRotationOnReposition()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var pointer = Pointer();
            RectTransform storage = Rect(controller, "Freeform Storage");
            var slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(controller.ItemCatalog.Single(item => item.ItemId == "fire_staff"), false));
            pointer.position = Screen(slot.transform);
            slot.OnBeginDrag(pointer);
            BackpackItemView proxy = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.Data.ItemId == "fire_staff");
            controller.RotateItem(proxy);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null,
                storage.TransformPoint(new Vector2(storage.rect.xMax - 1f, storage.rect.yMin + 1f)));
            slot.OnDrag(pointer);
            slot.OnEndDrag(pointer);
            yield return null;
            var stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            Assert.That(stored.StoredItem.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            AssertInsideStorage(stored, storage);

            pointer.position = Screen(stored.transform);
            stored.OnBeginDrag(pointer);
            proxy = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.Data.ItemId == "fire_staff");
            controller.RotateItem(proxy);
            pointer.position = Screen(storage);
            stored.OnDrag(pointer);
            stored.OnEndDrag(pointer);
            yield return null;
            Assert.That(stored.StoredItem.Rotation, Is.EqualTo(ItemRotation.Clockwise180));
            AssertShape(stored);
            AssertInsideStorage(stored, storage);
        }

        [UnityTest]
        public IEnumerator Layout_ReservesCharacterAndHealthArmorAreasBelowBothBags()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Assert.That(Rect(controller, "Backpack Grid").sizeDelta, Is.EqualTo(Vector2.one * 324f));
            Assert.That(Rect(controller, "Freeform Storage").sizeDelta, Is.EqualTo(new Vector2(610f, 208f)));
            Assert.That(controller.GetComponentsInChildren<Transform>(true).Any(child =>
                child.name == "Status Panel" || child.name == "Status" || child.name == "Item Details"), Is.False);
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            foreach (string side in new[] { "Player", "Enemy" })
            {
                RectTransform bag = Rect(controller, $"{side} Battle Backpack");
                RectTransform art = Rect(controller, $"{side} Character Illustration");
                RectTransform health = Rect(controller, $"{side} Health");
                RectTransform name = Rect(controller, $"{side} Name");
                RectTransform frame = Rect(controller, $"{side} Battle Frame");
                var armor = Rect(controller, $"{side} Armor Badge").GetComponent<PrototypeArmorBadge>();
                Assert.That(Mathf.Max(bag.sizeDelta.x, bag.sizeDelta.y), Is.LessThanOrEqualTo(228f));
                Assert.That(art.anchoredPosition.y + art.sizeDelta.y / 2f,
                    Is.LessThan(bag.anchoredPosition.y - bag.sizeDelta.y / 2f));
                Assert.That(health.anchoredPosition.y + health.sizeDelta.y / 2f,
                    Is.LessThan(art.anchoredPosition.y - art.sizeDelta.y / 2f));
                Assert.That(frame.sizeDelta, Is.EqualTo(bag.sizeDelta + Vector2.one * 24f));
                Assert.That(name.anchoredPosition.y + name.sizeDelta.y / 2f,
                    Is.LessThan(frame.anchoredPosition.y - frame.sizeDelta.y / 2f));
                Assert.That(art.anchoredPosition.y + art.sizeDelta.y / 2f,
                    Is.LessThan(name.anchoredPosition.y - name.sizeDelta.y / 2f));
                Assert.That(frame.parent.GetComponent<Image>(), Is.Null, "Only the bag gets a colored frame.");
                Assert.That(name.GetComponent<Text>().text, Is.EqualTo(side == "Player" ? "플레이어" : "고블린"));
                Assert.That(art.GetComponentsInChildren<Image>().All(image => image.sprite == null), Is.True);
                Assert.That(armor.gameObject.activeSelf, Is.False);
            }
            controller.TickCombat(1f);
            Text timer = Rect(controller, "Combat Timer").GetComponent<Text>();
            Assert.That(timer.fontSize, Is.EqualTo(28));
            Assert.That(timer.text, Is.EqualTo("ROUND 1 · 1.0초"));
            Assert.That(Rect(controller, "Battle Result").gameObject.activeSelf, Is.False);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "BattleLayout.png");
        }

        [UnityTest]
        public IEnumerator ArmorBadge_HidesAtZeroAndShowsPositiveValueOnLeftOfHealth()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            PrototypeArmorBadge badge = Rect(controller, "Player Armor Badge").GetComponent<PrototypeArmorBadge>();
            RectTransform health = Rect(controller, "Player Health");
            Assert.That(badge.gameObject.activeSelf, Is.False);
            badge.SetValue(45d); // Presentation preview only; no simulated armor is granted.
            Canvas.ForceUpdateCanvases();
            Assert.That(badge.gameObject.activeSelf, Is.True);
            Assert.That(badge.GetComponentInChildren<Text>().text, Is.EqualTo("45"));
            Assert.That(badge.rectTransform.anchoredPosition.y, Is.EqualTo(health.anchoredPosition.y));
            Assert.That(badge.rectTransform.anchoredPosition.x, Is.LessThan(health.anchoredPosition.x - health.sizeDelta.x / 2f));
            Assert.That(badge.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            Assert.That(badge.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "ArmorBadgePreview.png");
            foreach (double empty in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            {
                badge.SetValue(empty);
                Assert.That(badge.gameObject.activeSelf, Is.False);
                Assert.That(badge.GetComponentsInChildren<Text>(true).Single().text, Is.Empty);
            }
        }

        private static void AssertShape(PrototypeStorageItemView view)
        {
            var cells = view.StoredItem.ItemData.CreateShape().GetCells(view.StoredItem.Rotation);
            Image[] images = view.GetComponentsInChildren<Image>();
            Assert.That(images.Select(image => image.name), Is.EquivalentTo(cells.Select(cell => $"Cell {cell.x},{cell.y}")));
            foreach (Image image in images)
            {
                Assert.That(image.color, Is.EqualTo(view.StoredItem.ItemData.DisplayColor));
                Assert.That(image.rectTransform.sizeDelta, Is.EqualTo(Vector2.one * BackpackPrototypeController.ItemCellSize));
            }
            Assert.That(((RectTransform)view.transform).sizeDelta, Is.EqualTo(new Vector2(
                cells.Max(cell => cell.x) + 1, cells.Max(cell => cell.y) + 1) * BackpackPrototypeController.CellPitch));
        }

        private static void AssertInsideStorage(PrototypeStorageItemView view, RectTransform storage)
        {
            var corners = new Vector3[4];
            ((RectTransform)view.transform).GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 local = storage.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(storage.rect.xMin + 7.9f, storage.rect.xMax - 7.9f));
                Assert.That(local.y, Is.InRange(storage.rect.yMin + 7.9f, storage.rect.yMax - 31.9f));
            }
        }

        private static PointerEventData Pointer() => new PointerEventData(Object.FindFirstObjectByType<EventSystem>())
            { button = PointerEventData.InputButton.Left };
        private static Vector2 Screen(Transform transform) => RectTransformUtility.WorldToScreenPoint(null, transform.position);
        private static RectTransform Rect(BackpackPrototypeController controller, string name)
            => (RectTransform)controller.GetComponentsInChildren<Transform>(true).Single(child => child.name == name);
    }
}
