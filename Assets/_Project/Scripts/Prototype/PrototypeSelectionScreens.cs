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
            Root.gameObject.SetActive(true);
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
        private readonly Button _normalMode, _mineMode, _sweep;
        private readonly Text _error;
        private readonly RectTransform _rewardRoot;
        private readonly RectTransform[] _rewardSlots = new RectTransform[3];
        private readonly Text[] _rewardAmounts = new Text[3];
        private IReadOnlyList<StageData> _normalStages, _mineStages;
        private bool _showMines;
        private readonly Dictionary<StageData, Button> _choices = new();
        private StageData _selected;
        private PlayerProgress _progress;
        private string[] _stageOrder = Array.Empty<string>();
        public PrototypeStageSelectionScreen(Transform canvas, Action<StageData> confirm, Action close, Action<StageData> sweep)
        {
            _frame = new SelectionFrame(canvas, "Stage", "스테이지 선택", close);
            _name = SelectionUi.Label(_frame.Panel, "Stage Preview Name", "", 29, new Vector2(195, 158), new Vector2(580, 84));
            _details = SelectionUi.Label(_frame.Panel, "Stage Preview Details", "", 19, new Vector2(195, 56), new Vector2(580, 138));
            _details.fontStyle = FontStyle.Normal; _details.lineSpacing = 1.2f;
            _record = SelectionUi.Label(_frame.Panel, "Stage Preview Progress", "", 22, new Vector2(195, -138), new Vector2(580, 78));
            _record.color = new Color(.95f, .88f, .72f);
            _confirm = SelectionUi.Button(_frame.Panel, "Stage Confirm Button", "선택 완료", new Vector2(240, -247), new Vector2(300, 64),
                () => { if (_confirm.interactable && IsUnlocked(_selected)) confirm(_selected); });
            _normalMode = SelectionUi.Button(_frame.Panel, "Normal Stage Category", "일반 스테이지", new Vector2(-440, 211), new Vector2(155, 30), () => SwitchCategory(false));
            _mineMode = SelectionUi.Button(_frame.Panel, "Mine Stage Category", "기원의 광산", new Vector2(-270, 211), new Vector2(155, 30), () => SwitchCategory(true));
            var viewport = (RectTransform)_frame.Content.parent;
            SelectionUi.Place(viewport, new Vector2(-365, -15), new Vector2(300, 385));
            _sweep = SelectionUi.Button(_frame.Panel, "Sweep Mine Stage", "소탕", new Vector2(-28, -247), new Vector2(192, 64),
                () => { if (_sweep.interactable) sweep(_selected); });
            _sweep.GetComponent<Image>().color = new Color(.20f, .40f, .39f);
            _sweep.gameObject.AddComponent<CanvasGroup>();
            SelectionUi.Place(_sweep.GetComponentInChildren<Text>().rectTransform, new Vector2(0, 14), new Vector2(180, 26));
            var ticketIcon = new GameObject("Sweep Ticket Icon", typeof(RectTransform), typeof(PrototypeEconomyIcon));
            ticketIcon.transform.SetParent(_sweep.transform, false);
            ticketIcon.GetComponent<PrototypeEconomyIcon>().SetKind(EconomyIconKind.MineTicket);
            ticketIcon.GetComponent<PrototypeEconomyIcon>().raycastTarget = false;
            SelectionUi.Place((RectTransform)ticketIcon.transform, new Vector2(-13, -15), new Vector2(24, 20));
            SelectionUi.Label(_sweep.transform, "Sweep Ticket Cost", "1", 16, new Vector2(16, -15), new Vector2(28, 22));
            var rewards = new GameObject("Stage Expected Rewards", typeof(RectTransform));
            rewards.transform.SetParent(_frame.Panel, false);
            _rewardRoot = (RectTransform)rewards.transform;
            SelectionUi.Place(_rewardRoot, new Vector2(195, -66), new Vector2(570, 48));
            SelectionUi.Label(_frame.Panel, "Stage Reward Caption", "예상 보상", 14, new Vector2(195, -29), new Vector2(550, 22));
            string[] rewardNames = { "Experience", "Origin Shards", "Blueprints" };
            for (int i = 0; i < rewardNames.Length; i++)
            {
                var row = new GameObject("Stage Reward " + rewardNames[i], typeof(RectTransform));
                row.transform.SetParent(_rewardRoot, false);
                _rewardSlots[i] = (RectTransform)row.transform;
                SelectionUi.Place(_rewardSlots[i], Vector2.zero, new Vector2(170, 34));
                if (i == 0)
                {
                    var experience = new GameObject("Stage Reward Icon", typeof(RectTransform), typeof(PrototypeExperienceIcon));
                    experience.transform.SetParent(row.transform, false);
                    experience.GetComponent<PrototypeExperienceIcon>().raycastTarget = false;
                    SelectionUi.Place((RectTransform)experience.transform, new Vector2(-47, 0), new Vector2(30, 30));
                }
                else
                {
                    var icon = new GameObject("Stage Reward Icon", typeof(RectTransform), typeof(PrototypeEconomyIcon));
                    icon.transform.SetParent(row.transform, false);
                    icon.GetComponent<PrototypeEconomyIcon>().SetKind(i == 1 ? EconomyIconKind.OriginShard : EconomyIconKind.Blueprint);
                    icon.GetComponent<PrototypeEconomyIcon>().raycastTarget = false;
                    SelectionUi.Place((RectTransform)icon.transform, new Vector2(-47, 0), new Vector2(30, 30));
                }
                _rewardAmounts[i] = SelectionUi.Label(row.transform, "Stage Reward Amount", "", 22, new Vector2(17, 0), new Vector2(88, 32));
                _rewardAmounts[i].alignment = TextAnchor.MiddleLeft;
            }
            SelectionUi.Label(_rewardSlots[2], "Random Blueprint Caption", "무작위 설계도", 11,
                new Vector2(0, -24), new Vector2(160, 18));
            _error = SelectionUi.Label(_frame.Panel, "Stage Economy Error", "", 14, new Vector2(195, -201), new Vector2(580, 31));
            _error.color = new Color(1, .45f, .4f);
        }
        public void Show(IReadOnlyList<StageData> stages, StageData current, PlayerProgress progress, IReadOnlyList<StageData> mines = null)
        {
            _normalStages = stages; _mineStages = mines ?? Array.Empty<StageData>();
            _showMines = current?.IsOriginMine ?? false;
            RenderList(current, progress);
        }
        private void SwitchCategory(bool mines)
        {
            _showMines = mines;
            RenderList(null, _progress);
        }
        private void RenderList(StageData current, PlayerProgress progress)
        {
            IReadOnlyList<StageData> stages = _showMines ? _mineStages : _normalStages;
            _progress = progress; _stageOrder = stages.Select(s => s.StageId).ToArray();
            _error.text = "";
            _normalMode.GetComponent<Image>().color = _showMines ? SelectionUi.Idle : SelectionUi.Selected;
            _mineMode.GetComponent<Image>().color = _showMines ? SelectionUi.Selected : SelectionUi.Idle;
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
            _sweep.gameObject.SetActive(stage?.IsOriginMine == true);
            _rewardRoot.gameObject.SetActive(stage != null);
            if (stage == null) { _details.text = _record.text = ""; return; }
            string Names(bool bosses) => string.Join(" · ", stage.Rounds.Where(r => r != null && (!bosses || r.IsBoss))
                .Select(r => r.Encounter?.DisplayName).Where(n => !string.IsNullOrEmpty(n)).Distinct().Take(4));
            string bossNames = Names(true);
            _details.text = $"{stage.RoundCount}라운드\n\n주요 적  {Names(false)}\n\n보스  {(bossNames.Length == 0 ? "없음" : bossNames)}";
            _rewardSlots[0].gameObject.SetActive(!stage.IsOriginMine);
            _rewardSlots[2].gameObject.SetActive(stage.IsOriginMine);
            _rewardSlots[0].anchoredPosition = new Vector2(-97, 0);
            _rewardSlots[1].anchoredPosition = new Vector2(stage.IsOriginMine ? -97 : 97, 0);
            _rewardSlots[2].anchoredPosition = new Vector2(97, 0);
            _rewardAmounts[0].text = CompactNumber.Format(stage.ClearExperience);
            _rewardAmounts[1].text = CompactNumber.Format(stage.ClearOriginShards);
            _rewardAmounts[2].text = CompactNumber.Format(stage.ClearBlueprints);
            if (!_confirm.interactable) _details.text = "스테이지 설정을 확인해주세요.";
            StageProgress record = _progress.GetStage(stage.StageId);
            RefreshTickets();
            _record.text = record.Cleared ? $"클리어\n최고 도달 {record.HighestRound} / {stage.RoundCount}라운드"
                : record.HighestRound > 0 ? $"미클리어\n최고 도달 {record.HighestRound} / {stage.RoundCount}라운드" : "도전 기록 없음";
            foreach (var choice in _choices) choice.Value.GetComponent<Image>().color = !IsUnlocked(choice.Key)
                ? new Color(.24f, .24f, .24f) : choice.Key == stage ? SelectionUi.Selected : SelectionUi.Idle;
        }
        public void RefreshTickets()
        {
            _sweep.interactable = _selected != null && _selected.IsOriginMine && _progress != null
                && _progress.GetStage(_selected.StageId).Cleared && _progress.MineTickets > 0;
            _sweep.GetComponent<CanvasGroup>().alpha = _sweep.interactable ? 1 : .35f;
        }
        public void RefreshProgress(PlayerProgress progress)
        {
            _progress = progress;
            RefreshTickets();
        }
        public void ShowError(string error) => _error.text = error ?? "";
        public void Hide() => _frame.Hide();
    }
}
