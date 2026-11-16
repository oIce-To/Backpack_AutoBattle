using System.Linq;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEditor;

namespace BackpackAutoBattle.Tests.Items
{
    public sealed class ItemDataAssetTests
    {
        [Test]
        public void PrototypeItemAssets_HaveUniqueIdsAndValidShapes()
        {
            ItemData[] items = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Data/Items/Stage" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemData>)
                .ToArray();

            Assert.That(items, Has.Length.EqualTo(65));
            Assert.That(items.Count(item => !item.ExcludeFromShop), Is.EqualTo(35));
            Assert.That(items.Count(item => item.ExcludeFromShop), Is.EqualTo(30));
            Assert.That(items.Select(item => item.ItemId).Distinct().Count(), Is.EqualTo(items.Length));
            Assert.That(items.All(item => !string.IsNullOrWhiteSpace(item.DisplayName)), Is.True);
            Assert.That(items.All(item => item.PurchasePrice >= 0), Is.True);
            Assert.That(items.All(item => item.CreateShape().BaseCells.Count > 0), Is.True);
            foreach (ItemData item in items)
            {
                string artId = item.ItemId.Replace('_', '-');
                Assert.That(item.InventorySprite, Is.Not.Null, item.ItemId);
                Assert.That(AssetDatabase.GetAssetPath(item.InventorySprite),
                    Is.EqualTo($"Assets/_Project/Art/Items/Stage/{artId}/{artId}-approved.png"));
            }
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Common), Is.EqualTo(6));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Uncommon), Is.EqualTo(14));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Rare), Is.EqualTo(17));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Epic), Is.EqualTo(17));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Legendary && item.ExcludeFromShop), Is.EqualTo(11));
            ItemData[] weapons = items.Where(item => item.Category == ItemCategory.Weapon).ToArray();
            Assert.That(weapons, Has.Length.EqualTo(22));
            Assert.That(weapons.All(item => item.BaseWeaponStats != null && item.BaseWeaponStats.AttackDamage > 0f), Is.True);
            Assert.That(weapons.All(item => item.Activation.HasEffect(ItemEffectKind.Attack) && item.Activation.CooldownSeconds >= 0.1f), Is.True);
            Assert.That(weapons.All(item => item.BaseWeaponStats.AccuracyPercent >= 30f && item.BaseWeaponStats.AccuracyPercent <= 100f), Is.True);
            Assert.That(weapons.All(item => item.BaseWeaponStats.CriticalChancePercent >= 0f), Is.True);
            Assert.That(weapons.All(item => item.BaseWeaponStats.CriticalDamagePercent >= 100f), Is.True);
        }
    }
}
