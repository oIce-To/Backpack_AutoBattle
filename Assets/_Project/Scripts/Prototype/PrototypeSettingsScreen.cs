using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeSettingsScreen
    {
        private readonly BackpackPrototypeController _controller;
        private readonly RectTransform _root, _panel, _confirmation;
        private readonly CanvasGroup _panelInput;
        private readonly RectTransform[] _pages = new RectTransform[3];
        private readonly Button[] _tabs = new Button[3], _speeds = new Button[4];
        private readonly Slider[] _volumes = new Slider[3];
        private readonly Text[] _percentages = new Text[3];
        private readonly Text _damageLabel, _countdown, _notice;
        private readonly Dropdown _resolution;
        private readonly Button _windowed, _fullScreen, _apply;
        private readonly Toggle _damage;
        private IReadOnlyList<Vector2Int> _sizes = Array.Empty<Vector2Int>();
        private Vector2Int _currentSize;
        private bool _currentFullScreen, _draftFullScreen;
        private int _sizeIndex, _page;
        private float _speed;

        public bool IsOpen => _root.gameObject.activeSelf;

        public PrototypeSettingsScreen(Transform canvas, BackpackPrototypeController controller)
        {
            _controller = controller;
            _root = Overlay(canvas, "Settings Screen", new Color(.015f, .02f, .035f, .78f));
            _panel = Panel(_root, "Settings Panel", Vector2.zero, new Vector2(760, 516));
            _panelInput = _panel.gameObject.AddComponent<CanvasGroup>();
            var title = SelectionUi.Label(_panel, "Settings Title", "설정", 32, new Vector2(0, 215), new Vector2(400, 44));
            title.color = new Color(.95f, .88f, .72f);
            string[] names = { "소리", "화면", "전투" };
            for (int i = 0; i < names.Length; i++)
            {
                int page = i;
                _tabs[i] = Button(_panel, "Settings Tab " + i, names[i], new Vector2((i - 1) * 215, 152), new Vector2(200, 42), () => SelectPage(page));
                _pages[i] = Panel(_panel, "Settings Page " + i, new Vector2(0, -9), new Vector2(630, 250));
            }
            var reset = Button(_panel, "Settings Reset", "기본값 복원", new Vector2(-225, -195), new Vector2(180, 46), ResetCurrentPage);
            reset.image.color = new Color(.48f, .77f, .94f);
            reset.GetComponentInChildren<Text>().color = new Color(.08f, .17f, .25f);
            CreateCloseButton(_panel, "Settings Close", new Vector2(339, 217), _controller.CloseSettings);
            _notice = SelectionUi.Label(_panel, "Settings Notice", "", 15, new Vector2(0, -151), new Vector2(620, 28));
            _notice.color = new Color(1, .57f, .48f);

            string[] channels = { "전체 볼륨", "배경 음악", "효과음" };
            for (int i = 0; i < channels.Length; i++)
            {
                float y = 70 - i * 70;
                RowLabel(_pages[0], "Settings Volume Label " + i, channels[i], y);
                _volumes[i] = VolumeSlider(_pages[0], i, y);
                _percentages[i] = SelectionUi.Label(_pages[0], "Settings Volume Amount " + i, "", 17,
                    new Vector2(263, y), new Vector2(70, 28));
                _volumes[i].onValueChanged.AddListener(_ => ChangeVolumes());
            }

            RowLabel(_pages[1], "Settings Resolution Label", "해상도", 75);
            _resolution = CreateResolutionDropdown(_pages[1]);
            _resolution.onValueChanged.AddListener(index => { _sizeIndex = index; RenderDisplay(); });
            RowLabel(_pages[1], "Settings Mode Label", "화면 모드", 11);
            _windowed = Button(_pages[1], "Settings Windowed", "창 모드", new Vector2(43, 11), new Vector2(165, 40), () => SetDisplayMode(false));
            _fullScreen = Button(_pages[1], "Settings Full Screen", "전체 화면", new Vector2(217, 11), new Vector2(165, 40), () => SetDisplayMode(true));
            _apply = Button(_pages[1], "Settings Display Apply", "적용", new Vector2(217, -56), new Vector2(165, 40),
                () => _controller.PreviewDisplay(_sizes[_sizeIndex], _draftFullScreen));
            SelectionUi.Label(_pages[1], "Settings Display Hint", Application.isEditor
                    ? "실제 화면 전환은 실행 파일에서 적용됩니다." : "변경 후 15초 안에 확인하면 저장됩니다.",
                15, new Vector2(-80, -56), new Vector2(410, 42)).color = new Color(.72f, .77f, .84f);

            RowLabel(_pages[2], "Settings Battle Speed Label", "기본 전투 배속", 65);
            for (int i = 0; i < _speeds.Length; i++)
            {
                float speed = PrototypeBattleController.PlaybackSpeeds[i];
                _speeds[i] = Button(_pages[2], "Settings Speed " + i, $"{speed:0.#}×", new Vector2(-13 + 88 * i, 65),
                    new Vector2(80, 42), () => SetBattleSettings(speed, _damage.isOn));
            }
            RowLabel(_pages[2], "Settings Damage Label", "데미지 표기 설정", -13);
            var toggleFace = SelectionUi.Image(_pages[2], "Settings Damage Toggle", SelectionUi.Idle);
            SelectionUi.Place(toggleFace.rectTransform, new Vector2(251, -13), new Vector2(88, 42));
            toggleFace.raycastTarget = true;
            _damage = toggleFace.gameObject.AddComponent<Toggle>();
            _damage.targetGraphic = toggleFace;
            _damage.navigation = new Navigation { mode = Navigation.Mode.None };
            _damageLabel = SelectionUi.Label(toggleFace.transform, "Settings Damage State", "", 18, Vector2.zero, new Vector2(80, 38));
            _damage.onValueChanged.AddListener(show => SetBattleSettings(_speed, show));
            SelectionUi.Label(_pages[2], "Settings Speed Hint", "기본 배속은 다음 전투부터 적용됩니다.", 16,
                new Vector2(0, -83), new Vector2(570, 34)).color = new Color(.72f, .77f, .84f);

            _confirmation = Overlay(_root, "Settings Display Confirmation", new Color(.02f, .025f, .04f, .8f));
            var prompt = Panel(_confirmation, "Settings Confirmation Panel", Vector2.zero, new Vector2(510, 224));
            SelectionUi.Label(prompt, "Settings Confirm Title", "변경한 화면을 유지할까요?", 24, new Vector2(0, 62), new Vector2(460, 44));
            _countdown = SelectionUi.Label(prompt, "Settings Display Countdown", "", 18, new Vector2(0, 12), new Vector2(460, 38));
            Button(prompt, "Settings Display Confirm", "유지", new Vector2(-109, -60), new Vector2(190, 42), _controller.ConfirmDisplay);
            Button(prompt, "Settings Display Revert", "되돌리기", new Vector2(109, -60), new Vector2(190, 42), _controller.RevertDisplay);
            _confirmation.gameObject.SetActive(false);
            Hide();
        }

        public void Show(PrototypeGameSettings settings, IReadOnlyList<Vector2Int> sizes, Vector2Int current, bool fullScreen, bool fromBattle)
        {
            _sizes = sizes;
            _resolution.ClearOptions();
            _resolution.AddOptions(sizes.Select(size => $"{size.x} × {size.y}").ToList());
            ReadDisplay(current, fullScreen);
            SetVolumeControls(settings.master, settings.music, settings.effects);
            RenderBattleSettings(settings.battleSpeed, settings.damageNumbers);
            ShowNotice("");
            SelectPage(fromBattle ? 2 : 0);
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
        }

        public void Hide()
        {
            _resolution.Hide();
            _root.gameObject.SetActive(false);
        }
        public void ShowNotice(string message) => _notice.text = message;

        public void ShowDisplayConfirmation(int seconds)
        {
            _resolution.Hide();
            _panelInput.interactable = false;
            EventSystem.current?.SetSelectedGameObject(null);
            UpdateCountdown(seconds);
            _confirmation.gameObject.SetActive(true);
        }

        public void UpdateCountdown(int seconds) => _countdown.text = $"확인하지 않으면 {seconds}초 뒤 이전 화면으로 돌아갑니다.";

        public void EndDisplayConfirmation(Vector2Int size, bool fullScreen)
        {
            _confirmation.gameObject.SetActive(false);
            _panelInput.interactable = true;
            EventSystem.current?.SetSelectedGameObject(null);
            ReadDisplay(size, fullScreen);
        }

        private void ReadDisplay(Vector2Int size, bool fullScreen)
        {
            _currentSize = size;
            _sizeIndex = _sizes.ToList().IndexOf(size);
            if (_sizeIndex < 0) _sizeIndex = 0;
            _currentFullScreen = _draftFullScreen = fullScreen;
            RenderDisplay();
        }

        private void SetDisplayMode(bool fullScreen)
        {
            _draftFullScreen = fullScreen;
            RenderDisplay();
        }

        private void RenderDisplay()
        {
            Vector2Int size = _sizes[_sizeIndex];
            _resolution.SetValueWithoutNotify(_sizeIndex);
            _resolution.interactable = _sizes.Count > 1;
            _windowed.image.color = _draftFullScreen ? SelectionUi.Idle : SelectionUi.Selected;
            _fullScreen.image.color = _draftFullScreen ? SelectionUi.Selected : SelectionUi.Idle;
            _apply.interactable = size != _currentSize || _draftFullScreen != _currentFullScreen;
        }

        private void SelectPage(int page)
        {
            _resolution?.Hide();
            _page = page;
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].gameObject.SetActive(i == page);
                _tabs[i].image.color = i == page ? SelectionUi.Selected : SelectionUi.Idle;
            }
        }

        private void ResetCurrentPage()
        {
            switch (_page)
            {
                case 0:
                    SetVolumeControls(100, 100, 100);
                    ChangeVolumes();
                    break;
                case 1:
                    _sizeIndex = _sizes.ToList().IndexOf(BackpackPrototypeController.DefaultDisplaySize(_sizes));
                    SetDisplayMode(false);
                    break;
                case 2:
                    SetBattleSettings(PrototypeBattleController.DefaultPlaybackSpeed, true);
                    break;
            }
        }

        private void SetVolumeControls(int master, int music, int effects)
        {
            _volumes[0].SetValueWithoutNotify(master);
            _volumes[1].SetValueWithoutNotify(music);
            _volumes[2].SetValueWithoutNotify(effects);
            RenderVolumeAmounts();
        }

        private void ChangeVolumes()
        {
            RenderVolumeAmounts();
            _controller.ChangeVolumes((int)_volumes[0].value, (int)_volumes[1].value, (int)_volumes[2].value);
        }

        private void RenderVolumeAmounts()
        {
            for (int i = 0; i < _volumes.Length; i++) _percentages[i].text = $"{_volumes[i].value:0}%";
        }

        private void SetBattleSettings(float speed, bool show)
        {
            RenderBattleSettings(speed, show);
            _controller.ChangeBattleSettings(speed, show);
        }

        private void RenderBattleSettings(float speed, bool show)
        {
            _speed = speed;
            for (int i = 0; i < _speeds.Length; i++)
                _speeds[i].image.color = speed == PrototypeBattleController.PlaybackSpeeds[i] ? SelectionUi.Selected : SelectionUi.Idle;
            _damage.SetIsOnWithoutNotify(show);
            _damage.targetGraphic.color = show ? new Color(.23f, .42f, .30f) : SelectionUi.Idle;
            _damageLabel.text = show ? "ON" : "OFF";
        }

        private static Slider VolumeSlider(Transform parent, int index, float y)
        {
            var obj = new GameObject("Settings Volume " + index, typeof(RectTransform), typeof(Slider));
            obj.transform.SetParent(parent, false);
            SelectionUi.Place((RectTransform)obj.transform, new Vector2(64, y), new Vector2(305, 34));
            var track = SelectionUi.Image(obj.transform, "Track", new Color(.20f, .24f, .30f));
            SelectionUi.Place(track.rectTransform, Vector2.zero, new Vector2(305, 10));
            var fillArea = Panel(obj.transform, "Fill Area", Vector2.zero, new Vector2(287, 10), Color.clear);
            var fill = SelectionUi.Image(fillArea, "Fill", new Color(.67f, .54f, .33f));
            BackpackPrototypeController.Stretch(fill.rectTransform);
            var handleArea = Panel(obj.transform, "Handle Area", Vector2.zero, new Vector2(287, 34), Color.clear);
            var handle = SelectionUi.Image(handleArea, "Handle", new Color(.95f, .88f, .72f));
            SelectionUi.Place(handle.rectTransform, Vector2.zero, new Vector2(18, 26));
            handle.raycastTarget = track.raycastTarget = true;
            var slider = obj.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            // Slider stretches the handle's anchors vertically across the 34 px track area.
            handle.rectTransform.sizeDelta = new Vector2(18, -8);
            slider.targetGraphic = handle;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        private static Dropdown CreateResolutionDropdown(Transform parent)
        {
            var face = SelectionUi.Image(parent, "Settings Resolution Dropdown", SelectionUi.Idle);
            face.raycastTarget = true;
            SelectionUi.Place(face.rectTransform, new Vector2(113, 75), new Vector2(324, 40));
            var dropdown = face.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = face;
            dropdown.navigation = new Navigation { mode = Navigation.Mode.None };
            var caption = SelectionUi.Label(face.transform, "Settings Resolution", "", 20, new Vector2(-12, 0), new Vector2(280, 36));
            caption.alignment = TextAnchor.MiddleLeft;
            SelectionUi.Label(face.transform, "Resolution Dropdown Arrow", "▼", 15, new Vector2(141, 0), new Vector2(26, 30));

            var template = SelectionUi.Image(face.transform, "Resolution Options Template", new Color(.10f, .13f, .18f));
            template.raycastTarget = true;
            template.rectTransform.anchorMin = Vector2.zero;
            template.rectTransform.anchorMax = Vector2.right;
            template.rectTransform.pivot = new Vector2(.5f, 1);
            template.rectTransform.anchoredPosition = new Vector2(0, -4);
            template.rectTransform.sizeDelta = new Vector2(0, 210);
            var viewport = SelectionUi.Image(template.transform, "Viewport", Color.clear);
            viewport.raycastTarget = true;
            BackpackPrototypeController.Stretch(viewport.rectTransform, 4);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 36);
            var itemFace = SelectionUi.Image(contentRect, "Resolution Option", SelectionUi.Idle);
            itemFace.raycastTarget = true;
            itemFace.rectTransform.anchorMin = Vector2.zero;
            itemFace.rectTransform.anchorMax = Vector2.right;
            itemFace.rectTransform.pivot = new Vector2(.5f, .5f);
            itemFace.rectTransform.anchoredPosition = new Vector2(0, 18);
            itemFace.rectTransform.sizeDelta = new Vector2(0, 36);
            var toggle = itemFace.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = itemFace;
            var check = SelectionUi.Image(itemFace.transform, "Selected Mark", new Color(.65f, .86f, .74f));
            SelectionUi.Place(check.rectTransform, new Vector2(-142, 0), new Vector2(7, 20));
            toggle.graphic = check;
            var label = SelectionUi.Label(itemFace.transform, "Resolution Option Label", "", 19,
                new Vector2(7, 0), new Vector2(280, 34));
            label.alignment = TextAnchor.MiddleLeft;
            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform; scroll.content = contentRect;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.scrollSensitivity = 32;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            dropdown.template = template.rectTransform;
            dropdown.captionText = caption;
            dropdown.itemText = label;
            template.gameObject.SetActive(false);
            return dropdown;
        }

        internal static void CreateCloseButton(Transform parent, string name, Vector2 position, Action close)
        {
            var face = SelectionUi.Image(parent, name, new Color(.76f, .18f, .23f));
            face.raycastTarget = true;
            SelectionUi.Place(face.rectTransform, position, new Vector2(42, 42));
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => close());

            var light = SelectionUi.Image(face.transform, "Top Edge", new Color(.98f, .38f, .42f));
            SelectionUi.Place(light.rectTransform, new Vector2(0, 19.5f), new Vector2(42, 3));
            var shadow = SelectionUi.Image(face.transform, "Bottom Edge", new Color(.49f, .09f, .14f));
            SelectionUi.Place(shadow.rectTransform, new Vector2(0, -19.5f), new Vector2(42, 3));
            var glyph = new GameObject("Close X", typeof(RectTransform), typeof(PrototypeSettingsCloseGlyph));
            glyph.transform.SetParent(face.transform, false);
            SelectionUi.Place((RectTransform)glyph.transform, Vector2.zero, new Vector2(22, 22));
            glyph.GetComponent<PrototypeSettingsCloseGlyph>().raycastTarget = false;
        }

        internal static Button GearButton(Transform parent, string name, Vector2 position, Action open)
        {
            var face = SelectionUi.Image(parent, name, Color.clear);
            face.raycastTarget = true;
            SelectionUi.Place(face.rectTransform, position, new Vector2(42, 38));
            var button = face.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => open());
            var gear = new GameObject("Gear", typeof(RectTransform), typeof(PrototypeSettingsGear));
            gear.transform.SetParent(face.transform, false);
            SelectionUi.Place((RectTransform)gear.transform, Vector2.zero, new Vector2(32, 32));
            var glyph = gear.GetComponent<PrototypeSettingsGear>();
            glyph.raycastTarget = false;
            button.targetGraphic = glyph;
            var colors = button.colors;
            colors.normalColor = new Color(.92f, .92f, .92f);
            colors.highlightedColor = Color.white;
            button.colors = colors;
            return button;
        }

        private static Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size, Action click)
        {
            var button = SelectionUi.Button(parent, name, label, position, size, click);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static RectTransform Overlay(Transform parent, string name, Color tint)
        {
            var overlay = SelectionUi.Image(parent, name, tint);
            overlay.raycastTarget = true;
            BackpackPrototypeController.Stretch(overlay.rectTransform);
            return overlay.rectTransform;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Color? tint = null)
        {
            var image = SelectionUi.Image(parent, name, tint ?? new Color(.105f, .13f, .18f));
            SelectionUi.Place(image.rectTransform, position, size);
            return image.rectTransform;
        }

        private static void RowLabel(Transform parent, string name, string value, float y)
        {
            var label = SelectionUi.Label(parent, name, value, 20, new Vector2(-188, y), new Vector2(198, 38));
            label.alignment = TextAnchor.MiddleLeft;
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeSettingsCloseGlyph : MaskableGraphic
    {
        public PrototypeSettingsCloseGlyph() { useLegacyMeshGeneration = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            const int pixels = 9;
            Rect rect = GetPixelAdjustedRect();
            float pixel = Mathf.Min(rect.width, rect.height) / pixels;
            Vector2 bottomLeft = rect.center - Vector2.one * (pixels * pixel / 2);
            for (int y = 0; y < pixels; y++)
            for (int x = 0; x < pixels; x++)
            {
                if (Mathf.Abs(x - y) > 1 && Mathf.Abs(x + y - (pixels - 1)) > 1) continue;
                Vector2 corner = bottomLeft + new Vector2(x, y) * pixel;
                int start = mesh.currentVertCount;
                mesh.AddVert(corner, color, Vector2.zero);
                mesh.AddVert(corner + new Vector2(0, pixel), color, Vector2.zero);
                mesh.AddVert(corner + new Vector2(pixel, pixel), color, Vector2.zero);
                mesh.AddVert(corner + new Vector2(pixel, 0), color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeSettingsGear : MaskableGraphic
    {
        // Light on the upper-left bevel, with a darker metal edge offset down and right.
        private static readonly string[] Pixels =
        {
            ".......hhhh.......",
            ".......haams......",
            "....hh.haamdhh....",
            "...haahaaaahabh...",
            "...haaaaaaaabbms..",
            "...shaammmmbbmdd..",
            ".hhhaamddddhbbaaa.",
            ".haaaamddddhbbbbms",
            ".haaaamd...hbbbbmd",
            ".hmmaamd...abbmmmd",
            ".sddhabhhhabbmdddd",
            "..dhabbbbbbbbbaddd",
            "...hbbmbbbbmbbms..",
            "...shmdabbmdamdd..",
            "....dddabbmddddd..",
            ".....ddammmd.dd...",
            ".......sdddd......",
            "........dddd......"
        };

        public PrototypeSettingsGear() { useLegacyMeshGeneration = false; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float size = Mathf.Min(rect.width, rect.height);
            float pixel = size / Pixels.Length;
            Vector2 topLeft = rect.center + new Vector2(-size, size) * .5f;
            for (int y = 0; y < Pixels.Length; y++)
            {
                for (int x = 0; x < Pixels[y].Length; x++)
                {
                    char shade = Pixels[y][x];
                    if (shade == '.') continue;
                    float gray = shade switch
                    {
                        'h' => .91f,
                        'a' => .78f,
                        'b' => .66f,
                        'm' => .51f,
                        's' => .39f,
                        _ => .29f
                    };
                    Color tint = new Color(gray, gray, gray) * color;
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
