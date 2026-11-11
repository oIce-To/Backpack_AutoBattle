using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum UniqueGearSlot { Weapon, Armor, AccessoryOrPet }
    public enum UniqueGearEffectKind
    {
        None, MeleeHitArmor, FocusRangedCriticalChance, SearingTwilight,
        StoneGiantCuirass, SylphVest, BloodKnightHelm, ThornShellPauldron
    }

    public readonly struct UniqueGearEffect
    {
        public UniqueGearEffect(UniqueGearEffectKind kind, float amount) { Kind = kind; Amount = amount; }
        public UniqueGearEffectKind Kind { get; }
        public float Amount { get; }
        public const int BurnChancePercent = 50;
        public const int BurnStacks = 2;
        public const int AttacksPerBurnBurst = 5;
        public const int BurnBurstTicks = 3;
        public const double CuirassMinimumHealthRatio = .50;
        public const int CleanseStacks = 3;
        public const double HealthSacrificeRatio = .05;
        public const int ThornsChancePercent = 20;
        public string Describe(CharacterStatsSnapshot stats, string period = "2.0", string placementIcon = "◇") => Kind switch
        {
            UniqueGearEffectKind.MeleeHitArmor => $"모든 근접 무기가 명중할 때마다 방어도를 {stats.Attack * Amount:0.##} 얻습니다.",
            UniqueGearEffectKind.FocusRangedCriticalChance => $"집중 중첩당 모든 원거리 무기의 치명타 확률이 {Amount:0.##}% 증가합니다.",
            UniqueGearEffectKind.SearingTwilight => $"화상의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"이 무기와 {placementIcon}에 배치된 근접 무기로 적을 공격할 때마다 {BurnChancePercent}% 확률로 적에게 화상 {BurnStacks}중첩을 부여합니다.\n"
                + $"이 무기로 적을 {AttacksPerBurnBurst}번 공격할 때마다 적에게 즉시 화상의 초당 피해를 {BurnBurstTicks}회 적용합니다.",
            UniqueGearEffectKind.StoneGiantCuirass => $"현재 체력이 {CuirassMinimumHealthRatio:0%} 이상이면 {period}초마다 방어도를 {stats.MaximumHealth * Amount:0.##} 얻습니다.",
            UniqueGearEffectKind.SylphVest => $"신속의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"{period}초마다 무작위 디버프 하나를 {CleanseStacks}중첩 제거합니다. 디버프가 없으면 신속 1중첩을 얻습니다.",
            UniqueGearEffectKind.BloodKnightHelm => $"현재 체력이 {HealthSacrificeRatio:0%}를 초과하면 {period}초마다 체력을 {stats.MaximumHealth * HealthSacrificeRatio:0.##} 희생하고 방어도를 {stats.MaximumHealth * Amount:0.##} 얻습니다.",
            UniqueGearEffectKind.ThornShellPauldron => $"가시의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"적에게 피해를 입을 때마다 {ThornsChancePercent}% 확률로 가시 1중첩을 얻습니다.",
            _ => ""
        };
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Unique Gear", fileName = "UniqueGear_")]
    public sealed class UniqueGearData : ScriptableObject
    {
        [SerializeField] private ItemData _item;
        [SerializeField] private UniqueGearSlot _slot;
        [SerializeField] private Sprite _icon;
        [SerializeField, Min(0)] private float _iconInset;
        [SerializeField, Min(0.1f)] private float _iconScale = 1f;
        [SerializeField] private bool _bonusHealth;
        [SerializeField] private bool _bonusAttack;
        [SerializeField] private bool _bonusCriticalDamage;
        [SerializeField] private bool _bonusCriticalChance;
        [SerializeField, Min(0)] private float _commonBaseBonus = 5f;
        [SerializeField, Min(0)] private float _bonusPerEnhancement = .5f;
        [SerializeField] private UniqueGearEffectKind _effect;
        [SerializeField, Min(0)] private float _commonEffect;
        [SerializeField, Min(0)] private float _uncommonEffect;

        public string GearId => _item != null ? _item.ItemId : "";
        public ItemData Item => _item;
        public UniqueGearSlot Slot => _slot;
        public Sprite Icon => _icon;
        public float IconInset => _iconInset;
        public float IconScale => _iconScale;
        public bool BonusHealth => _bonusHealth;
        public bool BonusAttack => _bonusAttack;
        public bool BonusCriticalDamage => _bonusCriticalDamage;
        public bool BonusCriticalChance => _bonusCriticalChance;
        public bool FitsSlot => _item != null && _item.IsUniqueGear && (_slot switch
        {
            UniqueGearSlot.Weapon => _item.Category == ItemCategory.Weapon,
            UniqueGearSlot.Armor => _item.Category == ItemCategory.Armor,
            UniqueGearSlot.AccessoryOrPet => _item.Category is ItemCategory.Accessory or ItemCategory.Pet,
            _ => false
        });

        public static int MaxEnhancement(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common => 3,
            ItemRarity.Uncommon => 6,
            _ => -1
        };

        public static bool IsValidRank(ItemRarity rarity, int enhancement)
            => enhancement >= 0 && enhancement <= MaxEnhancement(rarity);

        public static double BonusPercent(ItemRarity rarity, int enhancement)
            => CalculateBonusPercent(rarity, enhancement, 5d, .5d);

        public double BonusPercentFor(ItemRarity rarity, int enhancement)
            => CalculateBonusPercent(rarity, enhancement, _commonBaseBonus, _bonusPerEnhancement);

        private static double CalculateBonusPercent(ItemRarity rarity, int enhancement, double commonBase, double perEnhancement)
        {
            if (!IsValidRank(rarity, enhancement)) throw new ArgumentOutOfRangeException(nameof(enhancement));
            int rankOffset = rarity == ItemRarity.Common ? 0 : MaxEnhancement(ItemRarity.Common);
            return commonBase + (rankOffset + enhancement) * perEnhancement;
        }

        public UniqueGearEffect EffectFor(ItemRarity rarity)
            => new(_effect, rarity == ItemRarity.Common ? _commonEffect : _uncommonEffect);
    }
}
