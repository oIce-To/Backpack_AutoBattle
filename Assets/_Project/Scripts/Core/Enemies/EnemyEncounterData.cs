using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.Serialization;

namespace BackpackAutoBattle.Enemies
{
    [Serializable]
    public sealed class EnemyItemPlacement
    {
        [Tooltip("Stable, unique slot ID within this encounter. Round overrides target this ID, not the item type or list index.")]
        [SerializeField] private string _slotId;
        [SerializeField] private ItemData _item;
        [SerializeField] private Vector2Int _origin;
        [SerializeField] private ItemRotation _rotation;
        [SerializeField] private EnemyItemStatOverrides _stats = new();

        public string SlotId => _slotId;
        public ItemData Item => _item;
        public Vector2Int Origin => _origin;
        public ItemRotation Rotation => _rotation;
        public EnemyItemStatOverrides Stats => _stats;

        public EnemyItemPlacement() { }
        public EnemyItemPlacement(string slotId, ItemData item, Vector2Int origin,
            ItemRotation rotation = ItemRotation.None, EnemyItemStatOverrides stats = null)
        { _slotId = slotId; _item = item; _origin = origin; _rotation = rotation; _stats = stats ?? new(); }
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Enemy Encounter", fileName = "Encounter_")]
    public sealed class EnemyEncounterData : ScriptableObject
    {
        public const int BackpackSize = 6;
        [FormerlySerializedAs("_enemyId")]
        [SerializeField] private string _encounterId = "goblin";
        [SerializeField] private EnemyData _enemy;
        [SerializeField, Min(1)] private int _maximumHealth = 30;
        [Tooltip("Owner stat for proportional item effects and periodic damage. Item overrides remain final values.")]
        [SerializeField, Min(0)] private float _attack = 10;
        [SerializeField] private List<EnemyItemPlacement> _items = new();

        public string EncounterId => _encounterId;
        public EnemyData Enemy => _enemy;
        public string DisplayName => _enemy != null ? _enemy.DisplayName : "적 종류 미지정";
        public int MaximumHealth => _maximumHealth;
        public int BackpackWidth => BackpackSize;
        public int BackpackHeight => BackpackSize;
        public IReadOnlyList<EnemyItemPlacement> Items => _items;

        public CombatantSnapshot CreateCombatantSnapshot(EnemyEncounterOverrides overrides = null)
        {
            if (_enemy == null || string.IsNullOrWhiteSpace(_enemy.EnemyId) || string.IsNullOrWhiteSpace(_enemy.DisplayName))
                throw new InvalidOperationException($"{name}: 몬스터 종류와 이름/ID를 지정하세요.");
            if (string.IsNullOrWhiteSpace(EncounterId) || MaximumHealth < 1)
                throw new InvalidOperationException($"{name}: 전투 구성 ID와 1 이상의 체력이 필요합니다.");
            if (_items == null) throw new InvalidOperationException($"{name}: 장비 목록이 없습니다.");
            var slotIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (EnemyItemPlacement placement in _items)
                if (placement == null || string.IsNullOrWhiteSpace(placement.SlotId) || !slotIds.Add(placement.SlotId))
                    throw new InvalidOperationException($"{name}: 장비 슬롯 ID가 비어 있거나 중복되었습니다.");
            Dictionary<string, EnemyItemStatOverrides> roundStats = overrides?.ValidateSlots(slotIds)
                ?? new Dictionary<string, EnemyItemStatOverrides>(StringComparer.Ordinal);
            float attack = overrides?.ResolveAttack(_attack) ?? _attack;
            if (float.IsNaN(attack) || float.IsInfinity(attack) || attack < 0)
                throw new InvalidOperationException($"{name}: 기준 공격력은 유효한 0 이상의 수치여야 합니다.");
            var owner = new CharacterStatsSnapshot(overrides?.ResolveHealth(MaximumHealth) ?? MaximumHealth, attack);
            var grid = new BackpackGrid(BackpackWidth, BackpackHeight);
            var snapshots = new List<CombatItemSnapshot>();
            for (int index = 0; index < _items.Count; index++)
            {
                EnemyItemPlacement placement = _items[index];
                string instanceId = $"{EncounterId}:{placement.SlotId}";
                if (placement?.Item == null)
                    throw new InvalidOperationException($"{DisplayName}: enemy item {index + 1} is missing.");
                ItemShape shape;
                try { shape = placement.Item.CreateShape(); }
                catch (ArgumentException exception) { throw new InvalidOperationException($"{DisplayName}: 장비 {index + 1}의 아이템 모양이 잘못되었습니다.", exception); }
                if ((int)placement.Rotation < 0 || (int)placement.Rotation > 3)
                    throw new InvalidOperationException($"{DisplayName}: 장비 {index + 1}의 회전 값이 잘못되었습니다.");
                if (!grid.TryPlace(instanceId, shape, placement.Origin, placement.Rotation))
                    throw new InvalidOperationException($"{DisplayName}: item {index + 1} overlaps or is outside its backpack.");
                grid.TryGetOccupiedCells(instanceId, out var occupiedCells);
                WeaponStats stats = placement.Item.ResolveWeaponStats(owner);
                ItemActivationData activation = placement.Item.Activation?.Resolve(owner) ?? new ItemActivationData();
                placement.Stats?.Apply(ref stats, ref activation);
                if (roundStats.TryGetValue(placement.SlotId, out EnemyItemStatOverrides modifier))
                    modifier?.Apply(ref stats, ref activation);
                snapshots.Add(new CombatItemSnapshot(instanceId, placement.Item, placement.Origin,
                    placement.Rotation, occupiedCells, stats, activation, owner));
            }

            return new CombatantSnapshot(DisplayName, owner.MaximumHealth,
                new CombatLoadoutSnapshot(snapshots, BackpackWidth, BackpackHeight), owner.Attack);
        }

        private void OnValidate()
        {
            _maximumHealth = Mathf.Max(1, _maximumHealth);
            _items ??= new List<EnemyItemPlacement>();
        }
    }
}
