using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed partial class CombatSimulation
    {
        private readonly Dictionary<(CombatSide, StatusEffectKind), double> _gearStatusBonuses = new();
        private readonly Dictionary<CombatItem, HashSet<string>> _twilightWeapons = new();
        private readonly Dictionary<CombatItem, int> _twilightAttackCounts = new();

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
                    _ => null
                };
                if (enhanced.HasValue)
                {
                    var key = (side, enhanced.Value);
                    _gearStatusBonuses.TryGetValue(key, out double bonus);
                    _gearStatusBonuses[key] = bonus + effect.Amount;
                }
                if (effect.Kind != UniqueGearEffectKind.SearingTwilight) continue;

                var weapons = PlacementEffectResolver.GetCells(loadout, item.Snapshot)
                    .Where(cell => cell.ReceivesEffect && item.Snapshot.PlacementEffects[cell.EffectIndex].TriggersGearEffect)
                    .Select(cell => cell.TargetInstanceId).ToHashSet();
                weapons.Add(item.InstanceId);
                _twilightWeapons.Add(item, weapons);
            }
        }

        private double StatusEffectMultiplier(CombatSide source, StatusEffectKind kind)
            => _gearStatusBonuses.TryGetValue((source, kind), out double bonus) ? 1d + bonus : 1d;

        private void ResolveGearAfterAttack(CombatWeapon weapon, CombatSide side, CombatSide target,
            ICollection<CombatItemEvent> events)
        {
            if (CurrentHealth(side) <= 0d || CurrentHealth(target) <= 0d) return;
            foreach (CombatItem source in GetItems(side))
            {
                if (!_twilightWeapons.TryGetValue(source, out var weapons) || !weapons.Contains(weapon.InstanceId)) continue;
                if (RollGearChance(UniqueGearEffect.BurnChancePercent))
                    ApplyItemStatus(source, side, target, StatusEffectKind.Burn, UniqueGearEffect.BurnStacks, events);
                if (source != weapon) continue;

                _twilightAttackCounts.TryGetValue(source, out int attacks);
                attacks++;
                if (attacks == UniqueGearEffect.AttacksPerBurnBurst)
                {
                    _twilightAttackCounts[source] = 0;
                    TickStatusImmediately(target, StatusEffectKind.Burn, UniqueGearEffect.BurnBurstTicks, events);
                }
                else _twilightAttackCounts[source] = attacks;
                if (Result != CombatResult.Running) break;
            }
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
                case UniqueGearEffectKind.BloodKnightHelm when health > maximum * UniqueGearEffect.HealthSacrificeRatio:
                    double sacrifice = maximum * UniqueGearEffect.HealthSacrificeRatio;
                    if (side == CombatSide.Player) PlayerHealth -= sacrifice;
                    else EnemyHealth -= sacrifice;
                    GainArmor(item, side, maximum * gear.Amount, events);
                    break;
            }
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
            int stacks, ICollection<CombatItemEvent> events)
        {
            ApplyStatus(target, kind, stacks, owner, item.InstanceId);
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, owner, item, ItemEffectKind.ApplyStatus,
                statusKind: kind, statusStacks: stacks, targetSide: target));
        }

        private bool RollGearChance(int chancePercent) => _random.NextDouble() * 100d < chancePercent;
        private double CurrentHealth(CombatSide side) => side == CombatSide.Player ? PlayerHealth : EnemyHealth;
    }
}
