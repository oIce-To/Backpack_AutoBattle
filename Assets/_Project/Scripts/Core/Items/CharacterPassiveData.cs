using System;
using UnityEngine;

namespace BackpackAutoBattle.Items
{
    public enum CharacterPassiveKind { None, LostLifeArmor, RangedAttackFocus }

    /// <summary>Captured once at stage entry. Combat never reads mutable character/passive assets.</summary>
    public sealed class CharacterPassiveSnapshot
    {
        public static CharacterPassiveSnapshot None { get; } = new(CharacterPassiveKind.None, "", "");
        public CharacterPassiveSnapshot(CharacterPassiveKind kind, string id, string name, int bonusLives = 0,
            double armorPercentPerLostLife = 0, double focusChancePercent = 0, int focusStacks = 0)
        {
            if (!Enum.IsDefined(typeof(CharacterPassiveKind), kind) || bonusLives < 0 || focusStacks < 0
                || !double.IsFinite(armorPercentPerLostLife) || armorPercentPerLostLife < 0
                || !double.IsFinite(focusChancePercent) || focusChancePercent < 0 || focusChancePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind != CharacterPassiveKind.None && string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A passive ID is required.", nameof(id));
            Kind = kind; Id = id ?? ""; DisplayName = name ?? "";
            BonusLives = kind == CharacterPassiveKind.None ? 0 : bonusLives; ArmorPercentPerLostLife = armorPercentPerLostLife;
            FocusChancePercent = focusChancePercent; FocusStacks = focusStacks;
        }
        public CharacterPassiveKind Kind { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public int BonusLives { get; }
        public double ArmorPercentPerLostLife { get; }
        public double FocusChancePercent { get; }
        public int FocusStacks { get; }
    }

    [CreateAssetMenu(menuName = "Backpack AutoBattle/Character Passive", fileName = "Passive_")]
    public sealed class CharacterPassiveData : ScriptableObject
    {
        [SerializeField] private string _passiveId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private CharacterPassiveKind _kind;
        [SerializeField, Min(0)] private int _bonusLives;
        [SerializeField, Min(0)] private float _armorPercentPerLostLife;
        [SerializeField, Range(0, 100)] private float _focusChancePercent;
        [SerializeField, Min(0)] private int _focusStacks;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public CharacterPassiveSnapshot Capture() => new(_kind, _passiveId, _displayName,
            _bonusLives, _armorPercentPerLostLife, _focusChancePercent, _focusStacks);
        public string Description => _kind switch
        {
            CharacterPassiveKind.LostLifeArmor => $"해당 캐릭터의 라이프가 {_bonusLives} 증가합니다.\n\n매 라운드 전투를 시작할 때, 잃은 라이프 하나당 최대 체력의 {_armorPercentPerLostLife:0.#}%만큼 방어도를 얻습니다.",
            CharacterPassiveKind.RangedAttackFocus => $"원거리 무기로 공격할 때마다 {_focusChancePercent:0.#}% 확률로 집중 {_focusStacks}중첩을 얻습니다.",
            _ => ""
        };
    }
}
