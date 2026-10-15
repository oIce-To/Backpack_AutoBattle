using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class ItemStatComparisonTests
    {
        [TestCase(WeaponStatIcon.Attack, 6f, ItemStatChange.Improved)]
        [TestCase(WeaponStatIcon.Attack, 4f, ItemStatChange.Reduced)]
        [TestCase(WeaponStatIcon.Cooldown, 1f, ItemStatChange.Improved)]
        [TestCase(WeaponStatIcon.Cooldown, 3f, ItemStatChange.Reduced)]
        [TestCase(WeaponStatIcon.Accuracy, 95f, ItemStatChange.Improved)]
        [TestCase(WeaponStatIcon.Accuracy, 80f, ItemStatChange.Reduced)]
        [TestCase(WeaponStatIcon.CriticalChance, 20f, ItemStatChange.Improved)]
        [TestCase(WeaponStatIcon.CriticalChance, 0f, ItemStatChange.Reduced)]
        [TestCase(WeaponStatIcon.CriticalDamage, 220f, ItemStatChange.Improved)]
        [TestCase(WeaponStatIcon.CriticalDamage, 150f, ItemStatChange.Reduced)]
        public void EffectiveStats_AreComparedWithBattleSnapshotAndCooldownUsesTheOppositeDirection(
            WeaponStatIcon stat, float value, ItemStatChange expected)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_TrainingBlade.asset");
            var snapshot = Snapshot(item);
            var runtime = new CombatWeapon(snapshot, () => 0d);
            switch (stat)
            {
                case WeaponStatIcon.Attack: runtime.Stats.AttackDamage = value; break;
                case WeaponStatIcon.Cooldown: runtime.Stats.CooldownSeconds = value; break;
                case WeaponStatIcon.Accuracy: runtime.Stats.AccuracyPercent = value; break;
                case WeaponStatIcon.CriticalChance: runtime.Stats.CriticalChancePercent = value; break;
                case WeaponStatIcon.CriticalDamage: runtime.Stats.CriticalDamagePercent = value; break;
            }
            ItemTooltipContent content = ItemTooltipContent.FromCombat(snapshot, runtime);
            foreach (WeaponStatIcon field in System.Enum.GetValues(typeof(WeaponStatIcon)))
                Assert.That(content.GetStatChange(field), Is.EqualTo(field == stat ? expected : ItemStatChange.Unchanged));
            Assert.That(item.Activation.CooldownSeconds, Is.EqualTo(2f));
            Assert.That(item.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
        }

        [Test]
        public void UnmodifiedAndShopStats_RemainNeutralAndComparisonIgnoresLaterSOMutations()
        {
            ItemData original = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_TrainingBlade.asset");
            ItemData copy = Object.Instantiate(original);
            try
            {
                var snapshot = Snapshot(copy);
                var runtime = new CombatWeapon(snapshot, () => 0d);
                var modified = new SerializedObject(copy);
                modified.FindProperty("_weaponStats").FindPropertyRelative("_attackDamage").floatValue = 99f;
                modified.FindProperty("_activation").FindPropertyRelative("_cooldownSeconds").floatValue = 99f;
                modified.ApplyModifiedPropertiesWithoutUndo();
                foreach (WeaponStatIcon stat in System.Enum.GetValues(typeof(WeaponStatIcon)))
                {
                    Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetStatChange(stat), Is.EqualTo(ItemStatChange.Unchanged));
                    Assert.That(ItemTooltipContent.FromItem(copy).GetStatChange(stat), Is.EqualTo(ItemStatChange.Unchanged));
                }
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void ArmorCooldown_UsesActivationBaselineRatherThanUnusedWeaponStats()
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_WoodenShield.asset");
            var snapshot = Snapshot(item);
            var runtime = new CombatItem(snapshot, () => 0d);
            runtime.Cycle.SetBaseInterval(1f);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetStatChange(WeaponStatIcon.Cooldown), Is.EqualTo(ItemStatChange.Improved));
            runtime.Cycle.SetBaseInterval(3f);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetStatChange(WeaponStatIcon.Cooldown), Is.EqualTo(ItemStatChange.Reduced));
            runtime.Cycle.SetBaseInterval(2f);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetStatChange(WeaponStatIcon.Cooldown), Is.EqualTo(ItemStatChange.Unchanged));
        }

        private static CombatItemSnapshot Snapshot(ItemData item) => new("comparison_test", item, Vector2Int.zero,
            ItemRotation.None, item.OccupiedCells);
    }
}
