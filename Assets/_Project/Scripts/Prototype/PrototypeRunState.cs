using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Prototype
{
    public enum PrototypePhase { Preparation, Battle, Victory, Defeat, Draw, Completed, Fusion }

    /// <summary>Run progress lives independently of both screen roots.</summary>
    public sealed class PrototypeRunState
    {
        public PrototypeRunState(int startingGold, string stageId = null, CharacterStatsSnapshot stats = null, string playerName = "플레이어")
        { Gold = startingGold; StageId = stageId; PlayerStats = stats ?? CharacterStatsSnapshot.Default; PlayerName = playerName; }
        public CharacterStatsSnapshot PlayerStats { get; }
        public string PlayerName { get; }
        public string StageId { get; }
        public int Gold { get; internal set; }
        public int Round { get; internal set; } = 1;
        public int PurchasedItemSequence { get; internal set; }
        public PrototypePhase Phase { get; internal set; } = PrototypePhase.Preparation;
    }
}
