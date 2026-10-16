using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public enum ItemStatChange { Unchanged, Improved, Reduced }

    public sealed class ItemTooltipContent
    {
        public const string PlacementIconName = "placement_cell";
        public const string PlacementIconTag = "<sprite name=\"" + PlacementIconName + "\">";

        private WeaponStats _baseStats;
        private float _baseCooldownSeconds;
        public string DisplayName { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public string Description { get; private set; }
        public string EffectDescription { get; private set; }
        public IReadOnlyList<string> PlacementDescriptions { get; private set; } = System.Array.Empty<string>();
        public bool HasWeaponStats { get; private set; }
        public bool HasCycle { get; private set; }
        public bool IsRuntime { get; private set; }
        public float AttackDamage { get; private set; }
        public float CooldownSeconds { get; private set; }
        public float AccuracyPercent { get; private set; }
        public float CriticalChancePercent { get; private set; }
        public float CriticalDamagePercent { get; private set; }

        public string GetDescriptionText()
        {
            var paragraphs = new List<string>(PlacementDescriptions);
            if (!string.IsNullOrWhiteSpace(EffectDescription)) paragraphs.Add(EffectDescription);
            if (!string.IsNullOrWhiteSpace(Description)) paragraphs.Add(Description);
            return paragraphs.Count == 0 ? "추가 설명 없음" : string.Join("\n\n", paragraphs);
        }

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
            result.SetPlacement(item.PlacementEffects);
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
            result.SetPlacement(item.PlacementEffects);
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

        private void SetPlacement(IEnumerable<ItemPlacementEffect> effects)
        {
            PlacementDescriptions = effects.Where(effect => effect != null).Select(effect =>
                $"{PlacementIconTag}에 배치되어 있는 {TargetLabel(effect.TargetFilter)}의 공격력을 {Mathf.Abs(effect.AttackDamageBonus):0.#} "
                + $"{(effect.AttackDamageBonus >= 0f ? "증가" : "감소")}시킵니다.").ToArray();
        }

        private static string TargetLabel(ItemTargetFilter filter)
        {
            var tags = new List<string>();
            foreach (var pair in new[] { (ItemTags.Melee, "근접"), (ItemTags.Ranged, "원거리"), (ItemTags.Magic, "마법"), (ItemTags.Summon, "소환") })
                if (filter != null && (filter.RequiredTags & pair.Item1) != 0) tags.Add(pair.Item2);
            string category = filter?.Category switch
            {
                ItemCategory.Weapon => "무기", ItemCategory.Armor => "방어구", ItemCategory.Accessory => "장신구",
                ItemCategory.Consumable => "소모품", ItemCategory.Material => "재료", ItemCategory.Pet => "펫", ItemCategory.Bag => "가방",
                _ => "공격 아이템"
            };
            tags.Add(category);
            return string.Join(" ", tags);
        }

        private static string DescribeStatus(StatusApplication status)
            => $"{(status.Target == StatusTarget.Self ? "자신에게" : "상대에게")} "
                + $"{(status.Kind == StatusEffectKind.Haste ? "신속" : "빙결")} {status.Stacks}중첩을 부여합니다.";
    }
}
