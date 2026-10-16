namespace BackpackAutoBattle.Prototype
{
    public enum PrototypePhase { Preparation, Battle, Victory, Defeat, Draw, Completed }

    /// <summary>Run progress lives independently of both screen roots.</summary>
    public sealed class PrototypeRunState
    {
        public PrototypeRunState(int startingGold, string stageId = null) { Gold = startingGold; StageId = stageId; }
        public string StageId { get; }
        public int Gold { get; internal set; }
        public int Round { get; internal set; } = 1;
        public int PurchasedItemSequence { get; internal set; }
        public PrototypePhase Phase { get; internal set; } = PrototypePhase.Preparation;
    }
}
