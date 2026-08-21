using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public static class EffectResolver
    {
        private static Dictionary<HeroValueDef, List<HeroValueEffectDef>> cachedEffects;

        public static void ClearCache()
        {
            cachedEffects = null;
        }

        private static void EnsureCache()
        {
            if (cachedEffects != null) return;

            cachedEffects = new Dictionary<HeroValueDef, List<HeroValueEffectDef>>();
            var allDefs = DefDatabase<HeroValueEffectDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.axis == null) continue;

                if (!cachedEffects.TryGetValue(def.axis, out var list))
                {
                    list = new List<HeroValueEffectDef>();
                    cachedEffects[def.axis] = list;
                }
                list.Add(def);
            }
        }

        public static float CalculateContribution(HeroValueDef axis, string source, float sourceValue)
        {
            if (axis == null || string.IsNullOrEmpty(source) || sourceValue == 0f)
                return 0f;

            EnsureCache();
            if (!cachedEffects.TryGetValue(axis, out var effectsList))
                return 0f;

            float total = 0f;
            for (int i = 0; i < effectsList.Count; i++)
            {
                var effectDef = effectsList[i];
                if (effectDef.effects == null) continue;

                for (int j = 0; j < effectDef.effects.Count; j++)
                {
                    var entry = effectDef.effects[j];
                    if (string.Equals(entry.source, source, System.StringComparison.OrdinalIgnoreCase))
                    {
                        float val = sourceValue * entry.perUnit;
                        if (entry.HasCap)
                        {
                            if (entry.perUnit < 0)
                                val = Mathf.Max(val, entry.cap); // negative cap e.g. -25
                            else
                                val = Mathf.Min(val, entry.cap); // positive cap
                        }
                        total += val;
                    }
                }
            }

            return total;
        }

        public static List<HeroValueEffectDef> GetEffectsForAxis(HeroValueDef axis)
        {
            if (axis == null) return null;
            EnsureCache();
            if (cachedEffects.TryGetValue(axis, out var list))
                return list;
            return null;
        }
    }
}
