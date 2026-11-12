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
            if (snapshot.GearEffect.HasFixedCycle) FixedCycle = new RuntimeItemCycle(UniqueGearEffect.FixedCooldownSeconds, time);
        }
        public CombatItemSnapshot Snapshot { get; }
        public string InstanceId => Snapshot.InstanceId;
        public string DisplayName => Snapshot.DisplayName;
        public ItemActivationData Activation { get; }
        public RuntimeItemCycle Cycle { get; }
        public RuntimeItemCycle FixedCycle { get; }
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
            long statusStacks = 0, CombatSide targetSide = CombatSide.Player, AttackOutcome? attackOutcome = null, double healthRestored = 0d)
        {
            Time = time; Side = side; InstanceId = item.InstanceId; DisplayName = item.DisplayName;
            Kind = kind; Hit = hit; Critical = critical; Damage = damage;
            ArmorAbsorbed = armorAbsorbed; HealthDamage = healthDamage; ArmorGained = armorGained;
            StatusKind = statusKind; StatusStacks = statusStacks; TargetSide = targetSide;
            HealthRestored = healthRestored;
            AttackOutcome = kind == ItemEffectKind.Attack
                ? attackOutcome ?? (hit ? global::BackpackAutoBattle.Combat.AttackOutcome.Hit : global::BackpackAutoBattle.Combat.AttackOutcome.AccuracyMiss)
                : global::BackpackAutoBattle.Combat.AttackOutcome.NotAttack;
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
        public long StatusStacks { get; }
        public double HealthRestored { get; }
        public CombatSide TargetSide { get; }
        public AttackOutcome AttackOutcome { get; }
        public bool TriggersAvoidance => AttackOutcome == global::BackpackAutoBattle.Combat.AttackOutcome.AccuracyMiss
            || AttackOutcome == global::BackpackAutoBattle.Combat.AttackOutcome.Evaded;
    }

    /// <summary>Chronological, category-independent item activations; no UI or frame-rate dependencies.</summary>
    public sealed partial class CombatSimulation
    {
        public const double EnrageStartsAt = 60d;
        public const double EnrageIntervalSeconds = 3d;
        public const double ForcedDefeatAt = 90d;
        public long EnrageStacks => ElapsedSeconds < EnrageStartsAt ? 0
            : Math.Min(10, 1 + (long)Math.Floor((ElapsedSeconds - EnrageStartsAt) / EnrageIntervalSeconds));
        public double EnrageDamageIncreasePercent => EnrageStacks * 10d;
        public bool IsForcedDefeat { get; private set; }

        // This battle rule applies once to every incoming damage kind, before armor and health.
        public double GetIncomingDamageMultiplier(CombatSide target)
            => target == CombatSide.Player ? 1d + EnrageDamageIncreasePercent / 100d : 1d;
        private readonly Random _random;
        private readonly double _maximumDuration;
        private readonly Dictionary<(CombatSide side, string source), (float percent, ItemTargetFilter filter)> _speedModifiers = new();
        private readonly Dictionary<CombatSide, long> _avoidanceCounts = new();
        public event Action<CombatItemEvent> AttackAvoided;
        public event Action<CombatDamageEvent> DamageApplied;
        public event Action<CombatItemEvent> ItemActivated;
        public long GetAvoidanceCount(CombatSide side) => _avoidanceCounts.TryGetValue(side, out long count) ? count : 0;

        public CombatSimulation(CombatantSnapshot player, CombatantSnapshot enemy, Random random = null,
            double maximumDuration = double.PositiveInfinity, IEnumerable<StatusEffectData> statusEffects = null)
        {
            if (maximumDuration <= 0d || double.IsNaN(maximumDuration))
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
            ApplyPlacementBonuses(Player.Loadout, PlayerWeapons);
            ApplyPlacementBonuses(Enemy.Loadout, EnemyWeapons);
            _random = random ?? new Random();
            _maximumDuration = maximumDuration;
            InitializeStatistics();
            InitializeUniqueGear(CombatSide.Player, player.Loadout);
            InitializeUniqueGear(CombatSide.Enemy, enemy.Loadout);
            InitializeStatuses(statusEffects);
            InitializeCharacterPassives();
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

        private static void ApplyPlacementBonuses(CombatLoadoutSnapshot loadout, IReadOnlyList<CombatWeapon> weapons)
        {
            IReadOnlyDictionary<string, float> bonuses = PlacementEffectResolver.GetAttackBonuses(loadout);
            foreach (CombatWeapon weapon in weapons)
                if (bonuses.TryGetValue(weapon.InstanceId, out float bonus)) weapon.Stats.AttackDamage += bonus;
        }

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
            double statusRate = 1d + StatusSpeedPercent(side) / 100d;
            foreach (CombatItem item in GetItems(side))
            {
                double rate = statusRate;
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
            double targetTime = Math.Min(ElapsedSeconds + deltaSeconds, Math.Min(_maximumDuration, ForcedDefeatAt));
            while (Result == CombatResult.Running)
            {
                CombatItem next = null;
                RuntimeItemCycle nextCycle = null;
                CombatSide side = CombatSide.Player;
                // Snapshot order is stable ID order. Player activations win exact time ties.
                foreach (CombatItem item in PlayerItems)
                    Consider(item, CombatSide.Player);
                foreach (CombatItem item in EnemyItems)
                    Consider(item, CombatSide.Enemy);
                void Consider(CombatItem item, CombatSide owner)
                {
                    RuntimeItemCycle cycle = item.Activation.HasCycle ? item.Cycle : null;
                    if (item.FixedCycle != null && (cycle == null || item.FixedCycle.NextActivationAt < cycle.NextActivationAt))
                        cycle = item.FixedCycle;
                    if (cycle == null || (nextCycle != null && cycle.NextActivationAt >= nextCycle.NextActivationAt)) return;
                    next = item; nextCycle = cycle; side = owner;
                }
                PeriodicState periodic = NextPeriodicTick();
                // Existing item ordering wins exact ties; status ties use target side, then status kind.
                if (periodic != null && (nextCycle == null || periodic.NextTickAt < nextCycle.NextActivationAt))
                {
                    if (periodic.NextTickAt >= ForcedDefeatAt || periodic.NextTickAt > targetTime + 1e-9d) break;
                    ElapsedSeconds = periodic.NextTickAt;
                    targetTime = Math.Max(targetTime, ElapsedSeconds);
                    ResolvePeriodicTick(periodic, events);
                    continue;
                }
                if (nextCycle == null || nextCycle.NextActivationAt >= ForcedDefeatAt || nextCycle.NextActivationAt > targetTime + 1e-9d) break;
                ElapsedSeconds = nextCycle.NextActivationAt;
                // Snap sub-nanosecond partition error forward, never move time backward after firing.
                targetTime = Math.Max(targetTime, ElapsedSeconds);
                if (nextCycle == next.FixedCycle)
                    ResolveFixedGearActivation(next, side, events);
                else foreach (ItemActivationEffect effect in next.Activation.Effects)
                {
                    if (effect.Trigger != ItemEffectTrigger.Activation) continue;
                    if (effect.Kind == ItemEffectKind.Attack) ResolveAttack((CombatWeapon)next, side, events);
                    else ResolveNonAttackEffect(next, side, effect, events);
                    if (Result != CombatResult.Running) break;
                }
                nextCycle.CompleteActivation(ElapsedSeconds);
            }
            if (Result == CombatResult.Running)
            {
                ElapsedSeconds = targetTime;
                if (targetTime >= ForcedDefeatAt) { IsForcedDefeat = true; Result = CombatResult.Defeat; }
                else if (targetTime >= _maximumDuration) Result = CombatResult.Draw;
            }
            return events;
        }

        private IReadOnlyList<CombatItem> CreateItems(CombatLoadoutSnapshot loadout)
            => Array.AsReadOnly(loadout.Items.Where(item => item.Activation.HasCycle || item.GearEffect.Kind != UniqueGearEffectKind.None).Select(item =>
                item.Activation.HasCycle && item.Activation.HasEffect(ItemEffectKind.Attack) ? new CombatWeapon(item, () => ElapsedSeconds)
                    : new CombatItem(item, () => ElapsedSeconds)).ToArray());

        private void ResolveAttack(CombatWeapon weapon, CombatSide side, ICollection<CombatItemEvent> events)
        {
            CombatSide target = side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
            AttackResolution attack = AttackResolver.Resolve(weapon.Stats.AccuracyPercent, GetEvasionPercent(target),
                weapon.Stats.CriticalChancePercent, _random);
            bool hit = attack.Hit, critical = attack.Critical;
            double damage = hit ? weapon.Stats.AttackDamage * (critical ? weapon.Stats.CriticalDamagePercent / 100d : 1d) : 0d;
            DamageResolution result = hit ? DealDamage(side, target, damage, CombatDamageKind.AttackItem, weapon.InstanceId)
                : DamageResolver.Resolve(0, target == CombatSide.Player ? PlayerArmor : EnemyArmor, target == CombatSide.Player ? PlayerHealth : EnemyHealth);
            damage *= GetIncomingDamageMultiplier(target);
            var itemEvent = new CombatItemEvent(ElapsedSeconds, side, weapon, ItemEffectKind.Attack, hit, critical,
                damage, result.ArmorAbsorbed, result.HealthDamage, targetSide: target, attackOutcome: attack.Outcome);
            RecordItemEvent(events, itemEvent);
            ApplyAttackPassive(weapon, side);
            if (itemEvent.TriggersAvoidance)
            {
                _avoidanceCounts[target] = checked(GetAvoidanceCount(target) + 1);
                AttackAvoided?.Invoke(itemEvent);
            }
            // Bind to this item's resolved hit, not an independent timer or another weapon's attack.
            // A blocked hit still qualifies; a miss, evade, status tick or dead target does not.
            if (hit && (target == CombatSide.Player ? PlayerHealth : EnemyHealth) > 0d && Result == CombatResult.Running)
                foreach (ItemActivationEffect effect in weapon.Activation.Effects)
                    if (effect.Trigger == ItemEffectTrigger.OnHit) ResolveNonAttackEffect(weapon, side, effect, events);
            if (hit)
            {
                if (weapon.Snapshot.Category == ItemCategory.Weapon && (weapon.Snapshot.Tags & ItemTags.Melee) != 0)
                    foreach (CombatItem source in GetItems(side))
                        if (source.Snapshot.GearEffect.Kind == UniqueGearEffectKind.MeleeHitArmor)
                            GainArmor(source, side, (side == CombatSide.Player ? Player : Enemy).Stats.Attack * source.Snapshot.GearEffect.Amount, events);
                ResolveDamageReactions(new CombatDamageEvent(side, target, CombatDamageKind.AttackItem, damage, result, weapon.InstanceId), events);
                ResolveGearDamageReceived(side, target, result, events);
            }
            ResolveGearAfterAttack(weapon, side, target, events);
            UpdateCombatResult(side);
        }

        private void ResolveNonAttackEffect(CombatItem item, CombatSide side, ItemActivationEffect effect, ICollection<CombatItemEvent> events)
        {
            if (effect.Kind == ItemEffectKind.Attack) throw new InvalidOperationException("명중 연계 효과에서 공격 효과를 재귀 실행할 수 없습니다.");
            if (effect.Kind == ItemEffectKind.GainArmor)
            {
                GainArmor(item, side, effect.Amount, events);
            }
            else if (effect.Kind == ItemEffectKind.ApplyStatus && effect.Status != null)
            {
                StatusApplication status = effect.Status;
                CombatSide target = TargetSide(side, status.Target);
                ApplyItemStatus(item, side, target, status.Kind, status.Stacks, events);
            }
            else if (effect.Kind == ItemEffectKind.UniqueGear) ResolveGearActivation(item, side, events);
        }

        private void GainArmor(CombatItem source, CombatSide side, double amount, ICollection<CombatItemEvent> events)
        {
            amount = ApplyArmorBonus(side, amount);
            AddArmor(side, source.InstanceId, amount);
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, side, source, ItemEffectKind.GainArmor, armorGained: amount));
        }

        private double ApplyArmorBonus(CombatSide side, double amount) => amount * OwnerStats(side).ArmorGainMultiplier;

        private void AddArmor(CombatSide side, string sourceId, double amount)
        {
            AddArmorOrigin(side, sourceId, amount);
            if (side == CombatSide.Player) PlayerArmor += amount;
            else EnemyArmor += amount;
        }

        /// <summary>Applies a resolved non-attack amount without attack rolls or avoidance events.
        /// Scheduled statuses resolve any status-specific modifiers in their own tick path.</summary>
        public DamageResolution ApplyNonAttackDamage(CombatSide source, CombatSide target, double damage, CombatDamageKind kind, string sourceItemId = null)
        {
            if (kind == CombatDamageKind.AttackItem || !Enum.IsDefined(typeof(CombatDamageKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(CombatSide), source) || !Enum.IsDefined(typeof(CombatSide), target))
                throw new ArgumentOutOfRangeException(nameof(target));
            if (Result != CombatResult.Running)
                return DamageResolver.Resolve(0, target == CombatSide.Player ? PlayerArmor : EnemyArmor, target == CombatSide.Player ? PlayerHealth : EnemyHealth);
            var result = DealDamage(source, target, damage, kind, sourceItemId);
            UpdateCombatResult();
            return result;
        }

        private DamageResolution DealDamage(CombatSide source, CombatSide target, double damage, CombatDamageKind kind, string itemId,
            IReadOnlyList<CreditWeight> credits = null, StatusEffectKind? cause = null, ICollection<CombatItemEvent> events = null)
        {
            damage *= GetIncomingDamageMultiplier(target);
            DamageResolution result = DamageResolver.Resolve(damage, target == CombatSide.Player ? PlayerArmor : EnemyArmor,
                target == CombatSide.Player ? PlayerHealth : EnemyHealth);
            if (target == CombatSide.Player) { PlayerArmor = result.ArmorRemaining; PlayerHealth = result.HealthRemaining; }
            else { EnemyArmor = result.ArmorRemaining; EnemyHealth = result.HealthRemaining; }
            CreditDamage(source, itemId, kind, result, credits, cause);
            CreditAbsorption(target, result.ArmorAbsorbed);
            DamageApplied?.Invoke(new CombatDamageEvent(source, target, kind, damage, result, itemId));
            if (kind != CombatDamageKind.AttackItem) ResolveGearDamageReceived(source, target, result, events);
            return result;
        }

        private void RecordItemEvent(ICollection<CombatItemEvent> events, CombatItemEvent itemEvent)
        { events?.Add(itemEvent); ItemActivated?.Invoke(itemEvent); }

        private void UpdateCombatResult(CombatSide? attackingSide = null)
        {
            // Finish the entire attack/reaction transaction before resolving a lethal trade.
            if (PlayerHealth <= 0d && EnemyHealth <= 0d && attackingSide.HasValue)
                Result = attackingSide == CombatSide.Player ? CombatResult.Victory : CombatResult.Defeat;
            else if (EnemyHealth <= 0d) Result = CombatResult.Victory;
            else if (PlayerHealth <= 0d) Result = CombatResult.Defeat;
        }
    }
}
