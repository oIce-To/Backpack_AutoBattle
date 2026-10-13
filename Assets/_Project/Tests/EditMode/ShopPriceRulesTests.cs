using BackpackAutoBattle.Shop;
using NUnit.Framework;

namespace BackpackAutoBattle.Tests.Shop
{
    public sealed class ShopPriceRulesTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(5, 3)]
        [TestCase(8, 4)]
        [TestCase(int.MaxValue, 1073741824)]
        public void HalfRoundedUp_PreservesRequestedRounding(int originalPrice, int expectedPrice)
        {
            Assert.That(ShopPriceRules.HalfRoundedUp(originalPrice), Is.EqualTo(expectedPrice));
        }
    }
}
