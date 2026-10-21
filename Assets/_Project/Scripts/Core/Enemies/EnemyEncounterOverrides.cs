using System;
using System.Collections.Generic;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Enemies
{
    [Serializable]
    public sealed class EnemyStatOverride
    {
        [SerializeField] private bool _enabled;
        [SerializeField] private float _value;
        public bool Enabled => _enabled;
        public float Value => _value;
        public EnemyStatOverride() { }
        public EnemyStatOverride(float value) { _enabled = true; _value = value; }
        internal float Resolve(float original, float minimum, float maximum, string label)
        {
            if (!Enabled) return original;
            if (float.IsNaN(Value) || float.IsInfinity(Value) || Value < minimum || Value > maximum)
                throw new InvalidOperationException($"{label}: 덮어쓰기 수치가 허용 범위를 벗어났습니다.");
            return Value;
        }
    }

    [Serializable]
    public sealed class EnemyItemStatOverrides
    {
        [SerializeField] private EnemyStatOverride _attackDamage = new();
        [SerializeField] private EnemyStatOverride _cooldownSeconds = new();
        [SerializeField] private EnemyStatOverride _accuracyPercent = new();
        [SerializeField] private EnemyStatOverride _criticalChancePercent = new();
        [SerializeField] private EnemyStatOverride _criticalDamagePercent = new();
        public EnemyItemStatOverrides() { }
        public EnemyItemStatOverrides(float? attackDamage = null, float? cooldownSeconds = null,
            float? accuracyPercent = null, float? criticalChancePercent = null, float? criticalDamagePercent = null)
        {
            _attackDamage = Optional(attackDamage); _cooldownSeconds = Optional(cooldownSeconds);
            _accuracyPercent = Optional(accuracyPercent); _criticalChancePercent = Optional(criticalChancePercent);
            _criticalDamagePercent = Optional(criticalDamagePercent);
        }
        private static EnemyStatOverride Optional(float? value) => value.HasValue ? new(value.Value) : new();
        private static float Resolve(EnemyStatOverride modifier, float original, float minimum, float maximum, string label)
            => modifier?.Resolve(original, minimum, maximum, label) ?? original;
        internal void Apply(ref WeaponStats stats, ref ItemActivationData activation)
        {
            float cooldown = Resolve(_cooldownSeconds, activation.CooldownSeconds, 0.1f, float.MaxValue, "사용 주기");
            stats = new WeaponStats(
                Resolve(_attackDamage, stats.AttackDamage, 0f, float.MaxValue, "공격력"), cooldown,
                Resolve(_accuracyPercent, stats.AccuracyPercent, 30f, 100f, "명중률"),
                Resolve(_criticalChancePercent, stats.CriticalChancePercent, 0f, float.MaxValue, "치명타 확률"),
                Resolve(_criticalDamagePercent, stats.CriticalDamagePercent, 100f, float.MaxValue, "치명타 피해"));
            activation = activation.CopyWithCooldown(cooldown);
        }
    }

    [Serializable]
    public sealed class EnemyItemOverride
    {
        [Tooltip("Copy the Slot Id from the selected encounter. Reordering its Items list does not change this target.")]
        [SerializeField] private string _slotId;
        [SerializeField] private EnemyItemStatOverrides _stats = new();
        public string SlotId => _slotId;
        public EnemyItemStatOverrides Stats => _stats;
        public EnemyItemOverride() { }
        public EnemyItemOverride(string slotId, EnemyItemStatOverrides stats) { _slotId = slotId; _stats = stats; }
    }

    [Serializable]
    public sealed class EnemyEncounterOverrides
    {
        [SerializeField] private bool _overrideMaximumHealth;
        [SerializeField, Min(1)] private int _maximumHealth = 30;
        [SerializeField] private EnemyStatOverride _attack = new();
        [SerializeField] private List<EnemyItemOverride> _items = new();
        public EnemyEncounterOverrides() { }
        public EnemyEncounterOverrides(int? maximumHealth, params EnemyItemOverride[] items)
        { _overrideMaximumHealth = maximumHealth.HasValue; _maximumHealth = maximumHealth ?? 30; _items = new(items); }
        public EnemyEncounterOverrides(int? maximumHealth, float attack, params EnemyItemOverride[] items) : this(maximumHealth, items)
        { _attack = new(attack); }
        internal float ResolveAttack(float original) => _attack?.Resolve(original, 0, float.MaxValue, "기준 공격력") ?? original;
        internal int ResolveHealth(int original)
        {
            if (!_overrideMaximumHealth) return original;
            if (_maximumHealth < 1) throw new InvalidOperationException("최대 체력 덮어쓰기는 1 이상이어야 합니다.");
            return _maximumHealth;
        }
        internal Dictionary<string, EnemyItemStatOverrides> ValidateSlots(HashSet<string> available)
        {
            var result = new Dictionary<string, EnemyItemStatOverrides>(StringComparer.Ordinal);
            foreach (EnemyItemOverride item in _items ?? new())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.SlotId) || !available.Contains(item.SlotId))
                    throw new InvalidOperationException($"라운드 스탯 덮어쓰기의 장비 슬롯 '{item?.SlotId}'이 존재하지 않습니다.");
                if (!result.TryAdd(item.SlotId, item.Stats))
                    throw new InvalidOperationException($"장비 슬롯 '{item.SlotId}'의 라운드 덮어쓰기가 중복되었습니다.");
            }
            return result;
        }
    }
}
