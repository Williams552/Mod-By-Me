using Verse;

namespace FireDiscipline.Suppression
{
    public class HediffCompProperties_SuppressionDecay : HediffCompProperties
    {
        /// <summary>Severity removed per second once decay starts.</summary>
        public float severityPerSecond = 0.10f;

        /// <summary>Grace period after the last round lands before decay begins.</summary>
        public int delayTicks = 120;

        public HediffCompProperties_SuppressionDecay()
        {
            compClass = typeof(HediffComp_SuppressionDecay);
        }
    }

    /// <summary>
    /// Continuous recovery from suppression.
    ///
    /// Replaces the previous model, where HediffComp_Disappears held severity flat for a few
    /// seconds and then dropped it to zero in a single frame. A pawn went from fully suppressed to
    /// completely unaffected with no transition, which made the state impossible to read on screen
    /// and impossible to play around: there was no moment where a pawn was "recovering".
    ///
    /// Mirrors the shape used by Suppression (Continued): a short grace period after the last round
    /// lands, then a steady drain. The hediff removes itself when severity reaches zero, so no
    /// separate expiry timer is needed.
    /// </summary>
    public class HediffComp_SuppressionDecay : HediffComp
    {
        private int lastAppliedTick = -9999;

        public HediffCompProperties_SuppressionDecay Props => (HediffCompProperties_SuppressionDecay)props;

        public void Notify_Applied()
        {
            lastAppliedTick = Find.TickManager?.TicksGame ?? 0;
        }

        public int TicksSinceApplied => (Find.TickManager?.TicksGame ?? 0) - lastAppliedTick;

        public override void CompPostTick(ref float severityAdjustment)
        {
            int delay = FireDisciplineMod.Settings?.suppressionDecayDelayTicks ?? Props.delayTicks;
            if (TicksSinceApplied < delay) return;

            float perSecond = FireDisciplineMod.Settings?.suppressionDecayPerSecond ?? Props.severityPerSecond;
            severityAdjustment -= perSecond / 60f;
        }

        public override bool CompShouldRemove => base.CompShouldRemove || parent.Severity <= 0f;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastAppliedTick, "fdLastAppliedTick", -9999);
        }

        public override string CompDebugString()
        {
            return $"ticks since last round: {TicksSinceApplied}";
        }
    }
}
