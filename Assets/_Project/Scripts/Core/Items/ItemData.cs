using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Inventory;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    [CreateAssetMenu(menuName = "Backpack AutoBattle/Item", fileName = "Item_")]
    public sealed class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _itemId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField, TextArea] private string _description = string.Empty;
        [SerializeField] private ItemCategory _category;

        [Header("Shop")]
        [SerializeField, Min(0)] private int _purchasePrice = 1;

        [Header("Backpack")]
        [SerializeField] private Color _displayColor = Color.white;
        [SerializeField] private bool _canRotate = true;
        [SerializeField] private List<Vector2Int> _occupiedCells = new() { Vector2Int.zero };

        public string ItemId => _itemId;

        public string DisplayName => _displayName;

        public string Description => _description;

        public ItemCategory Category => _category;

        public int PurchasePrice => _purchasePrice;

        public Color DisplayColor => _displayColor;

        public bool CanRotate => _canRotate;

        public IReadOnlyList<Vector2Int> OccupiedCells => _occupiedCells;

        public ItemShape CreateShape()
        {
            return new ItemShape(_occupiedCells);
        }

        private void OnValidate()
        {
            _purchasePrice = Mathf.Max(0, _purchasePrice);

            if (_occupiedCells == null || _occupiedCells.Count == 0)
            {
                _occupiedCells = new List<Vector2Int> { Vector2Int.zero };
                return;
            }

            Vector2Int[] distinctCells = _occupiedCells.Distinct().ToArray();
            int minimumX = distinctCells.Min(cell => cell.x);
            int minimumY = distinctCells.Min(cell => cell.y);
            _occupiedCells = distinctCells
                .Select(cell => new Vector2Int(cell.x - minimumX, cell.y - minimumY))
                .OrderBy(cell => cell.y)
                .ThenBy(cell => cell.x)
                .ToList();
        }
    }
}
