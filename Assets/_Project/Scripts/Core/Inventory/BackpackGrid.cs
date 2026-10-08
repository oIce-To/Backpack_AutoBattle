using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackAutoBattle.Inventory
{
    /// <summary>
    /// Owns backpack occupancy and performs atomic placement validation.
    /// This class intentionally contains no MonoBehaviour or UI concerns.
    /// </summary>
    public sealed class BackpackGrid
    {
        private static readonly Vector2Int[] OrthogonalDirections =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        private readonly Dictionary<Vector2Int, string> _occupancy = new();
        private readonly Dictionary<string, Placement> _placements = new(StringComparer.Ordinal);

        public BackpackGrid(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            }

            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }

        public int ItemCount => _placements.Count;

        public bool CanPlace(
            string instanceId,
            ItemShape shape,
            Vector2Int origin,
            ItemRotation rotation = ItemRotation.None)
        {
            ValidatePlacementArguments(instanceId, shape);
            IReadOnlyList<Vector2Int> targetCells = CalculateWorldCells(shape, origin, rotation);

            foreach (Vector2Int cell in targetCells)
            {
                if (!IsInside(cell))
                {
                    return false;
                }

                if (_occupancy.TryGetValue(cell, out string occupyingItemId)
                    && !string.Equals(occupyingItemId, instanceId, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryPlace(
            string instanceId,
            ItemShape shape,
            Vector2Int origin,
            ItemRotation rotation = ItemRotation.None)
        {
            if (!CanPlace(instanceId, shape, origin, rotation))
            {
                return false;
            }

            IReadOnlyList<Vector2Int> targetCells = CalculateWorldCells(shape, origin, rotation);
            Remove(instanceId);

            var placement = new Placement(targetCells);
            _placements.Add(instanceId, placement);

            foreach (Vector2Int cell in targetCells)
            {
                _occupancy.Add(cell, instanceId);
            }

            return true;
        }

        public bool Remove(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("An item instance ID is required.", nameof(instanceId));
            }

            if (!_placements.Remove(instanceId, out Placement placement))
            {
                return false;
            }

            foreach (Vector2Int cell in placement.OccupiedCells)
            {
                _occupancy.Remove(cell);
            }

            return true;
        }

        public bool TryGetItemAt(Vector2Int cell, out string instanceId)
        {
            return _occupancy.TryGetValue(cell, out instanceId);
        }

        public bool TryGetOccupiedCells(string instanceId, out IReadOnlyList<Vector2Int> occupiedCells)
        {
            if (_placements.TryGetValue(instanceId, out Placement placement))
            {
                occupiedCells = placement.OccupiedCells;
                return true;
            }

            occupiedCells = Array.Empty<Vector2Int>();
            return false;
        }

        public IReadOnlyList<string> GetAdjacentItemIds(string instanceId)
        {
            if (!_placements.TryGetValue(instanceId, out Placement placement))
            {
                return Array.Empty<string>();
            }

            var adjacentItemIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (Vector2Int occupiedCell in placement.OccupiedCells)
            {
                foreach (Vector2Int direction in OrthogonalDirections)
                {
                    if (_occupancy.TryGetValue(occupiedCell + direction, out string adjacentItemId)
                        && !string.Equals(adjacentItemId, instanceId, StringComparison.Ordinal))
                    {
                        adjacentItemIds.Add(adjacentItemId);
                    }
                }
            }

            return adjacentItemIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0
                   && cell.x < Width
                   && cell.y >= 0
                   && cell.y < Height;
        }

        private static IReadOnlyList<Vector2Int> CalculateWorldCells(
            ItemShape shape,
            Vector2Int origin,
            ItemRotation rotation)
        {
            return shape.GetCells(rotation).Select(cell => cell + origin).ToArray();
        }

        private static void ValidatePlacementArguments(string instanceId, ItemShape shape)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("An item instance ID is required.", nameof(instanceId));
            }

            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }
        }

        private sealed class Placement
        {
            public Placement(IReadOnlyList<Vector2Int> occupiedCells)
            {
                OccupiedCells = occupiedCells;
            }

            public IReadOnlyList<Vector2Int> OccupiedCells { get; }
        }
    }
}
