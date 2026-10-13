using System;

namespace BackpackAutoBattle.Shop
{
    public static class ShopPriceRules
    {
        // Equivalent to (price + 1) / 2, without overflowing for large prices.
        public static int HalfRoundedUp(int price)
        {
            if (price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price));
            }

            return price / 2 + price % 2;
        }
    }
}
