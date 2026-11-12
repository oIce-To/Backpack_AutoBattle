using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum UniqueGearSlot { Weapon, Armor, AccessoryOrPet }
    public enum UniqueGearEffectKind
    {
        None, MeleeHitArmor, FocusRangedCriticalChance, SearingTwilight,
        StoneGiantCuirass, SylphVest, BloodKnightHelm, ThornShellPauldron,
        FrostDawnFragment, WhiteNightLantern, StoneGuardianIdol, ExecutionersSpikedBall,
        PhoenixFeather, SnowflakeRabbit, CharmAmulet
    }

    public readonly struct UniqueGearEffect
    {
        public UniqueGearEffect(UniqueGearEffectKind kind, float amount) { Kind = kind; Amount = amount; }
        public UniqueGearEffectKind Kind { get; }
        public float Amount { get; }
        public const int BurnChancePercent = 50;
        public const int BurnStacks = 2;
        public const int BurnBurstTicks = 3;
        public const int FixedCooldownSeconds = 10;
        public const int FreezeStacks = 2;
        public const double FreezeCriticalChancePerStack = .5d;
        public const double FreezeCriticalDamagePerStack = 3d;
        public const double ArmorPerPlagueStack = .01d;
        public const double PoisonPowerPerPlagueStack = .002d;
        public const int ExecutionerThornsChancePercent = 30;
        public const int BuffStealCount = 3;
        public bool HasFixedCycle => Kind is UniqueGearEffectKind.SearingTwilight or UniqueGearEffectKind.WhiteNightLantern;
        public const double CuirassMinimumHealthRatio = .50;
        public const int CleanseStacks = 3;
        public const double HealthSacrificeRatio = .05;
        public const int ThornsChancePercent = 20;
        public string Describe(CharacterStatsSnapshot stats, string period = "2.0", string placementIcon = "◇") => Kind switch
        {
            UniqueGearEffectKind.MeleeHitArmor => $"모든 근접 무기가 명중할 때마다 방어도를 {stats.Attack * Amount * stats.ArmorGainMultiplier:0.##} 얻습니다.",
            UniqueGearEffectKind.FocusRangedCriticalChance => $"집중 중첩당 모든 원거리 무기의 치명타 확률이 {Amount:0.##}% 증가합니다.",
            UniqueGearEffectKind.SearingTwilight => $"화상의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"이 무기와 {placementIcon}에 배치된 근접 무기로 적을 공격할 때마다 {BurnChancePercent}% 확률로 적에게 화상 {BurnStacks}중첩을 부여합니다.\n"
                + $"태양의 분노 · 고정 쿨타임 {FixedCooldownSeconds}초\n적에게 즉시 화상의 초당 피해를 {BurnBurstTicks}회 적용합니다.",
            UniqueGearEffectKind.StoneGiantCuirass => $"현재 체력이 {CuirassMinimumHealthRatio:0%} 이상이면 {period}초마다 방어도를 {stats.MaximumHealth * Amount * stats.ArmorGainMultiplier:0.##} 얻습니다.",
            UniqueGearEffectKind.SylphVest => $"신속의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"{period}초마다 무작위 디버프 하나를 {CleanseStacks}중첩 제거합니다. 디버프가 없으면 신속 1중첩을 얻습니다.",
            UniqueGearEffectKind.BloodKnightHelm => $"현재 체력이 {HealthSacrificeRatio:0%}를 초과하면 {period}초마다 체력을 {stats.MaximumHealth * HealthSacrificeRatio:0.##} 희생하고 방어도를 {stats.MaximumHealth * Amount * stats.ArmorGainMultiplier:0.##} 얻습니다.",
            UniqueGearEffectKind.ThornShellPauldron => $"가시의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"적에게 피해를 입을 때마다 {ThornsChancePercent}% 확률로 가시 1중첩을 얻습니다.",
            UniqueGearEffectKind.FrostDawnFragment => $"빙결의 효과가 {Amount:0.##%} 증가합니다.\n"
                + $"적을 공격할 때마다 빙결 {FreezeStacks}중첩을 부여합니다.\n"
                + $"이 무기와 {placementIcon}의 아이템은 적의 빙결 1중첩당 치명타 확률이 {FreezeCriticalChancePerStack:0.##}%p, 치명타 피해가 {FreezeCriticalDamagePerStack:0.##}%p 증가합니다.",
            UniqueGearEffectKind.WhiteNightLantern => $"중독의 효과가 {Amount:0.##%} 증가합니다.\n"
                + "적을 공격할 때마다 중독 1중첩을 부여합니다.\n"
                + $"장송의 빛 · 고정 쿨타임 {FixedCooldownSeconds}초\n적의 중독 중첩 절반을 희생해 스테이지 동안 유지되는 자신의 역병으로 바꿉니다. "
                + $"이후 자신의 역병 1중첩당 방어도를 {stats.Attack * ArmorPerPlagueStack * stats.ArmorGainMultiplier:0.###} 얻습니다.",
            UniqueGearEffectKind.StoneGuardianIdol => $"{placementIcon}의 아이템은 공격력이 자신의 현재 방어도의 {Amount:0.##%}만큼 증가합니다.",
            UniqueGearEffectKind.ExecutionersSpikedBall => $"자신의 가시 1중첩당 적의 치유 효율을 {Amount:0.##%} 감소시킵니다.\n"
                + $"{placementIcon}의 아이템이 적을 공격할 때마다 {ExecutionerThornsChancePercent}% 확률로 가시 1중첩을 얻습니다.",
            UniqueGearEffectKind.PhoenixFeather => $"화상 피해가 치명타를 낼 수 있습니다. 적의 화상 1중첩당 화상의 치명타 확률이 {Amount:0.##%} 증가합니다.",
            UniqueGearEffectKind.SnowflakeRabbit => $"적을 공격할 때마다 빙결 {FreezeStacks}중첩을 부여하고, 적의 빙결 1중첩당 체력을 {stats.Attack * Amount * stats.HealingMultiplier:0.###} 회복합니다.",
            UniqueGearEffectKind.CharmAmulet => $"{period}초마다 적의 버프 {BuffStealCount}중첩을 강탈합니다.",
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
        [SerializeField] private bool _bonusArmorGain;
        [SerializeField] private bool _bonusHealing;
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
        public bool BonusArmorGain => _bonusArmorGain;
        public bool BonusHealing => _bonusHealing;
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
