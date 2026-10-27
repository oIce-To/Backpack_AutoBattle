using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum StatusEffectKind { Haste, Freeze, Precision, Focus, Blindness, Agility, Burn, Poison, Thorns, Lifesteal, Enrage }
    public enum StatusStat { ActivationSpeed, CriticalChance, Accuracy, Evasion, PeriodicDamage, ReflectedDamage, HealingOnAttack, DamageTaken }
    public enum StatusTarget { Self, Opponent }

    public static class StatusEffectRules
    {
        public static string DisplayName(StatusEffectKind kind) => kind switch
        {
            StatusEffectKind.Haste => "신속", StatusEffectKind.Freeze => "빙결", StatusEffectKind.Precision => "정밀",
            StatusEffectKind.Focus => "집중", StatusEffectKind.Blindness => "실명", StatusEffectKind.Agility => "민첩",
            StatusEffectKind.Burn => "화상", StatusEffectKind.Poison => "중독",
            StatusEffectKind.Thorns => "가시", StatusEffectKind.Lifesteal => "흡혈",
            StatusEffectKind.Enrage => "광폭화",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        public static bool IsDebuff(StatusEffectKind kind) => kind is StatusEffectKind.Freeze or StatusEffectKind.Blindness or StatusEffectKind.Burn or StatusEffectKind.Poison or StatusEffectKind.Enrage;
        public static StatusStat AffectedStat(StatusEffectKind kind) => kind switch
        {
            StatusEffectKind.Haste or StatusEffectKind.Freeze => StatusStat.ActivationSpeed,
            StatusEffectKind.Precision => StatusStat.CriticalChance,
            StatusEffectKind.Focus or StatusEffectKind.Blindness => StatusStat.Accuracy,
            StatusEffectKind.Agility => StatusStat.Evasion,
            StatusEffectKind.Burn or StatusEffectKind.Poison => StatusStat.PeriodicDamage,
            StatusEffectKind.Thorns => StatusStat.ReflectedDamage,
            StatusEffectKind.Lifesteal => StatusStat.HealingOnAttack,
            StatusEffectKind.Enrage => StatusStat.DamageTaken,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        public static StatusEffectKind? Opposite(StatusEffectKind kind) => kind switch
        {
            StatusEffectKind.Haste => StatusEffectKind.Freeze, StatusEffectKind.Freeze => StatusEffectKind.Haste,
            StatusEffectKind.Focus => StatusEffectKind.Blindness, StatusEffectKind.Blindness => StatusEffectKind.Focus,
            _ => null
        };
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Status Effect", fileName = "Status_")]
    public sealed class StatusEffectData : ScriptableObject
    {
        [SerializeField] private StatusEffectKind _kind;
        [SerializeField] private string _displayName = "신속";
        [SerializeField, Min(0f)] private float _percentPerStack = 1f;
        [Header("Periodic damage / damage reactions (owner stat scaling)")]
        [InspectorName("Amount Scaling")]
        [SerializeField] private StatScaling _damageScaling = new();
        [SerializeField, Min(0.01f)] private float _tickIntervalSeconds = 1f;
        [SerializeField, Min(0)] private int _stacksLostPerTick;
        public StatusEffectKind Kind => _kind;
        public string DisplayName => _displayName;
        public float PercentPerStack => Mathf.Max(0f, WeaponStats.FiniteOrDefault(_percentPerStack, 1f));
        public StatScaling DamageScaling => _damageScaling;
        public float TickIntervalSeconds => Mathf.Max(0.01f, WeaponStats.FiniteOrDefault(_tickIntervalSeconds, 1));
        public int StacksLostPerTick => Math.Max(0, _stacksLostPerTick);
    }

    /// <summary>Used by battle-start grants and periodic item effects alike.</summary>
    [Serializable]
    public sealed class StatusApplication
    {
        [SerializeField] private StatusEffectKind _kind;
        [SerializeField] private StatusTarget _target;
        [SerializeField, Min(0)] private int _stacks = 1;
        public StatusEffectKind Kind => _kind;
        public StatusTarget Target => _target;
        public int Stacks => Math.Max(0, _stacks);
        public StatusApplication(StatusEffectKind kind, int stacks, StatusTarget target = StatusTarget.Self)
        { _kind = kind; _stacks = stacks; _target = target; }
        public StatusApplication Copy() => new(Kind, Stacks, Target);
    }
}
