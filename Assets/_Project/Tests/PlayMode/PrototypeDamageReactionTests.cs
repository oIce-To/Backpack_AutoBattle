using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
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
    public sealed class PrototypeDamageReactionTests
    {
        [UnityTest]
        public IEnumerator PurchasedReactionItems_GrantStacks_ShowFinalTooltipAndChronologicalCombatFeedback()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            var enemy = Object.Instantiate(controller.CurrentEnemyEncounter);
            try
            {
                Set(enemy, "_maximumHealth", 120);
                var items = enemy.Items.ToList();
                items.Add(new EnemyItemPlacement("thorns", Item(controller, "thorn_armor"), new Vector2Int(3, 0)));
                items.Add(new EnemyItemPlacement("lifesteal", Item(controller, "vampiric_charm"), new Vector2Int(5, 0)));
                Set(enemy, "_items", items); Set(controller, "_debugEnemyOverride", enemy);
                Assert.That(controller.RefreshEncounterPreview(), Is.True);
                Buy(controller, "thorn_armor", Find(controller, "Grid Cell 3,0"));
                Buy(controller, "vampiric_charm", Find(controller, "Grid Cell 5,0"));
                Assert.That(controller.Gold, Is.EqualTo(10));
                var armorView = controller.GetComponentsInChildren<BackpackItemView>().Single(v => v.Data.ItemId == "thorn_armor");
                armorView.OnPointerEnter(Pointer(armorView.transform));
                Assert.That(Description(controller), Does.Contain("가시 3중첩").And.Not.Contain("공격력"));
                Assert.That(controller.TryStartBattle(), Is.True);
                var combat = controller.ActiveCombat;
                combat.PlayerWeapons.Single().Stats.AccuracyPercent = 100;
                var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
                foreach (CombatSide side in System.Enum.GetValues(typeof(CombatSide)))
                {
                    Assert.That(combat.GetStatusStacks(side, StatusEffectKind.Thorns), Is.EqualTo(3));
                    Assert.That(combat.GetStatusStacks(side, StatusEffectKind.Lifesteal), Is.EqualTo(3));
                }
                controller.TickCombat(2.1f); yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(reactions.Count(r => r.Kind == CombatReactionKind.ReflectDamage), Is.EqualTo(2));
                Assert.That(reactions.Any(r => r.HealthRestored > 0), Is.True);
                foreach (var kind in new[] { StatusEffectKind.Thorns, StatusEffectKind.Lifesteal })
                {
                    var icon = controller.GetComponentsInChildren<PrototypeStatusIcon>().Single(i => i.Side == CombatSide.Player && i.Kind == kind);
                    icon.OnPointerEnter(Pointer(icon.transform));
                    Assert.That(Description(controller), Does.Contain(kind == StatusEffectKind.Thorns ? "3 피해" : "3 회복")
                        .And.Not.Contain("%").And.Not.Contain("공격력"));
                    PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), kind + "ReactionTooltip.png");
                    icon.OnPointerExit(Pointer(icon.transform));
                }
                string log = Find(controller, "Combat Log").GetComponent<Text>().text;
                Assert.That(log, Does.Contain("흡혈").And.Contain("가시"));
                Assert.That(log.Split('\n').Select(l => float.Parse(l.Split(' ')[0].TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture)), Is.Ordered);
                PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "DamageReactionsInBattle.png");
                // A completed battle creates fresh, start-granted stacks next round rather than carrying runtime additions.
                combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Thorns, 100);
                combat.PlayerWeapons.Single().Stats.AttackDamage = 1000;
                controller.TickCombat(2); Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
                Assert.That(controller.AdvanceRound(), Is.True); yield return null;
                controller.TryStartBattle();
                Assert.That(controller.ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Thorns), Is.EqualTo(3));
            }
            finally { Object.Destroy(enemy); }
        }

        [UnityTest]
        public IEnumerator StoredReactionItem_DoesNotGrantCombatStatus()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            Buy(controller, "vampiric_charm", Find(controller, "Freeform Storage")); yield return null;
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            controller.TryStartBattle();
            Assert.That(controller.ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Lifesteal), Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReactionGlyphs_AreGraySpikedBallRedOpenFangsAndBlackBlindness_WithTransparentBackgrounds()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            foreach (var kind in new[] { StatusEffectKind.Thorns, StatusEffectKind.Lifesteal, StatusEffectKind.Blindness }) controller.ActiveCombat.ApplyStatus(CombatSide.Player, kind, 3);
            controller.TickCombat(0); Canvas.ForceUpdateCanvases();
            foreach (var icon in controller.GetComponentsInChildren<PrototypeStatusIcon>().Where(i => i.Kind is StatusEffectKind.Thorns or StatusEffectKind.Lifesteal or StatusEffectKind.Blindness))
            {
                var glyph = icon.GetComponentInChildren<PrototypeStatusGlyph>();
                Assert.That(glyph.color, Is.EqualTo(icon.Kind switch
                {
                    StatusEffectKind.Thorns => new Color(.68f, .70f, .73f),
                    StatusEffectKind.Lifesteal => new Color(.94f, .17f, .23f),
                    _ => Color.black
                }));
                Assert.That(icon.GetComponent<Image>().color.a, Is.Zero);
                Mesh mesh = glyph.canvasRenderer.GetMesh();
                Assert.That(mesh.vertexCount, Is.GreaterThan(30));
                Assert.That(mesh.bounds.size.y, Is.LessThanOrEqualTo(glyph.rectTransform.rect.height));
                Assert.That(mesh.bounds.size.x, Is.LessThanOrEqualTo(glyph.rectTransform.rect.width));
                float scale = Mathf.Min(glyph.rectTransform.rect.width, glyph.rectTransform.rect.height) / 20f;
                Vector2 center = glyph.rectTransform.rect.center;
                if (icon.Kind == StatusEffectKind.Lifesteal)
                {
                    Assert.That(Contains(mesh, center + new Vector2(0, 4) * scale), Is.True, "Upper incisors must remain visible between the long canines.");
                    Assert.That(Contains(mesh, center + new Vector2(0, -5.5f) * scale), Is.True, "Lower teeth must be present, not just two upper fangs.");
                    Assert.That(Contains(mesh, center), Is.False, "The open mouth must remain transparent.");
                }
                else if (icon.Kind == StatusEffectKind.Thorns)
                {
                    Assert.That(mesh.vertices.Count(v => ((Vector2)v - center).magnitude > 8.8f * scale), Is.EqualTo(7), "Seven sharp outer spikes define the ball silhouette.");
                    Assert.That(Contains(mesh, center), Is.False);
                }
            }
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "RevisedReactionGlyphsInBattle.png");
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("BACKPACK_QA_CAPTURE_DIR")))
            {
                var panel = new GameObject("Reaction Glyph QA Only", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(controller.BattleController.ScreenRoot, false);
                panel.GetComponent<Image>().color = new Color(.04f, .065f, .11f);
                BackpackPrototypeController.SetRect((RectTransform)panel.transform, Vector2.one * .5f, new Vector2(0, 100), new Vector2(520, 180));
                var kinds = new[] { StatusEffectKind.Thorns, StatusEffectKind.Lifesteal, StatusEffectKind.Blindness };
                for (int i = 0; i < kinds.Length; i++)
                {
                    var obj = new GameObject("Large Reaction QA Glyph", typeof(RectTransform), typeof(PrototypeStatusGlyph)); obj.transform.SetParent(panel.transform, false);
                    var glyph = obj.GetComponent<PrototypeStatusGlyph>(); glyph.Kind = kinds[i]; glyph.color = PrototypeStatusGlyph.ColorFor(kinds[i]);
                    BackpackPrototypeController.SetRect(glyph.rectTransform, Vector2.one * .5f, new Vector2((i - 1) * 160, 12), Vector2.one * 96);
                    var label = BackpackPrototypeController.CreateText(panel.transform, "QA Label", StatusEffectRules.DisplayName(kinds[i]), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
                    BackpackPrototypeController.SetRect(label.rectTransform, Vector2.one * .5f, new Vector2((i - 1) * 160, -62), new Vector2(140, 26));
                }
                PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "RevisedReactionGlyphsLarge.png");
                Object.Destroy(panel);
            }
        }

        private static bool Contains(Mesh mesh, Vector2 point)
        {
            float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
            Vector3[] vertices = mesh.vertices; int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                float x = Cross(b - a, point - a), y = Cross(c - b, point - b), z = Cross(a - c, point - c);
                if ((x >= 0 && y >= 0 && z >= 0) || (x <= 0 && y <= 0 && z <= 0)) return true;
            }
            return false;
        }
        private static ItemData Item(BackpackPrototypeController c, string id) => c.ItemCatalog.Single(i => i.ItemId == id);
        private static void Buy(BackpackPrototypeController c, string id, Transform destination)
        {
            var slot = c.GetComponentsInChildren<PrototypeShopSlotView>().First(); slot.Bind(new ShopOffer(Item(c, id), false));
            var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, destination.position); slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private static Transform Find(BackpackPrototypeController c, string name) => c.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, target.position) };
        private static string Description(BackpackPrototypeController c) => c.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>().text;
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
