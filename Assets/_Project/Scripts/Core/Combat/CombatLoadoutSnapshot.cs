using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Combat
{
    public sealed class CombatItemSnapshot
    {
        public CombatItemSnapshot(string instanceId, ItemData data, Vector2Int origin,
            ItemRotation rotation, IEnumerable<Vector2Int> occupiedCells)
        {
            InstanceId = instanceId;
            ItemId = data.ItemId;
            DisplayName = data.DisplayName;
            Category = data.Category;
            DisplayColor = data.DisplayColor;
            Origin = origin;
            Rotation = rotation;
            OccupiedCells = Array.AsReadOnly(occupiedCells.ToArray());
            BaseWeaponStats = data.BaseWeaponStats?.Copy() ?? new WeaponStats();
        }

        public string InstanceId { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public ItemCategory Category { get; }
        public Color DisplayColor { get; }
        public Vector2Int Origin { get; }
        public ItemRotation Rotation { get; }
        public IReadOnlyList<Vector2Int> OccupiedCells { get; }
        public WeaponStats BaseWeaponStats { get; }
    }

    public sealed class CombatLoadoutSnapshot
    {
        public CombatLoadoutSnapshot(IEnumerable<CombatItemSnapshot> items, int width = 6, int height = 6)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            Items = Array.AsReadOnly(items.OrderBy(item => item.InstanceId, StringComparer.Ordinal).ToArray());
            Width = width;
            Height = height;
        }

        public IReadOnlyList<CombatItemSnapshot> Items { get; }
        public int Width { get; }
        public int Height { get; }
    }

    public sealed class CombatantSnapshot
    {
        public CombatantSnapshot(string displayName, double maximumHealth, CombatLoadoutSnapshot loadout)
        {
            if (maximumHealth <= 0d || double.IsNaN(maximumHealth) || double.IsInfinity(maximumHealth))
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            DisplayName = displayName;
            MaximumHealth = maximumHealth;
            Loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
        }

        public string DisplayName { get; }
        public double MaximumHealth { get; }
        public CombatLoadoutSnapshot Loadout { get; }
    }
}
