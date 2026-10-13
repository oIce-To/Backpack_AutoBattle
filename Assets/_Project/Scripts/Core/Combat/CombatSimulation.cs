using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public enum CombatResult { Running, Victory, Defeat, Draw }
    public enum CombatSide { Player, Enemy }

    public sealed class CombatWeapon
    {
        public CombatWeapon(string instanceId, string displayName, WeaponStats baseStats)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            BaseStats = baseStats.Copy();
            Stats = new RuntimeWeaponStats(BaseStats);
            NextAttackAt = Stats.CooldownSeconds;
        }

        public string InstanceId { get; }
        public string DisplayName { get; }
        public WeaponStats BaseStats { get; }
        public RuntimeWeaponStats Stats { get; }
        public double NextAttackAt { get; internal set; }
    }

    public sealed class CombatAttackEvent
    {
        public CombatAttackEvent(double time, CombatSide side, string instanceId,
            string displayName, bool hit, bool critical, double damage)
        {
            Time = time;
            Side = side;
            InstanceId = instanceId;
            DisplayName = displayName;
            Hit = hit;
            Critical = critical;
            Damage = damage;
        }

        public double Time { get; }
        public CombatSide Side { get; }
        public string InstanceId { get; }
        public string DisplayName { get; }
        public bool Hit { get; }
        public bool Critical { get; }
        public double Damage { get; }
    }

    /// <summary>Chronological combat rules, independent of UI and frame rate.</summary>
    public sealed class CombatSimulation
    {
        private readonly Random _random;
        private readonly double _maximumDuration;

        public CombatSimulation(CombatantSnapshot player, CombatantSnapshot enemy, Random random = null,
            double maximumDuration = 120d)
        {
            if (maximumDuration <= 0d || double.IsNaN(maximumDuration) || double.IsInfinity(maximumDuration))
                throw new ArgumentOutOfRangeException(nameof(maximumDuration));

            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            PlayerMaxHealth = PlayerHealth = player.MaximumHealth;
            EnemyMaxHealth = EnemyHealth = enemy.MaximumHealth;
            PlayerWeapons = CreateWeapons(player.Loadout);
            EnemyWeapons = CreateWeapons(enemy.Loadout);
            _random = random ?? new Random();
            _maximumDuration = maximumDuration;
        }

        public CombatantSnapshot Player { get; }
        public CombatantSnapshot Enemy { get; }
        public CombatLoadoutSnapshot Loadout => Player.Loadout;
        public IReadOnlyList<CombatWeapon> PlayerWeapons { get; }
        public IReadOnlyList<CombatWeapon> EnemyWeapons { get; }
        public double PlayerMaxHealth { get; }
        public double EnemyMaxHealth { get; }
        public double PlayerHealth { get; private set; }
        public double EnemyHealth { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public CombatResult Result { get; private set; } = CombatResult.Running;

        public IReadOnlyList<CombatAttackEvent> Advance(double deltaSeconds)
        {
            if (deltaSeconds < 0d || double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (Result != CombatResult.Running || deltaSeconds == 0d)
                return Array.Empty<CombatAttackEvent>();

            var events = new List<CombatAttackEvent>();
            double targetTime = Math.Min(ElapsedSeconds + deltaSeconds, _maximumDuration);
            while (Result == CombatResult.Running)
            {
                CombatWeapon nextWeapon = null;
                CombatSide side = CombatSide.Player;
                // Stable player-first ordering for exactly simultaneous attacks.
                foreach (CombatWeapon weapon in PlayerWeapons)
                {
                    if (nextWeapon == null || weapon.NextAttackAt < nextWeapon.NextAttackAt)
                    {
                        nextWeapon = weapon;
                        side = CombatSide.Player;
                    }
                }

                foreach (CombatWeapon weapon in EnemyWeapons)
                {
                    if (nextWeapon == null || weapon.NextAttackAt < nextWeapon.NextAttackAt)
                    {
                        nextWeapon = weapon;
                        side = CombatSide.Enemy;
                    }
                }

                if (nextWeapon == null || nextWeapon.NextAttackAt > targetTime)
                    break;

                ElapsedSeconds = nextWeapon.NextAttackAt;
                ResolveAttack(nextWeapon, side, events);
                nextWeapon.NextAttackAt += nextWeapon.Stats.CooldownSeconds;
            }

            if (Result == CombatResult.Running)
            {
                ElapsedSeconds = targetTime;
                if (targetTime >= _maximumDuration)
                    Result = CombatResult.Draw;
            }

            return events;
        }

        private static IReadOnlyList<CombatWeapon> CreateWeapons(CombatLoadoutSnapshot loadout)
        {
            return Array.AsReadOnly(loadout.Items.Where(item => item.Category == ItemCategory.Weapon)
                .Select(item => new CombatWeapon(item.InstanceId, item.DisplayName, item.BaseWeaponStats)).ToArray());
        }

        private void ResolveAttack(CombatWeapon weapon, CombatSide side, ICollection<CombatAttackEvent> events)
        {
            bool hit = _random.NextDouble() * 100d < weapon.Stats.AccuracyPercent;
            bool critical = hit && _random.NextDouble() * 100d < weapon.Stats.CriticalChancePercent;
            double damage = hit ? weapon.Stats.AttackDamage
                * (critical ? weapon.Stats.CriticalDamagePercent / 100d : 1d) : 0d;

            if (side == CombatSide.Player)
                EnemyHealth = Math.Max(0d, EnemyHealth - damage);
            else
                PlayerHealth = Math.Max(0d, PlayerHealth - damage);

            events.Add(new CombatAttackEvent(ElapsedSeconds, side, weapon.InstanceId,
                weapon.DisplayName, hit, critical, damage));

            if (EnemyHealth <= 0d)
                Result = CombatResult.Victory;
            else if (PlayerHealth <= 0d)
                Result = CombatResult.Defeat;
        }
    }
}
