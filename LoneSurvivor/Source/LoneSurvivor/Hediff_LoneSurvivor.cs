using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace LoneSurvivor
{
    public class Hediff_LoneSurvivor : HediffWithComps
    {
        private HediffStage cachedStage;
        private float cachedRatio = -1f;

        public float CurrentRatio
        {
            get
            {
                int count = LoneSurvivorUtility.GetColonistCount(pawn);
                int threshold = LoneSurvivorMod.Settings.maxColonistsThreshold;
                return LoneSurvivorUtility.CalculateBuffRatio(count, threshold);
            }
        }

        public override HediffStage CurStage
        {
            get
            {
                float ratio = CurrentRatio;
                if (cachedStage == null || Mathf.Abs(cachedRatio - ratio) > 0.001f)
                {
                    cachedRatio = ratio;
                    cachedStage = BuildStage(ratio);
                }
                return cachedStage;
            }
        }

        private HediffStage BuildStage(float ratio)
        {
            var stage = new HediffStage
            {
                statOffsets = new List<StatModifier>(),
                statFactors = new List<StatModifier>()
            };

            var settings = LoneSurvivorMod.Settings;

            // 1. Global Work Speed
            float workBonus = settings.maxWorkSpeedBonus * ratio;
            if (workBonus > 0.0001f)
            {
                stage.statOffsets.Add(new StatModifier
                {
                    stat = StatDefOf.WorkSpeedGlobal,
                    value = workBonus
                });
            }

            // 2. Global Learning Factor
            float learnBonus = settings.maxLearningBonus * ratio;
            if (learnBonus > 0.0001f)
            {
                stage.statOffsets.Add(new StatModifier
                {
                    stat = StatDefOf.GlobalLearningFactor,
                    value = learnBonus
                });
            }

            // 3. Rest Fall Rate Reduction
            float restRed = settings.maxRestFallReduction * ratio;
            if (restRed > 0.0001f)
            {
                float factor = Mathf.Clamp(1f - restRed, 0.05f, 1f);
                if (StatDefOf.RestFallRateFactor != null)
                {
                    stage.statFactors.Add(new StatModifier
                    {
                        stat = StatDefOf.RestFallRateFactor,
                        value = factor
                    });
                }
                stage.restFallFactor = factor;
            }

            // 4. Move Speed (Optional)
            float moveBonus = settings.maxMoveSpeedBonus * ratio;
            if (moveBonus > 0.0001f)
            {
                stage.statOffsets.Add(new StatModifier
                {
                    stat = StatDefOf.MoveSpeed,
                    value = moveBonus
                });
            }

            // 5. Immunity Gain Speed (Optional)
            float immunityBonus = settings.maxImmunityBonus * ratio;
            if (immunityBonus > 0.0001f)
            {
                stage.statOffsets.Add(new StatModifier
                {
                    stat = StatDefOf.ImmunityGainSpeed,
                    value = immunityBonus
                });
            }

            return stage;
        }

        public override string LabelInBrackets
        {
            get
            {
                float ratio = CurrentRatio;
                return $"{(ratio * 100f):F0}%";
            }
        }

        public override string TipStringExtra
        {
            get
            {
                var sb = new StringBuilder();
                string baseTip = base.TipStringExtra;
                if (!string.IsNullOrEmpty(baseTip))
                {
                    sb.AppendLine(baseTip);
                }

                int count = LoneSurvivorUtility.GetColonistCount(pawn);
                int threshold = LoneSurvivorMod.Settings.maxColonistsThreshold;
                float ratio = CurrentRatio;

                sb.AppendLine($"Colony population: {count}/{threshold}");
                sb.AppendLine($"Buff effectiveness: {(ratio * 100f):F0}%\n");

                var settings = LoneSurvivorMod.Settings;
                sb.AppendLine($"• Work Speed: +{(settings.maxWorkSpeedBonus * ratio * 100f):F0}%");
                sb.AppendLine($"• Learning Rate: +{(settings.maxLearningBonus * ratio * 100f):F0}%");
                sb.AppendLine($"• Rest Fall Rate: -{(settings.maxRestFallReduction * ratio * 100f):F0}%");

                if (settings.maxMoveSpeedBonus > 0f)
                {
                    sb.AppendLine($"• Move Speed: +{(settings.maxMoveSpeedBonus * ratio):F2} c/s");
                }
                if (settings.maxImmunityBonus > 0f)
                {
                    sb.AppendLine($"• Immunity Gain: +{(settings.maxImmunityBonus * ratio * 100f):F0}%");
                }

                return sb.ToString().TrimEnd();
            }
        }
    }
}
