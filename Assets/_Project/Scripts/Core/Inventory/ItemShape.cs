using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BackpackAutoBattle.Inventory
{
    /// <summary>
    /// Immutable item footprint expressed as cells relative to a top-left origin.
    /// </summary>
    public sealed class ItemShape
    {
        private readonly Vector2Int[] _baseCells;

        public ItemShape(IEnumerable<Vector2Int> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            Vector2Int[] sourceCells = cells.Distinct().ToArray();
            if (sourceCells.Length == 0)
            {
                throw new ArgumentException("An item shape needs at least one cell.", nameof(cells));
            }

            _baseCells = Normalize(sourceCells);
        }

        public IReadOnlyList<Vector2Int> BaseCells => _baseCells;

        public IReadOnlyList<Vector2Int> GetCells(ItemRotation rotation)
        {
            Vector2Int[] rotatedCells = new Vector2Int[_baseCells.Length];
            int quarterTurns = (int)rotation;

            for (int index = 0; index < _baseCells.Length; index++)
            {
                Vector2Int cell = _baseCells[index];

                for (int turn = 0; turn < quarterTurns; turn++)
                {
                    // Backpack rows grow downward from a top-left origin.
                    cell = new Vector2Int(-cell.y, cell.x);
                }

                rotatedCells[index] = cell;
            }

            return Normalize(rotatedCells);
        }

        private static Vector2Int[] Normalize(IReadOnlyList<Vector2Int> cells)
        {
            int minimumX = cells.Min(cell => cell.x);
            int minimumY = cells.Min(cell => cell.y);

            return cells
                .Select(cell => new Vector2Int(cell.x - minimumX, cell.y - minimumY))
                .OrderBy(cell => cell.y)
                .ThenBy(cell => cell.x)
                .ToArray();
        }
    }
}
