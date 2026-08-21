using UnityEngine;
using Verse;

namespace LoneSurvivor
{
    public class LoneSurvivorSettings : ModSettings
    {
        public int maxColonistsThreshold = 5;
        public float maxWorkSpeedBonus = 2.0f;        // +200% at 1 pawn (3x normal work speed)
        public float maxLearningBonus = 1.0f;         // +100% at 1 pawn (2x learning rate)
        public float maxRestFallReduction = 0.50f;     // -50% rest fall rate at 1 pawn (sleep half as much)
        public float maxMoveSpeedBonus = 0.0f;        // +0 c/s bonus (optional)
        public float maxImmunityBonus = 0.0f;         // +0% immunity speed bonus (optional)
        public bool countPerMapOnly = false;          // False: count entire faction free colonists; True: count current map only
        public int checkIntervalTicks = 2000;         // ~33.3 seconds at 1x speed

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxColonistsThreshold, "maxColonistsThreshold", 5);
            Scribe_Values.Look(ref maxWorkSpeedBonus, "maxWorkSpeedBonus", 2.0f);
            Scribe_Values.Look(ref maxLearningBonus, "maxLearningBonus", 1.0f);
            Scribe_Values.Look(ref maxRestFallReduction, "maxRestFallReduction", 0.50f);
            Scribe_Values.Look(ref maxMoveSpeedBonus, "maxMoveSpeedBonus", 0.0f);
            Scribe_Values.Look(ref maxImmunityBonus, "maxImmunityBonus", 0.0f);
            Scribe_Values.Look(ref countPerMapOnly, "countPerMapOnly", false);
            Scribe_Values.Look(ref checkIntervalTicks, "checkIntervalTicks", 2000);
        }

        public void ResetToDefaults()
        {
            maxColonistsThreshold = 5;
            maxWorkSpeedBonus = 2.0f;
            maxLearningBonus = 1.0f;
            maxRestFallReduction = 0.50f;
            maxMoveSpeedBonus = 0.0f;
            maxImmunityBonus = 0.0f;
            countPerMapOnly = false;
            checkIntervalTicks = 2000;
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("Lone Survivor - Configuration");
            Text.Font = GameFont.Small;
            listing.GapLine();

            // Threshold
            listing.Label($"Colony Population Threshold (N): {maxColonistsThreshold} colonists");
            listing.Label("  (Buff applies when free colonists < N, and reaches 0% when population reaches N)");
            maxColonistsThreshold = Mathf.RoundToInt(listing.Slider(maxColonistsThreshold, 2, 15));
            listing.Gap(6f);

            // Work Speed
            listing.Label($"Solo Global Work Speed Bonus: +{(maxWorkSpeedBonus * 100f):F0}% ({100f + maxWorkSpeedBonus * 100f:F0}% total work speed)");
            maxWorkSpeedBonus = listing.Slider(maxWorkSpeedBonus, 0.0f, 5.0f);
            listing.Gap(6f);

            // Learning Bonus
            listing.Label($"Solo Global Learning Factor Bonus: +{(maxLearningBonus * 100f):F0}%");
            maxLearningBonus = listing.Slider(maxLearningBonus, 0.0f, 3.0f);
            listing.Gap(6f);

            // Rest Fall Reduction
            listing.Label($"Solo Rest Fall Rate Reduction: -{(maxRestFallReduction * 100f):F0}% (Colonist needs {(1f - maxRestFallReduction) * 100f:F0}% sleep)");
            maxRestFallReduction = listing.Slider(maxRestFallReduction, 0.0f, 0.90f);
            listing.Gap(6f);

            // Move Speed Bonus
            listing.Label($"Solo Movement Speed Bonus (Optional): +{maxMoveSpeedBonus:F2} c/s");
            maxMoveSpeedBonus = listing.Slider(maxMoveSpeedBonus, 0.0f, 2.0f);
            listing.Gap(6f);

            // Immunity Gain Bonus
            listing.Label($"Solo Immunity Gain Speed Bonus (Optional): +{(maxImmunityBonus * 100f):F0}%");
            maxImmunityBonus = listing.Slider(maxImmunityBonus, 0.0f, 1.0f);
            listing.Gap(6f);

            listing.GapLine();

            // Check mode & interval
            listing.CheckboxLabeled("Count Colonists Per Map Only (instead of entire faction)", ref countPerMapOnly, "If checked, only colonists present on the same map count toward the limit.");
            listing.Gap(4f);

            listing.Label($"Buff Check Frequency: Every {checkIntervalTicks} ticks (~{(checkIntervalTicks / 60f):F1}s)");
            checkIntervalTicks = Mathf.RoundToInt(listing.Slider(checkIntervalTicks / 250, 1, 20)) * 250;
            listing.Gap(10f);

            if (listing.ButtonText("Reset to Recommended Defaults"))
            {
                ResetToDefaults();
            }

            listing.End();
        }
    }
}
