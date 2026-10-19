using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        [Header("Fusion (list order is recipe priority)")]
        [SerializeField] private FusionRecipeData[] _fusionRecipes = Array.Empty<FusionRecipeData>();
        private readonly HashSet<string> _fusionLocks = new();
        private IReadOnlyList<FusionPlan> _pendingFusions = Array.Empty<FusionPlan>();
        private Component _fusionHoverOwner;
        private PrototypeFusionOverlay _fusionOverlay;
        public IReadOnlyList<FusionRecipeData> FusionRecipes => _fusionRecipes;
        public bool IsFusionLocked(string instanceId) => _fusionLocks.Contains(instanceId);
        public static bool FusionLockModifier => Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        public bool TryToggleFusionLock(string instanceId)
        {
            if (!CanEditLoadout || _draggingItem != null || string.IsNullOrEmpty(instanceId)) return false;
            ItemData data = _itemViews.TryGetValue(instanceId, out var bag) ? bag.Data
                : _storageViews.TryGetValue(instanceId, out var storage) ? storage.StoredItem.ItemData : null;
            if (data == null || !_fusionRecipes.Any(recipe => recipe != null && recipe.Contains(data))) return false;
            if (!_fusionLocks.Remove(instanceId)) _fusionLocks.Add(instanceId);
            RefreshFusionVisuals();
            return true;
        }

        public IReadOnlyList<FusionPlan> GetReadyFusions()
        {
            if (_draggingItem != null) return Array.Empty<FusionPlan>();
            var items = _itemViews.Values.Where(view => _grid.TryGetOccupiedCells(view.InstanceId, out _))
                .Select(view => new FusionItem(view.InstanceId, view.Data, view.Origin, view.Rotation, IsFusionLocked(view.InstanceId)));
            return FusionPlanner.Plan(GridSize, GridSize, items, _fusionRecipes);
        }

        private void CapturePendingFusions() => _pendingFusions = GetReadyFusions();

        private void ApplyPendingFusions()
        {
            // Validate every consumed instance and the complete final footprint before changing any inventory.
            var pending = _pendingFusions;
            if (pending.Count == 0) return;
            var consumed = new HashSet<string>(pending.SelectMany(plan => plan.Materials).Select(item => item.Id));
            var finalGrid = new BackpackGrid(GridSize, GridSize);
            foreach (var material in pending.SelectMany(plan => plan.Materials))
                if (!_itemViews.TryGetValue(material.Id, out var view) || view.Data != material.Data
                    || view.Origin != material.Origin || view.Rotation != material.Rotation)
                { Debug.LogError("합성 재료가 전투 시작 시점과 달라 합성을 보류했습니다."); return; }
            foreach (var view in _itemViews.Values.Where(view => !consumed.Contains(view.InstanceId)))
                if (!finalGrid.TryPlace(view.InstanceId, view.Shape, view.Origin, view.Rotation)) return;
            foreach (var plan in pending)
                if (!finalGrid.TryPlace(plan.Center.Id, plan.Result.CreateShape(), plan.Center.Origin, plan.Center.Rotation))
                { Debug.LogWarning("합성 결과를 놓을 공간이 없어 재료를 유지했습니다."); return; }

            _pendingFusions = Array.Empty<FusionPlan>();
            _itemTooltip.HideAll(); _fusionHoverOwner = null; ClearPlacementOverlay(); _selectedItem = null;
            foreach (string id in consumed)
            {
                var view = _itemViews[id];
                _itemViews.Remove(id); _grid.Remove(id); _fusionLocks.Remove(id);
                view.gameObject.SetActive(false); Destroy(view.gameObject);
            }
            foreach (var plan in pending)
            {
                string id = CreatePurchasedInstanceId(plan.Result);
                _grid.TryPlace(id, plan.Result.CreateShape(), plan.Center.Origin, plan.Center.Rotation);
                var view = CreateItemView(id, plan.Result, plan.Center.Origin, plan.Center.Rotation);
                _itemViews.Add(id, view); PositionItem(view);
                SetStatus($"합성 완료: {plan.Result.DisplayName}");
            }
        }

        private ItemTooltipContent PreparationTooltip(Component owner, ItemData item)
        {
            var content = ItemTooltipContent.FromItem(item);
            var recipes = _fusionRecipes.Where(recipe => recipe != null && recipe.Contains(item)).ToArray();
            if (recipes.Length == 0) return content;
            foreach (var recipe in recipes)
                content.AddDescription($"합성: {recipe.CenterItem.DisplayName} + "
                    + string.Join(" + ", recipe.Ingredients.Select(ingredient => ingredient.Item.DisplayName + (ingredient.Quantity > 1 ? $" ×{ingredient.Quantity}" : "")))
                    + $" → {recipe.ResultItem.DisplayName}");
            string id = owner is BackpackItemView bag ? bag.InstanceId : (owner as PrototypeStorageItemView)?.StoredItem.InstanceId;
            content.AddDescription(id == null ? "중심 아이템에 재료를 인접 배치하고 전투를 마치면 합성됩니다."
                : $"합성 {(IsFusionLocked(id) ? "잠금 중" : "허용")} · Shift + 우클릭으로 전환\n중심 아이템에 재료를 인접 배치하고 전투를 마치면 합성됩니다.");
            return content;
        }

        private void LateUpdate() => RefreshFusionVisuals();

        public void RefreshFusionVisuals()
        {
            if (_shopScreenRoot == null) return;
            if (_fusionOverlay == null)
            {
                var obj = new GameObject("Fusion Connections", typeof(RectTransform), typeof(PrototypeFusionOverlay));
                obj.transform.SetParent(_shopScreenRoot, false);
                _fusionOverlay = obj.GetComponent<PrototypeFusionOverlay>();
                Stretch(_fusionOverlay.rectTransform); _fusionOverlay.raycastTarget = false;
            }
            _fusionOverlay.transform.SetAsLastSibling();
            _fusionOverlay.Begin();
            if (!CanEditLoadout || _draggingItem != null) { _fusionOverlay.End(); return; }
            var nodes = new List<(Component owner, ItemData data, string id, RectTransform rect)>();
            foreach (var view in _itemViews.Values) nodes.Add((view, view.Data, view.InstanceId, view.RectTransform));
            foreach (var view in _storageViews.Values) nodes.Add((view, view.StoredItem.ItemData, view.StoredItem.InstanceId, (RectTransform)view.transform));
            foreach (var slot in _shopSlots.Where(slot => slot.CanPurchase && slot.gameObject.activeInHierarchy))
                nodes.Add((slot, slot.ItemData, null, slot.ShapeRect));
            Vector2 Point(RectTransform rect) => _fusionOverlay.rectTransform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            foreach (var node in nodes.Where(node => node.id != null && IsFusionLocked(node.id)))
                _fusionOverlay.AddLock(_fusionOverlay.rectTransform.InverseTransformPoint(node.rect.TransformPoint(
                    new Vector2(node.rect.rect.xMax - 10f, node.rect.rect.yMax - 12f))));
            var hovered = nodes.Find(node => node.owner == _fusionHoverOwner);
            var plans = GetReadyFusions();
            bool ReadyPair(string first, string second) => first != null && second != null && plans.Any(plan =>
                (plan.Center.Id == first && plan.Materials.Skip(1).Any(item => item.Id == second))
                || (plan.Center.Id == second && plan.Materials.Skip(1).Any(item => item.Id == first)));
            if (hovered.owner != null)
                foreach (var node in nodes)
                    if (node.owner != hovered.owner && !ReadyPair(hovered.id, node.id)
                        && _fusionRecipes.Any(recipe => IsRecipePair(recipe, hovered.data, node.data)))
                        _fusionOverlay.AddLink(Point(hovered.rect), Point(node.rect), false);
            foreach (var plan in plans)
            {
                Vector2 center = Point(_itemViews[plan.Center.Id].RectTransform), label = Vector2.zero;
                foreach (var item in plan.Materials.Skip(1))
                {
                    Vector2 end = Point(_itemViews[item.Id].RectTransform);
                    _fusionOverlay.AddLink(center, end, true); label += (center + end) * .5f;
                }
                _fusionOverlay.AddLabel(label / (plan.Materials.Count - 1), plan.Result.DisplayName);
            }
            _fusionOverlay.End();
        }

        private static bool IsRecipePair(FusionRecipeData recipe, ItemData first, ItemData second)
        {
            if (recipe == null || !recipe.Contains(first) || !recipe.Contains(second)) return false;
            if (first != second) return true;
            long count = recipe.CenterItem == first ? 1 : 0;
            foreach (var ingredient in recipe.Ingredients) if (ingredient.Item == first) count += ingredient.Quantity;
            return count >= 2;
        }
    }
}
