using System.Linq;
using BackpackAutoBattle.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeCombatStatisticsView
    {
        public RectTransform Root { get; }
        private readonly RectTransform _content, _tip;
        private readonly Text _subtitle, _totals, _tipTitle, _tipBody;
        private readonly Button _player, _enemy, _attack, _defense;
        private readonly ScrollRect _scroll;
        private CombatStatisticsSnapshot _snapshot;
        private CombatSide _side;
        private StatisticsChartMode _mode;
        private PrototypeStatisticsBarHover _hoverOwner;

        public PrototypeCombatStatisticsView(Transform canvas)
        {
            Root = Box(canvas, "Combat Statistics Screen", new Color(.015f, .025f, .04f, .94f));
            BackpackPrototypeController.Stretch(Root);
            var panel = Box(Root, "Combat Statistics Panel", new Color(.08f, .11f, .17f));
            Place(panel, Vector2.zero, new Vector2(940, 580));
            Label(panel, "Statistics Title", "전투 통계", 28, new Vector2(0, 246), new Vector2(850, 40));
            _subtitle = Label(panel, "Statistics Subtitle", "", 16, new Vector2(0, 207), new Vector2(850, 28));
            _player = Button(panel, "Player Statistics Tab", "플레이어", new Vector2(-100, 166), new Vector2(180, 34), () => SelectSide(CombatSide.Player));
            _enemy = Button(panel, "Enemy Statistics Tab", "적", new Vector2(100, 166), new Vector2(180, 34), () => SelectSide(CombatSide.Enemy));
            _attack = CategoryButton(panel, "Attack Statistics Tab", StatisticsChartMode.Attack, new Vector2(-100, 123));
            _defense = CategoryButton(panel, "Defense Statistics Tab", StatisticsChartMode.Defense, new Vector2(100, 123));
            var viewport = Box(panel, "Statistics Viewport", new Color(.055f, .08f, .13f));
            Place(viewport, new Vector2(0, -44), new Vector2(880, 280)); viewport.gameObject.AddComponent<RectMask2D>();
            _content = new GameObject("Statistics Rows", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = Vector2.one; _content.pivot = new Vector2(.5f, 1);
            _content.sizeDelta = Vector2.zero;
            _scroll = viewport.gameObject.AddComponent<ScrollRect>(); _scroll.viewport = viewport; _scroll.content = _content;
            _scroll.horizontal = false; _scroll.vertical = true; _scroll.scrollSensitivity = 40; _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.onValueChanged.AddListener(_ => HideBreakdown());
            _totals = Label(panel, "Statistics Totals", "", 18, new Vector2(0, -214), new Vector2(870, 30));
            Button(panel, "Close Statistics Button", "닫기", new Vector2(0, -262), new Vector2(180, 36), Hide);

            _tip = Box(Root, "Statistics Breakdown Tooltip", new Color(.10f, .14f, .21f, .99f));
            _tip.anchorMin = _tip.anchorMax = Vector2.one * .5f;
            _tip.pivot = new Vector2(0, 1);
            _tip.GetComponent<Image>().raycastTarget = false;
            var group = _tip.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            _tipTitle = Label(_tip, "Statistics Breakdown Title", "", 18, Vector2.zero, new Vector2(276, 28));
            _tipTitle.fontStyle = FontStyle.Bold; _tipTitle.alignment = TextAnchor.MiddleLeft;
            _tipBody = Label(_tip, "Statistics Breakdown Body", "", 16, Vector2.zero, new Vector2(276, 100));
            _tipBody.alignment = TextAnchor.UpperLeft; _tipBody.lineSpacing = 1.2f;
            foreach (var label in new[] { _tipTitle, _tipBody })
            {
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
                label.rectTransform.pivot = new Vector2(0, 1);
            }
            _tipTitle.rectTransform.anchoredPosition = new Vector2(12, -10);
            _tipBody.rectTransform.anchoredPosition = new Vector2(12, -44);
            Hide();
        }

        public void Show(CombatStatisticsSnapshot snapshot)
        {
            _snapshot = snapshot; _side = CombatSide.Player; _mode = StatisticsChartMode.Attack;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling(); Render();
        }
        public void Hide() { HideBreakdown(); Root.gameObject.SetActive(false); _snapshot = null; ClearRows(); }
        private void ClearRows()
        {
            HideBreakdown();
            foreach (Transform child in _content) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
        }
        private void SelectSide(CombatSide side) { _side = side; Render(); }
        private void SelectMode(StatisticsChartMode mode) { _mode = mode; Render(); }
        private void Render()
        {
            if (_snapshot == null) return;
            ClearRows(); _scroll.StopMovement(); _content.anchoredPosition = Vector2.zero;
            _player.interactable = _side != CombatSide.Player; _enemy.interactable = _side != CombatSide.Enemy;
            _attack.interactable = _mode != StatisticsChartMode.Attack; _defense.interactable = _mode != StatisticsChartMode.Defense;
            _subtitle.text = $"{(_side == CombatSide.Player ? _snapshot.PlayerName : _snapshot.EnemyName)} · 전투 시간 {_snapshot.Seconds:0.0}초";
            var rows = CombatStatisticsChart.Sort(_snapshot, _side, _mode);
            double maximum = rows.Length > 0 ? CombatStatisticsChart.Total(rows[0], _mode) : 0;
            _content.sizeDelta = new Vector2(0, Mathf.Max(280, rows.Length * 52));
            for (int i = 0; i < rows.Length; i++)
            {
                var item = rows[i]; var row = Box(_content, "Statistics Row " + (item.InstanceId ?? "Other"),
                    i % 2 == 0 ? new Color(.10f, .14f, .20f) : new Color(.08f, .12f, .18f));
                BackpackPrototypeController.SetRect(row, new Vector2(.5f, 1), new Vector2(0, -26 - i * 52), new Vector2(876, 50));
                var title = Label(row, "Statistics Item Name", item.DisplayName, 16, new Vector2(-300, 0), new Vector2(240, 46));
                title.alignment = TextAnchor.MiddleLeft;
                double total = CombatStatisticsChart.Total(item, _mode);
                float width = maximum > 0 ? (float)(480 * total / maximum) : 0;
                var bar = Box(row, "Statistics Bar", Color.clear);
                Place(bar, new Vector2(-145, 0), new Vector2(Mathf.Max(1, width), 28));
                bar.pivot = new Vector2(0, .5f); bar.anchoredPosition = new Vector2(-145, 0);
                bar.gameObject.AddComponent<PrototypeStatisticsBarHover>().Bind(this, item, _mode);
                var segments = CombatStatisticsChart.Segments(item, _mode);
                float offset = 0;
                for (int j = 0; j < segments.Count; j++)
                {
                    var segment = segments[j];
                    float length = j == segments.Count - 1 ? Mathf.Max(0, width - offset) : (float)(width * segment.Value / total);
                    var fill = Box(bar, "Statistics Segment " + segment.Metric, segment.Color);
                    fill.GetComponent<Image>().raycastTarget = false;
                    BackpackPrototypeController.SetRect(fill, new Vector2(0, .5f), new Vector2(offset, 0), new Vector2(length, 24));
                    fill.pivot = new Vector2(0, .5f); fill.anchoredPosition = new Vector2(offset, 0);
                    offset += length;
                }
                var value = Label(row, "Statistics Value", CombatStatisticsChart.Format(total), 18, new Vector2(380, 0), new Vector2(90, 40));
                value.resizeTextForBestFit = true; value.resizeTextMinSize = 12; value.resizeTextMaxSize = 18;
            }
            if (rows.Length == 0) Label(_content, "Statistics Empty", "기록할 아이템이 없습니다.", 18, new Vector2(0, -80), new Vector2(800, 40));
            _totals.text = $"합계  ·  공격 {CombatStatisticsChart.Format(rows.Sum(r => r.DamageDealt))}  ·  방어 {CombatStatisticsChart.Format(rows.Sum(r => r.DefenseTotal))}";
        }

        internal void ShowBreakdown(PrototypeStatisticsBarHover owner, CombatItemStatistics item, StatisticsChartMode mode, PointerEventData pointer)
        {
            _hoverOwner = owner;
            var segments = CombatStatisticsChart.Segments(item, mode);
            _tipTitle.text = item.DisplayName;
            _tipBody.text = string.Join("\n", segments.Select(s => $"<color=#{ColorUtility.ToHtmlStringRGB(s.Color)}>{s.Label}  {CombatStatisticsChart.Format(s.Value)}</color>"))
                + (segments.Count > 0 ? "\n" : "") + $"합계  {CombatStatisticsChart.Format(CombatStatisticsChart.Total(item, mode))}";
            float height = 64 + (segments.Count + 1) * 25;
            _tip.sizeDelta = new Vector2(300, height); _tipBody.rectTransform.sizeDelta = new Vector2(276, height - 54);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, pointer.position, pointer.enterEventCamera, out Vector2 point);
            _tip.anchoredPosition = new Vector2(Mathf.Clamp(point.x + 16, Root.rect.xMin + 8, Root.rect.xMax - 308),
                Mathf.Clamp(point.y - 12, Root.rect.yMin + height + 8, Root.rect.yMax - 8));
            _tip.gameObject.SetActive(true); _tip.SetAsLastSibling();
        }
        internal void HideBreakdown(PrototypeStatisticsBarHover owner = null)
        {
            if (owner != null && owner != _hoverOwner) return;
            _hoverOwner = null;
            if (_tip != null) _tip.gameObject.SetActive(false);
        }
        private Button CategoryButton(Transform parent, string name, StatisticsChartMode mode, Vector2 position)
        {
            var button = Button(parent, name, "", position, new Vector2(180, 36), () => SelectMode(mode));
            var obj = new GameObject("Statistics Category Glyph", typeof(RectTransform), typeof(PrototypeStatisticsTabGlyph));
            obj.transform.SetParent(button.transform, false);
            var glyph = obj.GetComponent<PrototypeStatisticsTabGlyph>(); glyph.Mode = mode; glyph.color = Color.white; glyph.raycastTarget = false;
            Place(glyph.rectTransform, Vector2.zero, new Vector2(30, 30)); return button;
        }
        private static RectTransform Box(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            obj.GetComponent<Image>().color = color; return (RectTransform)obj.transform;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size) => BackpackPrototypeController.SetRect(rect, Vector2.one * .5f, position, size);
        private static Text Label(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var text = BackpackPrototypeController.CreateText(parent, name, value, fontSize, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.raycastTarget = false; Place(text.rectTransform, position, size); return text;
        }
        private static Button Button(Transform parent, string name, string value, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var rect = Box(parent, name, new Color(.23f, .46f, .68f)); Place(rect, position, size);
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(click);
            Label(rect, name + " Label", value, 18, Vector2.zero, size); return button;
        }
    }
}
