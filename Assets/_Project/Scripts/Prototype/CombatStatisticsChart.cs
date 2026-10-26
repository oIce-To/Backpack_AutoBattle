using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public enum StatisticsChartMode { Attack, Defense }
    public enum StatisticsMetric { Direct, Burn, Poison, Thorns, Other, Healing, Armor, Reduction }
    public sealed class StatisticsChartSegment
    {
        public StatisticsMetric Metric { get; }
        public double Value { get; }
        public StatisticsChartSegment(StatisticsMetric metric, double value) { Metric = metric; Value = value; }
        public string Label => Metric switch
        {
            StatisticsMetric.Direct => "직접 공격 피해", StatisticsMetric.Burn => "화상 피해", StatisticsMetric.Poison => "중독 피해",
            StatisticsMetric.Thorns => "가시 피해", StatisticsMetric.Other => "기타 피해", StatisticsMetric.Healing => "체력 회복",
            StatisticsMetric.Armor => "방어도 흡수", _ => "피해량 감소"
        };
        public Color Color => Metric switch
        {
            StatisticsMetric.Burn => PrototypeStatusGlyph.ColorFor(StatusEffectKind.Burn),
            StatisticsMetric.Poison => PrototypeStatusGlyph.ColorFor(StatusEffectKind.Poison),
            StatisticsMetric.Thorns => PrototypeStatusGlyph.ColorFor(StatusEffectKind.Thorns),
            StatisticsMetric.Other => new Color(.70f, .73f, .76f),
            StatisticsMetric.Healing => new Color(.60f, .92f, .34f),
            StatisticsMetric.Armor => new Color(.40f, .80f, 1f), _ => Color.white
        };
    }
    public static class CombatStatisticsChart
    {
        public static string Format(double value) => value.ToString("0.#");
        public static double Total(CombatItemStatistics item, StatisticsChartMode mode)
            => mode == StatisticsChartMode.Attack ? item.DamageDealt : item.DefenseTotal;
        public static CombatItemStatistics[] Sort(CombatStatisticsSnapshot snapshot, CombatSide side, StatisticsChartMode mode)
            => snapshot.Items.Where(i => i.Side == side).OrderByDescending(i => Total(i, mode))
                .ThenBy(i => i.InstanceId == null).ThenBy(i => i.InstanceId, StringComparer.Ordinal).ToArray();
        public static IReadOnlyList<StatisticsChartSegment> Segments(CombatItemStatistics item, StatisticsChartMode mode)
        {
            var result = mode == StatisticsChartMode.Attack ? new[]
            {
                new StatisticsChartSegment(StatisticsMetric.Direct, item.DirectDamage),
                new StatisticsChartSegment(StatisticsMetric.Burn, item.BurnDamage),
                new StatisticsChartSegment(StatisticsMetric.Poison, item.PoisonDamage),
                new StatisticsChartSegment(StatisticsMetric.Thorns, item.ThornsDamage),
                new StatisticsChartSegment(StatisticsMetric.Other, Math.Max(0, item.DamageDealt - item.DirectDamage - item.BurnDamage - item.PoisonDamage - item.ThornsDamage))
            } : new[]
            {
                new StatisticsChartSegment(StatisticsMetric.Healing, item.Healing),
                new StatisticsChartSegment(StatisticsMetric.Armor, item.DamageBlocked),
                new StatisticsChartSegment(StatisticsMetric.Reduction, item.DamageReduced)
            };
            // Floating-point residue is not a meaningful extra damage category.
            double tolerance = Math.Max(1, Total(item, mode)) * 1e-12;
            return Array.AsReadOnly(result.Where(s => s.Value > tolerance).ToArray());
        }
    }
}
