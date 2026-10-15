using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    [Flags]
    public enum ItemTags { None = 0, Melee = 1, Ranged = 2, Magic = 4, Summon = 8 }
    public enum ItemEffectKind { Attack, GainArmor, ApplyStatus }

    [Serializable]
    public sealed class ItemActivationEffect
    {
        [SerializeField] private ItemEffectKind _kind;
        [Tooltip("Used by GainArmor. Attack uses the item's weapon stats.")]
        [SerializeField, Min(0f)] private float _amount = 1f;
        [Tooltip("Used only by ApplyStatus.")]
        [SerializeField] private StatusApplication _status = new(StatusEffectKind.Haste, 1);
        public ItemEffectKind Kind => _kind;
        public float Amount => Mathf.Max(0f, WeaponStats.FiniteOrDefault(_amount, 0f));
        public StatusApplication Status => _status;
        public ItemActivationEffect(ItemEffectKind kind, float amount = 1f) { _kind = kind; _amount = amount; }
        public ItemActivationEffect(StatusApplication status) { _kind = ItemEffectKind.ApplyStatus; _status = status.Copy(); }
        public ItemActivationEffect Copy() => new(Kind, Amount) { _status = _status?.Copy() };
    }

    /// <summary>One shared activation clock and effect list, independent of item category.</summary>
    [Serializable]
    public sealed class ItemActivationData
    {
        [SerializeField] private bool _enabled;
        [SerializeField, Min(0.1f)] private float _cooldownSeconds = 2f;
        [SerializeField] private List<ItemActivationEffect> _effects = new();
        public bool HasCycle => _enabled && _effects != null && _effects.Any(effect => effect != null);
        public float CooldownSeconds => Mathf.Max(0.1f, WeaponStats.FiniteOrDefault(_cooldownSeconds, 2f));
        public IReadOnlyList<ItemActivationEffect> Effects => (_effects ?? new List<ItemActivationEffect>()).AsReadOnly();
        public bool HasEffect(ItemEffectKind kind) => HasCycle && _effects.Any(effect => effect != null && effect.Kind == kind);
        public ItemActivationData() { }
        public ItemActivationData(float interval, params ItemActivationEffect[] effects)
        { _enabled = true; _cooldownSeconds = interval; _effects = effects.Where(effect => effect != null).Select(effect => effect.Copy()).ToList(); }
        public ItemActivationData Copy()
        {
            var result = new ItemActivationData(CooldownSeconds, Effects.Where(effect => effect != null).ToArray());
            result._enabled = _enabled;
            return result;
        }
    }

    /// <summary>Category and required tags are ANDed. No category/tags means all periodic items.</summary>
    [Serializable]
    public sealed class ItemTargetFilter
    {
        [SerializeField] private bool _filterByCategory;
        [SerializeField] private ItemCategory _category;
        [SerializeField] private ItemTags _requiredTags;
        public ItemTargetFilter(ItemCategory? category = null, ItemTags requiredTags = ItemTags.None)
        { _filterByCategory = category.HasValue; _category = category.GetValueOrDefault(); _requiredTags = requiredTags; }
        public bool Matches(ItemCategory category, ItemTags tags)
            => (!_filterByCategory || _category == category) && (tags & _requiredTags) == _requiredTags;
        public ItemTargetFilter Copy() => new(_filterByCategory ? _category : (ItemCategory?)null, _requiredTags);
    }
}
