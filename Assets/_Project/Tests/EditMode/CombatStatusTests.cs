using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class CombatStatusTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (Object obj in _temporary) Object.DestroyImmediate(obj); _temporary.Clear(); }

        [Test]
        public void DefaultAssets_UseOnePercentAndCharmGrantsTenOncePerEquippedInstance()
        {
            foreach (string name in new[] { "Haste", "Freeze" })
                Assert.That(AssetDatabase.LoadAssetAtPath<StatusEffectData>($"Assets/_Project/Data/StatusEffects/Status_{name}.asset")
                    .PercentPerStack, Is.EqualTo(1f));
            ItemData charm = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_SwiftCharm.asset");
            CombatSimulation combat = Battle(new[] { charm, charm, Armor() });
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(20));
            Assert.That(combat.PlayerItems, Has.Count.EqualTo(1), "A battle-start accessory has no periodic clock.");
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(2d / 1.2d).Within(1e-9));
            combat.Advance(10d);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(20));
            Assert.That(charm.BattleStartStatuses.Single().Stacks, Is.EqualTo(10));
            Assert.That(charm.Activation.HasCycle, Is.False);
        }

        [Test]
        public void Opposites_CancelStackForStackInBothDirectionsAndDoNotAffectTheOtherSide()
        {
            CombatSimulation combat = Battle(new[] { Armor() });
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 5);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 3);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(2));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.Zero);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 7);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.Zero);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.EqualTo(5));
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(2d / 0.95d).Within(1e-9));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 5);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.Zero);
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(2d));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Haste), Is.Zero);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Freeze), Is.Zero);
        }

        [Test]
        public void Statuses_UseTheSameWeaponArmorAndPetClocksAndCombineWithMeleeOnlyModifiers()
        {
            ItemData weapon = Armor(ItemCategory.Weapon, ItemTags.Melee);
            Set(weapon, "_activation", new ItemActivationData(2f, new ItemActivationEffect(ItemEffectKind.Attack)));
            CombatSimulation combat = Battle(new[] { weapon, Armor(), Armor(ItemCategory.Pet) });
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10);
            Assert.That(combat.PlayerItems.All(item => Math.Abs(item.Cycle.IntervalSeconds - 2d / 1.1d) < 1e-9), Is.True);
            combat.SetActivationSpeedModifier(CombatSide.Player, "melee", 50f, new(ItemCategory.Weapon, ItemTags.Melee));
            Assert.That(combat.PlayerWeapons.Single().Stats.CooldownSeconds, Is.EqualTo(1.25f));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 20);
            Assert.That(combat.PlayerWeapons.Single().Cycle.IntervalSeconds, Is.EqualTo(2d / 1.4d).Within(1e-9));
            Assert.That(combat.PlayerItems.Single(item => item.Snapshot.Category == ItemCategory.Armor).Cycle.IntervalSeconds,
                Is.EqualTo(2d / 0.9d).Within(1e-9));
            combat.RemoveActivationSpeedModifier(CombatSide.Player, "melee");
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.EqualTo(10));
        }

        [Test]
        public void MidCycleStacks_PreserveChargeAndZeroGrantsDoNotMoveDeadlines()
        {
            CombatSimulation combat = Battle(new[] { Armor() });
            RuntimeItemCycle cycle = combat.PlayerItems.Single().Cycle;
            combat.Advance(0.5d);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 10);
            Assert.That(cycle.ProgressAt(combat.ElapsedSeconds), Is.EqualTo(0.25d).Within(1e-9));
            Assert.That(cycle.NextActivationAt, Is.EqualTo(0.5d + 1.5d / 1.1d).Within(1e-9));
            double deadline = cycle.NextActivationAt;
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 0);
            Assert.That(cycle.NextActivationAt, Is.EqualTo(deadline));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 10);
            Assert.That(cycle.NextActivationAt, Is.EqualTo(2d).Within(1e-9));
        }

        [Test]
        public void DefinitionSOs_AreCopiedPerBattleAndCapsCanBeConfigured()
        {
            var data = ScriptableObject.CreateInstance<StatusEffectData>(); _temporary.Add(data);
            Set(data, "_percentPerStack", 2.5f); Set(data, "_maximumStacks", 8);
            ItemData armor = Armor();
            CombatSimulation combat = Battle(new[] { armor }, definitions: new[] { data });
            Set(data, "_percentPerStack", 10f);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, 20);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(8));
            Assert.That(combat.GetStatusDefinition(StatusEffectKind.Haste).PercentPerStack, Is.EqualTo(2.5f));
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(2d / 1.2d).Within(1e-9));
            Assert.That(armor.Activation.CooldownSeconds, Is.EqualTo(2f));
            CombatSimulation fresh = Battle(new[] { armor }, definitions: new[] { data });
            Assert.That(fresh.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.Zero);
            Assert.That(fresh.GetStatusDefinition(StatusEffectKind.Haste).PercentPerStack, Is.EqualTo(10f));
        }

        [Test]
        public void Snapshot_CopiesBattleStartGrantsAndNewBattleDoesNotInheritStacks()
        {
            ItemData charm = Armor();
            Set(charm, "_battleStartStatuses", new List<StatusApplication> { new(StatusEffectKind.Haste, 10) });
            CombatantSnapshot unit = Unit(new[] { charm });
            Set(charm.BattleStartStatuses.Single(), "_stacks", 99);
            CombatSimulation combat = new(unit, Unit(Array.Empty<ItemData>()));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, 15);
            CombatSimulation fresh = new(unit, Unit(Array.Empty<ItemData>()));
            Assert.That(fresh.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(10));
            Assert.That(fresh.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.Zero);
        }

        [Test]
        public void FreezeTestEnemy_GrantsFifteenAtStartThenFiveEveryThreeSeconds()
        {
            EnemyEncounterData enemy = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_GoblinFreezeTest.asset");
            ItemData charm = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_SwiftCharm.asset");
            CombatSimulation combat = new(Unit(new[] { charm, Armor() }), enemy.CreateCombatantSnapshot());
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.Zero);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.EqualTo(5));
            var status = combat.Advance(3d).Single(item => item.Kind == ItemEffectKind.ApplyStatus);
            Assert.That(status.Time, Is.EqualTo(3d));
            Assert.That(status.TargetSide, Is.EqualTo(CombatSide.Player));
            Assert.That(status.StatusKind, Is.EqualTo(StatusEffectKind.Freeze));
            Assert.That(status.StatusStacks, Is.EqualTo(5));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.EqualTo(10));
            Assert.That(AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_Goblin.asset").Items, Has.Count.EqualTo(1));
        }

        [Test]
        public void PeriodicStatuses_AreFramePartitionIndependentIncludingSelfAcceleration()
        {
            ItemData source = Armor();
            Set(source, "_activation", new ItemActivationData(1f, new ItemActivationEffect(new StatusApplication(StatusEffectKind.Haste, 5))));
            ItemData slow = Armor();
            Set(slow, "_activation", new ItemActivationData(1.3f,
                new ItemActivationEffect(new StatusApplication(StatusEffectKind.Freeze, 3, StatusTarget.Opponent))));
            CombatSimulation large = Battle(new[] { source, Armor() }, new[] { slow });
            CombatSimulation small = Battle(new[] { source, Armor() }, new[] { slow });
            var expected = large.Advance(10d);
            var actual = new List<CombatItemEvent>();
            for (int step = 0; step < 100; step++) actual.AddRange(small.Advance(0.1d));
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int index = 0; index < actual.Count; index++)
            {
                Assert.That(actual[index].Time, Is.EqualTo(expected[index].Time).Within(1e-8));
                Assert.That((actual[index].Side, actual[index].InstanceId, actual[index].Kind, actual[index].StatusStacks),
                    Is.EqualTo((expected[index].Side, expected[index].InstanceId, expected[index].Kind, expected[index].StatusStacks)));
            }
            Assert.That(small.PlayerArmor, Is.EqualTo(large.PlayerArmor));
            Assert.That(small.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste),
                Is.EqualTo(large.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste)));
        }

        [Test]
        public void ExtremeStacks_UseCommonSafetyLimitsWithoutOverflowAndRejectNegativeGrants()
        {
            CombatSimulation combat = Battle(new[] { Armor() });
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, int.MaxValue);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Freeze, int.MaxValue);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Freeze), Is.EqualTo(int.MaxValue));
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(40d));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, int.MaxValue);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, int.MaxValue);
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(0.1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Haste, -1));
        }

        private ItemData Armor(ItemCategory category = ItemCategory.Armor, ItemTags tags = ItemTags.None)
        {
            ItemData data = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(data);
            Set(data, "_category", category); Set(data, "_tags", tags);
            Set(data, "_activation", new ItemActivationData(2f, new ItemActivationEffect(ItemEffectKind.GainArmor)));
            return data;
        }
        private static void Set(object obj, string name, object value)
            => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        private static CombatantSnapshot Unit(IEnumerable<ItemData> items) => new("Test", 1000d,
            new CombatLoadoutSnapshot(items.Select((item, index) => new CombatItemSnapshot($"item_{index}", item,
                Vector2Int.zero, ItemRotation.None, new[] { Vector2Int.zero }))));
        private static CombatSimulation Battle(IEnumerable<ItemData> player, IEnumerable<ItemData> enemy = null,
            StatusEffectData[] definitions = null)
            => new(Unit(player), Unit(enemy ?? Array.Empty<ItemData>()), new System.Random(123), statusEffects: definitions);
    }
}
