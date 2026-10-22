using System.Collections;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeCompactTooltipTests
    {
        [UnityTest]
        public IEnumerator EffectOnlyTooltips_CollapseStatsAndWhitespace_AndColorDescriptionNumbersLive()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            var combat = controller.ActiveCombat;
            var views = controller.GetComponentsInChildren<PrototypeBattleItemView>();
            var weapon = views.Single(v => v.Side == CombatSide.Player && v.Snapshot.ItemId == "training_blade");
            weapon.OnPointerEnter(Pointer(weapon.transform));
            float weaponHeight = controller.ItemTooltip.RectTransform.sizeDelta.y;
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<PrototypeStatIcon>(), Has.Length.EqualTo(5));
            var shield = views.Single(v => v.Side == CombatSide.Player && v.Snapshot.ItemId == "wooden_shield");
            shield.OnPointerEnter(Pointer(shield.transform));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<PrototypeStatIcon>(), Is.Empty);
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Tooltip Stats").gameObject.activeSelf, Is.False);
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<Text>(true).Single(t => t.name == "Tooltip Context").gameObject.activeSelf, Is.False);
            Assert.That(controller.ItemTooltip.RectTransform.sizeDelta.y, Is.LessThan(weaponHeight - 80));
            var text = controller.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>();
            Assert.That(text.text, Does.Contain("<color=#59E680>1.82</color>초마다 방어도를 2.5 얻습니다."));
            AssertNumberTint(text, "1.82", improved: true);
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "CompactShieldHaste.png");
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 20); controller.ItemTooltip.Refresh();
            Assert.That(text.text, Does.Contain("<color=#FF5C57>2.22</color>초마다"));
            AssertNumberTint(text, "2.22", improved: false);
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "CompactShieldFreeze.png");
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10); controller.ItemTooltip.Refresh();
            Assert.That(text.text, Does.Contain("2.0초마다").And.Not.Contain("<color="));
            weapon.OnPointerEnter(Pointer(weapon.transform));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<PrototypeStatIcon>(), Has.Length.EqualTo(5));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<Text>().Single(t => t.name == "Tooltip Context").text, Is.EqualTo("전투 중 실제 스탯"));
        }

        [UnityTest]
        public IEnumerator RevisedStatusGlyphs_AreRedFlameTaperedPurpleSkullAndOrangePrecision()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            controller.ActiveCombat.ApplyStatus(CombatSide.Player, StatusEffectKind.Precision, 20);
            controller.ActiveCombat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2);
            controller.ActiveCombat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2);
            controller.TickCombat(0); Canvas.ForceUpdateCanvases();
            foreach (var kind in new[] { StatusEffectKind.Burn, StatusEffectKind.Poison, StatusEffectKind.Precision })
            {
                var icon = controller.GetComponentsInChildren<PrototypeStatusIcon>().Single(i => i.Kind == kind);
                var glyph = icon.GetComponentInChildren<PrototypeStatusGlyph>();
                Color expected = kind == StatusEffectKind.Poison ? new Color(.71f, .53f, .96f)
                    : kind == StatusEffectKind.Burn ? new Color(1, .25f, .20f) : new Color(1, .52f, .18f);
                Assert.That(glyph.color, Is.EqualTo(expected));
                Assert.That(icon.GetComponent<Image>().color.a, Is.Zero);
                Mesh mesh = glyph.canvasRenderer.GetMesh();
                Assert.That(mesh.vertexCount, Is.GreaterThan(8));
                if (kind == StatusEffectKind.Burn)
                {
                    Assert.That(mesh.bounds.size.x, Is.LessThan(mesh.bounds.size.y * .85f));
                    Assert.That(mesh.triangles.Length, Is.EqualTo((mesh.vertexCount - 2) * 3), "All curved boundary vertices must be triangulated, including the notches.");
                    float unit = Mathf.Min(glyph.rectTransform.rect.width, glyph.rectTransform.rect.height) / 20f;
                    Vector2 center = glyph.rectTransform.rect.center;
                    foreach (Vector2 anchor in new[] { new Vector2(0, 9.5f), new Vector2(0, -9.5f), new Vector2(3.32f, 3), new Vector2(-4.98f, .5f), new Vector2(-.83f, .5f) })
                        Assert.That(mesh.vertices.Any(v => Vector2.Distance(new Vector2(v.x - center.x, v.y - center.y) / unit, anchor) < .015f), Is.True,
                            $"Approved preview outline anchor {anchor} is missing. Color-only changes must not replace the curled flame shape.");
                }
                if (kind == StatusEffectKind.Poison)
                {
                    Assert.That(mesh.vertexCount, Is.GreaterThan(200), "Skull has a curved outline and separate facial features, not a droplet polygon.");
                    float unit = Mathf.Min(glyph.rectTransform.rect.width, glyph.rectTransform.rect.height) / 20f;
                    Vector2 center = glyph.rectTransform.rect.center;
                    float lowerWidth = mesh.vertices.Where(v => v.y < center.y - 4 * unit).Max(v => Mathf.Abs(v.x - center.x));
                    float upperWidth = mesh.vertices.Where(v => v.y > center.y).Max(v => Mathf.Abs(v.x - center.x));
                    Assert.That(lowerWidth, Is.LessThan(upperWidth * .95f), "Cheeks and jaw must narrow below the cranium instead of flaring outward.");
                }
            }
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "RevisedStatusGlyphsInBattle.png");
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("BACKPACK_QA_CAPTURE_DIR")))
            {
                var panel = new GameObject("Glyph QA Only", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(controller.BattleController.ScreenRoot, false);
                panel.GetComponent<Image>().color = new Color(.04f, .065f, .11f);
                BackpackPrototypeController.SetRect((RectTransform)panel.transform, Vector2.one * .5f, new Vector2(0, 100), new Vector2(500, 180));
                var kinds = new[] { StatusEffectKind.Burn, StatusEffectKind.Poison, StatusEffectKind.Precision };
                for (int i = 0; i < kinds.Length; i++)
                {
                    var obj = new GameObject("Large QA Glyph", typeof(RectTransform), typeof(PrototypeStatusGlyph)); obj.transform.SetParent(panel.transform, false);
                    var glyph = obj.GetComponent<PrototypeStatusGlyph>(); glyph.Kind = kinds[i]; glyph.color = PrototypeStatusGlyph.ColorFor(kinds[i]);
                    BackpackPrototypeController.SetRect(glyph.rectTransform, Vector2.one * .5f, new Vector2((i - 1) * 150, 12), Vector2.one * 96);
                    Text label = BackpackPrototypeController.CreateText(panel.transform, "QA Label", StatusEffectRules.DisplayName(kinds[i]), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
                    BackpackPrototypeController.SetRect(label.rectTransform, Vector2.one * .5f, new Vector2((i - 1) * 150, -62), new Vector2(140, 26));
                }
                PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "RevisedStatusGlyphsLarge.png");
                Object.Destroy(panel);
            }
        }

        private static void AssertNumberTint(TextMeshProUGUI text, string number, bool improved)
        {
            text.ForceMeshUpdate(); string parsed = text.GetParsedText(); int index = parsed.IndexOf(number, System.StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            for (int i = index; i < index + number.Length; i++)
            {
                Color32 tint = text.textInfo.characterInfo[i].color;
                if (improved) Assert.That(tint.g, Is.GreaterThan(tint.r)); else Assert.That(tint.r, Is.GreaterThan(tint.g));
            }
            Color32 following = text.textInfo.characterInfo[index + number.Length].color;
            Assert.That(following.r, Is.GreaterThan(200), "The following word must stay neutral, not inherit the numeric tint.");
            Assert.That(following.g, Is.GreaterThan(200));
        }
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, target.position) };
    }
}
