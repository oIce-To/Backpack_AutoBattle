using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Shop
{
    public sealed class ShopOfferLockTests
    {
        private ItemData _item;
        [SetUp] public void Setup() => _item = ScriptableObject.CreateInstance<ItemData>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(_item);

        [TestCase(false)]
        [TestCase(true)]
        public void LockToggle_PreservesItemSaleAndCapturedPrices(bool sale)
        {
            var offer = new ShopOffer(_item, sale);
            int price = offer.PurchasePrice;
            int original = offer.OriginalPrice;
            Assert.That(offer.IsLocked, Is.False);
            Assert.That(offer.TryToggleLock(), Is.True);
            Assert.That(offer.IsLocked, Is.True);
            Assert.That(offer.TryToggleLock(), Is.True);
            Assert.That(offer.IsLocked, Is.False);
            Assert.That(offer.ItemData, Is.SameAs(_item));
            Assert.That(offer.IsOnSale, Is.EqualTo(sale));
            Assert.That(offer.PurchasePrice, Is.EqualTo(price));
            Assert.That(offer.OriginalPrice, Is.EqualTo(original));
        }

        [Test]
        public void TwoOffersOfTheSameItem_HaveIndependentLocks()
        {
            var first = new ShopOffer(_item, true);
            var second = new ShopOffer(_item, false);
            first.TryToggleLock();
            Assert.That(first.IsLocked, Is.True);
            Assert.That(second.IsLocked, Is.False);
            Assert.That(new ShopOffer(_item, true).IsLocked, Is.False);
        }

        [Test]
        public void LockedOffer_CanBePurchasedOnceAndCannotBeRelockedAfterPurchase()
        {
            var offer = new ShopOffer(_item, true);
            offer.TryToggleLock();
            Assert.That(offer.TryMarkPurchased(), Is.True);
            Assert.That(offer.IsPurchased, Is.True);
            Assert.That(offer.IsLocked, Is.False);
            Assert.That(offer.TryToggleLock(), Is.False);
            Assert.That(offer.TryMarkPurchased(), Is.False);
            Assert.That(offer.IsLocked, Is.False);
        }
    }
}
