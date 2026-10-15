using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Combat
{
    /// <summary>Per-instance effective stats. Changes here never modify an ItemData asset.</summary>
    public sealed class RuntimeWeaponStats
    {
        private float _attackDamage;
        private readonly RuntimeItemCycle _cycle;
        private float _accuracyPercent;
        private float _criticalChancePercent;
        private float _criticalDamagePercent;

        public RuntimeWeaponStats(WeaponStats baseStats, RuntimeItemCycle cycle)
        {
            _cycle = cycle;
            AttackDamage = baseStats.AttackDamage;
            AccuracyPercent = baseStats.AccuracyPercent;
            CriticalChancePercent = baseStats.CriticalChancePercent;
            CriticalDamagePercent = baseStats.CriticalDamagePercent;
        }

        public float AttackDamage
        {
            get => _attackDamage;
            set => _attackDamage = Mathf.Max(0f, WeaponStats.FiniteOrDefault(value, 0f));
        }

        public float CooldownSeconds
        {
            get => (float)_cycle.IntervalSeconds;
            set => _cycle.SetBaseInterval(value);
        }

        public float AccuracyPercent
        {
            get => _accuracyPercent;
            set => _accuracyPercent = Mathf.Clamp(WeaponStats.FiniteOrDefault(value, 0f), 0f, 100f);
        }

        public float CriticalChancePercent
        {
            get => _criticalChancePercent;
            set => _criticalChancePercent = Mathf.Clamp(WeaponStats.FiniteOrDefault(value, 0f), 0f, 100f);
        }

        public float CriticalDamagePercent
        {
            get => _criticalDamagePercent;
            set => _criticalDamagePercent = Mathf.Max(100f, WeaponStats.FiniteOrDefault(value, 200f));
        }
    }
}
