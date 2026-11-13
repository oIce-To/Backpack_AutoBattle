using System;
using System.Collections.Generic;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeMainMenuScreen
    {
        public RectTransform Root { get; }
        private readonly Image _portrait;
        private readonly Text _name, _health, _attack, _error, _level, _emptyInventory;
        private readonly PrototypeItemTooltip _tooltip;
        private readonly PrototypeMenuPopup _inventory, _mail;
        private readonly ScrollRect _inventoryScroll;
        private PlayerProgress _profile;
        private IReadOnlyList<UniqueGearData> _gear;

        public PrototypeMainMenuScreen(Transform canvas, PrototypeItemTooltip tooltip, Action quit)
        {
            _tooltip = tooltip;
            var background = SelectionUi.Image(canvas, "Main Menu Screen", new Color(.035f, .04f, .055f));
            Root = background.rectTransform;
            BackpackPrototypeController.Stretch(Root);
            background.raycastTarget = true;
            var panel = SelectionUi.Image(Root, "Main Menu Panel", new Color(.07f, .075f, .085f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(1100, 610));

            var profile = MakeCard(panel.transform, "Main Profile Card", new Vector2(-347, 193), new Vector2(296, 79));
            SelectionUi.Label(profile, "Main Profile Name", "플레이어", 25, Vector2.zero, new Vector2(256, 48)).alignment = TextAnchor.MiddleLeft;

            var stats = MakeCard(panel.transform, "Main Character Card", new Vector2(-347, 23), new Vector2(296, 184));
            _name = SelectionUi.Label(stats, "Main Character Name", "", 27, new Vector2(-43, 49), new Vector2(170, 36));
            _name.alignment = TextAnchor.MiddleLeft;
            _level = SelectionUi.Label(stats, "Main Character Level", "", 19, new Vector2(85, 49), new Vector2(90, 36));
            _level.alignment = TextAnchor.MiddleRight;
            _level.color = new Color(.88f, .77f, .55f);
            _health = SelectionUi.Label(stats, "Main Character Health", "", 22, new Vector2(0, 2), new Vector2(256, 35));
            _health.alignment = TextAnchor.MiddleLeft;
            _attack = SelectionUi.Label(stats, "Main Character Attack", "", 22, new Vector2(0, -43), new Vector2(256, 35));
            _attack.alignment = TextAnchor.MiddleLeft;

            _portrait = SelectionUi.Image(panel.transform, "Main Character Illustration", Color.white);
            SelectionUi.Place(_portrait.rectTransform, new Vector2(12, -1), new Vector2(371, 523));
            _portrait.preserveAspect = true;
            _portrait.useSpriteMesh = false;

            AddShortcut(panel.transform, "Main Inventory Button", "가방", new Vector2(415, 163), "UI/HomeBackpackIcon", ShowInventory);
            AddShortcut(panel.transform, "Main Mail Button", "우편함", new Vector2(415, 40), "UI/HomeMailboxIcon", () => _mail.Show());
            SelectionUi.Button(panel.transform, "Main Quit Button", "게임 종료", new Vector2(415, -247), new Vector2(125, 38), quit);
            _error = SelectionUi.Label(panel.transform, "Main Menu Error", "", 14, new Vector2(-344, -252), new Vector2(300, 62));
            _error.color = new Color(1f, .45f, .4f);

            _inventory = new PrototypeMenuPopup(canvas, "Main Inventory Popup", "가방", new Vector2(674, 506), ClosePopup, PrototypeMenuPopup.CloseStyle.Pixel);
            var viewport = SelectionUi.Image(_inventory.Panel, "Main Blueprint Viewport", new Color(.065f, .08f, .11f));
            SelectionUi.Place(viewport.rectTransform, new Vector2(0, -22), new Vector2(608, 390));
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Main Blueprint Items", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(.5f, 1);
            _inventoryScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _inventoryScroll.viewport = viewport.rectTransform;
            _inventoryScroll.content = contentRect;
            _inventoryScroll.horizontal = false;
            _inventoryScroll.movementType = ScrollRect.MovementType.Clamped;
            _emptyInventory = SelectionUi.Label(viewport.transform, "Main Blueprint Empty", "보유한 설계도가 없습니다.", 19, Vector2.zero, new Vector2(525, 55));

            _mail = new PrototypeMenuPopup(canvas, "Main Mail Popup", "우편함", new Vector2(490, 274), ClosePopup, PrototypeMenuPopup.CloseStyle.Pixel);
            SelectionUi.Label(_mail.Panel, "Main Mail Empty", "수령할 편지가 없습니다.", 20, new Vector2(0, -15), new Vector2(418, 55));
            Hide();
        }

        internal bool IsPopupOpen => _inventory.IsOpen || _mail.IsOpen;

        public void Show(CharacterData character, PlayerProgress profile, CharacterStatsSnapshot stats, IReadOnlyList<UniqueGearData> gear)
        {
            _profile = profile;
            _gear = gear;
            _level.text = $"Lv. {profile.GetCharacterLevel(character?.CharacterId)}";
            _portrait.sprite = character?.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _name.text = character != null ? character.DisplayName : "캐릭터";
            _health.text = $"체력  {stats.MaximumHealth:0.##}";
            _attack.text = $"공격력  {stats.Attack:0.##}";
            _error.text = "";
            Root.gameObject.SetActive(true);
            Root.SetAsLastSibling();
        }

        private void ShowInventory()
        {
            if (_profile == null || IsPopupOpen) return;
            RectTransform content = _inventoryScroll.content;
            foreach (Transform previous in content)
            {
                previous.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(previous.gameObject);
            }
            const int columns = 5;
            const float stride = 115f;
            int count = 0;
            foreach (UniqueGearData definition in _gear)
            {
                long quantity = _profile.GetBlueprints(definition.GearId);
                if (quantity <= 0) continue;
                var tile = SelectionUi.Image(content, "Blueprint " + definition.GearId, SelectionUi.Idle);
                tile.raycastTarget = true;
                BackpackPrototypeController.SetRect(tile.rectTransform, new Vector2(.5f, 1),
                    new Vector2((count % columns - 2) * stride, -62 - count / columns * stride), Vector2.one * 103);
                PrototypeGearBadge.RenderBorder(tile.transform, ItemRarity.Common);
                if (definition.Icon != null)
                {
                    var illustration = SelectionUi.Image(tile.transform, "Blueprint Item Illustration", Color.white);
                    illustration.sprite = definition.Icon;
                    illustration.preserveAspect = true;
                    SelectionUi.Place(illustration.rectTransform, new Vector2(2, 4), Vector2.one * 79);
                }
                var paper = new GameObject("Blueprint Paper Icon", typeof(RectTransform), typeof(PrototypeEconomyIcon));
                paper.transform.SetParent(tile.transform, false);
                var glyph = paper.GetComponent<PrototypeEconomyIcon>();
                glyph.SetKind(EconomyIconKind.Blueprint);
                glyph.raycastTarget = false;
                SelectionUi.Place(glyph.rectTransform, definition.Icon == null ? Vector2.zero : new Vector2(-30, 29),
                    Vector2.one * (definition.Icon == null ? 61 : 28));
                var label = SelectionUi.Label(tile.transform, "Blueprint Quantity", CompactNumber.Format(quantity), 19,
                    new Vector2(1, -36), new Vector2(84, 24));
                label.alignment = TextAnchor.MiddleRight;
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.035f, .04f, .07f);
                outline.effectDistance = new Vector2(1.3f, -1.3f);
                tile.gameObject.AddComponent<PrototypeBlueprintHover>().Bind(_tooltip, definition.Item.DisplayName + " 설계도", quantity);
                count++;
            }
            _emptyInventory.gameObject.SetActive(count == 0);
            content.sizeDelta = new Vector2(0f, Mathf.Max(390, Mathf.CeilToInt(count / (float)columns) * stride + 8));
            _inventory.Show();
            _inventoryScroll.StopMovement();
            _inventoryScroll.verticalNormalizedPosition = 1;
        }

        internal void ClosePopup()
        {
            if (!IsPopupOpen) return;
            _tooltip.HideAll();
            _inventory.Hide();
            _mail.Hide();
        }

        public void ShowError(string error) => _error.text = (error ?? "").Split('\n')[0];

        public void Hide()
        {
            ClosePopup();
            Root.gameObject.SetActive(false);
        }

        private static RectTransform MakeCard(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(PrototypeRoundedPanel));
            card.transform.SetParent(parent, false);
            var face = card.GetComponent<PrototypeRoundedPanel>();
            face.color = new Color(.12f, .15f, .20f);
            face.DrawBorder = false;
            face.raycastTarget = false;
            SelectionUi.Place(face.rectTransform, position, size);
            return face.rectTransform;
        }

        private static void AddShortcut(Transform parent, string name, string caption, Vector2 center, string spritePath, Action open)
        {
            var icon = SelectionUi.Image(parent, name, Color.white);
            icon.sprite = Resources.Load<Sprite>(spritePath);
            icon.preserveAspect = true;
            icon.raycastTarget = true;
            SelectionUi.Place(icon.rectTransform, center + new Vector2(0, 10), new Vector2(84, 78));
            var button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => open());
            SelectionUi.Label(parent, name + " Label", caption, 19, center + new Vector2(0, -45), new Vector2(112, 27));
        }
    }
}
