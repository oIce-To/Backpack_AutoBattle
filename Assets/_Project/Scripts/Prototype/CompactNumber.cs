using System;
using System.Globalization;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Display-only formatting; saved balances and affordability checks keep their exact values.</summary>
    public static class CompactNumber
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B" };

        public static string Format(long amount)
        {
            decimal value = Math.Abs((decimal)amount);
            int unit = 0;
            while (value >= 1000 && unit < Suffixes.Length - 1) { value /= 1000; unit++; }
            value = Math.Round(value, 1, MidpointRounding.AwayFromZero);
            if (value >= 1000 && unit < Suffixes.Length - 1) { value /= 1000; unit++; }
            return (amount < 0 ? "-" : "") + value.ToString("0.#", CultureInfo.InvariantCulture) + Suffixes[unit];
        }

        public static string Exact(long amount) => amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
