using System.Collections;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypePlacementPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShopDragAndHover_DiamondsUseEligibilityAndTooltipContainsARealIcon()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            ItemData stoneData = controller.ItemCatalog.Single(item => item.ItemId == "whetstone");
            slot.Bind(new ShopOffer(stoneData, false));
            slot.OnPointerEnter(Pointer(slot.transform));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<PrototypePlacementDiamond>(), Is.Empty,
                "Tooltip icons must be text elements, not separately positioned graphics.");
            Canvas.ForceUpdateCanvases();
            TextMeshProUGUI effectText = controller.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>();
            effectText.ForceMeshUpdate();
            Assert.That(effectText.text, Does.StartWith(ItemTooltipContent.PlacementIconTag + "에 배치되어 있는 근접 무기의"));
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "WhetstoneTooltip.png");
            Assert.That(SpriteCount(effectText), Is.EqualTo(1));
            Assert.That(effectText.textInfo.characterInfo[0].elementType, Is.EqualTo(TMP_TextElementType.Sprite));
            Assert.That(effectText.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal));
            Assert.That(effectText.textInfo.lineCount, Is.GreaterThanOrEqualTo(2));
            TMP_CharacterInfo wrappedStart = effectText.textInfo.characterInfo[effectText.textInfo.lineInfo[1].firstCharacterIndex];
            Assert.That(wrappedStart.origin, Is.EqualTo(0f).Within(0.1f), "Wrapped lines start at the normal left margin, not after the icon.");
            Assert.That(effectText.isTextOverflowing, Is.False);
            Assert.That(effectText.textInfo.characterInfo.Take(effectText.textInfo.characterCount)
                .Where(character => character.character >= 0xAC00 && character.character <= 0xD7A3)
                .All(character => character.textElement.unicode == character.character), Is.True, "Korean must not be substituted with missing glyphs.");
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "WhetstoneTooltip.png");
            var pointer = Pointer(slot.transform);
            slot.OnBeginDrag(pointer);
            pointer.position = Screen(Cell(controller, 1, 2));
            slot.OnDrag(pointer);
            Assert.That(controller.ItemTooltip.IsVisible, Is.False);
            AssertMarker(controller, new(0, 2), true);
            AssertMarker(controller, new(2, 2), false);
            Assert.That(Markers(controller).All(marker => !marker.raycastTarget), Is.True);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "WhetstoneDrag.png");
            slot.OnEndDrag(pointer);
            yield return null;
            Assert.That(controller.Gold, Is.EqualTo(18));
            Assert.That(slot.CanPurchase, Is.False);
            Assert.That(Markers(controller), Is.Empty);
            BackpackItemView stone = Stone(controller);
            stone.OnPointerEnter(Pointer(stone.transform));
            AssertMarker(controller, new(0, 2), true);
            AssertMarker(controller, new(2, 2), false);
            Canvas.ForceUpdateCanvases();
            Assert.That(Markers(controller).Single(marker => marker.IsFilled).canvasRenderer.GetMesh().vertexCount, Is.EqualTo(4));
            Assert.That(Markers(controller).Single(marker => !marker.IsFilled).canvasRenderer.GetMesh().vertexCount, Is.EqualTo(8));

            // A ranged weapon occupies the right effect cell, but must NOT turn its diamond solid.
            stone.OnPointerExit(Pointer(stone.transform));
            Buy(controller, "throwing_axes", 2, 2);
            yield return null;
            stone.OnPointerEnter(Pointer(stone.transform));
            AssertMarker(controller, new(0, 2), true);
            AssertMarker(controller, new(2, 2), false);
            controller.ItemTooltip.HideAll(); // Capture the range unobstructed; ordinary gameplay still shows the tooltip.
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "WhetstoneRangedRejected.png");

            // Rotation moves the effect cells even though a 1x1 footprint looks unchanged.
            controller.RotateItem(stone);
            AssertMarker(controller, new(1, 1), false); // Wooden shield: also ineligible.
            AssertMarker(controller, new(1, 3), false);
            Assert.That(Markers(controller).Select(marker => marker.Cell), Is.EquivalentTo(new[] { new Vector2Int(1, 1), new Vector2Int(1, 3) }));
            stone.OnPointerExit(Pointer(stone.transform));
            Assert.That(Markers(controller), Is.Empty);
        }

        [UnityTest]
        public IEnumerator InvalidDragRotationAndCancel_DoNotClaimAnActiveEffectOrLeaveGhostMarkers()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Buy(controller, "whetstone", 1, 2);
            yield return null;
            BackpackItemView stone = Stone(controller);
            var pointer = Pointer(stone.transform);
            stone.OnBeginDrag(pointer);
            pointer.position = Screen(Cell(controller, 1, 1)); // Invalid: overlaps the shield.
            stone.OnDrag(pointer);
            AssertMarker(controller, new(0, 1), false); // Melee target exists, but the source can't be placed.
            controller.RotateItem(stone);
            Assert.That(Markers(controller).Select(marker => marker.Cell), Is.EquivalentTo(new[] { new Vector2Int(1, 0), new Vector2Int(1, 2) }));
            stone.OnEndDrag(pointer);
            Assert.That(stone.Origin, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(stone.Rotation, Is.EqualTo(ItemRotation.None));
            Assert.That(Markers(controller), Is.Empty);
            stone.OnPointerEnter(Pointer(stone.transform));
            AssertMarker(controller, new(0, 2), true);
            stone.OnPointerExit(pointer);

            stone.OnBeginDrag(Pointer(stone.transform));
            pointer.position = Screen(Cell(controller, 0, 4));
            stone.OnDrag(pointer);
            Assert.That(Markers(controller), Has.Length.EqualTo(1), "Off-grid effect cells are clipped, not wrapped around.");
            AssertMarker(controller, new(1, 4), false);
            pointer.position = Screen(Rect(controller, "Freeform Storage"));
            stone.OnDrag(pointer);
            Assert.That(Markers(controller), Is.Empty);
            stone.OnEndDrag(pointer);
            yield return null;
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(controller.CaptureCombatSnapshot().Items.All(item => item.ItemId != "whetstone"), Is.True);
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            Assert.That(controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(5f));
        }

        [UnityTest]
        public IEnumerator PlacementBonus_AffectsActualBattleAndHoverButDisappearsAfterMovingSourceAway()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Buy(controller, "whetstone", 1, 2);
            yield return null;
            BackpackItemView stone = Stone(controller);
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            CombatWeapon weapon = controller.ActiveCombat.PlayerWeapons.Single();
            Assert.That(weapon.Stats.AttackDamage, Is.EqualTo(6f));
            Assert.That(weapon.BaseStats.AttackDamage, Is.EqualTo(5f));
            var battleStone = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(view => view.Snapshot.ItemId == "whetstone");
            battleStone.OnPointerEnter(Pointer(battleStone.transform));
            Transform battleBag = Rect(controller, "Player Battle Backpack");
            var cells = battleBag.GetComponentsInChildren<PrototypePlacementDiamond>();
            Assert.That(cells.Single(cell => cell.Cell == new Vector2Int(0, 2)).IsFilled, Is.True);
            Assert.That(cells.Single(cell => cell.Cell == new Vector2Int(2, 2)).IsFilled, Is.False);
            controller.ItemTooltip.HideAll();
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "WhetstoneBattleRange.png");
            battleStone.OnPointerExit(Pointer(battleStone.transform));
            var blade = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(view => view.Snapshot.ItemId == "training_blade");
            blade.OnPointerEnter(Pointer(blade.transform));
            Text damage = controller.ItemTooltip.GetComponentsInChildren<Text>().Single(text => text.name == "Attack Value");
            Assert.That(damage.text, Is.EqualTo("6"));
            Assert.That(damage.color.g, Is.GreaterThan(damage.color.r));
            Assert.That(battleBag.GetComponentsInChildren<PrototypePlacementDiamond>(), Is.Empty);
            controller.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>().ForceMeshUpdate();
            Assert.That(SpriteCount(controller.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>()), Is.Zero,
                "A weapon tooltip must not retain the previous item's inline diamond.");
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "WhetstoneBattleStats.png");
            weapon.Stats.AccuracyPercent = 100f; weapon.Stats.CriticalChancePercent = 0f;
            controller.TickCombat(2f);
            Assert.That(controller.ActiveCombat.EnemyHealth, Is.EqualTo(24d));
            weapon.Stats.AttackDamage = 100f;
            controller.TickCombat(2f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            controller.AdvanceRound();
            var pointer = Pointer(stone.transform);
            stone.OnBeginDrag(pointer);
            pointer.position = Screen(Cell(controller, 3, 0));
            stone.OnDrag(pointer); stone.OnEndDrag(pointer);
            stone.OnPointerEnter(Pointer(stone.transform));
            AssertMarker(controller, new(2, 0), false); // Armor, not a melee weapon.
            AssertMarker(controller, new(4, 0), false);
            controller.TryStartBattle();
            Assert.That(controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(5f));
            Assert.That(stone.Data.PlacementEffects.Single().AttackDamageBonus, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator StoragePurchasePreviewAndReturn_PreserveFreePositionAndRangeRotation()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(controller.ItemCatalog.Single(item => item.ItemId == "whetstone"), false));
            var pointer = Pointer(slot.transform);
            slot.OnBeginDrag(pointer);
            pointer.position = Screen(Rect(controller, "Freeform Storage")) + new Vector2(37f, -17f);
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
            yield return null;
            var stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            Vector2 original = stored.StoredItem.StoragePosition;
            pointer.position = Screen(stored.transform);
            stored.OnBeginDrag(pointer);
            pointer.position = Screen(Cell(controller, 1, 2));
            stored.OnDrag(pointer);
            AssertMarker(controller, new(0, 2), true);
            AssertMarker(controller, new(2, 2), false);
            // Reject a rotated overlapping drop and verify storage is unchanged.
            BackpackItemView proxy = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None).Single(view => view.Data.ItemId == "whetstone");
            controller.RotateItem(proxy);
            pointer.position = Screen(Cell(controller, 1, 0));
            stored.OnDrag(pointer); stored.OnEndDrag(pointer);
            yield return null;
            Assert.That(stored.StoredItem.StoragePosition, Is.EqualTo(original));
            Assert.That(stored.StoredItem.Rotation, Is.EqualTo(ItemRotation.None));
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(Markers(controller), Is.Empty);
        }

        [UnityTest]
        public IEnumerator InlineDescription_SupportsMultipleEffectsAndIconsInTheMiddleWithoutCustomLayout()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            ItemData temporary = Object.Instantiate(controller.ItemCatalog.Single(item => item.ItemId == "whetstone"));
            try
            {
                Set(temporary, "_placementEffects", new System.Collections.Generic.List<ItemPlacementEffect>
                {
                    new(new[] { Vector2Int.left }, 1f),
                    new(new[] { new Vector2Int(2, -1) }, -2f, new(ItemCategory.Weapon, ItemTags.Ranged))
                });
                Set(temporary, "_description", "원하는 문장 중간에도 " + ItemTooltipContent.PlacementIconTag + " 표시를 넣습니다.");
                controller.ItemTooltip.Show(controller, () => ItemTooltipContent.FromItem(temporary), Screen(controller.transform), null);
                Canvas.ForceUpdateCanvases();
                TextMeshProUGUI text = controller.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>();
                text.ForceMeshUpdate();
                Assert.That(controller.ItemTooltip.GetComponentsInChildren<TextMeshProUGUI>(), Has.Length.EqualTo(1));
                PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "InlineMultipleEffectsTooltip.png");
                Assert.That(SpriteCount(text), Is.EqualTo(3));
                Assert.That(text.text, Does.Contain("원거리 무기의 공격력을 2 감소"));
                Assert.That(text.isTextOverflowing, Is.False);
                foreach (TMP_CharacterInfo character in text.textInfo.characterInfo.Take(text.textInfo.characterCount)
                    .Where(character => character.elementType == TMP_TextElementType.Sprite))
                {
                    Assert.That(character.textElement.textAsset, Is.SameAs(text.spriteAsset));
                    Assert.That(character.isVisible, Is.True);
                }
                Assert.That(text.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
                PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "InlineMultipleEffectsTooltip.png");

                controller.BattleController.AutoTick = false;
                controller.TryStartBattle();
                controller.ItemTooltip.ShowStatus(controller,
                    () => StatusTooltipContent.FromCombat(controller.ActiveCombat, CombatSide.Player, StatusEffectKind.Haste),
                    Screen(controller.transform), null);
                Canvas.ForceUpdateCanvases();
                text.ForceMeshUpdate();
                Assert.That(SpriteCount(text), Is.Zero, "Switching to a status tooltip must clear the previous inline sprites.");
                Assert.That(text.isTextOverflowing, Is.False);
                Assert.That(text.text, Is.EqualTo("모든 아이템이 10% 가속합니다."));
            }
            finally
            {
                controller.ItemTooltip.HideAll();
                Object.Destroy(temporary);
            }
        }

        // This bundled TMP version resets textInfo.spriteCount from an unmaintained field after layout.
        // Inspect the generated elements instead: these are the actual sprite glyphs rendered in the sentence.
        private static int SpriteCount(TextMeshProUGUI text)
            => text.textInfo.characterInfo.Take(text.textInfo.characterCount).Count(character => character.elementType == TMP_TextElementType.Sprite);

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);

        private static void Buy(BackpackPrototypeController controller, string id, int x, int y)
        {
            var slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(controller.ItemCatalog.Single(item => item.ItemId == id), false));
            var pointer = Pointer(slot.transform);
            slot.OnBeginDrag(pointer); pointer.position = Screen(Cell(controller, x, y));
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private static BackpackItemView Stone(BackpackPrototypeController controller)
            => controller.GetComponentsInChildren<BackpackItemView>().Single(view => view.Data.ItemId == "whetstone");
        private static PrototypePlacementDiamond[] Markers(BackpackPrototypeController controller)
            => Rect(controller, "Backpack Grid").GetComponentsInChildren<PrototypePlacementDiamond>();
        private static void AssertMarker(BackpackPrototypeController controller, Vector2Int cell, bool filled)
            => Assert.That(Markers(controller).Single(marker => marker.Cell == cell).IsFilled, Is.EqualTo(filled), $"Cell {cell}");
        private static RectTransform Rect(BackpackPrototypeController controller, string name)
            => (RectTransform)controller.GetComponentsInChildren<Transform>(true).Single(item => item.name == name);
        private static Transform Cell(BackpackPrototypeController controller, int x, int y) => Rect(controller, $"Grid Cell {x},{y}");
        private static Vector2 Screen(Transform transform) => RectTransformUtility.WorldToScreenPoint(null,
            transform is RectTransform rect ? rect.TransformPoint(rect.rect.center) : transform.position);
        private static PointerEventData Pointer(Transform transform) => new(EventSystem.current)
            { position = Screen(transform), button = PointerEventData.InputButton.Left };
    }
}
