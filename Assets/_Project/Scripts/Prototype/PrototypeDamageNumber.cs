using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Draws tinted glyphs from the shared, shaded Liberation Sans bitmap.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeDamageNumber : MaskableGraphic
    {
        private const string Alphabet = "0123456789-.<CRIT";
        private const int Columns = 8, CellWidth = 32, CellHeight = 36;
        private Texture2D _atlas;
        private float _numberScale;
        private bool _critical;

        public string Text { get; private set; } = "";
        public override Texture mainTexture => _atlas != null ? _atlas : base.mainTexture;

        public PrototypeDamageNumber() { useLegacyMeshGeneration = false; }

        internal void Initialize(Texture2D atlas)
        {
            _atlas = atlas;
            raycastTarget = false;
        }

        internal Vector2 SetNumber(string text, bool critical, Color tint)
        {
            Text = text;
            _critical = critical;
            color = tint;
            float width = Measure(text);
            _numberScale = Mathf.Min(critical ? 1.43f : 1.16f, 148 / width);
            SetVerticesDirty();
            return new Vector2(Mathf.Max(width * _numberScale + 4, critical ? 52 : 0), critical ? 57 : 33);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_atlas == null || Text.Length == 0) return;
            if (_critical) DrawLine(mesh, "CRIT", .69f, 17);
            DrawLine(mesh, Text, _numberScale, _critical ? -10 : -2);
        }

        private void DrawLine(VertexHelper mesh, string text, float scale, float y)
        {
            float left = -Measure(text) * scale / 2;
            foreach (char symbol in text)
            {
                int index = Alphabet.IndexOf(symbol);
                float advance = Advance(symbol) * scale;
                if (index >= 0)
                {
                    float x = left + (advance - CellWidth * scale) / 2;
                    float bottom = y - CellHeight * scale / 2;
                    float right = x + CellWidth * scale;
                    float top = bottom + CellHeight * scale;
                    float u0 = (index % Columns) * CellWidth / (float)_atlas.width;
                    float u1 = u0 + CellWidth / (float)_atlas.width;
                    float v1 = 1 - (index / Columns) * CellHeight / (float)_atlas.height;
                    float v0 = v1 - CellHeight / (float)_atlas.height;
                    int start = mesh.currentVertCount;
                    mesh.AddVert(new Vector3(x, bottom), color, new Vector2(u0, v0));
                    mesh.AddVert(new Vector3(x, top), color, new Vector2(u0, v1));
                    mesh.AddVert(new Vector3(right, top), color, new Vector2(u1, v1));
                    mesh.AddVert(new Vector3(right, bottom), color, new Vector2(u1, v0));
                    mesh.AddTriangle(start, start + 1, start + 2);
                    mesh.AddTriangle(start, start + 2, start + 3);
                }
                left += advance;
            }
        }

        private static float Measure(string text)
        {
            float width = 0;
            foreach (char symbol in text) width += Advance(symbol);
            return width;
        }

        private static float Advance(char symbol) => symbol switch
        {
            '.' or 'I' => 10,
            '-' => 14,
            'C' or 'R' => 22,
            _ => 20
        };
    }
}
