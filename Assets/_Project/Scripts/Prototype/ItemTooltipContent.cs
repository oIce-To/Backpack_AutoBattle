using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using System.Linq;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public enum ItemStatChange { Unchanged, Improved, Reduced }

    public sealed class ItemTooltipContent
    {
        private WeaponStats _baseStats;
        private float _baseCooldownSeconds;
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

        public ItemStatChange GetStatChange(WeaponStatIcon stat)
        {
            if (!IsRuntime) return ItemStatChange.Unchanged;
            (float actual, float original) = stat switch
            {
                WeaponStatIcon.Attack => (AttackDamage, _baseStats.AttackDamage),
                WeaponStatIcon.Cooldown => (CooldownSeconds, _baseCooldownSeconds),
                WeaponStatIcon.Accuracy => (AccuracyPercent, _baseStats.AccuracyPercent),
                WeaponStatIcon.CriticalChance => (CriticalChancePercent, _baseStats.CriticalChancePercent),
                WeaponStatIcon.CriticalDamage => (CriticalDamagePercent, _baseStats.CriticalDamagePercent),
                _ => (0f, 0f)
            };
            if (Mathf.Approximately(actual, original)) return ItemStatChange.Unchanged;
            bool improved = stat == WeaponStatIcon.Cooldown ? actual < original : actual > original;
            return improved ? ItemStatChange.Improved : ItemStatChange.Reduced;
        }

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
            result.SetBattleStart(item.BattleStartStatuses);
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
            result.SetBattleStart(item.BattleStartStatuses);
            return result;
        }

        private void SetStats(WeaponStats baseStats, RuntimeWeaponStats runtime)
        {
            _baseStats = baseStats.Copy();
            AttackDamage = runtime?.AttackDamage ?? baseStats.AttackDamage;
            AccuracyPercent = runtime?.AccuracyPercent ?? baseStats.AccuracyPercent;
            CriticalChancePercent = runtime?.CriticalChancePercent ?? baseStats.CriticalChancePercent;
            CriticalDamagePercent = runtime?.CriticalDamagePercent ?? baseStats.CriticalDamagePercent;
        }

        private void SetActivation(ItemActivationData activation, float interval)
        {
            _baseCooldownSeconds = activation.CooldownSeconds;
            CooldownSeconds = interval;
            string automatic = string.Join("\n", activation.Effects.Where(effect => effect != null && activation.HasCycle
                && effect.Kind == ItemEffectKind.GainArmor).Select(effect => $"{interval:0.0#}초마다 방어도를 {effect.Amount:0.#} 얻습니다."));
            if (!string.IsNullOrEmpty(automatic))
                EffectDescription = string.IsNullOrWhiteSpace(EffectDescription) ? automatic : automatic + "\n" + EffectDescription;
            foreach (ItemActivationEffect effect in activation.Effects.Where(effect => effect != null && activation.HasCycle
                && effect.Kind == ItemEffectKind.ApplyStatus && effect.Status != null))
                AppendEffect($"{interval:0.0#}초마다 {DescribeStatus(effect.Status)}");
        }

        private void SetBattleStart(System.Collections.Generic.IEnumerable<StatusApplication> statuses)
        {
            foreach (StatusApplication status in statuses)
                if (status != null) AppendEffect($"전투 시작 시 {DescribeStatus(status)}");
        }

        private void AppendEffect(string text)
            => EffectDescription = string.IsNullOrWhiteSpace(EffectDescription) ? text : EffectDescription + "\n" + text;

        private static string DescribeStatus(StatusApplication status)
            => $"{(status.Target == StatusTarget.Self ? "자신에게" : "상대에게")} "
                + $"{(status.Kind == StatusEffectKind.Haste ? "신속" : "빙결")} {status.Stacks}중첩을 부여합니다.";
    }
}
