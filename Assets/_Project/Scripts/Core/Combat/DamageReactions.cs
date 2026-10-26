using System;
using BackpackAutoBattle.Items;

namespace BackpackAutoBattle.Combat
{
    public enum DamageReactionTrigger { AttackDamageDealt, AttackDamageReceived }
    public enum CombatReactionKind { HealSelf, ReflectDamage }

    /// <summary>Item-independent reaction rules. Both HP damage and armor absorption qualify.
    /// StatusEffect/Other damage never enters this path, so reflected damage cannot recurse.</summary>
    public sealed class CombatDamageReactionRule
    {
        public StatusEffectKind Status { get; }
        public DamageReactionTrigger Trigger { get; }
        public CombatReactionKind Effect { get; }
        public CombatDamageReactionRule(StatusEffectKind status, DamageReactionTrigger trigger, CombatReactionKind effect)
        { Status = status; Trigger = trigger; Effect = effect; }
        public bool Matches(CombatDamageEvent damage) => damage.Kind == CombatDamageKind.AttackItem
            && damage.Resolution.ArmorAbsorbed + damage.Resolution.HealthDamage > 0;
    }

    public sealed class CombatReactionEvent
    {
        public double Time { get; }
        public CombatSide Owner { get; }
        public CombatSide Target { get; }
        public StatusEffectKind Status { get; }
        public CombatReactionKind Kind { get; }
        public string TriggeringItemId { get; }
        public double Amount { get; }
        public double HealthRestored { get; }
        public DamageResolution Damage { get; }
        public CombatReactionEvent(double time, CombatSide owner, CombatSide target, StatusEffectKind status,
            CombatReactionKind kind, string itemId, double amount, double restored = 0, DamageResolution damage = default)
        {
            Time = time; Owner = owner; Target = target; Status = status; Kind = kind;
            TriggeringItemId = itemId; Amount = amount; HealthRestored = restored; Damage = damage;
        }
    }

    public sealed partial class CombatSimulation
    {
        // Heal before retaliation: never resurrect an attacker killed by thorns, and never exceed max HP.
        private static readonly CombatDamageReactionRule[] DamageReactions =
        {
            new(StatusEffectKind.Lifesteal, DamageReactionTrigger.AttackDamageDealt, CombatReactionKind.HealSelf),
            new(StatusEffectKind.Thorns, DamageReactionTrigger.AttackDamageReceived, CombatReactionKind.ReflectDamage)
        };
        public event Action<CombatReactionEvent> ReactionTriggered;

        public double GetStatusReactionAmount(CombatSide owner, StatusEffectKind status)
            => GetStatusDefinition(status).AmountPerStack((owner == CombatSide.Player ? Player : Enemy).Stats)
                * GetStatusStacks(owner, status);

        private void ResolveDamageReactions(CombatDamageEvent attack)
        {
            foreach (CombatDamageReactionRule rule in DamageReactions)
            {
                if (!rule.Matches(attack)) continue;
                CombatSide owner = rule.Trigger == DamageReactionTrigger.AttackDamageDealt ? attack.Source : attack.Target;
                double amount = GetStatusReactionAmount(owner, rule.Status);
                if (amount <= 0) continue;
                if (rule.Effect == CombatReactionKind.HealSelf)
                {
                    double health = owner == CombatSide.Player ? PlayerHealth : EnemyHealth;
                    if (health <= 0) continue;
                    double maximum = owner == CombatSide.Player ? PlayerMaxHealth : EnemyMaxHealth;
                    double healed = Math.Min(amount, maximum - health);
                    if (owner == CombatSide.Player) PlayerHealth += healed; else EnemyHealth += healed;
                    CreditHealing(owner, healed, StatusWeights(owner, rule.Status));
                    ReactionTriggered?.Invoke(new CombatReactionEvent(ElapsedSeconds, owner, owner, rule.Status,
                        rule.Effect, attack.SourceItemId, amount, healed));
                }
                else
                {
                    // A defender killed by this attack still retaliates. Do not resolve victory here.
                    DamageResolution damage = DealDamage(owner, attack.Source, amount, CombatDamageKind.Other, null, StatusWeights(owner, rule.Status), StatusEffectKind.Thorns);
                    ReactionTriggered?.Invoke(new CombatReactionEvent(ElapsedSeconds, owner, attack.Source, rule.Status,
                        rule.Effect, attack.SourceItemId, amount, damage: damage));
                }
            }
        }
    }
}
