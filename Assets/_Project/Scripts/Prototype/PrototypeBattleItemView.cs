using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeBattleItemView : MonoBehaviour, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private readonly List<(RectTransform rect, float bottom)> _colorFills = new();
        private Action<CombatItemSnapshot> _inspect;
        private PrototypeItemTooltip _tooltip;
        private Func<ItemTooltipContent> _tooltipContent;
        private Text _label;
        private float _itemHeight;
        private float _cellSize;
        private Coroutine _pulse;
        private Action _showPlacement;
        private Action _hidePlacement;

        public CombatItemSnapshot Snapshot { get; private set; }
        public CombatSide Side { get; private set; }
        public float CycleProgress { get; private set; }

        public void Initialize(CombatItemSnapshot item, CombatSide side, float pitch,
            Action<CombatItemSnapshot> inspect, PrototypeItemTooltip tooltip, Func<ItemTooltipContent> tooltipContent,
            Action showPlacement = null, Action hidePlacement = null)
        {
            Snapshot = item;
            Side = side;
            _inspect = inspect;
            _tooltip = tooltip;
            _tooltipContent = tooltipContent;
            _showPlacement = showPlacement;
            _hidePlacement = hidePlacement;
            gameObject.name = $"Battle Item {item.InstanceId}";
            Vector2Int[] cells = item.OccupiedCells.Select(cell => cell - item.Origin).ToArray();
            int width = cells.Max(cell => cell.x) + 1;
            int height = cells.Max(cell => cell.y) + 1;
            _cellSize = pitch - 6f;
            _itemHeight = height * pitch - 6f;
            RectTransform rect = (RectTransform)transform;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width * pitch - 6f, _itemHeight);
            bool hasCycle = item.Activation.HasCycle;
            Color.RGBToHSV(item.DisplayColor, out float hue, out float saturation, out float brightness);
            Color muted = Color.HSVToRGB(hue, saturation * 0.18f, Mathf.Lerp(brightness, 0.65f, 0.3f));
            muted.a = item.DisplayColor.a;

            foreach (Vector2Int cell in cells)
            {
                var cellObject = new GameObject($"Shape Cell {cell.x},{cell.y}", typeof(RectTransform), typeof(Image));
                cellObject.transform.SetParent(transform, false);
                RectTransform cellRect = (RectTransform)cellObject.transform;
                BackpackPrototypeController.SetRect(cellRect, new Vector2(0f, 1f), new Vector2(cell.x * pitch, -cell.y * pitch), Vector2.one * _cellSize);
                cellRect.pivot = new Vector2(0f, 1f);
                cellObject.GetComponent<Image>().color = hasCycle ? muted : item.DisplayColor;
                if (!hasCycle) continue;

                var fillObject = new GameObject("Cycle Color", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(cellObject.transform, false);
                Image fill = fillObject.GetComponent<Image>();
                fill.color = item.DisplayColor;
                fill.raycastTarget = false;
                RectTransform fillRect = fill.rectTransform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(1f, 0f);
                fillRect.pivot = new Vector2(0.5f, 0f);
                fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                float bottom = _itemHeight - cell.y * pitch - _cellSize;
                _colorFills.Add((fillRect, bottom));
            }

            _label = BackpackPrototypeController.CreateText(transform, "Label", item.DisplayName, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 9;
            _label.resizeTextMaxSize = 14;
            _label.raycastTarget = false;
            Outline outline = _label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.03f, 0.05f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
            BackpackPrototypeController.Stretch(_label.rectTransform, 3f);
        }

        public void RenderCooldown(CombatItem item, double elapsed)
        {
            CycleProgress = (float)item.Cycle.ProgressAt(elapsed);
            float fillHeight = _itemHeight * CycleProgress;
            foreach (var fill in _colorFills)
            {
                // A single horizontal frontier travels through the whole rotated item, including gaps.
                float cellProgress = Mathf.Clamp01((fillHeight - fill.bottom) / _cellSize);
                fill.rect.anchorMax = new Vector2(1f, cellProgress);
            }
        }

        public void PulseAttack(bool hit, bool critical)
        {
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = StartCoroutine(PulseLabel(hit, critical));
        }

        private IEnumerator PulseLabel(bool hit, bool critical)
        {
            _label.color = !hit ? new Color(0.62f, 0.66f, 0.73f)
                : critical ? new Color(1f, 0.8f, 0.2f) : Color.white;
            yield return new WaitForSeconds(0.18f);
            _label.color = Color.white;
            _pulse = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _inspect(Snapshot);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip != null) _tooltip.Show(this, _tooltipContent, eventData.position, eventData.enterEventCamera);
            _showPlacement?.Invoke();
        }
        public void OnPointerMove(PointerEventData eventData)
        {
            if (_tooltip != null) _tooltip.Move(this, eventData.position, eventData.enterEventCamera);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip != null) _tooltip.Hide(this);
            _hidePlacement?.Invoke();
        }
        private void OnDisable()
        {
            if (_tooltip != null) _tooltip.Hide(this);
            _hidePlacement?.Invoke();
        }
    }
}
