using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Stages;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal static class SelectionUi
    {
        internal static readonly Color Idle = new(.15f, .18f, .23f);
        internal static readonly Color Selected = new(.39f, .29f, .17f);
        internal static void Place(RectTransform rect, Vector2 position, Vector2 size)
            => BackpackPrototypeController.SetRect(rect, Vector2.one * .5f, position, size);
        internal static Image Image(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        internal static Text Label(Transform parent, string name, string text, int font, Vector2 position, Vector2 size)
        {
            var label = BackpackPrototypeController.CreateText(parent, name, text, font, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.raycastTarget = false; Place(label.rectTransform, position, size); return label;
        }
        internal static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size, Action action)
        {
            var image = Image(parent, name, Idle); image.raycastTarget = true; Place(image.rectTransform, position, size);
            var button = image.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => action());
            Label(image.transform, name + " Label", text, 19, Vector2.zero, size - new Vector2(12, 0)); return button;
        }
    }

    internal sealed class SelectionFrame
    {
        public RectTransform Root { get; }
        public RectTransform Panel { get; }
        public RectTransform Content { get; }
        private readonly ScrollRect _scroll;
        public SelectionFrame(Transform canvas, string prefix, string title, Action close)
        {
            var background = SelectionUi.Image(canvas, prefix + " Selection Screen", new Color(.035f, .04f, .055f));
            background.raycastTarget = true; Root = background.rectTransform; BackpackPrototypeController.Stretch(Root);
            var panel = SelectionUi.Image(Root, prefix + " Selection Panel", new Color(.07f, .075f, .085f));
            Panel = panel.rectTransform; SelectionUi.Place(Panel, Vector2.zero, new Vector2(1100, 610));
            var label = SelectionUi.Label(Panel, prefix + " Selection Title", title, 32, new Vector2(0, 253), new Vector2(950, 56));
            label.color = new Color(.95f, .88f, .72f);
            var viewport = SelectionUi.Image(Panel, prefix + " Choices Viewport", Color.clear);
            viewport.raycastTarget = true; SelectionUi.Place(viewport.rectTransform, new Vector2(-365, 0), new Vector2(300, 410));
            viewport.gameObject.AddComponent<RectMask2D>();
            var obj = new GameObject(prefix + " Choices", typeof(RectTransform)); obj.transform.SetParent(viewport.transform, false);
            Content = (RectTransform)obj.transform;
            Content.anchorMin = new Vector2(0, 1); Content.anchorMax = Vector2.one; Content.pivot = new Vector2(.5f, 1);
            _scroll = viewport.gameObject.AddComponent<ScrollRect>(); _scroll.content = Content; _scroll.viewport = viewport.rectTransform;
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.scrollSensitivity = 35; _scroll.movementType = ScrollRect.MovementType.Clamped;
            SelectionUi.Button(Panel, prefix + " Selection Back Button", "뒤로", new Vector2(-365, -253), new Vector2(260, 46), close);
            Hide();
        }
        public void Prepare(int count)
        {
            foreach (Transform child in Content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            Content.sizeDelta = new Vector2(0, Mathf.Max(410, count * 80));
            Content.anchoredPosition = Vector2.zero; _scroll.verticalNormalizedPosition = 1;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }
        public Button Option(string name, string label, int index, Action action)
        {
            var button = SelectionUi.Button(Content, name, label, Vector2.zero, new Vector2(280, 70), action);
            BackpackPrototypeController.SetRect((RectTransform)button.transform, new Vector2(.5f, 1), new Vector2(0, -36 - index * 80), new Vector2(280, 70));
            return button;
        }
        public void Hide() => Root.gameObject.SetActive(false);
    }

    public sealed class PrototypeStageSelectionScreen
    {
        private readonly SelectionFrame _frame;
        private readonly Text _name, _details, _record;
        private readonly Button _confirm;
        private readonly Dictionary<StageData, Button> _choices = new();
        private StageData _selected;
        private PlayerProgress _progress;
        private string[] _stageOrder = Array.Empty<string>();
        public PrototypeStageSelectionScreen(Transform canvas, Action<StageData> confirm, Action close)
        {
            _frame = new SelectionFrame(canvas, "Stage", "스테이지 선택", close);
            _name = SelectionUi.Label(_frame.Panel, "Stage Preview Name", "", 29, new Vector2(195, 158), new Vector2(580, 84));
            _details = SelectionUi.Label(_frame.Panel, "Stage Preview Details", "", 19, new Vector2(195, 20), new Vector2(580, 180));
            _details.fontStyle = FontStyle.Normal; _details.lineSpacing = 1.2f;
            _record = SelectionUi.Label(_frame.Panel, "Stage Preview Progress", "", 22, new Vector2(195, -138), new Vector2(580, 78));
            _record.color = new Color(.95f, .88f, .72f);
            _confirm = SelectionUi.Button(_frame.Panel, "Stage Confirm Button", "선택 완료", new Vector2(240, -253), new Vector2(300, 46),
                () => { if (_confirm.interactable && IsUnlocked(_selected)) confirm(_selected); });
        }
        public void Show(IReadOnlyList<StageData> stages, StageData current, PlayerProgress progress)
        {
            _progress = progress; _stageOrder = stages.Select(s => s.StageId).ToArray();
            _choices.Clear(); _frame.Prepare(stages.Count);
            for (int i = 0; i < stages.Count; i++)
            {
                StageData stage = stages[i]; var record = progress.GetStage(stage.StageId);
                _choices[stage] = _frame.Option("Stage Choice " + stage.StageId, stage.DisplayName + (record.Cleared ? "\n클리어" : ""), i, () => Preview(stage));
                bool unlocked = IsUnlocked(stage);
                _choices[stage].interactable = unlocked;
                if (!unlocked)
                {
                    _choices[stage].gameObject.AddComponent<CanvasGroup>().alpha = .4f;
                    var colors = _choices[stage].colors; colors.disabledColor = Color.white; _choices[stage].colors = colors;
                }
            }
            Preview(stages.FirstOrDefault(s => s == current && IsUnlocked(s)) ?? stages.FirstOrDefault(IsUnlocked));
        }
        private bool IsUnlocked(StageData stage) => stage != null && _progress.IsStageUnlocked(stage.StageId, _stageOrder);
        private void Preview(StageData stage)
        {
            if (stage != null && !IsUnlocked(stage)) return;
            _selected = stage; _confirm.interactable = stage != null && stage.GetValidationErrors().Count == 0;
            _name.text = stage?.DisplayName ?? "스테이지가 없습니다.";
            if (stage == null) { _details.text = _record.text = ""; return; }
            string Names(bool bosses) => string.Join(" · ", stage.Rounds.Where(r => r != null && (!bosses || r.IsBoss))
                .Select(r => r.Encounter?.DisplayName).Where(n => !string.IsNullOrEmpty(n)).Distinct().Take(4));
            string bossNames = Names(true);
            _details.text = $"{stage.RoundCount}라운드\n\n주요 적  {Names(false)}\n\n보스  {(bossNames.Length == 0 ? "없음" : bossNames)}\n\n클리어 경험치  {stage.ClearExperience:N0}";
            if (!_confirm.interactable) _details.text = "스테이지 설정을 확인해주세요.";
            StageProgress record = _progress.GetStage(stage.StageId);
            _record.text = record.Cleared ? $"클리어\n최고 도달 {record.HighestRound} / {stage.RoundCount}라운드"
                : record.HighestRound > 0 ? $"미클리어\n최고 도달 {record.HighestRound} / {stage.RoundCount}라운드" : "도전 기록 없음";
            foreach (var choice in _choices) choice.Value.GetComponent<Image>().color = !IsUnlocked(choice.Key)
                ? new Color(.24f, .24f, .24f) : choice.Key == stage ? SelectionUi.Selected : SelectionUi.Idle;
        }
        public void Hide() => _frame.Hide();
    }
}
