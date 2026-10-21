using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Inventory;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    [Serializable]
    public sealed class ItemPlacementEffect
    {
        [Tooltip("Offsets from the unrotated footprint's top-left: X right, Y down. Negative, diagonal and distant cells are allowed.")]
        [SerializeField] private List<Vector2Int> _cells = new() { Vector2Int.left, Vector2Int.right };
        [SerializeField] private ItemTargetFilter _targetFilter = new(ItemCategory.Weapon, ItemTags.Melee);
        [SerializeField] private float _attackDamageBonus = 1f;
        [SerializeField] private StatScaling _attackScaling = new();
        public IReadOnlyList<Vector2Int> Cells => _cells;
        public ItemTargetFilter TargetFilter => _targetFilter;
        public float AttackDamageBonus => WeaponStats.FiniteOrDefault(_attackDamageBonus, 0f);
        public ItemPlacementEffect() { }
        public ItemPlacementEffect(IEnumerable<Vector2Int> cells, float bonus, ItemTargetFilter filter = null)
        { _cells = cells.Distinct().ToList(); _attackDamageBonus = bonus; _targetFilter = (filter ?? new(ItemCategory.Weapon, ItemTags.Melee)).Copy(); }
        public IEnumerable<Vector2Int> GetWorldCells(ItemShape shape, Vector2Int origin, ItemRotation rotation)
            => (_cells ?? new List<Vector2Int>()).Distinct().Select(cell => origin + shape.TransformOffset(cell, rotation));
        public ItemPlacementEffect Copy() => new(_cells ?? new List<Vector2Int>(), AttackDamageBonus, _targetFilter) { _attackScaling = _attackScaling?.Copy() };
        public ItemPlacementEffect Resolve(CharacterStatsSnapshot owner)
            => new(_cells ?? new List<Vector2Int>(), _attackScaling?.Resolve(AttackDamageBonus, owner) ?? AttackDamageBonus, _targetFilter);
    }
}
