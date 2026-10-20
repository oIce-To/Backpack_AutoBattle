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
            long stacks = combat.GetStatusStacks(side, kind);
            if (stacks == 0) return null;
            CombatStatusDefinition definition = combat.GetStatusDefinition(kind);
            double percent = definition.PercentAt(stacks);
            string description = definition.AffectedStat switch
            {
                StatusStat.ActivationSpeed => $"모든 아이템이 {percent:0.##}% {(definition.IsDebuff ? "감속" : "가속")}합니다.",
                StatusStat.CriticalChance => $"모든 공격 아이템의 치명타 확률이 {percent:0.##}%p 증가합니다.\n100%를 넘어도 치명타는 한 번만 적용됩니다.",
                StatusStat.Accuracy => $"모든 공격 아이템의 명중률이 {percent:0.##}%p {(definition.IsDebuff ? "감소" : "증가")}합니다.\n실제 명중률은 30~100%입니다.",
                StatusStat.Evasion => $"회피율이 {percent:0.##}%p 증가합니다.\n현재 회피율: {combat.GetEvasionPercent(side):0.##}% (최대 70%)\n중첩 수는 상한 없이 유지됩니다.\n명중 실패도 회피 시 효과를 발동합니다.\n화상·중독·가시 피해는 회피하지 못합니다.",
                _ => ""
            };
            // Keep the common clock's existing safety clamp explicit at extreme stack counts.
            if (definition.AffectedStat == StatusStat.ActivationSpeed)
            {
                if (definition.IsDebuff && percent > 95d) description += "\n실제 작동 속도는 최소 5%를 유지합니다.";
                if (!definition.IsDebuff && percent > 1900d) description += "\n실제 작동 속도는 최대 20배입니다.";
            }
            return new StatusTooltipContent
            {
                Title = $"{definition.DisplayName} · {stacks}중첩", Description = description,
                Color = definition.IsDebuff ? new Color(0.34f, 0.70f, 1f) : new Color(0.36f, 0.88f, 0.66f)
            };
        }
    }
}
