using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Unscaled, retargetable circular carousel. Recycled slots change sprites outside the clipped viewport.</summary>
    public sealed class PrototypeCharacterCarousel : MonoBehaviour, IScrollHandler
    {
        public const float Spacing = 310f;
        private readonly List<Image> _slots = new();
        private readonly List<Material> _materials = new();
        private CharacterData[] _characters = Array.Empty<CharacterData>();
        private Action<CharacterData, bool> _changed;
        private int _target;
        private float _position, _velocity;
        private InputAction _previousKey, _nextKey;
        public float Position => _position;
        public bool IsAnimating => Mathf.Abs(_position - _target) > .001f || Mathf.Abs(_velocity) > .01f;
        public int PreviewIndex => _characters.Length == 0 ? -1 : Mod(_target, _characters.Length);
        public CharacterData PreviewedCharacter => PreviewIndex < 0 ? null : _characters[PreviewIndex];

        public void Configure(RectTransform viewport, Action<CharacterData, bool> changed)
        {
            _changed = changed;
            Shader shader = Resources.Load<Shader>("UI/CharacterCarousel");
            for (int offset = -2; offset <= 2; offset++)
            {
                string name = offset == 0 ? "Character Preview Illustration" : offset == -1 ? "Character Carousel Previous"
                    : offset == 1 ? "Character Carousel Next" : "Character Carousel Outer " + offset;
                Image image = SelectionUi.Image(viewport, name, Color.white);
                image.preserveAspect = true; image.useSpriteMesh = false; image.raycastTarget = true;
                var material = new Material(shader); image.material = material; _materials.Add(material);
                var button = image.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                int slot = offset;
                button.onClick.AddListener(() => SetTarget(Mathf.FloorToInt(_position) + slot));
                _slots.Add(image);
            }
        }
        public void SetCharacters(IReadOnlyList<CharacterData> characters, CharacterData current)
        {
            _characters = characters.ToArray();
            int index = Array.FindIndex(_characters, c => c == current || c.CharacterId == current?.CharacterId);
            _target = Mathf.Max(0, index); _position = _target; _velocity = 0; Render();
        }
        public void Move(int direction)
        {
            if (direction != 0) SetTarget(_target + Math.Sign(direction));
        }
        private void SetTarget(int target)
        {
            if (_characters.Length <= 1 || !gameObject.activeInHierarchy) return;
            _target = target; Render();
        }
        public void OnScroll(PointerEventData pointer)
        {
            if (Mathf.Abs(pointer.scrollDelta.y) > .001f) Move(pointer.scrollDelta.y > 0 ? -1 : 1);
        }
        private static bool IsEditingText()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && (selected.GetComponent<InputField>() != null || selected.GetComponent<TMPro.TMP_InputField>() != null);
        }
        private void Awake()
        {
            _previousKey = new InputAction("Previous character", InputActionType.Button, "<Keyboard>/leftArrow");
            _nextKey = new InputAction("Next character", InputActionType.Button, "<Keyboard>/rightArrow");
            _previousKey.performed += _ => { if (!IsEditingText()) Move(-1); };
            _nextKey.performed += _ => { if (!IsEditingText()) Move(1); };
        }
        private void OnEnable() { _previousKey?.Enable(); _nextKey?.Enable(); }
        private void Update() => TickAnimation(Time.unscaledDeltaTime);
        public void TickAnimation(float deltaSeconds)
        {
            if (_characters.Length == 0 || !gameObject.activeInHierarchy || !IsAnimating) return;
            _position = Mathf.SmoothDamp(_position, _target, ref _velocity, .12f, Mathf.Infinity, Mathf.Clamp(deltaSeconds, 0, .05f));
            if (!IsAnimating) { _position = _target; _velocity = 0; }
            Render();
        }
        private void Render()
        {
            int basis = Mathf.FloorToInt(_position);
            for (int i = 0; i < _slots.Count; i++)
            {
                int offset = i - 2; Image image = _slots[i];
                bool visible = _characters.Length > 0 && (_characters.Length > 1 || offset == 0);
                image.gameObject.SetActive(visible); if (!visible) continue;
                int step = basis + offset;
                float distance = step - _position, fade = Mathf.Clamp01(Mathf.Abs(distance));
                image.sprite = _characters[Mod(step, _characters.Length)].Portrait;
                float scale = Mathf.Lerp(1, .84f, fade);
                SelectionUi.Place(image.rectTransform, new Vector2(distance * Spacing, 0), new Vector2(300, 438) * scale);
                image.color = new Color(1, 1, 1, Mathf.Lerp(1, .24f, fade));
                _materials[i].SetFloat("_Saturation", Mathf.Clamp01(1 - Mathf.Abs(distance) * 1.4f));
            }
            _changed?.Invoke(PreviewedCharacter, IsAnimating);
        }
        private static int Mod(int value, int count) => (value % count + count) % count;
        private void OnDisable() { _previousKey?.Disable(); _nextKey?.Disable(); _velocity = 0; }
        private void OnDestroy()
        {
            _previousKey?.Dispose(); _nextKey?.Dispose();
            foreach (var material in _materials) if (material != null) Destroy(material);
        }
    }

    public sealed class PrototypeCharacterSelectionScreen
    {
        public RectTransform Root { get; }
        public PrototypeCharacterCarousel Carousel { get; }
        private readonly Text _name, _stats, _level, _levelCost, _levelUpTitle, _error;
        private readonly PrototypeExperienceIcon _costIcon;
        private readonly Button _confirm, _levelUp;
        private PlayerProgress _progress;
        public PrototypeCharacterSelectionScreen(Transform canvas, Action<CharacterData> confirm, Action close, Action<CharacterData> levelUp)
        {
            var background = SelectionUi.Image(canvas, "Character Selection Screen", new Color(.035f, .04f, .055f));
            background.raycastTarget = true; Root = background.rectTransform; BackpackPrototypeController.Stretch(Root);
            var panel = SelectionUi.Image(Root, "Character Selection Panel", new Color(.07f, .075f, .085f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(1100, 610));
            var title = SelectionUi.Label(panel.transform, "Character Selection Title", "캐릭터 선택", 30,
                new Vector2(0, 258), new Vector2(950, 48)); title.color = new Color(.95f, .88f, .72f);
            var viewport = SelectionUi.Image(panel.transform, "Character Carousel Viewport", Color.clear);
            SelectionUi.Place(viewport.rectTransform, new Vector2(-195, 10), new Vector2(660, 440));
            viewport.raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var detail = SelectionUi.Image(panel.transform, "Character Details Panel", new Color(.10f, .12f, .16f));
            SelectionUi.Place(detail.rectTransform, new Vector2(340, -30), new Vector2(340, 510));
            _name = SelectionUi.Label(panel.transform, "Character Preview Name", "", 31, new Vector2(340, 154), new Vector2(300, 50));
            _level = SelectionUi.Label(panel.transform, "Character Preview Level", "", 19, new Vector2(340, 111), new Vector2(300, 28));
            _stats = SelectionUi.Label(panel.transform, "Character Preview Stats", "", 23, new Vector2(340, 48), new Vector2(300, 84));
            _stats.lineSpacing = 1.7f;
            var upgrade = new GameObject("Character Level Up Button", typeof(RectTransform), typeof(PrototypeRoundedPanel), typeof(Button));
            upgrade.transform.SetParent(panel.transform, false);
            SelectionUi.Place((RectTransform)upgrade.transform, new Vector2(340, -100), new Vector2(300, 104));
            var face = upgrade.GetComponent<PrototypeRoundedPanel>(); face.color = new Color(.23f, .42f, .30f);
            _levelUp = upgrade.GetComponent<Button>(); _levelUp.targetGraphic = face;
            _levelUp.navigation = new Navigation { mode = Navigation.Mode.None };
            _levelUp.onClick.AddListener(() => { if (_levelUp.interactable && !Carousel.IsAnimating) levelUp(Carousel.PreviewedCharacter); });
            _levelUpTitle = SelectionUi.Label(upgrade.transform, "Character Level Up Button Label", "레벨 업", 23,
                new Vector2(0, 23), new Vector2(280, 32));
            var costIcon = new GameObject("Character Level Up Experience Icon", typeof(RectTransform), typeof(PrototypeExperienceIcon));
            costIcon.transform.SetParent(upgrade.transform, false); _costIcon = costIcon.GetComponent<PrototypeExperienceIcon>();
            _levelCost = SelectionUi.Label(upgrade.transform, "Character Level Up Cost", "", 23, new Vector2(19, -22), new Vector2(150, 32));
            _levelCost.resizeTextForBestFit = true; _levelCost.resizeTextMinSize = 14; _levelCost.resizeTextMaxSize = 23;
            _error = SelectionUi.Label(panel.transform, "Character Growth Error", "", 14, new Vector2(340, -194), new Vector2(300, 42));
            _error.color = new Color(1, .45f, .4f);
            _confirm = SelectionUi.Button(panel.transform, "Character Confirm Button", "선택", new Vector2(340, -253), new Vector2(300, 46),
                () => { if (!Carousel.IsAnimating && Carousel.PreviewedCharacter != null) confirm(Carousel.PreviewedCharacter); });
            _confirm.GetComponent<Image>().color = new Color(.57f, .78f, .59f);
            _confirm.GetComponentInChildren<Text>().color = new Color(.08f, .16f, .10f);
            SelectionUi.Button(panel.transform, "Character Selection Back Button", "취소", new Vector2(-340, -253), new Vector2(300, 46), close);
            Carousel = Root.gameObject.AddComponent<PrototypeCharacterCarousel>(); Carousel.Configure(viewport.rectTransform, RenderDetails);
            Hide();
        }
        public void Show(IReadOnlyList<CharacterData> characters, CharacterData current, PlayerProgress progress)
        {
            _progress = progress; _error.text = "";
            Root.gameObject.SetActive(true); Root.SetAsLastSibling(); Carousel.SetCharacters(characters, current);
        }
        public void RefreshProgress(PlayerProgress progress)
        {
            _progress = progress; _error.text = "";
            RenderDetails(Carousel.PreviewedCharacter, Carousel.IsAnimating);
        }
        public void ShowError(string error) => _error.text = error ?? "";
        private void RenderDetails(CharacterData character, bool moving)
        {
            _confirm.interactable = character != null && !moving;
            _name.text = character?.DisplayName ?? "캐릭터가 없습니다.";
            int level = _progress?.GetCharacterLevel(character?.CharacterId) ?? 1;
            _level.text = $"Lv. {level}";
            var stats = character?.CaptureStats(level); _stats.text = stats == null ? "" : $"체력  {stats.MaximumHealth:0.#}\n공격력  {stats.Attack:0.#}";
            long cost = 0;
            bool canGrow = character?.ExperienceTable != null && character.ExperienceTable.TryGetCost(level, out cost);
            _levelUpTitle.text = canGrow ? "레벨 업" : "레벨업 불가";
            _levelCost.text = canGrow ? $"{cost:N0}" : "—";
            float costWidth = Mathf.Min(220, _levelCost.preferredWidth);
            SelectionUi.Place(_costIcon.rectTransform, new Vector2(-(costWidth + 10) / 2, -22), new Vector2(28, 28));
            SelectionUi.Place(_levelCost.rectTransform, new Vector2(19, -22), new Vector2(costWidth, 32));
            _levelUp.interactable = canGrow && !moving && (_progress?.SharedExperience ?? 0) >= cost;
        }
        public void Hide() => Root.gameObject.SetActive(false);
    }
}
