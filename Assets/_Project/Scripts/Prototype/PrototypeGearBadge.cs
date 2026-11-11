using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal static class PrototypeGearBadge
    {
        internal static void Render(Transform parent, UniqueGearData definition, UniqueGearInstance item, int equippedMarkerSize = 14)
        {
            var badge = parent.Find("Gear Badge") as RectTransform;
            if (definition == null || item == null)
            {
                if (badge != null) badge.gameObject.SetActive(false);
                return;
            }
            if (badge == null)
            {
                var obj = new GameObject("Gear Badge", typeof(RectTransform));
                obj.transform.SetParent(parent, false);
                badge = (RectTransform)obj.transform;
                var glyph = new GameObject("Gear Glyph", typeof(RectTransform), typeof(PrototypeGearGlyph));
                glyph.transform.SetParent(badge, false);
                glyph.GetComponent<PrototypeGearGlyph>().raycastTarget = false;
                var sprite = SelectionUi.Image(badge, "Gear Sprite", Color.white);
                sprite.preserveAspect = true;
                AddMarker(badge, "Gear Rank", new Vector2(0, 0), TextAnchor.MiddleLeft);
                AddMarker(badge, "Gear Enhancement", new Vector2(1, 0), TextAnchor.MiddleRight);
                AddMarker(badge, "Gear Equipped", new Vector2(1, 1), TextAnchor.MiddleRight, equippedMarkerSize);
            }
            badge.gameObject.SetActive(true);
            BackpackPrototypeController.Stretch(badge);
            var icon = badge.Find("Gear Sprite").GetComponent<Image>();
            icon.sprite = definition.Icon;
            icon.enabled = definition.Icon != null;
            BackpackPrototypeController.Stretch(icon.rectTransform, definition.IconInset);
            icon.rectTransform.localScale = Vector3.one * definition.IconScale;
            var drawn = badge.GetComponentInChildren<PrototypeGearGlyph>(true);
            drawn.enabled = !icon.enabled;
            BackpackPrototypeController.Stretch(drawn.rectTransform);
            drawn.Configure(definition.Slot, definition.Item.Tags, item.Rarity.GetDisplayColor());
            var rank = badge.Find("Gear Rank").GetComponent<Text>();
            rank.color = item.Rarity.GetDisplayColor();
            rank.text = item.Rarity.GetDisplayName();
            badge.Find("Gear Enhancement").GetComponent<Text>().text = $"+{item.Enhancement}";
            var equipped = badge.Find("Gear Equipped").GetComponent<Text>();
            equipped.text = "E";
            equipped.color = new Color(.62f, .94f, .72f);
            equipped.gameObject.SetActive(!string.IsNullOrEmpty(item.EquippedBy));
            var locked = badge.Find("Gear Locked") as RectTransform;
            if (locked == null)
            {
                var obj = new GameObject("Gear Locked", typeof(RectTransform), typeof(PrototypeShopLockIcon));
                obj.transform.SetParent(badge, false); locked = (RectTransform)obj.transform;
                var glyph = obj.GetComponent<PrototypeShopLockIcon>(); glyph.raycastTarget = false;
                glyph.color = new Color(1f, .81f, .19f);
            }
            float lockSize = equippedMarkerSize >= 28 ? 28 : 20;
            locked.anchorMin = locked.anchorMax = new Vector2(0, 1);
            locked.sizeDelta = Vector2.one * lockSize;
            locked.anchoredPosition = new Vector2(4 + lockSize / 2, -4 - lockSize / 2);
            locked.gameObject.SetActive(item.IsLocked);
            RenderBorder(badge, item.Rarity);
        }

        internal static void RenderBorder(Transform parent, ItemRarity rarity)
        {
            var border = parent.Find("Gear Rarity Border") as RectTransform;
            if (border == null)
            {
                var obj = new GameObject("Gear Rarity Border", typeof(RectTransform));
                obj.transform.SetParent(parent, false); border = (RectTransform)obj.transform;
                BackpackPrototypeController.Stretch(border);
                for (int i = 0; i < 4; i++)
                {
                    var edge = SelectionUi.Image(border, "Edge " + i, Color.white).rectTransform;
                    bool horizontal = i < 2;
                    edge.anchorMin = horizontal ? new Vector2(0, i) : new Vector2(i - 2, 0);
                    edge.anchorMax = horizontal ? new Vector2(1, i) : new Vector2(i - 2, 1);
                    edge.sizeDelta = horizontal ? new Vector2(0, 1.5f) : new Vector2(1.5f, 0);
                    edge.anchoredPosition = horizontal ? new Vector2(0, i == 0 ? .75f : -.75f)
                        : new Vector2(i == 2 ? .75f : -.75f, 0);
                }
            }
            foreach (var edge in border.GetComponentsInChildren<Image>()) edge.color = rarity.GetDisplayColor();
        }

        private static void AddMarker(RectTransform badge, string name, Vector2 anchor, TextAnchor alignment, int fontSize = 14)
        {
            var label = SelectionUi.Label(badge, name, "", fontSize, Vector2.zero, new Vector2(42, fontSize + 6));
            label.alignment = alignment;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = anchor;
            label.rectTransform.anchoredPosition = new Vector2(anchor.x == 0 ? 6f : -6f, anchor.y == 0 ? 3f : -3f);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.035f, .04f, .07f, .98f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class PrototypeGearGlyph : MaskableGraphic
    {
        private UniqueGearSlot _slot;
        private ItemTags _tags;
        public PrototypeGearGlyph() { useLegacyMeshGeneration = false; }

        internal void Configure(UniqueGearSlot slot, ItemTags tags, Color tint)
        {
            _slot = slot;
            _tags = tags;
            color = tint;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 Point(float x, float y) => rect.min + Vector2.Scale(rect.size, new Vector2(x, y));
            void Segment(float ax, float ay, float bx, float by, float width)
            {
                Vector2 a = Point(ax, ay), b = Point(bx, by);
                Vector2 tangent = (b - a).normalized;
                Vector2 offset = new Vector2(-tangent.y, tangent.x) * (rect.width * width * .5f);
                int i = mesh.currentVertCount;
                mesh.AddVert(a - offset, color, Vector2.zero);
                mesh.AddVert(a + offset, color, Vector2.zero);
                mesh.AddVert(b + offset, color, Vector2.zero);
                mesh.AddVert(b - offset, color, Vector2.zero);
                mesh.AddTriangle(i, i + 1, i + 2);
                mesh.AddTriangle(i, i + 2, i + 3);
            }

            if (_slot == UniqueGearSlot.Weapon && (_tags & ItemTags.Ranged) != 0)
            {
                Segment(.24f, .20f, .52f, .22f, .08f);
                Segment(.52f, .22f, .75f, .49f, .10f);
                Segment(.75f, .49f, .78f, .78f, .10f);
                Segment(.24f, .20f, .78f, .78f, .045f);
                Segment(.25f, .74f, .74f, .25f, .07f);
                Segment(.74f, .25f, .56f, .27f, .065f);
                Segment(.74f, .25f, .72f, .43f, .065f);
            }
            else if (_slot == UniqueGearSlot.Weapon)
            {
                Segment(.32f, .32f, .78f, .78f, .13f);
                Segment(.70f, .70f, .83f, .83f, .06f);
                Segment(.23f, .23f, .38f, .38f, .07f);
                Segment(.25f, .46f, .46f, .25f, .07f);
            }
            else
            {
                float shoulder = _slot == UniqueGearSlot.Armor ? .71f : .50f;
                Segment(.50f, .82f, .24f, shoulder, .075f);
                Segment(.24f, shoulder, .50f, .18f, .075f);
                Segment(.50f, .18f, .76f, shoulder, .075f);
                Segment(.76f, shoulder, .50f, .82f, .075f);
            }
        }
    }
}
