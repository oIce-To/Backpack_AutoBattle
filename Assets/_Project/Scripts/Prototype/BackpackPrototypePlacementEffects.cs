using System.Linq;
using BackpackAutoBattle.Combat;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private PrototypePlacementOverlay _placementOverlay;
        private Component _placementHoverOwner;
        private bool _dragRangeInsideGrid;

        private PrototypePlacementOverlay PlacementOverlay => _placementOverlay ??= new(_gridRoot, CellPitch);

        private void ShowPreparationPlacement(Component owner)
        {
            _placementHoverOwner = owner;
            RefreshPlacementHover();
        }

        private void RefreshPlacementHover()
        {
            if (_placementHoverOwner is BackpackItemView item && _grid.TryGetOccupiedCells(item.InstanceId, out _))
            {
                CombatLoadoutSnapshot loadout = CaptureCombatSnapshot();
                PlacementOverlay.Show(item, loadout, loadout.Items.Single(entry => entry.InstanceId == item.InstanceId));
            }
            else _placementOverlay?.Hide();
        }

        private void RefreshDragPlacementOverlay()
        {
            if (_draggingItem == null || !_dragRangeInsideGrid) { _placementOverlay?.Hide(); return; }
            BackpackItemView item = _draggingItem;
            var source = new CombatItemSnapshot(item.InstanceId, item.Data, _previewOrigin, item.Rotation,
                item.Shape.GetCells(item.Rotation).Select(cell => cell + _previewOrigin), ownerStats: _run.PlayerStats);
            CombatLoadoutSnapshot current = CaptureCombatSnapshot();
            // Remove the dragged instance's OLD footprint so its previous position never acts as a ghost target.
            var targets = new CombatLoadoutSnapshot(current.Items.Where(entry => entry.InstanceId != item.InstanceId), GridSize, GridSize);
            bool valid = _grid.CanPlace(item.InstanceId, item.Shape, _previewOrigin, item.Rotation)
                && (_dragSource != DragSource.Shop || CanPurchaseDraggedOffer());
            PlacementOverlay.Show(item, targets, source, !valid);
        }

        private void ClearPlacementOverlay()
        { _placementHoverOwner = null; _placementOverlay?.Hide(); }
    }
}
