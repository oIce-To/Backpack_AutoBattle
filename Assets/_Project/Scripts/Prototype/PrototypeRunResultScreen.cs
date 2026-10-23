using System;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Presentation only: no scene loading, life changes, inventory reset or reward writes.</summary>
    public sealed class PrototypeRunResultScreen
    {
        public RectTransform Root { get; }
        private readonly Text _title;
        private readonly Text _summary;
        private readonly Button _restart;

        public PrototypeRunResultScreen(Transform canvas, Action restart)
        {
            var obj = new GameObject("Run Result Screen", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(canvas, false); Root = (RectTransform)obj.transform;
            BackpackPrototypeController.Stretch(Root);
            obj.GetComponent<Image>().color = new Color(.015f, .025f, .04f, .90f);
            var panel = new GameObject("Run Result Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(Root, false);
            panel.GetComponent<Image>().color = new Color(.08f, .11f, .17f);
            BackpackPrototypeController.SetRect((RectTransform)panel.transform, Vector2.one * .5f, Vector2.zero, new Vector2(610, 450));
            _title = Text(panel.transform, "Run Result Title", 32, new Vector2(0, 164), new Vector2(550, 48));
            _summary = Text(panel.transform, "Run Result Summary", 20, new Vector2(0, 20), new Vector2(550, 218));
            _summary.lineSpacing = 1.4f;
            var button = new GameObject("Restart Run Button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(panel.transform, false);
            button.GetComponent<Image>().color = new Color(.23f, .46f, .68f);
            BackpackPrototypeController.SetRect((RectTransform)button.transform, Vector2.one * .5f, new Vector2(0, -160), new Vector2(330, 54));
            _restart = button.GetComponent<Button>(); _restart.onClick.AddListener(() => restart());
            var label = Text(button.transform, "Restart Run Label", 20, Vector2.zero, new Vector2(320, 48));
            label.text = "같은 스테이지 다시 시작";
            Hide();
        }

        public void Show(PrototypeRunResult result)
        {
            bool cleared = result.Outcome == PrototypeRunOutcome.Cleared;
            _title.text = cleared ? "스테이지 클리어!" : "게임 오버";
            _title.color = cleared ? new Color(1, .8f, .24f) : new Color(1, .4f, .4f);
            _summary.text = $"{result.StageName ?? result.StageId ?? "스테이지"}\n"
                + $"{(cleared ? "완료" : "도달")} 라운드  {result.Round} / {result.RoundCount}\n"
                + $"남은 라이프  {result.Lives} / {result.StartingLives}\n"
                + $"승리 {result.Victories} · 패배 {result.Defeats}\n"
                + $"누적 전투 시간  {result.TotalBattleSeconds:0.0}초";
            _restart.interactable = true;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        public void Hide() => Root.gameObject.SetActive(false);
        public void SetRestartPending() => _restart.interactable = false;
        private static Text Text(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
        {
            var text = BackpackPrototypeController.CreateText(parent, name, "", size, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            BackpackPrototypeController.SetRect(text.rectTransform, Vector2.one * .5f, position, dimensions);
            return text;
        }
    }
}
