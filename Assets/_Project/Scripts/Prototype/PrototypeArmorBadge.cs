using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Presentation-only armor badge. Zero armor hides the shield and its number together.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeArmorBadge : MaskableGraphic
    {
        private Text _value;
        public PrototypeArmorBadge() { useLegacyMeshGeneration = false; }

        public static PrototypeArmorBadge Create(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(PrototypeArmorBadge));
            obj.transform.SetParent(parent, false);
            PrototypeArmorBadge badge = obj.GetComponent<PrototypeArmorBadge>();
            BackpackPrototypeController.SetRect(badge.rectTransform, Vector2.one * 0.5f,
                new Vector2(-140f, -214f), new Vector2(44f, 48f));
            badge.color = new Color(0.02f, 0.66f, 0.93f);
            badge.raycastTarget = false;
            badge._value = BackpackPrototypeController.CreateText(obj.transform, "Armor Value", string.Empty,
                17, FontStyle.Bold, TextAnchor.MiddleCenter);
            badge._value.raycastTarget = false;
            badge._value.resizeTextForBestFit = true;
            badge._value.resizeTextMinSize = 10;
            badge._value.resizeTextMaxSize = 17;
            BackpackPrototypeController.Stretch(badge._value.rectTransform, 6f);
            badge.SetValue(0d);
            return badge;
        }

        public void SetValue(double armor)
        {
            bool visible = armor > 0d && !double.IsNaN(armor) && !double.IsInfinity(armor);
            _value.text = visible ? $"{armor:0.#}" : string.Empty;
            gameObject.SetActive(visible);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2[] outline =
            {
                new(0f, 0.46f), new(0.46f, 0.27f), new(0.45f, 0f),
                new(0.40f, -0.19f), new(0.29f, -0.34f), new(0.14f, -0.44f),
                new(0f, -0.49f), new(-0.14f, -0.44f), new(-0.29f, -0.34f),
                new(-0.40f, -0.19f), new(-0.45f, 0f), new(-0.46f, 0.27f)
            };
            vh.AddVert(rect.center, color, Vector2.zero);
            foreach (Vector2 point in outline)
                vh.AddVert(rect.center + Vector2.Scale(point, rect.size), color, Vector2.zero);
            for (int index = 0; index < outline.Length; index++)
                vh.AddTriangle(0, index + 1, (index + 1) % outline.Length + 1);
        }
    }
}
