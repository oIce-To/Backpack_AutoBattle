using System;
using System.Collections.Generic;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    /// <summary>Immutable battle-local copy, never a live reference to an editable SO.</summary>
    public sealed class CombatStatusDefinition
    {
        public CombatStatusDefinition(StatusEffectKind kind, string name, float percent = 1f, int maximum = 0)
        { Kind = kind; DisplayName = name; PercentPerStack = percent; MaximumStacks = maximum; }
        public StatusEffectKind Kind { get; }
        public string DisplayName { get; }
        public float PercentPerStack { get; }
        public int MaximumStacks { get; }
        public bool IsDebuff => Kind == StatusEffectKind.Freeze;
        public double PercentAt(int stacks) => (double)stacks * PercentPerStack;
    }

    public sealed partial class CombatSimulation
    {
        private readonly Dictionary<StatusEffectKind, CombatStatusDefinition> _statusDefinitions = new();
        private readonly Dictionary<(CombatSide, StatusEffectKind), int> _statusStacks = new();
        public CombatStatusDefinition GetStatusDefinition(StatusEffectKind kind) => _statusDefinitions[kind];
        public int GetStatusStacks(CombatSide side, StatusEffectKind kind)
            => _statusStacks.TryGetValue((side, kind), out int stacks) ? stacks : 0;

        public void ApplyStatus(CombatSide side, StatusEffectKind kind, int stacks)
        {
            if (stacks < 0) throw new ArgumentOutOfRangeException(nameof(stacks));
            if (!Enum.IsDefined(typeof(CombatSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (!_statusDefinitions.ContainsKey(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (Result != CombatResult.Running || stacks == 0) return;
            StatusEffectKind opposite = kind == StatusEffectKind.Haste ? StatusEffectKind.Freeze : StatusEffectKind.Haste;
            int cancelled = Math.Min(stacks, GetStatusStacks(side, opposite));
            _statusStacks[(side, opposite)] = GetStatusStacks(side, opposite) - cancelled;
            long total = (long)GetStatusStacks(side, kind) + stacks - cancelled;
            int maximum = GetStatusDefinition(kind).MaximumStacks;
            _statusStacks[(side, kind)] = (int)Math.Min(total, maximum > 0 ? maximum : int.MaxValue);
            RefreshItemSpeeds(side);
        }

        private double StatusSpeedPercent(CombatSide side)
            => GetStatusDefinition(StatusEffectKind.Haste).PercentAt(GetStatusStacks(side, StatusEffectKind.Haste))
                - GetStatusDefinition(StatusEffectKind.Freeze).PercentAt(GetStatusStacks(side, StatusEffectKind.Freeze));

        private void InitializeStatuses(IEnumerable<StatusEffectData> definitions)
        {
            _statusDefinitions.Add(StatusEffectKind.Haste, new(StatusEffectKind.Haste, "신속"));
            _statusDefinitions.Add(StatusEffectKind.Freeze, new(StatusEffectKind.Freeze, "빙결"));
            if (definitions != null)
                foreach (StatusEffectData data in definitions)
                    if (data != null) _statusDefinitions[data.Kind] = new(data.Kind, data.DisplayName, data.PercentPerStack, data.MaximumStacks);
            ApplyBattleStartStatuses(Player.Loadout, CombatSide.Player);
            ApplyBattleStartStatuses(Enemy.Loadout, CombatSide.Enemy);
        }

        private void ApplyBattleStartStatuses(CombatLoadoutSnapshot loadout, CombatSide owner)
        {
            foreach (CombatItemSnapshot item in loadout.Items)
                foreach (StatusApplication status in item.BattleStartStatuses)
                    ApplyStatus(TargetSide(owner, status.Target), status.Kind, status.Stacks);
        }

        private static CombatSide TargetSide(CombatSide owner, StatusTarget target)
            => target == StatusTarget.Self ? owner : owner == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
    }
}
