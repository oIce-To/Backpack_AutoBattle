using System;
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
        private double _statusAccuracyBonus, _statusCriticalBonus;
        internal Func<double> PlacementAttackBonus { private get; set; }
        internal Func<double> PlacementCriticalChanceBonus { private get; set; }
        internal Func<double> PlacementCriticalDamageBonus { private get; set; }
        internal double StageAccuracyBonus { private get; set; }
        internal Func<double> StageAttackFactor { private get; set; }
        internal Func<double> StageCriticalBonus { private get; set; }
        internal Func<double> StageCriticalDamageBonus { private get; set; }
        internal Func<double> StageCriticalDamageFactor { private get; set; }
        internal Func<double> EnemyAttackFactor { private get; set; }
        internal Func<double> EnemyCriticalBonus { private get; set; }

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
            get => (float)Math.Clamp((_attackDamage + (PlacementAttackBonus?.Invoke() ?? 0d))
                * (StageAttackFactor?.Invoke() ?? 1d) * (EnemyAttackFactor?.Invoke() ?? 1d), 0d, float.MaxValue);
            set => _attackDamage = Mathf.Max(0f, WeaponStats.FiniteOrDefault(value, 0f));
        }

        public float CooldownSeconds
        {
            get => (float)_cycle.IntervalSeconds;
            set => _cycle.SetBaseInterval(value);
        }

        public float AccuracyPercent
        {
            get => (float)Math.Clamp(_accuracyPercent + _statusAccuracyBonus + StageAccuracyBonus, 30d, 100d);
            set => _accuracyPercent = WeaponStats.FiniteOrDefault(value, 30f);
        }

        public float CriticalChancePercent
        {
            get => (float)Math.Clamp(_criticalChancePercent + _statusCriticalBonus + (PlacementCriticalChanceBonus?.Invoke() ?? 0d)
                + (StageCriticalBonus?.Invoke() ?? 0d) + (EnemyCriticalBonus?.Invoke() ?? 0d), 0d, float.MaxValue);
            set => _criticalChancePercent = Mathf.Max(0f, WeaponStats.FiniteOrDefault(value, 0f));
        }

        // Replace this contribution instead of incrementing effective stats (avoids drift at caps).
        internal void SetStatusModifiers(double accuracy, double precision)
        { _statusAccuracyBonus = accuracy; _statusCriticalBonus = precision; }

        public float CriticalDamagePercent
        {
            get => (float)Math.Clamp(_criticalDamagePercent * (StageCriticalDamageFactor?.Invoke() ?? 1d)
                + (PlacementCriticalDamageBonus?.Invoke() ?? 0d) + (StageCriticalDamageBonus?.Invoke() ?? 0d), 100d, float.MaxValue);
            set => _criticalDamagePercent = Mathf.Max(100f, WeaponStats.FiniteOrDefault(value, 200f));
        }
    }
}
