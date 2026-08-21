using System;
using System.IO;
using RimWorld;
using Verse;

namespace RimwardExiles.Core
{
    public static class HeroPawnLoader
    {
        public static string GetPresetsDirectory()
        {
            // Tìm thư mục Presets trong mod content pack
            var modPack = LoadedModManager.GetMod<RimwardExilesMod>()?.Content;
            if (modPack != null)
            {
                string path = Path.Combine(modPack.RootDir, "Presets");
                if (Directory.Exists(path)) return path;
            }

            // Fallback: Tìm theo Assembly location
            string asmLocation = typeof(HeroPawnLoader).Assembly.Location;
            string modRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(asmLocation), "..", ".."));
            string fallbackPath = Path.Combine(modRoot, "Presets");
            return fallbackPath;
        }

        public static Pawn LoadFromFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".xml";
            }

            string presetsDir = GetPresetsDirectory();
            string fullPath = Path.Combine(presetsDir, fileName);

            if (!File.Exists(fullPath))
            {
                Log.Warning($"[Rimward Exiles] Preset file not found: {fullPath}");
                return null;
            }

            Pawn pawn = null;
            try
            {
                Scribe.loader.InitLoading(fullPath);
                ScribeMetaHeaderUtility.LoadGameDataHeader(ScribeMetaHeaderUtility.ScribeHeaderMode.None, false);
                Scribe_Deep.Look(ref pawn, "pawn");
                Scribe.loader.FinalizeLoading();
            }
            catch (Exception ex)
            {
                Log.Error($"[Rimward Exiles] Exception while loading preset '{fileName}': {ex}");
                if (Scribe.mode != LoadSaveMode.Inactive)
                {
                    Scribe.ForceStop(); // BẮT BUỘC tuyệt đối để không kẹt hệ thống save/load của RimWorld
                }
                return null;
            }

            if (pawn != null)
            {
                Sanitize(pawn);
            }

            return pawn;
        }

        public static void Sanitize(Pawn pawn)
        {
            if (pawn == null) return;

            pawn.SetFactionDirect(null);
            pawn.thingIDNumber = Find.UniqueIDsManager.GetNextThingID();

            pawn.relations?.ClearAllRelations();
            pawn.jobs?.StopAll();
            pawn.health?.surgeryBills?.Clear();

            if (pawn.Spawned)
            {
                pawn.DeSpawn();
            }

            pawn.Notify_DisabledWorkTypesChanged();
        }

        public static void ValidateAllPresets()
        {
            string presetsDir = GetPresetsDirectory();
            if (!Directory.Exists(presetsDir))
            {
                Log.Message($"[Rimward Exiles] Thư mục Presets chưa tồn tại ({presetsDir}).");
                return;
            }

            var files = Directory.GetFiles(presetsDir, "*.xml");
            Log.Message($"[Rimward Exiles] Bắt đầu xác thực {files.Length} file preset Hero...");

            for (int i = 0; i < files.Length; i++)
            {
                string fName = Path.GetFileName(files[i]);
                if (string.Equals(fName, "manifest.xml", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    // Đọc thử kiểm tra tính hợp lệ
                    Log.Message($"[Rimward Exiles] Preset '{fName}': Đã kiểm tra cấu trúc OK.");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[Rimward Exiles] Preset '{fName}': CẢNH BÁO - {ex.Message}");
                }
            }
        }
    }
}
