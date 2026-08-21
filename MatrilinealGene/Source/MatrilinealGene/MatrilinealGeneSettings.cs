using UnityEngine;
using Verse;

namespace MatrilinealGene
{
    public class MatrilinealGeneSettings : ModSettings
    {
        public bool forceAllFemale = true;
        public bool inheritMotherXenotype = true;
        public bool enableBirthNotification = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref forceAllFemale, "forceAllFemale", true);
            Scribe_Values.Look(ref inheritMotherXenotype, "inheritMotherXenotype", true);
            Scribe_Values.Look(ref enableBirthNotification, "enableBirthNotification", true);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled(
                "Matrilineal_Settings_ForceFemale_Label".Translate(),
                ref forceAllFemale,
                "Matrilineal_Settings_ForceFemale_Desc".Translate()
            );

            listing.Gap(12f);

            listing.CheckboxLabeled(
                "Matrilineal_Settings_InheritXenotype_Label".Translate(),
                ref inheritMotherXenotype,
                "Matrilineal_Settings_InheritXenotype_Desc".Translate()
            );

            listing.Gap(12f);

            listing.CheckboxLabeled(
                "Matrilineal_Settings_Notification_Label".Translate(),
                ref enableBirthNotification,
                "Matrilineal_Settings_Notification_Desc".Translate()
            );

            listing.Gap(24f);

            if (listing.ButtonText("Matrilineal_Settings_ResetDefaults".Translate()))
            {
                forceAllFemale = true;
                inheritMotherXenotype = true;
                enableBirthNotification = true;
            }

            listing.End();
        }
    }
}
