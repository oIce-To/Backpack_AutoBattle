using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using UnityEngine;

namespace BackpackAutoBattle.Stages
{
    [Serializable]
    public sealed class StageRoundData
    {
        [SerializeField] private EnemyEncounterData _encounter;
        [SerializeField] private bool _isBoss;
        [SerializeField] private EnemyEncounterOverrides _overrides = new();
        public EnemyEncounterData Encounter => _encounter;
        public bool IsBoss => _isBoss;
        public StageRoundData() { }
        public StageRoundData(EnemyEncounterData encounter, EnemyEncounterOverrides overrides = null, bool isBoss = false)
        { _encounter = encounter; _overrides = overrides ?? new(); _isBoss = isBoss; }
        public CombatantSnapshot CreateEnemySnapshot()
        {
            if (_encounter == null) throw new InvalidOperationException("라운드의 Enemy Encounter가 지정되지 않았습니다.");
            return _encounter.CreateCombatantSnapshot(_overrides);
        }
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Stage", fileName = "Stage_")]
    public sealed class StageData : ScriptableObject
    {
        public const int RoundsPerStage = 12;
        [SerializeField] private string _stageId;
        [SerializeField] private string _displayName;
        [SerializeField] private List<StageRoundData> _rounds = Enumerable.Range(0, RoundsPerStage).Select(_ => new StageRoundData()).ToList();
        public string StageId => _stageId;
        public string DisplayName => _displayName;
        public int RoundCount => _rounds?.Count ?? 0;
        public IReadOnlyList<StageRoundData> Rounds => _rounds;
        public StageRoundData GetRound(int round)
        {
            if (round < 1 || round > RoundCount) throw new InvalidOperationException($"{DisplayName}: {round}라운드가 없습니다.");
            return _rounds[round - 1] ?? throw new InvalidOperationException($"{DisplayName}: {round}라운드 설정이 비어 있습니다.");
        }
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(StageId) || string.IsNullOrWhiteSpace(DisplayName)) errors.Add("Stage ID와 Display Name을 지정하세요.");
            if (RoundCount != RoundsPerStage) errors.Add($"라운드는 정확히 {RoundsPerStage}개여야 합니다. 현재 {RoundCount}개입니다.");
            for (int round = 1; round <= RoundCount; round++)
            {
                try { GetRound(round).CreateEnemySnapshot(); }
                catch (InvalidOperationException exception) { errors.Add($"ROUND {round}: {exception.Message}"); }
            }
            return errors;
        }
        public void ValidateOrThrow()
        {
            List<string> errors = GetValidationErrors();
            if (errors.Count > 0) throw new InvalidOperationException($"{name}: " + string.Join("\n", errors));
        }
    }
}
