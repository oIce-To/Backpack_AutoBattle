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
    public sealed class PrototypeShieldPlayModeTests
    {
        [UnityTest]
        public IEnumerator Shield_ActivatesAbsorbsDamageAndUsesCommonSpeedAndTooltip()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            ItemData definition = controller.ItemCatalog.Single(item => item.ItemId == "wooden_shield");
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>());
            BackpackItemView preparationShield = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.Data == definition);
            pointer.position = Screen(preparationShield.transform);
            preparationShield.OnPointerEnter(pointer);
            Assert.That(Text(controller.ItemTooltip, "Cooldown Value").text, Is.EqualTo("2.0s"));
            Assert.That(Text(controller.ItemTooltip, "Tooltip Description").text, Does.Contain("방어도를 1"));
            Assert.That(controller.ItemTooltip.GetComponentsInChildren<PrototypeStatIcon>(), Has.Length.EqualTo(1));
            preparationShield.OnPointerExit(pointer);

            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            CombatSimulation combat = controller.ActiveCombat;
            // This fixture checks the unmodified 2s shield. Status-specific acceleration has its own fixture.
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste));
            combat.PlayerWeapons.Single().Stats.AccuracyPercent = 100f;
            combat.PlayerWeapons.Single().Stats.CriticalChancePercent = 0f;
            // Slow the opponent only for this UI test, so armor remains visible between attacks.
            combat.SetActivationSpeedModifier(CombatSide.Enemy, "qa_slow", -50f);
            CombatItem shield = combat.PlayerItems.Single(item => item.Snapshot.ItemId == "wooden_shield");
            PrototypeBattleItemView view = controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .Single(item => item.Snapshot.ItemId == "wooden_shield");
            PrototypeArmorBadge badge = controller.GetComponentsInChildren<PrototypeArmorBadge>(true)
                .Single(item => item.name == "Player Armor Badge");
            Assert.That(view.GetComponentsInChildren<Transform>().Any(child => child.name == "Cycle Color"), Is.True);
            controller.TickCombat(1f);
            Assert.That(view.CycleProgress, Is.EqualTo(0.5f));
            controller.TickCombat(1f);
            Assert.That(view.CycleProgress, Is.Zero);
            Assert.That(combat.PlayerArmor, Is.EqualTo(1d));
            Assert.That(badge.gameObject.activeSelf, Is.True);
            Assert.That(badge.GetComponentInChildren<Text>().text, Is.EqualTo("1"));
            pointer.position = Screen(view.transform);
            view.OnPointerEnter(pointer);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "ShieldDefaultCycle.png");
            view.OnPointerExit(pointer);
            controller.TickCombat(2f);
            Assert.That(combat.PlayerArmor, Is.Zero);
            Assert.That(combat.PlayerHealth, Is.EqualTo(37d));
            Assert.That(badge.gameObject.activeSelf, Is.False);
            Assert.That(controller.GetComponentsInChildren<Text>().Single(text => text.name == "Combat Log").text,
                Does.Contain("방어도 2 흡수"));

            combat.SetActivationSpeedModifier(CombatSide.Player, "qa_global", 100f);
            combat.SetActivationSpeedModifier(CombatSide.Player, "qa_melee", 100f,
                new ItemTargetFilter(ItemCategory.Weapon, ItemTags.Melee));
            Assert.That(shield.Cycle.IntervalSeconds, Is.EqualTo(1d));
            Assert.That(combat.PlayerWeapons.Single().Cycle.IntervalSeconds, Is.EqualTo(2d / 3d).Within(1e-9));
            controller.TickCombat(0.5f);
            Assert.That(view.CycleProgress, Is.EqualTo(0.5f));
            pointer.position = Screen(view.transform);
            view.OnPointerEnter(pointer);
            controller.ItemTooltip.Refresh();
            Assert.That(Text(controller.ItemTooltip, "Cooldown Value").text, Is.EqualTo("1.0s"));
            Assert.That(Text(controller.ItemTooltip, "Tooltip Description").text, Does.Contain("1.0초마다"));
            Assert.That(definition.Activation.CooldownSeconds, Is.EqualTo(2f));
            controller.TickCombat(0.5f);
            Assert.That(combat.PlayerArmor, Is.EqualTo(1d));
            Assert.That(badge.gameObject.activeSelf, Is.True);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "ShieldCombat.png");
        }

        private static Text Text(PrototypeItemTooltip tooltip, string name)
            => tooltip.GetComponentsInChildren<Text>(true).Single(text => text.name == name);
        private static Vector2 Screen(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);
    }
}
