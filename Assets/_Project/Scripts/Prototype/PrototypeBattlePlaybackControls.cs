using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Battle-local playback controls; never changes Unity's global time scale.</summary>
    public sealed class PrototypeBattlePlaybackControls
    {
        private readonly RectTransform _root;
        private readonly Button _pause;
        private readonly PrototypePlaybackGlyph _glyph;
        private readonly Text _pausedLabel;
        private readonly Dictionary<float, Button> _speeds = new();
        private static readonly Color Idle = new(.10f, .15f, .22f);
        private static readonly Color Selected = new(.23f, .46f, .68f);

        public PrototypeBattlePlaybackControls(Transform parent, Action<float> changeSpeed, Action togglePause)
        {
            var obj = new GameObject("Combat Playback Controls", typeof(RectTransform));
            obj.transform.SetParent(parent, false); _root = (RectTransform)obj.transform;
            BackpackPrototypeController.SetRect(_root, Vector2.one * .5f, new Vector2(0, 270), new Vector2(258, 36));
            _pause = Button("Combat Pause Button", -108f, 42f, togglePause);
            var glyphObject = new GameObject("Playback Pause Resume Glyph", typeof(RectTransform), typeof(PrototypePlaybackGlyph));
            glyphObject.transform.SetParent(_pause.transform, false); _glyph = glyphObject.GetComponent<PrototypePlaybackGlyph>();
            _glyph.color = Color.white; _glyph.raycastTarget = false;
            BackpackPrototypeController.SetRect(_glyph.rectTransform, Vector2.one * .5f, Vector2.zero, Vector2.one * 22);
            int index = 0;
            foreach (float speed in PrototypeBattleController.PlaybackSpeeds)
            {
                string value = speed.ToString("0.#", CultureInfo.InvariantCulture);
                Button button = Button($"Combat Speed {value} Button", -57f + index++ * 54f, 48f, () => changeSpeed(speed));
                _speeds.Add(speed, button);
                Text label = BackpackPrototypeController.CreateText(button.transform, "Playback Speed Label", value + "×", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.raycastTarget = false; BackpackPrototypeController.Stretch(label.rectTransform);
            }
            _pausedLabel = BackpackPrototypeController.CreateText(_root, "Combat Paused Label", "일시정지", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            _pausedLabel.raycastTarget = false;
            BackpackPrototypeController.SetRect(_pausedLabel.rectTransform, Vector2.one * .5f, new Vector2(0, -32), new Vector2(130, 24));
            SetState(false, 1f, false);
        }

        public void SetState(bool running, float speed, bool paused)
        {
            _root.gameObject.SetActive(running);
            _pause.interactable = running;
            _pause.GetComponent<Image>().color = paused ? Selected : Idle;
            _glyph.ShowPlay = paused;
            _pausedLabel.gameObject.SetActive(paused);
            foreach (var pair in _speeds)
            {
                pair.Value.interactable = running;
                pair.Value.GetComponent<Image>().color = pair.Key == speed ? Selected : Idle;
            }
        }

        private Button Button(string name, float x, float width, Action action)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(_root, false);
            BackpackPrototypeController.SetRect((RectTransform)obj.transform, Vector2.one * .5f, new Vector2(x, 0), new Vector2(width, 36));
            Button button = obj.GetComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>();
            button.onClick.AddListener(() => action()); return button;
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypePlaybackGlyph : MaskableGraphic
    {
        private bool _showPlay;
        public bool ShowPlay
        {
            get => _showPlay;
            set { if (_showPlay == value) return; _showPlay = value; SetVerticesDirty(); }
        }
        public PrototypePlaybackGlyph() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect rect = GetPixelAdjustedRect(); float scale = Mathf.Min(rect.width, rect.height) / 20;
            void Polygon(params Vector2[] points)
            {
                int first = vh.currentVertCount;
                foreach (Vector2 point in points) vh.AddVert(rect.center + point * scale, color, Vector2.zero);
                for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(first, first + i, first + i + 1);
            }
            if (ShowPlay) Polygon(new(-5, -8), new(-5, 8), new(8, 0));
            else
            {
                Polygon(new(-7, -8), new(-7, 8), new(-2, 8), new(-2, -8));
                Polygon(new(2, -8), new(2, 8), new(7, 8), new(7, -8));
            }
        }
    }
}
