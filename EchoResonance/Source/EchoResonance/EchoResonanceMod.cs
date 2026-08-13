using HarmonyLib;
using UnityEngine;
using Verse;

namespace EchoResonance
{
    public class EchoResonanceMod : Mod
    {
        public static EchoResonanceSettings Settings { get; private set; }

        public EchoResonanceMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<EchoResonanceSettings>();

            var harmony = new Harmony("william.echoresonance");
            harmony.PatchAll();

            Log.Message("[Echo Resonance] Initialized successfully with Harmony patches.");
        }

        public override string SettingsCategory() => "Echo Resonance";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }

    public class EchoResonanceSettings : ModSettings
    {
        public float baseDailyPassiveEcho = 0.25f;
        public float costMultiplierExponent = 1.6f;
        public float specializationDiscount = 0.75f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref baseDailyPassiveEcho, "baseDailyPassiveEcho", 0.25f);
            Scribe_Values.Look(ref costMultiplierExponent, "costMultiplierExponent", 1.6f);
            Scribe_Values.Look(ref specializationDiscount, "specializationDiscount", 0.75f);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label($"Base Daily Passive Echo: {baseDailyPassiveEcho:F2}");
            baseDailyPassiveEcho = listing.Slider(baseDailyPassiveEcho, 0.05f, 2.0f);

            listing.Label($"Cost Escalation Exponent (1.6^N): {costMultiplierExponent:F2}");
            costMultiplierExponent = listing.Slider(costMultiplierExponent, 1.1f, 3.0f);

            listing.Label($"Specialization Discount: {specializationDiscount * 100:F0}%");
            specializationDiscount = listing.Slider(specializationDiscount, 0.5f, 0.95f);

            listing.End();
        }
    }
}
