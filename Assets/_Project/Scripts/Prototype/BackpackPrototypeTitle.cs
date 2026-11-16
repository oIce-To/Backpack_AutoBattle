using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private RectTransform _titleRoot;
        private Button _titleStart, _titleSettings;
        private bool _titleTransition;
        public bool IsTitleScreen { get; private set; }

        private void ShowTitleScreen()
        {
            IsTitleScreen = true;
            var canvas = GetComponentInChildren<Canvas>().transform;
            var backdrop = SelectionUi.Image(canvas, "Title Screen", Color.black);
            _titleRoot = backdrop.rectTransform;
            Stretch(_titleRoot);
            PrototypeGuildHall.Add(_titleRoot, "UI/TitleJourney", prefix: "Title Journey");

            var title = SelectionUi.Label(_titleRoot, "Game Title", "Backpack\nAutoBattle", 78,
                new Vector2(0, 188), new Vector2(900, 205));
            title.color = PrototypeGuildStyle.Cream;
            title.lineSpacing = .93f;
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.13f, .10f, .08f, 1f);
            shadow.effectDistance = new Vector2(5, -6);
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.21f, .15f, .10f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            _titleStart = SelectionUi.GuildButton(_titleRoot, "Title Start Button", "게임 시작",
                new Vector2(0, -250), new Vector2(300, 64), () => TryStartFromTitle());
            _titleStart.GetComponentInChildren<Text>().fontSize = 29;
            _titleSettings = PrototypeSettingsScreen.GearButton(_titleRoot, "Title Settings Button", Vector2.zero, () => TryOpenSettings());
            SetRect((RectTransform)_titleSettings.transform, Vector2.one, new Vector2(-42, -38), new Vector2(42, 38));
            RefreshCombatInterface();
            _titleRoot.SetAsLastSibling();
        }

        public bool TryStartFromTitle()
        {
            if (!IsTitleScreen || IsSettingsOpen || _titleTransition) return false;
            _titleTransition = true;
            _titleStart.interactable = _titleSettings.interactable = false;
            EventSystem.current?.SetSelectedGameObject(null);
            StartCoroutine(EnterFromTitle());
            return true;
        }

        private IEnumerator EnterFromTitle()
        {
            var fade = SelectionUi.Image(_titleRoot.parent, "Title Transition", Color.clear);
            Stretch(fade.rectTransform);
            fade.raycastTarget = true;
            const float duration = .28f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float amount = Mathf.Clamp01(elapsed / duration);
                fade.color = new Color(0, 0, 0, amount);
                _musicFade = 1 - amount;
                ApplySettings();
                yield return null;
            }
            fade.color = Color.black;
            _musicFade = 0;
            ApplySettings();
            IsTitleScreen = false;
            _titleRoot.gameObject.SetActive(false);
            OpenApplicationEntry();
            RefreshMusic();
            fade.transform.SetAsLastSibling();
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float amount = Mathf.Clamp01(elapsed / duration);
                fade.color = new Color(0, 0, 0, 1 - amount);
                _musicFade = amount;
                ApplySettings();
                yield return null;
            }
            _musicFade = 1;
            ApplySettings();
            fade.gameObject.SetActive(false);
            Destroy(fade.gameObject);
            _titleTransition = false;
        }
    }
}
