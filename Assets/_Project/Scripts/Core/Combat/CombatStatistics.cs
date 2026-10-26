using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed class CombatItemStatistics
    {
        internal CombatItemStatistics(CombatSide side, string instanceId, string name, double health, double armor,
            double healing, double blocked, double direct, double status, double other, int column, int row,
            double burn = 0, double poison = 0, double thorns = 0, double reduced = 0)
        {
            Side = side; InstanceId = instanceId; DisplayName = name; HealthDamage = health; ArmorDamage = armor;
            Healing = healing; DamageBlocked = blocked; DirectDamage = direct; StatusDamage = status; OtherDamage = other;
            Column = column; Row = row;
            BurnDamage = burn; PoisonDamage = poison; ThornsDamage = thorns; DamageReduced = reduced;
        }
        public CombatSide Side { get; }
        public string InstanceId { get; }
        public string DisplayName { get; }
        public int Column { get; }
        public int Row { get; }
        public double HealthDamage { get; }
        public double ArmorDamage { get; }
        public double DamageDealt => HealthDamage + ArmorDamage;
        public double Healing { get; }
        public double DamageBlocked { get; }
        public double DirectDamage { get; }
        public double StatusDamage { get; }
        public double OtherDamage { get; }
        public double BurnDamage { get; }
        public double PoisonDamage { get; }
        public double ThornsDamage { get; }
        public double DamageReduced { get; }
        public double DefenseTotal => Healing + DamageBlocked + DamageReduced;
    }

    /// <summary>Detached, immutable per-battle results. Reading/reopening the UI never aggregates again.</summary>
    public sealed class CombatStatisticsSnapshot
    {
        internal CombatStatisticsSnapshot(double seconds, CombatResult result, string player, string enemy,
            IEnumerable<CombatItemStatistics> items)
        { Seconds = seconds; Result = result; PlayerName = player; EnemyName = enemy; Items = Array.AsReadOnly(items.ToArray()); }
        public double Seconds { get; }
        public CombatResult Result { get; }
        public string PlayerName { get; }
        public string EnemyName { get; }
        public IReadOnlyList<CombatItemStatistics> Items { get; }
    }

    public sealed partial class CombatSimulation
    {
        private sealed class ItemCounter
        {
            public CombatSide Side;
            public string Id, Name;
            public int Column, Row;
            public double Health, Armor, Healing, Blocked, Direct, Status, Other;
            public double Burn, Poison, Thorns, Reduced;
            public CombatItemStatistics Capture() => new(Side, Id, Name, Health, Armor, Healing, Blocked, Direct, Status, Other, Column, Row,
                Burn, Poison, Thorns, Reduced);
        }
        private sealed class StatusOrigin
        {
            public CombatSide Source;
            public string ItemId;
            public long Stacks;
        }
        private sealed class ArmorOrigin
        {
            public string ItemId;
            public double Remaining;
        }
        private readonly struct CreditWeight
        {
            public readonly CombatSide Side;
            public readonly string ItemId;
            public readonly double Weight;
            public CreditWeight(CombatSide side, string itemId, double weight) { Side = side; ItemId = itemId; Weight = weight; }
        }
        private readonly Dictionary<(CombatSide, string), ItemCounter> _itemCounters = new();
        private readonly Dictionary<(CombatSide, StatusEffectKind), List<StatusOrigin>> _statusOrigins = new();
        private readonly Dictionary<CombatSide, List<ArmorOrigin>> _armorOrigins = new()
        { [CombatSide.Player] = new(), [CombatSide.Enemy] = new() };

        private void InitializeStatistics()
        {
            foreach (CombatSide side in Enum.GetValues(typeof(CombatSide)))
                foreach (CombatItemSnapshot item in (side == CombatSide.Player ? Player : Enemy).Loadout.Items)
                    _itemCounters.TryAdd((side, item.InstanceId), new ItemCounter { Side = side, Id = item.InstanceId, Name = item.DisplayName,
                        Column = item.Origin.x + 1, Row = item.Origin.y + 1 });
        }

        public CombatStatisticsSnapshot CaptureStatistics() => new(ElapsedSeconds, Result, Player.DisplayName, Enemy.DisplayName,
            _itemCounters.Values.OrderBy(c => c.Side).ThenBy(c => c.Id == null).ThenBy(c => c.Id, StringComparer.Ordinal).Select(c => c.Capture()));

        private ItemCounter Counter(CombatSide side, string id)
        {
            if (_itemCounters.TryGetValue((side, id), out var counter)) return counter;
            // Explicit/debug/non-item effects have a visible bucket, never a guessed item owner.
            if (!_itemCounters.TryGetValue((side, null), out counter))
                _itemCounters[(side, null)] = counter = new ItemCounter { Side = side, Name = "기타 효과" };
            return counter;
        }

        private static void SplitCredit(double amount, IReadOnlyList<CreditWeight> weights, CombatSide fallback, string itemId,
            Action<CombatSide, string, double> record)
        {
            if (amount <= 0) return;
            var positive = weights?.Where(w => w.Weight > 0).ToArray();
            if (positive == null || positive.Length == 0) { record(fallback, itemId, amount); return; }
            double total = positive.Sum(w => w.Weight), remaining = amount;
            for (int i = 0; i < positive.Length; i++)
            {
                // Give only the floating-point remainder to the final source; never round per event.
                double share = i == positive.Length - 1 ? remaining : Math.Min(remaining, amount * (positive[i].Weight / total));
                remaining -= share; record(positive[i].Side, positive[i].ItemId, share);
            }
        }

        private void CreditDamage(CombatSide source, string itemId, CombatDamageKind kind, DamageResolution damage,
            IReadOnlyList<CreditWeight> weights, StatusEffectKind? cause)
        {
            void Add(CombatSide side, string id, double amount, bool armor)
            {
                var c = Counter(side, id);
                if (armor) c.Armor += amount; else c.Health += amount;
                if (kind == CombatDamageKind.AttackItem) c.Direct += amount;
                else if (kind == CombatDamageKind.StatusEffect) c.Status += amount;
                else c.Other += amount;
                if (cause == StatusEffectKind.Burn) c.Burn += amount;
                else if (cause == StatusEffectKind.Poison) c.Poison += amount;
                else if (cause == StatusEffectKind.Thorns) c.Thorns += amount;
            }
            SplitCredit(damage.HealthDamage, weights, source, itemId, (side, id, amount) => Add(side, id, amount, false));
            SplitCredit(damage.ArmorAbsorbed, weights, source, itemId, (side, id, amount) => Add(side, id, amount, true));
        }

        private void CreditHealing(CombatSide owner, double healed, IReadOnlyList<CreditWeight> weights)
            => SplitCredit(healed, weights, owner, null, (side, id, amount) => Counter(side, id).Healing += amount);

        /// <summary>Statistics hook for actual damage already prevented by a future reduction effect.
        /// This does not itself reduce incoming damage or create a new item effect.</summary>
        public void RecordDamageReduction(CombatSide source, string sourceItemId, double preventedDamage)
        {
            if (!Enum.IsDefined(typeof(CombatSide), source)) throw new ArgumentOutOfRangeException(nameof(source));
            if (!double.IsFinite(preventedDamage) || preventedDamage < 0) throw new ArgumentOutOfRangeException(nameof(preventedDamage));
            if (Result == CombatResult.Running && preventedDamage > 0) Counter(source, sourceItemId).Reduced += preventedDamage;
        }

        private void AddArmorOrigin(CombatSide side, string itemId, double amount)
        {
            if (amount <= 0) return;
            var list = _armorOrigins[side]; var origin = list.FirstOrDefault(o => o.ItemId == itemId);
            if (origin == null) { origin = new ArmorOrigin { ItemId = itemId }; list.Add(origin); }
            origin.Remaining += amount;
        }

        private void CreditAbsorption(CombatSide target, double absorbed)
        {
            var origins = _armorOrigins[target];
            var weights = origins.Where(o => o.Remaining > 0).Select(o => new CreditWeight(target, o.ItemId, o.Remaining)).ToArray();
            SplitCredit(absorbed, weights, target, null, (side, id, amount) =>
            {
                Counter(side, id).Blocked += amount;
                var origin = origins.FirstOrDefault(o => o.ItemId == id);
                if (origin != null) origin.Remaining = Math.Max(0, origin.Remaining - amount);
            });
            origins.RemoveAll(o => o.Remaining <= 0);
        }

        private void AddStatusOrigin(CombatSide target, StatusEffectKind kind, CombatSide source, string itemId, long stacks)
        {
            if (stacks == 0) return;
            if (!_statusOrigins.TryGetValue((target, kind), out var list)) _statusOrigins[(target, kind)] = list = new();
            var origin = list.FirstOrDefault(o => o.Source == source && o.ItemId == itemId);
            if (origin == null) { origin = new StatusOrigin { Source = source, ItemId = itemId }; list.Add(origin); }
            origin.Stacks = checked(origin.Stacks + stacks);
        }

        private void RemoveStatusOrigins(CombatSide target, StatusEffectKind kind, long stacks, CombatSide? source = null)
        {
            if (!_statusOrigins.TryGetValue((target, kind), out var list)) return;
            foreach (var origin in list)
            {
                if (source.HasValue && origin.Source != source.Value) continue;
                long take = Math.Min(stacks, origin.Stacks); origin.Stacks -= take; stacks -= take;
                if (stacks == 0) break;
            }
            list.RemoveAll(o => o.Stacks == 0);
        }

        private IReadOnlyList<CreditWeight> StatusWeights(CombatSide target, StatusEffectKind kind, CombatSide? source = null)
            => _statusOrigins.TryGetValue((target, kind), out var list)
                ? list.Where(o => !source.HasValue || o.Source == source.Value).Select(o => new CreditWeight(o.Source, o.ItemId, o.Stacks)).ToArray()
                : Array.Empty<CreditWeight>();
    }
}
