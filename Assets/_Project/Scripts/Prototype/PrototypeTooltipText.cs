using System;
using TMPro;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Shared inline-sprite text setup for item and status tooltip descriptions.</summary>
    public static class PrototypeTooltipText
    {
        public const string FontResource = "Tooltip/Tooltip Korean SDF";
        public const string SpriteResource = "Tooltip/Tooltip Icons";

        public static TextMeshProUGUI Create(Transform parent)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontResource);
            TMP_SpriteAsset sprites = Resources.Load<TMP_SpriteAsset>(SpriteResource);
            if (font == null || sprites == null)
                throw new InvalidOperationException("Tooltip text assets are missing. Run Tools/Backpack AutoBattle/Build Tooltip Text Assets.");

            var obj = new GameObject("Tooltip Description", typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.spriteAsset = sprites;
            text.fontSize = 16f;
            text.color = new Color(0.91f, 0.94f, 0.98f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.richText = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
