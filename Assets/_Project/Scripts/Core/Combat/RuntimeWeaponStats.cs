using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Combat
{
    /// <summary>Per-instance effective stats. Changes here never modify an ItemData asset.</summary>
    public sealed class RuntimeWeaponStats
    {
        private float _attackDamage;
        private float _cooldownSeconds;
        private float _accuracyPercent;
        private float _criticalChancePercent;
        private float _criticalDamagePercent;

        public RuntimeWeaponStats(WeaponStats baseStats)
        {
            AttackDamage = baseStats.AttackDamage;
            CooldownSeconds = baseStats.CooldownSeconds;
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
            get => _cooldownSeconds;
            set => _cooldownSeconds = Mathf.Max(0.1f, WeaponStats.FiniteOrDefault(value, 2f));
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
