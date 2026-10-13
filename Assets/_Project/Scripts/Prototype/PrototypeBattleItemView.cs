using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Battle items deliberately expose inspection only, with no drag or placement handlers.
    public sealed class PrototypeBattleItemView : MonoBehaviour, IPointerClickHandler
    {
        private readonly List<Image> _cells = new();
        private Action<CombatItemSnapshot> _inspect;
        private Image _cooldownFill;
        private Text _cooldownText;
        private Coroutine _pulse;

        public CombatItemSnapshot Snapshot { get; private set; }
        public CombatSide Side { get; private set; }

        public void Initialize(CombatItemSnapshot item, CombatSide side, float pitch, Action<CombatItemSnapshot> inspect)
        {
            Snapshot = item;
            Side = side;
            _inspect = inspect;
            gameObject.name = $"Battle Item {item.InstanceId}";
            Vector2Int[] cells = item.OccupiedCells.Select(cell => cell - item.Origin).ToArray();
            int width = cells.Max(cell => cell.x) + 1;
            int height = cells.Max(cell => cell.y) + 1;
            RectTransform rect = (RectTransform)transform;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width * pitch - 6f, height * pitch - 6f);

            foreach (Vector2Int cell in cells)
            {
                var cellObject = new GameObject($"Shape Cell {cell.x},{cell.y}", typeof(RectTransform), typeof(Image));
                cellObject.transform.SetParent(transform, false);
                RectTransform cellRect = (RectTransform)cellObject.transform;
                BackpackPrototypeController.SetRect(cellRect, new Vector2(0f, 1f), new Vector2(cell.x * pitch, -cell.y * pitch), Vector2.one * (pitch - 6f));
                cellRect.pivot = new Vector2(0f, 1f);
                Image image = cellObject.GetComponent<Image>();
                image.color = item.DisplayColor;
                _cells.Add(image);
            }

            Text label = BackpackPrototypeController.CreateText(transform, "Label", item.DisplayName, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 14;
            label.raycastTarget = false;
            AddTextOutline(label);
            BackpackPrototypeController.Stretch(label.rectTransform, 3f);

            var cooldownObject = new GameObject("Cooldown", typeof(RectTransform), typeof(Image));
            cooldownObject.transform.SetParent(transform, false);
            Image background = cooldownObject.GetComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.9f);
            background.raycastTarget = false;
            RectTransform barRect = background.rectTransform;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0f, 5f);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(cooldownObject.transform, false);
            _cooldownFill = fillObject.GetComponent<Image>();
            _cooldownFill.color = new Color(0.42f, 0.87f, 1f);
            _cooldownFill.raycastTarget = false;
            BackpackPrototypeController.Stretch(_cooldownFill.rectTransform);
            _cooldownText = BackpackPrototypeController.CreateText(transform, "Cooldown Text", string.Empty, 11, FontStyle.Bold, TextAnchor.LowerRight);
            _cooldownText.raycastTarget = false;
            AddTextOutline(_cooldownText);
            BackpackPrototypeController.Stretch(_cooldownText.rectTransform, 7f);
            cooldownObject.SetActive(item.Category == BackpackAutoBattle.Items.ItemCategory.Weapon);
        }

        public void RenderCooldown(CombatWeapon weapon, double elapsed)
        {
            double remaining = Math.Max(0d, weapon.NextAttackAt - elapsed);
            float progress = Mathf.Clamp01(1f - (float)(remaining / weapon.Stats.CooldownSeconds));
            _cooldownFill.rectTransform.anchorMax = new Vector2(progress, 1f);
            _cooldownText.text = $"{remaining:0.0}s";
        }

        public void PulseAttack(bool hit, bool critical)
        {
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = StartCoroutine(Pulse(hit, critical));
        }

        private IEnumerator Pulse(bool hit, bool critical)
        {
            Color color = !hit ? new Color(0.43f, 0.45f, 0.48f) : critical ? new Color(1f, 0.80f, 0.2f) : Color.white;
            foreach (Image cell in _cells) cell.color = color;
            yield return new WaitForSeconds(0.18f);
            foreach (Image cell in _cells) cell.color = Snapshot.DisplayColor;
            _pulse = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) _inspect(Snapshot);
        }

        private static void AddTextOutline(Text text)
        {
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.03f, 0.05f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
    }
}
