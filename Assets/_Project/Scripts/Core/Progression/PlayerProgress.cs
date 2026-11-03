using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Stages;

namespace BackpackAutoBattle.Progression
{
    [Serializable]
    public sealed class ProfileSaveData
    {
        public int version;
        public string selectedCharacterId;
        public string selectedStageId;
        public List<StageProgressData> stages;
    }

    [Serializable]
    public sealed class StageProgressData
    {
        public string stageId;
        public int highestRound;
        public bool cleared;
    }

    public sealed class StageProgress
    {
        internal StageProgress(string id, int highest, bool cleared)
        { StageId = id; HighestRound = highest; Cleared = cleared; }
        public string StageId { get; }
        public int HighestRound { get; }
        public bool Cleared { get; }
    }

    /// <summary>Permanent progress, independent of editable content assets and the current run.</summary>
    public sealed class PlayerProgress
    {
        public const int SaveVersion = 1;
        private readonly Dictionary<string, StageProgress> _stages = new(StringComparer.Ordinal);
        public string SelectedCharacterId { get; private set; } = "";
        public string SelectedStageId { get; private set; } = "";
        public StageProgress GetStage(string id) => _stages.TryGetValue(id ?? "", out var progress)
            ? progress : new StageProgress(id, 0, false);
        public void SelectCharacter(string id) => SelectedCharacterId = ValidId(id);
        public void SelectStage(string id) => SelectedStageId = ValidId(id);

        // Catalog order defines progression; displayed names and IDs need not contain a stage number.
        public bool IsStageUnlocked(string id, IReadOnlyList<string> orderedStageIds)
        {
            if (string.IsNullOrWhiteSpace(id) || orderedStageIds == null) return false;
            for (int i = 0; i < orderedStageIds.Count; i++)
                if (orderedStageIds[i] == id) return i == 0 || GetStage(orderedStageIds[i - 1]).Cleared;
            return false;
        }

        public bool RecordRound(string id, int round, int roundCount, bool cleared = false)
        {
            ValidId(id);
            if (roundCount != StageData.RoundsPerStage || round < 1 || round > roundCount || cleared && round != roundCount)
                throw new ArgumentOutOfRangeException(nameof(round));
            StageProgress old = GetStage(id);
            var next = new StageProgress(id, Math.Max(old.HighestRound, round), old.Cleared || cleared);
            if (next.HighestRound == old.HighestRound && next.Cleared == old.Cleared) return false;
            _stages[id] = next;
            return true;
        }

        public ProfileSaveData Export() => new()
        {
            version = SaveVersion, selectedCharacterId = SelectedCharacterId, selectedStageId = SelectedStageId,
            stages = _stages.Values.OrderBy(p => p.StageId, StringComparer.Ordinal).Select(p => new StageProgressData
            { stageId = p.StageId, highestRound = p.HighestRound, cleared = p.Cleared }).ToList()
        };

        public static PlayerProgress FromData(ProfileSaveData data)
        {
            if (data == null || data.version < 1) throw new FormatException("Invalid progress save.");
            if (data.version > SaveVersion) throw new NotSupportedException("This progress save uses a newer version.");
            var progress = new PlayerProgress
            {
                SelectedCharacterId = data.selectedCharacterId ?? "", SelectedStageId = data.selectedStageId ?? ""
            };
            foreach (var stage in data.stages ?? new List<StageProgressData>())
            {
                if (stage == null || string.IsNullOrWhiteSpace(stage.stageId) || stage.highestRound < 1 || stage.highestRound > StageData.RoundsPerStage
                    || stage.cleared && stage.highestRound != StageData.RoundsPerStage) throw new FormatException("Invalid stage progress.");
                progress.RecordRound(stage.stageId, stage.highestRound, StageData.RoundsPerStage, stage.cleared);
            }
            return progress;
        }

        private static string ValidId(string id) => !string.IsNullOrWhiteSpace(id) ? id
            : throw new ArgumentException("A stable content ID is required.", nameof(id));
    }
}
