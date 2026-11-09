using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Progression
{
    [Serializable]
    public sealed class BlueprintBalanceData
    {
        public string gearId;
        public long count;
    }

    public sealed class StageReward
    {
        public StageReward(long experience, long shards, IEnumerable<string> blueprintRolls = null)
        {
            if (experience < 0 || shards < 0) throw new ArgumentOutOfRangeException(nameof(shards));
            Experience = experience; OriginShards = shards;
            var rolls = (blueprintRolls ?? Array.Empty<string>()).ToArray();
            if (rolls.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Blueprint IDs are required.");
            Blueprints = Array.AsReadOnly(rolls.GroupBy(id => id, StringComparer.Ordinal)
                .Select(group => new KeyValuePair<string, long>(group.Key, group.LongCount())).ToArray());
        }
        public long Experience { get; }
        public long OriginShards { get; }
        public IReadOnlyList<KeyValuePair<string, long>> Blueprints { get; }
    }

    public sealed partial class PlayerProgress
    {
        public const int MaximumMineTickets = 5;
        public static readonly TimeSpan MineTicketInterval = TimeSpan.FromHours(8);
        public const long GearCraftShardCost = 500;
        public const long GearCraftBlueprintCost = 10;
        private static readonly long[] CommonEnhancementCosts = { 500, 700, 1000 };
        private static readonly long[] UncommonEnhancementCosts = { 2000, 2500, 3200, 4000, 5000, 6500 };
        private readonly Dictionary<string, long> _blueprints = new(StringComparer.Ordinal);
        private long _nextMineTicketUtcTicks, _ticketClockUtcTicks;
        private string _lastRewardClaim = "";
        public long OriginShards { get; private set; }
        public int MineTickets { get; private set; } = MaximumMineTickets;
        public long GetBlueprints(string gearId) => _blueprints.TryGetValue(gearId ?? "", out long count) ? count : 0;

        public void RefreshMineTickets(DateTimeOffset now)
        {
            long clock = Math.Max(now.UtcDateTime.Ticks, _ticketClockUtcTicks);
            _ticketClockUtcTicks = clock;
            if (MineTickets == MaximumMineTickets) { _nextMineTicketUtcTicks = 0; return; }
            if (_nextMineTicketUtcTicks == 0) _nextMineTicketUtcTicks = checked(clock + MineTicketInterval.Ticks);
            if (clock < _nextMineTicketUtcTicks) return;
            long gained = Math.Min(MaximumMineTickets - MineTickets, 1 + (clock - _nextMineTicketUtcTicks) / MineTicketInterval.Ticks);
            MineTickets += (int)gained;
            _nextMineTicketUtcTicks = MineTickets == MaximumMineTickets ? 0
                : checked(_nextMineTicketUtcTicks + gained * MineTicketInterval.Ticks);
        }

        public TimeSpan UntilNextMineTicket(DateTimeOffset now)
        {
            RefreshMineTickets(now);
            return MineTickets == MaximumMineTickets ? TimeSpan.Zero
                : TimeSpan.FromTicks(Math.Max(0, _nextMineTicketUtcTicks - _ticketClockUtcTicks));
        }

        public void RefillMineTickets(DateTimeOffset now)
        {
            _ticketClockUtcTicks = Math.Max(_ticketClockUtcTicks, now.UtcDateTime.Ticks);
            MineTickets = MaximumMineTickets;
            _nextMineTicketUtcTicks = 0;
        }

        // The caller saves this candidate together with the stage clear and inventory changes.
        public bool TryClaimReward(string claimId, StageReward reward, bool spendMineTicket, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(claimId) || reward == null) return false;
            if (_lastRewardClaim == claimId) return true;
            RefreshMineTickets(now);
            if (spendMineTicket && MineTickets == 0 || reward.Experience > long.MaxValue - SharedExperience
                || reward.OriginShards > long.MaxValue - OriginShards
                || reward.Blueprints.Any(pair => pair.Value > long.MaxValue - GetBlueprints(pair.Key))) return false;
            SharedExperience += reward.Experience;
            OriginShards += reward.OriginShards;
            foreach (var pair in reward.Blueprints) _blueprints[pair.Key] = GetBlueprints(pair.Key) + pair.Value;
            if (spendMineTicket)
            {
                if (MineTickets == MaximumMineTickets) _nextMineTicketUtcTicks = checked(_ticketClockUtcTicks + MineTicketInterval.Ticks);
                MineTickets--;
            }
            _lastRewardClaim = claimId;
            return true;
        }

        public UniqueGearInstance TryCraftGear(UniqueGearData definition)
        {
            if (definition == null || !definition.FitsSlot || OriginShards < GearCraftShardCost
                || GetBlueprints(definition.GearId) < GearCraftBlueprintCost) return null;
            var gear = AddGear(definition, ItemRarity.Common, 0);
            if (gear == null) return null;
            OriginShards -= GearCraftShardCost;
            _blueprints[definition.GearId] -= GearCraftBlueprintCost;
            return gear;
        }

        public static long EnhancementCost(UniqueGearInstance gear)
        {
            if (gear == null) return 0;
            long[] costs = gear.Rarity == ItemRarity.Common ? CommonEnhancementCosts
                : gear.Rarity == ItemRarity.Uncommon ? UncommonEnhancementCosts : Array.Empty<long>();
            return gear.Enhancement >= 0 && gear.Enhancement < costs.Length ? costs[gear.Enhancement] : 0;
        }

        public bool TryEnhanceGear(string instanceId)
        {
            if (instanceId == null || !_uniqueGear.TryGetValue(instanceId, out var gear)) return false;
            long cost = EnhancementCost(gear);
            if (cost == 0 || OriginShards < cost) return false;
            _uniqueGear[instanceId] = new UniqueGearInstance(gear.InstanceId, gear.GearId, gear.Rarity,
                gear.Enhancement + 1, gear.Slot, gear.EquippedBy);
            OriginShards -= cost;
            return true;
        }

        public IReadOnlyList<UniqueGearInstance> PromotionMaterials(string instanceId)
        {
            if (instanceId == null || !_uniqueGear.TryGetValue(instanceId, out var gear)) return Array.Empty<UniqueGearInstance>();
            return _uniqueGear.Values.Where(item => item.InstanceId != instanceId && item.GearId == gear.GearId
                && item.Rarity == gear.Rarity && item.Enhancement == 0 && string.IsNullOrEmpty(item.EquippedBy))
                .OrderBy(item => item.InstanceId, StringComparer.Ordinal).ToArray();
        }

        public bool TryPromoteGear(string instanceId, IReadOnlyList<string> materialIds)
        {
            if (instanceId == null || !_uniqueGear.TryGetValue(instanceId, out var gear)
                || gear.Rarity != ItemRarity.Common || gear.Enhancement != UniqueGearData.MaxEnhancement(gear.Rarity)
                || materialIds == null || materialIds.Count != 2 || materialIds[0] == materialIds[1]) return false;
            var allowed = new HashSet<string>(PromotionMaterials(instanceId).Select(item => item.InstanceId));
            if (materialIds.Any(id => !allowed.Contains(id))) return false;
            foreach (string id in materialIds) _uniqueGear.Remove(id);
            _uniqueGear[instanceId] = new UniqueGearInstance(gear.InstanceId, gear.GearId, ItemRarity.Uncommon, 0, gear.Slot, gear.EquippedBy);
            return true;
        }

        private void LoadEconomy(ProfileSaveData data)
        {
            if (data.originShards < 0 || data.mineTickets < 0 || data.mineTickets > MaximumMineTickets
                || data.ticketClockUtcTicks < 0 || data.ticketClockUtcTicks > DateTime.MaxValue.Ticks - MineTicketInterval.Ticks
                || data.nextMineTicketUtcTicks < 0 || data.nextMineTicketUtcTicks > DateTime.MaxValue.Ticks
                || data.mineTickets < MaximumMineTickets && data.nextMineTicketUtcTicks == 0)
                throw new FormatException("Invalid economy or ticket save.");
            OriginShards = data.originShards;
            MineTickets = data.mineTickets;
            _nextMineTicketUtcTicks = MineTickets == MaximumMineTickets ? 0 : data.nextMineTicketUtcTicks;
            _ticketClockUtcTicks = data.ticketClockUtcTicks;
            _lastRewardClaim = data.lastRewardClaim ?? "";
            foreach (var item in data.blueprints ?? new List<BlueprintBalanceData>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.gearId) || item.count < 0 || !_blueprints.TryAdd(item.gearId, item.count))
                    throw new FormatException("Invalid blueprint save.");
            }
        }
    }
}
