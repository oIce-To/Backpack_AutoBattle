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
                StatusStat.CriticalChance => $"모든 공격 아이템의 치명타 확률이 {percent:0.##}%p 증가합니다.",
                StatusStat.Accuracy => $"모든 공격 아이템의 명중률이 {percent:0.##}%p {(definition.IsDebuff ? "감소" : "증가")}합니다.",
                StatusStat.Evasion => $"회피율이 {percent:0.##}%p 증가합니다.",
                StatusStat.DamageTaken => $"받는 모든 피해가 {percent:0.#}% 증가합니다.",
                StatusStat.PeriodicDamage => $"{definition.TickIntervalSeconds:0.##}초마다 {combat.GetStatusDamagePerTick(side, kind):0.##} 피해를 받습니다."
                    + (definition.StacksLostPerTick > 0 ? $" 피해 후 {definition.StacksLostPerTick}중첩 감소합니다." : ""),
                StatusStat.ReflectedDamage => $"공격 아이템으로 피해를 받으면 공격자에게 {combat.GetStatusReactionAmount(side, kind):0.##} 피해를 반사합니다.",
                StatusStat.HealingOnAttack => $"공격 아이템으로 피해를 주면 체력을 {combat.GetStatusReactionAmount(side, kind):0.##} 회복합니다.",
                _ => ""
            };
            return new StatusTooltipContent
            {
                Title = $"{definition.DisplayName} · {stacks}중첩", Description = description,
                Color = definition.IsDebuff ? new Color(0.34f, 0.70f, 1f) : new Color(0.36f, 0.88f, 0.66f)
            };
        }
    }
}
