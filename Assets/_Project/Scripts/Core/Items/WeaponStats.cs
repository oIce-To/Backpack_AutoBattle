using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    [Serializable]
    public sealed class WeaponStats
    {
        [SerializeField, Min(0f)] private float _attackDamage = 5f;
        // Legacy serialized/import seed only. ItemActivationData is the authoritative definition of timing.
        [SerializeField, HideInInspector] private float _cooldownSeconds = 2f;
        [SerializeField, Range(0f, 100f)] private float _accuracyPercent = 90f;
        [SerializeField, Range(0f, 100f)] private float _criticalChancePercent = 10f;
        [Tooltip("Total damage on a critical hit. 200% means twice the normal damage.")]
        [SerializeField, Min(100f)] private float _criticalDamagePercent = 200f;

        public float AttackDamage => _attackDamage;
        public float CooldownSeconds => _cooldownSeconds;
        public float AccuracyPercent => _accuracyPercent;
        public float CriticalChancePercent => _criticalChancePercent;
        public float CriticalDamagePercent => _criticalDamagePercent;

        public WeaponStats() { }

        public WeaponStats(float attackDamage, float cooldownSeconds, float accuracyPercent,
            float criticalChancePercent, float criticalDamagePercent)
        {
            _attackDamage = attackDamage;
            _cooldownSeconds = cooldownSeconds;
            _accuracyPercent = accuracyPercent;
            _criticalChancePercent = criticalChancePercent;
            _criticalDamagePercent = criticalDamagePercent;
            Validate();
        }

        public WeaponStats Copy()
        {
            return new WeaponStats(AttackDamage, CooldownSeconds, AccuracyPercent,
                CriticalChancePercent, CriticalDamagePercent);
        }

        public void Validate()
        {
            _attackDamage = Mathf.Max(0f, FiniteOrDefault(_attackDamage, 0f));
            _cooldownSeconds = Mathf.Max(0.1f, FiniteOrDefault(_cooldownSeconds, 2f));
            _accuracyPercent = Mathf.Clamp(FiniteOrDefault(_accuracyPercent, 90f), 0f, 100f);
            _criticalChancePercent = Mathf.Clamp(FiniteOrDefault(_criticalChancePercent, 0f), 0f, 100f);
            _criticalDamagePercent = Mathf.Max(100f, FiniteOrDefault(_criticalDamagePercent, 200f));
        }

        internal static float FiniteOrDefault(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }
    }
}
