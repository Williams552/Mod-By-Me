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

        public FireDisciplineGameComponent(Game game)
        {
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            AimStance.AimStanceTracker.ClearCache();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            AimStance.AimStanceTracker.ClearCache();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick - lastCleanupTick > 3000) // Cleanup stale pawn stance tracker entries every 3000 ticks (~50s)
            {
                lastCleanupTick = currentTick;
                AimStance.AimStanceTracker.CleanupStaleEntries();
            }
        }
    }
}
