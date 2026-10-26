using BackpackAutoBattle.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeStatisticsBarHover : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        private PrototypeCombatStatisticsView _view;
        private CombatItemStatistics _item;
        private StatisticsChartMode _mode;
        public void Bind(PrototypeCombatStatisticsView view, CombatItemStatistics item, StatisticsChartMode mode)
        { _view = view; _item = item; _mode = mode; }
        public void OnPointerEnter(PointerEventData pointer) => _view.ShowBreakdown(this, _item, _mode, pointer);
        public void OnPointerMove(PointerEventData pointer) => _view.ShowBreakdown(this, _item, _mode, pointer);
        public void OnPointerExit(PointerEventData pointer) => _view.HideBreakdown(this);
        private void OnDisable() => _view?.HideBreakdown(this);
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeStatisticsTabGlyph : MaskableGraphic
    {
        public StatisticsChartMode Mode { get; set; }
        public PrototypeStatisticsTabGlyph() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect rect = GetPixelAdjustedRect(); float scale = Mathf.Min(rect.width, rect.height) / 22;
            void Polygon(params Vector2[] points)
            {
                int start = vh.currentVertCount;
                foreach (var p in points) vh.AddVert(rect.center + p * scale, color, Vector2.zero);
                for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
            }
            void Line(Vector2 a, Vector2 b)
            {
                Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * .8f;
                Polygon(a - n, a + n, b + n, b - n);
            }
            if (Mode == StatisticsChartMode.Attack)
            {
                Polygon(new(-1.5f, -2), new(1.5f, -2), new(1.5f, 6), new(0, 10), new(-1.5f, 6));
                Polygon(new(-5, -4), new(5, -4), new(5, -2), new(-5, -2));
                Polygon(new(-1, -9), new(1, -9), new(1, -4), new(-1, -4));
            }
            else
            {
                Vector2[] outline = { new(0, 8), new(7, 5), new(6, -3), new(3, -7), new(0, -9), new(-3, -7), new(-6, -3), new(-7, 5) };
                for (int i = 0; i < outline.Length; i++) Line(outline[i], outline[(i + 1) % outline.Length]);
            }
        }
    }
}
