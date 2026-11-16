using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Presentation-only armor badge. Zero armor hides the shield and its number together.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeArmorBadge : MaskableGraphic
    {
        // Upper-left highlights and a lower-right shadow bevel the shield around the darker face.
        private static readonly string[] Pixels =
        {
            "..........##..........",
            "........##hl##........",
            "......##hlmsbl##......",
            "....##hlbmmsssbl##....",
            "..##hlbbbmmsssssbl##..",
            ".#hlbbbbbmmsssssssbl#.",
            ".#hlbbbbbmmsssssssbb#.",
            ".#hlbbbbbmmsssssssdb#.",
            ".#llmmmmmmmsssssssdb#.",
            ".#llmmmmmmmsssssssdb#.",
            ".#hlmmmmmmmsssssssdd#.",
            ".#lbmmmmmmmsssssssdd#.",
            ".#lbmmmmmmmsssssssdd#.",
            "..#lbmmmmmmssssssdd#..",
            "..#lbmmmmmmssssssdd#..",
            "...#lbmmmmmsssssdd#...",
            "...#lbmmmmmsssssdd#...",
            "....#bbmmmmssssdd#....",
            ".....#bbmmmsssdd#.....",
            "......#bbmmssdd#......",
            ".......#bbmsdd#.......",
            "........#bbdd#........",
            ".........#bd#.........",
            "..........##.........."
        };

        private Text _value;
        public PrototypeArmorBadge() { useLegacyMeshGeneration = false; }

        public static PrototypeArmorBadge Create(Transform parent, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(PrototypeArmorBadge));
            obj.transform.SetParent(parent, false);
            PrototypeArmorBadge badge = obj.GetComponent<PrototypeArmorBadge>();
            BackpackPrototypeController.SetRect(badge.rectTransform, Vector2.one * 0.5f,
                new Vector2(-140f, -214f), new Vector2(44f, 48f));
            badge.raycastTarget = false;
            badge._value = BackpackPrototypeController.CreateText(obj.transform, "Armor Value", string.Empty,
                17, FontStyle.Bold, TextAnchor.MiddleCenter);
            badge._value.raycastTarget = false;
            badge._value.resizeTextForBestFit = true;
            badge._value.resizeTextMinSize = 10;
            badge._value.resizeTextMaxSize = 17;
            var outline = badge._value.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(12, 25, 40, 230);
            outline.effectDistance = new Vector2(1f, -1f);
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

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            int width = Pixels[0].Length;
            int height = Pixels.Length;
            float pixel = Mathf.Min(rect.width / width, rect.height / height);
            Vector2 topLeft = rect.center + new Vector2(-width, height) * (pixel * .5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char shade = Pixels[y][x];
                    if (shade == '.') continue;
                    Color32 tint = shade switch
                    {
                        'h' => new Color32(156, 234, 253, 255),
                        'l' => new Color32(100, 193, 226, 255),
                        'b' => new Color32(64, 139, 181, 255),
                        'm' => new Color32(45, 113, 153, 255),
                        's' => new Color32(36, 92, 130, 255),
                        'd' => new Color32(27, 65, 94, 255),
                        _ => new Color32(12, 25, 40, 255)
                    };
                    Vector2 corner = topLeft + new Vector2(x, -y) * pixel;
                    int start = mesh.currentVertCount;
                    mesh.AddVert(corner, tint, Vector2.zero);
                    mesh.AddVert(corner + new Vector2(pixel, 0), tint, Vector2.zero);
                    mesh.AddVert(corner + new Vector2(pixel, -pixel), tint, Vector2.zero);
                    mesh.AddVert(corner + new Vector2(0, -pixel), tint, Vector2.zero);
                    mesh.AddTriangle(start, start + 1, start + 2);
                    mesh.AddTriangle(start, start + 2, start + 3);
                }
            }
        }
    }
}
