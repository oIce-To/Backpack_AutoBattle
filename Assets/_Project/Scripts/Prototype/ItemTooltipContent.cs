using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using System.Linq;

namespace BackpackAutoBattle.Prototype
{
    public sealed class ItemTooltipContent
    {
        public string DisplayName { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public string Description { get; private set; }
        public string EffectDescription { get; private set; }
        public bool HasWeaponStats { get; private set; }
        public bool HasCycle { get; private set; }
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
                HasWeaponStats = item.Activation.HasEffect(ItemEffectKind.Attack),
                HasCycle = item.Activation.HasCycle
            };
            result.SetStats(item.BaseWeaponStats, null);
            result.SetActivation(item.Activation, item.Activation.CooldownSeconds);
            return result;
        }

        public static ItemTooltipContent FromCombat(CombatItemSnapshot item, CombatItem runtime)
        {
            var result = new ItemTooltipContent
            {
                DisplayName = item.DisplayName,
                Rarity = item.Rarity,
                Description = item.Description,
                EffectDescription = item.EffectDescription,
                HasWeaponStats = item.Activation.HasEffect(ItemEffectKind.Attack),
                HasCycle = item.Activation.HasCycle,
                IsRuntime = runtime != null
            };
            result.SetStats(item.BaseWeaponStats, (runtime as CombatWeapon)?.Stats);
            result.SetActivation(item.Activation, (float)(runtime?.Cycle.IntervalSeconds ?? item.Activation.CooldownSeconds));
            return result;
        }

        private void SetStats(WeaponStats baseStats, RuntimeWeaponStats runtime)
        {
            AttackDamage = runtime?.AttackDamage ?? baseStats.AttackDamage;
            AccuracyPercent = runtime?.AccuracyPercent ?? baseStats.AccuracyPercent;
            CriticalChancePercent = runtime?.CriticalChancePercent ?? baseStats.CriticalChancePercent;
            CriticalDamagePercent = runtime?.CriticalDamagePercent ?? baseStats.CriticalDamagePercent;
        }

        private void SetActivation(ItemActivationData activation, float interval)
        {
            CooldownSeconds = interval;
            string automatic = string.Join("\n", activation.Effects.Where(effect => effect != null && activation.HasCycle
                && effect.Kind == ItemEffectKind.GainArmor).Select(effect => $"{interval:0.0#}초마다 방어도를 {effect.Amount:0.#} 얻습니다."));
            if (!string.IsNullOrEmpty(automatic))
                EffectDescription = string.IsNullOrWhiteSpace(EffectDescription) ? automatic : automatic + "\n" + EffectDescription;
        }
    }
}
