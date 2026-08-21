using HarmonyLib;
using UnityEngine;
using Verse;

namespace LoneSurvivor
{
    public class LoneSurvivorMod : Mod
    {
        public static LoneSurvivorSettings Settings { get; private set; }

        public LoneSurvivorMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LoneSurvivorSettings>();

            var harmony = new Harmony("william.lonesurvivor");
            harmony.PatchAll();

            Log.Message("[Lone Survivor] Initialized successfully.");
        }

        public override string SettingsCategory() => "Lone Survivor";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
