using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed class StatusTooltipContent
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public Color Color { get; private set; }

        public static StatusTooltipContent FromCombat(CombatSimulation combat, CombatSide side, StatusEffectKind kind)
        {
            int stacks = combat.GetStatusStacks(side, kind);
            if (stacks == 0) return null;
            CombatStatusDefinition definition = combat.GetStatusDefinition(kind);
            double percent = definition.PercentAt(stacks);
            string description = $"모든 아이템이 {percent:0.##}% {(definition.IsDebuff ? "감속" : "가속")}합니다.";
            // Keep the common clock's existing safety clamp explicit at extreme stack counts.
            if (definition.IsDebuff && percent > 95d) description += "\n실제 작동 속도는 최소 5%를 유지합니다.";
            if (!definition.IsDebuff && percent > 1900d) description += "\n실제 작동 속도는 최대 20배입니다.";
            return new StatusTooltipContent
            {
                Title = $"{definition.DisplayName} · {stacks}중첩", Description = description,
                Color = definition.IsDebuff ? new Color(0.34f, 0.70f, 1f) : new Color(0.36f, 0.88f, 0.66f)
            };
        }
    }
}
