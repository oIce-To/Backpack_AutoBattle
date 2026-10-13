using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class ItemRarityExtensions
    {
        public static string GetDisplayName(this ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "일반",
                ItemRarity.Uncommon => "고급",
                ItemRarity.Rare => "희귀",
                ItemRarity.Epic => "영웅",
                ItemRarity.Legendary => "전설",
                _ => rarity.ToString()
            };
        }

        public static Color GetDisplayColor(this ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.88f, 0.90f, 0.93f, 1f),
                ItemRarity.Uncommon => new Color(0.28f, 0.78f, 0.38f, 1f),
                ItemRarity.Rare => new Color(0.26f, 0.52f, 0.95f, 1f),
                ItemRarity.Epic => new Color(0.65f, 0.34f, 0.90f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.76f, 0.18f, 1f),
                _ => Color.white
            };
        }
    }
}
