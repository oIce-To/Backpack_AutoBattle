using System;
using BackpackAutoBattle.Progression;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    /// <summary>Stage-entry values, never a live reference to character/progression assets.</summary>
    public sealed class CharacterStatsSnapshot
    {
        public static CharacterStatsSnapshot Default { get; } = new(50, 10);
        public double MaximumHealth { get; }
        public double Attack { get; }
        public double CriticalDamageBonusPercent { get; }
        public double CriticalChanceBonusPercent { get; }
        public double ArmorGainBonusPercent { get; }
        public double HealingBonusPercent { get; }
        public double ArmorGainMultiplier => 1d + ArmorGainBonusPercent / 100d;
        public double HealingMultiplier => 1d + HealingBonusPercent / 100d;
        public CharacterStatsSnapshot(double maximumHealth, double attack, double criticalDamageBonusPercent = 0,
            double criticalChanceBonusPercent = 0, double armorGainBonusPercent = 0, double healingBonusPercent = 0)
        {
            if (!double.IsFinite(maximumHealth) || maximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (!double.IsFinite(attack) || attack < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            if (!double.IsFinite(criticalDamageBonusPercent) || criticalDamageBonusPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(criticalDamageBonusPercent));
            if (!double.IsFinite(criticalChanceBonusPercent) || criticalChanceBonusPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(criticalChanceBonusPercent));
            if (!double.IsFinite(armorGainBonusPercent) || armorGainBonusPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(armorGainBonusPercent));
            if (!double.IsFinite(healingBonusPercent) || healingBonusPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(healingBonusPercent));
            MaximumHealth = maximumHealth; Attack = attack;
            CriticalDamageBonusPercent = criticalDamageBonusPercent;
            CriticalChanceBonusPercent = criticalChanceBonusPercent;
            ArmorGainBonusPercent = armorGainBonusPercent;
            HealingBonusPercent = healingBonusPercent;
        }
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Character", fileName = "Character_")]
    public sealed class CharacterData : ScriptableObject
    {
        [SerializeField] private string _characterId;
        [SerializeField] private string _displayName = "플레이어";
        [SerializeField] private Sprite _portrait;
        [SerializeField] private bool _portraitFacesLeft;
        [SerializeField, Min(1)] private float _maximumHealth = 50;
        [SerializeField, Min(0)] private float _attack = 10;
        [SerializeField] private ExperienceTableData _experienceTable;
        [SerializeField] private bool _initiallyAvailable;
        [SerializeField] private UniqueGearData _starterGear;
        [SerializeField] private CharacterPassiveData _passive;
        public CharacterPassiveData Passive => _passive;
        public bool InitiallyAvailable => _initiallyAvailable;
        public UniqueGearData StarterGear => _starterGear;
        public string DisplayName => _displayName;
        public string CharacterId => _characterId;
        public Sprite Portrait => _portrait;
        public bool PortraitFacesLeft => _portraitFacesLeft;
        public ExperienceTableData ExperienceTable => _experienceTable;
        public CharacterStatsSnapshot CaptureStats() => new(_maximumHealth, _attack);
        public CharacterStatsSnapshot CaptureStats(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            double growth = Math.Pow(1.1, level - 1);
            return new CharacterStatsSnapshot(Math.Floor(_maximumHealth * growth), Math.Floor(_attack * growth));
        }
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
