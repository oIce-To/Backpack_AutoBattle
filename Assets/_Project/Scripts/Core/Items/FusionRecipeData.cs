using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    [Serializable]
    public sealed class FusionIngredient
    {
        [SerializeField] private ItemData _item;
        [SerializeField, Min(1)] private int _quantity = 1;
        public ItemData Item => _item;
        public int Quantity => _quantity;
        public FusionIngredient() { }
        public FusionIngredient(ItemData item, int quantity = 1) { _item = item; _quantity = quantity; }
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Fusion Recipe", fileName = "Recipe_")]
    public sealed class FusionRecipeData : ScriptableObject
    {
        [Tooltip("One central item. Every additional ingredient must touch this item's occupied cells orthogonally.")]
        [SerializeField] private ItemData _centerItem;
        [SerializeField] private List<FusionIngredient> _ingredients = new();
        [SerializeField] private ItemData _resultItem;
        public ItemData CenterItem => _centerItem;
        public IReadOnlyList<FusionIngredient> Ingredients => _ingredients;
        public ItemData ResultItem => _resultItem;
        public string ValidationError
        {
            get
            {
                if (_centerItem == null || _resultItem == null) return "중심 아이템과 결과 아이템을 지정하세요.";
                if (_ingredients == null || _ingredients.Count == 0) return "추가 재료를 하나 이상 지정하세요.";
                foreach (var ingredient in _ingredients)
                    if (ingredient == null || ingredient.Item == null || ingredient.Quantity < 1)
                        return "모든 재료의 아이템과 1 이상의 수량을 지정하세요.";
                return null;
            }
        }
        public bool Contains(ItemData item)
        {
            if (item == null || ValidationError != null) return false;
            if (_centerItem == item) return true;
            foreach (var ingredient in _ingredients) if (ingredient.Item == item) return true;
            return false;
        }
    }
}
