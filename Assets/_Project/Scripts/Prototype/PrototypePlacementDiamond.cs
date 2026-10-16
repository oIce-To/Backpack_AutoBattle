using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypePlacementDiamond : MaskableGraphic
    {
        public static readonly Color Yellow = new(1f, 0.88f, 0f);
        public Vector2Int Cell { get; private set; }
        public bool IsFilled { get; private set; }
        public PrototypePlacementDiamond() { useLegacyMeshGeneration = false; }
        public void Set(Vector2Int cell, bool filled)
        { Cell = cell; IsFilled = filled; color = Yellow; raycastTarget = false; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float inner = Mathf.Max(0f, radius - 2.2f);
            Vector2[] points = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
            if (IsFilled)
            {
                foreach (Vector2 point in points) vh.AddVert(rect.center + point * radius, color, Vector2.zero);
                vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
            }
            else
            {
                foreach (Vector2 point in points)
                { vh.AddVert(rect.center + point * radius, color, Vector2.zero); vh.AddVert(rect.center + point * inner, color, Vector2.zero); }
                for (int index = 0; index < 4; index++)
                { int next = (index + 1) % 4 * 2; vh.AddTriangle(index * 2, next, next + 1); vh.AddTriangle(index * 2, next + 1, index * 2 + 1); }
            }
        }
    }
}
