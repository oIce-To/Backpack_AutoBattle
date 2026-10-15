using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeStatusPlayModeTests
    {
        [UnityTest]
        public IEnumerator BattleStartCharm_HasteAndFreezeUpdateIconsTooltipAndAllCyclesLive()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            CombatSimulation combat = controller.ActiveCombat;
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            Assert.That(combat.PlayerItems.All(item => System.Math.Abs(item.Cycle.IntervalSeconds - 2d / 1.1d) < 1e-9), Is.True);
            PrototypeStatusIcon haste = Icon(controller, CombatSide.Player, StatusEffectKind.Haste);
            Assert.That(haste.gameObject.activeInHierarchy, Is.True);
            Assert.That(haste.GetComponentInChildren<Text>().text, Is.EqualTo("10"));
            Assert.That(haste.GetComponentInChildren<Text>().alignment, Is.EqualTo(TextAnchor.LowerRight));
            Assert.That(((RectTransform)haste.transform).sizeDelta, Is.EqualTo(Vector2.one * 42f));
            Assert.That(haste.GetComponent<Image>().color.a, Is.Zero);
            Assert.That(haste.GetComponentInChildren<PrototypeStatusGlyph>().color, Is.EqualTo(Color.white));
            Canvas.ForceUpdateCanvases();
            Mesh wing = haste.GetComponentInChildren<PrototypeStatusGlyph>().canvasRenderer.GetMesh();
            Assert.That(wing.vertexCount, Is.EqualTo(16), "The haste glyph is three feather polygons, not arrow strokes.");
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "StatusLayout.png");
            var pointer = Pointer(haste.transform);
            haste.OnPointerEnter(pointer);
            Assert.That(Description(controller), Is.EqualTo("모든 아이템이 10% 가속합니다."));
            Assert.That(controller.ItemTooltip.RectTransform.sizeDelta.x, Is.EqualTo(300f));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "StatusHaste.png");

            controller.TickCombat(0.5f);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 5);
            controller.TickCombat(0f);
            controller.ItemTooltip.Refresh();
            Assert.That(haste.GetComponentInChildren<Text>().text, Is.EqualTo("5"));
            Assert.That(Description(controller), Is.EqualTo("모든 아이템이 5% 가속합니다."));
            Assert.That(combat.PlayerItems.All(item => System.Math.Abs(item.Cycle.ProgressAt(combat.ElapsedSeconds) - 0.275d) < 1e-9), Is.True);

            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 10);
            controller.TickCombat(0f);
            Assert.That(haste.gameObject.activeSelf, Is.False);
            Assert.That(controller.ItemTooltip.IsVisible, Is.False, "Removing the hovered status must dismiss its tooltip.");
            PrototypeStatusIcon freeze = Icon(controller, CombatSide.Player, StatusEffectKind.Freeze);
            Assert.That(freeze.gameObject.activeInHierarchy, Is.True);
            Assert.That(freeze.GetComponentInChildren<Text>().text, Is.EqualTo("5"));
            // A newly enabled masked section joins the canvas/raycast registry on the next UI frame.
            yield return null;
            Canvas.ForceUpdateCanvases();
            pointer.position = Screen(freeze.transform);
            freeze.OnPointerEnter(pointer);
            Assert.That(Description(controller), Is.EqualTo("모든 아이템이 5% 감속합니다."));
            Assert.That(combat.PlayerItems.All(item => System.Math.Abs(item.Cycle.IntervalSeconds - 2d / 0.95d) < 1e-9), Is.True);
            Assert.That(controller.ItemCatalog.Single(item => item.ItemId == "wooden_shield").Activation.CooldownSeconds, Is.EqualTo(2f));
            Canvas.ForceUpdateCanvases();
            Assert.That(freeze.GetComponentInChildren<PrototypeStatusGlyph>().canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "StatusFreeze.png");
            Assert.That(hits.Any(hit => hit.gameObject.GetComponentInParent<PrototypeStatusIcon>() == freeze), Is.True,
                $"Pointer {pointer.position}; icon depth {freeze.GetComponent<Image>().depth}; "
                + $"culled {freeze.GetComponent<Image>().canvasRenderer.cull}; hits {string.Join(", ", hits.Select(hit => hit.gameObject.name))}");

            // Both sides use the same display and calculation path.
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Haste, 7);
            controller.TickCombat(0f);
            PrototypeStatusIcon enemy = Icon(controller, CombatSide.Enemy, StatusEffectKind.Haste);
            Assert.That(enemy.gameObject.activeInHierarchy, Is.True);
            pointer.position = Screen(enemy.transform);
            enemy.OnPointerEnter(pointer);
            freeze.OnPointerExit(pointer); // Stale pointer exits must not dismiss another owner's tooltip.
            Assert.That(controller.ItemTooltip.Owner, Is.SameAs(enemy));
            Assert.That(Description(controller), Is.EqualTo("모든 아이템이 7% 가속합니다."));
            enemy.OnPointerExit(pointer);
            Assert.That(controller.ItemTooltip.IsVisible, Is.False);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "StatusOuterBothSides.png");

            // Switching back to an item restores the full item layout after a compact status tooltip.
            var sword = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(view => view.Snapshot.ItemId == "training_blade");
            sword.OnPointerEnter(Pointer(sword.transform));
            Assert.That(controller.ItemTooltip.RectTransform.sizeDelta.x, Is.EqualTo(390f));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<PrototypeStatIcon>(), Has.Length.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator StatusAreas_UseOuterScreenColumnsWithoutCaptionsOrVisibleBackgrounds()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            Canvas.ForceUpdateCanvases();
            RectTransform canvas = (RectTransform)Object.FindFirstObjectByType<Canvas>().transform;
            Rect canvasBounds = Bounds(canvas);
            foreach (string side in new[] { "Player", "Enemy" })
            {
                RectTransform area = Rect(controller, $"{side} Status Area");
                RectTransform art = Rect(controller, $"{side} Character Illustration");
                RectTransform health = Rect(controller, $"{side} Health");
                RectTransform bag = Rect(controller, $"{side} Battle Backpack");
                Rect areaBounds = Bounds(area);
                Assert.That(areaBounds.xMin, Is.GreaterThan(canvasBounds.xMin));
                Assert.That(areaBounds.xMax, Is.LessThan(canvasBounds.xMax));
                foreach (RectTransform mainUI in new[] { bag, art, health })
                {
                    Rect bounds = Bounds(mainUI);
                    if (side == "Player") Assert.That(areaBounds.xMax, Is.LessThan(bounds.xMin));
                    else Assert.That(areaBounds.xMin, Is.GreaterThan(bounds.xMax));
                }
                Assert.That(areaBounds.yMin, Is.GreaterThan(Bounds(Rect(controller, "Battle Log Panel")).yMax));
                Assert.That(areaBounds.yMax, Is.LessThan(Bounds(Rect(controller, "Battle Result")).yMin));
                Assert.That(area.GetComponentsInChildren<Text>(true).All(text => text.name == "Status Stacks"), Is.True);
                Assert.That(area.GetComponentsInChildren<Image>(true).All(image => image.color.a == 0f), Is.True);
                foreach (ScrollRect scroll in area.GetComponentsInChildren<ScrollRect>(true))
                {
                    Assert.That(scroll.horizontal, Is.False);
                    Assert.That(scroll.vertical, Is.True);
                    Assert.That(scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
                    Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
                }
            }
            var icon = Icon(controller, CombatSide.Player, StatusEffectKind.Haste);
            var pointer = Pointer(icon.transform);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null,
                canvas.TransformPoint(new Vector2(canvas.rect.xMax - 1f, canvas.rect.yMin + 1f)));
            icon.OnPointerEnter(pointer);
            Assert.That(controller.ItemTooltip.RectTransform.anchoredPosition.x + controller.ItemTooltip.RectTransform.sizeDelta.x,
                Is.LessThanOrEqualTo(canvas.rect.width - 7f));
            Assert.That(controller.ItemTooltip.RectTransform.anchoredPosition.y - controller.ItemTooltip.RectTransform.sizeDelta.y,
                Is.GreaterThanOrEqualTo(7f));
        }

        [UnityTest]
        public IEnumerator StoredCharm_DoesNotGrantHaste()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            var charm = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None).Single(view => view.Data.ItemId == "swift_charm");
            var pointer = Pointer(charm.transform);
            charm.OnBeginDrag(pointer);
            pointer.position = Screen(Rect(controller, "Freeform Storage"));
            charm.OnDrag(pointer);
            charm.OnEndDrag(pointer);
            yield return null;
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            controller.TryStartBattle();
            Assert.That(controller.ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.Zero);
            Assert.That(controller.ActiveCombat.PlayerItems.All(item => item.Cycle.IntervalSeconds == 2d), Is.True);
            Assert.That(Icon(controller, CombatSide.Player, StatusEffectKind.Haste).gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator NewRound_ResetsStatusStacksAndHidesOldTooltip()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            var haste = Icon(controller, CombatSide.Player, StatusEffectKind.Haste);
            haste.OnPointerEnter(Pointer(haste.transform));
            var weapon = controller.ActiveCombat.PlayerWeapons.Single();
            weapon.Stats.AttackDamage = 100f; weapon.Stats.AccuracyPercent = 100f;
            controller.TickCombat(2f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(controller.ItemTooltip.IsVisible, Is.False);
            yield return null;
            controller.TryStartBattle();
            yield return null;
            Assert.That(controller.ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            Assert.That(controller.ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.Zero);
            Assert.That(controller.GetComponentsInChildren<PrototypeStatusIcon>(true), Has.Length.EqualTo(4));
        }

        private static PrototypeStatusIcon Icon(BackpackPrototypeController controller, CombatSide side, StatusEffectKind kind)
            => controller.GetComponentsInChildren<PrototypeStatusIcon>(true).Single(icon => icon.Side == side && icon.Kind == kind);
        private static string Description(BackpackPrototypeController controller)
            => controller.ItemTooltip.GetComponentsInChildren<Text>(true).Single(text => text.name == "Tooltip Description").text;
        private static RectTransform Rect(BackpackPrototypeController controller, string name)
            => (RectTransform)controller.GetComponentsInChildren<Transform>(true).Single(child => child.name == name);
        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return UnityEngine.Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        private static Vector2 Screen(Transform target) => RectTransformUtility.WorldToScreenPoint(null,
            target is RectTransform rect ? rect.TransformPoint(rect.rect.center) : target.position);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
            { position = Screen(target), button = PointerEventData.InputButton.Left };
    }
}
