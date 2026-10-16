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
            ItemData[] items = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Data/Items" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemData>)
                .ToArray();

            Assert.That(items, Has.Length.EqualTo(9));
            Assert.That(items.Select(item => item.ItemId).Distinct().Count(), Is.EqualTo(items.Length));
            Assert.That(items.All(item => !string.IsNullOrWhiteSpace(item.DisplayName)), Is.True);
            Assert.That(items.All(item => item.PurchasePrice >= 0), Is.True);
            Assert.That(items.All(item => item.CreateShape().BaseCells.Count > 0), Is.True);
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Common), Is.EqualTo(3));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Uncommon), Is.EqualTo(2));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Rare), Is.EqualTo(3));
            Assert.That(items.Count(item => item.Rarity == ItemRarity.Epic), Is.EqualTo(1));
            Assert.That(items.All(item => item.Rarity != ItemRarity.Legendary), Is.True);
            ItemData[] weapons = items.Where(item => item.Category == ItemCategory.Weapon).ToArray();
            Assert.That(weapons, Has.Length.EqualTo(4));
            Assert.That(weapons.All(item => item.BaseWeaponStats != null && item.BaseWeaponStats.AttackDamage > 0f), Is.True);
            Assert.That(weapons.All(item => item.Activation.HasEffect(ItemEffectKind.Attack) && item.Activation.CooldownSeconds >= 0.1f), Is.True);
            Assert.That(weapons.All(item => item.BaseWeaponStats.AccuracyPercent >= 0f && item.BaseWeaponStats.AccuracyPercent <= 100f), Is.True);
            Assert.That(weapons.All(item => item.BaseWeaponStats.CriticalChancePercent >= 0f && item.BaseWeaponStats.CriticalChancePercent <= 100f), Is.True);
            Assert.That(weapons.All(item => item.BaseWeaponStats.CriticalDamagePercent >= 100f), Is.True);
        }
    }
}
