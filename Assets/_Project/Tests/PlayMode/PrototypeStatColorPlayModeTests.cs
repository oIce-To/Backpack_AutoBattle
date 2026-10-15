using System.Collections;
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
    public sealed class PrototypeStatColorPlayModeTests
    {
        private static readonly Color Improved = new(0.35f, 0.90f, 0.50f);
        private static readonly Color Reduced = new(1f, 0.36f, 0.34f);
        private static readonly Color Neutral = new(0.91f, 0.94f, 0.98f);
        private static readonly string[] Values = { "Attack Value", "Cooldown Value", "Accuracy Value", "Critical Chance Value", "Critical Damage Value" };

        [UnityTest]
        public IEnumerator TooltipColors_TrackAllFiveStatsAndResetAfterEffectsAndOnShopReturn()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            CombatSimulation combat = controller.ActiveCombat;
            CombatWeapon weapon = combat.PlayerWeapons.Single();
            PrototypeBattleItemView view = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(item => item.Snapshot.ItemId == "training_blade");
            view.OnPointerEnter(Pointer(view.transform));
            Assert.That(Value(controller, "Cooldown Value").text, Is.EqualTo("1.82s"));
            Assert.That(Value(controller, "Cooldown Value").color, Is.EqualTo(Improved));
            foreach (string field in Values.Where(field => field != "Cooldown Value"))
                Assert.That(Value(controller, field).color, Is.EqualTo(Neutral));

            SetStats(weapon, 8f, 95f, 20f, 250f);
            controller.ItemTooltip.Refresh();
            AssertColors(controller, Improved);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "TooltipStatsImproved.png");

            SetStats(weapon, 4f, 80f, 0f, 150f);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 20);
            controller.TickCombat(0f);
            controller.ItemTooltip.Refresh();
            Assert.That(Value(controller, "Cooldown Value").text, Is.EqualTo("2.22s"));
            AssertColors(controller, Reduced);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "TooltipStatsReduced.png");

            SetStats(weapon, 5f, 90f, 10f, 200f);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10);
            controller.TickCombat(0f);
            controller.ItemTooltip.Refresh();
            AssertColors(controller, Neutral);

            // Armor uses the same cooldown comparison, even though it has no attack stats.
            PrototypeBattleItemView shield = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(item => item.Snapshot.ItemId == "wooden_shield");
            shield.OnPointerEnter(Pointer(shield.transform));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10);
            controller.ItemTooltip.Refresh();
            Assert.That(Value(controller, "Cooldown Value").color, Is.EqualTo(Improved));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 20);
            controller.ItemTooltip.Refresh();
            Assert.That(Value(controller, "Cooldown Value").color, Is.EqualTo(Reduced));

            // Reusing the tooltip for an untouched enemy item resets colors rather than leaking the last tint.
            PrototypeBattleItemView enemy = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(item => item.Side == CombatSide.Enemy);
            enemy.OnPointerEnter(Pointer(enemy.transform));
            AssertColors(controller, Neutral);
            combat.EnemyWeapons.Single().Stats.AttackDamage = 7f;
            controller.ItemTooltip.Refresh();
            Assert.That(Value(controller, "Attack Value").color, Is.EqualTo(Improved));

            weapon.Stats.AttackDamage = 100f; weapon.Stats.AccuracyPercent = 100f;
            controller.TickCombat(3f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            controller.AdvanceRound();
            var shopSword = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None).Single(item => item.Data.ItemId == "training_blade");
            shopSword.OnPointerEnter(Pointer(shopSword.transform));
            AssertColors(controller, Neutral);
            Assert.That(Value(controller, "Attack Value").text, Is.EqualTo("5"));
            Assert.That(Value(controller, "Cooldown Value").text, Is.EqualTo("2.0s"));
        }

        private static void SetStats(CombatWeapon weapon, float attack, float accuracy, float chance, float criticalDamage)
        {
            weapon.Stats.AttackDamage = attack; weapon.Stats.AccuracyPercent = accuracy;
            weapon.Stats.CriticalChancePercent = chance; weapon.Stats.CriticalDamagePercent = criticalDamage;
        }
        private static Text Value(BackpackPrototypeController controller, string name)
            => controller.ItemTooltip.GetComponentsInChildren<Text>(true).Single(text => text.name == name);
        private static void AssertColors(BackpackPrototypeController controller, Color color)
        {
            foreach (string name in Values) Assert.That(Value(controller, name).color, Is.EqualTo(color), name);
        }
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, target is RectTransform rect ? rect.TransformPoint(rect.rect.center) : target.position)
        };
    }
}
