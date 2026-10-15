using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>White code-native glyph; the stack label belongs to the slot, not the glyph.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeStatusGlyph : MaskableGraphic
    {
        public StatusEffectKind Kind { get; set; }
        public PrototypeStatusGlyph() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) / 20f;
            void Line(Vector2 start, Vector2 end, float thickness = 1.7f)
            {
                Vector2 normal = new Vector2(-(end - start).y, (end - start).x).normalized * thickness / 2f;
                int offset = vh.currentVertCount;
                foreach (Vector2 point in new[] { start - normal, start + normal, end + normal, end - normal })
                    vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                vh.AddTriangle(offset, offset + 1, offset + 2);
                vh.AddTriangle(offset, offset + 2, offset + 3);
            }
            if (Kind == StatusEffectKind.Haste)
            {
                // Three overlapping feathers share a tapered root; transparent gaps separate the tips.
                void Feather(params Vector2[] points)
                {
                    int offset = vh.currentVertCount;
                    foreach (Vector2 point in points) vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                    for (int index = 1; index < points.Length - 1; index++) vh.AddTriangle(offset, offset + index, offset + index + 1);
                }
                Feather(new(-8f, -7f), new(-6f, 1f), new(0f, 6f), new(9f, 9f), new(5f, 3f), new(-1f, -2f));
                Feather(new(-8f, -7f), new(-2f, -2f), new(6f, 1f), new(4f, -4f), new(0f, -6f));
                Feather(new(-8f, -7f), new(-3f, -4f), new(3f, -3f), new(1f, -8f), new(-3f, -9f));
            }
            else
            {
                for (int index = 0; index < 6; index++)
                {
                    float angle = index * Mathf.PI / 3f;
                    Vector2 axis = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 normal = new(-axis.y, axis.x);
                    Line(Vector2.zero, axis * 8f, 1.4f);
                    Line(axis * 4.5f, axis * 6.5f + normal * 2f, 1.3f);
                    Line(axis * 4.5f, axis * 6.5f - normal * 2f, 1.3f);
                }
            }
        }
    }

    public sealed class PrototypeStatusIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        public const float Size = 42f;
        private CombatSimulation _combat;
        private PrototypeItemTooltip _tooltip;
        private Text _count;
        public CombatSide Side { get; private set; }
        public StatusEffectKind Kind { get; private set; }
        public int Stacks => _combat.GetStatusStacks(Side, Kind);
        public bool IsDebuff => _combat.GetStatusDefinition(Kind).IsDebuff;

        public static PrototypeStatusIcon Create(Transform parent, CombatSimulation combat, CombatSide side,
            StatusEffectKind kind, PrototypeItemTooltip tooltip)
        {
            var obj = new GameObject($"{side} {kind} Status", typeof(RectTransform), typeof(Image), typeof(PrototypeStatusIcon));
            obj.transform.SetParent(parent, false);
            PrototypeStatusIcon icon = obj.GetComponent<PrototypeStatusIcon>();
            icon._combat = combat; icon._tooltip = tooltip; icon.Side = side; icon.Kind = kind;
            Image background = obj.GetComponent<Image>();
            background.color = Color.clear; // Invisible hover target; only the white glyph and stack count are drawn.
            background.raycastTarget = true;
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = Vector2.one * Size;
            var glyphObject = new GameObject("Status Glyph", typeof(RectTransform), typeof(PrototypeStatusGlyph));
            glyphObject.transform.SetParent(obj.transform, false);
            PrototypeStatusGlyph glyph = glyphObject.GetComponent<PrototypeStatusGlyph>();
            glyph.Kind = kind; glyph.color = Color.white; glyph.raycastTarget = false;
            BackpackPrototypeController.Stretch(glyph.rectTransform, 6f);
            icon._count = BackpackPrototypeController.CreateText(obj.transform, "Status Stacks", string.Empty, 20,
                FontStyle.Bold, TextAnchor.LowerRight);
            icon._count.raycastTarget = false;
            icon._count.resizeTextForBestFit = true;
            icon._count.resizeTextMinSize = 12;
            icon._count.resizeTextMaxSize = 20;
            BackpackPrototypeController.Stretch(icon._count.rectTransform, 1.5f);
            Outline outline = icon._count.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.03f, 0.05f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            icon.Refresh();
            return icon;
        }

        public void Refresh()
        {
            _count.text = Stacks.ToString();
            gameObject.SetActive(Stacks > 0);
        }
        public void OnPointerEnter(PointerEventData pointer)
            => _tooltip.ShowStatus(this, () => StatusTooltipContent.FromCombat(_combat, Side, Kind), pointer.position, pointer.enterEventCamera);
        public void OnPointerMove(PointerEventData pointer) => _tooltip.Move(this, pointer.position, pointer.enterEventCamera);
        public void OnPointerExit(PointerEventData pointer) => _tooltip.Hide(this);
        private void OnDisable() { if (_tooltip != null) _tooltip.Hide(this); }
    }
}
