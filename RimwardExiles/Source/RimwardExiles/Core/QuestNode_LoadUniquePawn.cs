using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace RimwardExiles.Core
{
    public class QuestNode_LoadUniquePawn : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> presetFileName;
        [NoTranslate]
        public SlateRef<string> storeAs;
        public SlateRef<HeroCreedDef> creed;

        protected override bool TestRunInt(Slate slate)
        {
            return !string.IsNullOrEmpty(presetFileName.GetValue(slate));
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            string fileName = presetFileName.GetValue(slate);
            string varName = storeAs.GetValue(slate);
            HeroCreedDef heroCreed = creed.GetValue(slate);

            Pawn pawn = HeroPawnLoader.LoadFromFile(fileName);
            if (pawn != null)
            {
                // Đăng ký vào hệ thống Hero
                GameComponent_Exiles.Instance?.RegisterHero(pawn, heroCreed);

                // Chuyển pawn vào World Pawn Pool với cờ KeepForever
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);

                if (!string.IsNullOrEmpty(varName))
                {
                    slate.Set(varName, pawn);
                }
            }
            else
            {
                Log.Error($"[Rimward Exiles] QuestNode_LoadUniquePawn: Không thể nạp preset '{fileName}'");
            }
        }
    }
}
