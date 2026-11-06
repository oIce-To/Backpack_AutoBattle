using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum UniqueGearSlot { Weapon, Armor, AccessoryOrPet }
    public enum UniqueGearEffectKind { None, MeleeHitArmor, FocusRangedCriticalChance }

    public readonly struct UniqueGearEffect
    {
        public UniqueGearEffect(UniqueGearEffectKind kind, float amount) { Kind = kind; Amount = amount; }
        public UniqueGearEffectKind Kind { get; }
        public float Amount { get; }
        public string Describe(CharacterStatsSnapshot stats) => Kind switch
        {
            UniqueGearEffectKind.MeleeHitArmor => $"모든 근접 무기가 명중할 때마다 방어도를 {stats.Attack * Amount:0.##} 얻습니다.",
            UniqueGearEffectKind.FocusRangedCriticalChance => $"집중 중첩당 모든 원거리 무기의 치명타 확률이 {Amount:0.##}% 증가합니다.",
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
        [SerializeField] private bool _bonusHealth;
        [SerializeField] private bool _bonusAttack;
        [SerializeField] private bool _bonusCriticalDamage;
        [SerializeField] private UniqueGearEffectKind _effect;
        [SerializeField, Min(0)] private float _commonEffect;
        [SerializeField, Min(0)] private float _uncommonEffect;

        public string GearId => _item != null ? _item.ItemId : "";
        public ItemData Item => _item;
        public UniqueGearSlot Slot => _slot;
        public Sprite Icon => _icon;
        public float IconInset => _iconInset;
        public bool BonusHealth => _bonusHealth;
        public bool BonusAttack => _bonusAttack;
        public bool BonusCriticalDamage => _bonusCriticalDamage;
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
        {
            if (!IsValidRank(rarity, enhancement)) throw new ArgumentOutOfRangeException(nameof(enhancement));
            return (rarity == ItemRarity.Common ? 5d : 6.5d) + enhancement * .5d;
        }

        public UniqueGearEffect EffectFor(ItemRarity rarity)
            => new(_effect, rarity == ItemRarity.Common ? _commonEffect : _uncommonEffect);
    }
}
