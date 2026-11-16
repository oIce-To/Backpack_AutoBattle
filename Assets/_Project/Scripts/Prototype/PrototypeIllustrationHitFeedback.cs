using System;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Position-only feedback. Never changes the approved sprite, size, scale or import settings.</summary>
    public sealed class PrototypeIllustrationHitFeedback
    {
        public const double Duration = .15;
        private readonly RectTransform _rect;
        private readonly Vector2 _restPosition;
        private double _hitAt = double.NegativeInfinity;

        public PrototypeIllustrationHitFeedback(RectTransform rect)
        {
            _rect = rect;
            _restPosition = rect.anchoredPosition;
        }

        public void Show(CombatItemEvent attack, ItemCategory category)
        {
            if (attack.Kind != ItemEffectKind.Attack || category is not (ItemCategory.Weapon or ItemCategory.Pet)
                || !attack.Hit || (attack.HealthDamage <= 0 && attack.ArmorAbsorbed <= 0)) return;
            _hitAt = attack.Time;
        }

        public void Tick(double combatTime)
        {
            double elapsed = combatTime - _hitAt;
            float x = elapsed >= 0 && elapsed < Duration
                ? (float)Math.Round(Math.Sin(elapsed / Duration * Math.PI * 6) * 6 * (1 - elapsed / Duration)) : 0;
            _rect.anchoredPosition = _restPosition + new Vector2(x, 0);
        }

        public void Reset()
        {
            _hitAt = double.NegativeInfinity;
            _rect.anchoredPosition = _restPosition;
        }
    }
}
