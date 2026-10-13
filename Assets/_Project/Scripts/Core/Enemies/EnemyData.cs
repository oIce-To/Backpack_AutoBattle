using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Enemies
{
    [Serializable]
    public sealed class EnemyItemPlacement
    {
        [SerializeField] private ItemData _item;
        [SerializeField] private Vector2Int _origin;
        [SerializeField] private ItemRotation _rotation;

        public ItemData Item => _item;
        public Vector2Int Origin => _origin;
        public ItemRotation Rotation => _rotation;
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Enemy", fileName = "Enemy_")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string _enemyId = "goblin";
        [SerializeField] private string _displayName = "고블린";
        [SerializeField, Min(1)] private int _maximumHealth = 30;
        [SerializeField, Min(1)] private int _backpackWidth = 3;
        [SerializeField, Min(1)] private int _backpackHeight = 3;
        [SerializeField] private List<EnemyItemPlacement> _items = new();

        public string EnemyId => _enemyId;
        public string DisplayName => _displayName;
        public int MaximumHealth => _maximumHealth;
        public int BackpackWidth => _backpackWidth;
        public int BackpackHeight => _backpackHeight;
        public IReadOnlyList<EnemyItemPlacement> Items => _items;

        public CombatantSnapshot CreateCombatantSnapshot()
        {
            var grid = new BackpackGrid(BackpackWidth, BackpackHeight);
            var snapshots = new List<CombatItemSnapshot>();
            for (int index = 0; index < _items.Count; index++)
            {
                EnemyItemPlacement placement = _items[index];
                string instanceId = $"{EnemyId}_item_{index}";
                if (placement?.Item == null)
                    throw new InvalidOperationException($"{DisplayName}: enemy item {index + 1} is missing.");
                if (!grid.TryPlace(instanceId, placement.Item.CreateShape(), placement.Origin, placement.Rotation))
                    throw new InvalidOperationException($"{DisplayName}: item {index + 1} overlaps or is outside its backpack.");
                grid.TryGetOccupiedCells(instanceId, out var occupiedCells);
                snapshots.Add(new CombatItemSnapshot(instanceId, placement.Item, placement.Origin,
                    placement.Rotation, occupiedCells));
            }

            return new CombatantSnapshot(DisplayName, MaximumHealth,
                new CombatLoadoutSnapshot(snapshots, BackpackWidth, BackpackHeight));
        }

        private void OnValidate()
        {
            _maximumHealth = Mathf.Max(1, _maximumHealth);
            _backpackWidth = Mathf.Max(1, _backpackWidth);
            _backpackHeight = Mathf.Max(1, _backpackHeight);
            _items ??= new List<EnemyItemPlacement>();
        }
    }
}
