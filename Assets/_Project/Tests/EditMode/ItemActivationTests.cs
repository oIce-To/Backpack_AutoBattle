using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class ItemActivationTests
    {
        private readonly List<ItemData> _data = new();
        [TearDown] public void Cleanup() { foreach (ItemData item in _data) UnityEngine.Object.DestroyImmediate(item); _data.Clear(); }

        [Test]
        public void WoodenShieldAsset_GainsFivePercentMaxHealthEveryTwoSecondsAndCopiesItsDefinition()
        {
            ItemData shield = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_WoodenShield.asset");
            Assert.That(shield.Category, Is.EqualTo(ItemCategory.Armor));
            Assert.That(shield.Activation.CooldownSeconds, Is.EqualTo(2f));
            Assert.That(shield.Activation.Effects.Single().Kind, Is.EqualTo(ItemEffectKind.GainArmor));
            Assert.That(shield.Activation.Effects.Single().Amount, Is.EqualTo(1f));
            CombatSimulation combat = Battle(new[] { shield });
            Assert.That(combat.PlayerWeapons, Is.Empty);
            Assert.That(combat.Advance(1.9d), Is.Empty);
            Assert.That(combat.Advance(0.1d).Single().ArmorGained, Is.EqualTo(5d));
            Assert.That(combat.PlayerArmor, Is.EqualTo(5d));
            Assert.That(combat.Advance(4d).Select(item => item.Time), Is.EqualTo(new[] { 4d, 6d }));
            Assert.That(combat.PlayerArmor, Is.EqualTo(15d));
            Assert.That(combat.PlayerItems.Single().Activation, Is.Not.SameAs(shield.Activation));
            Assert.That(shield.Activation.CooldownSeconds, Is.EqualTo(2f));
        }

        [TestCase(5d, 0d, 40d, 0d, 35d, 0d, 5d)]
        [TestCase(5d, 1d, 40d, 0d, 36d, 1d, 4d)]
        [TestCase(5d, 8d, 40d, 3d, 40d, 5d, 0d)]
        [TestCase(5d, 5d, 40d, 0d, 40d, 5d, 0d)]
        [TestCase(0d, 5d, 40d, 5d, 40d, 0d, 0d)]
        [TestCase(2.5d, 1.2d, 1d, 0d, 0d, 1.2d, 1d)]
        public void Damage_ConsumesArmorBeforeHealth(double damage, double armor, double health,
            double armorRemaining, double healthRemaining, double absorbed, double healthDamage)
        {
            DamageResolution result = DamageResolver.Resolve(damage, armor, health);
            Assert.That(result.ArmorRemaining, Is.EqualTo(armorRemaining).Within(1e-9));
            Assert.That(result.HealthRemaining, Is.EqualTo(healthRemaining).Within(1e-9));
            Assert.That(result.ArmorAbsorbed, Is.EqualTo(absorbed).Within(1e-9));
            Assert.That(result.HealthDamage, Is.EqualTo(healthDamage).Within(1e-9));
        }

        [Test]
        public void SimultaneousShieldActivation_ProtectsAgainstFollowingEnemyAttack()
        {
            CombatSimulation combat = Battle(new[] { Armor() }, new[] { Weapon() });
            var events = combat.Advance(2d);
            Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[] { ItemEffectKind.GainArmor, ItemEffectKind.Attack }));
            Assert.That(events.Last().Damage, Is.EqualTo(5d));
            Assert.That(events.Last().ArmorAbsorbed, Is.EqualTo(1d));
            Assert.That(events.Last().HealthDamage, Is.EqualTo(4d));
            Assert.That(combat.PlayerHealth, Is.EqualTo(96d));
            Assert.That(combat.PlayerArmor, Is.Zero);
        }

        [Test]
        public void EnemyArmor_UsesTheSameAbsorptionRulesAndMissesDoNotConsumeIt()
        {
            ItemData weapon = Weapon(3f);
            CombatSimulation combat = Battle(new[] { weapon }, new[] { Armor() });
            combat.Advance(3d);
            Assert.That(combat.EnemyArmor, Is.Zero);
            Assert.That(combat.EnemyHealth, Is.EqualTo(96d));
            combat.PlayerWeapons.Single().Stats.AccuracyPercent = 0f;
            var events = combat.Advance(3d);
            Assert.That(events.Last(item => item.Kind == ItemEffectKind.Attack).ArmorAbsorbed, Is.Zero);
            Assert.That(combat.EnemyArmor, Is.EqualTo(2d));
            Assert.That(combat.EnemyHealth, Is.EqualTo(96d));
        }

        [Test]
        public void GlobalSpeed_AffectsWeaponArmorAndPetThroughOneClock()
        {
            CombatSimulation combat = Battle(new[] { Weapon(), Armor(), Armor(ItemCategory.Pet) });
            combat.SetActivationSpeedModifier(CombatSide.Player, "global", 100f);
            Assert.That(combat.PlayerItems.All(item => item.Cycle.IntervalSeconds == 1d), Is.True);
            Assert.That(combat.PlayerWeapons.Single().Stats.CooldownSeconds, Is.EqualTo(1f));
            Assert.That(combat.PlayerItems.OfType<CombatWeapon>().Single(), Is.SameAs(combat.PlayerWeapons.Single()));
            Assert.That(combat.Advance(1d), Has.Count.EqualTo(3));
            Assert.That(combat.PlayerArmor, Is.EqualTo(2d));
            Assert.That(combat.PlayerItems.All(item => item.Snapshot.Activation.CooldownSeconds == 2f), Is.True);
        }

        [Test]
        public void CategoryAndTags_AllowMeleeOnlyAndMultiTagTargeting()
        {
            CombatSimulation combat = Battle(new[]
            {
                Weapon(tags: ItemTags.Melee), Weapon(tags: ItemTags.Ranged), Weapon(tags: ItemTags.Melee | ItemTags.Magic),
                Armor(tags: ItemTags.Melee), Armor(ItemCategory.Pet, ItemTags.Melee)
            });
            combat.SetActivationSpeedModifier(CombatSide.Player, "melee", 100f, new ItemTargetFilter(ItemCategory.Weapon, ItemTags.Melee));
            foreach (CombatItem item in combat.PlayerItems)
                Assert.That(item.Cycle.IntervalSeconds, Is.EqualTo(item.Snapshot.Category == ItemCategory.Weapon
                    && item.Snapshot.Tags.HasFlag(ItemTags.Melee) ? 1d : 2d));
            combat.SetActivationSpeedModifier(CombatSide.Player, "enchanted", 100f,
                new ItemTargetFilter(requiredTags: ItemTags.Melee | ItemTags.Magic));
            Assert.That(combat.PlayerItems.Single(item => item.Snapshot.Tags == (ItemTags.Melee | ItemTags.Magic)).Cycle.IntervalSeconds,
                Is.EqualTo(2d / 3d).Within(1e-9));
        }

        [Test]
        public void MidCycleSpeedChanges_PreserveProgressAndDoNotResetOrInstantlyActivate()
        {
            CombatSimulation combat = Battle(new[] { Armor() });
            RuntimeItemCycle cycle = combat.PlayerItems.Single().Cycle;
            combat.Advance(0.5d);
            combat.SetActivationSpeedModifier(CombatSide.Player, "haste", 100f);
            Assert.That(cycle.ProgressAt(combat.ElapsedSeconds), Is.EqualTo(0.25d));
            Assert.That(cycle.NextActivationAt, Is.EqualTo(1.25d));
            Assert.That(combat.Advance(0.5d), Is.Empty);
            combat.RemoveActivationSpeedModifier(CombatSide.Player, "haste");
            Assert.That(cycle.ProgressAt(combat.ElapsedSeconds), Is.EqualTo(0.75d));
            Assert.That(cycle.NextActivationAt, Is.EqualTo(1.5d));
            Assert.That(combat.Advance(0.5d).Single().Time, Is.EqualTo(1.5d));
            Assert.That(cycle.ProgressAt(combat.ElapsedSeconds), Is.Zero);
        }

        [Test]
        public void ModifierSources_ReplaceRemoveCombineAndStayOnTheirOwnSide()
        {
            CombatSimulation combat = Battle(new[] { Armor() }, new[] { Armor() });
            RuntimeItemCycle player = combat.PlayerItems.Single().Cycle;
            combat.SetActivationSpeedModifier(CombatSide.Player, "haste", 100f);
            combat.SetActivationSpeedModifier(CombatSide.Player, "haste", 50f);
            Assert.That(player.IntervalSeconds, Is.EqualTo(2d / 1.5d).Within(1e-9));
            combat.SetActivationSpeedModifier(CombatSide.Player, "freeze", -50f);
            Assert.That(player.IntervalSeconds, Is.EqualTo(2d));
            Assert.That(combat.EnemyItems.Single().Cycle.IntervalSeconds, Is.EqualTo(2d));
            combat.RemoveActivationSpeedModifier(CombatSide.Player, "haste");
            Assert.That(player.IntervalSeconds, Is.EqualTo(4d));
            combat.RemoveActivationSpeedModifier(CombatSide.Player, "freeze");
            Assert.That(player.IntervalSeconds, Is.EqualTo(2d));
            Assert.Throws<ArgumentOutOfRangeException>(() => combat.SetActivationSpeedModifier(CombatSide.Player, "bad", float.NaN));
        }

        [Test]
        public void UnrelatedOrUnchangedModifiers_DoNotMoveAnItemsDeadline()
        {
            CombatSimulation combat = Battle(new[] { Weapon(), Data(ItemCategory.Armor, ItemTags.None, 3.7f, ItemEffectKind.GainArmor) });
            RuntimeItemCycle shield = combat.PlayerItems.Single(item => item.Snapshot.Category == ItemCategory.Armor).Cycle;
            double expected = shield.NextActivationAt;
            foreach (double step in new[] { 0.013d, 0.719d, 0.07d, 0.123456789d })
            {
                combat.Advance(step);
                combat.SetActivationSpeedModifier(CombatSide.Player, "melee", 50f, new ItemTargetFilter(ItemCategory.Weapon, ItemTags.Melee));
                Assert.That(shield.NextActivationAt, Is.EqualTo(expected));
                combat.RemoveActivationSpeedModifier(CombatSide.Player, "melee");
                Assert.That(shield.NextActivationAt, Is.EqualTo(expected));
            }
        }

        [Test]
        public void SafetyLimits_PreventZeroIntervalsAndNegativeSpeed()
        {
            CombatSimulation combat = Battle(new[] { Armor() });
            RuntimeItemCycle cycle = combat.PlayerItems.Single().Cycle;
            combat.SetActivationSpeedModifier(CombatSide.Player, "slow", -1000f);
            Assert.That(cycle.IntervalSeconds, Is.EqualTo(40d));
            combat.SetActivationSpeedModifier(CombatSide.Player, "slow", 100000f);
            Assert.That(cycle.IntervalSeconds, Is.EqualTo(0.1d));
        }

        [Test]
        public void WeaponCooldownAdapter_ChangesTheCommonClockAndPreservesProgress()
        {
            CombatSimulation combat = Battle(new[] { Weapon() });
            combat.Advance(0.5d);
            CombatWeapon weapon = combat.PlayerWeapons.Single();
            weapon.Stats.CooldownSeconds = 4f;
            Assert.That(weapon.Cycle.ProgressAt(combat.ElapsedSeconds), Is.EqualTo(0.25d));
            Assert.That(weapon.NextAttackAt, Is.EqualTo(3.5d));
            Assert.That(weapon.Cycle.BaseIntervalSeconds, Is.EqualTo(4d));
        }

        [Test]
        public void MixedItemCombat_IsFrameRateIndependentAndNewBattleResetsArmorAndSpeed()
        {
            ItemData[] player = { Weapon(1.5f), Armor(), Armor(ItemCategory.Pet) };
            ItemData[] enemy = { Weapon(1.5f), Armor() };
            CombatSimulation large = Battle(player, enemy);
            CombatSimulation small = Battle(player, enemy);
            large.SetActivationSpeedModifier(CombatSide.Player, "global", 50f);
            small.SetActivationSpeedModifier(CombatSide.Player, "global", 50f);
            var expected = large.Advance(10d);
            var actual = new List<CombatItemEvent>();
            for (int step = 0; step < 100; step++) actual.AddRange(small.Advance(0.1d));
            Assert.That(actual.Select(item => (item.Time, item.Side, item.InstanceId, item.Kind, item.Damage, item.ArmorAbsorbed, item.ArmorGained)),
                Is.EqualTo(expected.Select(item => (item.Time, item.Side, item.InstanceId, item.Kind, item.Damage, item.ArmorAbsorbed, item.ArmorGained))));
            Assert.That(small.PlayerArmor, Is.EqualTo(large.PlayerArmor));
            Assert.That(small.PlayerHealth, Is.EqualTo(large.PlayerHealth));
            CombatSimulation fresh = new(large.Player, large.Enemy);
            Assert.That(fresh.PlayerArmor, Is.Zero);
            Assert.That(fresh.PlayerItems.All(item => item.Cycle.IntervalSeconds == item.Activation.CooldownSeconds), Is.True);
            Assert.That(player[0].Activation.CooldownSeconds, Is.EqualTo(1.5f));
        }

        private ItemData Weapon(float interval = 2f, ItemTags tags = ItemTags.Melee)
            => Data(ItemCategory.Weapon, tags, interval, ItemEffectKind.Attack);
        private ItemData Armor(ItemCategory category = ItemCategory.Armor, ItemTags tags = ItemTags.None)
            => Data(category, tags, 2f, ItemEffectKind.GainArmor);
        private ItemData Data(ItemCategory category, ItemTags tags, float interval, ItemEffectKind kind)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _data.Add(item);
            Set(item, "_category", category); Set(item, "_tags", tags);
            Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(kind)));
            Set(item, "_weaponStats", new WeaponStats(5f, interval, 100f, 0f, 200f));
            return item;
        }
        private static void Set(ItemData item, string name, object value)
            => typeof(ItemData).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, value);
        private static CombatSimulation Battle(IEnumerable<ItemData> player, IEnumerable<ItemData> enemy = null)
        {
            CombatantSnapshot Unit(IEnumerable<ItemData> items) => new("Test", 100d, new CombatLoadoutSnapshot(items.Select((data, index) =>
                new CombatItemSnapshot($"item_{index}", data, Vector2Int.zero, ItemRotation.None, new[] { Vector2Int.zero }, ownerStats: new CharacterStatsSnapshot(100, 10)))));
            return new CombatSimulation(Unit(player), Unit(enemy ?? Array.Empty<ItemData>()), new System.Random(123));
        }
    }
}
