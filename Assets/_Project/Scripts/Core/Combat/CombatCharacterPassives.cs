using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public sealed partial class CombatSimulation
    {
        public const string CharacterPassiveSourceId = "$character-passive";

        private void InitializeCharacterPassives()
        {
            InitializeCharacterPassive(CombatSide.Player, Player);
            InitializeCharacterPassive(CombatSide.Enemy, Enemy);
        }

        private void InitializeCharacterPassive(CombatSide side, CombatantSnapshot owner)
        {
            var passive = owner.Passive;
            if (passive.Kind == CharacterPassiveKind.None) return;
            _itemCounters.Add((side, CharacterPassiveSourceId), new ItemCounter { Side = side, Id = CharacterPassiveSourceId,
                Name = $"캐릭터 패시브 · {passive.DisplayName}" });
            if (passive.Kind != CharacterPassiveKind.LostLifeArmor) return;
            double armor = owner.LostLives * (owner.MaximumHealth * passive.ArmorPercentPerLostLife / 100d);
            AddArmor(side, CharacterPassiveSourceId, ApplyArmorBonus(side, armor));
        }

        private void ApplyAttackPassive(CombatWeapon weapon, CombatSide side)
        {
            var passive = (side == CombatSide.Player ? Player : Enemy).Passive;
            if (passive.Kind != CharacterPassiveKind.RangedAttackFocus || weapon.Snapshot.Category != ItemCategory.Weapon
                || (weapon.Snapshot.Tags & ItemTags.Ranged) == 0 || passive.FocusStacks == 0 || passive.FocusChancePercent <= 0) return;
            // Roll after this attack's accuracy/critical result; its newly gained focus affects subsequent attacks.
            // Accuracy misses, evades, and fully blocked attacks all count as ranged attack attempts.
            if (_random.NextDouble() * 100d < passive.FocusChancePercent)
                ApplyStatus(side, StatusEffectKind.Focus, passive.FocusStacks, side, CharacterPassiveSourceId);
        }
    }
}
