using System;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Shop
{
    /// <summary>A single shelf entry. Two entries can offer the same ItemData independently.</summary>
    public sealed class ShopOffer
    {
        public ShopOffer(ItemData itemData, bool isOnSale)
        {
            ItemData = itemData != null ? itemData : throw new ArgumentNullException(nameof(itemData));
            OriginalPrice = itemData.PurchasePrice;
            IsOnSale = isOnSale;
            PurchasePrice = isOnSale ? ShopPriceRules.HalfRoundedUp(OriginalPrice) : OriginalPrice;
        }

        public ItemData ItemData { get; }
        public int OriginalPrice { get; }
        public int PurchasePrice { get; }
        public bool IsOnSale { get; }
        public bool IsPurchased { get; private set; }

        public bool TryMarkPurchased()
        {
            if (IsPurchased)
            {
                return false;
            }

            IsPurchased = true;
            return true;
        }
    }
}
