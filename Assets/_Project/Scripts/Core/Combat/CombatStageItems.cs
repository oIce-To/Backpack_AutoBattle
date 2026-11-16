using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed partial class CombatSimulation
    {
        private readonly Dictionary<CombatSide, CombatItem[]> _stageItems = new();
        private readonly Dictionary<CombatItem, HashSet<string>> _stageTargets = new();
        private static readonly ItemTargetFilter MeleeWeapon = new(ItemCategory.Weapon, ItemTags.Melee);

        private void InitializeStageItems(CombatSide side, CombatLoadoutSnapshot loadout)
        {
            var items = GetItems(side).Where(item => item.Snapshot.StageEffect != StageItemEffect.None).ToArray();
            _stageItems.Add(side, items);
            foreach (var item in items)
            {
                if (item.Snapshot.PlacementEffects.Count == 0) continue;
                var targets = PlacementEffectResolver.GetCells(loadout, item.Snapshot).Where(cell => cell.ReceivesEffect)
                    .Select(cell => cell.TargetInstanceId).ToHashSet();
                if (item.Snapshot.StageEffect == StageItemEffect.TrueShot) targets.Add(item.InstanceId);
                _stageTargets.Add(item, targets);
            }

            CombatSide enemy = Opponent(side);
            foreach (var weapon in GetItems(side).OfType<CombatWeapon>())
            {
                var linked = items.Where(item => StageLinks(item, weapon.InstanceId)).ToArray();
                int focusSources = linked.Count(item => item.Snapshot.StageEffect is StageItemEffect.Hawkeye or StageItemEffect.TrueShot);
                int hasteSources = linked.Count(item => item.Snapshot.StageEffect == StageItemEffect.WingedBoots);
                bool useOverflow = linked.Any(item => item.Snapshot.StageEffect == StageItemEffect.TrueShot);
                StageItemEffect effect = weapon.Snapshot.StageEffect;
                bool scalesWithStacks = effect is StageItemEffect.Eclipse or StageItemEffect.SnowBloom or StageItemEffect.NeedleStorm;
                weapon.Stats.StageAccuracyBonus = linked.Sum(item => item.Snapshot.StageEffect switch
                {
                    StageItemEffect.Quiver => 10d,
                    StageItemEffect.PoisonQuiver => 20d,
                    _ => 0d
                });
                weapon.StageSpeedBonus = linked.Count(item => item.Snapshot.StageEffect is StageItemEffect.Quiver or StageItemEffect.PoisonQuiver) * 15d
                    + (MeleeWeapon.Matches(weapon.Snapshot.Category, weapon.Snapshot.Tags)
                        ? items.Count(item => item.Snapshot.StageEffect == StageItemEffect.Mithril) * 10d : 0d);
                if (scalesWithStacks || effect == StageItemEffect.Excalibur)
                    weapon.Stats.StageAttackFactor = () => 1d + (effect switch
                    {
                        StageItemEffect.Eclipse => GetStatusStacks(side, StatusEffectKind.Lifesteal) * .03d,
                        StageItemEffect.SnowBloom => GetStatusStacks(enemy, StatusEffectKind.Freeze) * .01d,
                        StageItemEffect.Excalibur => weapon.StageAttackCount * .05d,
                        _ => 0d
                    });
                if (scalesWithStacks || focusSources > 0 || hasteSources > 0 || effect is StageItemEffect.Excalibur or StageItemEffect.Dragonslayer)
                    weapon.Stats.StageCriticalBonus = () => weapon.StageCriticalBonus
                        + (focusSources == 0 ? 0d : focusSources * (double)GetStatusStacks(side, StatusEffectKind.Focus))
                        + (hasteSources == 0 ? 0d : hasteSources * (double)GetStatusStacks(side, StatusEffectKind.Haste))
                        + (effect switch
                        {
                            StageItemEffect.Eclipse => GetStatusStacks(side, StatusEffectKind.Lifesteal),
                            StageItemEffect.SnowBloom => GetStatusStacks(enemy, StatusEffectKind.Freeze) * .5d,
                            StageItemEffect.NeedleStorm => GetStatusStacks(enemy, StatusEffectKind.Poison) * .5d,
                            _ => 0d
                        });
                if (useOverflow || hasteSources > 0 || effect is StageItemEffect.NeedleStorm or StageItemEffect.Excalibur)
                {
                    weapon.Stats.StageCriticalDamageBonus = () =>
                        (useOverflow ? Math.Max(0d, weapon.Stats.CriticalChancePercent - 100d) * 2d : 0d)
                        + (hasteSources == 0 ? 0d : hasteSources * 2d * GetStatusStacks(side, StatusEffectKind.Haste));
                    weapon.Stats.StageCriticalDamageFactor = () => 1d + (effect switch
                    {
                        StageItemEffect.NeedleStorm => GetStatusStacks(enemy, StatusEffectKind.Poison) * .01d,
                        StageItemEffect.Excalibur => weapon.StageAttackCount * .05d,
                        _ => 0d
                    });
                }
            }
        }

        private bool StageLinks(CombatItem item, string targetId)
            => _stageTargets.TryGetValue(item, out var targets) && targets.Contains(targetId);

        private static CombatSide Opponent(CombatSide side) => TargetSide(side, StatusTarget.Opponent);

        private bool HasStageItem(CombatSide side, StageItemEffect effect)
            => _stageItems[side].Any(item => item.Snapshot.StageEffect == effect);

        private void ApplyStageBattleStart()
        {
            foreach (CombatSide side in new[] { CombatSide.Player, CombatSide.Enemy })
                foreach (CombatItem item in _stageItems[side])
                {
                    switch (item.Snapshot.StageEffect)
                    {
                        case StageItemEffect.WornHelmet:
                        case StageItemEffect.IronThornHelmet:
                            double ratio = item.Snapshot.StageEffect.StartingArmorRatio();
                            GainArmor(item, side, OwnerStats(side).MaximumHealth * ratio, null);
                            break;
                        case StageItemEffect.BloodRose:
                            RemoveStatus(Opponent(side), StatusEffectKind.Thorns, 5);
                            RemoveStatus(Opponent(side), StatusEffectKind.Lifesteal, 5);
                            ApplyItemStatus(item, side, side, StatusEffectKind.Thorns, 5, null);
                            ApplyItemStatus(item, side, side, StatusEffectKind.Lifesteal, 5, null);
                            break;
                    }
                }
            RefreshItemSpeeds(CombatSide.Player);
            RefreshItemSpeeds(CombatSide.Enemy);
        }

        private void ResolveStageActivation(CombatItem item, CombatSide side, ICollection<CombatItemEvent> events)
        {
            CombatSide enemy = Opponent(side);
            switch (item.Snapshot.StageEffect)
            {
                case StageItemEffect.Moonflower: TakeDebuff(item, side, 2, transfer: false, events); break;
                case StageItemEffect.DeceptionCloak:
                    if (TakeStatus(item, side, enemy, StatusEffectKind.Agility, 1, events) > 0)
                        ApplyItemStatus(item, side, enemy, StatusEffectKind.Blindness, 1, events);
                    break;
                case StageItemEffect.NightVeil:
                    ApplyItemStatus(item, side, side, StatusEffectKind.Agility, 1, events);
                    TakeStatus(item, side, enemy, StatusEffectKind.Agility, 1, events);
                    ApplyItemStatus(item, side, enemy, StatusEffectKind.Blindness, 1, events);
                    break;
                case StageItemEffect.BurningCloak: TakeStatus(item, side, side, StatusEffectKind.Freeze, 3, events); break;
                case StageItemEffect.SolarRadiance: TakeStatus(item, side, side, StatusEffectKind.Freeze, 2, events); break;
                case StageItemEffect.IceHeart: TakeStatus(item, side, side, StatusEffectKind.Burn, 3, events); break;
            }
        }

        private static double ArmorDamageFactor(CombatItem item) => item.Snapshot.StageEffect switch
        {
            StageItemEffect.Spear or StageItemEffect.Halberd => 1.5d,
            StageItemEffect.Dragonslayer => 2d,
            StageItemEffect.Bola => 3d,
            _ => 1d
        };

        private (CombatItem item, double rate) StrongestShield(CombatSide side)
        {
            CombatItem strongest = null;
            double rate = 0d;
            foreach (var item in _stageItems[side])
            {
                double candidate = item.Snapshot.StageEffect switch
                {
                    StageItemEffect.WoodShield => .10d,
                    StageItemEffect.SolarRadiance => .15d,
                    StageItemEffect.MithrilShield => .20d,
                    StageItemEffect.GenesisArmor => .30d,
                    _ => 0d
                };
                if (candidate <= rate) continue;
                strongest = item;
                rate = candidate;
            }
            return (strongest, rate);
        }

        private double ApplyStageDamageReduction(CombatSide target, double damage, CombatItem protector, double rate, double armorFactor = 1d)
        {
            if (protector == null) return damage;
            double reduced = damage * (1d - rate);
            double maximum = CurrentHealth(target) + (target == CombatSide.Player ? PlayerArmor : EnemyArmor) / armorFactor;
            double incoming = GetIncomingDamageMultiplier(target);
            RecordDamageReduction(target, protector.InstanceId,
                Math.Min(maximum, damage * incoming) - Math.Min(maximum, reduced * incoming));
            return reduced;
        }

        private void ResolveStageAvoidance(CombatSide side)
        {
            foreach (var item in _stageItems[side])
                if (item.Snapshot.StageEffect is StageItemEffect.IronDagger or StageItemEffect.VampireKnife
                    or StageItemEffect.Eclipse or StageItemEffect.WingedBoots)
                    item.Cycle.Charge(.30d);
        }

        private void ResolveStageAttackAttempt(CombatWeapon weapon, CombatSide side, ICollection<CombatItemEvent> events)
        {
            double speed = weapon.Snapshot.StageEffect switch
            {
                StageItemEffect.BattleAxe => 5d,
                StageItemEffect.Halberd => 7d,
                StageItemEffect.Dragonslayer => 10d,
                _ => 0d
            };
            if (weapon.Snapshot.StageEffect == StageItemEffect.Excalibur)
            {
                weapon.StageAttackCount++;
                weapon.StageCriticalBonus++;
                TakeDebuff(weapon, side, 3, transfer: false, events);
            }
            if (weapon.Snapshot.StageEffect == StageItemEffect.LunarEclipse)
                speed += TakeDebuff(weapon, side, 2, transfer: true, events) + StealBuffs(weapon, side, 2, events);
            if (speed <= 0d) return;
            weapon.StageSpeedBonus += speed;
            RefreshItemSpeeds(side);
        }

        private void ResolveStageHit(CombatWeapon weapon, CombatSide side, DamageResolution damage, bool critical, ICollection<CombatItemEvent> events)
        {
            CombatSide enemy = Opponent(side);
            bool enemyAlive = CurrentHealth(enemy) > 0d;
            switch (weapon.Snapshot.StageEffect)
            {
                case StageItemEffect.Bola when enemyAlive: TakeStatus(weapon, side, enemy, StatusEffectKind.Agility, 15, events); break;
                case StageItemEffect.Dragonslayer when damage.ArmorAbsorbed > 0d: weapon.StageCriticalBonus += 5d; break;
                case StageItemEffect.VampireKnife when RollGearChance(50):
                    ApplyItemStatus(weapon, side, side, StatusEffectKind.Lifesteal, 1, events); break;
                case StageItemEffect.Eclipse:
                    ApplyItemStatus(weapon, side, side, StatusEffectKind.Lifesteal, critical ? 2 : 1, events); break;
                case StageItemEffect.PrimalFlame when enemyAlive:
                    ApplyItemStatus(weapon, side, enemy, StatusEffectKind.Burn, critical ? 3 : 2, events); break;
                case StageItemEffect.SnowBloom:
                    if (enemyAlive) ApplyItemStatus(weapon, side, enemy, StatusEffectKind.Freeze, critical ? 3 : 2, events);
                    HealFromStageItem(weapon, side, (damage.ArmorAbsorbed + damage.HealthDamage) * .30d, events);
                    break;
                case StageItemEffect.NeedleStorm when enemyAlive:
                    ApplyItemStatus(weapon, side, enemy, StatusEffectKind.Poison, 2, events); break;
            }
            foreach (var item in _stageItems[side])
            {
                if (!StageLinks(item, weapon.InstanceId)) continue;
                switch (item.Snapshot.StageEffect)
                {
                    case StageItemEffect.PoisonQuiver when enemyAlive && RollGearChance(50):
                        ApplyItemStatus(item, side, enemy, StatusEffectKind.Poison, 1, events); break;
                    case StageItemEffect.TrueShot when RollGearChance(40):
                        ApplyItemStatus(item, side, side, StatusEffectKind.Focus, 1, events); break;
                    case StageItemEffect.Eclipse: item.Cycle.Charge(.30d); break;
                    case StageItemEffect.NeedleStorm when enemyAlive:
                        ApplyItemStatus(item, side, enemy, StatusEffectKind.Poison, 1, events); break;
                }
            }
        }

        private void ResolveStageAttackReceived(CombatSide target, DamageResolution damage, ICollection<CombatItemEvent> events)
        {
            if (damage.HealthRemaining <= 0d || damage.ArmorAbsorbed + damage.HealthDamage <= 0d) return;
            foreach (var item in _stageItems[target])
            {
                int chance = item.Snapshot.StageEffect switch
                {
                    StageItemEffect.BurningCloak => 30,
                    StageItemEffect.SolarRadiance => 50,
                    _ => 0
                };
                if (chance > 0 && RollGearChance(chance) && CurrentHealth(Opponent(target)) > 0d)
                    ApplyItemStatus(item, target, Opponent(target), StatusEffectKind.Burn, 1, events);
            }
        }

        private void ResolveStageHealthThreshold(CombatSide side, double previousHealth, ICollection<CombatItemEvent> events)
        {
            double health = CurrentHealth(side), maximum = OwnerStats(side).MaximumHealth;
            double threshold = maximum * StageItemEffectRules.PotionHealthThreshold;
            if (health <= 0d || health >= threshold || previousHealth < threshold) return;
            foreach (var potion in _stageItems[side])
            {
                if (potion.Snapshot.StageEffect != StageItemEffect.Potion || potion.StageSpent) continue;
                potion.StageSpent = true;
                HealFromStageItem(potion, side, maximum * StageItemEffectRules.PotionHealRatio, events);
            }
        }

        private void HealFromStageItem(CombatItem source, CombatSide side, double amount, ICollection<CombatItemEvent> events)
        {
            double restored = RestoreHealth(side, amount * HealingMultiplier(side));
            if (restored <= 0d) return;
            Counter(side, source.InstanceId).Healing += restored;
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, side, source, ItemEffectKind.Heal,
                targetSide: side, healthRestored: restored));
        }

        private long TakeStatus(CombatItem item, CombatSide owner, CombatSide target, StatusEffectKind kind, long count,
            ICollection<CombatItemEvent> events)
        {
            long taken = Math.Min(count, GetStatusStacks(target, kind));
            if (taken <= 0) return 0;
            RemoveStatus(target, kind, taken);
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, owner, item, ItemEffectKind.ApplyStatus,
                statusKind: kind, statusStacks: -taken, targetSide: target));
            return taken;
        }

        private long TakeDebuff(CombatItem item, CombatSide side, int count, bool transfer, ICollection<CombatItemEvent> events)
        {
            var available = _statusDefinitions.Values.Where(status => status.IsDebuff && status.Kind != StatusEffectKind.Enrage
                && GetStatusStacks(side, status.Kind) > 0).ToArray();
            if (available.Length == 0) return 0;
            var kind = available[_random.Next(available.Length)].Kind;
            long taken = TakeStatus(item, side, side, kind, count, events);
            return transfer ? ApplyItemStatus(item, side, Opponent(side), kind, taken, events) : taken;
        }

        private double StageArmorGainBonus(CombatSide side) => _stageItems[side].Sum(item => item.Snapshot.StageEffect switch
        {
            StageItemEffect.Mithril or StageItemEffect.MithrilArmor or StageItemEffect.MithrilShield => .10d,
            StageItemEffect.GenesisArmor => .30d,
            _ => 0d
        });

        private double StageHealingPenalty(CombatSide side)
            => _stageItems[Opponent(side)].Count(item => item.Snapshot.StageEffect == StageItemEffect.RazorThorn) * .20d
                + PrimalFlamePenalty(side);

        private double PrimalFlamePenalty(CombatSide side)
            => HasStageItem(Opponent(side), StageItemEffect.PrimalFlame) ? GetStatusStacks(side, StatusEffectKind.Burn) * .01d : 0d;

        private long ScaleStageStatusStacks(CombatSide target, CombatSide source, string itemId, StatusEffectKind kind, long stacks)
        {
            if (kind == StatusEffectKind.Freeze && itemId != null)
            {
                int supports = _stageItems[source].Count(item => item.Snapshot.StageEffect == StageItemEffect.Permafrost && StageLinks(item, itemId));
                if (supports > 0)
                    stacks = (long)Math.Min(long.MaxValue, Math.Round(stacks * (1m + supports * .50m), MidpointRounding.AwayFromZero));
            }
            if (_statusDefinitions[kind].IsDebuff && HasStageItem(target, StageItemEffect.GenesisArmor)) stacks /= 2;
            return stacks;
        }

        private bool RollThornsCritical(CombatSide side, out double multiplier)
        {
            var armors = _stageItems[side];
            int blood = armors.Count(item => item.Snapshot.StageEffect == StageItemEffect.BloodIronArmor);
            int iron = armors.Count(item => item.Snapshot.StageEffect == StageItemEffect.IronMaiden);
            long stacks = GetStatusStacks(side, StatusEffectKind.Thorns);
            multiplier = 2d * (1d + OwnerStats(side).CriticalDamageBonusPercent / 100d) * (1d + stacks * (blood * .02d + iron * .01d));
            return blood + iron > 0 && _random.NextDouble() < stacks * (blood * .01d + iron * .005d);
        }

        private void HealThornsCritical(CombatSide side, DamageResolution damage, ICollection<CombatItemEvent> events)
        {
            foreach (var armor in _stageItems[side])
                if (armor.Snapshot.StageEffect == StageItemEffect.BloodIronArmor)
                    HealFromStageItem(armor, side, (damage.HealthDamage + damage.ArmorAbsorbed) * .25d, events);
        }
    }
}
