using System;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Progression
{
    public sealed class ExperienceProgressionTests
    {
        [TestCase(1, 100L)] [TestCase(2, 125L)] [TestCase(3, 156L)]
        [TestCase(4, 195L)] [TestCase(5, 244L)] [TestCase(6, 305L)] [TestCase(10, 745L)]
        public void CostUsesUnroundedExponentialBase(int level, long expected)
        {
            var table = AssetDatabase.LoadAssetAtPath<ExperienceTableData>("Assets/_Project/Data/ExperienceTable_Default.asset");
            Assert.That(table.TryGetCost(level, out long cost), Is.True); Assert.That(cost, Is.EqualTo(expected));
        }

        [TestCase(1, 50, 10)] [TestCase(2, 55, 11)] [TestCase(3, 60, 12)]
        [TestCase(5, 73, 14)] [TestCase(11, 129, 25)]
        public void StatsUseBaseTimesCompoundGrowth_AndFloorOnlyOnce(int level, int health, int attack)
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Character_Default.asset");
            var stats = character.CaptureStats(level);
            Assert.That(stats.MaximumHealth, Is.EqualTo(health)); Assert.That(stats.Attack, Is.EqualTo(attack));
            Assert.That(character.CaptureStats().MaximumHealth, Is.EqualTo(50), "Never mutate the base asset.");
        }

        [TestCase(1, 10L)] [TestCase(4, 10L)] [TestCase(5, 25L)] [TestCase(8, 25L)]
        [TestCase(9, 50L)] [TestCase(12, 50L)]
        public void DefeatRewardUsesTerminalRoundBandAndFloorsFinalAmount(int round, long reward)
        {
            Assert.That(StageExperienceReward.Calculate(101, false, round), Is.EqualTo(reward));
            Assert.That(StageExperienceReward.Calculate(101, true, 12), Is.EqualTo(101));
        }

        [Test]
        public void SharedWalletSpendsOnOnlyTheChosenCharacter_AndRefusesInsufficientFundsOrOverflow()
        {
            var table = ScriptableObject.CreateInstance<ExperienceTableData>();
            try
            {
                var progress = new PlayerProgress();
                Assert.That(progress.TryLevelUp("allen", table), Is.False);
                progress.TryAddExperience(400);
                Assert.That(progress.TryLevelUp("allen", table), Is.True);
                Assert.That(progress.TryLevelUp("serena", table), Is.True);
                Assert.That(progress.TryLevelUp("allen", table), Is.True);
                Assert.That(progress.SharedExperience, Is.EqualTo(75));
                Assert.That(progress.GetCharacterLevel("allen"), Is.EqualTo(3)); Assert.That(progress.GetCharacterLevel("serena"), Is.EqualTo(2));
                Assert.That(progress.TryLevelUp("serena", table), Is.False); Assert.That(progress.SharedExperience, Is.EqualTo(75));
                Assert.That(progress.TryAddExperience(long.MaxValue), Is.False); Assert.That(progress.SharedExperience, Is.EqualTo(75));
                Assert.That(table.TryGetCost(int.MaxValue, out _), Is.False);
                Assert.That(table.TryGetCost(10000, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void InvalidVersionTwoEconomyIsRejected()
        {
            Assert.Throws<FormatException>(() => PlayerProgress.FromData(new ProfileSaveData { version = 2, sharedExperience = -1 }));
            Assert.Throws<FormatException>(() => PlayerProgress.FromData(new ProfileSaveData
            { version = 2, characters = new() { new() { characterId = "allen", level = 0 } } }));
        }
    }
}
