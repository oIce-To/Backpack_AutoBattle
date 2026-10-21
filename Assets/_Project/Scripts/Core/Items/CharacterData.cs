using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    /// <summary>Stage-entry values, never a live reference to character/progression assets.</summary>
    public sealed class CharacterStatsSnapshot
    {
        public static CharacterStatsSnapshot Default { get; } = new(50, 10);
        public double MaximumHealth { get; }
        public double Attack { get; }
        public CharacterStatsSnapshot(double maximumHealth, double attack)
        {
            if (!double.IsFinite(maximumHealth) || maximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (!double.IsFinite(attack) || attack < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            MaximumHealth = maximumHealth; Attack = attack;
        }
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Character", fileName = "Character_")]
    public sealed class CharacterData : ScriptableObject
    {
        [SerializeField] private string _displayName = "플레이어";
        [SerializeField, Min(1)] private float _maximumHealth = 50;
        [SerializeField, Min(0)] private float _attack = 10;
        public string DisplayName => _displayName;
        public CharacterStatsSnapshot CaptureStats() => new(_maximumHealth, _attack);
    }

    public enum StatReference { Fixed, Attack, MaximumHealth }

    /// <summary>Pure proportional scaling, with explicit fixed-value compatibility for authored enemy overrides.
    /// There is deliberately no additive constant in a scaled formula.</summary>
    [Serializable]
    public sealed class StatScaling
    {
        [SerializeField] private StatReference _stat;
        [SerializeField] private float _coefficient;
        public StatReference Stat => _stat;
        public float Coefficient => _coefficient;
        public StatScaling() { }
        public StatScaling(StatReference stat, float coefficient) { _stat = stat; _coefficient = coefficient; }
        public float Resolve(float fixedValue, CharacterStatsSnapshot stats)
        {
            if (_stat == StatReference.Fixed) return fixedValue;
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            double reference = _stat switch
            {
                StatReference.Attack => stats.Attack,
                StatReference.MaximumHealth => stats.MaximumHealth,
                _ => throw new ArgumentOutOfRangeException(nameof(Stat))
            };
            double amount = reference * _coefficient;
            if (!double.IsFinite(amount) || Math.Abs(amount) > float.MaxValue) throw new InvalidOperationException("능력치 계수 계산 결과가 유효 범위를 벗어났습니다.");
            return (float)amount;
        }
        public StatScaling Copy() => new(_stat, _coefficient);
    }
}
