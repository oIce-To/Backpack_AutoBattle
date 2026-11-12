using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed partial class CombatSimulation
    {
        private readonly Dictionary<(CombatSide, StatusEffectKind), double> _gearStatusBonuses = new();
        private readonly Dictionary<CombatItem, HashSet<string>> _gearAttackTargets = new();

        private void InitializeUniqueGear(CombatSide side, CombatLoadoutSnapshot loadout)
        {
            foreach (CombatItem item in GetItems(side))
            {
                UniqueGearEffect effect = item.Snapshot.GearEffect;
                StatusEffectKind? enhanced = effect.Kind switch
                {
                    UniqueGearEffectKind.SearingTwilight => StatusEffectKind.Burn,
                    UniqueGearEffectKind.SylphVest => StatusEffectKind.Haste,
                    UniqueGearEffectKind.ThornShellPauldron => StatusEffectKind.Thorns,
                    UniqueGearEffectKind.FrostDawnFragment => StatusEffectKind.Freeze,
                    UniqueGearEffectKind.WhiteNightLantern => StatusEffectKind.Poison,
                    _ => null
                };
                if (enhanced.HasValue)
                {
                    var key = (side, enhanced.Value);
                    _gearStatusBonuses.TryGetValue(key, out double bonus);
                    _gearStatusBonuses[key] = bonus + effect.Amount;
                }
                if (effect.Kind is not (UniqueGearEffectKind.SearingTwilight or UniqueGearEffectKind.FrostDawnFragment
                    or UniqueGearEffectKind.StoneGuardianIdol or UniqueGearEffectKind.ExecutionersSpikedBall)) continue;

                var weapons = PlacementEffectResolver.GetCells(loadout, item.Snapshot)
                    .Where(cell => cell.ReceivesEffect && item.Snapshot.PlacementEffects[cell.EffectIndex].TriggersGearEffect)
                    .Select(cell => cell.TargetInstanceId).ToHashSet();
                if (effect.Kind is UniqueGearEffectKind.SearingTwilight or UniqueGearEffectKind.FrostDawnFragment)
                    weapons.Add(item.InstanceId);
                _gearAttackTargets.Add(item, weapons);
            }
            CombatSide opponent = TargetSide(side, StatusTarget.Opponent);
            foreach (CombatWeapon weapon in GetItems(side).OfType<CombatWeapon>())
            {
                var sources = GetItems(side).Where(source => _gearAttackTargets.TryGetValue(source, out var targets)
                    && targets.Contains(weapon.InstanceId)).ToArray();
                int frost = sources.Count(source => source.Snapshot.GearEffect.Kind == UniqueGearEffectKind.FrostDawnFragment);
                if (frost > 0)
                {
                    weapon.Stats.PlacementCriticalChanceBonus = () => frost * UniqueGearEffect.FreezeCriticalChancePerStack * GetStatusStacks(opponent, StatusEffectKind.Freeze);
                    weapon.Stats.PlacementCriticalDamageBonus = () => frost * UniqueGearEffect.FreezeCriticalDamagePerStack * GetStatusStacks(opponent, StatusEffectKind.Freeze);
                }
                double armorRatio = sources.Where(source => source.Snapshot.GearEffect.Kind == UniqueGearEffectKind.StoneGuardianIdol)
                    .Sum(source => (double)source.Snapshot.GearEffect.Amount);
                if (armorRatio > 0)
                    weapon.Stats.PlacementAttackBonus = () => armorRatio * (side == CombatSide.Player ? PlayerArmor : EnemyArmor);
            }
        }

        private double StatusEffectMultiplier(CombatSide source, StatusEffectKind kind)
        {
            _gearStatusBonuses.TryGetValue((source, kind), out double bonus);
            if (kind == StatusEffectKind.Poison)
                bonus += GetStatusStacks(source, StatusEffectKind.Plague) * UniqueGearEffect.PoisonPowerPerPlagueStack;
            return 1d + bonus;
        }

        private void ResolveGearAfterAttack(CombatWeapon weapon, CombatSide side, CombatSide target,
            ICollection<CombatItemEvent> events)
        {
            if (CurrentHealth(side) <= 0d || CurrentHealth(target) <= 0d) return;
            foreach (CombatItem source in GetItems(side))
            {
                bool linked = _gearAttackTargets.TryGetValue(source, out var weapons) && weapons.Contains(weapon.InstanceId);
                if (linked && source.Snapshot.GearEffect.Kind == UniqueGearEffectKind.SearingTwilight && RollGearChance(UniqueGearEffect.BurnChancePercent))
                    ApplyItemStatus(source, side, target, StatusEffectKind.Burn, UniqueGearEffect.BurnStacks, events);
                else if (linked && source.Snapshot.GearEffect.Kind == UniqueGearEffectKind.ExecutionersSpikedBall
                    && RollGearChance(UniqueGearEffect.ExecutionerThornsChancePercent))
                    ApplyItemStatus(source, side, side, StatusEffectKind.Thorns, 1, events);
            }
            UniqueGearEffect effect = weapon.Snapshot.GearEffect;
            switch (effect.Kind)
            {
                case UniqueGearEffectKind.FrostDawnFragment:
                    ApplyItemStatus(weapon, side, target, StatusEffectKind.Freeze, UniqueGearEffect.FreezeStacks, events);
                    break;
                case UniqueGearEffectKind.WhiteNightLantern:
                    ApplyItemStatus(weapon, side, target, StatusEffectKind.Poison, 1, events);
                    break;
                case UniqueGearEffectKind.SnowflakeRabbit:
                    ApplyItemStatus(weapon, side, target, StatusEffectKind.Freeze, UniqueGearEffect.FreezeStacks, events);
                    double amount = GetStatusStacks(target, StatusEffectKind.Freeze) * OwnerStats(side).Attack * effect.Amount;
                    double restored = RestoreHealth(side, amount * HealingMultiplier(side));
                    if (restored <= 0d) break;
                    Counter(side, weapon.InstanceId).Healing += restored;
                    RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, side, weapon, ItemEffectKind.Heal,
                        targetSide: side, healthRestored: restored));
                    break;
            }
        }

        private void ResolveFixedGearActivation(CombatItem item, CombatSide side, ICollection<CombatItemEvent> events)
        {
            CombatSide target = TargetSide(side, StatusTarget.Opponent);
            if (item.Snapshot.GearEffect.Kind == UniqueGearEffectKind.SearingTwilight)
            {
                TickStatusImmediately(target, StatusEffectKind.Burn, UniqueGearEffect.BurnBurstTicks, events);
                return;
            }
            long sacrificed = GetStatusStacks(target, StatusEffectKind.Poison) / 2;
            if (sacrificed > 0)
            {
                RemoveStatus(target, StatusEffectKind.Poison, sacrificed);
                ApplyItemStatus(item, side, side, StatusEffectKind.Plague, sacrificed, events);
            }
            double armor = GetStatusStacks(side, StatusEffectKind.Plague) * OwnerStats(side).Attack * UniqueGearEffect.ArmorPerPlagueStack;
            if (armor > 0d) GainArmor(item, side, armor, events);
        }

        private void ResolveGearActivation(CombatItem item, CombatSide side, ICollection<CombatItemEvent> events)
        {
            UniqueGearEffect gear = item.Snapshot.GearEffect;
            double maximum = side == CombatSide.Player ? PlayerMaxHealth : EnemyMaxHealth;
            double health = CurrentHealth(side);
            switch (gear.Kind)
            {
                case UniqueGearEffectKind.StoneGiantCuirass when health >= maximum * UniqueGearEffect.CuirassMinimumHealthRatio:
                    GainArmor(item, side, maximum * gear.Amount, events);
                    break;
                case UniqueGearEffectKind.SylphVest:
                    CleanseOrGainHaste(item, side, events);
                    break;
                case UniqueGearEffectKind.CharmAmulet:
                    StealBuffs(item, side, events);
                    break;
                case UniqueGearEffectKind.BloodKnightHelm when health > maximum * UniqueGearEffect.HealthSacrificeRatio:
                    double sacrifice = maximum * UniqueGearEffect.HealthSacrificeRatio;
                    if (side == CombatSide.Player) PlayerHealth -= sacrifice;
                    else EnemyHealth -= sacrifice;
                    GainArmor(item, side, maximum * gear.Amount, events);
                    break;
            }
        }

        private void StealBuffs(CombatItem item, CombatSide side, ICollection<CombatItemEvent> events)
        {
            CombatSide target = TargetSide(side, StatusTarget.Opponent);
            for (int i = 0; i < UniqueGearEffect.BuffStealCount; i++)
            {
                var available = _statusDefinitions.Values.Where(definition => !definition.IsDebuff && definition.Kind != StatusEffectKind.Plague
                    && GetStatusStacks(target, definition.Kind) > 0).ToArray();
                if (available.Length == 0) break;
                StatusEffectKind kind = available[_random.Next(available.Length)].Kind;
                RemoveStatus(target, kind, 1);
                ApplyItemStatus(item, side, side, kind, 1, events);
            }
        }

        private bool RollBurnCritical(CombatSide source, CombatSide target)
        {
            double bonus = GetItems(source).Where(item => item.Snapshot.GearEffect.Kind == UniqueGearEffectKind.PhoenixFeather)
                .Sum(item => (double)item.Snapshot.GearEffect.Amount);
            double chance = GetStatusStacks(target, StatusEffectKind.Burn) * bonus;
            return chance > 0d && _random.NextDouble() < chance;
        }

        private double HealingMultiplier(CombatSide side)
        {
            CombatSide opponent = TargetSide(side, StatusTarget.Opponent);
            double reduction = GetItems(opponent).Where(item => item.Snapshot.GearEffect.Kind == UniqueGearEffectKind.ExecutionersSpikedBall)
                .Sum(item => (double)item.Snapshot.GearEffect.Amount) * GetStatusStacks(opponent, StatusEffectKind.Thorns);
            return Math.Max(0d, OwnerStats(side).HealingMultiplier - reduction);
        }

        private double RestoreHealth(CombatSide side, double amount)
        {
            double health = CurrentHealth(side);
            if (health <= 0d) return 0d;
            double maximum = side == CombatSide.Player ? PlayerMaxHealth : EnemyMaxHealth;
            double restored = Math.Min(amount, maximum - health);
            if (side == CombatSide.Player) PlayerHealth += restored; else EnemyHealth += restored;
            return restored;
        }

        private void CleanseOrGainHaste(CombatItem item, CombatSide side, ICollection<CombatItemEvent> events)
        {
            var debuffs = _statusDefinitions.Values.Where(definition => definition.IsDebuff
                && definition.Kind != StatusEffectKind.Enrage && GetStatusStacks(side, definition.Kind) > 0).ToArray();
            if (debuffs.Length == 0)
            {
                ApplyItemStatus(item, side, side, StatusEffectKind.Haste, 1, events);
                return;
            }
            StatusEffectKind kind = debuffs.Length == 1 ? debuffs[0].Kind : debuffs[_random.Next(debuffs.Length)].Kind;
            int removed = (int)Math.Min(UniqueGearEffect.CleanseStacks, GetStatusStacks(side, kind));
            RemoveStatus(side, kind, removed);
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, side, item, ItemEffectKind.ApplyStatus,
                statusKind: kind, statusStacks: -removed, targetSide: side));
        }

        private void ResolveGearDamageReceived(CombatSide source, CombatSide target, DamageResolution damage,
            ICollection<CombatItemEvent> events)
        {
            if (source == target || damage.HealthRemaining <= 0d || damage.HealthDamage + damage.ArmorAbsorbed <= 0d) return;
            foreach (CombatItem item in GetItems(target))
                if (item.Snapshot.GearEffect.Kind == UniqueGearEffectKind.ThornShellPauldron
                    && RollGearChance(UniqueGearEffect.ThornsChancePercent))
                    ApplyItemStatus(item, target, target, StatusEffectKind.Thorns, 1, events);
        }

        private void ApplyItemStatus(CombatItem item, CombatSide owner, CombatSide target, StatusEffectKind kind,
            long stacks, ICollection<CombatItemEvent> events)
        {
            ApplyStatus(target, kind, stacks, owner, item.InstanceId);
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, owner, item, ItemEffectKind.ApplyStatus,
                statusKind: kind, statusStacks: stacks, targetSide: target));
        }

        private bool RollGearChance(int chancePercent) => _random.NextDouble() * 100d < chancePercent;
        private double CurrentHealth(CombatSide side) => side == CombatSide.Player ? PlayerHealth : EnemyHealth;
        private CharacterStatsSnapshot OwnerStats(CombatSide side) => (side == CombatSide.Player ? Player : Enemy).Stats;
    }
}
