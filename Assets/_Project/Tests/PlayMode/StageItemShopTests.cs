using System.Collections;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class StageItemShopTests
    {
        private BackpackPrototypeController _controller;

        [UnityTest]
        public IEnumerator ShopReturnPaysRoundIncomeAfterLossAndWinAndConsumesFreeRerollsFirst()
        {
            yield return LoadStage();
            _controller.AutoTickFusionPresentation = false;
            Assert.That(_controller.Gold, Is.EqualTo(20));
            for (int i = 0; i < 7; i++) Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(_controller.CurrentRound, Is.EqualTo(8));
            Assert.That(_controller.Gold, Is.EqualTo(63));
            Assert.That(Place("piggy_bank", 5, 5), Is.True);
            Assert.That(Place("platinum_card", 4, 5), Is.True);

            Assert.That(_controller.TryStartBattle(), Is.True);
            foreach (CombatWeapon weapon in _controller.ActiveCombat.PlayerWeapons.Concat(_controller.ActiveCombat.EnemyWeapons))
                weapon.Stats.AttackDamage = 0;
            _controller.TickCombat(90);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
            Assert.That(ReturnButtonText(), Does.Contain("+7G"));
            Assert.That(_controller.ReturnToPreparation(), Is.True);
            Assert.That(_controller.CurrentRound, Is.EqualTo(8));
            Assert.That(_controller.Gold, Is.EqualTo(71));
            Assert.That(Run.FreeShopRerolls, Is.EqualTo(1));
            Assert.That(_controller.TryRerollShop(), Is.False);
            TickPiggyBanks(1f);

            Win();
            Assert.That(Run.FreeShopRerolls, Is.Zero);
            Assert.That(ReturnButtonText(), Does.Contain("+10G"));
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(_controller.CurrentRound, Is.EqualTo(9));
            Assert.That(_controller.Gold, Is.EqualTo(82));
            Assert.That(Run.FreeShopRerolls, Is.EqualTo(1));
            var bank = _controller.GetComponentsInChildren<BackpackItemView>().Single(view => view.InstanceId == "piggy_bank");
            var position = bank.RectTransform.anchoredPosition;
            Assert.That(_controller.GetComponentsInChildren<Text>().Single(label => label.name == "Piggy Bank Gold").text, Is.EqualTo("+1G"));
            Assert.That(_controller.TryOpenSettings(), Is.False);
            Assert.That(_controller.TryStartBattle(), Is.False);
            TickPiggyBanks(.18f);
            Assert.That(bank.RectTransform.localScale.x, Is.GreaterThan(1f));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "PiggyBankReward.png");
            TickPiggyBanks(1f);
            Assert.That(bank.RectTransform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(bank.RectTransform.anchoredPosition, Is.EqualTo(position));
            Assert.That(_controller.TryRerollShop(), Is.True);
            Assert.That(_controller.Gold, Is.EqualTo(82));
            Assert.That(_controller.TryRerollShop(), Is.True);
            Assert.That(_controller.Gold, Is.EqualTo(81));
        }

        [UnityTest]
        public IEnumerator LiveCatalogForgesWoodenSwordAndWhetstoneIntoUpdatedSteelSword()
        {
            yield return LoadStage();
            _controller.AutoTickFusionPresentation = false;
            Assert.That(_controller.ItemCatalog.Count, Is.EqualTo(35));
            Assert.That(_controller.ItemCatalog.Any(item => item.ItemId is "training_blade" or "steel_sword" or "swift_charm"), Is.False);
            var sword = _controller.ItemCatalog.Single(item => item.ItemId == "wooden_sword");
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            slot.Bind(new ShopOffer(sword, false));
            Assert.That(slot.ShapeRect.Find("Item Artwork").GetComponent<Image>().sprite, Is.SameAs(sword.InventorySprite));
            Assert.That(Place("wooden_sword", 2, 0), Is.True);
            Assert.That(Place("whetstone", 3, 1), Is.True);
            Assert.That(Place("piggy_bank", 5, 5), Is.True);
            var whetstone = _controller.GetComponentsInChildren<BackpackItemView>().Single(view => view.InstanceId == "whetstone");
            Assert.That(whetstone.transform.Find("Item Artwork").GetComponent<Image>().sprite, Is.SameAs(whetstone.Data.InventorySprite));
            Assert.That(whetstone.transform.Find("Label"), Is.Null);
            _controller.RotateItem(whetstone);
            Assert.That(whetstone.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Assert.That(_controller.GetReadyFusions().Single().Result.ItemId, Is.EqualTo("steel_sword"));
            Win();
            var battleSword = _controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(view => view.Snapshot.ItemId == sword.ItemId);
            Assert.That(battleSword.transform.Find("Item Artwork").GetComponent<Image>().sprite, Is.SameAs(sword.InventorySprite));
            Assert.That(battleSword.transform.Find("Artwork Cycle Color").GetComponent<Image>().sprite, Is.SameAs(sword.InventorySprite));
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(_controller.CanEditLoadout, Is.False);
            Assert.That(_controller.IsFusionPresenting, Is.False);
            TickPiggyBanks(1f);
            Assert.That(_controller.IsFusionPresenting, Is.True);
            Assert.That(_controller.CanEditLoadout, Is.False);
            _controller.TickFusionPresentation(5);
            yield return null;
            var stored = _controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
            var result = stored.StoredItem.ItemData;
            Assert.That(stored.transform.Find("Item Artwork").GetComponent<Image>().sprite, Is.SameAs(result.InventorySprite));
            Assert.That(result.BaseWeaponStats.AccuracyPercent, Is.EqualTo(90));
            Assert.That(result.BaseWeaponStats.CriticalChancePercent, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator CloverPromotesOnlyNewOffersAndLeavesLockedOffersIntact()
        {
            yield return LoadStage();
            var slots = _controller.GetComponentsInChildren<PrototypeShopSlotView>();
            var locked = slots[0].Offer;
            Assert.That(_controller.TryToggleShopLock(slots[0]), Is.True);
            var clover = _controller.ItemCatalog.Single(item => item.ItemId == "four_leaf_clover");
            var platinum = _controller.FusionRecipes.Single(recipe => recipe.ResultItem.ItemId == "platinum_clover").ResultItem;
            var cursed = _controller.FusionRecipes.Single(recipe => recipe.ResultItem.ItemId == "cursed_clover").ResultItem;
            Assert.That(Place(clover, 5, 5), Is.True);
            RerollAndCheck(clover, 0, ItemRarity.Uncommon);
            Assert.That(Place(platinum, 4, 5), Is.True);
            RerollAndCheck(platinum, 0, ItemRarity.Uncommon);
            Assert.That(Place(cursed, 3, 5), Is.True);
            RerollAndCheck(cursed, .75, ItemRarity.Common);

            void RerollAndCheck(ItemData source, double rarityRoll, ItemRarity expectedRarity)
            {
                typeof(BackpackPrototypeController).GetField("_shopRandom", PrivateInstance)
                    .SetValue(_controller, new ShopRandom(rarityRoll));
                Assert.That(_controller.TryRerollShop(), Is.True);
                Assert.That(slots[0].Offer, Is.SameAs(locked));
                Assert.That(slots[0].transform.Find("Rarity Clover Flash").gameObject.activeSelf, Is.False);
                foreach (var slot in slots.Skip(1))
                {
                    Assert.That(slot.Offer.RarityShiftSource, Is.SameAs(source));
                    Assert.That(slot.ItemData.Rarity, Is.EqualTo(expectedRarity));
                    var flash = slot.transform.Find("Rarity Clover Flash").GetComponent<Image>();
                    Assert.That(flash.gameObject.activeSelf, Is.True);
                    Assert.That(flash.sprite, Is.SameAs(source.InventorySprite));
                    Assert.That(flash.raycastTarget, Is.False);
                }
                PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), $"RarityShift-{source.ItemId}.png");
            }
        }

        private IEnumerator LoadStage()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            _controller.BattleController.AutoTick = false;
            yield return null;
        }

        private bool Place(string id, int x, int y)
            => PrototypeCombatFixture.PlaceItem(_controller, id, id, new Vector2Int(x, y));

        private bool Place(ItemData item, int x, int y) => (bool)typeof(BackpackPrototypeController)
            .GetMethod("TryPlaceItem", PrivateInstance).Invoke(_controller, new object[] { item, item.ItemId, new Vector2Int(x, y) });

        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private PrototypeRunState Run => (PrototypeRunState)typeof(BackpackPrototypeController)
            .GetField("_run", PrivateInstance).GetValue(_controller);
        private string ReturnButtonText() => _controller.GetComponentsInChildren<Button>()
            .Single(button => button.name == "Combat Action Button").GetComponentInChildren<Text>().text;
        private void TickPiggyBanks(float delta) => typeof(BackpackPrototypeController)
            .GetMethod("TickPiggyBankPresentation", PrivateInstance).Invoke(_controller, new object[] { delta });

        private void Win()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            foreach (CombatWeapon weapon in _controller.ActiveCombat.PlayerWeapons)
            {
                weapon.Stats.AttackDamage = 10_000;
                weapon.Stats.AccuracyPercent = 100;
            }
            foreach (CombatWeapon weapon in _controller.ActiveCombat.EnemyWeapons) weapon.Stats.AttackDamage = 0;
            _controller.TickCombat(10);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
        }

        private sealed class ShopRandom : System.Random
        {
            private readonly double _rarityRoll;
            private int _roll;
            public ShopRandom(double rarityRoll) => _rarityRoll = rarityRoll;
            public override double NextDouble() => _roll++ % 3 == 0 ? _rarityRoll : 0d;
        }
    }
}
