using RimWorld;
using Verse;

namespace LoneSurvivor
{
    public class GameComponent_LoneSurvivor : GameComponent
    {
        private int ticksPassed = 0;

        public GameComponent_LoneSurvivor(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            UpdateAllColonists();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            UpdateAllColonists();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            UpdateAllColonists();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            ticksPassed++;

            int interval = LoneSurvivorMod.Settings != null ? LoneSurvivorMod.Settings.checkIntervalTicks : 2000;
            if (interval <= 0) interval = 2000;

            if (ticksPassed >= interval)
            {
                ticksPassed = 0;
                UpdateAllColonists();
            }
        }

        public static void UpdateAllColonists()
        {
            if (Current.Game == null || LoneSurvivorMod.Settings == null) return;

            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("LoneSurvivorBuff");
            if (hediffDef == null) return;

            var colonists = LoneSurvivorUtility.GetAllAliveFreeColonists();
            int threshold = LoneSurvivorMod.Settings.maxColonistsThreshold;

            for (int i = 0; i < colonists.Count; i++)
            {
                var pawn = colonists[i];
                if (pawn == null || pawn.Dead || !pawn.IsFreeColonist || pawn.health == null) continue;

                int count = LoneSurvivorUtility.GetColonistCount(pawn);
                var existingHediff = pawn.health.hediffSet?.GetFirstHediffOfDef(hediffDef);

                if (count < threshold)
                {
                    if (existingHediff == null)
                    {
                        pawn.health.AddHediff(hediffDef);
                    }
                }
                else
                {
                    if (existingHediff != null)
                    {
                        pawn.health.RemoveHediff(existingHediff);
                    }
                }
            }
        }
    }
}
