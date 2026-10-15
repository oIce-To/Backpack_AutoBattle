using System;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeItemTooltip : MonoBehaviour
    {
        private const float Width = 390f;
        private const float Padding = 18f;
        private RectTransform _canvasRect;
        private Image _border;
        private Text _name;
        private Text _rarity;
        private Text _context;
        private RectTransform _statsRoot;
        private Image _divider;
        private Text _description;
        private readonly Text[] _values = new Text[5];
        private readonly RectTransform[] _statIcons = new RectTransform[5];
        private readonly Text[] _statCaptions = new Text[5];
        private Component _owner;
        private Func<ItemTooltipContent> _content;
        private Vector2 _pointer;
        private Camera _eventCamera;

        public RectTransform RectTransform { get; private set; }
        public bool IsVisible => gameObject.activeSelf;
        public Component Owner => _owner;

        public static PrototypeItemTooltip Create(Transform canvas)
        {
            var obj = new GameObject("Item Tooltip", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(PrototypeItemTooltip));
            obj.transform.SetParent(canvas, false);
            PrototypeItemTooltip tooltip = obj.GetComponent<PrototypeItemTooltip>();
            tooltip.Build((RectTransform)canvas);
            return tooltip;
        }

        private void Build(RectTransform canvas)
        {
            _canvasRect = canvas;
            RectTransform = (RectTransform)transform;
            RectTransform.anchorMin = RectTransform.anchorMax = Vector2.zero;
            RectTransform.pivot = new Vector2(0f, 1f);
            RectTransform.sizeDelta = new Vector2(Width, 290f);
            _border = GetComponent<Image>();
            _border.raycastTarget = false;
            CanvasGroup group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Image background = CreateImage(transform, "Background", new Color(0.07f, 0.095f, 0.14f, 0.99f));
            BackpackPrototypeController.Stretch(background.rectTransform, 2f);
            _name = Label(transform, "Tooltip Name", 25, FontStyle.Bold);
            Place(_name.rectTransform, Padding, -26f, 245f, 36f);
            _rarity = Label(transform, "Tooltip Rarity", 20, FontStyle.Bold);
            _rarity.alignment = TextAnchor.MiddleRight;
            Place(_rarity.rectTransform, 267f, -26f, 104f, 36f);
            _context = Label(transform, "Tooltip Context", 12, FontStyle.Normal);
            _context.color = new Color(0.60f, 0.68f, 0.79f);
            Place(_context.rectTransform, Padding, -57f, Width - Padding * 2f, 20f);

            var statsObject = new GameObject("Tooltip Stats", typeof(RectTransform));
            statsObject.transform.SetParent(transform, false);
            _statsRoot = (RectTransform)statsObject.transform;
            BackpackPrototypeController.Stretch(_statsRoot);
            string[] captions = { "공격력", "사용 주기", "명중률", "치명타 확률", "치명타 피해" };
            string[] names = { "Attack", "Cooldown", "Accuracy", "Critical Chance", "Critical Damage" };
            for (int index = 0; index < 5; index++)
            {
                float x = Padding + (index % 3) * 119f;
                float y = index < 3 ? -101f : -161f;
                var iconObject = new GameObject($"{names[index]} Icon", typeof(RectTransform), typeof(PrototypeStatIcon));
                iconObject.transform.SetParent(_statsRoot, false);
                PrototypeStatIcon icon = iconObject.GetComponent<PrototypeStatIcon>();
                icon.SetKind((WeaponStatIcon)index);
                icon.color = new Color(0.90f, 0.94f, 1f);
                icon.raycastTarget = false;
                Place((RectTransform)iconObject.transform, x, y, 31f, 31f);
                _statIcons[index] = (RectTransform)iconObject.transform;
                _values[index] = Label(_statsRoot, $"{names[index]} Value", 20, FontStyle.Bold);
                Place(_values[index].rectTransform, x + 36f, y, 78f, 32f);
                Text caption = Label(_statsRoot, $"{names[index]} Caption", 12, FontStyle.Normal);
                caption.text = captions[index];
                _statCaptions[index] = caption;
                caption.color = new Color(0.67f, 0.75f, 0.86f);
                Place(caption.rectTransform, x + 2f, y - 29f, 114f, 20f);
            }

            _divider = CreateImage(transform, "Divider", new Color(0.28f, 0.35f, 0.44f));
            _description = Label(transform, "Tooltip Description", 16, FontStyle.Normal);
            _description.alignment = TextAnchor.UpperLeft;
            _description.horizontalOverflow = HorizontalWrapMode.Wrap;
            gameObject.SetActive(false);
        }

        public void Show(Component owner, Func<ItemTooltipContent> content, Vector2 pointer, Camera eventCamera)
        {
            _owner = owner;
            _content = content;
            _pointer = pointer;
            _eventCamera = eventCamera;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Move(Component owner, Vector2 pointer, Camera eventCamera)
        {
            if (_owner != owner) return;
            _pointer = pointer;
            _eventCamera = eventCamera;
            Position();
        }

        public void Hide(Component owner)
        {
            if (_owner == owner) HideAll();
        }

        public void HideAll()
        {
            _owner = null;
            _content = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (_owner == null || !_owner.gameObject.activeInHierarchy || _content == null)
            {
                HideAll();
                return;
            }
            ItemTooltipContent item = _content();
            if (item == null) { HideAll(); return; }
            _name.text = item.DisplayName;
            _rarity.text = item.Rarity.GetDisplayName();
            _rarity.color = _border.color = item.Rarity.GetDisplayColor();
            _context.text = "전투 중 실제 스탯";
            _context.gameObject.SetActive(item.IsRuntime);
            _statsRoot.anchoredPosition = new Vector2(0f, item.IsRuntime ? 0f : 18f);
            _statsRoot.gameObject.SetActive(item.HasWeaponStats || item.HasCycle);
            for (int index = 0; index < 5; index++)
            {
                bool visible = item.HasWeaponStats || (item.HasCycle && index == 1);
                _statIcons[index].gameObject.SetActive(visible);
                _values[index].gameObject.SetActive(visible);
                _statCaptions[index].gameObject.SetActive(visible);
                float x = Padding + (item.HasWeaponStats ? index % 3 * 119f : 0f);
                float y = index < 3 ? -101f : -161f;
                Place(_statIcons[index], x, y, 31f, 31f);
                Place(_values[index].rectTransform, x + 36f, y, 78f, 32f);
                Place(_statCaptions[index].rectTransform, x + 2f, y - 29f, 114f, 20f);
            }
            _values[0].text = $"{item.AttackDamage:0.#}";
            _values[1].text = $"{item.CooldownSeconds:0.0#}s";
            _values[2].text = $"{item.AccuracyPercent:0.#}%";
            _values[3].text = $"{item.CriticalChancePercent:0.#}%";
            _values[4].text = $"{item.CriticalDamagePercent:0.#}%";
            float descriptionTop = (item.HasWeaponStats ? 211f : item.HasCycle ? 151f : 84f) - (item.IsRuntime ? 0f : 18f);
            string description = item.Description ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(item.EffectDescription))
                description = string.IsNullOrWhiteSpace(description) ? item.EffectDescription : $"{item.EffectDescription}\n\n{description}";
            _description.text = string.IsNullOrWhiteSpace(description) ? "추가 설명 없음" : description;
            float available = Mathf.Max(32f, _canvasRect.rect.height - descriptionTop - Padding * 2f);
            Place(_description.rectTransform, Padding, -descriptionTop - available / 2f, Width - Padding * 2f, available);
            float height = Mathf.Clamp(_description.preferredHeight, 32f, Mathf.Min(available, 260f));
            Place(_description.rectTransform, Padding, -descriptionTop - height / 2f, Width - Padding * 2f, height);
            Place(_divider.rectTransform, Padding, -descriptionTop + 12f, Width - Padding * 2f, 1f);
            RectTransform.sizeDelta = new Vector2(Width, descriptionTop + height + Padding);
            Position();
        }

        private void Position()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, _pointer, _eventCamera, out Vector2 pointer);
            Rect canvas = _canvasRect.rect;
            float px = pointer.x - canvas.xMin;
            float py = pointer.y - canvas.yMin;
            float x = px + 20f;
            if (x + Width > canvas.width - 8f) x = px - Width - 20f;
            x = Mathf.Clamp(x, 8f, Mathf.Max(8f, canvas.width - Width - 8f));
            float y = Mathf.Clamp(py + 18f, RectTransform.sizeDelta.y + 8f, canvas.height - 8f);
            RectTransform.anchoredPosition = new Vector2(x, y);
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Text Label(Transform parent, string name, int size, FontStyle style)
        {
            Text text = BackpackPrototypeController.CreateText(parent, name, string.Empty, size, style, TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
