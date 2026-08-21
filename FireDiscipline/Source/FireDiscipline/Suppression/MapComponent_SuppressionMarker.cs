using FireDiscipline.Core;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FireDiscipline.Suppression
{
    public class MapComponent_SuppressionMarker : MapComponent
    {
        public MapComponent_SuppressionMarker(Map map) : base(map)
        {
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            FireDisciplineSettings settings = FireDisciplineMod.Settings;
            if (settings == null || !settings.enableSuppressionMarker) return;
            if (!PatchRegistry.IsModuleEnabled(SuppressionMarkerModule.Id)) return;
            if (Find.CurrentMap != map || WorldRendererUtility.WorldRendered) return;

            float minSev = settings.suppressionMarkerMinSeverity;
            float pinnedThreshold = settings.pinnedSeverityThreshold;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || pawn.Position.Fogged(map)) continue;

                float sev = SuppressionEngine.GetSeverity(pawn);
                if (sev < minSev) continue;

                DrawMarkerForPawn(pawn, sev, pinnedThreshold);
            }
        }

        public static (string label, Color color) GetStageInfo(float severity, float pinnedThreshold)
        {
            if (severity >= pinnedThreshold)
            {
                return ("PINNED", new Color(1.0f, 0.2f, 0.2f));
            }
            if (severity >= 5.5f)
            {
                return ("cowering", new Color(1.0f, 0.4f, 0.2f));
            }
            if (severity >= 2.0f)
            {
                return ("ducking", new Color(1.0f, 0.8f, 0.2f));
            }
            if (severity >= 1.0f)
            {
                return ("wavering", new Color(0.9f, 0.9f, 0.3f));
            }
            if (severity >= 0.5f)
            {
                return ("shaken", new Color(0.7f, 0.9f, 0.7f));
            }
            return ("unsettled", Color.white);
        }

        private void DrawMarkerForPawn(Pawn pawn, float severity, float pinnedThreshold)
        {
            Vector2 screenPos = GenMapUI.LabelDrawPosFor(pawn, -0.4f);
            var (stageLabel, stageColor) = GetStageInfo(severity, pinnedThreshold);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            string text = $"{stageLabel} ({severity:F1})";
            Vector2 size = Text.CalcSize(text);

            Rect labelRect = new Rect(screenPos.x - size.x / 2f, screenPos.y - size.y / 2f, size.x, size.y);

            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(labelRect, BaseContent.WhiteTex);

            GUI.color = stageColor;
            Widgets.Label(labelRect, text);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
}
