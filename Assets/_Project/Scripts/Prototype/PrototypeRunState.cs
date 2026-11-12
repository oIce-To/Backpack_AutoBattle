using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;

namespace BackpackAutoBattle.Prototype
{
    public enum PrototypePhase { Preparation, Battle, Victory, Defeat, Draw, Completed, Fusion, GameOver, MainMenu }

    public enum PrototypeRunOutcome { Cleared, GameOver }

    /// <summary>Immutable handoff to a result view or future progression/main-screen coordinator.</summary>
    public sealed class PrototypeRunResult
    {
        internal PrototypeRunResult(PrototypeRunState run, int roundCount, PrototypeRunOutcome outcome)
        {
            Outcome = outcome; StageId = run.StageId; StageName = run.StageName;
            Round = run.Round; RoundCount = roundCount; Lives = run.Lives; StartingLives = run.StartingLives;
            Gold = run.Gold; Victories = run.Victories; Defeats = run.Defeats; Draws = run.Draws;
            TotalBattleSeconds = run.TotalBattleSeconds; LastBattleResult = run.LastBattleResult.Value;
            ExperienceReward = StageExperienceReward.Calculate(run.ClearExperience, outcome == PrototypeRunOutcome.Cleared, Round);
            OriginShardReward = run.IsOriginMine && outcome != PrototypeRunOutcome.Cleared ? 0
                : StageExperienceReward.Calculate(run.ClearOriginShards, outcome == PrototypeRunOutcome.Cleared, Round);
            BlueprintRewardCount = run.IsOriginMine && outcome == PrototypeRunOutcome.Cleared ? run.ClearBlueprints : 0;
        }
        public PrototypeRunOutcome Outcome { get; }
        public string StageId { get; }
        public string StageName { get; }
        public int Round { get; }
        public int RoundCount { get; }
        public int Lives { get; }
        public int StartingLives { get; }
        public int Gold { get; }
        public int Victories { get; }
        public int Defeats { get; }
        public int Draws { get; }
        public double TotalBattleSeconds { get; }
        public CombatResult LastBattleResult { get; }
        public long ExperienceReward { get; }
        public long OriginShardReward { get; }
        public int BlueprintRewardCount { get; }
    }

    /// <summary>Run progress lives independently of both screen roots.</summary>
    public sealed class PrototypeRunState
    {
        public PrototypeRunState(int startingGold, string stageId = null, CharacterStatsSnapshot stats = null,
            string playerName = "플레이어", int startingLives = 3, string stageName = null, int clearExperience = 0,
            int clearOriginShards = 0, bool isOriginMine = false, int clearBlueprints = 0, IEnumerable<string> blueprintPool = null,
            CharacterPassiveSnapshot passive = null)
        {
            if (startingGold < 0) throw new ArgumentOutOfRangeException(nameof(startingGold));
            if (startingLives < 1) throw new ArgumentOutOfRangeException(nameof(startingLives));
            if (clearExperience < 0) throw new ArgumentOutOfRangeException(nameof(clearExperience));
            Gold = startingGold; StageId = stageId; StageName = stageName;
            PlayerStats = stats ?? CharacterStatsSnapshot.Default; PlayerName = playerName;
            Passive = passive ?? CharacterPassiveSnapshot.None;
            Lives = StartingLives = checked(startingLives + Passive.BonusLives);
            ClearExperience = clearExperience;
            ClearOriginShards = clearOriginShards;
            IsOriginMine = isOriginMine;
            ClearBlueprints = clearBlueprints;
            BlueprintPool = new List<string>(blueprintPool ?? Array.Empty<string>()).AsReadOnly();
        }
        public CharacterStatsSnapshot PlayerStats { get; }
        public CharacterPassiveSnapshot Passive { get; }
        internal IReadOnlyDictionary<string, StartingGear> StartingGear { get; set; } = new Dictionary<string, StartingGear>();
        public string PlayerName { get; }
        public string StageId { get; }
        public string StageName { get; }
        public int ClearExperience { get; }
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public int ClearOriginShards { get; }
        public bool IsOriginMine { get; }
        public int ClearBlueprints { get; }
        public IReadOnlyList<string> BlueprintPool { get; }
        public int StartingLives { get; }
        public int Lives { get; private set; }
        public int Victories { get; private set; }
        public int Defeats { get; private set; }
        public int Draws { get; private set; }
        public double TotalBattleSeconds { get; private set; }
        public CombatResult? LastBattleResult { get; private set; }
        public int LastLifeLoss { get; private set; }
        public PrototypeRunResult Result { get; private set; }
        public bool IsFinished => Result != null;
        public int Gold { get; internal set; }
        public int Round { get; internal set; } = 1;
        public int PurchasedItemSequence { get; internal set; }
        public long PlagueStacks { get; private set; }
        public PrototypePhase Phase { get; internal set; } = PrototypePhase.Preparation;

        public bool TryBeginBattle()
        {
            if (Phase != PrototypePhase.Preparation || IsFinished || Lives == 0) return false;
            LastBattleResult = null; LastLifeLoss = 0;
            Phase = PrototypePhase.Battle;
            return true;
        }

        public bool TryReturnToPreparation()
        {
            if (IsFinished || Lives == 0 || Phase is not (PrototypePhase.Defeat or PrototypePhase.Draw)) return false;
            Phase = PrototypePhase.Preparation;
            return true;
        }

        /// <summary>Only a battle-to-result transition can spend a life or finish a run, exactly once.
        /// Draw is retained for explicit test deadlines; normal play forces defeat at 90 combat seconds.</summary>
        public bool TryCompleteBattle(CombatResult result, int roundCount, double elapsedSeconds, long? plagueStacks = null)
        {
            if (Phase != PrototypePhase.Battle || IsFinished || result == CombatResult.Running) return false;
            if (!Enum.IsDefined(typeof(CombatResult), result)) throw new ArgumentOutOfRangeException(nameof(result));
            if (roundCount < Round) throw new ArgumentOutOfRangeException(nameof(roundCount));
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (plagueStacks < PlagueStacks) throw new ArgumentOutOfRangeException(nameof(plagueStacks));
            if (plagueStacks.HasValue) PlagueStacks = plagueStacks.Value;
            LastBattleResult = result; TotalBattleSeconds += elapsedSeconds;
            if (result == CombatResult.Victory)
            {
                Victories++;
                Phase = Round == roundCount ? PrototypePhase.Completed : PrototypePhase.Victory;
            }
            else if (result == CombatResult.Defeat)
            {
                Defeats++; Lives--; LastLifeLoss = 1;
                Phase = Lives == 0 ? PrototypePhase.GameOver : PrototypePhase.Defeat;
            }
            else { Draws++; Phase = PrototypePhase.Draw; }
            if (Phase is PrototypePhase.Completed or PrototypePhase.GameOver)
                Result = new PrototypeRunResult(this, roundCount,
                    Phase == PrototypePhase.Completed ? PrototypeRunOutcome.Cleared : PrototypeRunOutcome.GameOver);
            return true;
        }
    }
}
