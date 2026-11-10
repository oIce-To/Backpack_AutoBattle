using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Progression
{
    [Serializable]
    public sealed class UniqueGearSaveData
    {
        public string instanceId;
        public string gearId;
        public ItemRarity rarity;
        public int enhancement;
        public string equippedBy;
        public UniqueGearSlot slot;
        public bool isLocked;
    }

    public sealed class UniqueGearInstance
    {
        internal UniqueGearInstance(string instanceId, string gearId, ItemRarity rarity, int enhancement,
            UniqueGearSlot slot, string equippedBy, bool isLocked = false)
        {
            InstanceId = instanceId; GearId = gearId; Rarity = rarity; Enhancement = enhancement;
            Slot = slot; EquippedBy = equippedBy; IsLocked = isLocked;
        }
        public string InstanceId { get; }
        public string GearId { get; }
        public ItemRarity Rarity { get; }
        public int Enhancement { get; }
        public UniqueGearSlot Slot { get; }
        public string EquippedBy { get; }
        public bool IsLocked { get; }
        internal UniqueGearInstance WithOwner(string owner) => new(InstanceId, GearId, Rarity, Enhancement, Slot, owner, IsLocked);
        internal UniqueGearInstance WithLock(bool locked) => new(InstanceId, GearId, Rarity, Enhancement, Slot, EquippedBy, locked);
        internal UniqueGearSaveData Export() => new()
        {
            instanceId = InstanceId, gearId = GearId, rarity = Rarity, enhancement = Enhancement,
            slot = Slot, equippedBy = EquippedBy, isLocked = IsLocked
        };
    }

    public sealed partial class PlayerProgress
    {
        private readonly HashSet<string> _acquiredCharacters = new(StringComparer.Ordinal);
        private readonly Dictionary<string, UniqueGearInstance> _uniqueGear = new(StringComparer.Ordinal);
        public IReadOnlyCollection<UniqueGearInstance> UniqueGear => _uniqueGear.Values;

        public bool HasCharacter(string id) => _acquiredCharacters.Contains(id);

        public bool AcquireCharacter(CharacterData character)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId) || HasCharacter(character.CharacterId)) return false;
            var starter = character.StarterGear;
            if (starter != null)
            {
                UniqueGearInstance gear = AddGear(starter, ItemRarity.Common, 0);
                if (gear == null) return false;
                _uniqueGear[gear.InstanceId] = gear.WithOwner(character.CharacterId);
            }
            return _acquiredCharacters.Add(character.CharacterId);
        }

        public UniqueGearInstance AddGear(UniqueGearData definition, ItemRarity rarity, int enhancement)
        {
            if (definition == null || !definition.FitsSlot || string.IsNullOrWhiteSpace(definition.GearId)
                || !UniqueGearData.IsValidRank(rarity, enhancement)) return null;
            var gear = new UniqueGearInstance(Guid.NewGuid().ToString("N"), definition.GearId, rarity, enhancement, definition.Slot, "");
            _uniqueGear.Add(gear.InstanceId, gear);
            return gear;
        }

        public UniqueGearInstance GetEquippedGear(string characterId, UniqueGearSlot slot)
            => string.IsNullOrEmpty(characterId) ? null : _uniqueGear.Values.FirstOrDefault(item => item.EquippedBy == characterId && item.Slot == slot);

        public bool TrySetGearLocked(string instanceId, bool locked)
        {
            if (instanceId == null || !_uniqueGear.TryGetValue(instanceId, out var gear)) return false;
            _uniqueGear[instanceId] = gear.WithLock(locked);
            return true;
        }

        public bool TryEquipGear(string characterId, UniqueGearSlot slot, string instanceId, UniqueGearData definition,
            string expectedOwner = "")
        {
            if (!HasCharacter(characterId) || string.IsNullOrEmpty(instanceId) || !_uniqueGear.TryGetValue(instanceId, out var gear)
                || gear.EquippedBy != expectedOwner || gear.EquippedBy == characterId || definition == null || !definition.FitsSlot
                || definition.GearId != gear.GearId || gear.Slot != slot || definition.Slot != slot) return false;
            var replaced = GetEquippedGear(characterId, slot);
            if (replaced != null) _uniqueGear[replaced.InstanceId] = replaced.WithOwner("");
            _uniqueGear[instanceId] = gear.WithOwner(characterId);
            return true;
        }

        public bool TryUnequipGear(string characterId, UniqueGearSlot slot)
        {
            var equipped = GetEquippedGear(characterId, slot);
            if (equipped == null) return false;
            _uniqueGear[equipped.InstanceId] = equipped.WithOwner("");
            return true;
        }

        private void LoadUniqueGear(ProfileSaveData data)
        {
            foreach (string id in data.acquiredCharacters ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(id)) throw new FormatException("Invalid acquired character.");
                _acquiredCharacters.Add(id);
            }
            var occupiedSlots = new HashSet<(string, UniqueGearSlot)>();
            foreach (var item in data.uniqueGear ?? new List<UniqueGearSaveData>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.instanceId) || string.IsNullOrWhiteSpace(item.gearId)
                    || !Enum.IsDefined(typeof(UniqueGearSlot), item.slot) || !UniqueGearData.IsValidRank(item.rarity, item.enhancement))
                    throw new FormatException("Invalid unique gear.");
                string owner = item.equippedBy ?? "";
                if (owner.Length > 0 && (!HasCharacter(owner) || !occupiedSlots.Add((owner, item.slot))))
                    throw new FormatException("Invalid unique gear assignment.");
                var gear = new UniqueGearInstance(item.instanceId, item.gearId, item.rarity, item.enhancement, item.slot, owner,
                    data.version >= 5 && item.isLocked);
                if (!_uniqueGear.TryAdd(gear.InstanceId, gear)) throw new FormatException("Duplicate unique gear.");
            }
        }
    }
}
