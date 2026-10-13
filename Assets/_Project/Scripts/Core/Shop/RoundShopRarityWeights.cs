using System;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Shop
{
    [Serializable]
    public sealed class RoundShopRarityWeights
    {
        [SerializeField, Range(0f, 100f)] private float _common = 70f;
        [SerializeField, Range(0f, 100f)] private float _uncommon = 20f;
        [SerializeField, Range(0f, 100f)] private float _rare = 10f;
        [SerializeField, Range(0f, 100f)] private float _epic;

        public float Common => _common;
        public float Uncommon => _uncommon;
        public float Rare => _rare;
        public float Epic => _epic;
        public float Total => _common + _uncommon + _rare + _epic;

        public float GetWeight(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => _common,
                ItemRarity.Uncommon => _uncommon,
                ItemRarity.Rare => _rare,
                ItemRarity.Epic => _epic,
                ItemRarity.Legendary => 0f,
                _ => 0f
            };
        }
    }
}
