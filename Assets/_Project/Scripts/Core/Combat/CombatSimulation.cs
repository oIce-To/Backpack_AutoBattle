using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public enum CombatResult { Running, Victory, Defeat, Draw }
    public enum CombatSide { Player, Enemy }

    public class CombatItem
    {
        public CombatItem(CombatItemSnapshot snapshot, Func<double> time)
        {
            Snapshot = snapshot;
            Activation = snapshot.Activation.Copy();
            Cycle = new RuntimeItemCycle(Activation.CooldownSeconds, time);
        }
        public CombatItemSnapshot Snapshot { get; }
        public string InstanceId => Snapshot.InstanceId;
        public string DisplayName => Snapshot.DisplayName;
        public ItemActivationData Activation { get; }
        public RuntimeItemCycle Cycle { get; }
    }

    /// <summary>Attack-specific stats on top of the same clock used by armor, pets and other items.</summary>
    public sealed class CombatWeapon : CombatItem
    {
        public CombatWeapon(CombatItemSnapshot snapshot, Func<double> time) : base(snapshot, time)
        { BaseStats = snapshot.BaseWeaponStats.Copy(); Stats = new RuntimeWeaponStats(BaseStats, Cycle); }
        public WeaponStats BaseStats { get; }
        public RuntimeWeaponStats Stats { get; }
        public double NextAttackAt => Cycle.NextActivationAt;
    }

    public sealed class CombatItemEvent
    {
        public CombatItemEvent(double time, CombatSide side, CombatItem item, ItemEffectKind kind,
            bool hit = true, bool critical = false, double damage = 0d, double armorAbsorbed = 0d,
            double healthDamage = 0d, double armorGained = 0d, StatusEffectKind statusKind = StatusEffectKind.Haste,
            int statusStacks = 0, CombatSide targetSide = CombatSide.Player)
        {
            Time = time; Side = side; InstanceId = item.InstanceId; DisplayName = item.DisplayName;
            Kind = kind; Hit = hit; Critical = critical; Damage = damage;
            ArmorAbsorbed = armorAbsorbed; HealthDamage = healthDamage; ArmorGained = armorGained;
            StatusKind = statusKind; StatusStacks = statusStacks; TargetSide = targetSide;
        }
        public double Time { get; }
        public CombatSide Side { get; }
        public string InstanceId { get; }
        public string DisplayName { get; }
        public ItemEffectKind Kind { get; }
        public bool Hit { get; }
        public bool Critical { get; }
        public double Damage { get; }
        public double ArmorAbsorbed { get; }
        public double HealthDamage { get; }
        public double ArmorGained { get; }
        public StatusEffectKind StatusKind { get; }
        public int StatusStacks { get; }
        public CombatSide TargetSide { get; }
    }

    /// <summary>Chronological, category-independent item activations; no UI or frame-rate dependencies.</summary>
    public sealed partial class CombatSimulation
    {
        private readonly Random _random;
        private readonly double _maximumDuration;
        private readonly Dictionary<(CombatSide side, string source), (float percent, ItemTargetFilter filter)> _speedModifiers = new();

        public CombatSimulation(CombatantSnapshot player, CombatantSnapshot enemy, Random random = null,
            double maximumDuration = 120d, IEnumerable<StatusEffectData> statusEffects = null)
        {
            if (maximumDuration <= 0d || double.IsNaN(maximumDuration) || double.IsInfinity(maximumDuration))
                throw new ArgumentOutOfRangeException(nameof(maximumDuration));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            PlayerMaxHealth = PlayerHealth = player.MaximumHealth;
            EnemyMaxHealth = EnemyHealth = enemy.MaximumHealth;
            PlayerItems = CreateItems(player.Loadout);
            EnemyItems = CreateItems(enemy.Loadout);
            // These are references into the common item lists, never separately ticked copies.
            PlayerWeapons = Array.AsReadOnly(PlayerItems.OfType<CombatWeapon>().ToArray());
            EnemyWeapons = Array.AsReadOnly(EnemyItems.OfType<CombatWeapon>().ToArray());
            _random = random ?? new Random();
            _maximumDuration = maximumDuration;
            InitializeStatuses(statusEffects);
        }

        public CombatantSnapshot Player { get; }
        public CombatantSnapshot Enemy { get; }
        public CombatLoadoutSnapshot Loadout => Player.Loadout;
        public IReadOnlyList<CombatItem> PlayerItems { get; }
        public IReadOnlyList<CombatItem> EnemyItems { get; }
        public IReadOnlyList<CombatWeapon> PlayerWeapons { get; }
        public IReadOnlyList<CombatWeapon> EnemyWeapons { get; }
        public double PlayerMaxHealth { get; }
        public double EnemyMaxHealth { get; }
        public double PlayerHealth { get; private set; }
        public double EnemyHealth { get; private set; }
        public double PlayerArmor { get; private set; }
        public double EnemyArmor { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public CombatResult Result { get; private set; } = CombatResult.Running;
        public IReadOnlyList<CombatItem> GetItems(CombatSide side) => side == CombatSide.Player ? PlayerItems : EnemyItems;

        /// <summary>+50 means 1.5x activation speed. Sources add; replacing/removing a source does not stack it twice.</summary>
        public void SetActivationSpeedModifier(CombatSide side, string sourceId, float percent, ItemTargetFilter filter = null)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A stable modifier source ID is required.", nameof(sourceId));
            if (float.IsNaN(percent) || float.IsInfinity(percent)) throw new ArgumentOutOfRangeException(nameof(percent));
            if (Result != CombatResult.Running) return;
            _speedModifiers[(side, sourceId)] = (percent, (filter ?? new ItemTargetFilter()).Copy());
            RefreshItemSpeeds(side);
        }

        public void RemoveActivationSpeedModifier(CombatSide side, string sourceId)
        {
            if (Result != CombatResult.Running || !_speedModifiers.Remove((side, sourceId))) return;
            RefreshItemSpeeds(side);
        }

        private void RefreshItemSpeeds(CombatSide side)
        {
            foreach (CombatItem item in GetItems(side))
            {
                double rate = 1d + StatusSpeedPercent(side) / 100d;
                foreach (var modifier in _speedModifiers)
                    if (modifier.Key.side == side && modifier.Value.filter.Matches(item.Snapshot.Category, item.Snapshot.Tags))
                        rate += modifier.Value.percent / 100d;
                item.Cycle.SetSpeedRate(rate);
            }
        }

        public IReadOnlyList<CombatItemEvent> Advance(double deltaSeconds)
        {
            if (deltaSeconds < 0d || double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (Result != CombatResult.Running || deltaSeconds == 0d) return Array.Empty<CombatItemEvent>();
            var events = new List<CombatItemEvent>();
            double targetTime = Math.Min(ElapsedSeconds + deltaSeconds, _maximumDuration);
            while (Result == CombatResult.Running)
            {
                CombatItem next = null;
                CombatSide side = CombatSide.Player;
                // Snapshot order is stable ID order. Player activations win exact time ties.
                foreach (CombatItem item in PlayerItems)
                    if (next == null || item.Cycle.NextActivationAt < next.Cycle.NextActivationAt) { next = item; side = CombatSide.Player; }
                foreach (CombatItem item in EnemyItems)
                    if (next == null || item.Cycle.NextActivationAt < next.Cycle.NextActivationAt) { next = item; side = CombatSide.Enemy; }
                if (next == null || next.Cycle.NextActivationAt > targetTime + 1e-9d) break;
                ElapsedSeconds = next.Cycle.NextActivationAt;
                // Snap sub-nanosecond partition error forward, never move time backward after firing.
                targetTime = Math.Max(targetTime, ElapsedSeconds);
                foreach (ItemActivationEffect effect in next.Activation.Effects)
                {
                    if (effect.Kind == ItemEffectKind.Attack) ResolveAttack((CombatWeapon)next, side, events);
                    else if (effect.Kind == ItemEffectKind.GainArmor)
                    {
                        if (side == CombatSide.Player) PlayerArmor += effect.Amount;
                        else EnemyArmor += effect.Amount;
                        events.Add(new CombatItemEvent(ElapsedSeconds, side, next, effect.Kind, armorGained: effect.Amount));
                    }
                    else if (effect.Kind == ItemEffectKind.ApplyStatus && effect.Status != null)
                    {
                        StatusApplication status = effect.Status;
                        CombatSide target = TargetSide(side, status.Target);
                        ApplyStatus(target, status.Kind, status.Stacks);
                        events.Add(new CombatItemEvent(ElapsedSeconds, side, next, effect.Kind,
                            statusKind: status.Kind, statusStacks: status.Stacks, targetSide: target));
                    }
                    if (Result != CombatResult.Running) break;
                }
                next.Cycle.CompleteActivation(ElapsedSeconds);
            }
            if (Result == CombatResult.Running)
            {
                ElapsedSeconds = targetTime;
                if (targetTime >= _maximumDuration) Result = CombatResult.Draw;
            }
            return events;
        }

        private IReadOnlyList<CombatItem> CreateItems(CombatLoadoutSnapshot loadout)
            => Array.AsReadOnly(loadout.Items.Where(item => item.Activation.HasCycle).Select(item =>
                item.Activation.HasEffect(ItemEffectKind.Attack) ? new CombatWeapon(item, () => ElapsedSeconds)
                    : new CombatItem(item, () => ElapsedSeconds)).ToArray());

        private void ResolveAttack(CombatWeapon weapon, CombatSide side, ICollection<CombatItemEvent> events)
        {
            bool hit = _random.NextDouble() * 100d < weapon.Stats.AccuracyPercent;
            bool critical = hit && _random.NextDouble() * 100d < weapon.Stats.CriticalChancePercent;
            double damage = hit ? weapon.Stats.AttackDamage * (critical ? weapon.Stats.CriticalDamagePercent / 100d : 1d) : 0d;
            bool hitsEnemy = side == CombatSide.Player;
            DamageResolution result = DamageResolver.Resolve(damage, hitsEnemy ? EnemyArmor : PlayerArmor,
                hitsEnemy ? EnemyHealth : PlayerHealth);
            if (hitsEnemy) { EnemyArmor = result.ArmorRemaining; EnemyHealth = result.HealthRemaining; }
            else { PlayerArmor = result.ArmorRemaining; PlayerHealth = result.HealthRemaining; }
            events.Add(new CombatItemEvent(ElapsedSeconds, side, weapon, ItemEffectKind.Attack, hit, critical,
                damage, result.ArmorAbsorbed, result.HealthDamage));
            if (EnemyHealth <= 0d) Result = CombatResult.Victory;
            else if (PlayerHealth <= 0d) Result = CombatResult.Defeat;
        }
    }
}
