using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeLifeHearts : MaskableGraphic
    {
        private static readonly string[] Pixels =
        {
            "..###...###..",
            ".#hrr#.#rrs#.",
            "#hrrrr#rrrrs#",
            "#hrrrrrrrrrs#",
            "#rrrrrrrrrss#",
            ".#rrrrrrrss#.",
            "..#rrrrrss#..",
            "...#rrrss#...",
            "....#rss#....",
            ".....#s#.....",
            "......#......"
        };

        public PrototypeLifeHearts() { useLegacyMeshGeneration = false; }
        public int Lives { get; private set; }
        public int Capacity { get; private set; }

        internal void SetLives(int lives, int capacity)
        {
            if (Lives == lives && Capacity == capacity) return;
            Lives = lives;
            Capacity = capacity;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Capacity <= 0) return;
            Rect rect = GetPixelAdjustedRect();
            float pixel = Mathf.Min(2f, Mathf.Floor(rect.width / (Capacity * 16f - 3f)));
            if (pixel < 1f) pixel = rect.width / (Capacity * 16f - 3f);
            for (int heart = 0; heart < Capacity; heart++)
            {
                bool filled = heart < Lives;
                float left = rect.xMax - ((Capacity - heart) * 16f - 3f) * pixel;
                for (int row = 0; row < Pixels.Length; row++)
                    for (int col = 0; col < Pixels[row].Length; col++)
                    {
                        char part = Pixels[row][col];
                        if (part == '.' || !filled && part != '#') continue;
                        Color32 tint = part switch
                        {
                            '#' => filled ? new Color32(125, 24, 47, 255) : new Color32(137, 146, 159, 255),
                            'h' => new Color32(255, 156, 167, 255),
                            's' => new Color32(198, 42, 67, 255),
                            _ => new Color32(247, 62, 84, 255)
                        };
                        float x = left + col * pixel;
                        float y = rect.center.y + (Pixels.Length * .5f - row - 1) * pixel;
                        int start = mesh.currentVertCount;
                        mesh.AddVert(new Vector3(x, y), tint, Vector2.zero);
                        mesh.AddVert(new Vector3(x, y + pixel), tint, Vector2.zero);
                        mesh.AddVert(new Vector3(x + pixel, y + pixel), tint, Vector2.zero);
                        mesh.AddVert(new Vector3(x + pixel, y), tint, Vector2.zero);
                        mesh.AddTriangle(start, start + 1, start + 2);
                        mesh.AddTriangle(start, start + 2, start + 3);
                    }
            }
        }
    }
}
