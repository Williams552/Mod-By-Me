using RimWorld;
using UnityEngine;
using Verse;
using FireDiscipline.Core;

namespace FireDiscipline.Suppression
{
    public class StatPart_SuppressionMoveSpeed : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!PatchRegistry.IsModuleEnabled(SuppressionCoreModule.Id))
                return;

            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.health == null)
                return;

            HediffDef def = SuppressionEngine.SuppressedDef;
            if (def == null) return;

            Hediff hediff = pawn.health.hediffSet?.GetFirstHediffOfDef(def);
            if (hediff == null) return;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float mult = 1f;

            switch (hediff.CurStageIndex)
            {
                case 0: // unsettled
                    mult = 1f;
                    break;
                case 1: // shaken
                    mult = settings?.suppressionMoveSpeedFactorStage1 ?? 0.95f;
                    break;
                case 2: // wavering
                    mult = settings?.suppressionMoveSpeedFactorStage2 ?? 0.80f;
                    break;
                case 3: // ducking
                    mult = settings?.suppressionMoveSpeedFactorStage3 ?? 0.50f;
                    break;
                case 4: // cowering
                    mult = settings?.suppressionMoveSpeedFactorStage4 ?? 0.15f;
                    break;
            }

            if (mult < 1f)
            {
                float floor = settings?.suppressedMoveSpeedFloor ?? 0.7f;
                
                // val * mult            : toc do sau khi bi ap che
                // Mathf.Min(val, floor) : san, nhung KHONG BAO GIO cao hon toc do goc.
                //                         Neu pawn von da cham hon 0.7 (thuong tich, encumbrance, Prone),
                //                         suppression khong duoc phep lam no NHANH LEN.
                // Mathf.Max(...)        : lay cai lon hon -> khong tut duoi san.
                
                // HAN CHE DA BIET:
                // StatPart chay theo thu tu khong xac dinh so voi cac StatPart khac tren MoveSpeed
                // (VD: StatPart_Encumbrance mang ca Encumbrance lan Prone). San chi duoc ap tai thoi
                // diem StatPart NAY chay; mot StatPart khac chay SAU van co the keo gia tri xuong
                // duoi 0.7. Chap nhan duoc cho v1.0.
                
                val = Mathf.Max(val * mult, Mathf.Min(val, floor));
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!PatchRegistry.IsModuleEnabled(SuppressionCoreModule.Id))
                return null;

            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.health == null)
                return null;

            HediffDef def = SuppressionEngine.SuppressedDef;
            if (def == null) return null;

            Hediff hediff = pawn.health.hediffSet?.GetFirstHediffOfDef(def);
            if (hediff == null) return null;

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            float mult = 1f;

            switch (hediff.CurStageIndex)
            {
                case 1: mult = settings?.suppressionMoveSpeedFactorStage1 ?? 0.95f; break;
                case 2: mult = settings?.suppressionMoveSpeedFactorStage2 ?? 0.80f; break;
                case 3: mult = settings?.suppressionMoveSpeedFactorStage3 ?? 0.50f; break;
                case 4: mult = settings?.suppressionMoveSpeedFactorStage4 ?? 0.15f; break;
            }

            if (mult < 1f)
            {
                return "Suppressed (" + hediff.CurStage.label + "): x" + mult.ToStringPercent();
            }

            return null;
        }
    }
}
