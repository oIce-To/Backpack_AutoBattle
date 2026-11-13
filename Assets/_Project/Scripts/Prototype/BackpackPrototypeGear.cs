using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class StartingGear
    {
        internal StartingGear(UniqueGearInstance instance, UniqueGearData definition)
        {
            InstanceId = $"unique_{(int)instance.Slot}_{instance.InstanceId}";
            Item = definition.Item;
            Rarity = instance.Rarity;
            Effect = definition.EffectFor(Rarity);
        }
        internal string InstanceId { get; }
        internal ItemData Item { get; }
        internal ItemRarity Rarity { get; }
        internal UniqueGearEffect Effect { get; }
    }

    public sealed partial class BackpackPrototypeController
    {
        private PrototypeEquipmentScreen _equipment;
        private IEnumerable<CharacterData> CatalogCharacters => (_adventureCatalog?.Characters ?? new[] { _characterData })
            .Where(character => character != null).Distinct();

        private bool AcquireInitialCharacters()
        {
            bool changed = false;
            foreach (var character in CatalogCharacters)
                if (_adventureCatalog == null || character.InitiallyAvailable)
                    changed |= _progress.AcquireCharacter(character);
            return changed;
        }

        public UniqueGearData FindGearDefinition(string gearId)
            => _adventureCatalog?.UniqueGear.FirstOrDefault(definition => definition != null && definition.FitsSlot && definition.GearId == gearId)
                ?? CatalogCharacters.Select(character => character.StarterGear)
                    .FirstOrDefault(definition => definition != null && definition.FitsSlot && definition.GearId == gearId);

        private IEnumerable<(UniqueGearInstance instance, UniqueGearData definition)> EquippedGear(CharacterData character)
        {
            if (character == null) yield break;
            foreach (UniqueGearSlot slot in Enum.GetValues(typeof(UniqueGearSlot)))
            {
                var instance = _progress.GetEquippedGear(character.CharacterId, slot);
                if (instance == null) continue;
                var definition = FindGearDefinition(instance.GearId);
                if (definition != null && definition.Slot == slot) yield return (instance, definition);
            }
        }

        public CharacterStatsSnapshot GetCharacterStats(CharacterData character)
        {
            var stats = character?.CaptureStats(_progress.GetCharacterLevel(character.CharacterId)) ?? CharacterStatsSnapshot.Default;
            double health = 0, attack = 0, criticalDamage = 0, criticalChance = 0, armorGain = 0, healing = 0;
            foreach (var (instance, definition) in EquippedGear(character))
            {
                double bonus = definition.BonusPercentFor(instance.Rarity, instance.Enhancement);
                if (definition.BonusHealth) health += bonus;
                if (definition.BonusAttack) attack += bonus;
                if (definition.BonusCriticalDamage) criticalDamage += bonus;
                if (definition.BonusCriticalChance) criticalChance += bonus;
                if (definition.BonusArmorGain) armorGain += bonus;
                if (definition.BonusHealing) healing += bonus;
            }
            return new CharacterStatsSnapshot(stats.MaximumHealth * (1 + health / 100),
                stats.Attack * (1 + attack / 100), criticalDamage, criticalChance, armorGain, healing);
        }

        public bool TryEquipUniqueGear(UniqueGearSlot slot, string instanceId, string expectedOwner = "")
        {
            if (!CanChangeEquipment) return false;
            var item = _progress.UniqueGear.FirstOrDefault(gear => gear.InstanceId == instanceId);
            if (item == null) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            return candidate.TryEquipGear(_characterData.CharacterId, slot, instanceId, FindGearDefinition(item.GearId), expectedOwner)
                && SaveEquipment(candidate);
        }

        public bool TryUnequipUniqueGear(UniqueGearSlot slot)
        {
            if (!CanChangeEquipment) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            return candidate.TryUnequipGear(_characterData.CharacterId, slot) && SaveEquipment(candidate);
        }

        private bool CanChangeEquipment => IsMainMenu && !IsSettingsOpen && !_rewardPopup.IsOpen
            && !_mainMenu.IsPopupOpen && !_stageSelection.IsEntryOpen && _progressStore != null && _characterData != null;

        public bool TrySetUniqueGearLocked(string instanceId, bool locked)
        {
            if (!CanChangeEquipment) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            if (!candidate.TrySetGearLocked(instanceId, locked) || !SaveEquipment(candidate, refreshScreen: false)) return false;
            _equipment.RefreshGearLockState();
            return true;
        }

        public bool TryDismantleUniqueGear(IReadOnlyList<string> instanceIds)
        {
            if (!CanChangeEquipment) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            return candidate.TryDismantleGear(instanceIds) && SaveEquipment(candidate);
        }

        private bool SaveEquipment(PlayerProgress candidate, bool refreshScreen = true)
        {
            if (!_progressStore.TrySave(candidate, out _saveNotice))
            {
                _equipment.ShowError(_saveNotice);
                if (ActiveMainTab == MainMenuTab.Home) _mainMenu.ShowError(_saveNotice);
                return false;
            }
            _progress = candidate;
            if (refreshScreen) ShowMainMenuTab(ActiveMainTab);
            return true;
        }
    }
}
