using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public static class ExilesDebugActions
    {
        [DebugAction("Rimward Exiles", "Set loyalty...", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetLoyalty(Pawn pawn)
        {
            if (pawn == null) return;
            var comp = GameComponent_Exiles.Instance;
            var hero = comp?.GetHeroState(pawn);

            if (hero == null)
            {
                Messages.Message($"{pawn.LabelShort} không phải là Hero được quản lý bởi Rimward Exiles.", MessageTypeDefOf.RejectInput);
                return;
            }

            var options = new List<DebugMenuOption>();
            float[] values = { 0f, 15f, 30f, 50f, 65f, 80f, 100f };

            for (int i = 0; i < values.Length; i++)
            {
                float val = values[i];
                options.Add(new DebugMenuOption($"{val:F0}", DebugMenuOptionMode.Action, () =>
                {
                    hero.loyalty = val;
                    Messages.Message($"Đã đặt Loyalty của {pawn.LabelShort} thành {val:F0}", MessageTypeDefOf.TaskCompletion);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Rimward Exiles", "Make Hero with Creed...", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void MakeHero(Pawn pawn)
        {
            if (pawn == null) return;
            var comp = GameComponent_Exiles.Instance;
            if (comp == null) return;

            var options = new List<DebugMenuOption>();
            var allCreeds = DefDatabase<HeroCreedDef>.AllDefsListForReading;

            for (int i = 0; i < allCreeds.Count; i++)
            {
                var creed = allCreeds[i];
                options.Add(new DebugMenuOption(creed.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    comp.RegisterHero(pawn, creed);
                    Messages.Message($"Đã chỉ định {pawn.LabelShort} thành Hero với Creed '{creed.LabelCap}'", MessageTypeDefOf.PositiveEvent);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Rimward Exiles", "Dump body profile", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpBodyProfile(Pawn pawn)
        {
            if (pawn == null) return;

            var profile = BodyPathClassifier.SnapshotBody(pawn);
            var sb = new StringBuilder();
            sb.AppendLine($"[Rimward Exiles] Body Profile cho {pawn.LabelCap}:");
            sb.AppendLine($" - Steel parts: {profile.steelParts}");
            sb.AppendLine($" - Flesh parts: {profile.fleshParts}");
            sb.AppendLine($" - Xenogenes (cấy ghép): {profile.geneImpl}");
            sb.AppendLine($" - Endogenes (bẩm sinh): {profile.geneInher}");
            sb.AppendLine($" - Missing parts: {profile.missingParts}");
            sb.AppendLine($" - Intact natural parts: {profile.intactNaturalParts}");
            sb.AppendLine($" - Avg Part Efficiency: {profile.avgPartEfficiency * 100:F0}%");
            sb.AppendLine($" - Psylink Level: {profile.psylinkLevel}");

            Log.Message(sb.ToString());
            Messages.Message($"Đã xuất Body Profile của {pawn.LabelShort} vào Log (Ctrl + `)", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction("Rimward Exiles", "Dump loyalty factors", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DumpLoyaltyFactors(Pawn pawn)
        {
            if (pawn == null) return;
            var comp = GameComponent_Exiles.Instance;
            var hero = comp?.GetHeroState(pawn);

            if (hero == null)
            {
                Messages.Message($"{pawn.LabelShort} không phải là Hero.", MessageTypeDefOf.RejectInput);
                return;
            }

            var factors = LoyaltyCalculator.GatherFactors(hero);
            var sb = new StringBuilder();
            sb.AppendLine($"[Rimward Exiles] Loyalty Analysis cho {pawn.LabelCap} (Hiện tại: {hero.loyalty:F1}, Mục tiêu: {hero.targetLoyalty:F1}):");
            sb.AppendLine("Anchor cơ sở: +50");

            for (int i = 0; i < factors.Count; i++)
            {
                var f = factors[i];
                sb.AppendLine($" - [{f.category}] {f.label}: {(f.delta >= 0 ? "+" : "")}{f.delta:F1}");
            }

            Log.Message(sb.ToString());
            Messages.Message($"Đã xuất bảng tính Loyalty của {pawn.LabelShort} vào Log", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction("Rimward Exiles", "Spawn hero preset...", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnHeroPreset()
        {
            string presetsDir = HeroPawnLoader.GetPresetsDirectory();
            if (!System.IO.Directory.Exists(presetsDir))
            {
                Messages.Message($"Thư mục Presets không tồn tại: {presetsDir}", MessageTypeDefOf.RejectInput);
                return;
            }

            var files = System.IO.Directory.GetFiles(presetsDir, "*.xml");
            var options = new List<DebugMenuOption>();

            for (int i = 0; i < files.Length; i++)
            {
                string fName = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                if (string.Equals(fName, "manifest", System.StringComparison.OrdinalIgnoreCase)) continue;

                options.Add(new DebugMenuOption(fName, DebugMenuOptionMode.Tool, () =>
                {
                    Pawn p = HeroPawnLoader.LoadFromFile(fName);
                    if (p != null)
                    {
                        IntVec3 cell = UI.MouseCell();
                        Map map = Find.CurrentMap;
                        GenSpawn.Spawn(p, cell, map);
                        p.SetFaction(Faction.OfPlayer);
                        Messages.Message($"Đã spawn Hero '{fName}' ({p.LabelShort})", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message($"Không thể load hero '{fName}'", MessageTypeDefOf.RejectInput);
                    }
                }));
            }

            if (options.Count == 0)
            {
                Messages.Message("Chưa có file preset .xml nào trong thư mục Presets/.", MessageTypeDefOf.RejectInput);
                return;
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Rimward Exiles", "Validate all presets", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ValidateAllPresetsAction()
        {
            HeroPawnLoader.ValidateAllPresets();
            Messages.Message("Đã chạy kiểm tra và xác thực Presets (Xem kết quả tại Log)", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction("Rimward Exiles", "Force decision...", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceDecisionAction()
        {
            var allDecisions = DefDatabase<HeroDecisionDef>.AllDefsListForReading;
            if (allDecisions == null || allDecisions.Count == 0)
            {
                Messages.Message("Chưa có HeroDecisionDef nào được nạp.", MessageTypeDefOf.RejectInput);
                return;
            }

            var options = new List<DebugMenuOption>();
            for (int i = 0; i < allDecisions.Count; i++)
            {
                var dec = allDecisions[i];
                options.Add(new DebugMenuOption(dec.LabelCap, DebugMenuOptionMode.Action, () =>
                {
                    HeroDecisionWorker.FireDecision(dec, Find.CurrentMap);
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("Rimward Exiles", "Clear all memories", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearAllMemories(Pawn pawn)
        {
            if (pawn == null) return;
            var hero = GameComponent_Exiles.Instance?.GetHeroState(pawn);
            if (hero != null)
            {
                hero.memories?.Clear();
                Messages.Message($"Đã xoá toàn bộ Memory của {pawn.LabelShort}", MessageTypeDefOf.TaskCompletion);
            }
        }
    }
}
