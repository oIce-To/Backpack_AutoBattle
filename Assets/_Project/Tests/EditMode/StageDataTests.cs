using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Stages;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Stages
{
    public sealed class StageDataTests
    {
        private readonly List<Object> _temporary = new();
        private static StageData First => AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Project/Data/Stages/Stage_01.asset");
        private static StageData Late => AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Project/Data/Stages/Stage_08_Example.asset");
        private static EnemyEncounterData Goblin => AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_Goblin.asset");
        [TearDown] public void Cleanup() { foreach (Object obj in _temporary) Object.DestroyImmediate(obj); _temporary.Clear(); }
        private T Clone<T>(T source) where T : Object { T result = Object.Instantiate(source); _temporary.Add(result); return result; }
        private static void Set(object value, string name, object content)
            => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, content);

        [Test]
        public void StageAssets_HaveTwelveValidRoundsIncreasingHealthAndFixedSixBySixEnemies()
        {
            foreach (StageData stage in new[] { First, Late })
            {
                Assert.That(stage, Is.Not.Null);
                Assert.That(stage.GetValidationErrors(), Is.Empty);
                Assert.That(stage.RoundCount, Is.EqualTo(12));
                double previous = 0d;
                foreach (StageRoundData round in stage.Rounds)
                {
                    CombatantSnapshot snapshot = round.CreateEnemySnapshot();
                    Assert.That(snapshot.MaximumHealth, Is.GreaterThan(previous));
                    previous = snapshot.MaximumHealth;
                    Assert.That(snapshot.Loadout.Width, Is.EqualTo(6));
                    Assert.That(snapshot.Loadout.Height, Is.EqualTo(6));
                }
                Assert.That(stage.GetRound(12).IsBoss, Is.True);
                Assert.Throws<InvalidOperationException>(() => stage.GetRound(0));
                Assert.Throws<InvalidOperationException>(() => stage.GetRound(13));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyEncounterData", new[] { "Assets/_Project/Data/Enemies" }))
            {
                var encounter = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(encounter.CreateCombatantSnapshot().Loadout.Width, Is.EqualTo(6));
                Assert.That(new SerializedObject(encounter).FindProperty("_backpackWidth"), Is.Null, "Backpack size is not an authorable scaling stat.");
            }
        }

        [Test]
        public void SameSpecies_GetsDifferentHealthAndAttackAcrossRoundsAndStagesWithoutChangingOriginals()
        {
            Assert.That(First.GetRound(1).Encounter, Is.SameAs(First.GetRound(2).Encounter));
            Assert.That(First.GetRound(1).Encounter.Enemy, Is.SameAs(Late.GetRound(1).Encounter.Enemy));
            CombatantSnapshot first = First.GetRound(1).CreateEnemySnapshot();
            CombatantSnapshot second = First.GetRound(2).CreateEnemySnapshot();
            CombatantSnapshot late = Late.GetRound(1).CreateEnemySnapshot();
            Assert.That(first.MaximumHealth, Is.EqualTo(30d));
            Assert.That(second.MaximumHealth, Is.EqualTo(35d));
            Assert.That(late.MaximumHealth, Is.EqualTo(180d));
            Assert.That(first.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(second.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(6f));
            Assert.That(late.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(25f));
            Assert.That(Goblin.MaximumHealth, Is.EqualTo(30));
            Assert.That(Goblin.Items[0].Item.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
        }

        [Test]
        public void Overrides_ApplyItemThenEncounterThenRoundAndBecomeTheTooltipBaseline()
        {
            EnemyEncounterData encounter = Clone(Goblin);
            var baseline = new EnemyItemStatOverrides(attackDamage: 9f, cooldownSeconds: 4f, accuracyPercent: 80f,
                criticalChancePercent: 20f, criticalDamagePercent: 300f);
            Set(encounter, "_items", new List<EnemyItemPlacement> { new("club", Goblin.Items[0].Item, Vector2Int.zero, stats: baseline) });
            var round = new EnemyEncounterOverrides(60, new EnemyItemOverride("club", new(attackDamage: 12f, criticalChancePercent: 0f)));
            CombatantSnapshot snapshot = encounter.CreateCombatantSnapshot(round);
            CombatItemSnapshot item = snapshot.Loadout.Items.Single();
            Assert.That(snapshot.MaximumHealth, Is.EqualTo(60d));
            Assert.That(item.BaseWeaponStats.AttackDamage, Is.EqualTo(12f));
            Assert.That(item.BaseWeaponStats.AccuracyPercent, Is.EqualTo(80f));
            Assert.That(item.BaseWeaponStats.CriticalChancePercent, Is.Zero);
            Assert.That(item.BaseWeaponStats.CriticalDamagePercent, Is.EqualTo(300f));
            Assert.That(item.Activation.CooldownSeconds, Is.EqualTo(4f));
            var combat = new CombatSimulation(new("Player", 1000d, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>())), snapshot);
            CombatWeapon weapon = combat.EnemyWeapons.Single();
            ItemTooltipContent tooltip = ItemTooltipContent.FromCombat(item, weapon);
            foreach (WeaponStatIcon stat in Enum.GetValues(typeof(WeaponStatIcon)))
                Assert.That(tooltip.GetStatChange(stat), Is.EqualTo(ItemStatChange.Unchanged));
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Haste, 10);
            Assert.That(ItemTooltipContent.FromCombat(item, weapon).GetStatChange(WeaponStatIcon.Cooldown), Is.EqualTo(ItemStatChange.Improved));
            Assert.That(item.Activation.CooldownSeconds, Is.EqualTo(4f));
            Assert.That(encounter.CreateCombatantSnapshot().Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(9f));
            Assert.That(Goblin.Items[0].Item.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Set(round, "_maximumHealth", 99);
            Set(encounter, "_items", new List<EnemyItemPlacement>());
            Assert.That(snapshot.MaximumHealth, Is.EqualTo(60d));
            Assert.That(snapshot.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(12f));
        }

        [Test]
        public void SlotIds_TargetOneOfTwoIdenticalItemsAndSurviveReordering()
        {
            EnemyEncounterData encounter = Clone(Goblin);
            var placements = new List<EnemyItemPlacement>
            {
                new("main", Goblin.Items[0].Item, Vector2Int.zero),
                new("offhand", Goblin.Items[0].Item, new Vector2Int(4, 0))
            };
            Set(encounter, "_items", placements);
            var overrides = new EnemyEncounterOverrides(null, new EnemyItemOverride("offhand", new(attackDamage: 10f)));
            var before = encounter.CreateCombatantSnapshot(overrides).Loadout;
            placements.Reverse();
            var after = encounter.CreateCombatantSnapshot(overrides).Loadout;
            Assert.That(before.Items.Select(item => item.InstanceId), Is.EqualTo(after.Items.Select(item => item.InstanceId)));
            Assert.That(after.Items.Single(item => item.InstanceId.EndsWith(":main")).BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(after.Items.Single(item => item.InstanceId.EndsWith(":offhand")).BaseWeaponStats.AttackDamage, Is.EqualTo(10f));
            Set(placements[0], "_origin", new Vector2Int(5, 4));
            Assert.DoesNotThrow(() => encounter.CreateCombatantSnapshot()); // Last column/row are usable in the 6x6 bag.
        }

        [TestCase("health")]
        [TestCase("attack")]
        [TestCase("cooldown")]
        [TestCase("accuracy")]
        [TestCase("criticalChance")]
        [TestCase("criticalDamage")]
        [TestCase("nan")]
        [TestCase("infinity")]
        [TestCase("missingSlot")]
        [TestCase("duplicateOverride")]
        public void InvalidOverrides_AreRejectedRatherThanClampedOrSilentlyIgnored(string invalid)
        {
            EnemyItemStatOverrides stats = invalid switch
            {
                "attack" => new(attackDamage: -1f), "cooldown" => new(cooldownSeconds: 0f),
                "accuracy" => new(accuracyPercent: 101f), "criticalChance" => new(criticalChancePercent: -1f),
                "criticalDamage" => new(criticalDamagePercent: 99f), "nan" => new(attackDamage: float.NaN),
                "infinity" => new(cooldownSeconds: float.PositiveInfinity), _ => new()
            };
            var changes = new List<EnemyItemOverride> { new(invalid == "missingSlot" ? "missing" : "club", stats) };
            if (invalid == "duplicateOverride") changes.Add(new("club", new()));
            var overrides = new EnemyEncounterOverrides(invalid == "health" ? 0 : null, changes.ToArray());
            Assert.Throws<InvalidOperationException>(() => Goblin.CreateCombatantSnapshot(overrides));
        }

        [Test]
        public void ZeroOverrides_AreRealValuesAndDisabledFieldsUseTheOriginal()
        {
            var changes = new EnemyItemStatOverrides(attackDamage: 0f, accuracyPercent: 0f, criticalChancePercent: 0f);
            CombatItemSnapshot item = Goblin.CreateCombatantSnapshot(new(null, new EnemyItemOverride("club", changes))).Loadout.Items.Single();
            Assert.That(item.BaseWeaponStats.AttackDamage, Is.Zero);
            Assert.That(item.BaseWeaponStats.AccuracyPercent, Is.Zero);
            Assert.That(item.BaseWeaponStats.CriticalChancePercent, Is.Zero);
            Assert.That(item.BaseWeaponStats.CriticalDamagePercent, Is.EqualTo(200f));
            Assert.That(item.Activation.CooldownSeconds, Is.EqualTo(2f));
        }

        [TestCase("count")]
        [TestCase("round")]
        [TestCase("encounter")]
        [TestCase("species")]
        [TestCase("duplicateSlot")]
        [TestCase("overlap")]
        [TestCase("outside")]
        [TestCase("item")]
        public void InvalidStageOrLoadout_HasActionableValidationErrors(string invalid)
        {
            StageData stage = Clone(First);
            EnemyEncounterData encounter = Clone(Goblin);
            var rounds = Enumerable.Range(0, 12).Select(_ => new StageRoundData(encounter)).ToList();
            if (invalid == "count") rounds.RemoveAt(11);
            if (invalid == "round") rounds[0] = null;
            if (invalid == "encounter") rounds[0] = new(null);
            if (invalid == "species") Set(encounter, "_enemy", null);
            if (invalid == "duplicateSlot") Set(encounter, "_items", new List<EnemyItemPlacement>
                { new("club", Goblin.Items[0].Item, Vector2Int.zero), new("club", Goblin.Items[0].Item, new Vector2Int(3, 0)) });
            if (invalid == "overlap") Set(encounter, "_items", new List<EnemyItemPlacement>
                { new("club", Goblin.Items[0].Item, Vector2Int.zero), new("second", Goblin.Items[0].Item, Vector2Int.zero) });
            if (invalid == "outside") Set(encounter, "_items", new List<EnemyItemPlacement> { new("club", Goblin.Items[0].Item, new Vector2Int(5, 5)) });
            if (invalid == "item") Set(encounter, "_items", new List<EnemyItemPlacement> { new("club", null, Vector2Int.zero) });
            Set(stage, "_rounds", rounds);
            Assert.That(stage.GetValidationErrors(), Is.Not.Empty);
            Assert.Throws<InvalidOperationException>(() => stage.ValidateOrThrow());
            Assert.That(First.GetValidationErrors(), Is.Empty);
        }
    }
}
