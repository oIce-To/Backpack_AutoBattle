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
        public long sharedExperience;
        public List<CharacterProgressData> characters;
        public List<string> acquiredCharacters;
        public List<UniqueGearSaveData> uniqueGear;
        public long originShards;
        public List<BlueprintBalanceData> blueprints;
        public int mineTickets;
        public long nextMineTicketUtcTicks;
        public long ticketClockUtcTicks;
        public string lastRewardClaim;
    }

    [Serializable]
    public sealed class CharacterProgressData
    {
        public string characterId;
        public int level;
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
    public sealed partial class PlayerProgress
    {
        public const int SaveVersion = 5;
        private readonly Dictionary<string, StageProgress> _stages = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _characterLevels = new(StringComparer.Ordinal);
        public long SharedExperience { get; private set; }
        public int GetCharacterLevel(string id) => _characterLevels.TryGetValue(id ?? "", out int level) ? level : 1;
        public bool TryAddExperience(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > long.MaxValue - SharedExperience) return false;
            SharedExperience += amount;
            return true;
        }
        public bool TryLevelUp(string id, ExperienceTableData table)
        {
            ValidId(id);
            int level = GetCharacterLevel(id);
            if (table == null || !table.TryGetCost(level, out long cost) || SharedExperience < cost) return false;
            SharedExperience -= cost;
            _characterLevels[id] = level + 1;
            return true;
        }
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
            sharedExperience = SharedExperience,
            acquiredCharacters = _acquiredCharacters.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            uniqueGear = _uniqueGear.Values.Select(item => item.Export()).ToList(),
            originShards = OriginShards,
            blueprints = _blueprints.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new BlueprintBalanceData { gearId = pair.Key, count = pair.Value }).ToList(),
            mineTickets = MineTickets,
            nextMineTicketUtcTicks = _nextMineTicketUtcTicks,
            ticketClockUtcTicks = _ticketClockUtcTicks,
            lastRewardClaim = _lastRewardClaim,
            characters = _characterLevels.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => new CharacterProgressData { characterId = p.Key, level = p.Value }).ToList(),
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
            if (data.version >= 2)
            {
                if (data.sharedExperience < 0) throw new FormatException("Invalid experience balance.");
                progress.SharedExperience = data.sharedExperience;
                foreach (var character in data.characters ?? new List<CharacterProgressData>())
                {
                    if (character == null || string.IsNullOrWhiteSpace(character.characterId) || character.level < 1)
                        throw new FormatException("Invalid character progress.");
                    progress._characterLevels[character.characterId] = Math.Max(progress.GetCharacterLevel(character.characterId), character.level);
                }
            }
            if (data.version >= 3) progress.LoadUniqueGear(data);
            if (data.version >= 4) progress.LoadEconomy(data);
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
