using System;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Stages;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeMainMenuScreen
    {
        public RectTransform Root { get; }
        private readonly Image _portrait;
        private readonly Text _name, _health, _attack, _stage, _error;

        public PrototypeMainMenuScreen(Transform canvas, Action start, Action quit, Action characters, Action stages)
        {
            Image background = Image(canvas, "Main Menu Screen", new Color(.035f, .04f, .055f));
            Root = background.rectTransform; BackpackPrototypeController.Stretch(Root);
            background.raycastTarget = true;
            Image panel = Image(Root, "Main Menu Panel", new Color(.07f, .075f, .085f));
            Place(panel.rectTransform, Vector2.zero, new Vector2(1100, 610));
            _portrait = Image(panel.transform, "Main Character Illustration", Color.white);
            Place(_portrait.rectTransform, new Vector2(-280, 0), new Vector2(390, 580));
            _portrait.preserveAspect = true;
            // Use a rectangle so the illustration's authored background and soft edge are retained.
            _portrait.useSpriteMesh = false;
            Image divider = Image(panel.transform, "Main Menu Divider", new Color(.64f, .47f, .26f, .5f));
            Place(divider.rectTransform, new Vector2(-46, 0), new Vector2(1, 520));
            Text title = Label(panel.transform, "Main Menu Title", "BACKPACK\nAUTO BATTLE", 36,
                new Vector2(232, 216), new Vector2(450, 100));
            title.color = new Color(.95f, .88f, .72f);
            _name = Label(panel.transform, "Main Character Name", "", 36, new Vector2(232, 112), new Vector2(430, 52));
            _health = Label(panel.transform, "Main Character Health", "", 23, new Vector2(128, 65), new Vector2(192, 40));
            _attack = Label(panel.transform, "Main Character Attack", "", 23, new Vector2(338, 65), new Vector2(192, 40));
            Button(panel.transform, "Main Character Select Button", "캐릭터 선택", new Vector2(232, 12), new Vector2(350, 38),
                new Color(.15f, .18f, .23f), characters);
            _stage = Label(panel.transform, "Main Stage Summary", "", 17, new Vector2(232, -54), new Vector2(450, 70));
            _stage.color = new Color(.69f, .73f, .8f);
            Button(panel.transform, "Main Stage Select Button", "스테이지 선택", new Vector2(232, -112), new Vector2(350, 38),
                new Color(.15f, .18f, .23f), stages);
            Button(panel.transform, "Main Start Button", "스테이지 시작", new Vector2(232, -166), new Vector2(350, 52),
                new Color(.39f, .29f, .17f), start);
            Button(panel.transform, "Main Quit Button", "게임 종료", new Vector2(232, -225), new Vector2(350, 40),
                new Color(.15f, .18f, .23f), quit);
            _error = Label(panel.transform, "Main Menu Error", "", 14, new Vector2(232, -275), new Vector2(450, 42));
            _error.color = new Color(1, .45f, .4f); _error.fontStyle = FontStyle.Normal;
            Hide();
        }

        public void Show(CharacterData character, StageData stage, StageProgress progress)
        {
            CharacterStatsSnapshot stats = character != null ? character.CaptureStats() : CharacterStatsSnapshot.Default;
            _portrait.sprite = character?.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _name.text = character != null ? character.DisplayName : "플레이어";
            _health.text = $"체력  {stats.MaximumHealth:0.#}";
            _attack.text = $"공격력  {stats.Attack:0.#}";
            _stage.text = stage != null ? $"{stage.DisplayName}\n{stage.RoundCount}라운드 · "
                + (progress.Cleared ? "클리어" : progress.HighestRound > 0 ? $"최고 {progress.HighestRound}라운드" : "도전 기록 없음")
                : "스테이지가 지정되지 않았습니다.";
            _error.text = "";
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }
        public void ShowError(string error) => _error.text = error.Split('\n')[0];
        public void Hide() => Root.gameObject.SetActive(false);
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
            => BackpackPrototypeController.SetRect(rect, Vector2.one * .5f, position, size);
        private static Image Image(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static Text Label(Transform parent, string name, string value, int size, Vector2 position, Vector2 dimensions)
        {
            Text text = BackpackPrototypeController.CreateText(parent, name, value, size, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.raycastTarget = false; Place(text.rectTransform, position, dimensions); return text;
        }
        private static void Button(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, Action action)
        {
            Image image = Image(parent, name, color); Place(image.rectTransform, position, size); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => action());
            Label(image.transform, name + " Label", label, 21, Vector2.zero, size - new Vector2(12, 0));
        }
    }
}
