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
        public IEnumerator PurchasedStatusItems_GrantOnBattleStart_AndShowFourDistinctWhiteGlyphsAndLiveStatColors()
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
                Assert.That(glyph.color, Is.EqualTo(Color.white)); Assert.That(glyph.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
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
            Assert.That(body, Does.Contain("100%p")); Assert.That(body, Does.Contain("현재 회피율: 70%"));
            Assert.That(body, Does.Contain("화상·중독·가시"));
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

        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
            { position = RectTransformUtility.WorldToScreenPoint(null, target.position), button = PointerEventData.InputButton.Left };
    }
}
