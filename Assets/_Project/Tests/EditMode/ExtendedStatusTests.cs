using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class ExtendedStatusTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var obj in _temporary) Object.DestroyImmediate(obj); _temporary.Clear(); }

        [Test]
        public void SeventyAccuracyAndThirtyEvasion_YieldExactly49PercentHitsAnd51Avoidance()
        {
            int hits = 0, misses = 0, dodges = 0, avoided = 0;
            for (int a = 0; a < 100; a++) for (int e = 0; e < 100; e++)
            {
                var outcome = AttackResolver.Resolve(70, 30, 0, new Rolls((a + .5) / 100d, (e + .5) / 100d, .5));
                if (outcome.Hit) hits++;
                if (outcome.Outcome == AttackOutcome.AccuracyMiss) misses++;
                if (outcome.Outcome == AttackOutcome.Evaded) dodges++;
                if (outcome.TriggersAvoidance) avoided++;
            }
            Assert.That((hits, misses, dodges, avoided), Is.EqualTo((4900, 3000, 2100, 5100)));
            Assert.That(AttackResolver.SuccessChancePercent(70, 30), Is.EqualTo(49).Within(1e-9));
        }

        [Test]
        public void FailedAccuracy_SkipsEvasionAndCrit_ButBothFailuresPublishOneDefenderAvoidance()
        {
            var random = new Rolls(.8, .5, .2, .5, .8, .5);
            var combat = Battle(Weapon(70), random);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 30);
            var avoided = new List<CombatItemEvent>(); combat.AttackAvoided += avoided.Add;
            var events = combat.Advance(3);
            Assert.That(events.Select(item => item.AttackOutcome), Is.EqualTo(new[] { AttackOutcome.AccuracyMiss, AttackOutcome.Evaded, AttackOutcome.Hit }));
            Assert.That(avoided.Count, Is.EqualTo(2));
            Assert.That(avoided.All(item => item.TargetSide == CombatSide.Enemy), Is.True);
            Assert.That(combat.GetAvoidanceCount(CombatSide.Enemy), Is.EqualTo(2));
            Assert.That(combat.GetAvoidanceCount(CombatSide.Player), Is.Zero);
            Assert.That(random.Calls, Is.EqualTo(6));
            Assert.That(combat.EnemyHealth, Is.EqualTo(995));
        }

        [Test]
        public void AgilityStacks_AreNotCappedByEffectiveEvasion_OrByInt32()
        {
            var combat = Battle(Weapon());
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Agility, 100);
            Assert.That(combat.GetEvasionPercent(CombatSide.Player), Is.EqualTo(70));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Agility), Is.EqualTo(100));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Agility) * 2, Is.EqualTo(200));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Agility, int.MaxValue);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Agility, int.MaxValue);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Agility), Is.EqualTo(100L + 2L * int.MaxValue));
            Assert.That(combat.GetEvasionPercent(CombatSide.Player), Is.EqualTo(70));
        }

        [Test]
        public void Precision_StoresOvercapChanceButCritsOnlyOnce_AndKeepsOtherStatuses()
        {
            var item = Weapon(90, 10);
            var combat = Battle(item, new Rolls(.1, .99, .1, .99));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Precision, 150);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Precision), Is.EqualTo(150));
            var weapon = combat.PlayerWeapons.Single();
            Assert.That(weapon.Stats.CriticalChancePercent, Is.EqualTo(160));
            Assert.That(ItemTooltipContent.FromCombat(weapon.Snapshot, weapon).CriticalChancePercent, Is.EqualTo(160));
            var attacks = combat.Advance(2);
            Assert.That(attacks.Count, Is.EqualTo(2));
            Assert.That(attacks.All(attack => attack.Critical && attack.Damage == 10), Is.True);
            Assert.That(item.BaseWeaponStats.CriticalChancePercent, Is.EqualTo(10));
        }

        [Test]
        public void FocusAndBlindness_CancelWithoutCapDrift_AndNeverAffectHasteOrCrit()
        {
            var combat = Battle(Weapon(90, 10));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Precision, 5);
            var stats = combat.PlayerWeapons.Single().Stats;
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Focus, 100);
            Assert.That(stats.AccuracyPercent, Is.EqualTo(100));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Blindness, 40);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(60));
            Assert.That(stats.AccuracyPercent, Is.EqualTo(100));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Blindness, 100);
            Assert.That(stats.AccuracyPercent, Is.EqualTo(50));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Blindness, 100);
            Assert.That(stats.AccuracyPercent, Is.EqualTo(30));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Blindness), Is.EqualTo(140));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Focus, 140);
            Assert.That(stats.AccuracyPercent, Is.EqualTo(90));
            Assert.That(stats.CriticalChancePercent, Is.EqualTo(15));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Blindness), Is.Zero);
        }

        [TestCase(CombatDamageKind.StatusEffect)]
        [TestCase(CombatDamageKind.Other)]
        public void NonAttackDamage_CannotBeEvadedOrCrit_AndNeverTriggersAvoidance(CombatDamageKind kind)
        {
            var random = new Rolls(); var combat = Battle(Weapon(), random);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Agility, 100);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Precision, 1000);
            int avoided = 0; combat.AttackAvoided += _ => avoided++;
            var damages = new List<CombatDamageEvent>(); combat.DamageApplied += damages.Add;
            var result = combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 7, kind);
            Assert.That(result.HealthDamage, Is.EqualTo(7));
            Assert.That(combat.PlayerHealth, Is.EqualTo(993));
            Assert.That(avoided, Is.Zero); Assert.That(combat.GetAvoidanceCount(CombatSide.Player), Is.Zero);
            Assert.That(random.Calls, Is.Zero);
            Assert.That(damages.Single().Kind, Is.EqualTo(kind));
            Assert.That(damages.Single().Target, Is.EqualTo(CombatSide.Player));
            Assert.Throws<ArgumentOutOfRangeException>(() => combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 7, CombatDamageKind.AttackItem));
        }

        [Test]
        public void Caps_ApplyToEffectiveStats_WithoutDiscardingOvercapPrecision()
        {
            var baseStats = new WeaponStats(5, 1, 0, 150, 200);
            Assert.That(baseStats.AccuracyPercent, Is.EqualTo(30));
            Assert.That(baseStats.CriticalChancePercent, Is.EqualTo(150));
            var stats = new RuntimeWeaponStats(baseStats, new RuntimeItemCycle(1, () => 0));
            stats.AccuracyPercent = -50; stats.CriticalChancePercent = -1;
            Assert.That(stats.AccuracyPercent, Is.EqualTo(30)); Assert.That(stats.CriticalChancePercent, Is.Zero);
            stats.AccuracyPercent = 300; stats.CriticalChancePercent = 250;
            Assert.That(stats.AccuracyPercent, Is.EqualTo(100)); Assert.That(stats.CriticalChancePercent, Is.EqualTo(250));
        }

        [Test]
        public void NewDefinitionsAndTestEnemy_AreValidAndBattleStartWorksForBothSides()
        {
            foreach (StatusEffectKind kind in Enum.GetValues(typeof(StatusEffectKind)))
            {
                if (kind == StatusEffectKind.Enrage)
                {
                    // System-owned time rule: no editable item status asset or battle-start grant.
                    var timed = new CombatSimulation(Unit(Array.Empty<ItemData>()), Unit(Array.Empty<ItemData>()));
                    Assert.That(timed.GetStatusDefinition(kind).PercentPerStack, Is.EqualTo(10));
                    Assert.That(timed.GetStatusDefinition(kind).IsDebuff, Is.True);
                    continue;
                }
                var definition = AssetDatabase.LoadAssetAtPath<StatusEffectData>($"Assets/_Project/Data/StatusEffects/Status_{kind}.asset");
                Assert.That(definition, Is.Not.Null); Assert.That(definition.PercentPerStack, Is.EqualTo(1));
            }
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_StatusTest.asset");
            var combat = new CombatSimulation(Unit(Array.Empty<ItemData>()), enemy.CreateCombatantSnapshot());
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Precision), Is.EqualTo(20));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Focus), Is.EqualTo(20));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Agility), Is.EqualTo(30));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Blindness), Is.EqualTo(20));
            Assert.That(combat.EnemyWeapons.Single().Stats.AccuracyPercent, Is.EqualTo(90));
        }

        private ItemData Weapon(float accuracy = 100, float crit = 0)
        {
            var item = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(item);
            Set(item, "_weaponStats", new WeaponStats(5, 1, accuracy, crit, 200));
            Set(item, "_activation", new ItemActivationData(1, new ItemActivationEffect(ItemEffectKind.Attack)));
            return item;
        }
        private static CombatSimulation Battle(ItemData weapon, System.Random random = null)
            => new(Unit(new[] { weapon }), Unit(Array.Empty<ItemData>()), random ?? new System.Random(12));
        private static CombatantSnapshot Unit(IEnumerable<ItemData> items) => new("Test", 1000,
            new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot($"test_{i}", item, Vector2Int.zero, ItemRotation.None, item.OccupiedCells))));
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        private sealed class Rolls : System.Random
        {
            private readonly Queue<double> _rolls;
            public int Calls { get; private set; }
            public Rolls(params double[] rolls) { _rolls = new Queue<double>(rolls); }
            public override double NextDouble() { Calls++; return _rolls.Dequeue(); }
        }
    }
}
