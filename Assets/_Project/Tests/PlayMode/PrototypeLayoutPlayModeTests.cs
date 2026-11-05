using System.Collections;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
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
                Assert.That(name.GetComponent<Text>().text, Is.EqualTo(side == "Player" ? "알렌" : "고블린"));
                var portrait = art.GetComponent<Image>();
                Assert.That(portrait.preserveAspect, Is.True);
                Assert.That(portrait.sprite.texture.name, Is.EqualTo(side == "Player" ? "allen-standing-approved" : "goblin-standing-approved"));
                Assert.That(portrait.rectTransform.localScale.x, Is.EqualTo(-1));
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
        public IEnumerator BattleDamagePopups_ShowTotalDamageThroughArmorAndUseEffectColors()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            var combat = controller.ActiveCombat;
            var enemy = combat.EnemyWeapons.Single().Stats;
            enemy.AccuracyPercent = 100;
            enemy.CriticalChancePercent = 100;
            enemy.CriticalDamagePercent = 200;
            enemy.AttackDamage = 1;
            var sword = combat.PlayerWeapons.Single().Stats;
            sword.AttackDamage = 0;
            sword.AccuracyPercent = 100;
            sword.CriticalChancePercent = 0;

            controller.TickCombat(2);
            Assert.That(combat.PlayerHealth, Is.EqualTo(combat.PlayerMaxHealth));
            Assert.That(combat.PlayerArmor, Is.GreaterThan(0));
            Assert.That(Popups("Player").Single().Text, Is.EqualTo("-2"), "A fully absorbed critical hit still shows its total damage.");
            Assert.That(Popups("Enemy"), Is.Empty);

            enemy.AttackDamage = 4;
            controller.TickCombat(2);
            var critical = Popups("Player").Single(p => p.Text == "-8");
            Assert.That(combat.PlayerHealth, Is.EqualTo(combat.PlayerMaxHealth - 5),
                "The critical hit shows 8 while armor absorbs 3 and health loses 5.");
            Assert.That(critical.color, Is.EqualTo(new Color(1f, .81f, .28f)));

            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 8);
            sword.AttackDamage = 4;
            controller.TickCombat(1.6f);
            var damage = Popups("Enemy");
            Assert.That(damage.Any(p => p.Text == "-4" && p.color == Color.white), Is.True);
            Assert.That(damage.Any(p => p.Text == "-2.4" && p.color == PrototypeStatusGlyph.ColorFor(StatusEffectKind.Burn)), Is.True);
            Assert.That(damage.Any(p => p.Text == "-2" && p.color == PrototypeStatusGlyph.ColorFor(StatusEffectKind.Poison)), Is.True);
            Assert.That(Popups("Player").Any(p => p.Text == "-8" && p.color == PrototypeStatusGlyph.ColorFor(StatusEffectKind.Thorns)), Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(critical.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            Assert.That(critical.mainTexture.name, Is.EqualTo("BattleDamageGlyphs"));
            foreach (string side in new[] { "Player", "Enemy" })
            {
                var area = Rect(controller, side + " Damage");
                foreach (var popup in Popups(side))
                    Assert.That(area.rect.Contains(popup.rectTransform.anchoredPosition), Is.True);
            }
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "BattlePortraitsAndDamage.png");
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(Popups("Player"), Is.Empty);
            Assert.That(Popups("Enemy"), Is.Empty);

            PrototypeDamageNumber[] Popups(string side) => Rect(controller, side + " Damage").GetComponentsInChildren<PrototypeDamageNumber>();
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
            Assert.That(badge.gameObject.activeSelf, Is.False);
            badge.SetValue(45d); // Presentation preview only; no simulated armor is granted.
            var enemyBadge = Rect(controller, "Enemy Armor Badge").GetComponent<PrototypeArmorBadge>();
            enemyBadge.SetValue(12d);
            Canvas.ForceUpdateCanvases();
            Assert.That(badge.gameObject.activeSelf, Is.True);
            Assert.That(badge.GetComponentInChildren<Text>().text, Is.EqualTo("45"));
            foreach (string side in new[] { "Player", "Enemy" })
            {
                var shield = Rect(controller, $"{side} Armor Badge").GetComponent<PrototypeArmorBadge>();
                RectTransform health = Rect(controller, $"{side} Health");
                Vector3 shieldCenter = shield.rectTransform.TransformPoint(shield.rectTransform.rect.center);
                Vector3 healthLeft = health.TransformPoint(new Vector2(health.rect.xMin, health.rect.center.y));
                Assert.That(Vector3.Distance(shieldCenter, healthLeft), Is.LessThan(0.01f),
                    "The shield center should overlap the HP bar's left endpoint on both sides.");
                Vector3[] vertices = shield.canvasRenderer.GetMesh().vertices;
                Assert.That(vertices.Length, Is.GreaterThan(0));
                float peakY = vertices.Max(vertex => vertex.y);
                Assert.That(vertices.Where(vertex => Mathf.Abs(vertex.y - peakY) < 0.01f)
                    .All(vertex => Mathf.Abs(vertex.x) < 0.01f), Is.True,
                    "The shield should have a single central peak, not raised horns at either side.");
            }
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
