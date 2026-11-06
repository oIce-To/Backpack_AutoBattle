using System;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
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
    }
}
