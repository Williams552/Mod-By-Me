using HarmonyLib;
using Verse;

namespace RimwardExiles.Core
{
    public class RimwardExilesMod : Mod
    {
        public static RimwardExilesMod Instance { get; private set; }

        public RimwardExilesMod(ModContentPack content) : base(content)
        {
            Instance = this;

            var harmony = new Harmony("william.rimwardexiles");
            harmony.PatchAll();

            Log.Message("[Rimward Exiles] Core engine initialized successfully.");
        }
    }
}
