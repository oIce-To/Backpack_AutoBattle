using System;

namespace BackpackAutoBattle.Combat
{
    public readonly struct DamageResolution
    {
        public DamageResolution(double armor, double health, double absorbed, double healthDamage)
        { ArmorRemaining = armor; HealthRemaining = health; ArmorAbsorbed = absorbed; HealthDamage = healthDamage; }
        public double ArmorRemaining { get; }
        public double HealthRemaining { get; }
        public double ArmorAbsorbed { get; }
        public double HealthDamage { get; }
    }

    public static class DamageResolver
    {
        public static DamageResolution Resolve(double damage, double armor, double health, double armorDamageFactor = 1d)
        {
            foreach (double value in new[] { damage, armor, health })
                if (value < 0d || double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(damage));
            if (!double.IsFinite(armorDamageFactor) || armorDamageFactor < 1d) throw new ArgumentOutOfRangeException(nameof(armorDamageFactor));
            double absorbed = Math.Min(armor, damage * armorDamageFactor);
            double healthDamage = Math.Min(health, Math.Max(0d, damage - absorbed / armorDamageFactor));
            return new DamageResolution(armor - absorbed, health - healthDamage, absorbed, healthDamage);
        }
    }
}
