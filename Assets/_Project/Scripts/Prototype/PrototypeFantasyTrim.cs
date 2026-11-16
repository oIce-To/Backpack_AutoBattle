using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Geometry rather than a stretched texture keeps stitches and wood grain at reference-pixel size.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeFantasyTrim : MaskableGraphic
    {
        internal bool Wood;
        public PrototypeFantasyTrim() { useLegacyMeshGeneration = false; }
        internal static void Add(Image panel, bool wood)
        {
            var obj = new GameObject(wood ? "Woodwork" : "Leatherwork", typeof(RectTransform), typeof(PrototypeFantasyTrim));
            obj.transform.SetParent(panel.transform, false);
            BackpackPrototypeController.Stretch((RectTransform)obj.transform);
            obj.GetComponent<PrototypeFantasyTrim>().Wood = wood;
            obj.GetComponent<PrototypeFantasyTrim>().raycastTarget = false;
        }
        internal static void Quad(VertexHelper vh, Rect r, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin, r.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMin), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            void Box(float x, float y, float w, float h, uint c) => Quad(vh, new Rect(x, y, w, h), PrototypePixelSkin.Hex(c));
            if (Wood)
            {
                for (float y = r.yMin + 18; y < r.yMax - 16; y += 64)
                {
                    Box(r.xMin + 10, y, r.width - 20, 2, 0x302018);
                    Box(r.xMin + 10, y + 2, r.width - 20, 1, 0x82603d);
                    for (int n = 0; n < 3; n++)
                    {
                        if (y + 18 + n * 6 >= r.yMax - 12) continue;
                        float x = r.xMin + 16 + n * 94;
                        Box(x, y + 15 + n * 6, 48, 1, 0x59402b);
                        Box(x + 12, y + 17 + n * 6, 30, 1, 0x795536);
                    }
                }
                foreach (float x in new[] { r.xMin + 5, r.xMax - 13 })
                {
                    Box(x, r.yMin + 10, 8, r.height - 20, 0x8a6039);
                    Box(x + 5, r.yMin + 10, 3, r.height - 20, 0x422d21);
                    foreach (float y in new[] { r.yMin + 16, r.yMax - 22 })
                    { Box(x, y, 6, 6, 0x29221c); Box(x + 1, y + 2, 3, 3, 0xb09662); }
                }
                // Carved sign edge below the shop's title and purse.
                Box(r.xMin + 22, r.yMax - 49, r.width - 44, 3, 0x302018);
                Box(r.xMin + 22, r.yMax - 46, r.width - 44, 1, 0xa07747);
                return;
            }
            // Raised carrying loop, open center, leather highlights.
            Box(-34, r.yMax - 4, 12, 18, 0x281c17); Box(22, r.yMax - 4, 12, 18, 0x281c17);
            Box(-28, r.yMax + 12, 56, 7, 0x281c17); Box(-26, r.yMax + 13, 52, 3, 0xb0804b);
            Box(-31, r.yMax, 6, 13, 0x906039); Box(25, r.yMax, 6, 13, 0x906039);
            // Saddle stitching, kept away from the inventory's hit cells.
            for (float x = r.xMin + 22; x < r.xMax - 20; x += 10)
            { Box(x, r.yMin + 6, 4, 2, 0xb99361); Box(x, r.yMax - 8, 4, 2, 0xb99361); }
            foreach (float x in new[] { r.xMin + 6, r.xMax - 8 })
                for (float y = r.yMin + 22; y < r.yMax - 20; y += 10) Box(x, y, 2, 4, 0xb99361);
            foreach (float x in new[] { r.xMin + 1, r.xMax - 13 })
                foreach (float y in new[] { r.yMin + 45, r.yMax - 73 })
                {
                    Box(x, y - 7, 12, 42, 0x2d2019); Box(x + 2, y - 5, 8, 38, 0x97613b);
                    Box(x - 1, y + 4, 14, 20, 0x352819); Box(x + 1, y + 6, 10, 16, 0xc7a263);
                    Box(x + 3, y + 9, 6, 10, 0x493121); Box(x + 6, y + 8, 2, 12, 0xe3c788);
                }
            foreach (float x in new[] { r.xMin + 5, r.xMax - 17 })
                foreach (float y in new[] { r.yMin + 5, r.yMax - 17 })
                { Box(x, y, 12, 12, 0x3c2a1b); Box(x + 2, y + 2, 8, 8, 0xb58b4e); Box(x + 4, y + 6, 4, 2, 0xf0ce85); }
        }
    }
}
