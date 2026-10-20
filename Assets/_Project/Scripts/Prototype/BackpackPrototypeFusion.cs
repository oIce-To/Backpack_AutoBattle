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
        private PrototypeFusionAnimation _fusionAnimation;
        public bool IsFusionPresenting => _fusionAnimation != null;
        public bool AutoTickFusionPresentation { get; set; } = true;
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

        private void BeginFusionPresentation()
        {
            // Commit inventory once, then animate independent visual copies. Interrupting a presentation
            // only reveals the already-owned outputs; it can never repeat consumption or lose a result.
            var pending = _pendingFusions;
            if (pending.Count == 0) return;
            var consumed = new HashSet<string>(pending.SelectMany(plan => plan.Materials).Select(item => item.Id));
            if (consumed.Count != pending.Sum(plan => plan.Materials.Count))
            { Debug.LogError("합성 재료가 중복되어 합성을 보류했습니다."); return; }
            foreach (var material in pending.SelectMany(plan => plan.Materials))
                if (!_itemViews.TryGetValue(material.Id, out var view) || view.Data != material.Data
                    || view.Origin != material.Origin || view.Rotation != material.Rotation)
                { Debug.LogError("합성 재료가 전투 시작 시점과 달라 합성을 보류했습니다."); return; }
            var jobs = new List<PrototypeFusionAnimation.Job>();
            foreach (var plan in pending)
            {
                var materials = plan.Materials.Select(item =>
                {
                    RectTransform rect = _itemViews[item.Id].RectTransform;
                    Vector2 position = _shopScreenRoot.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                    return new PrototypeFusionAnimation.Material(item.Data, item.Rotation, position);
                }).ToArray();
                Vector2 positionInStorage = FindFusionStoragePosition(plan.Result, plan.Center.Rotation);
                string id = CreatePurchasedInstanceId(plan.Result);
                var stored = new PrototypeStoredItem(id, plan.Result, positionInStorage, plan.Center.Rotation);
                _storedItems.Add(stored); CreateStorageView(stored);
                var storedView = _storageViews[id];
                storedView.gameObject.SetActive(false);
                Vector2 destination = _shopScreenRoot.InverseTransformPoint(_storageRoot.TransformPoint(positionInStorage));
                jobs.Add(new PrototypeFusionAnimation.Job(materials, plan.Result, plan.Center.Rotation, destination, storedView));
            }
            _pendingFusions = Array.Empty<FusionPlan>();
            _itemTooltip.HideAll(); _fusionHoverOwner = null; ClearPlacementOverlay(); _selectedItem = null;
            foreach (string id in consumed)
            {
                var view = _itemViews[id];
                _itemViews.Remove(id); _grid.Remove(id); _fusionLocks.Remove(id);
                view.gameObject.SetActive(false); Destroy(view.gameObject);
            }
            _run.Phase = PrototypePhase.Fusion;
            _fusionOverlay?.Clear();
            _fusionAnimation = new PrototypeFusionAnimation(_shopScreenRoot, jobs);
        }

        // Uses continuous UI positions, not backpack cells. Existing storage items never move.
        private Vector2 FindFusionStoragePosition(ItemData result, ItemRotation rotation)
        {
            var cells = result.CreateShape().GetCells(rotation);
            Vector2 half = new Vector2(cells.Max(cell => cell.x) + 1, cells.Max(cell => cell.y) + 1) * CellPitch * .5f;
            Rect rect = _storageRoot.rect;
            float left = ClampStorageAxis(rect.xMin + half.x + 8f, rect.xMin + half.x + 8f, rect.xMax - half.x - 8f);
            float right = ClampStorageAxis(rect.xMax - half.x - 8f, rect.xMin + half.x + 8f, rect.xMax - half.x - 8f);
            float bottom = ClampStorageAxis(rect.yMin + half.y + 8f, rect.yMin + half.y + 8f, rect.yMax - half.y - 32f);
            float top = ClampStorageAxis(rect.yMax - half.y - 32f, rect.yMin + half.y + 8f, rect.yMax - half.y - 32f);
            int columns = Mathf.Max(1, Mathf.CeilToInt((right - left) / 18f));
            int rows = Mathf.Max(1, Mathf.CeilToInt((top - bottom) / 18f));
            Vector2 best = new(left, top);
            float leastOverlap = float.PositiveInfinity;
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    Vector2 position = new(Mathf.Lerp(left, right, (float)x / columns), Mathf.Lerp(top, bottom, (float)y / rows));
                    Rect candidate = new(position - half, half * 2f);
                    float overlap = 0f;
                    foreach (var view in _storageViews.Values)
                    {
                        Vector2 size = ((RectTransform)view.transform).rect.size;
                        Rect other = new(view.StoredItem.StoragePosition - size * .5f, size);
                        overlap += Mathf.Max(0, Mathf.Min(candidate.xMax, other.xMax) - Mathf.Max(candidate.xMin, other.xMin))
                            * Mathf.Max(0, Mathf.Min(candidate.yMax, other.yMax) - Mathf.Max(candidate.yMin, other.yMin));
                    }
                    if (overlap >= leastOverlap) continue;
                    best = position; leastOverlap = overlap;
                    if (overlap == 0f) return best;
                }
            return best; // Unlimited freeform storage: use the least covered location when crowded.
        }

        public void TickFusionPresentation(float deltaSeconds)
        {
            if (_fusionAnimation == null) return;
            if (_fusionAnimation.Tick(deltaSeconds)) CompleteFusionPresentation();
        }

        private void CompleteFusionPresentation()
        {
            if (_fusionAnimation == null) return;
            _fusionAnimation.Dispose(); _fusionAnimation = null;
            _run.Phase = PrototypePhase.Preparation;
            if (_shopScreenRoot != null && _battleController != null) RefreshCombatInterface();
        }

        private void OnDisable() => CompleteFusionPresentation();

        private void LateUpdate() => RefreshFusionVisuals();

        public void RefreshFusionVisuals()
        {
            if (_shopScreenRoot == null) return;
            foreach (var view in _itemViews.Values)
                PrototypeItemShapeVisual.SetFusionLocked(view.RectTransform, IsFusionLocked(view.InstanceId));
            foreach (var view in _storageViews.Values)
                PrototypeItemShapeVisual.SetFusionLocked((RectTransform)view.transform, IsFusionLocked(view.StoredItem.InstanceId));
            if (_fusionOverlay == null)
            {
                var obj = new GameObject("Fusion Connections", typeof(RectTransform), typeof(PrototypeFusionOverlay));
                obj.transform.SetParent(_shopScreenRoot, false);
                _fusionOverlay = obj.GetComponent<PrototypeFusionOverlay>();
                Stretch(_fusionOverlay.rectTransform); _fusionOverlay.raycastTarget = false;
            }
            if (!CanEditLoadout || _draggingItem != null) { _fusionOverlay.Clear(); return; }
            _fusionOverlay.transform.SetAsLastSibling();
            _fusionOverlay.Begin();
            var nodes = new List<(Component owner, ItemData data, string id, RectTransform rect)>();
            foreach (var view in _itemViews.Values) nodes.Add((view, view.Data, view.InstanceId, view.RectTransform));
            foreach (var view in _storageViews.Values) nodes.Add((view, view.StoredItem.ItemData, view.StoredItem.InstanceId, (RectTransform)view.transform));
            foreach (var slot in _shopSlots.Where(slot => slot.CanPurchase && slot.gameObject.activeInHierarchy))
                nodes.Add((slot, slot.ItemData, null, slot.ShapeRect));
            Vector2 Point(RectTransform rect) => _fusionOverlay.rectTransform.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            var hovered = nodes.Find(node => node.owner == _fusionHoverOwner);
            var plans = GetReadyFusions();
            bool ReadyPair(string first, string second) => first != null && second != null && plans.Any(plan =>
                (plan.Center.Id == first && plan.Materials.Skip(1).Any(item => item.Id == second))
                || (plan.Center.Id == second && plan.Materials.Skip(1).Any(item => item.Id == first)));
            if (hovered.owner != null)
                foreach (var node in nodes)
                    if (node.owner != hovered.owner && !ReadyPair(hovered.id, node.id)
                        && _fusionRecipes.Any(recipe => IsRecipePair(recipe, hovered.data, node.data)))
                        _fusionOverlay.AddLink(Point(hovered.rect), Point(node.rect), false, hovered.owner.GetInstanceID(), node.owner.GetInstanceID());
            foreach (var plan in plans)
            {
                Vector2 center = Point(_itemViews[plan.Center.Id].RectTransform), label = Vector2.zero;
                foreach (var item in plan.Materials.Skip(1))
                {
                    Vector2 end = Point(_itemViews[item.Id].RectTransform);
                    _fusionOverlay.AddLink(center, end, true, _itemViews[plan.Center.Id].GetInstanceID(), _itemViews[item.Id].GetInstanceID());
                    label += PrototypeFusionOverlay.CurvePoint(center, end, .5f);
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
