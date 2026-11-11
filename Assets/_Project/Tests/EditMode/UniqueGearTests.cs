using System;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Stages;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Items
{
    public sealed class UniqueGearTests
    {
        private const string Data = "Assets/_Project/Data/";
        private static UniqueGearData Gear(string name) => AssetDatabase.LoadAssetAtPath<UniqueGearData>($"{Data}Items/UniqueGear_{name}.asset");

        [Test]
        public void SharedInventory_MigratesAndTransfersInstances_WithoutReissuingStarterGear()
        {
            var allen = AssetDatabase.LoadAssetAtPath<CharacterData>(Data + "Character_Default.asset");
            var serena = AssetDatabase.LoadAssetAtPath<CharacterData>(Data + "Character_Serena.asset");
            var profile = PlayerProgress.FromData(new ProfileSaveData { version = 2, selectedCharacterId = "serena", sharedExperience = 125 });
            Assert.That(profile.AcquireCharacter(allen), Is.True);
            Assert.That(profile.AcquireCharacter(serena), Is.True);
            var sword = profile.GetEquippedGear("allen", UniqueGearSlot.Weapon);
            var bow = profile.GetEquippedGear("serena", UniqueGearSlot.Weapon);
            Assert.That(sword.GearId, Is.EqualTo("knight_sword"));
            Assert.That(bow.GearId, Is.EqualTo("silver_longbow"));
            Assert.That(profile.TryEquipGear("serena", UniqueGearSlot.Weapon, sword.InstanceId, allen.StarterGear), Is.False);
            Assert.That(profile.TryEquipGear("serena", UniqueGearSlot.Weapon, sword.InstanceId, allen.StarterGear, "serena"), Is.False);
            Assert.That(profile.TryEquipGear("serena", UniqueGearSlot.Armor, sword.InstanceId, allen.StarterGear, "allen"), Is.False);
            Assert.That(profile.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(bow.InstanceId));
            Assert.That(profile.TryEquipGear("serena", UniqueGearSlot.Weapon, sword.InstanceId, allen.StarterGear, "allen"), Is.True);

            profile = PlayerProgress.FromData(profile.Export());
            Assert.That(profile.AcquireCharacter(allen), Is.False);
            Assert.That(profile.AcquireCharacter(serena), Is.False);
            Assert.That(profile.UniqueGear, Has.Count.EqualTo(2));
            Assert.That(profile.GetEquippedGear("allen", UniqueGearSlot.Weapon), Is.Null);
            Assert.That(profile.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(sword.InstanceId));
            Assert.That(profile.UniqueGear.Single(item => item.InstanceId == bow.InstanceId).EquippedBy, Is.Empty);
            Assert.That(profile.SharedExperience, Is.EqualTo(125));
            Assert.That(profile.SelectedCharacterId, Is.EqualTo("serena"));
        }

        [Test]
        public void RankCaps_AndGradeBonuses_AreIndependentOfWeaponBaseStats()
        {
            var sword = Gear("KnightSword");
            var bow = Gear("SilverLongbow");
            var profile = new PlayerProgress();
            Assert.That(profile.AddGear(sword, ItemRarity.Common, 4), Is.Null);
            Assert.That(profile.AddGear(bow, ItemRarity.Uncommon, 7), Is.Null);
            var enhanced = profile.AddGear(sword, ItemRarity.Uncommon, 6);
            Assert.That(PlayerProgress.FromData(profile.Export()).UniqueGear.Single().Enhancement, Is.EqualTo(6));
            Assert.That(UniqueGearData.BonusPercent(ItemRarity.Common, 0), Is.EqualTo(5));
            Assert.That(UniqueGearData.BonusPercent(ItemRarity.Common, 3), Is.EqualTo(6.5));
            Assert.That(UniqueGearData.BonusPercent(ItemRarity.Uncommon, 0), Is.EqualTo(6.5));
            Assert.That(UniqueGearData.BonusPercent(enhanced.Rarity, enhanced.Enhancement), Is.EqualTo(9.5));
            Assert.That(Snapshot("common", sword, 0, ItemRarity.Common).BaseWeaponStats.AttackDamage,
                Is.EqualTo(Snapshot("uncommon", sword, 0, enhanced.Rarity).BaseWeaponStats.AttackDamage));
            Assert.That(sword.Item.ExcludeFromShop && bow.Item.ExcludeFromShop, Is.True);
        }

        [TestCase(ItemRarity.Common, 0.5, 5)]
        [TestCase(ItemRarity.Uncommon, 0.7, 7)]
        public void BattleGear_ProcsFromMatchingWeapons_AndTracksFocusDynamically(ItemRarity rarity, double armorPerHit, double critBonus)
        {
            var sword = Gear("KnightSword");
            var bow = Gear("SilverLongbow");
            var otherMelee = AssetDatabase.LoadAssetAtPath<ItemData>(Data + "Items/Item_TrainingBlade.asset");
            var otherRanged = AssetDatabase.LoadAssetAtPath<ItemData>(Data + "Items/Item_ThrowingAxes.asset");
            var items = new[]
            {
                Snapshot("sword", sword, 0, rarity), Snapshot("bow", bow, 1, rarity),
                new CombatItemSnapshot("melee", otherMelee, new Vector2Int(2, 0), ItemRotation.None, otherMelee.OccupiedCells),
                new CombatItemSnapshot("ranged", otherRanged, new Vector2Int(3, 0), ItemRotation.None, otherRanged.OccupiedCells)
            };
            var simulation = new CombatSimulation(new CombatantSnapshot("player", 50, new CombatLoadoutSnapshot(items)),
                new CombatantSnapshot("enemy", 100, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>())), new AlwaysHitRandom());
            simulation.ApplyStatus(CombatSide.Player, StatusEffectKind.Focus, 10);
            foreach (var weapon in simulation.PlayerWeapons)
            {
                bool ranged = (weapon.Snapshot.Tags & ItemTags.Ranged) != 0;
                Assert.That(weapon.Stats.CriticalChancePercent,
                    Is.EqualTo(weapon.BaseStats.CriticalChancePercent + (ranged ? critBonus : 0)).Within(.00001));
            }
            var events = simulation.Advance(3);
            var armorEvents = events.Where(e => e.Kind == ItemEffectKind.GainArmor).ToArray();
            Assert.That(armorEvents, Has.Length.EqualTo(2));
            Assert.That(armorEvents.All(e => e.InstanceId == "sword"), Is.True);
            Assert.That(simulation.PlayerArmor, Is.EqualTo(armorPerHit * 2).Within(.00001));
            simulation.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, .25, CombatDamageKind.Other);
            Assert.That(simulation.CaptureStatistics().Items.Single(item => item.InstanceId == "sword").DamageBlocked, Is.EqualTo(.25));
        }

        private static CombatItemSnapshot Snapshot(string id, UniqueGearData definition, int column, ItemRarity rarity)
            => new(id, definition.Item, new Vector2Int(column, 0), ItemRotation.None, definition.Item.OccupiedCells,
                rarity: rarity, gearEffect: definition.EffectFor(rarity));

        private sealed class AlwaysHitRandom : System.Random
        {
            public override double NextDouble() => .05;
        }

        [Test]
        public void ExpandedCatalog_ProvidesFiveCraftableUniqueItems_WithRankSpecificBonuses()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AdventureCatalogData>(Data + "AdventureCatalog.asset");
            var twilight = Gear("SearingTwilight");
            var cuirass = Gear("StoneGiantCuirass");
            foreach (var gear in new[] { twilight, cuirass, Gear("SylphVest"), Gear("BloodKnightHelm"), Gear("ThornShellPauldron") })
            {
                Assert.That(catalog.UniqueGear, Does.Contain(gear));
                Assert.That(gear.FitsSlot && gear.Item.IsUniqueGear && gear.Item.ExcludeFromShop, Is.True);
                Assert.That(gear.Item.InventorySprite, Is.Not.Null, gear.GearId);
                Assert.That(gear.Icon, Is.Not.Null, gear.GearId);
                Assert.That(gear.Icon, Is.Not.SameAs(gear.Item.InventorySprite), gear.GearId);
                Assert.That(gear.BonusPercentFor(ItemRarity.Common, 3), Is.EqualTo(gear.BonusPercentFor(ItemRarity.Uncommon, 0)));
            }
            Assert.That(twilight.BonusCriticalChance, Is.True);
            Assert.That(twilight.BonusPercentFor(ItemRarity.Uncommon, 6), Is.EqualTo(9.5));
            Assert.That(cuirass.BonusPercentFor(ItemRarity.Uncommon, 6), Is.EqualTo(19));
            var stats = twilight.Item.ResolveWeaponStats(new CharacterStatsSnapshot(50, 10, criticalChanceBonusPercent: 5));
            Assert.That(stats.CriticalChancePercent, Is.EqualTo(20));
        }

        [Test]
        public void TwilightPlacement_RotatesBothDiamonds_AndOnlyLinksMeleeAttacks()
        {
            var twilight = Gear("SearingTwilight");
            var source = Item("twilight", twilight.Item, new Vector2Int(2, 2), ItemRotation.Clockwise90,
                twilight.EffectFor(ItemRarity.Common));
            var melee = Item("melee", DataItem("IronDagger"), new Vector2Int(2, 0));
            var ranged = Item("ranged", DataItem("ThrowingAxes"), new Vector2Int(2, 3));
            var unrelated = Item("other", DataItem("IronDagger"), new Vector2Int(5, 0));
            var loadout = new CombatLoadoutSnapshot(new[] { source, melee, ranged, unrelated });
            var cells = PlacementEffectResolver.GetCells(loadout, source);
            Assert.That(cells.Select(cell => cell.Cell), Is.EquivalentTo(new[] { new Vector2Int(2, 1), new Vector2Int(2, 3) }));
            Assert.That(cells.Where(cell => cell.ReceivesEffect).Select(cell => cell.TargetInstanceId), Is.EqualTo(new[] { "melee" }));
            Assert.That(PlacementEffectResolver.GetAttackBonuses(loadout), Is.Empty);
            var combat = Battle(loadout.Items);
            var events = combat.Advance(3);
            int linkedAttacks = events.Count(e => e.Kind == ItemEffectKind.Attack && (e.InstanceId == "melee" || e.InstanceId == "twilight"));
            Assert.That(events.Count(e => e.StatusKind == StatusEffectKind.Burn && e.StatusStacks == 2 && e.InstanceId == "twilight"),
                Is.EqualTo(linkedAttacks));
            var tooltip = ItemTooltipContent.FromCombat(source, null);
            Assert.That(tooltip.GetDescriptionText(), Does.Contain(ItemTooltipContent.PlacementIconTag));
            Assert.That(tooltip.GetDescriptionText(), Does.Not.Contain("감소"));
        }

        [Test]
        public void TwilightBurst_FiresOnFifthAttackAttempt_DecaysEachTick_AndKeepsNormalClock()
        {
            var gear = Gear("SearingTwilight");
            var combat = Battle(new[] { Snapshot("twilight", gear, 0, ItemRarity.Uncommon) }, random: new MissAndProcRandom());
            var ticks = new System.Collections.Generic.List<PeriodicStatusTickEvent>();
            combat.StatusTicked += ticks.Add;
            combat.Advance(.25);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 1, CombatSide.Player, "twilight");
            var events = combat.Advance(4.75);
            Assert.That(events.Where(e => e.Kind == ItemEffectKind.Attack).All(e => !e.Hit), Is.True);
            var burst = ticks.Where(tick => tick.Time == 5).ToArray();
            Assert.That(burst.Select(tick => tick.Stacks), Is.EqualTo(new long[] { 7, 6, 5 }));
            Assert.That(burst[0].Damage, Is.EqualTo(7 * 1.2 * 1.14).Within(.00001));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(4));
            combat.Advance(.25);
            Assert.That(ticks.Last().Time, Is.EqualTo(5.25));
            Assert.That(ticks.Last().Stacks, Is.EqualTo(4));
            Assert.That(combat.CaptureStatistics().Items.Single(item => item.InstanceId == "twilight").BurnDamage,
                Is.EqualTo(ticks.Sum(tick => tick.Damage)).Within(.00001));
        }

        [TestCase(ItemRarity.Common, 5d, 10d)]
        [TestCase(ItemRarity.Uncommon, 7d, 14d)]
        public void ArmorCycles_RespectInclusiveCuirassAndExclusiveHelmetThresholds(ItemRarity rarity, double cuirassArmor, double helmArmor)
        {
            var combat = Battle(new[] { Snapshot("cuirass", Gear("StoneGiantCuirass"), 0, rarity) },
                new[] { Snapshot("helm", Gear("BloodKnightHelm"), 0, rarity) });
            combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 50, CombatDamageKind.Other);
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 90, CombatDamageKind.Other);
            combat.Advance(2);
            Assert.That(combat.PlayerArmor, Is.EqualTo(cuirassArmor).Within(.00001));
            Assert.That(combat.EnemyArmor, Is.EqualTo(helmArmor).Within(.00001));
            Assert.That(combat.EnemyHealth, Is.EqualTo(5));
            combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, cuirassArmor + 1, CombatDamageKind.Other);
            combat.Advance(2);
            Assert.That(combat.PlayerArmor, Is.Zero);
            Assert.That(combat.EnemyArmor, Is.EqualTo(helmArmor).Within(.00001));
            Assert.That(combat.EnemyHealth, Is.EqualTo(5));
        }

        [Test]
        public void SylphVest_CleansesOneDebuffWithItsDamageOrigins_ThenAmplifiesHaste()
        {
            var combat = Battle(new[] { Snapshot("vest", Gear("SylphVest"), 0, ItemRarity.Common) });
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Poison, 5, CombatSide.Enemy);
            var events = combat.Advance(2);
            Assert.That(events.Single().StatusStacks, Is.EqualTo(-3));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Poison), Is.EqualTo(2));
            Assert.That(combat.GetStatusDamagePerTick(CombatSide.Player, StatusEffectKind.Poison), Is.EqualTo(2).Within(.00001));
            combat.Advance(4);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Poison), Is.Zero);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(1));
            Assert.That(combat.GetStatusPercent(CombatSide.Player, StatusEffectKind.Haste), Is.EqualTo(1.1).Within(.00001));
            Assert.That(combat.PlayerItems.Single().Cycle.IntervalSeconds, Is.EqualTo(2 / 1.011).Within(.00001));
        }

        [Test]
        public void Pauldron_GainedThornsReflectOnTheNextAttack_AndCreditTheGear()
        {
            var armor = Gear("ThornShellPauldron");
            var combat = Battle(new[] { Snapshot("pauldron", armor, 0, ItemRarity.Uncommon) },
                new[] { Item("dagger", DataItem("IronDagger"), Vector2Int.zero) });
            combat.Advance(1);
            Assert.That(combat.EnemyHealth, Is.EqualTo(100));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Thorns), Is.EqualTo(1));
            combat.Advance(1);
            Assert.That(combat.EnemyHealth, Is.EqualTo(98.86).Within(.00001));
            Assert.That(combat.CaptureStatistics().Items.Single(item => item.InstanceId == "pauldron").ThornsDamage,
                Is.EqualTo(1.14).Within(.00001));
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Player, 1, CombatDamageKind.Other);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Thorns), Is.EqualTo(2));
        }

        private static ItemData DataItem(string name) => AssetDatabase.LoadAssetAtPath<ItemData>($"{Data}Items/Item_{name}.asset");

        private static CombatItemSnapshot Item(string id, ItemData item, Vector2Int origin, ItemRotation rotation = ItemRotation.None,
            UniqueGearEffect gear = default)
            => new(id, item, origin, rotation, item.CreateShape().GetCells(rotation).Select(cell => cell + origin), gearEffect: gear);

        private static CombatSimulation Battle(System.Collections.Generic.IEnumerable<CombatItemSnapshot> player,
            System.Collections.Generic.IEnumerable<CombatItemSnapshot> enemy = null, System.Random random = null)
            => new(new CombatantSnapshot("player", 100, new CombatLoadoutSnapshot(player)),
                new CombatantSnapshot("enemy", 100, new CombatLoadoutSnapshot(enemy ?? Array.Empty<CombatItemSnapshot>())),
                random ?? new AlwaysHitRandom());

        private sealed class MissAndProcRandom : System.Random
        {
            private int _rolls;
            public override double NextDouble() => _rolls++ % 2 == 0 ? .99 : .49;
        }
    }
}
