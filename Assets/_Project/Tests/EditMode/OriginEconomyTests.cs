using System;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using BackpackAutoBattle.Stages;
using NUnit.Framework;
using UnityEditor;

namespace BackpackAutoBattle.Tests.Progression
{
    public sealed class OriginEconomyTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);
        private static UniqueGearData Sword => AssetDatabase.LoadAssetAtPath<UniqueGearData>("Assets/_Project/Data/Items/UniqueGear_KnightSword.asset");

        [Test]
        public void Tickets_RechargeOfflineOnExactBoundaries_PreservePartialTime_AndStopAtCapacity()
        {
            var profile = new PlayerProgress();
            Assert.That(profile.MineTickets, Is.EqualTo(5));
            Assert.That(profile.TryClaimReward("clear1", new StageReward(0, 500), true, Now), Is.True);
            Assert.That(profile.MineTickets, Is.EqualTo(4));
            profile = PlayerProgress.FromData(profile.Export());
            Assert.That(profile.UntilNextMineTicket(Now.AddHours(3)), Is.EqualTo(TimeSpan.FromHours(5)));
            Assert.That(profile.TryClaimReward("clear2", new StageReward(0, 500), true, Now.AddHours(3)), Is.True);
            Assert.That(profile.UntilNextMineTicket(Now.AddHours(7)), Is.EqualTo(TimeSpan.FromHours(1)));
            profile.RefreshMineTickets(Now.AddHours(8));
            Assert.That(profile.MineTickets, Is.EqualTo(4));
            Assert.That(profile.UntilNextMineTicket(Now.AddHours(9)), Is.EqualTo(TimeSpan.FromHours(7)));
            profile.RefreshMineTickets(Now.AddDays(30));
            Assert.That(profile.MineTickets, Is.EqualTo(5));
            Assert.That(profile.TryClaimReward("clear3", new StageReward(0, 0), true, Now.AddDays(30)), Is.True);
            Assert.That(profile.UntilNextMineTicket(Now.AddDays(30)), Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(profile.UntilNextMineTicket(Now.AddHours(1)), Is.EqualTo(TimeSpan.FromHours(8)), "Clock rollback cannot grant or restart tickets.");
        }

        [Test]
        public void Rewards_ConsumeExactlyOneTicket_AndCannotOverflowOrDuplicateAfterReload()
        {
            var profile = new PlayerProgress();
            var reward = new StageReward(0, 500, new[] { "knight_sword", "silver_longbow", "knight_sword" });
            Assert.That(profile.TryClaimReward("mine-clear", reward, true, Now), Is.True);
            profile = PlayerProgress.FromData(profile.Export());
            Assert.That(profile.TryClaimReward("mine-clear", reward, true, Now), Is.True);
            Assert.That(profile.MineTickets, Is.EqualTo(4));
            Assert.That(profile.OriginShards, Is.EqualTo(500));
            Assert.That(profile.GetBlueprints("knight_sword"), Is.EqualTo(2));
            Assert.That(profile.GetBlueprints("silver_longbow"), Is.EqualTo(1));
            for (int i = 0; i < 4; i++) Assert.That(profile.TryClaimReward("sweep" + i, reward, true, Now), Is.True);
            Assert.That(profile.TryClaimReward("no-ticket", reward, true, Now), Is.False);
            Assert.That(profile.OriginShards, Is.EqualTo(2500));
            Assert.That(profile.TryClaimReward("overflow", new StageReward(0, long.MaxValue), false, Now), Is.False);
            Assert.That(profile.OriginShards, Is.EqualTo(2500));
        }

        [Test]
        public void CraftEnhancePromote_DebitExactCosts_KeepIdentityAndOwner_AndRejectEquippedOrEnhancedMaterials()
        {
            var profile = new PlayerProgress();
            var allen = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Character_Default.asset");
            profile.AcquireCharacter(allen);
            var center = profile.GetEquippedGear("allen", UniqueGearSlot.Weapon);
            Assert.That(profile.TryClaimReward("seed", new StageReward(0, 100000, Enumerable.Repeat(Sword.GearId, 30)), false, Now), Is.True);
            Assert.That(profile.TryCraftGear(Sword), Is.Not.Null);
            var material = profile.TryCraftGear(Sword);
            Assert.That(material, Is.Not.Null);
            Assert.That(profile.OriginShards, Is.EqualTo(99000));
            Assert.That(profile.GetBlueprints(Sword.GearId), Is.EqualTo(10));
            foreach (long cost in new long[] { 500, 700, 1000 })
            {
                var item = profile.UniqueGear.Single(gear => gear.InstanceId == center.InstanceId);
                Assert.That(PlayerProgress.EnhancementCost(item), Is.EqualTo(cost));
                Assert.That(profile.TryEnhanceGear(center.InstanceId), Is.True);
            }
            Assert.That(profile.TryEnhanceGear(center.InstanceId), Is.False);
            var materials = profile.PromotionMaterials(center.InstanceId).Select(gear => gear.InstanceId).ToArray();
            Assert.That(profile.TryPromoteGear(center.InstanceId, new[] { materials[0], materials[0] }), Is.False);
            Assert.That(profile.TryPromoteGear(center.InstanceId, new[] { center.InstanceId, materials[0] }), Is.False);
            Assert.That(profile.TryPromoteGear(center.InstanceId, materials), Is.True);
            var promoted = profile.UniqueGear.Single();
            Assert.That(promoted.InstanceId, Is.EqualTo(center.InstanceId));
            Assert.That(promoted.EquippedBy, Is.EqualTo("allen"));
            Assert.That(promoted.Rarity, Is.EqualTo(ItemRarity.Uncommon));
            Assert.That(promoted.Enhancement, Is.Zero);
            Assert.That(UniqueGearData.BonusPercent(promoted.Rarity, promoted.Enhancement), Is.EqualTo(6.5));
            foreach (long cost in new long[] { 2000, 2500, 3200, 4000, 5000, 6500 })
            {
                promoted = profile.UniqueGear.Single();
                Assert.That(PlayerProgress.EnhancementCost(promoted), Is.EqualTo(cost));
                Assert.That(profile.TryEnhanceGear(promoted.InstanceId), Is.True);
            }
            Assert.That(profile.UniqueGear.Single().Enhancement, Is.EqualTo(6));
            Assert.That(profile.TryEnhanceGear(center.InstanceId), Is.False);
            Assert.That(profile.OriginShards, Is.EqualTo(73600));
        }

        [Test]
        public void LegacyProfiles_StartWithFullTicketsWithoutChangingExistingProgress()
        {
            var profile = PlayerProgress.FromData(new ProfileSaveData { version = 3, sharedExperience = 123 });
            Assert.That(profile.MineTickets, Is.EqualTo(5));
            Assert.That(profile.SharedExperience, Is.EqualTo(123));
            Assert.That(profile.OriginShards, Is.Zero);
            Assert.That(PlayerProgress.FromData(profile.Export()).MineTickets, Is.EqualTo(5));
        }

        [Test]
        public void MineAssets_CopyNormalRounds_HaveIndependentIdsAndAgreedRewards()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AdventureCatalogData>("Assets/_Project/Data/AdventureCatalog.asset");
            Assert.That(catalog.OriginMines.Count, Is.EqualTo(5));
            int[] normalRewards = { 100, 126, 159, 200, 252 }, mineRewards = { 500, 630, 794, 1000, 1260 };
            for (int i = 0; i < 5; i++)
            {
                var stage = catalog.Stages[i]; var mine = catalog.OriginMines[i];
                Assert.That(mine.GetValidationErrors(), Is.Empty);
                Assert.That(mine.IsOriginMine, Is.True);
                Assert.That(mine.StageId, Is.Not.EqualTo(stage.StageId));
                Assert.That(stage.ClearOriginShards, Is.EqualTo(normalRewards[i]));
                Assert.That(mine.ClearOriginShards, Is.EqualTo(mineRewards[i]));
                Assert.That(mine.ClearBlueprints, Is.EqualTo(i + 5));
                Assert.That(mine.ClearExperience, Is.Zero);
                for (int round = 1; round <= 12; round++)
                    Assert.That(mine.GetRound(round).CreateEnemySnapshot().MaximumHealth,
                        Is.EqualTo(stage.GetRound(round).CreateEnemySnapshot().MaximumHealth));
            }
        }
    }
}
