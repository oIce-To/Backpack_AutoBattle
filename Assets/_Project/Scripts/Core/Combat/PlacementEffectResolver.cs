using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Combat
{
    public sealed class PlacementEffectCell
    {
        public PlacementEffectCell(Vector2Int cell, int effectIndex, string target)
        { Cell = cell; EffectIndex = effectIndex; TargetInstanceId = target; }
        public Vector2Int Cell { get; }
        public int EffectIndex { get; }
        public string TargetInstanceId { get; }
        public bool ReceivesEffect => TargetInstanceId != null;
    }

    /// <summary>One eligibility path shared by hover/drag previews and actual combat bonuses.</summary>
    public static class PlacementEffectResolver
    {
        public static IReadOnlyList<PlacementEffectCell> GetCells(CombatLoadoutSnapshot loadout, CombatItemSnapshot source)
        {
            var result = new List<PlacementEffectCell>();
            for (int index = 0; index < source.PlacementEffects.Count; index++)
            {
                ItemPlacementEffect effect = source.PlacementEffects[index];
                foreach (Vector2Int cell in effect.GetWorldCells(source.Shape, source.Origin, source.Rotation))
                {
                    if (cell.x < 0 || cell.y < 0 || cell.x >= loadout.Width || cell.y >= loadout.Height) continue;
                    CombatItemSnapshot target = loadout.Items.FirstOrDefault(item => item.InstanceId != source.InstanceId && item.OccupiedCells.Contains(cell));
                    bool matches = target != null && effect.AttackDamageBonus != 0f
                        && target.Activation.HasEffect(ItemEffectKind.Attack)
                        && (effect.TargetFilter?.Matches(target.Category, target.Tags) ?? true);
                    result.Add(new PlacementEffectCell(cell, index, matches ? target.InstanceId : null));
                }
            }
            return result.AsReadOnly();
        }

        public static IReadOnlyDictionary<string, float> GetAttackBonuses(CombatLoadoutSnapshot loadout)
        {
            var bonuses = new Dictionary<string, float>();
            foreach (CombatItemSnapshot source in loadout.Items)
                foreach (var target in GetCells(loadout, source).Where(cell => cell.ReceivesEffect)
                    .GroupBy(cell => (cell.EffectIndex, cell.TargetInstanceId)))
                {
                    string id = target.Key.TargetInstanceId;
                    bonuses.TryGetValue(id, out float bonus);
                    bonuses[id] = bonus + source.PlacementEffects[target.Key.EffectIndex].AttackDamageBonus;
                }
            return bonuses;
        }
    }
}
