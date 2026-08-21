using HarmonyLib;
using UnityEngine;
using Verse;

namespace MatrilinealGene
{
    public class MatrilinealGeneMod : Mod
    {
        public static MatrilinealGeneSettings Settings { get; private set; }

        public MatrilinealGeneMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MatrilinealGeneSettings>();

            var harmony = new Harmony("william.matrilinealgene");
            harmony.PatchAll();

            Log.Message("[Matrilineal Gene] Initialized successfully with Biotech support.");
        }

        public override string SettingsCategory() => "Matrilineal_Settings_Category".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
