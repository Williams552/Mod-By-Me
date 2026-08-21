using System.Text;
using FireDiscipline.Core;
using RimWorld;
using Verse;

namespace FireDiscipline.AimStance
{
    /// <summary>
    /// StatPart injected into StatDefOf.ShootingAccuracyPawn.
    /// Provides clear, transparent explanation text inside the Pawn's Character Stat Info window,
    /// detailing how the active Fire Discipline stance affects accuracy.
    /// </summary>
    public class StatPart_ShootingAccuracy : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            // Base per-cell shooting accuracy is preserved in vanilla.
            // Stance accuracy modifiers are calculated precisely per distance in ShotReport.
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!PatchRegistry.IsModuleEnabled(AimStanceModule.Id)) return null;

            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.Dead) return null;

            AimStanceMode stance = AimStanceTracker.GetStance(pawn);

            StringBuilder sb = new StringBuilder();

            if (stance == AimStanceMode.Rapid)
            {
                sb.AppendLine("Fire Discipline Stance (Rapid): 100% Close-range accuracy (Progressive penalty past d0 threshold)");
            }
            else if (stance == AimStanceMode.Sharpshot)
            {
                sb.AppendLine("Fire Discipline Stance (Sharpshot): Long-range precision exponent (x0.80), Close-range penalty (<5c: x0.70)");
            }
            if (AimStanceTracker.IsDugIn(pawn))
            {
                float mult = FireDisciplineMod.Settings?.proneAccuracyMultiplier ?? 1.10f;
                float sizeMult = FireDisciplineMod.Settings?.proneTargetSizeFactor ?? 0.65f;
                sb.AppendLine($"Fire Discipline Passive (Dug-In / Prone): Shooter accuracy x{mult:F2} (Target size x{sizeMult:F2})");
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
        }
    }
}
