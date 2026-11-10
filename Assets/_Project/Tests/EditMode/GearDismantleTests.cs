using System;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Progression
{
    public sealed class GearDismantleTests
    {
        private static UniqueGearData Sword => AssetDatabase.LoadAssetAtPath<UniqueGearData>("Assets/_Project/Data/Items/UniqueGear_KnightSword.asset");
        private static UniqueGearData Bow => AssetDatabase.LoadAssetAtPath<UniqueGearData>("Assets/_Project/Data/Items/UniqueGear_SilverLongbow.asset");
        private static CharacterData Allen => AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Character_Default.asset");
        private static CharacterData Serena => AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Character_Serena.asset");

        [TestCase(ItemRarity.Common, 0, 250, 8)]
        [TestCase(ItemRarity.Common, 1, 500, 8)]
        [TestCase(ItemRarity.Common, 2, 850, 8)]
        [TestCase(ItemRarity.Common, 3, 1350, 8)]
        [TestCase(ItemRarity.Uncommon, 0, 1850, 24)]
        [TestCase(ItemRarity.Uncommon, 1, 2850, 24)]
        [TestCase(ItemRarity.Uncommon, 2, 4100, 24)]
        [TestCase(ItemRarity.Uncommon, 3, 5700, 24)]
        [TestCase(ItemRarity.Uncommon, 4, 7700, 24)]
        [TestCase(ItemRarity.Uncommon, 5, 10200, 24)]
        [TestCase(ItemRarity.Uncommon, 6, 13450, 24)]
        public void ReturnsHalfOfRecursiveShardInvestment_AndEightyPercentOfBlueprints(ItemRarity rarity, int enhancement, long shards, long blueprints)
        {
            var profile = new PlayerProgress();
            var gear = profile.AddGear(Sword, rarity, enhancement);
            var value = PlayerProgress.DismantleValue(gear);
            Assert.That(value.OriginShards, Is.EqualTo(shards));
            Assert.That(value.Blueprints, Is.EqualTo(blueprints));
            Assert.That(profile.TryDismantleGear(new[] { gear.InstanceId }), Is.True);
            Assert.That(profile.OriginShards, Is.EqualTo(shards));
            Assert.That(profile.GetBlueprints(Sword.GearId), Is.EqualTo(blueprints));
            Assert.That(profile.UniqueGear, Is.Empty);
        }

        [Test]
        public void BatchDismantle_GroupsBlueprintsByGearType_AndCannotBeReplayed()
        {
            var profile = new PlayerProgress();
            string a = profile.AddGear(Sword, ItemRarity.Common, 2).InstanceId;
            string b = profile.AddGear(Sword, ItemRarity.Common, 0).InstanceId;
            string c = profile.AddGear(Bow, ItemRarity.Uncommon, 0).InstanceId;
            Assert.That(profile.TryDismantleGear(new[] { a, b, c }), Is.True);
            Assert.That(profile.OriginShards, Is.EqualTo(2950));
            Assert.That(profile.GetBlueprints(Sword.GearId), Is.EqualTo(16));
            Assert.That(profile.GetBlueprints(Bow.GearId), Is.EqualTo(24));
            Assert.That(profile.TryDismantleGear(new[] { a, b, c }), Is.False);
            Assert.That(profile.OriginShards, Is.EqualTo(2950));
        }

        [Test]
        public void InvalidOrProtectedSelection_IsRejectedAsAWhole()
        {
            var profile = new PlayerProgress(); profile.AcquireCharacter(Allen); profile.AcquireCharacter(Serena);
            string free = profile.AddGear(Sword, ItemRarity.Common, 2).InstanceId;
            string locked = profile.AddGear(Bow, ItemRarity.Common, 0).InstanceId;
            profile.TrySetGearLocked(locked, true);
            string allen = profile.GetEquippedGear("allen", UniqueGearSlot.Weapon).InstanceId;
            string serena = profile.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId;
            string before = JsonUtility.ToJson(profile.Export());
            foreach (var ids in new[] { new[] { free, free }, new[] { free, "missing" }, new[] { free, locked },
                new[] { free, allen }, new[] { free, serena }, new string[] { free, null }, Array.Empty<string>() })
            {
                Assert.That(profile.TryDismantleGear(ids), Is.False);
                Assert.That(JsonUtility.ToJson(profile.Export()), Is.EqualTo(before));
            }
            Assert.That(profile.TryDismantleGear(null), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RewardOverflow_DoesNotDeleteEquipmentOrPayPartialRewards(bool overflowShards)
        {
            var profile = new PlayerProgress();
            string id = profile.AddGear(Sword, ItemRarity.Common, 0).InstanceId;
            var data = profile.Export();
            if (overflowShards) data.originShards = long.MaxValue;
            else data.blueprints.Add(new BlueprintBalanceData { gearId = Sword.GearId, count = long.MaxValue });
            profile = PlayerProgress.FromData(data);
            string before = JsonUtility.ToJson(profile.Export());
            Assert.That(profile.TryDismantleGear(new[] { id }), Is.False);
            Assert.That(JsonUtility.ToJson(profile.Export()), Is.EqualTo(before));
        }

        [Test]
        public void Lock_PersistsThroughOwnershipEnhancementAndPromotion_AndBlocksPromotionMaterials()
        {
            var profile = new PlayerProgress(); profile.AcquireCharacter(Allen); profile.AcquireCharacter(Serena);
            var center = profile.GetEquippedGear("allen", UniqueGearSlot.Weapon);
            profile.TrySetGearLocked(center.InstanceId, true);
            profile.TryClaimReward("seed", new StageReward(0, 100000), false, DateTimeOffset.UtcNow);
            for (int i = 0; i < 3; i++) Assert.That(profile.TryEnhanceGear(center.InstanceId), Is.True);
            string a = profile.AddGear(Sword, ItemRarity.Common, 0).InstanceId;
            string b = profile.AddGear(Sword, ItemRarity.Common, 0).InstanceId;
            profile.TrySetGearLocked(a, true);
            Assert.That(profile.PromotionMaterials(center.InstanceId).Select(item => item.InstanceId), Is.EquivalentTo(new[] { b }));
            Assert.That(profile.TryPromoteGear(center.InstanceId, new[] { a, b }), Is.False);
            profile.TrySetGearLocked(a, false);
            Assert.That(profile.TryPromoteGear(center.InstanceId, new[] { a, b }), Is.True);
            Assert.That(profile.GetEquippedGear("allen", UniqueGearSlot.Weapon).IsLocked, Is.True);
            Assert.That(profile.TryUnequipGear("allen", UniqueGearSlot.Weapon), Is.True);
            Assert.That(profile.TryEquipGear("serena", UniqueGearSlot.Weapon, center.InstanceId, Sword), Is.True);
            profile = PlayerProgress.FromData(JsonUtility.FromJson<ProfileSaveData>(JsonUtility.ToJson(profile.Export())));
            var gear = profile.GetEquippedGear("serena", UniqueGearSlot.Weapon);
            Assert.That(gear.IsLocked, Is.True); Assert.That(gear.Rarity, Is.EqualTo(ItemRarity.Uncommon));
            Assert.That(profile.TrySetGearLocked(gear.InstanceId, false), Is.True);
            Assert.That(profile.GetEquippedGear("serena", UniqueGearSlot.Weapon).IsLocked, Is.False);
        }

        [Test]
        public void VersionFourGear_MigratesUnlockedWithoutChangingOtherProgress()
        {
            var profile = new PlayerProgress(); profile.AcquireCharacter(Allen);
            profile.TryClaimReward("seed", new StageReward(123, 456), false, DateTimeOffset.UtcNow);
            var data = profile.Export(); data.version = 4;
            profile = PlayerProgress.FromData(data);
            Assert.That(profile.UniqueGear.Single().IsLocked, Is.False);
            Assert.That(profile.SharedExperience, Is.EqualTo(123)); Assert.That(profile.OriginShards, Is.EqualTo(456));
            Assert.That(profile.GetEquippedGear("allen", UniqueGearSlot.Weapon), Is.Not.Null);
            Assert.That(profile.Export().version, Is.EqualTo(5));
        }
    }
}
