using System;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class RunLifecycleTests
    {
        [Test]
        public void NewRun_HasThreeLivesAndFreshProgress_WithCapturedCharacterStats()
        {
            var stats = new CharacterStatsSnapshot(100, 20);
            var run = new PrototypeRunState(20, "stage", stats, "Hero", stageName: "Stage");
            Assert.That(run.Lives, Is.EqualTo(3)); Assert.That(run.StartingLives, Is.EqualTo(3));
            Assert.That(run.Gold, Is.EqualTo(20)); Assert.That(run.Round, Is.EqualTo(1));
            Assert.That(run.PlayerStats, Is.SameAs(stats)); Assert.That(run.StageName, Is.EqualTo("Stage"));
            Assert.That(run.Phase, Is.EqualTo(PrototypePhase.Preparation)); Assert.That(run.Result, Is.Null);
            Assert.That(run.Victories + run.Defeats + run.Draws, Is.Zero);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(9)]
        public void ConfiguredStartingLives_AreIndependentOfHitPoints(int lives)
        {
            var run = new PrototypeRunState(20, stats: new CharacterStatsSnapshot(5000, 100), startingLives: lives);
            Assert.That(run.TryBeginBattle(), Is.True);
            Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 2), Is.True);
            Assert.That(run.Lives, Is.EqualTo(lives - 1));
            Assert.That(run.PlayerStats.MaximumHealth, Is.EqualTo(5000));
            Assert.That(run.Phase, Is.EqualTo(lives == 1 ? PrototypePhase.GameOver : PrototypePhase.Defeat));
        }

        [Test]
        public void Defeat_CostsExactlyOneLife_AndRetryKeepsRoundGoldAndCounters()
        {
            var run = new PrototypeRunState(17);
            Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 3), Is.False);
            Assert.That(run.TryBeginBattle(), Is.True); Assert.That(run.TryBeginBattle(), Is.False);
            Assert.That(run.TryCompleteBattle(CombatResult.Running, 12, 3), Is.False);
            Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 3), Is.True);
            Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 3), Is.False);
            Assert.That(run.Lives, Is.EqualTo(2)); Assert.That(run.LastLifeLoss, Is.EqualTo(1));
            Assert.That(run.Defeats, Is.EqualTo(1)); Assert.That(run.TotalBattleSeconds, Is.EqualTo(3));
            Assert.That(run.TryReturnToPreparation(), Is.True); Assert.That(run.TryReturnToPreparation(), Is.False);
            Assert.That(run.Round, Is.EqualTo(1)); Assert.That(run.Gold, Is.EqualTo(17));
            Assert.That(run.TryBeginBattle(), Is.True); Assert.That(run.LastLifeLoss, Is.Zero);
            Assert.That(run.TryCompleteBattle(CombatResult.Victory, 12, 2), Is.True);
            Assert.That(run.Lives, Is.EqualTo(2)); Assert.That(run.Result, Is.Null);
            Assert.That(run.Victories, Is.EqualTo(1)); Assert.That(run.TotalBattleSeconds, Is.EqualTo(5));
        }

        [Test]
        public void ThirdDefeat_FinishesOnce_AndCannotReturnToShopOrStartAnotherBattle()
        {
            var run = new PrototypeRunState(20, "stage", stageName: "Stage");
            for (int i = 0; i < 3; i++)
            {
                Assert.That(run.TryBeginBattle(), Is.True);
                Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 2), Is.True);
                if (i < 2) Assert.That(run.TryReturnToPreparation(), Is.True);
            }
            var result = run.Result;
            Assert.That(run.Phase, Is.EqualTo(PrototypePhase.GameOver)); Assert.That(run.Lives, Is.Zero);
            Assert.That(result.Outcome, Is.EqualTo(PrototypeRunOutcome.GameOver));
            Assert.That(result.Defeats, Is.EqualTo(3)); Assert.That(result.TotalBattleSeconds, Is.EqualTo(6));
            Assert.That(result.Round, Is.EqualTo(1)); Assert.That(result.RoundCount, Is.EqualTo(12));
            Assert.That(result.StageId, Is.EqualTo("stage")); Assert.That(result.StageName, Is.EqualTo("Stage"));
            Assert.That(result.Gold, Is.EqualTo(20)); Assert.That(result.LastBattleResult, Is.EqualTo(CombatResult.Defeat));
            Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 2), Is.False);
            Assert.That(run.TryReturnToPreparation(), Is.False); Assert.That(run.TryBeginBattle(), Is.False);
            Assert.That(run.Result, Is.SameAs(result)); Assert.That(run.Lives, Is.Zero);
        }

        [Test]
        public void FinalVictory_CapturesImmutableResult_WithoutChargingLifeOrGivingExtraIncome()
        {
            var run = new PrototypeRunState(35, "stage"); run.TryBeginBattle();
            Assert.That(run.TryCompleteBattle(CombatResult.Victory, 1, 1.75), Is.True);
            var result = run.Result;
            Assert.That(run.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(result.Outcome, Is.EqualTo(PrototypeRunOutcome.Cleared)); Assert.That(result.Lives, Is.EqualTo(3));
            Assert.That(result.Gold, Is.EqualTo(35)); Assert.That(result.Victories, Is.EqualTo(1));
            Assert.That(result.TotalBattleSeconds, Is.EqualTo(1.75));
            Assert.That(run.TryBeginBattle(), Is.False); Assert.That(run.TryReturnToPreparation(), Is.False);
            Assert.That(run.TryCompleteBattle(CombatResult.Victory, 1, 1.75), Is.False);
            _ = new PrototypeRunState(20, "stage");
            Assert.That(result.Gold, Is.EqualTo(35)); Assert.That(result.Victories, Is.EqualTo(1));
        }

        [Test]
        public void ExplicitSimulationDraw_RemainsSupportedButDoesNotCostALife()
        {
            var run = new PrototypeRunState(20); run.TryBeginBattle();
            Assert.That(run.TryCompleteBattle(CombatResult.Draw, 12, 3), Is.True);
            Assert.That(run.Lives, Is.EqualTo(3)); Assert.That(run.LastLifeLoss, Is.Zero);
            Assert.That(run.Draws, Is.EqualTo(1)); Assert.That(run.Result, Is.Null);
            Assert.That(run.TryReturnToPreparation(), Is.True);
        }

        [Test]
        public void DefaultCombatForcesDefeatAt90_WhileEarlierExplicitTestDeadlineStillWorks()
        {
            var unit = new CombatantSnapshot("Idle", 50, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>()));
            var unlimited = new CombatSimulation(unit, unit);
            unlimited.Advance(121); unlimited.Advance(3600);
            Assert.That(unlimited.Result, Is.EqualTo(CombatResult.Defeat));
            Assert.That(unlimited.IsForcedDefeat, Is.True);
            Assert.That(unlimited.ElapsedSeconds, Is.EqualTo(90));
            Assert.That(unlimited.PlayerHealth, Is.EqualTo(50));
            var limited = new CombatSimulation(unit, unit, maximumDuration: 3);
            limited.Advance(100);
            Assert.That(limited.Result, Is.EqualTo(CombatResult.Draw)); Assert.That(limited.ElapsedSeconds, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatSimulation(unit, unit, maximumDuration: double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatSimulation(unit, unit, maximumDuration: double.NegativeInfinity));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidStartingLives_AreRejected(int lives)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new PrototypeRunState(20, startingLives: lives));

        [Test]
        public void InvalidFinishInput_DoesNotPartiallySpendLifeOrAccumulateResults()
        {
            var run = new PrototypeRunState(20); run.TryBeginBattle();
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TryCompleteBattle(CombatResult.Defeat, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TryCompleteBattle(CombatResult.Defeat, 12, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TryCompleteBattle((CombatResult)99, 12, 2));
            Assert.That(run.Lives, Is.EqualTo(3)); Assert.That(run.Defeats, Is.Zero); Assert.That(run.TotalBattleSeconds, Is.Zero);
            Assert.That(run.LastBattleResult, Is.Null); Assert.That(run.Phase, Is.EqualTo(PrototypePhase.Battle));
        }
    }
}
