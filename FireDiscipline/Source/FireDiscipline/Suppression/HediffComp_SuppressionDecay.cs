using FireDiscipline.Core;
using RimWorld;
using Verse;

namespace FireDiscipline.Suppression
{
    public class HediffCompProperties_SuppressionDecay : HediffCompProperties_TimedDecay
    {
        public HediffCompProperties_SuppressionDecay()
        {
            compClass = typeof(HediffComp_SuppressionDecay);
        }
    }

    /// <summary>
    /// Continuous recovery from suppression.
    /// Derived from HediffComp_TimedDecay, incorporating Suppression-specific mod settings overrides.
    /// </summary>
    public class HediffComp_SuppressionDecay : HediffComp_TimedDecay
    {
        public new HediffCompProperties_SuppressionDecay Props => (HediffCompProperties_SuppressionDecay)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            int delay = FireDisciplineMod.Settings?.suppressionDecayDelayTicks ?? Props.delayTicks;
            if (TicksSinceApplied < delay) return;

            float perSecond = FireDisciplineMod.Settings?.suppressionDecayPerSecond ?? Props.severityPerSecond;
            StatDef recSpeedDef = SuppressionStatDefOf.SuppressionRecoverySpeed;
            float recoverySpeed = (recSpeedDef != null && Pawn != null) ? Pawn.GetStatValue(recSpeedDef, true) : 1.0f;
            severityAdjustment -= (perSecond / 60f) * recoverySpeed;
        }
    }
}
