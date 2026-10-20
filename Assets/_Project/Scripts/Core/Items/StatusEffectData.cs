using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum StatusEffectKind { Haste, Freeze, Precision, Focus, Blindness, Agility }
    public enum StatusStat { ActivationSpeed, CriticalChance, Accuracy, Evasion }
    public enum StatusTarget { Self, Opponent }

    public static class StatusEffectRules
    {
        public static string DisplayName(StatusEffectKind kind) => kind switch
        {
            StatusEffectKind.Haste => "신속", StatusEffectKind.Freeze => "빙결", StatusEffectKind.Precision => "정밀",
            StatusEffectKind.Focus => "집중", StatusEffectKind.Blindness => "실명", StatusEffectKind.Agility => "민첩",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        public static bool IsDebuff(StatusEffectKind kind) => kind == StatusEffectKind.Freeze || kind == StatusEffectKind.Blindness;
        public static StatusStat AffectedStat(StatusEffectKind kind) => kind switch
        {
            StatusEffectKind.Haste or StatusEffectKind.Freeze => StatusStat.ActivationSpeed,
            StatusEffectKind.Precision => StatusStat.CriticalChance,
            StatusEffectKind.Focus or StatusEffectKind.Blindness => StatusStat.Accuracy,
            StatusEffectKind.Agility => StatusStat.Evasion,
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
        public StatusEffectKind Kind => _kind;
        public string DisplayName => _displayName;
        public float PercentPerStack => Mathf.Max(0f, WeaponStats.FiniteOrDefault(_percentPerStack, 1f));
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
