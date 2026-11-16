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
        private static StageData LoadStage(int number)
            => AssetDatabase.LoadAssetAtPath<StageData>($"Assets/_Project/Data/Stages/Stage_{number:00}.asset");
        private static StageData First => LoadStage(1);
        private static EnemyEncounterData Goblin => AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_Goblin.asset");
        [TearDown] public void Cleanup() { foreach (Object obj in _temporary) Object.DestroyImmediate(obj); _temporary.Clear(); }
        private T Clone<T>(T source) where T : Object { T result = Object.Instantiate(source); _temporary.Add(result); return result; }
        private static void Set(object value, string name, object content)
            => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, content);

        [Test]
        public void StageAssets_HaveTwelveValidRoundsIncreasingHealthAndFixedSixBySixEnemies()
        {
            var stages = Enumerable.Range(1, 6).Select(LoadStage).ToArray();
            foreach (StageData stage in stages)
            {
                Assert.That(stage, Is.Not.Null);
                Assert.That(stage.GetValidationErrors(), Is.Empty);
                Assert.That(stage.RoundCount, Is.EqualTo(12));
                double previous = 0d;
                for (int index = 0; index < stage.RoundCount; index++)
                {
                    StageRoundData round = stage.Rounds[index];
                    CombatantSnapshot snapshot = round.CreateEnemySnapshot();
                    Assert.That(snapshot.MaximumHealth, Is.GreaterThan(previous));
                    previous = snapshot.MaximumHealth;
                    Assert.That(snapshot.Loadout.Width, Is.EqualTo(6));
                    Assert.That(snapshot.Loadout.Height, Is.EqualTo(6));
                    Assert.That(snapshot.Loadout.Items.All(item => item.ItemId != "wooden_shield"), Is.True);
                    Assert.That(snapshot.Illustration, Is.Not.Null);
                    Assert.That(AssetDatabase.GetAssetPath(snapshot.Illustration), Does.EndWith("-standing-approved.png"));
                    Assert.That(snapshot.Loadout.Items.All(item => item.ItemId != "goblin_club"), Is.True);
                    Assert.That(round.IsBoss, Is.EqualTo((index + 1) % 4 == 0));
                }
                Assert.That(stage.Rounds.Select(round => round.Encounter.Enemy).Distinct().Count(), Is.EqualTo(4));
                Assert.Throws<InvalidOperationException>(() => stage.GetRound(0));
                Assert.Throws<InvalidOperationException>(() => stage.GetRound(13));
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<StageData>("Assets/_Project/Data/Stages/Stage_08_Example.asset"), Is.Null);
            for (int index = 1; index < stages.Length; index++)
                for (int round = 1; round <= 12; round++)
                {
                    var previous = stages[index - 1].GetRound(round).CreateEnemySnapshot();
                    var current = stages[index].GetRound(round).CreateEnemySnapshot();
                    Assert.That(current.MaximumHealth, Is.GreaterThan(previous.MaximumHealth));
                    Assert.That(WeaponDamagePerSecond(current), Is.GreaterThan(WeaponDamagePerSecond(previous)));
                }
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyEncounterData", new[] { "Assets/_Project/Data/Enemies" }))
            {
                var encounter = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(encounter.CreateCombatantSnapshot().Loadout.Width, Is.EqualTo(6));
                Assert.That(encounter.Items.All(item => item.Item.ItemId != "wooden_shield"), Is.True);
                Assert.That(new SerializedObject(encounter).FindProperty("_backpackWidth"), Is.Null, "Backpack size is not an authorable scaling stat.");
            }
        }

        [Test]
        public void OriginMines_ReuseTheMinerAndDrawDistinctBossesFromTheCorrectPools()
        {
            var normalStages = Enumerable.Range(1, 6).Select(LoadStage).ToArray();
            var midPool = normalStages.SelectMany(stage => new[] { stage.GetRound(4).Encounter, stage.GetRound(8).Encounter }).ToArray();
            var finalPool = normalStages.Select(stage => stage.GetRound(12).Encounter).ToArray();
            var finalBosses = new HashSet<EnemyEncounterData>();
            for (int i = 1; i <= 5; i++)
            {
                var mine = AssetDatabase.LoadAssetAtPath<StageData>($"Assets/_Project/Data/Stages/OriginMine_{i:00}.asset");
                Assert.That(mine.GetValidationErrors(), Is.Empty);
                Assert.That(midPool, Has.Member(mine.GetRound(4).Encounter));
                Assert.That(midPool, Has.Member(mine.GetRound(8).Encounter));
                Assert.That(mine.GetRound(4).Encounter, Is.Not.SameAs(mine.GetRound(8).Encounter));
                Assert.That(finalPool, Has.Member(mine.GetRound(12).Encounter));
                Assert.That(finalBosses.Add(mine.GetRound(12).Encounter), Is.True);
                for (int round = 1; round <= mine.RoundCount; round++)
                {
                    StageRoundData definition = mine.GetRound(round);
                    Assert.That(definition.IsBoss, Is.EqualTo(round % 4 == 0));
                    if (definition.IsBoss)
                    {
                        Rect crop = definition.Encounter.Enemy.PortraitCrop;
                        Assert.That(crop.width, Is.InRange(.01f, .99f));
                        Assert.That(crop.height, Is.InRange(.01f, .99f));
                        Assert.That(crop.xMin >= 0 && crop.xMax <= 1 && crop.yMin >= 0 && crop.yMax <= 1, Is.True);
                    }
                    else
                    {
                        Assert.That(definition.Encounter.Enemy.EnemyId, Is.EqualTo("skeleton_miner"));
                        Assert.That(AssetDatabase.GetAssetPath(definition.CreateEnemySnapshot().Illustration),
                            Does.EndWith("skeleton-miner-standing-approved.png"));
                    }
                }
            }
        }

        [Test]
        public void EachRoundScalesItsEncounterWithoutChangingOriginals()
        {
            Assert.That(First.GetRound(1).Encounter, Is.SameAs(First.GetRound(2).Encounter));
            CombatantSnapshot first = First.GetRound(1).CreateEnemySnapshot();
            CombatantSnapshot second = First.GetRound(2).CreateEnemySnapshot();
            CombatantSnapshot late = LoadStage(5).GetRound(1).CreateEnemySnapshot();
            Assert.That(first.MaximumHealth, Is.EqualTo(30d));
            Assert.That(second.MaximumHealth, Is.EqualTo(35d));
            Assert.That(late.MaximumHealth, Is.EqualTo(78d));
            Assert.That(first.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(second.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(6f));
            Assert.That(late.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(13f));
            Assert.That(Goblin.MaximumHealth, Is.EqualTo(30));
            Assert.That(Goblin.Items[0].Item.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
        }

        private static double WeaponDamagePerSecond(CombatantSnapshot snapshot)
            => snapshot.Loadout.Items.Where(item => item.Activation.HasEffect(ItemEffectKind.Attack))
                .Sum(item => item.BaseWeaponStats.AttackDamage / item.Activation.CooldownSeconds);

        private static CombatSimulation Fight(int stage, int round)
            => new(new("Player", 10000d, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>())),
                LoadStage(stage).GetRound(round).CreateEnemySnapshot(), new System.Random(1));

        [TestCase(2, 4, StatusEffectKind.Poison)]
        [TestCase(3, 8, StatusEffectKind.Burn)]
        [TestCase(5, 12, StatusEffectKind.Freeze)]
        [TestCase(6, 4, StatusEffectKind.Poison)]
        public void EnemyImmunities_BlockIncomingStacksButKeepOutgoingStacks(int stage, int round, StatusEffectKind kind)
        {
            CombatSimulation combat = Fight(stage, round);
            combat.ApplyStatus(CombatSide.Enemy, kind, 3);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, kind), Is.Zero);
            combat.Advance(2);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, kind), Is.GreaterThan(0));
        }

        [TestCase(3, true)]
        [TestCase(6, false)]
        public void FinalBoss_RevivesOnceWithASeparatePharaohDamageImmunityWindow(int stage, bool invulnerable)
        {
            CombatSimulation combat = Fight(stage, 12);
            CombatWeapon weapon = combat.EnemyWeapons.Single();
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 1000, CombatDamageKind.Other);
            Assert.That(combat.EnemyHealth, Is.EqualTo(combat.EnemyMaxHealth * .3d));
            Assert.That(weapon.Stats.AttackDamage, Is.EqualTo(weapon.BaseStats.AttackDamage * 1.5f));
            Assert.That(weapon.Stats.CooldownSeconds, Is.LessThan(weapon.BaseStats.CooldownSeconds));
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Running));
            if (invulnerable)
            {
                combat.Advance(2.999);
                Assert.That(combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 1000, CombatDamageKind.Other).HealthDamage, Is.Zero);
                combat.Advance(.0011);
            }
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 1000, CombatDamageKind.Other);
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Victory));
        }

        [Test]
        public void ArmorAndHealthChange_RefreshEnemyWeaponStats()
        {
            CombatSimulation guardian = Fight(2, 8);
            CombatWeapon fist = guardian.EnemyWeapons.Single();
            Assert.That(guardian.EnemyArmor, Is.EqualTo(guardian.EnemyMaxHealth * .35d));
            Assert.That(fist.Stats.AttackDamage, Is.EqualTo(fist.BaseStats.AttackDamage * 1.5f));
            guardian.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, guardian.EnemyArmor, CombatDamageKind.Other);
            guardian.Advance(2);
            Assert.That(guardian.EnemyArmor, Is.Zero);
            Assert.That(fist.Stats.AttackDamage, Is.EqualTo(fist.BaseStats.AttackDamage));

            CombatSimulation yeti = Fight(5, 4);
            CombatWeapon claw = yeti.EnemyWeapons.Single();
            yeti.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, yeti.EnemyMaxHealth * .5d, CombatDamageKind.Other);
            Assert.That(claw.Stats.AttackDamage, Is.EqualTo(claw.BaseStats.AttackDamage * 1.25f));
            Assert.That(claw.Stats.CooldownSeconds, Is.EqualTo(1.6f));
        }

        [Test]
        public void EnemyHitBonuses_AccumulateForThePriestAndAcrossTheWeaponMastersLoadout()
        {
            CombatSimulation priest = Fight(4, 4);
            Assert.That(priest.Advance(2).Count(item => item.Kind == ItemEffectKind.Attack && item.Hit), Is.EqualTo(1));
            Assert.That(priest.EnemyWeapons.Single().Stats.CooldownSeconds, Is.EqualTo(2f / 1.05f).Within(.0001f));

            CombatSimulation master = Fight(6, 8);
            Assert.That(master.EnemyWeapons.Count, Is.EqualTo(4));
            int hits = master.Advance(4).Count(item => item.Kind == ItemEffectKind.Attack && item.Hit);
            Assert.That(hits, Is.EqualTo(4));
            Assert.That(master.EnemyWeapons.All(weapon => weapon.Stats.CriticalChancePercent == hits), Is.True);
        }

        [Test]
        public void EnemySkills_TransferBuffsAndScaleFromTheirResults()
        {
            CombatSimulation dryad = Fight(2, 12);
            dryad.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 30, CombatDamageKind.Other);
            dryad.ApplyStatus(CombatSide.Player, StatusEffectKind.Focus, 2);
            dryad.Advance(5);
            Assert.That(dryad.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.Zero);
            Assert.That(dryad.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Focus), Is.EqualTo(2));
            Assert.That(dryad.EnemyHealth, Is.EqualTo(dryad.EnemyMaxHealth * 1.06d - 30).Within(.0001d));

            CombatSimulation idol = Fight(4, 12);
            idol.Advance(5);
            var weapon = idol.EnemyWeapons.Single();
            Assert.That(weapon.Stats.AttackDamage, Is.EqualTo(weapon.BaseStats.AttackDamage * 1.06f).Within(.0001f));

            CombatSimulation commander = Fight(6, 12);
            commander.ApplyStatus(CombatSide.Player, StatusEffectKind.Precision, 4);
            commander.Advance(5);
            Assert.That(commander.GetStatusStacks(CombatSide.Player, StatusEffectKind.Precision), Is.EqualTo(1));
            Assert.That(commander.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Precision), Is.EqualTo(3));
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
            var changes = new EnemyItemStatOverrides(attackDamage: 0f, accuracyPercent: 30f, criticalChancePercent: 0f);
            CombatItemSnapshot item = Goblin.CreateCombatantSnapshot(new(null, new EnemyItemOverride("club", changes))).Loadout.Items.Single();
            Assert.That(item.BaseWeaponStats.AttackDamage, Is.Zero);
            Assert.That(item.BaseWeaponStats.AccuracyPercent, Is.EqualTo(30f));
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
