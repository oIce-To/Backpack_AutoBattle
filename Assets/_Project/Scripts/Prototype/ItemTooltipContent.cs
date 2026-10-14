using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Prototype
{
    public sealed class ItemTooltipContent
    {
        public string DisplayName { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public string Description { get; private set; }
        public string EffectDescription { get; private set; }
        public bool HasWeaponStats { get; private set; }
        public bool IsRuntime { get; private set; }
        public float AttackDamage { get; private set; }
        public float CooldownSeconds { get; private set; }
        public float AccuracyPercent { get; private set; }
        public float CriticalChancePercent { get; private set; }
        public float CriticalDamagePercent { get; private set; }

        public static ItemTooltipContent FromItem(ItemData item)
        {
            var result = new ItemTooltipContent
            {
                DisplayName = item.DisplayName,
                Rarity = item.Rarity,
                Description = item.Description,
                EffectDescription = item.EffectDescription,
                HasWeaponStats = item.Category == ItemCategory.Weapon
            };
            result.SetStats(item.BaseWeaponStats, null);
            return result;
        }

        public static ItemTooltipContent FromCombat(CombatItemSnapshot item, RuntimeWeaponStats runtime)
        {
            var result = new ItemTooltipContent
            {
                DisplayName = item.DisplayName,
                Rarity = item.Rarity,
                Description = item.Description,
                EffectDescription = item.EffectDescription,
                HasWeaponStats = item.Category == ItemCategory.Weapon,
                IsRuntime = runtime != null
            };
            result.SetStats(item.BaseWeaponStats, runtime);
            return result;
        }

        private void SetStats(WeaponStats baseStats, RuntimeWeaponStats runtime)
        {
            AttackDamage = runtime?.AttackDamage ?? baseStats.AttackDamage;
            CooldownSeconds = runtime?.CooldownSeconds ?? baseStats.CooldownSeconds;
            AccuracyPercent = runtime?.AccuracyPercent ?? baseStats.AccuracyPercent;
            CriticalChancePercent = runtime?.CriticalChancePercent ?? baseStats.CriticalChancePercent;
            CriticalDamagePercent = runtime?.CriticalDamagePercent ?? baseStats.CriticalDamagePercent;
        }
    }
}
