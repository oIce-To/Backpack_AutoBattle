using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed class PeriodicStatusTickEvent
    {
        public double Time { get; }
        public CombatSide Source { get; }
        public CombatSide Target { get; }
        public StatusEffectKind Kind { get; }
        public long Stacks { get; }
        public double Damage { get; }
        public DamageResolution Resolution { get; }
        public PeriodicStatusTickEvent(double time, CombatSide source, CombatSide target, StatusEffectKind kind,
            long stacks, double damage, DamageResolution resolution)
        { Time = time; Source = source; Target = target; Kind = kind; Stacks = stacks; Damage = damage; Resolution = resolution; }
    }

    public sealed partial class CombatSimulation
    {
        private sealed class PeriodicContribution
        {
            public CombatSide Source;
            public long Stacks;
            public double DamagePerStack;
        }
        private sealed class PeriodicState
        {
            public CombatSide Target;
            public StatusEffectKind Kind;
            public double NextTickAt;
            public readonly List<PeriodicContribution> Contributions = new();
        }
        private readonly Dictionary<(CombatSide, StatusEffectKind), PeriodicState> _periodicStates = new();
        public event Action<PeriodicStatusTickEvent> StatusTicked;

        public double GetStatusDamagePerTick(CombatSide target, StatusEffectKind kind)
            => _periodicStates.TryGetValue((target, kind), out var state)
                ? state.Contributions.Sum(c => c.Stacks * c.DamagePerStack) * GetIncomingDamageMultiplier(target) : 0;

        private void AddPeriodicStacks(CombatSide target, StatusEffectKind kind, CombatSide source, long stacks)
        {
            if (!_periodicStates.TryGetValue((target, kind), out var state))
            {
                state = new PeriodicState { Target = target, Kind = kind,
                    NextTickAt = ElapsedSeconds + GetStatusDefinition(kind).TickIntervalSeconds };
                _periodicStates.Add((target, kind), state);
            }
            var contribution = state.Contributions.FirstOrDefault(c => c.Source == source);
            if (contribution == null)
            {
                contribution = new PeriodicContribution { Source = source,
                    DamagePerStack = GetStatusDefinition(kind).DamagePerStack((source == CombatSide.Player ? Player : Enemy).Stats) };
                state.Contributions.Add(contribution);
            }
            contribution.Stacks = checked(contribution.Stacks + stacks);
        }

        public void RemoveStatus(CombatSide target, StatusEffectKind kind, long stacks)
        {
            if (stacks < 0 || !Enum.IsDefined(typeof(CombatSide), target) || !_statusDefinitions.ContainsKey(kind))
                throw new ArgumentOutOfRangeException(nameof(stacks));
            if (Result != CombatResult.Running || kind == StatusEffectKind.Enrage) return;
            RemoveStatusStacks(target, kind, stacks);
            if (GetStatusDefinition(kind).AffectedStat == StatusStat.ActivationSpeed) RefreshItemSpeeds(target);
            if (GetStatusDefinition(kind).AffectedStat is StatusStat.Accuracy or StatusStat.CriticalChance) RefreshWeaponStatusModifiers(target);
        }

        private void RemoveStatusStacks(CombatSide target, StatusEffectKind kind, long stacks)
        {
            long removed = Math.Min(stacks, GetStatusStacks(target, kind));
            _statusStacks[(target, kind)] = GetStatusStacks(target, kind) - removed;
            if (!_periodicStates.TryGetValue((target, kind), out var state))
            { RemoveStatusOrigins(target, kind, removed); return; }
            // Natural decay/cleansing removes oldest source contributions first, one stack total, not one per source.
            foreach (var contribution in state.Contributions)
            {
                long take = Math.Min(removed, contribution.Stacks);
                RemoveStatusOrigins(target, kind, take, contribution.Source);
                contribution.Stacks -= take; removed -= take;
            }
            state.Contributions.RemoveAll(c => c.Stacks == 0);
            if (state.Contributions.Count == 0) _periodicStates.Remove((target, kind));
        }

        private PeriodicState NextPeriodicTick()
        {
            PeriodicState next = null;
            foreach (var state in _periodicStates.Values)
                if (next == null || state.NextTickAt < next.NextTickAt
                    || state.NextTickAt == next.NextTickAt && ((int)state.Target < (int)next.Target
                        || state.Target == next.Target && (int)state.Kind < (int)next.Kind)) next = state;
            return next;
        }

        private void ResolvePeriodicTick(PeriodicState state)
        {
            // Advance the shared status clock before callbacks. Adding stacks does not postpone it.
            state.NextTickAt += GetStatusDefinition(state.Kind).TickIntervalSeconds;
            foreach (var contribution in state.Contributions.ToArray())
            {
                if (Result != CombatResult.Running) break;
                double damage = contribution.Stacks * contribution.DamagePerStack;
                // Keep the original one-damage-event-per-source-side rule; provenance must not change combat outcomes.
                DamageResolution resolution = DealDamage(contribution.Source, state.Target, damage, CombatDamageKind.StatusEffect,
                    null, StatusWeights(state.Target, state.Kind, contribution.Source), state.Kind);
                UpdateCombatResult();
                StatusTicked?.Invoke(new PeriodicStatusTickEvent(ElapsedSeconds, contribution.Source, state.Target,
                    state.Kind, contribution.Stacks, damage * GetIncomingDamageMultiplier(state.Target), resolution));
            }
            RemoveStatusStacks(state.Target, state.Kind, GetStatusDefinition(state.Kind).StacksLostPerTick);
        }
    }
}
