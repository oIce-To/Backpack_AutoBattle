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

            Assert.That(items, Has.Length.EqualTo(8));
            Assert.That(items.Select(item => item.ItemId).Distinct().Count(), Is.EqualTo(items.Length));
            Assert.That(items.All(item => !string.IsNullOrWhiteSpace(item.DisplayName)), Is.True);
            Assert.That(items.All(item => item.PurchasePrice >= 0), Is.True);
            Assert.That(items.All(item => item.CreateShape().BaseCells.Count > 0), Is.True);
        }
    }
}
