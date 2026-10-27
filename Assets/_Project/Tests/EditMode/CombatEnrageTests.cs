using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class CombatEnrageTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var item in _temporary) Object.DestroyImmediate(item); _temporary.Clear(); }

        [TestCase(59.999, 0)]
        [TestCase(60, 1)]
        [TestCase(62.999, 1)]
        [TestCase(63, 2)]
        [TestCase(84, 9)]
        [TestCase(87, 10)]
        [TestCase(89.999, 10)]
        public void AdditiveDamageRule_UsesBattleTimeAndOnlyAffectsPlayer(double seconds, long stacks)
        {
            var combat = Battle(); combat.Advance(seconds);
            Assert.That(combat.EnrageStacks, Is.EqualTo(stacks));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Enrage), Is.EqualTo(stacks));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Enrage), Is.Zero);
            Assert.That(combat.GetIncomingDamageMultiplier(CombatSide.Player), Is.EqualTo(1 + stacks * .1).Within(1e-12));
            var damage = combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 10, CombatDamageKind.Other);
            Assert.That(damage.HealthDamage, Is.EqualTo(10 + stacks).Within(1e-10));
            Assert.That(combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 10, CombatDamageKind.Other).HealthDamage, Is.EqualTo(10));
        }

        [Test]
        public void SixtySecondAttack_IsAmplifiedOnceBeforeArmor_AndStatisticsAndEventsAgree()
        {
            var shield = Item(new ItemActivationData(59, new ItemActivationEffect(ItemEffectKind.GainArmor, 5)));
            var combat = new CombatSimulation(Unit(shield), Unit(Weapon(10, 60)), new FixedRandom());
            var damage = new List<CombatDamageEvent>(); combat.DamageApplied += damage.Add;
            var attack = combat.Advance(60).Single(e => e.Kind == ItemEffectKind.Attack);
            Assert.That(attack.Damage, Is.EqualTo(11).Within(1e-10));
            Assert.That(attack.ArmorAbsorbed, Is.EqualTo(5)); Assert.That(attack.HealthDamage, Is.EqualTo(6).Within(1e-10));
            Assert.That(damage.Single().Resolution.HealthDamage, Is.EqualTo(6).Within(1e-10));
            var statistics = combat.CaptureStatistics();
            Assert.That(statistics.Items.Single(i => i.Side == CombatSide.Enemy).DamageDealt, Is.EqualTo(11).Within(1e-10));
            Assert.That(statistics.Items.Single(i => i.Side == CombatSide.Player).DamageBlocked, Is.EqualTo(5));
        }

        [TestCase(StatusEffectKind.Burn, 1.2)]
        [TestCase(StatusEffectKind.Poison, 1)]
        public void PeriodicDamage_IsAmplifiedWithoutChangingTickClockAndIsCreditedToSource(StatusEffectKind kind, double perStack)
        {
            var combat = Battle(); combat.Advance(59);
            combat.ApplyStatus(CombatSide.Player, kind, 2, CombatSide.Enemy, "dot_source");
            var ticks = new List<PeriodicStatusTickEvent>(); combat.StatusTicked += ticks.Add;
            combat.Advance(1);
            double expected = perStack * 2 * 1.1;
            Assert.That(ticks.Single().Time, Is.EqualTo(60));
            Assert.That(ticks.Single().Damage, Is.EqualTo(expected).Within(1e-5));
            Assert.That(ticks.Single().Resolution.HealthDamage, Is.EqualTo(expected).Within(1e-5));
            Assert.That(combat.CaptureStatistics().Items.Single().DamageDealt, Is.EqualTo(expected).Within(1e-5));
        }

        [Test]
        public void ReflectedDamage_IsAmplified_ButHealingIsNot_AndLethalAttackerPriorityRemains()
        {
            var combat = new CombatSimulation(Unit(Weapon(10, 60)), Unit(), new FixedRandom());
            combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 10, CombatDamageKind.Other);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 2);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 3);
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            combat.Advance(60);
            Assert.That(reactions[0].HealthRestored, Is.EqualTo(2).Within(1e-5));
            Assert.That(reactions[1].Amount, Is.EqualTo(3.3).Within(1e-5));
            Assert.That(reactions[1].Damage.HealthDamage, Is.EqualTo(3.3).Within(1e-5));

            var lethal = new CombatSimulation(Unit(Weapon(1000, 60)), Unit(), new FixedRandom());
            lethal.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 999, CombatDamageKind.Other);
            lethal.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 1);
            lethal.Advance(60);
            Assert.That(lethal.PlayerHealth, Is.Zero); Assert.That(lethal.EnemyHealth, Is.Zero);
            Assert.That(lethal.Result, Is.EqualTo(CombatResult.Victory));
        }

        [Test]
        public void DeadlinePrecedesItemAndStatusEventsAtExactly90_AndDoesNotFabricateDamage()
        {
            var combat = new CombatSimulation(Unit(Weapon(1000, 90)), Unit(Weapon(1000, 90)), new FixedRandom(), maximumDuration: 90);
            combat.Advance(89);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Poison, 1000, CombatSide.Enemy);
            int ticks = 0, damage = 0;
            combat.StatusTicked += _ => ticks++; combat.DamageApplied += _ => damage++;
            Assert.That(combat.Advance(1000), Is.Empty);
            Assert.That(combat.ElapsedSeconds, Is.EqualTo(90)); Assert.That(combat.IsForcedDefeat, Is.True);
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Defeat));
            Assert.That(combat.PlayerHealth, Is.EqualTo(1000)); Assert.That(combat.EnemyHealth, Is.EqualTo(1000));
            Assert.That(ticks, Is.Zero); Assert.That(damage, Is.Zero);
            Assert.That(combat.CaptureStatistics().Items.Sum(i => i.DamageDealt), Is.Zero);
            combat.Advance(1); Assert.That(combat.ElapsedSeconds, Is.EqualTo(90));
        }

        [Test]
        public void FramePartitions_HaveIdenticalDamageAndResultsAcrossEnrageBoundaries()
        {
            var unit = Unit(Weapon(2, 3));
            var whole = new CombatSimulation(unit, unit, new FixedRandom());
            var split = new CombatSimulation(unit, unit, new FixedRandom());
            var a = whole.Advance(120);
            var b = new List<CombatItemEvent>(); for (int i = 0; i < 1200; i++) b.AddRange(split.Advance(.1));
            Assert.That(b.Select(e => (e.Time, e.Side, e.Damage)), Is.EqualTo(a.Select(e => (e.Time, e.Side, e.Damage))));
            Assert.That(split.PlayerHealth, Is.EqualTo(whole.PlayerHealth));
            Assert.That(split.Result, Is.EqualTo(CombatResult.Defeat)); Assert.That(split.ElapsedSeconds, Is.EqualTo(90));
        }

        [Test]
        public void EnrageCannotBeGrantedCleansedOrCarriedIntoNextBattle()
        {
            var combat = Battle(); combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Enrage, 999);
            Assert.That(combat.EnrageStacks, Is.Zero);
            combat.Advance(63); combat.RemoveStatus(CombatSide.Player, StatusEffectKind.Enrage, long.MaxValue);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Enrage, 999);
            Assert.That(combat.EnrageStacks, Is.EqualTo(2));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Enrage), Is.Zero);
            Assert.That(Battle().EnrageStacks, Is.Zero);
        }

        private CombatSimulation Battle() => new(Unit(), Unit());
        private ItemData Weapon(float damage, float interval)
        {
            var item = Item(new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.Attack)));
            Set(item, "_category", ItemCategory.Weapon); Set(item, "_weaponStats", new WeaponStats(damage, interval, 100, 0, 200));
            return item;
        }
        private ItemData Item(ItemActivationData activation)
        {
            var item = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(item); Set(item, "_activation", activation); return item;
        }
        private static CombatantSnapshot Unit(params ItemData[] items) => new("Test", 1000,
            new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot($"item_{i}", item,
                Vector2Int.zero, ItemRotation.None, item.OccupiedCells))), 10);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        private sealed class FixedRandom : System.Random { public override double NextDouble() => .5; }
    }
}
