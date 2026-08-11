using Verse;

namespace FireDiscipline.Core
{
    public class HediffCompProperties_TimedDecay : HediffCompProperties
    {
        /// <summary>Severity removed per second once decay starts.</summary>
        public float severityPerSecond = 0.10f;

        /// <summary>Grace period after last application before decay begins.</summary>
        public int delayTicks = 120;

        public HediffCompProperties_TimedDecay()
        {
            compClass = typeof(HediffComp_TimedDecay);
        }
    }

    /// <summary>
    /// Generic continuous decay for Fire Discipline hediffs (FD_Suppressed, FD_ShellShock, FD_CombatShock).
    /// Holds severity during grace period (delayTicks), then steadily drains severity.
    /// Resets delay timer when Notify_Applied() is called on re-application.
    /// Removes hediff when severity drops to zero.
    /// </summary>
    public class HediffComp_TimedDecay : HediffComp
    {
        private int lastAppliedTick = -9999;

        public HediffCompProperties_TimedDecay Props => (HediffCompProperties_TimedDecay)props;

        public void Notify_Applied()
        {
            lastAppliedTick = Find.TickManager?.TicksGame ?? 0;
        }

        public int TicksSinceApplied => (Find.TickManager?.TicksGame ?? 0) - lastAppliedTick;

        public override void CompPostTick(ref float severityAdjustment)
        {
            int delay = Props.delayTicks;
            if (TicksSinceApplied < delay) return;

            float perSecond = Props.severityPerSecond;
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
            return $"ticks since last application: {TicksSinceApplied}";
        }
    }
}
