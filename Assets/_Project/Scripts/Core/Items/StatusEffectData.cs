using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum StatusEffectKind { Haste, Freeze }
    public enum StatusTarget { Self, Opponent }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Status Effect", fileName = "Status_")]
    public sealed class StatusEffectData : ScriptableObject
    {
        [SerializeField] private StatusEffectKind _kind;
        [SerializeField] private string _displayName = "신속";
        [SerializeField, Min(0f)] private float _percentPerStack = 1f;
        [Tooltip("0 means no stack cap. Activation speed still uses the common clock's safety limits.")]
        [SerializeField, Min(0)] private int _maximumStacks;
        public StatusEffectKind Kind => _kind;
        public string DisplayName => _displayName;
        public float PercentPerStack => Mathf.Max(0f, WeaponStats.FiniteOrDefault(_percentPerStack, 1f));
        public int MaximumStacks => Math.Max(0, _maximumStacks);
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
