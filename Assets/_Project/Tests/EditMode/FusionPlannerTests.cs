using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Inventory
{
    public sealed class FusionPlannerTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var item in _temporary) Object.DestroyImmediate(item); _temporary.Clear(); }

        [Test]
        public void AuthoredRecipeAndSteelSword_HaveExpectedEconomyAndShape()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<FusionRecipeData>("Assets/_Project/Data/Recipes/Recipe_SteelSword.asset");
            Assert.That(recipe.ValidationError, Is.Null);
            Assert.That(recipe.CenterItem.ItemId, Is.EqualTo("training_blade"));
            Assert.That(recipe.Ingredients.Single().Item.ItemId, Is.EqualTo("whetstone"));
            Assert.That(recipe.ResultItem.DisplayName, Is.EqualTo("강철 검"));
            Assert.That(recipe.ResultItem.Rarity, Is.EqualTo(ItemRarity.Uncommon));
            Assert.That(recipe.ResultItem.ExcludeFromShop, Is.True);
            Assert.That(recipe.ResultItem.PurchasePrice, Is.EqualTo(6));
            Assert.That(ShopPriceRules.HalfRoundedUp(recipe.ResultItem.PurchasePrice), Is.EqualTo(3));
            Assert.That(recipe.ResultItem.OccupiedCells, Is.EqualTo(recipe.CenterItem.OccupiedCells));
            Assert.That(recipe.ResultItem.BaseWeaponStats.AttackDamage, Is.GreaterThan(recipe.CenterItem.BaseWeaponStats.AttackDamage));
        }

        [TestCase(1, 1, true)]
        [TestCase(0, 3, true)]
        [TestCase(1, 3, false)]
        [TestCase(2, 1, false)]
        public void Adjacency_UsesActualOccupiedCellsAndExcludesDiagonals(int x, int y, bool expected)
        {
            var center = Item(Vector2Int.zero, Vector2Int.up, Vector2Int.up * 2);
            var stone = Item(Vector2Int.zero); var output = Item(Vector2Int.zero);
            var recipe = Recipe(center, output, new FusionIngredient(stone));
            var plan = Plan(recipe, At("a", center, 0, 0), At("b", stone, x, y));
            Assert.That(plan.Count, Is.EqualTo(expected ? 1 : 0));
        }

        [Test]
        public void ThreeMaterials_MustTouchCenter_NotJustAnotherMaterial()
        {
            var center = Item(Vector2Int.zero); var stone = Item(Vector2Int.zero); var extra = Item(Vector2Int.zero);
            var recipe = Recipe(center, center, new FusionIngredient(stone), new FusionIngredient(extra));
            Assert.That(Plan(recipe, At("c", center, 2, 2), At("s", stone, 3, 2), At("e", extra, 4, 2)), Is.Empty);
            var plans = Plan(recipe, At("c", center, 2, 2), At("s", stone, 3, 2), At("e", extra, 2, 1));
            Assert.That(plans.Single().Materials.Count, Is.EqualTo(3));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void LockingAnyMaterial_PreventsItsConsumption(bool centerLocked, bool stoneLocked)
        {
            var center = Item(Vector2Int.zero); var stone = Item(Vector2Int.zero);
            var recipe = Recipe(center, center, new FusionIngredient(stone));
            Assert.That(Plan(recipe, At("c", center, 1, 1, centerLocked), At("s", stone, 2, 1, stoneLocked)), Is.Empty);
        }

        [Test]
        public void DuplicateIngredients_RequireDistinctInstancesIncludingCenter()
        {
            var item = Item(Vector2Int.zero);
            var recipe = Recipe(item, item, new FusionIngredient(item, 2));
            Assert.That(Plan(recipe, At("a", item, 1, 1), At("b", item, 2, 1)), Is.Empty);
            Assert.That(Plan(recipe, At("a", item, 1, 1), At("b", item, 2, 1), At("c", item, 1, 2)).Single().Materials.Count, Is.EqualTo(3));
        }

        [Test]
        public void SharedMaterial_IsUsedOnceWithStableSpatialPriority()
        {
            var center = Item(Vector2Int.zero); var stone = Item(Vector2Int.zero);
            var recipe = Recipe(center, center, new FusionIngredient(stone));
            var first = At("left", center, 0, 0); var second = At("right", center, 2, 0); var shared = At("stone", stone, 1, 0);
            Assert.That(Plan(recipe, second, shared, first).Single().Center.Id, Is.EqualTo("left"));
            Assert.That(Plan(recipe, first, shared, second).Single().Center.Id, Is.EqualTo("left"));
        }

        [Test]
        public void IndependentRecipes_CanBothResolveButNewOutputsNeverChain()
        {
            var a = Item(Vector2Int.zero); var b = Item(Vector2Int.zero); var c = Item(Vector2Int.zero);
            var first = Recipe(a, c, new FusionIngredient(b)); var chain = Recipe(c, a, new FusionIngredient(b));
            var items = new[] { At("a", a, 0, 0), At("b", b, 1, 0), At("b2", b, 0, 1), At("a2", a, 4, 0), At("b3", b, 5, 0) };
            var plans = FusionPlanner.Plan(6, 6, items, new[] { first, chain });
            Assert.That(plans.Count, Is.EqualTo(2));
            Assert.That(plans.All(plan => plan.Recipe == first), Is.True);
            Assert.That(plans.SelectMany(plan => plan.Materials).Select(item => item.Id).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void RecipePriority_AndRotation_ArePreserved()
        {
            var a = Item(Vector2Int.zero, Vector2Int.up, Vector2Int.up * 2); var b = Item(Vector2Int.zero);
            var first = Recipe(a, a, new FusionIngredient(b)); var second = Recipe(a, b, new FusionIngredient(b));
            var items = new[] { new FusionItem("a", a, Vector2Int.zero, ItemRotation.Clockwise90), At("b", b, 2, 1) };
            var plan = FusionPlanner.Plan(6, 6, items, new[] { second, first }).Single();
            Assert.That(plan.Recipe, Is.SameAs(second));
            Assert.That(plan.Center.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Assert.That(a.OccupiedCells.Count, Is.EqualTo(3));
        }

        [Test]
        public void OutputMustFit_SelectsTheMaterialInItsFootprint()
        {
            var a = Item(Vector2Int.zero); var b = Item(Vector2Int.zero); var wide = Item(Vector2Int.zero, Vector2Int.right);
            var recipe = Recipe(a, wide, new FusionIngredient(b));
            var plans = Plan(recipe, At("a", a, 1, 1), At("left", b, 0, 1), At("right", b, 2, 1));
            Assert.That(plans.Single().Materials[1].Id, Is.EqualTo("right"));
            Assert.That(Plan(recipe, At("a", a, 5, 1), At("left", b, 4, 1)), Is.Empty, "Out of bounds result must not consume anything.");
            Assert.That(Plan(recipe, At("a", a, 1, 1), At("left", b, 0, 1), At("obstacle", wide, 2, 1)), Is.Empty);
        }

        [Test]
        public void InvalidRecipes_DoNotReserveOrConsumeMaterials()
        {
            var a = Item(Vector2Int.zero); var b = Item(Vector2Int.zero);
            var invalid = Recipe(a, b, new FusionIngredient(b, 0)); var valid = Recipe(a, b, new FusionIngredient(b));
            Assert.That(invalid.ValidationError, Is.Not.Null);
            var plans = FusionPlanner.Plan(6, 6, new[] { At("a", a, 0, 0), At("b", b, 1, 0) }, new[] { null, invalid, valid });
            Assert.That(plans.Single().Recipe, Is.SameAs(valid));
        }

        private ItemData Item(params Vector2Int[] cells)
        { var data = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(data); Set(data, "_occupiedCells", cells.ToList()); return data; }
        private FusionRecipeData Recipe(ItemData center, ItemData result, params FusionIngredient[] ingredients)
        {
            var recipe = ScriptableObject.CreateInstance<FusionRecipeData>(); _temporary.Add(recipe);
            Set(recipe, "_centerItem", center); Set(recipe, "_resultItem", result); Set(recipe, "_ingredients", ingredients.ToList()); return recipe;
        }
        private static FusionItem At(string id, ItemData data, int x, int y, bool locked = false) => new(id, data, new Vector2Int(x, y), ItemRotation.None, locked);
        private static IReadOnlyList<FusionPlan> Plan(FusionRecipeData recipe, params FusionItem[] items) => FusionPlanner.Plan(6, 6, items, new[] { recipe });
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
