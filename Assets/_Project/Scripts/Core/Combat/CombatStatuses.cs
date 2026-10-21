using System;
using System.Collections.Generic;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    /// <summary>Immutable battle-local copy, never a live reference to an editable SO.</summary>
    public sealed class CombatStatusDefinition
    {
        public CombatStatusDefinition(StatusEffectKind kind, string name, float percent = 1f,
            StatScaling damageScaling = null, double tickInterval = 1, int? stacksLostPerTick = null)
        {
            Kind = kind; DisplayName = name; PercentPerStack = percent;
            _damageScaling = damageScaling?.Copy() ?? new(StatReference.Attack, kind == StatusEffectKind.Burn ? .12f : .10f);
            TickIntervalSeconds = tickInterval;
            StacksLostPerTick = stacksLostPerTick ?? (kind == StatusEffectKind.Burn ? 1 : 0);
        }
        private readonly StatScaling _damageScaling;
        public double TickIntervalSeconds { get; }
        public int StacksLostPerTick { get; }
        public double DamagePerStack(CharacterStatsSnapshot owner) => _damageScaling.Resolve(0, owner);
        public StatusEffectKind Kind { get; }
        public string DisplayName { get; }
        public float PercentPerStack { get; }
        public bool IsDebuff => StatusEffectRules.IsDebuff(Kind);
        public StatusStat AffectedStat => StatusEffectRules.AffectedStat(Kind);
        public StatusEffectKind? Opposite => StatusEffectRules.Opposite(Kind);
        public double PercentAt(long stacks) => (double)stacks * PercentPerStack;
    }

    public sealed partial class CombatSimulation
    {
        private readonly Dictionary<StatusEffectKind, CombatStatusDefinition> _statusDefinitions = new();
        private readonly Dictionary<(CombatSide, StatusEffectKind), long> _statusStacks = new();
        public CombatStatusDefinition GetStatusDefinition(StatusEffectKind kind) => _statusDefinitions[kind];
        public long GetStatusStacks(CombatSide side, StatusEffectKind kind)
            => _statusStacks.TryGetValue((side, kind), out long stacks) ? stacks : 0;
        public double GetEvasionPercent(CombatSide side)
            => Math.Clamp(GetStatusDefinition(StatusEffectKind.Agility).PercentAt(GetStatusStacks(side, StatusEffectKind.Agility)), 0d, 70d);

        public void ApplyStatus(CombatSide side, StatusEffectKind kind, long stacks, CombatSide? source = null)
        {
            if (stacks < 0) throw new ArgumentOutOfRangeException(nameof(stacks));
            if (!Enum.IsDefined(typeof(CombatSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (!_statusDefinitions.ContainsKey(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (source.HasValue && !Enum.IsDefined(typeof(CombatSide), source.Value)) throw new ArgumentOutOfRangeException(nameof(source));
            if (Result != CombatResult.Running || stacks == 0) return;
            StatusEffectKind? opposite = GetStatusDefinition(kind).Opposite;
            long cancelled = opposite.HasValue ? Math.Min(stacks, GetStatusStacks(side, opposite.Value)) : 0;
            long total = checked(GetStatusStacks(side, kind) + (stacks - cancelled));
            if (opposite.HasValue) _statusStacks[(side, opposite.Value)] = GetStatusStacks(side, opposite.Value) - cancelled;
            _statusStacks[(side, kind)] = total;
            if (GetStatusDefinition(kind).AffectedStat == StatusStat.PeriodicDamage)
                AddPeriodicStacks(side, kind, source ?? TargetSide(side, StatusTarget.Opponent), stacks);
            switch (GetStatusDefinition(kind).AffectedStat)
            {
                case StatusStat.ActivationSpeed: RefreshItemSpeeds(side); break;
                case StatusStat.Accuracy:
                case StatusStat.CriticalChance: RefreshWeaponStatusModifiers(side); break;
            }
        }

        private void RefreshWeaponStatusModifiers(CombatSide side)
        {
            double accuracy = GetStatusDefinition(StatusEffectKind.Focus).PercentAt(GetStatusStacks(side, StatusEffectKind.Focus))
                - GetStatusDefinition(StatusEffectKind.Blindness).PercentAt(GetStatusStacks(side, StatusEffectKind.Blindness));
            double precision = GetStatusDefinition(StatusEffectKind.Precision).PercentAt(GetStatusStacks(side, StatusEffectKind.Precision));
            foreach (var weapon in side == CombatSide.Player ? PlayerWeapons : EnemyWeapons)
                weapon.Stats.SetStatusModifiers(accuracy, precision);
        }

        private double StatusSpeedPercent(CombatSide side)
            => GetStatusDefinition(StatusEffectKind.Haste).PercentAt(GetStatusStacks(side, StatusEffectKind.Haste))
                - GetStatusDefinition(StatusEffectKind.Freeze).PercentAt(GetStatusStacks(side, StatusEffectKind.Freeze));

        private void InitializeStatuses(IEnumerable<StatusEffectData> definitions)
        {
            foreach (StatusEffectKind kind in Enum.GetValues(typeof(StatusEffectKind)))
                _statusDefinitions.Add(kind, new(kind, StatusEffectRules.DisplayName(kind)));
            if (definitions != null)
                foreach (StatusEffectData data in definitions)
                    if (data != null) _statusDefinitions[data.Kind] = new(data.Kind, data.DisplayName, data.PercentPerStack,
                        data.DamageScaling, data.TickIntervalSeconds, data.StacksLostPerTick);
            ApplyBattleStartStatuses(Player.Loadout, CombatSide.Player);
            ApplyBattleStartStatuses(Enemy.Loadout, CombatSide.Enemy);
        }

        private void ApplyBattleStartStatuses(CombatLoadoutSnapshot loadout, CombatSide owner)
        {
            foreach (CombatItemSnapshot item in loadout.Items)
                foreach (StatusApplication status in item.BattleStartStatuses)
                    ApplyStatus(TargetSide(owner, status.Target), status.Kind, status.Stacks, owner);
        }

        private static CombatSide TargetSide(CombatSide owner, StatusTarget target)
            => target == StatusTarget.Self ? owner : owner == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
    }
}
