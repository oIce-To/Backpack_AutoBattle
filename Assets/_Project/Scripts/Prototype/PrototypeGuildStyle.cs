using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // The lobby has its own square iron-bound joinery, independent of the in-stage skin.
    internal static class PrototypeGuildStyle
    {
        internal enum Surface { Oak, Paper, Iron, Button, Selected }
        private static readonly Dictionary<Surface, Sprite> Sprites = new();
        internal static readonly Color Ink = Hex(0x392c23);
        internal static readonly Color Cream = Hex(0xf0dfb4);
        internal static Color Hex(uint rgb) => new((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);

        internal static void Apply(Image image, Surface surface = Surface.Oak)
        {
            image.sprite = SpriteFor(surface);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        internal static void Apply(Button button, bool selected = false)
        {
            Apply(button.image, selected ? Surface.Selected : Surface.Button);
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .93f, .78f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.68f, .61f, .48f);
            colors.disabledColor = new Color(.45f, .44f, .40f);
            colors.fadeDuration = .06f;
            button.colors = colors;
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.color = selected ? Ink : Cream;
        }

        private static Sprite SpriteFor(Surface surface)
        {
            if (Sprites.TryGetValue(surface, out var cached) && cached != null) return cached;
            const int size = 40;
            bool paper = surface == Surface.Paper;
            Color fill = surface switch
            {
                Surface.Paper => Hex(0xc7ad79), Surface.Iron => Hex(0x293235),
                Surface.Selected => Hex(0xc09a57), Surface.Button => Hex(0x5b4130), _ => Hex(0x3d3028)
            };
            Color rim = paper ? Hex(0x725135) : Hex(0x171e20);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "Guild " + surface, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int ex = Mathf.Min(x, size - 1 - x), ey = Mathf.Min(y, size - 1 - y);
                    Color c = fill;
                    if (ex < 2 && ey < 2) c = Color.clear;
                    else if (ex < 2 || ey < 2) c = rim;
                    else if (ex < 4 || ey < 4) c = paper ? Hex(0xe6ce96) : Hex(0x7b7770);
                    else if (ex < 6 || ey < 6) c = paper ? Hex(0xa78c5f) : Hex(0x282c2b);
                    else if (!paper && ey == 8) c = Hex(0x7b5939);
                    // Broad corner iron straps with square-headed rivets, not the stage bevel.
                    if (!paper && ex < 12 && ey < 12 && ex >= 2 && ey >= 2)
                    {
                        c = Hex(ex < 4 || ey < 4 ? 0x96948au : 0x495050u);
                        if (ex >= 6 && ex < 8 && ey >= 6 && ey < 8) c = Hex(0xd0bb83);
                    }
                    if (paper && ex == 8 && ey > 10) c = Hex(0xb69a68);
                    texture.SetPixel(x, y, c);
                }
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f,
                100, 0, SpriteMeshType.FullRect, Vector4.one * 14);
            sprite.name = texture.name; sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[surface] = sprite;
            return sprite;
        }
    }
}
