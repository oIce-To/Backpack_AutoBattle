using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Small point-filtered, nine-sliced sprites keep bevels crisp at every panel size.
    internal static class PrototypePixelSkin
    {
        internal enum Surface { Panel, Leather, Wood, WoodSign, WoodSignSelected, ShopSlot, Slot, Cell, Primary, Secondary, Disabled }
        private static readonly Dictionary<(Surface, bool, bool), Sprite> Sprites = new();
        internal static readonly Color Ink = Hex(0xe5dfc5);
        internal static readonly Color Muted = Hex(0x9baaa4);

        internal static Color Hex(uint rgb) => new((rgb >> 16 & 255) / 255f, (rgb >> 8 & 255) / 255f, (rgb & 255) / 255f);

        internal static void Apply(Image image, Surface surface, bool preserveTint = false)
        {
            image.sprite = SpriteFor(surface);
            image.type = Image.Type.Sliced;
            if (!preserveTint) image.color = Color.white;
            if (surface is Surface.Leather or Surface.Wood) PrototypeFantasyTrim.Add(image, surface == Surface.Wood);
        }

        internal static void Apply(Button button, bool primary = false, bool wood = false)
        {
            Apply(button.image, primary ? Surface.Primary : Surface.Secondary);
            button.transition = Selectable.Transition.None;
            var feedback = button.gameObject.AddComponent<PrototypePixelButtonFeedback>();
            feedback.Initialize(button, primary, wood);
        }

        internal static Sprite SpriteFor(Surface surface, bool pressed = false, bool focused = false)
        {
            var key = (surface, pressed, focused);
            if (Sprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            const int size = 32;
            Color fill, light, dark;
            switch (surface)
            {
                case Surface.Leather: fill = Hex(0x755033); light = Hex(0xb38a54); dark = Hex(0x35251c); break;
                case Surface.Wood: fill = Hex(0x67492f); light = Hex(0xa47b4a); dark = Hex(0x2e211b); break;
                case Surface.WoodSign: fill = Hex(0x69482f); light = Hex(0xb58a53); dark = Hex(0x302119); break;
                case Surface.WoodSignSelected: fill = Hex(0xb1874e); light = Hex(0xefcf8f); dark = Hex(0x5c3d24); break;
                case Surface.ShopSlot: fill = Hex(0x302921); light = Hex(0x84633c); dark = Hex(0x1c1917); break;
                case Surface.Slot: fill = Hex(0x25333b); light = Hex(0x52635e); dark = Hex(0x101c23); break;
                case Surface.Cell: fill = Color.white; light = Hex(0x66716a); dark = Hex(0xc1c9aa); break;
                case Surface.Primary: fill = Hex(0xa3894b); light = Hex(0xead5a1); dark = Hex(0x5d4d2a); break;
                case Surface.Secondary: fill = Hex(0x3a4f50); light = Hex(0x81958a); dark = Hex(0x1b2c32); break;
                case Surface.Disabled: fill = Hex(0x3c474a); light = Hex(0x586460); dark = Hex(0x273236); break;
                default: fill = Hex(0x1c2b33); light = Hex(0x63766b); dark = Hex(0x0c181f); break;
            }
            if (pressed) (light, dark) = (dark, light);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Pixel UI " + surface, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int edgeX = Mathf.Min(x, size - 1 - x), edgeY = Mathf.Min(y, size - 1 - y);
                    bool cell = surface == Surface.Cell;
                    bool cut = !cell && (edgeX < 4 && edgeY < 8 || edgeX < 8 && edgeY < 4);
                    Color color = fill;
                    if (cut) color = Color.clear;
                    else if (edgeX < 2 || edgeY < 2 || !cell && (edgeX < 6 && edgeY < 10 || edgeX < 10 && edgeY < 6))
                        color = cell ? Hex(0x566060) : focused ? Hex(0xead5a1) : Hex(0x0b1218);
                    else if (x < 4 || y >= size - 4) color = light;
                    else if (x >= size - 4 || y < 4) color = dark;
                    else if (!cell && (x < 6 || y >= size - 6)) color = Color.Lerp(fill, light, .35f);
                    if (surface is Surface.WoodSign or Surface.WoodSignSelected)
                    {
                        if (y == 10 && x >= 8 && x < 20 || y == 22 && x >= 13 && x < 24)
                            color = Color.Lerp(fill, dark, .4f);
                        if ((x == 6 || x == 25) && y >= 15 && y <= 16) color = Hex(0xd2bd8c);
                    }
                    // Corner rivets are confined to the unscaled nine-slice corners.
                    if ((surface == Surface.Leather || surface == Surface.Panel) && edgeX >= 8 && edgeX < 10 && edgeY >= 8 && edgeY < 10)
                        color = Hex(0xd2b47a);
                    texture.SetPixel(x, y, color);
                }
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0,
                SpriteMeshType.FullRect, Vector4.one * 12);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[key] = sprite;
            return sprite;
        }
    }
}
