using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimwardExiles.Core
{
    [StaticConstructorOnStartup]
    public static class ExilesStartup
    {
        static ExilesStartup()
        {
            InjectInspectorTabs();
        }

        private static void InjectInspectorTabs()
        {
            var tabType = typeof(ITab_Pawn_Loyalty);
            var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            int count = 0;

            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.race != null && def.race.Humanlike)
                {
                    if (def.inspectorTabs == null)
                    {
                        def.inspectorTabs = new List<Type>();
                    }

                    if (!def.inspectorTabs.Contains(tabType))
                    {
                        def.inspectorTabs.Add(tabType);
                        if (def.inspectorTabsResolved != null)
                        {
                            def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(tabType));
                        }
                        count++;
                    }
                }
            }

            Log.Message($"[Rimward Exiles] Injected ITab_Pawn_Loyalty into {count} humanlike ThingDefs.");
        }
    }
}
