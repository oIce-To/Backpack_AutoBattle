using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Inventory
{
    public sealed class FusionItem
    {
        public string Id { get; }
        public ItemData Data { get; }
        public Vector2Int Origin { get; }
        public ItemRotation Rotation { get; }
        public bool Locked { get; }
        public FusionItem(string id, ItemData data, Vector2Int origin, ItemRotation rotation, bool locked = false)
        { Id = id; Data = data; Origin = origin; Rotation = rotation; Locked = locked; }
    }

    public sealed class FusionPlan
    {
        public FusionRecipeData Recipe { get; }
        public FusionItem Center => Materials[0];
        public IReadOnlyList<FusionItem> Materials { get; }
        public ItemData Result { get; }
        public FusionPlan(FusionRecipeData recipe, IEnumerable<FusionItem> materials)
        { Recipe = recipe; Result = recipe.ResultItem; Materials = Array.AsReadOnly(materials.ToArray()); }
    }

    // Pure planning: never mutates the source grid, instances or SOs. One ingredient can be consumed only once.
    public static class FusionPlanner
    {
        public static IReadOnlyList<FusionPlan> Plan(int width, int height, IEnumerable<FusionItem> items,
            IEnumerable<FusionRecipeData> recipes)
        {
            var ordered = items.OrderBy(item => item.Origin.y).ThenBy(item => item.Origin.x)
                .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
            var grid = new BackpackGrid(width, height);
            foreach (var item in ordered)
                if (!grid.TryPlace(item.Id, item.Data.CreateShape(), item.Origin, item.Rotation))
                    throw new InvalidOperationException("Cannot plan fusion for an invalid backpack.");
            var used = new HashSet<string>();
            var plans = new List<FusionPlan>();
            foreach (var recipe in recipes ?? Array.Empty<FusionRecipeData>())
            {
                if (recipe == null || recipe.ValidationError != null) continue;
                foreach (var center in ordered)
                {
                    if (center.Locked || used.Contains(center.Id) || center.Data != recipe.CenterItem) continue;
                    var adjacent = new HashSet<string>(grid.GetAdjacentItemIds(center.Id));
                    var candidates = ordered.Where(item => !item.Locked && !used.Contains(item.Id) && adjacent.Contains(item.Id)).ToArray();
                    if (recipe.Ingredients.Sum(ingredient => (long)ingredient.Quantity) > candidates.Length) continue;
                    var required = recipe.Ingredients.GroupBy(ingredient => ingredient.Item)
                        .ToDictionary(group => group.Key, group => group.Sum(ingredient => ingredient.Quantity));
                    var selected = new List<FusionItem> { center };
                    var mandatory = new HashSet<string>();
                    bool fits = true;
                    foreach (var offset in recipe.ResultItem.CreateShape().GetCells(center.Rotation))
                    {
                        var cell = center.Origin + offset;
                        if (!grid.IsInside(cell)) { fits = false; break; }
                        if (grid.TryGetItemAt(cell, out string occupant) && occupant != center.Id) mandatory.Add(occupant);
                    }
                    if (!fits || mandatory.Any(id => !candidates.Any(candidate => candidate.Id == id))) continue;
                    // Materials under the output footprint must be consumed. Select those first, then fill
                    // remaining quantities in stable spatial order. This avoids factorial permutation searches.
                    foreach (var candidate in candidates.Where(candidate => mandatory.Contains(candidate.Id)))
                    {
                        if (!required.TryGetValue(candidate.Data, out int remaining) || remaining == 0) { fits = false; break; }
                        selected.Add(candidate); required[candidate.Data]--;
                    }
                    if (!fits) continue;
                    foreach (var candidate in candidates.Where(candidate => !mandatory.Contains(candidate.Id)))
                        if (required.TryGetValue(candidate.Data, out int remaining) && remaining > 0)
                        { selected.Add(candidate); required[candidate.Data]--; }
                    if (required.Values.Any(remaining => remaining != 0)) continue;
                    var plan = new FusionPlan(recipe, selected);
                    plans.Add(plan);
                    foreach (var item in selected) { used.Add(item.Id); grid.Remove(item.Id); }
                    // Reserve the output footprint; it is not a candidate for another fusion in this battle.
                    grid.TryPlace(center.Id, recipe.ResultItem.CreateShape(), center.Origin, center.Rotation);

                }
            }
            return plans;
        }
    }
}
