using System;

namespace BackpackAutoBattle.Combat
{
    public enum AttackOutcome { NotAttack, Hit, AccuracyMiss, Evaded }
    public enum CombatDamageKind { AttackItem, StatusEffect, Other }

    public readonly struct AttackResolution
    {
        public AttackOutcome Outcome { get; }
        public bool Hit => Outcome == AttackOutcome.Hit;
        public bool TriggersAvoidance => Outcome == AttackOutcome.AccuracyMiss || Outcome == AttackOutcome.Evaded;
        public bool Critical { get; }
        public AttackResolution(AttackOutcome outcome, bool critical = false) { Outcome = outcome; Critical = critical; }
    }

    public static class AttackResolver
    {
        public static double SuccessChancePercent(double accuracy, double evasion)
            => Math.Clamp(accuracy, 30d, 100d) * (1d - Math.Clamp(evasion, 0d, 70d) / 100d);

        public static AttackResolution Resolve(double accuracy, double evasion, double criticalChance, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            foreach (double value in new[] { accuracy, evasion, criticalChance })
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(accuracy));
            if (random.NextDouble() * 100d >= Math.Clamp(accuracy, 30d, 100d)) return new(AttackOutcome.AccuracyMiss);
            // A miss never rolls evasion or critical. Zero evasion does not consume an extra random roll.
            if (evasion > 0d && random.NextDouble() * 100d < Math.Clamp(evasion, 0d, 70d)) return new(AttackOutcome.Evaded);
            return new(AttackOutcome.Hit, random.NextDouble() * 100d < Math.Clamp(criticalChance, 0d, 100d));
        }
    }

    public sealed class CombatDamageEvent
    {
        public CombatSide Source { get; }
        public CombatSide Target { get; }
        public CombatDamageKind Kind { get; }
        public string SourceItemId { get; }
        public double Damage { get; }
        public DamageResolution Resolution { get; }
        public CombatDamageEvent(CombatSide source, CombatSide target, CombatDamageKind kind,
            double damage, DamageResolution resolution, string sourceItemId)
        { Source = source; Target = target; Kind = kind; Damage = damage; Resolution = resolution; SourceItemId = sourceItemId; }
    }
}
