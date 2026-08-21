using Verse;

namespace FireDiscipline.Core
{
    /// <summary>
    /// GameComponent for Fire Discipline.
    /// Resets runtime AimStanceTracker caches on save load / game start, and periodically purges stale pawn entries.
    /// </summary>
    public class FireDisciplineGameComponent : GameComponent
    {
        private int lastCleanupTick = 0;

        /// <summary>
        /// True once the player has been told that a No-Fire Zone stopped a turret from firing. Saved
        /// with the game so the explanation appears once per colony rather than once per load.
        /// </summary>
        public bool noFireZoneNoticeShown = false;

        public FireDisciplineGameComponent(Game game)
        {
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            AimStance.AimStanceTracker.ClearCache();
            Shock.Patch_Pawn_Kill_Down.ClearCache();
            Variance.HitVarianceState.ClearCache();
            CoverStackingUtility.ClearCache();
            NoFireZone.NoFireZoneNotice.Reset(noFireZoneNoticeShown);
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            AimStance.AimStanceTracker.ClearCache();
            Shock.Patch_Pawn_Kill_Down.ClearCache();
            Variance.HitVarianceState.ClearCache();
            CoverStackingUtility.ClearCache();
            NoFireZone.NoFireZoneNotice.Reset(noFireZoneNoticeShown);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref noFireZoneNoticeShown, "fdNoFireZoneNoticeShown", false);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick - lastCleanupTick > 3000) // Cleanup stale entries every 3000 ticks (~50s)
            {
                lastCleanupTick = currentTick;
                AimStance.AimStanceTracker.CleanupStaleEntries();
                Shock.Patch_Pawn_Kill_Down.CleanupStaleEntries();
                Variance.HitVarianceState.CleanupStaleEntries();
            }
        }
    }
}
