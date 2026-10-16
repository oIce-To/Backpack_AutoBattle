using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class PlacementEffectTests
    {
        private readonly List<ItemData> _temporary = new();
        [TearDown] public void Cleanup() { foreach (ItemData data in _temporary) Object.DestroyImmediate(data); _temporary.Clear(); }

        [Test]
        public void WhetstoneAsset_HasRequestedPriceRarityFootprintRangeAndGeneratedDescription()
        {
            ItemData stone = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_Whetstone.asset");
            Assert.That(stone.DisplayName, Is.EqualTo("숫돌"));
            Assert.That(stone.Rarity, Is.EqualTo(ItemRarity.Common));
            Assert.That(stone.PurchasePrice, Is.EqualTo(2));
            Assert.That(stone.OccupiedCells, Is.EqualTo(new[] { Vector2Int.zero }));
            Assert.That(stone.Activation.HasCycle, Is.False);
            ItemPlacementEffect effect = stone.PlacementEffects.Single();
            Assert.That(effect.Cells, Is.EquivalentTo(new[] { Vector2Int.left, Vector2Int.right }));
            Assert.That(effect.TargetFilter.Category, Is.EqualTo(ItemCategory.Weapon));
            Assert.That(effect.TargetFilter.RequiredTags, Is.EqualTo(ItemTags.Melee));
            Assert.That(effect.AttackDamageBonus, Is.EqualTo(1f));
            Assert.That(ItemTooltipContent.FromItem(stone).PlacementDescriptions.Single(),
                Is.EqualTo(ItemTooltipContent.PlacementIconTag + "에 배치되어 있는 근접 무기의 공격력을 1 증가시킵니다."));
        }

        [Test]
        public void EveryPlacementEffect_AutomaticallyGetsAnInlineIconWithoutItemSpecificLayout()
        {
            ItemData data = Stone();
            Set(data, "_placementEffects", new List<ItemPlacementEffect>
            {
                new(new[] { Vector2Int.left }, 1f),
                new(new[] { new Vector2Int(2, -1) }, -2f, new(ItemCategory.Weapon, ItemTags.Ranged))
            });
            Set(data, "_description", "일반 설명");
            Set(data, "_effectDescription", "추가 효과");
            ItemTooltipContent content = ItemTooltipContent.FromItem(data);
            Assert.That(content.PlacementDescriptions.Count, Is.EqualTo(2));
            Assert.That(content.PlacementDescriptions.All(text => text.StartsWith(ItemTooltipContent.PlacementIconTag)), Is.True);
            Assert.That(content.PlacementDescriptions[1], Does.Contain("원거리 무기의 공격력을 2 감소"));
            Assert.That(content.GetDescriptionText(), Is.EqualTo(string.Join("\n\n", content.PlacementDescriptions) + "\n\n추가 효과\n\n일반 설명"));
            var snapshot = Snap("future_item", data, Vector2Int.zero);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, null).GetDescriptionText(), Is.EqualTo(content.GetDescriptionText()));
        }

        [TestCase(ItemCategory.Weapon, ItemTags.Melee, true)]
        [TestCase(ItemCategory.Weapon, ItemTags.Melee | ItemTags.Magic, true)]
        [TestCase(ItemCategory.Weapon, ItemTags.Ranged, false)]
        [TestCase(ItemCategory.Weapon, ItemTags.Magic, false)]
        [TestCase(ItemCategory.Armor, ItemTags.Melee, false)]
        public void Eligibility_IsIdenticalForDiamondFillAndCombatBonus(ItemCategory category, ItemTags tags, bool expected)
        {
            CombatItemSnapshot stone = Snap("stone", Stone(), new(2, 2));
            CombatItemSnapshot target = Snap("target", Weapon(category, tags), new(1, 2));
            var loadout = new CombatLoadoutSnapshot(new[] { stone, target });
            var cells = PlacementEffectResolver.GetCells(loadout, stone);
            Assert.That(cells.Single(cell => cell.Cell == new Vector2Int(1, 2)).ReceivesEffect, Is.EqualTo(expected));
            Assert.That(cells.Single(cell => cell.Cell == new Vector2Int(3, 2)).ReceivesEffect, Is.False);
            Assert.That(PlacementEffectResolver.GetAttackBonuses(loadout).ContainsKey("target"), Is.EqualTo(expected));
        }

        [Test]
        public void SameTargetAcrossMultipleCells_CountsOncePerSourceButSeparateStonesStack()
        {
            ItemData wide = Weapon();
            Set(wide, "_occupiedCells", new List<Vector2Int> { Vector2Int.zero, new(2, 0) });
            ItemData source = Stone(new(-1, 0), new(1, 0), new(-1, 0));
            var stone = Snap("stone", source, new(2, 2));
            var weapon = Snap("weapon", wide, new(1, 2));
            var loadout = new CombatLoadoutSnapshot(new[] { stone, weapon });
            Assert.That(PlacementEffectResolver.GetCells(loadout, stone).Count(cell => cell.ReceivesEffect), Is.EqualTo(2));
            Assert.That(PlacementEffectResolver.GetAttackBonuses(loadout)["weapon"], Is.EqualTo(1f));
            var second = Snap("second", Stone(new Vector2Int(0, -1)), new(1, 3));
            loadout = new CombatLoadoutSnapshot(new[] { stone, second, weapon });
            Assert.That(PlacementEffectResolver.GetAttackBonuses(loadout)["weapon"], Is.EqualTo(2f));
        }

        [Test]
        public void ArbitraryDiagonalAndDistantOffsets_RotateUsingIrregularFootprintNotRangeBounds()
        {
            ItemData source = Stone(new(2, -1), new(3, 1));
            Set(source, "_occupiedCells", new List<Vector2Int> { new(0, 0), new(0, 1), new(1, 1) });
            var stone = Snap("stone", source, new(2, 2), ItemRotation.Clockwise90);
            var target = Snap("target", Weapon(), new(4, 4));
            var armor = Snap("armor", Weapon(ItemCategory.Armor), new(2, 5));
            var loadout = new CombatLoadoutSnapshot(new[] { stone, target, armor });
            var cells = PlacementEffectResolver.GetCells(loadout, stone);
            Assert.That(cells.Select(cell => cell.Cell), Is.EquivalentTo(new[] { new Vector2Int(4, 4), new Vector2Int(2, 5) }));
            Assert.That(cells.Single(cell => cell.Cell == new Vector2Int(4, 4)).ReceivesEffect, Is.True);
            Assert.That(cells.Single(cell => cell.Cell == new Vector2Int(2, 5)).ReceivesEffect, Is.False);
            Assert.That(stone.OccupiedCells, Is.EquivalentTo(new[] { new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(2, 3) }));
        }

        [TestCase(ItemRotation.None, -1, 0)]
        [TestCase(ItemRotation.Clockwise90, 0, -1)]
        [TestCase(ItemRotation.Clockwise180, 1, 0)]
        [TestCase(ItemRotation.Clockwise270, 0, 1)]
        public void RotatingSingleCellItem_RotatesItsEffectOffsets(ItemRotation rotation, int dx, int dy)
        {
            var stone = Snap("stone", Stone(new Vector2Int(-1, 0)), new(2, 2), rotation);
            var loadout = new CombatLoadoutSnapshot(new[] { stone });
            Assert.That(PlacementEffectResolver.GetCells(loadout, stone).Single().Cell, Is.EqualTo(new Vector2Int(2 + dx, 2 + dy)));
        }

        [Test]
        public void OutsideGridAndSelfTargets_DoNotGrantBonuses()
        {
            ItemData self = Weapon();
            Set(self, "_placementEffects", new List<ItemPlacementEffect> { new(new[] { Vector2Int.zero, Vector2Int.left }, 1f) });
            var source = Snap("self", self, Vector2Int.zero);
            var loadout = new CombatLoadoutSnapshot(new[] { source });
            Assert.That(PlacementEffectResolver.GetCells(loadout, source), Has.Count.EqualTo(1));
            Assert.That(PlacementEffectResolver.GetCells(loadout, source).Single().ReceivesEffect, Is.False);
            Assert.That(PlacementEffectResolver.GetAttackBonuses(loadout), Is.Empty);
        }

        [Test]
        public void SnapshotAndSOStayUnchanged_ActualDamageAndGreenTooltipUseTheBonusOnBothSides()
        {
            ItemData stone = Stone(); ItemData blade = Weapon();
            var source = Snap("stone", stone, new(1, 0));
            var weapon = Snap("weapon", blade, Vector2Int.zero);
            var loadout = new CombatLoadoutSnapshot(new[] { source, weapon });
            Set(stone, "_placementEffects", new List<ItemPlacementEffect> { new(new[] { Vector2Int.left }, 9f) });
            CombatSimulation Battle(CombatLoadoutSnapshot player, CombatLoadoutSnapshot enemy)
                => new(new("Player", 100d, player), new("Enemy", 100d, enemy), new System.Random(123));
            var empty = new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>());
            CombatSimulation combat = Battle(loadout, empty);
            Assert.That(combat.PlayerItems, Has.Count.EqualTo(1));
            Assert.That(combat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(6f));
            Assert.That(combat.Advance(2d).Single().Damage, Is.EqualTo(6d));
            Assert.That(ItemTooltipContent.FromCombat(weapon, combat.PlayerWeapons.Single()).GetStatChange(WeaponStatIcon.Attack), Is.EqualTo(ItemStatChange.Improved));
            Assert.That(blade.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(weapon.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(source.PlacementEffects.Single().AttackDamageBonus, Is.EqualTo(1f));
            Assert.That(Battle(loadout, loadout).EnemyWeapons.Single().Stats.AttackDamage, Is.EqualTo(6f));
            Assert.That(Battle(loadout, loadout).PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(6f));
            Assert.That(Battle(new CombatLoadoutSnapshot(new[] { weapon }), loadout).PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(5f));
        }

        private ItemData Stone(params Vector2Int[] cells)
        {
            ItemData data = Data();
            Set(data, "_placementEffects", new List<ItemPlacementEffect> { new(cells.Length == 0 ? new[] { Vector2Int.left, Vector2Int.right } : cells, 1f) });
            return data;
        }
        private ItemData Weapon(ItemCategory category = ItemCategory.Weapon, ItemTags tags = ItemTags.Melee)
        {
            ItemData data = Data();
            Set(data, "_category", category); Set(data, "_tags", tags);
            Set(data, "_activation", new ItemActivationData(2f, new ItemActivationEffect(ItemEffectKind.Attack)));
            Set(data, "_weaponStats", new WeaponStats(5f, 2f, 100f, 0f, 200f));
            return data;
        }
        private ItemData Data() { var data = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(data); return data; }
        private static void Set(object item, string field, object value)
            => item.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, value);
        private static CombatItemSnapshot Snap(string id, ItemData data, Vector2Int origin, ItemRotation rotation = ItemRotation.None)
            => new(id, data, origin, rotation, data.CreateShape().GetCells(rotation).Select(cell => cell + origin));
    }
}
