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
            var icon = badge.GetComponentInChildren<Image>(true);
            icon.sprite = definition.Icon;
            icon.enabled = definition.Icon != null;
            BackpackPrototypeController.Stretch(icon.rectTransform, definition.IconInset);
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
