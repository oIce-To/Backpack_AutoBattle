using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class CombatSimulationTests
    {
        private ItemData _weapon;
        private ItemData _enemyWeapon;

        [SetUp]
        public void SetUp()
        {
            _weapon = ScriptableObject.CreateInstance<ItemData>();
            _enemyWeapon = ScriptableObject.CreateInstance<ItemData>();
            SetField("_itemId", "test_weapon");
            SetField("_displayName", "Test weapon");
            SetField("_category", ItemCategory.Weapon);
            SetField("_weaponStats", new WeaponStats(5f, 2f, 100f, 0f, 200f));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_weapon);
            UnityEngine.Object.DestroyImmediate(_enemyWeapon);
        }

        [TestCase(100f, 0f, true, false, 5d)]
        [TestCase(100f, 100f, true, true, 10d)]
        [TestCase(30f, 100f, false, false, 0d)]
        public void Attack_ResolvesAccuracyAndCriticalDamage(float accuracy, float criticalChance,
            bool expectedHit, bool expectedCritical, double expectedDamage)
        {
            SetField("_weaponStats", new WeaponStats(5f, 2f, accuracy, criticalChance, 200f));
            CombatSimulation combat = CreateCombat(new SequenceRandom(.95d, .5d));
            Assert.That(combat.Advance(1d), Is.Empty);
            CombatItemEvent attack = combat.Advance(1d).Single();
            Assert.That(attack.Hit, Is.EqualTo(expectedHit));
            Assert.That(attack.Critical, Is.EqualTo(expectedCritical));
            Assert.That(attack.Damage, Is.EqualTo(expectedDamage));
            Assert.That(combat.EnemyHealth, Is.EqualTo(100d - expectedDamage));
        }

        [Test]
        public void Miss_ConsumesCooldownAndSkipsCriticalRoll()
        {
            SetField("_weaponStats", new WeaponStats(5f, 2f, 90f, 10f, 200f));
            CombatSimulation combat = CreateCombat(new SequenceRandom(0.95d, 0.5d, 0.05d));
            Assert.That(combat.Advance(2d).Single().Hit, Is.False);
            Assert.That(combat.Advance(1d), Is.Empty);
            CombatItemEvent nextAttack = combat.Advance(1d).Single();
            Assert.That(nextAttack.Time, Is.EqualTo(4d));
            Assert.That(nextAttack.Critical, Is.True);
            Assert.That(nextAttack.Damage, Is.EqualTo(10d));
        }

        [Test]
        public void RuntimeStats_AreIndependentOfAssetSnapshotAndOtherCopies()
        {
            CombatLoadoutSnapshot snapshot = Snapshot("copy_a", "copy_b");
            CombatSimulation combat = CreateBattle(snapshot, 100d, 1000d, QuietEnemy());
            combat.PlayerWeapons[0].Stats.AttackDamage = 30f;
            combat.PlayerWeapons[0].Stats.AccuracyPercent = 0f;
            Assert.That(_weapon.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(snapshot.Items[0].BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(combat.PlayerWeapons[1].Stats.AttackDamage, Is.EqualTo(5f));
            Assert.That(combat.PlayerWeapons[1].Stats.AccuracyPercent, Is.EqualTo(100f));

            // Later changes to the source definition do not affect an already captured battle.
            SetField("_weaponStats", new WeaponStats(99f, 1f, 10f, 50f, 300f));
            Assert.That(combat.PlayerWeapons[1].Stats.AttackDamage, Is.EqualTo(5f));
            CombatSimulation fresh = CreateBattle(snapshot, 100d, 1000d, QuietEnemy());
            Assert.That(fresh.PlayerWeapons[0].Stats.AttackDamage, Is.EqualTo(5f));
        }

        [Test]
        public void Snapshot_CopiesOccupiedCellsAndRotation()
        {
            var cells = new List<Vector2Int> { new(4, 2), new(5, 2) };
            var snapshot = new CombatItemSnapshot("a", _weapon, new Vector2Int(4, 2), ItemRotation.Clockwise90, cells);
            cells.Clear();
            Assert.That(snapshot.OccupiedCells, Has.Count.EqualTo(2));
            Assert.That(snapshot.Origin, Is.EqualTo(new Vector2Int(4, 2)));
            Assert.That(snapshot.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
        }

        [Test]
        public void PassiveItems_DoNotActivate()
        {
            SetField("_category", ItemCategory.Armor);
            SetField("_activation", new ItemActivationData());
            CombatSimulation combat = CreateCombat();
            Assert.That(combat.PlayerWeapons, Is.Empty);
            Assert.That(combat.Advance(10d), Is.Empty);
        }

        [Test]
        public void LargeAndSmallTimeSteps_ProduceIdenticalChronologicalCombat()
        {
            WeaponStats enemy = new(3f, 1.5f, 80f, 10f, 150f);
            CombatSimulation large = CreateBattle(Snapshot("a", "b"), 1000d, 1000d, enemy, new System.Random(123));
            CombatSimulation small = CreateBattle(Snapshot("a", "b"), 1000d, 1000d, enemy, new System.Random(123));
            var largeEvents = large.Advance(10d);
            var smallEvents = new List<CombatItemEvent>();
            for (int step = 0; step < 20; step++) smallEvents.AddRange(small.Advance(0.5d));
            Assert.That(small.PlayerHealth, Is.EqualTo(large.PlayerHealth));
            Assert.That(small.EnemyHealth, Is.EqualTo(large.EnemyHealth));
            Assert.That(smallEvents.Select(e => (e.Time, e.Side, e.InstanceId, e.Hit, e.Critical, e.Damage)),
                Is.EqualTo(largeEvents.Select(e => (e.Time, e.Side, e.InstanceId, e.Hit, e.Critical, e.Damage))));
        }

        [Test]
        public void LethalHit_StopsCombatWithoutPostDeathRetaliation()
        {
            SetField("_weaponStats", new WeaponStats(100f, 1f, 100f, 0f, 200f));
            CombatSimulation combat = CreateBattle(Snapshot("a"), 10d, 10d, new WeaponStats(100f, 1f, 100f, 0f, 200f));
            Assert.That(combat.Advance(20d), Has.Count.EqualTo(1));
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Victory));
            Assert.That(combat.EnemyHealth, Is.Zero);
            Assert.That(combat.PlayerHealth, Is.EqualTo(10d));
            Assert.That(combat.Advance(20d), Is.Empty);
        }

        [Test]
        public void EnemyLethalHit_EndsInDefeat()
        {
            CombatSimulation combat = CreateBattle(new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>()),
                5d, 10d, new WeaponStats(10f, 1f, 100f, 0f, 200f));
            Assert.That(combat.Advance(20d), Has.Count.EqualTo(1));
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Defeat));
            Assert.That(combat.PlayerHealth, Is.Zero);
        }

        [Test]
        public void NoDamageBattle_EndsAtTimeLimit()
        {
            SetField("_weaponStats", new WeaponStats(0f, 1f, 100f, 0f, 200f));
            CombatSimulation combat = CreateBattle(Snapshot("a"), 10d, 10d,
                new WeaponStats(0f, 1f, 100f, 0f, 200f), maximumDuration: 3d);
            combat.Advance(100d);
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Draw));
            Assert.That(combat.ElapsedSeconds, Is.EqualTo(3d));
            Assert.That(combat.PlayerHealth, Is.EqualTo(10d));
            Assert.That(combat.Advance(10d), Is.Empty);
        }

        private CombatSimulation CreateCombat(System.Random random = null)
        {
            return CreateBattle(Snapshot("weapon"), 100d, 100d, QuietEnemy(), random);
        }

        [Test]
        public void EnemyWeapons_AttackIndependentlyFromTheEnemyBackpack()
        {
            typeof(ItemData).GetField("_weaponStats", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_enemyWeapon, new WeaponStats(5f, 1f, 100f, 0f, 200f));
            ConfigureEnemyActivation(1f);
            var first = new CombatItemSnapshot("club_a", _enemyWeapon, Vector2Int.zero,
                ItemRotation.None, new[] { Vector2Int.zero });
            var second = new CombatItemSnapshot("club_b", _enemyWeapon, Vector2Int.right,
                ItemRotation.None, new[] { Vector2Int.right });
            var enemy = new CombatantSnapshot("Enemy", 30d, new CombatLoadoutSnapshot(new[] { first, second }, 3, 3));
            var player = new CombatantSnapshot("Player", 40d, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>()));
            var combat = new CombatSimulation(player, enemy);
            var attacks = combat.Advance(1d);
            Assert.That(attacks, Has.Count.EqualTo(2));
            Assert.That(attacks.Select(attack => attack.InstanceId), Is.EqualTo(new[] { "club_a", "club_b" }));
            Assert.That(combat.PlayerHealth, Is.EqualTo(30d));
            Assert.That(combat.EnemyWeapons, Has.Count.EqualTo(2));
        }

        private CombatSimulation CreateBattle(CombatLoadoutSnapshot loadout, double playerHealth,
            double enemyHealth, WeaponStats enemyStats, System.Random random = null, double maximumDuration = 120d)
        {
            typeof(ItemData).GetField("_weaponStats", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_enemyWeapon, enemyStats);
            ConfigureEnemyActivation(enemyStats.CooldownSeconds);
            var enemyItem = new CombatItemSnapshot("enemy_weapon", _enemyWeapon, Vector2Int.zero,
                ItemRotation.None, new[] { Vector2Int.zero });
            return new CombatSimulation(new CombatantSnapshot("Player", playerHealth, loadout),
                new CombatantSnapshot("Enemy", enemyHealth, new CombatLoadoutSnapshot(new[] { enemyItem }, 3, 3)),
                random, maximumDuration);
        }

        private CombatLoadoutSnapshot Snapshot(params string[] instanceIds)
        {
            return new CombatLoadoutSnapshot(instanceIds.Select(id => new CombatItemSnapshot(id,
                _weapon, Vector2Int.zero, ItemRotation.None, new[] { Vector2Int.zero })));
        }

        private static WeaponStats QuietEnemy() => new(0f, 100f, 100f, 0f, 200f);

        private void SetField(string name, object value)
        {
            typeof(ItemData).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_weapon, value);
            if (name == "_weaponStats")
                SetField("_activation", new ItemActivationData(((WeaponStats)value).CooldownSeconds, new ItemActivationEffect(ItemEffectKind.Attack)));
        }

        private void ConfigureEnemyActivation(float seconds) => typeof(ItemData)
            .GetField("_activation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_enemyWeapon,
                new ItemActivationData(seconds, new ItemActivationEffect(ItemEffectKind.Attack)));

        private sealed class SequenceRandom : System.Random
        {
            private readonly Queue<double> _rolls;
            public SequenceRandom(params double[] rolls) { _rolls = new Queue<double>(rolls); }
            public override double NextDouble() => _rolls.Dequeue();
        }
    }
}
