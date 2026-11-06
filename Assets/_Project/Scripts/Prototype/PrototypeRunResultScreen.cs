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
        private readonly Text _experience, _notice;
        private readonly Button _restart;
        private readonly Button _mainMenu;

        public PrototypeRunResultScreen(Transform canvas, Action restart, Action statistics, Action mainMenu)
        {
            var obj = new GameObject("Run Result Screen", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(canvas, false); Root = (RectTransform)obj.transform;
            BackpackPrototypeController.Stretch(Root);
            obj.GetComponent<Image>().color = new Color(.015f, .025f, .04f, .90f);
            var panel = new GameObject("Run Result Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(Root, false);
            panel.GetComponent<Image>().color = new Color(.08f, .11f, .17f);
            BackpackPrototypeController.SetRect((RectTransform)panel.transform, Vector2.one * .5f, Vector2.zero, new Vector2(610, 500));
            _title = Text(panel.transform, "Run Result Title", 32, new Vector2(0, 164), new Vector2(550, 48));
            _summary = Text(panel.transform, "Run Result Summary", 20, new Vector2(0, 52), new Vector2(550, 184));
            _summary.lineSpacing = 1.4f;
            _experience = Text(panel.transform, "Run Result Experience", 23, new Vector2(0, -70), new Vector2(550, 34));
            _experience.color = new Color(.95f, .88f, .72f);
            _notice = Text(panel.transform, "Run Result Save Notice", 14, new Vector2(0, -232), new Vector2(560, 26));
            _notice.color = new Color(1, .45f, .4f);
            var button = new GameObject("Restart Run Button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(panel.transform, false);
            button.GetComponent<Image>().color = new Color(.23f, .46f, .68f);
            BackpackPrototypeController.SetRect((RectTransform)button.transform, Vector2.one * .5f, new Vector2(-140, -180), new Vector2(280, 54));
            _restart = button.GetComponent<Button>(); _restart.onClick.AddListener(() => restart());
            var label = Text(button.transform, "Restart Run Label", 18, Vector2.zero, new Vector2(270, 48));
            label.text = "같은 스테이지 다시 시작";
            var main = new GameObject("Return To Main Menu Button", typeof(RectTransform), typeof(Image), typeof(Button));
            main.transform.SetParent(panel.transform, false);
            main.GetComponent<Image>().color = new Color(.16f, .21f, .29f);
            BackpackPrototypeController.SetRect((RectTransform)main.transform, Vector2.one * .5f, new Vector2(150, -180), new Vector2(260, 54));
            _mainMenu = main.GetComponent<Button>(); _mainMenu.onClick.AddListener(() => mainMenu());
            Text(main.transform, "Return To Main Menu Label", 18, Vector2.zero, new Vector2(250, 48)).text = "메인으로 돌아가기";
            var stats = new GameObject("Run Statistics Button", typeof(RectTransform), typeof(Image), typeof(Button));
            stats.transform.SetParent(panel.transform, false); stats.GetComponent<Image>().color = new Color(.23f, .46f, .68f);
            BackpackPrototypeController.SetRect((RectTransform)stats.transform, Vector2.one * .5f, new Vector2(0, -120), new Vector2(190, 36));
            stats.GetComponent<Button>().onClick.AddListener(() => statistics());
            Text(stats.transform, "Run Statistics Label", 18, Vector2.zero, new Vector2(180, 34)).text = "전투 통계";
            Hide();
        }

        public void Show(PrototypeRunResult result, long? experience = null, string saveNotice = null)
        {
            bool cleared = result.Outcome == PrototypeRunOutcome.Cleared;
            _title.text = cleared ? "스테이지 클리어!" : "게임 오버";
            _title.color = cleared ? new Color(1, .8f, .24f) : new Color(1, .4f, .4f);
            _summary.text = $"{result.StageName ?? result.StageId ?? "스테이지"}\n"
                + $"{(cleared ? "완료" : "도달")} 라운드  {result.Round} / {result.RoundCount}\n"
                + $"승리 {result.Victories} · 패배 {result.Defeats}\n"
                + $"누적 전투 시간  {result.TotalBattleSeconds:0.0}초";
            _experience.text = experience.HasValue ? $"획득 경험치  +{experience.Value:N0}" : "";
            _notice.text = saveNotice ?? "";
            _restart.interactable = true;
            _mainMenu.interactable = true;
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
        }

        public void Hide() => Root.gameObject.SetActive(false);
        public void SetRestartPending() { _restart.interactable = false; _mainMenu.interactable = false; }
        private static Text Text(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
        {
            var text = BackpackPrototypeController.CreateText(parent, name, "", size, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            BackpackPrototypeController.SetRect(text.rectTransform, Vector2.one * .5f, position, dimensions);
            return text;
        }
    }
}
