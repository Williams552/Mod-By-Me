namespace EchoResonance.Core
{
    public static class EchoTuning
    {
        // Core Resonator Constants
        public const int MaxPylonsPerResonator = 4;
        public const float PylonRadius = 12.0f;
        public const float MinPylonSpacing = 8.0f;
        public const float PylonMultiplierBonus = 0.5f; // Each pylon adds +50% (+0.50)

        // Base Echo Reward Values
        public const float EchoSkillLevel1_10 = 0.5f;
        public const float EchoSkillLevel11_15 = 1.5f;
        public const float EchoSkillLevel16_20 = 4.0f;
        public const float EchoFirstLevel20Bonus = 10.0f;
        public const float EchoCraftMasterwork = 1.0f;
        public const float EchoCraftLegendary = 3.0f;
        public const float EchoQuadrumSurvival = 3.0f;
        public const float EchoSuccessfulRitual = 1.0f;
        public const float EchoHighWildnessTame = 1.0f;
        public const float EchoPawnJoin = 2.0f;

        // Base Costs for Perk Tiers
        public const float Tier1BaseCost = 20.0f;
        public const float Tier2BaseCost = 60.0f;
        public const float Tier3BaseCost = 150.0f;

        // Discount & Scaling Defaults
        public const float EscalatingExponent = 1.6f;
        public const float SpecializationDiscountFactor = 0.75f;
        public const float TradeOffDiscountFactor = 0.60f; // -40% price
    }
}
