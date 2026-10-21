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

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class ScalingAndPeriodicStatusTests
    {
        private const double Tolerance = 1e-5;
        private static ItemData Item(string name) => AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/_Project/Data/Items/Item_{name}.asset");
        private static CombatantSnapshot Unit(double attack = 10, double health = 1000)
            => new("Test", health, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>()), attack);
        private static CombatItemSnapshot Snap(string id, ItemData data, Vector2Int origin, CharacterStatsSnapshot stats)
            => new(id, data, origin, ItemRotation.None, data.CreateShape().GetCells(ItemRotation.None).Select(c => c + origin), ownerStats: stats);

        [TestCase(50, 10, 5, 2.5f, 1)]
        [TestCase(100, 20, 10, 5, 2)]
        [TestCase(50, 0, 0, 2.5f, 0)]
        public void AuthoredItems_UseOnlyOwnerCoefficients_AndTooltipSharesResolvedValues(double hp, double attack,
            float swordDamage, float armor, float stoneBonus)
        {
            var owner = new CharacterStatsSnapshot(hp, attack);
            var sword = Snap("sword", Item("TrainingBlade"), Vector2Int.zero, owner);
            var shield = Snap("shield", Item("WoodenShield"), new(2, 0), owner);
            var stone = Snap("stone", Item("Whetstone"), new(1, 0), owner);
            Assert.That(sword.BaseWeaponStats.AttackDamage, Is.EqualTo(swordDamage).Within(Tolerance));
            Assert.That(shield.Activation.Effects.Single().Amount, Is.EqualTo(armor).Within(Tolerance));
            Assert.That(stone.PlacementEffects.Single().AttackDamageBonus, Is.EqualTo(stoneBonus).Within(Tolerance));
            Assert.That(ItemTooltipContent.FromItem(Item("TrainingBlade"), owner).AttackDamage, Is.EqualTo(swordDamage).Within(Tolerance));
            string description = ItemTooltipContent.FromItem(Item("WoodenShield"), owner).GetDescriptionText();
            Assert.That(description, Does.Contain($"방어도를 {armor:0.#}"));
            Assert.That(description, Does.Not.Contain("계수").And.Not.Contain("최대 체력").And.Not.Contain("5%"));
            var player = new CombatantSnapshot("Player", hp, new(new[] { sword, shield, stone }), attack);
            var combat = new CombatSimulation(player, Unit());
            var runtime = combat.PlayerWeapons.Single();
            Assert.That(runtime.Stats.AttackDamage, Is.EqualTo(swordDamage + stoneBonus).Within(Tolerance));
            Assert.That(ItemTooltipContent.FromCombat(sword, runtime).GetStatChange(WeaponStatIcon.Attack),
                Is.EqualTo(stoneBonus > 0 ? ItemStatChange.Improved : ItemStatChange.Unchanged));
            Assert.That(Item("TrainingBlade").BaseWeaponStats.AttackDamage, Is.EqualTo(5));
        }

        [Test]
        public void CharacterCaptureAndItemSnapshot_DoNotReadBackChangedAssets()
        {
            var character = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Character_Default.asset"));
            var sword = UnityEngine.Object.Instantiate(Item("TrainingBlade"));
            try
            {
                var entry = character.CaptureStats();
                var snapshot = Snap("sword", sword, Vector2Int.zero, entry);
                Set(character, "_maximumHealth", 200f); Set(character, "_attack", 100f);
                Set(sword, "_attackScaling", new StatScaling(StatReference.Attack, 9));
                Assert.That(entry.MaximumHealth, Is.EqualTo(50)); Assert.That(entry.Attack, Is.EqualTo(10));
                Assert.That(snapshot.BaseWeaponStats.AttackDamage, Is.EqualTo(5));
                Assert.That(character.CaptureStats().Attack, Is.EqualTo(100));
            }
            finally { UnityEngine.Object.DestroyImmediate(character); UnityEngine.Object.DestroyImmediate(sword); }
        }

        [Test]
        public void EnemyRoundStats_ResolveScalingFirst_AndExplicitItemOverridesRemainFinal()
        {
            var enemy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_Goblin.asset"));
            try
            {
                Set(enemy, "_items", new List<EnemyItemPlacement>
                {
                    new("club", AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Enemies/Items/Item_GoblinClub.asset"), Vector2Int.zero),
                    new("shield", Item("WoodenShield"), new(2, 0))
                });
                var snapshot = enemy.CreateCombatantSnapshot(new EnemyEncounterOverrides(200, 40,
                    new EnemyItemOverride("club", new EnemyItemStatOverrides(attackDamage: 7))));
                Assert.That(snapshot.Stats.Attack, Is.EqualTo(40)); Assert.That(snapshot.MaximumHealth, Is.EqualTo(200));
                Assert.That(snapshot.Loadout.Items.Single(i => i.ItemId == "goblin_club").BaseWeaponStats.AttackDamage, Is.EqualTo(7));
                Assert.That(snapshot.Loadout.Items.Single(i => i.ItemId == "wooden_shield").Activation.Effects.Single().Amount, Is.EqualTo(10));
                var combat = new CombatSimulation(Unit(10), snapshot);
                combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Burn, 2, CombatSide.Enemy);
                Assert.That(combat.GetStatusDamagePerTick(CombatSide.Player, StatusEffectKind.Burn), Is.EqualTo(9.6).Within(Tolerance));
            }
            finally { UnityEngine.Object.DestroyImmediate(enemy); }
        }

        [Test]
        public void BurnDecaysAfterDamage_PoisonPersists_AndFractionalDamageIsPreserved()
        {
            var combat = new CombatSimulation(Unit(), Unit());
            var ticks = new List<PeriodicStatusTickEvent>(); combat.StatusTicked += ticks.Add;
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 3, CombatSide.Player);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2, CombatSide.Player);
            combat.Advance(.99); Assert.That(ticks, Is.Empty);
            combat.Advance(2.01);
            Assert.That(ticks.Select(t => t.Time), Is.EqualTo(new[] { 1d, 1d, 2d, 2d, 3d, 3d }));
            Assert.That(ticks.Where(t => t.Kind == StatusEffectKind.Burn).Select(t => t.Stacks), Is.EqualTo(new long[] { 3, 2, 1 }));
            Assert.That(combat.EnemyHealth, Is.EqualTo(1000 - 13.2).Within(Tolerance));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.Zero);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Poison), Is.EqualTo(2));
            combat.Advance(2); Assert.That(combat.EnemyHealth, Is.EqualTo(1000 - 17.2).Within(Tolerance));
        }

        [Test]
        public void MoreStacks_DoNotPostponeTick_AndClearedStatusesStartANewClock()
        {
            var combat = new CombatSimulation(Unit(), Unit());
            var ticks = new List<PeriodicStatusTickEvent>(); combat.StatusTicked += ticks.Add;
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 1);
            combat.Advance(.75); combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2);
            combat.Advance(.25); Assert.That(ticks.Single().Time, Is.EqualTo(1));
            Assert.That(ticks.Single().Damage, Is.EqualTo(3).Within(Tolerance));
            combat.RemoveStatus(CombatSide.Enemy, StatusEffectKind.Poison, long.MaxValue);
            combat.Advance(.5); combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 1);
            combat.Advance(.5); Assert.That(ticks.Count, Is.EqualTo(1));
            combat.Advance(.5); Assert.That(ticks.Last().Time, Is.EqualTo(2.5));
        }

        [Test]
        public void DamageUsesEachSourcesEntryAttack_NotTargetsStats_AndBurnLosesOneStackTotal()
        {
            var combat = new CombatSimulation(Unit(10), Unit(20));
            var sources = new List<CombatDamageEvent>(); combat.DamageApplied += sources.Add;
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2, CombatSide.Player);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 1, CombatSide.Enemy);
            combat.Advance(1);
            Assert.That(sources.Select(e => e.Source), Is.EqualTo(new[] { CombatSide.Player, CombatSide.Enemy }));
            Assert.That(combat.EnemyHealth, Is.EqualTo(995.2).Within(Tolerance));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(2));
            Assert.That(combat.GetStatusDamagePerTick(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(3.6).Within(Tolerance));
        }

        [Test]
        public void PeriodicDamage_IgnoresSpeedEvasionAndCritical_AndPublishesOnlyStatusDamage()
        {
            var random = new NoRandom();
            var combat = new CombatSimulation(Unit(), Unit(), random);
            int avoided = 0; combat.AttackAvoided += _ => avoided++;
            var damage = new List<CombatDamageEvent>(); combat.DamageApplied += damage.Add;
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 100);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Freeze, 95);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 1000);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Precision, 1000);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 1);
            Assert.That(combat.Advance(3), Is.Empty);
            Assert.That(damage, Has.Count.EqualTo(3));
            Assert.That(damage.All(d => d.Kind == CombatDamageKind.StatusEffect), Is.True);
            Assert.That(combat.EnemyHealth, Is.EqualTo(997).Within(Tolerance));
            Assert.That(avoided, Is.Zero); Assert.That(combat.GetAvoidanceCount(CombatSide.Enemy), Is.Zero);
        }

        [Test]
        public void StatusDefinitionIsCopied_AndDotTickStopsCombatAtItsExactTime()
        {
            var definition = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<StatusEffectData>("Assets/_Project/Data/StatusEffects/Status_Burn.asset"));
            try
            {
                var combat = new CombatSimulation(Unit(), Unit(100, 1), statusEffects: new[] { definition });
                Set(definition, "_damageScaling", new StatScaling(StatReference.Attack, 10));
                Set(definition, "_tickIntervalSeconds", .1f);
                combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 1);
                combat.Advance(10);
                Assert.That(combat.Result, Is.EqualTo(CombatResult.Victory));
                Assert.That(combat.ElapsedSeconds, Is.EqualTo(1));
                Assert.That(combat.EnemyHealth, Is.Zero);
                Assert.That(combat.Advance(10), Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        [Test]
        public void MixedItemAndStatusTimeline_IsIdenticalForLargeAndSmallAdvances()
        {
            CombatSimulation Build()
            {
                var stats = new CharacterStatsSnapshot(1000, 10);
                var loadout = new CombatLoadoutSnapshot(new[] { Snap("fire", Item("FireStaff"), Vector2Int.zero, stats), Snap("poison", Item("PoisonVial"), new(3, 0), stats) });
                return new(new CombatantSnapshot("Player", 1000, loadout), Unit(), new System.Random(4));
            }
            var large = Build(); var small = Build();
            var bigTicks = new List<PeriodicStatusTickEvent>(); var smallTicks = new List<PeriodicStatusTickEvent>();
            large.StatusTicked += bigTicks.Add; small.StatusTicked += smallTicks.Add;
            large.Advance(15);
            for (int i = 0; i < 150; i++) small.Advance(.1);
            Assert.That(small.EnemyHealth, Is.EqualTo(large.EnemyHealth).Within(Tolerance));
            Assert.That(smallTicks.Select(t => (t.Time, t.Kind, t.Stacks)), Is.EqualTo(bigTicks.Select(t => (t.Time, t.Kind, t.Stacks))));
            Assert.That(small.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Poison), Is.EqualTo(large.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Poison)));
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private sealed class NoRandom : System.Random { public override double NextDouble() => throw new AssertionException("Periodic damage must not roll attack randomness."); }
    }
}
