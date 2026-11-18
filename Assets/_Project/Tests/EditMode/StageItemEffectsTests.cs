using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class StageItemEffectsTests
    {
        private const string CloverNote = "네잎클로버류의 상점 상품 등급 변화 효과는 하나만 적용됩니다.";
        private const string PlatinumNote = "플래티넘 클로버가 있으면 무료 새로고침과 할인 확률 증가 효과는 플래티넘 클로버 하나만 적용됩니다.";
        private const string ShieldNote = "무기나 펫으로부터 받는 피해 감소 효과 중 가장 높은 효과 하나만 적용됩니다.";
        private const string PoisonNote = "만독불침의 서와 만천화우의 중독 피해 감소 효과는 하나만 적용됩니다.";
        private const string BurnNote = "용의 숨결과 태초의 불꽃의 화상 중첩 유지 효과는 하나만 적용됩니다.";

        [TestCase("FourLeafClover", CloverNote)]
        [TestCase("CursedClover", CloverNote)]
        [TestCase("PlatinumClover", CloverNote + "\n\n" + PlatinumNote)]
        [TestCase("PlatinumCard", PlatinumNote)]
        [TestCase("WoodenShield", ShieldNote)]
        [TestCase("MithrilShield", ShieldNote)]
        [TestCase("SolarRadiance", ShieldNote)]
        [TestCase("GenesisArmor", ShieldNote)]
        [TestCase("PoisonImmunityBook", PoisonNote)]
        [TestCase("NeedleStorm", PoisonNote)]
        [TestCase("DragonsBreath", BurnNote)]
        [TestCase("PrimalFlame", BurnNote)]
        public void SharedEffects_DescribeTheirStackingRuleOnceInItsOwnParagraph(string itemName, string note)
        {
            var item = Unit(50, (itemName, 0, 0)).Loadout.Items.Single();
            var content = ItemTooltipContent.FromCombat(item, null);
            Assert.That(content.GetDescriptionText(), Does.EndWith("\n\n" + note));
            Assert.That(content.EffectDescription, Does.Not.Contain("하나만 적용됩니다."));
        }

        [Test]
        public void EveryFusionIsReachableFromShopMaterials()
        {
            var items = LoadAssets<ItemData>("Items");
            var recipes = LoadAssets<FusionRecipeData>("Recipes");
            var reachable = items.Where(item => !item.ExcludeFromShop).ToHashSet();
            Assert.That(recipes.Select(recipe => recipe.ResultItem).Distinct().Count(), Is.EqualTo(recipes.Length));
            Assert.That(recipes.All(recipe => recipe.ValidationError == null && recipe.ResultItem.ExcludeFromShop), Is.True);
            int previous;
            do
            {
                previous = reachable.Count;
                foreach (var recipe in recipes)
                    if (reachable.Contains(recipe.CenterItem) && recipe.Ingredients.All(ingredient => reachable.Contains(ingredient.Item)))
                        reachable.Add(recipe.ResultItem);
            } while (reachable.Count > previous);
            Assert.That(reachable, Is.EquivalentTo(items));
        }

        [Test]
        public void StrongestShieldPreventsDirectDamageAndPotionHealsExactlyOnce()
        {
            var combat = new CombatSimulation(Unit(50, ("HealingPotion", 0, 0), ("WornHelmet", 1, 0),
                ("WoodenShield", 2, 0), ("MithrilShield", 3, 0)), Unit(100, ("WoodenSword", 0, 0)), new ConstantRandom(.5));
            Assert.That(combat.PlayerArmor, Is.EqualTo(11).Within(.001));
            combat.EnemyWeapons.Single().Stats.AttackDamage = 46;
            combat.EnemyWeapons.Single().Stats.AccuracyPercent = 100;
            combat.Advance(2);
            Assert.That(combat.PlayerHealth, Is.EqualTo(31.7).Within(.001));
            var stats = combat.CaptureStatistics().Items;
            Assert.That(stats.Single(item => item.InstanceId == "HealingPotion").Healing, Is.EqualTo(7.5));
            Assert.That(stats.Single(item => item.InstanceId == "WoodenShield").DamageReduced, Is.Zero);
            Assert.That(stats.Single(item => item.InstanceId == "MithrilShield").DamageReduced, Is.EqualTo(9.2).Within(.001));
            combat.EnemyWeapons.Single().Stats.AttackDamage = 10;
            combat.Advance(2);
            Assert.That(combat.PlayerHealth, Is.EqualTo(23.7).Within(.001));
        }

        [Test]
        public void PotionTooltipShowsThresholdAndResolvedHealing()
        {
            var player = Unit(80, ("HealingPotion", 0, 0));
            var potion = player.Loadout.Items.Single();
            Assert.That(ItemTooltipContent.FromCombat(potion, null).GetDescriptionText(),
                Is.EqualTo("체력이 최대 체력의 50% 미만으로 떨어지면 전투당 한 번 체력을 12 회복합니다."));

            var combat = new CombatSimulation(player, Unit(100, ("RazorThorn", 0, 0)));
            Assert.That(ItemTooltipContent.FromCombat(potion, combat.PlayerItems.Single(),
                healingMultiplier: combat.HealingMultiplier(CombatSide.Player)).GetDescriptionText(),
                Does.Contain("<color=#FF5C57>9.6</color>").And.Not.Contain("15%"));
        }

        [Test]
        public void BolaIgnoresEvasionBreaksArmorAndNeverFiresTwice()
        {
            var combat = new CombatSimulation(Unit(50, ("Bola", 0, 0)), Unit(100, ("WornHelmet", 0, 0)), new ConstantRandom(.5));
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 70);
            Assert.That(combat.Advance(3).Count(item => item.Kind == ItemEffectKind.Attack), Is.EqualTo(1));
            Assert.That(combat.EnemyArmor, Is.Zero);
            Assert.That(combat.EnemyHealth, Is.EqualTo(100 - (10d - 20d / 3d)).Within(.001));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Agility), Is.EqualTo(55));
        }

        [Test]
        public void PlacementModifiersApplyToTheirTargetsAndTrueShotConvertsOverflow()
        {
            var combat = new CombatSimulation(Unit(50, ("TrueShot", 0, 0), ("PoisonQuiver", 2, 0),
                ("WoodenBow", 1, 0), ("WoodenSword", 3, 0)), Unit(100), new ConstantRandom(.3));
            var bow = combat.PlayerWeapons.Single(item => item.InstanceId == "WoodenBow");
            var sword = combat.PlayerWeapons.Single(item => item.InstanceId == "WoodenSword");
            Assert.That(bow.Stats.AccuracyPercent, Is.EqualTo(100)); // Concentration from True Shot plus the quiver.
            Assert.That(bow.Cycle.IntervalSeconds, Is.EqualTo(1.5 / 1.15).Within(.001));
            Assert.That(sword.Cycle.IntervalSeconds, Is.EqualTo(2));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Focus, 85);
            Assert.That(bow.Stats.CriticalChancePercent, Is.EqualTo(110));
            Assert.That(bow.Stats.CriticalDamagePercent, Is.EqualTo(220));
            Assert.That(sword.Stats.CriticalChancePercent, Is.EqualTo(10));
            combat.Advance(1.32);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(101));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Poison), Is.EqualTo(1));
        }

        [Test]
        public void PermafrostSupportsPassiveFreezeAndGenesisReducesIncomingStacks()
        {
            var combat = new CombatSimulation(Unit(50, ("Permafrost", 0, 0), ("IceCrystal", 1, 0)),
                Unit(100, ("GenesisArmor", 0, 0)));
            // 3 × 1.5 rounds to 5, then the incoming effect halves it to 2.
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Freeze), Is.EqualTo(2));
        }

        [Test]
        public void BurnPreservationAndPoisonResistanceApplyOnce()
        {
            var combat = new CombatSimulation(Unit(50, ("DragonsBreath", 0, 0), ("PoisonImmunityBook", 1, 0), ("NeedleStorm", 3, 0)), Unit(100));
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Poison, 4);
            combat.Advance(1);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(2));
            Assert.That(combat.PlayerHealth, Is.EqualTo(48).Within(.001));
            Assert.That(combat.CaptureStatistics().Items.Sum(item => item.DamageReduced), Is.EqualTo(2).Within(.001));
        }

        private static CombatantSnapshot Unit(double health, params (string name, int x, int y)[] placements)
        {
            var owner = new CharacterStatsSnapshot(health, 10);
            var items = new List<CombatItemSnapshot>();
            foreach (var placement in placements)
            {
                var data = AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/_Project/Data/Items/Stage/Item_{placement.name}.asset");
                Assert.That(data, Is.Not.Null, placement.name);
                var origin = new Vector2Int(placement.x, placement.y);
                items.Add(new CombatItemSnapshot(placement.name, data, origin, ItemRotation.None,
                    data.OccupiedCells.Select(cell => origin + cell), ownerStats: owner));
            }
            return new CombatantSnapshot("Test", health, new CombatLoadoutSnapshot(items));
        }

        private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
            => AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { $"Assets/_Project/Data/{folder}/Stage" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).ToArray();

        private sealed class ConstantRandom : System.Random
        {
            private readonly double _value;
            public ConstantRandom(double value) => _value = value;
            public override double NextDouble() => _value;
        }
    }
}
