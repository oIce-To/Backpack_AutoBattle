using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class CombatStatisticsChartTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var item in _temporary) Object.DestroyImmediate(item); _temporary.Clear(); }
        [Test]
        public void AttackExample_Sorts10070And50_WithSeparateBurnPoisonAndThorns()
        {
            // SO coefficients use floats: allow their sub-display precision at the lethal boundary.
            var c = new CombatSimulation(Unit(100, Weapon(70), Weapon(50), Grant(StatusEffectKind.Thorns, 50)), Unit(219.999995, Weapon(1, 1.5f)), new FixedRandom());
            c.Advance(1); foreach (var item in c.PlayerItems) item.Cycle.SetBaseInterval(100);
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 25, CombatSide.Player, "i0");
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 20, CombatSide.Player, "i1"); c.Advance(1);
            Assert.That(c.Result, Is.EqualTo(CombatResult.Victory));
            var rows = CombatStatisticsChart.Sort(c.CaptureStatistics(), CombatSide.Player, StatisticsChartMode.Attack);
            Assert.That(rows.Select(r => r.InstanceId), Is.EqualTo(new[] { "i0", "i1", "i2" }));
            Assert.That(rows[0].DamageDealt, Is.EqualTo(100).Within(1e-5));
            Assert.That(rows[1].DamageDealt, Is.EqualTo(70).Within(1e-5));
            Assert.That(rows[2].DamageDealt, Is.EqualTo(50).Within(1e-5));
            Assert.That(rows[0].BurnDamage, Is.EqualTo(30).Within(1e-5));
            Assert.That(rows[1].PoisonDamage, Is.EqualTo(20).Within(1e-5));
            Assert.That(rows[2].ThornsDamage, Is.EqualTo(50).Within(1e-5));
            Assert.That(CombatStatisticsChart.Segments(rows[0], StatisticsChartMode.Attack).Select(s => s.Metric), Is.EqualTo(new[] { StatisticsMetric.Direct, StatisticsMetric.Burn }));
            Assert.That(CombatStatisticsChart.Segments(rows[1], StatisticsChartMode.Attack).Select(s => s.Metric), Is.EqualTo(new[] { StatisticsMetric.Direct, StatisticsMetric.Poison }));
            Assert.That(CombatStatisticsChart.Segments(rows[2], StatisticsChartMode.Attack).Single().Metric, Is.EqualTo(StatisticsMetric.Thorns));
        }
        [Test]
        public void DefenseCombinesActualHealingAbsorptionAndRecordedReduction_WithoutChangingHealth()
        {
            var c = new CombatSimulation(Unit(100, Weapon(1), Grant(StatusEffectKind.Lifesteal, 2), Armor(10, .5f), Passive()), Unit(1000, Weapon(3, 1.5f)), new FixedRandom());
            c.RecordDamageReduction(CombatSide.Player, "i1", 5); c.RecordDamageReduction(CombatSide.Player, "i3", 4);
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 5, CombatDamageKind.Other); c.Advance(1.5);
            var rows = CombatStatisticsChart.Sort(c.CaptureStatistics(), CombatSide.Player, StatisticsChartMode.Defense);
            Assert.That(rows.Select(r => r.InstanceId), Is.EqualTo(new[] { "i1", "i3", "i2", "i0" }));
            Assert.That(rows[0].DefenseTotal, Is.EqualTo(7).Within(1e-5));
            Assert.That(rows[0].Healing, Is.EqualTo(2).Within(1e-5)); Assert.That(rows[0].DamageReduced, Is.EqualTo(5));
            Assert.That(rows[2].DamageBlocked, Is.EqualTo(3)); Assert.That(c.PlayerHealth, Is.EqualTo(97).Within(1e-5));
            Assert.That(CombatStatisticsChart.Segments(rows[0], StatisticsChartMode.Defense).Select(s => s.Metric), Is.EqualTo(new[] { StatisticsMetric.Healing, StatisticsMetric.Reduction }));
        }
        [Test]
        public void UnknownDamageHasNeutralSegment_NotFakePoisonOrThorns()
        {
            var c = new CombatSimulation(Unit(100, Passive()), Unit(100));
            c.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 4, CombatDamageKind.StatusEffect, "i0");
            c.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 3, CombatDamageKind.Other, "i0");
            var segment = CombatStatisticsChart.Segments(c.CaptureStatistics().Items.Single(), StatisticsChartMode.Attack).Single();
            Assert.That(segment.Metric, Is.EqualTo(StatisticsMetric.Other)); Assert.That(segment.Value, Is.EqualTo(7));
        }
        [TestCase(33.333333, "33.3")]
        [TestCase(26.666667, "26.7")]
        [TestCase(100, "100")]
        [TestCase(0, "0")]
        public void DisplayUsesAtMostOneDecimal(double value, string expected) => Assert.That(CombatStatisticsChart.Format(value), Is.EqualTo(expected));
        [Test]
        public void SortingAndSnapshotsKeepUnroundedValues_AndReductionStopsAtBattleEnd()
        {
            var c = new CombatSimulation(Unit(100, Weapon(2), Passive()), Unit(1), new FixedRandom());
            c.RecordDamageReduction(CombatSide.Player, "i0", .041); c.RecordDamageReduction(CombatSide.Player, "i1", .049);
            var snapshot = c.CaptureStatistics();
            Assert.That(CombatStatisticsChart.Sort(snapshot, CombatSide.Player, StatisticsChartMode.Defense).First().InstanceId, Is.EqualTo("i1"));
            Assert.That(snapshot.Items.First().DamageReduced, Is.EqualTo(.041));
            c.Advance(1); c.RecordDamageReduction(CombatSide.Player, "i0", 100);
            Assert.That(c.CaptureStatistics().Items.First().DamageReduced, Is.EqualTo(.041));
            Assert.That(snapshot.Items.First().DamageReduced, Is.EqualTo(.041));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.RecordDamageReduction(CombatSide.Player, "i0", double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.RecordDamageReduction(CombatSide.Player, "i0", -1));
        }
        [Test]
        public void SegmentColorsMatchTheirStatusOrDefenseMeaning()
        {
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Direct, 1).Color, Is.EqualTo(Color.white));
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Reduction, 1).Color, Is.EqualTo(Color.white));
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Burn, 1).Color, Is.EqualTo(PrototypeStatusGlyph.ColorFor(StatusEffectKind.Burn)));
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Poison, 1).Color, Is.EqualTo(PrototypeStatusGlyph.ColorFor(StatusEffectKind.Poison)));
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Thorns, 1).Color, Is.EqualTo(PrototypeStatusGlyph.ColorFor(StatusEffectKind.Thorns)));
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Healing, 1).Color, Is.EqualTo(new Color(.6f, .92f, .34f)));
            Assert.That(new StatisticsChartSegment(StatisticsMetric.Armor, 1).Color, Is.EqualTo(new Color(.4f, .8f, 1)));
        }
        private ItemData Passive() { var item = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(item); return item; }
        private ItemData Weapon(float damage, float interval = 1)
        {
            var item = Passive(); Set(item, "_weaponStats", new WeaponStats(damage, interval, 100, 0, 200));
            Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.Attack))); return item;
        }
        private ItemData Grant(StatusEffectKind kind, int stacks)
        { var item = Passive(); Set(item, "_battleStartStatuses", new List<StatusApplication> { new(kind, stacks) }); return item; }
        private ItemData Armor(float amount, float interval)
        { var item = Passive(); Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.GainArmor, amount))); return item; }
        private static CombatantSnapshot Unit(double hp, params ItemData[] items) => new("Test", hp,
            new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot("i" + i, item, new Vector2Int(i, 0), ItemRotation.None, item.OccupiedCells))));
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        private sealed class FixedRandom : System.Random { public override double NextDouble() => .5; }
    }
}
