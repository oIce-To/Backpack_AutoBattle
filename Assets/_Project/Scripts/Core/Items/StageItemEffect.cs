namespace BackpackAutoBattle.Items
{
    // Serialized in ItemData assets. Append new effects to preserve the existing values.
    public enum StageItemEffect
    {
        None,
        PiggyBank,
        Clover,
        PlatinumCard,
        PlatinumClover,
        CursedClover,
        Potion,
        WornHelmet,
        WoodShield,
        IronThornHelmet,
        IronDagger,
        Spear,
        Quiver,
        Moonflower,
        BattleAxe,
        Halberd,
        Bola,
        DeceptionCloak,
        NightVeil,
        BurningCloak,
        SolarRadiance,
        IceHeart,
        RazorThorn,
        PoisonQuiver,
        VampireKnife,
        Mithril,
        Excalibur,
        MithrilArmor,
        MithrilShield,
        GenesisArmor,
        Dragonslayer,
        Hawkeye,
        TrueShot,
        WingedBoots,
        DragonsBreath,
        PrimalFlame,
        BloodRose,
        Eclipse,
        LunarEclipse,
        IronMaiden,
        BloodIronArmor,
        Permafrost,
        PoisonBook,
        NeedleStorm,
        SnowBloom,
    }

    public static class StageItemEffectRules
    {
        public const double PotionHealthThreshold = .50d;
        public const double PotionHealRatio = .15d;

        public static double StartingArmorRatio(this StageItemEffect effect) => effect switch
        {
            StageItemEffect.WornHelmet => .20d,
            StageItemEffect.IronThornHelmet => .30d,
            _ => 0d
        };
    }
}
