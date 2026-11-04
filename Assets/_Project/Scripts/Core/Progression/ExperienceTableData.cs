using System;
using UnityEngine;

namespace BackpackAutoBattle.Progression
{
    [CreateAssetMenu(menuName = "Backpack AutoBattle/Experience Table", fileName = "ExperienceTable_")]
    public sealed class ExperienceTableData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _baseExperience = 100;
        [SerializeField, Min(1)] private float _costMultiplier = 1.25f;

        // Round only the final cost, never the previous level's already-rounded cost.
        public bool TryGetCost(int currentLevel, out long cost)
        {
            cost = 0;
            if (currentLevel < 1 || currentLevel == int.MaxValue || _baseExperience < 1
                || !float.IsFinite(_costMultiplier) || _costMultiplier < 1) return false;
            double amount = _baseExperience * Math.Pow(_costMultiplier, currentLevel - 1);
            if (!double.IsFinite(amount) || amount >= long.MaxValue) return false;
            cost = (long)Math.Floor(amount);
            return true;
        }
    }

    public static class StageExperienceReward
    {
        public static long Calculate(int clearExperience, bool cleared, int round)
        {
            if (clearExperience < 0) throw new ArgumentOutOfRangeException(nameof(clearExperience));
            if (round < 1 || round > 12) throw new ArgumentOutOfRangeException(nameof(round));
            int percent = cleared ? 100 : round <= 4 ? 10 : round <= 8 ? 25 : 50;
            return (long)clearExperience * percent / 100;
        }
    }
}
