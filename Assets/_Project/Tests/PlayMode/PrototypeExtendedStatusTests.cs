using System.Collections;
using System.Linq;
using BackpackAutoBattle.Combat;
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
    public sealed class PrototypeExtendedStatusTests
    {
        [UnityTest]
        public IEnumerator PurchasedStatusItems_GrantOnBattleStart_AndShowDistinctColoredGlyphsAndLiveStatColors()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            Buy("precision_charm", 3, 0); Buy("focus_charm", 4, 0); Buy("agility_charm", 5, 0); Buy("blinding_dust", 5, 1);
            Assert.That(controller.Gold, Is.EqualTo(4));
            controller.TryStartBattle(); var combat = controller.ActiveCombat;
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Precision), Is.EqualTo(20));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(20));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Agility), Is.EqualTo(30));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Blindness), Is.EqualTo(20));
            Assert.That(combat.PlayerWeapons.Single().Stats.AccuracyPercent, Is.EqualTo(100));
            Assert.That(combat.PlayerWeapons.Single().Stats.CriticalChancePercent, Is.EqualTo(30));
            Assert.That(combat.EnemyWeapons.Single().Stats.AccuracyPercent, Is.EqualTo(80));
            Canvas.ForceUpdateCanvases();
            foreach (var kind in new[] { StatusEffectKind.Precision, StatusEffectKind.Focus, StatusEffectKind.Blindness, StatusEffectKind.Agility })
            {
                var icon = Icon(kind == StatusEffectKind.Blindness ? CombatSide.Enemy : CombatSide.Player, kind);
                Assert.That(icon.gameObject.activeInHierarchy, Is.True);
                var glyph = icon.GetComponentInChildren<PrototypeStatusGlyph>();
                Color expectedColor = kind switch
                {
                    StatusEffectKind.Precision => new Color(1f, 0.85f, 0.20f),
                    StatusEffectKind.Agility => new Color(0.36f, 0.88f, 0.48f),
                    StatusEffectKind.Blindness => new Color(0.62f, 0.65f, 0.69f),
                    _ => Color.white
                };
                Assert.That(glyph.color, Is.EqualTo(expectedColor)); Assert.That(glyph.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
                Assert.That(icon.GetComponent<Image>().color.a, Is.Zero);
                icon.OnPointerEnter(Pointer(icon.transform));
                Assert.That(controller.ItemTooltip.GetComponentsInChildren<Text>().Single(text => text.name == "Tooltip Name").text,
                    Does.Contain(StatusEffectRules.DisplayName(kind)));
                icon.OnPointerExit(Pointer(icon.transform));
            }
            var sword = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(view => view.Snapshot.ItemId == "training_blade");
            sword.OnPointerEnter(Pointer(sword.transform));
            Assert.That(Value("Accuracy").color.g, Is.GreaterThan(Value("Accuracy").color.r));
            Assert.That(Value("Critical Chance").text, Is.EqualTo("30%"));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Precision, 100);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Blindness, 60);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Agility, 70);
            controller.TickCombat(0); controller.ItemTooltip.Refresh();
            Assert.That(Value("Critical Chance").text, Is.EqualTo("130%"));
            Assert.That(Value("Accuracy").text, Is.EqualTo("50%"));
            Assert.That(Value("Accuracy").color.r, Is.GreaterThan(Value("Accuracy").color.g));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "ExtendedStatusWeaponStats.png");
            var agility = Icon(CombatSide.Player, StatusEffectKind.Agility);
            Assert.That(agility.GetComponentInChildren<Text>().text, Is.EqualTo("100"));
            agility.OnPointerEnter(Pointer(agility.transform));
            string body = controller.ItemTooltip.GetComponentInChildren<TMPro.TextMeshProUGUI>().text;
            Assert.That(body, Is.EqualTo("회피율이 100%p 증가합니다."));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "AgilityOvercapTooltip.png");
            agility.OnPointerExit(Pointer(agility.transform));
            yield return null;

            Text Value(string stat) => controller.ItemTooltip.GetComponentsInChildren<Text>().Single(text => text.name == stat + " Value");
            PrototypeStatusIcon Icon(CombatSide side, StatusEffectKind kind) => controller.GetComponentsInChildren<PrototypeStatusIcon>(true).Single(icon => icon.Side == side && icon.Kind == kind);
            void Buy(string id, int x, int y)
            {
                var slot = controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
                slot.Bind(new ShopOffer(controller.ItemCatalog.Single(item => item.ItemId == id), false));
                var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
                var cell = controller.GetComponentsInChildren<Transform>().Single(t => t.name == $"Grid Cell {x},{y}");
                pointer.position = RectTransformUtility.WorldToScreenPoint(null, cell.position);
                slot.OnDrag(pointer); slot.OnEndDrag(pointer);
            }
        }

        [UnityTest]
        public IEnumerator EvasionReadouts_StayOutsideIllustrations_AndShowLiveEffectiveRatesIncludingZeroAndCap()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            var combat = controller.ActiveCombat;
            Canvas.ForceUpdateCanvases();
            Rect canvasBounds = Bounds((RectTransform)controller.GetComponentInChildren<Canvas>().transform);
            foreach (string side in new[] { "Player", "Enemy" })
            {
                RectTransform row = Find(side + " Evasion");
                Rect rowBounds = Bounds(row), art = Bounds(Find(side + " Character Illustration"));
                if (side == "Player") Assert.That(rowBounds.xMax, Is.LessThan(art.xMin));
                else Assert.That(rowBounds.xMin, Is.GreaterThan(art.xMax));
                Assert.That(rowBounds.center.y, Is.EqualTo(art.center.y).Within(0.1f));
                // The batch runner's Game view may be 4:3; verify the readout stays visible
                // beside its character there too. Reference 16:9 spacing is captured below.
                Assert.That(rowBounds.xMin, Is.GreaterThan(canvasBounds.xMin));
                Assert.That(rowBounds.xMax, Is.LessThan(canvasBounds.xMax));
                Assert.That(rowBounds.yMin, Is.GreaterThan(Bounds(Find(side + " Health")).yMax));
                var glyph = row.GetComponentInChildren<PrototypeStatusGlyph>();
                var value = row.GetComponentInChildren<Text>();
                Assert.That(glyph.Kind, Is.EqualTo(StatusEffectKind.Agility));
                Assert.That(glyph.color, Is.EqualTo(new Color(0.36f, 0.88f, 0.48f)));
                Assert.That(glyph.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
                Assert.That(Bounds(glyph.rectTransform).xMax, Is.LessThan(Bounds(value.rectTransform).xMin));
                Assert.That(row.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);
                Assert.That(value.text, Is.EqualTo("0%"));
            }
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Agility, 100);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 23);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Freeze, 10);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Blindness, 20);
            controller.TickCombat(0);
            Assert.That(Find("Player Evasion").GetComponentInChildren<Text>().text, Is.EqualTo("70%"));
            Assert.That(Find("Enemy Evasion").GetComponentInChildren<Text>().text, Is.EqualTo("23%"));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Agility), Is.EqualTo(100));
            var freeze = controller.GetComponentsInChildren<PrototypeStatusIcon>().Single(icon => icon.Kind == StatusEffectKind.Freeze);
            Assert.That(freeze.GetComponentInChildren<PrototypeStatusGlyph>().color, Is.EqualTo(new Color(0.40f, 0.80f, 1f)));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "StatusEvasionReadouts.png");
            yield return null;

            RectTransform Find(string name) => (RectTransform)controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        }

        [UnityTest]
        public IEnumerator StatusDescriptions_OnlyShowTheirEffect_AndKeepExistingTooltipColors()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            var combat = controller.ActiveCombat;
            foreach (var pair in new[]
            {
                (StatusEffectKind.Haste, "모든 아이템이 10000% 가속합니다."),
                (StatusEffectKind.Freeze, "모든 아이템이 10000% 감속합니다."),
                (StatusEffectKind.Precision, "모든 공격 아이템의 치명타 확률이 10000%p 증가합니다."),
                (StatusEffectKind.Focus, "모든 공격 아이템의 명중률이 10000%p 증가합니다."),
                (StatusEffectKind.Blindness, "모든 공격 아이템의 명중률이 10000%p 감소합니다."),
                (StatusEffectKind.Agility, "회피율이 10000%p 증가합니다.")
            })
            {
                // Opposite effects cancel, so add enough to leave exactly 10000 stacks.
                var opposite = StatusEffectRules.Opposite(pair.Item1);
                long cancelled = opposite.HasValue ? combat.GetStatusStacks(CombatSide.Enemy, opposite.Value) : 0;
                combat.ApplyStatus(CombatSide.Enemy, pair.Item1, 10000 + cancelled);
                var content = StatusTooltipContent.FromCombat(combat, CombatSide.Enemy, pair.Item1);
                Assert.That(content.Description, Is.EqualTo(pair.Item2));
                Assert.That(content.Title, Is.EqualTo($"{StatusEffectRules.DisplayName(pair.Item1)} · 10000중첩"));
                Assert.That(content.Color, Is.EqualTo(StatusEffectRules.IsDebuff(pair.Item1)
                    ? new Color(0.34f, 0.70f, 1f) : new Color(0.36f, 0.88f, 0.66f)));
            }
        }

        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, target.position), button = PointerEventData.InputButton.Left };
    }
}
