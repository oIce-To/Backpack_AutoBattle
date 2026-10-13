using System.Collections.Generic;
using UnityEngine;

namespace BackpackAutoBattle.Shop
{
    [CreateAssetMenu(menuName = "Backpack AutoBattle/Shop Rarity Table", fileName = "ShopRarityTable_")]
    public sealed class ShopRarityTableData : ScriptableObject
    {
        public const int MaximumRound = 12;

        [SerializeField] private List<RoundShopRarityWeights> _rounds = new();

        public IReadOnlyList<RoundShopRarityWeights> Rounds => _rounds;

        public RoundShopRarityWeights GetWeights(int round)
        {
            if (_rounds == null || _rounds.Count == 0)
            {
                return new RoundShopRarityWeights();
            }

            int index = Mathf.Clamp(round, 1, _rounds.Count) - 1;
            return _rounds[index];
        }

        private void OnValidate()
        {
            _rounds ??= new List<RoundShopRarityWeights>();
            while (_rounds.Count < MaximumRound)
            {
                _rounds.Add(new RoundShopRarityWeights());
            }

            if (_rounds.Count > MaximumRound)
            {
                _rounds.RemoveRange(MaximumRound, _rounds.Count - MaximumRound);
            }

            for (int index = 0; index < _rounds.Count; index++)
            {
                _rounds[index] ??= new RoundShopRarityWeights();
            }
        }
    }
}
