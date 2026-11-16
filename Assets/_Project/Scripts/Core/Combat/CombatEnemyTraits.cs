using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed partial class CombatSimulation
    {
        private static readonly StatusEffectKind[] IdolDebuffs =
        {
            StatusEffectKind.Blindness, StatusEffectKind.Freeze, StatusEffectKind.Burn, StatusEffectKind.Poison
        };

        private bool _stoneArmorBroken;
        private bool _enemyRevived;
        private double _enemyInvulnerableUntil;
        private int _enemyWeaponHits;

        private void InitializeEnemyTrait()
        {
            foreach (CombatWeapon weapon in EnemyWeapons)
            {
                if (Enemy.EnemyTrait is EnemyTrait.StoneArmor or EnemyTrait.Pharaoh or EnemyTrait.FalseIdol
                    or EnemyTrait.Yeti or EnemyTrait.NamelessKnight or EnemyTrait.LegionCommander)
                    weapon.Stats.EnemyAttackFactor = EnemyWeaponAttackFactor;
                if (Enemy.EnemyTrait == EnemyTrait.WeaponMaster)
                    weapon.Stats.EnemyCriticalBonus = () => _enemyWeaponHits;
            }
            if (Enemy.EnemyTrait == EnemyTrait.StoneArmor && EnemyWeapons.Count > 0)
                GainArmor(EnemyWeapons[0], CombatSide.Enemy, EnemyMaxHealth * .35d, null);
        }

        private bool EnemyResistsStatus(StatusEffectKind kind) => Enemy.EnemyTrait switch
        {
            EnemyTrait.VenomImmunity => kind == StatusEffectKind.Poison,
            EnemyTrait.BurnImmunity => kind == StatusEffectKind.Burn,
            EnemyTrait.FreezeImmunity => kind == StatusEffectKind.Freeze,
            _ => false
        };

        private double EnemyMissingHealth => 1d - EnemyHealth / EnemyMaxHealth;

        private double EnemyWeaponAttackFactor() => Enemy.EnemyTrait switch
        {
            EnemyTrait.StoneArmor when !_stoneArmorBroken && EnemyArmor > 0d => 1.5d,
            EnemyTrait.FalseIdol => 1d + .02d * IdolDebuffs.Sum(kind => (double)GetStatusStacks(CombatSide.Player, kind)),
            EnemyTrait.Yeti => 1d + .5d * EnemyMissingHealth,
            EnemyTrait.NamelessKnight when PlayerArmor > 0d => 1.5d,
            EnemyTrait.Pharaoh or EnemyTrait.LegionCommander when _enemyRevived => 1.5d,
            _ => 1d
        };

        private double EnemyWeaponSpeedBonus() => Enemy.EnemyTrait switch
        {
            EnemyTrait.PenitentPriest => _enemyWeaponHits * .05d,
            EnemyTrait.Yeti => .5d * EnemyMissingHealth,
            EnemyTrait.Pharaoh or EnemyTrait.LegionCommander when _enemyRevived => .3d,
            _ => 0d
        };

        private void RecordEnemyWeaponHit(CombatWeapon weapon)
        {
            if (weapon.Snapshot.Category != ItemCategory.Weapon) return;
            switch (Enemy.EnemyTrait)
            {
                case EnemyTrait.PenitentPriest:
                    _enemyWeaponHits++;
                    RefreshItemSpeeds(CombatSide.Enemy);
                    break;
                case EnemyTrait.WeaponMaster:
                    _enemyWeaponHits++;
                    break;
            }
        }

        private void RefreshEnemyHealthDependentSpeed()
        {
            if (Enemy.EnemyTrait == EnemyTrait.Yeti) RefreshItemSpeeds(CombatSide.Enemy);
        }

        private void ResolveEnemyDamage(DamageResolution damage, ICollection<CombatItemEvent> events)
        {
            if (damage.ArmorAbsorbed > 0d && damage.ArmorRemaining == 0d && Enemy.EnemyTrait == EnemyTrait.StoneArmor)
                _stoneArmorBroken = true;
            if (damage.HealthDamage > 0d) RefreshEnemyHealthDependentSpeed();
            if (EnemyHealth > 0d || _enemyRevived || Enemy.EnemyTrait is not (EnemyTrait.Pharaoh or EnemyTrait.LegionCommander))
                return;

            _enemyRevived = true;
            EnemyHealth = EnemyMaxHealth * .3d;
            if (Enemy.EnemyTrait == EnemyTrait.Pharaoh) _enemyInvulnerableUntil = ElapsedSeconds + 3d;
            RefreshItemSpeeds(CombatSide.Enemy);

            // Revival sets a fixed phase-entry HP. Normal healing bonuses and penalties do not change it.
            if (EnemyWeapons.Count == 0) return;
            CombatWeapon source = EnemyWeapons[0];
            Counter(CombatSide.Enemy, source.InstanceId).Healing += EnemyHealth;
            RecordItemEvent(events, new CombatItemEvent(ElapsedSeconds, CombatSide.Enemy, source, ItemEffectKind.Heal,
                targetSide: CombatSide.Enemy, healthRestored: EnemyHealth));
        }

        private void ResolveEnemySkill(CombatItem source, ICollection<CombatItemEvent> events)
        {
            switch (Enemy.EnemyTrait)
            {
                case EnemyTrait.Dryad:
                    int stolen = StealBuffs(source, CombatSide.Enemy, 2, events);
                    if (stolen > 0) HealFromStageItem(source, CombatSide.Enemy, EnemyMaxHealth * .03d * stolen, events);
                    break;
                case EnemyTrait.FalseIdol:
                    ApplyItemStatus(source, CombatSide.Enemy, CombatSide.Player,
                        IdolDebuffs[_random.Next(IdolDebuffs.Length)], 3, events);
                    break;
                case EnemyTrait.LegionCommander:
                    StealBuffs(source, CombatSide.Enemy, 3, events);
                    break;
            }
        }
    }
}
