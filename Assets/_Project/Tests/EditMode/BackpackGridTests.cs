using System.Collections.Generic;
using BackpackAutoBattle.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Inventory
{
    public sealed class BackpackGridTests
    {
        private static readonly ItemShape SingleCellShape = new(new[]
        {
            Vector2Int.zero
        });

        [Test]
        public void ItemShape_RotatesClockwiseAndNormalizesItsOrigin()
        {
            var shape = new ItemShape(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            });

            IReadOnlyList<Vector2Int> rotatedCells = shape.GetCells(ItemRotation.Clockwise90);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1)
                },
                rotatedCells);
        }

        [Test]
        public void TryPlace_RejectsCellsOutsideTheBackpack()
        {
            var grid = new BackpackGrid(2, 2);
            var horizontalShape = new ItemShape(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0)
            });

            bool placed = grid.TryPlace("sword", horizontalShape, new Vector2Int(1, 0));

            Assert.That(placed, Is.False);
            Assert.That(grid.ItemCount, Is.Zero);
        }

        [Test]
        public void TryPlace_RejectsOverlapWithAnotherItem()
        {
            var grid = new BackpackGrid(3, 3);
            Assert.That(grid.TryPlace("apple", SingleCellShape, new Vector2Int(1, 1)), Is.True);

            bool placed = grid.TryPlace("stone", SingleCellShape, new Vector2Int(1, 1));

            Assert.That(placed, Is.False);
            Assert.That(grid.ItemCount, Is.EqualTo(1));
            Assert.That(grid.TryGetItemAt(new Vector2Int(1, 1), out string itemId), Is.True);
            Assert.That(itemId, Is.EqualTo("apple"));
        }

        [Test]
        public void TryPlace_FailedMoveKeepsTheOriginalPlacement()
        {
            var grid = new BackpackGrid(2, 2);
            Assert.That(grid.TryPlace("apple", SingleCellShape, Vector2Int.zero), Is.True);
            Assert.That(grid.TryPlace("stone", SingleCellShape, Vector2Int.right), Is.True);

            bool moved = grid.TryPlace("apple", SingleCellShape, Vector2Int.right);

            Assert.That(moved, Is.False);
            Assert.That(grid.TryGetItemAt(Vector2Int.zero, out string itemId), Is.True);
            Assert.That(itemId, Is.EqualTo("apple"));
        }

        [Test]
        public void GetAdjacentItemIds_ReturnsOrthogonalItemsOnlyOnce()
        {
            var grid = new BackpackGrid(4, 4);
            var verticalShape = new ItemShape(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1)
            });

            Assert.That(grid.TryPlace("shield", verticalShape, new Vector2Int(1, 1)), Is.True);
            Assert.That(grid.TryPlace("sword", verticalShape, new Vector2Int(2, 1)), Is.True);
            Assert.That(grid.TryPlace("gem", SingleCellShape, new Vector2Int(2, 3)), Is.True);

            IReadOnlyList<string> adjacentItems = grid.GetAdjacentItemIds("shield");

            CollectionAssert.AreEqual(new[] { "sword" }, adjacentItems);
        }
    }
}
