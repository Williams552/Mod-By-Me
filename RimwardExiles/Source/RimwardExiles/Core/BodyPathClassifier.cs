using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public struct BodyProfile
    {
        public int steelParts;
        public int fleshParts;
        public int geneImpl;      // Xenogenes cấy ghép
        public int geneInher;     // Endogenes bẩm sinh
        public int missingParts;  // Bộ phận bị cụt chưa thay
        public int intactNaturalParts;
        public float avgPartEfficiency;
        public int psylinkLevel;

        public int TotalEnhancements => steelParts + fleshParts + geneImpl;
    }

    public static class BodyPathClassifier
    {
        private static Dictionary<HediffDef, ModPath> cachedClassifications;
        private static bool initialized = false;

        public static void ClearCache()
        {
            cachedClassifications = null;
            initialized = false;
        }

        private static void EnsureInitialized()
        {
            if (initialized && cachedClassifications != null) return;

            cachedClassifications = new Dictionary<HediffDef, ModPath>();
            var overrideDefs = DefDatabase<HeroBodyPathDef>.AllDefsListForReading;

            var allHediffs = DefDatabase<HediffDef>.AllDefsListForReading;
            for (int i = 0; i < allHediffs.Count; i++)
            {
                var hDef = allHediffs[i];
                ModPath result = ModPath.None;
                bool matchedOverride = false;

                // 1. Kiểm tra override trực tiếp theo tên
                if (overrideDefs != null)
                {
                    for (int o = 0; o < overrideDefs.Count; o++)
                    {
                        var oDef = overrideDefs[o];
                        if (oDef.entries != null)
                        {
                            for (int e = 0; e < oDef.entries.Count; e++)
                            {
                                if (string.Equals(oDef.entries[e].hediff, hDef.defName, StringComparison.OrdinalIgnoreCase))
                                {
                                    result = oDef.entries[e].path;
                                    matchedOverride = true;
                                    break;
                                }
                            }
                        }
                        if (matchedOverride) break;

                        // Kiểm tra override theo PackageId
                        if (oDef.packageIdRules != null && hDef.modContentPack != null)
                        {
                            string pkg = hDef.modContentPack.PackageIdPlayerFacing?.ToLowerInvariant() ?? "";
                            for (int r = 0; r < oDef.packageIdRules.Count; r++)
                            {
                                if (!string.IsNullOrEmpty(oDef.packageIdRules[r].packageIdContains) &&
                                    pkg.Contains(oDef.packageIdRules[r].packageIdContains.ToLowerInvariant()))
                                {
                                    result = oDef.packageIdRules[r].path;
                                    matchedOverride = true;
                                    break;
                                }
                            }
                        }
                        if (matchedOverride) break;
                    }
                }

                // 2. Thuật toán phân loại mặc định nếu không có override
                if (!matchedOverride && hDef.addedPartProps != null)
                {
                    string name = hDef.defName.ToLowerInvariant();
                    string label = hDef.label?.ToLowerInvariant() ?? "";

                    if (name.Contains("flesh") || name.Contains("bio") || name.Contains("organ") ||
                        label.Contains("thịt") || label.Contains("sinh học") || label.Contains("nội tạng"))
                    {
                        result = ModPath.Flesh;
                    }
                    else
                    {
                        // Mặc định bionic, archotech, prosthetic là Steel
                        result = ModPath.Steel;
                    }
                }

                cachedClassifications[hDef] = result;
            }

            initialized = true;
        }

        public static ModPath ClassifyHediff(HediffDef def)
        {
            if (def == null) return ModPath.None;
            EnsureInitialized();
            if (cachedClassifications.TryGetValue(def, out var path))
                return path;
            return ModPath.None;
        }

        public static BodyProfile SnapshotBody(Pawn pawn)
        {
            var profile = new BodyProfile();
            if (pawn == null || pawn.health?.hediffSet == null) return profile;

            var hediffs = pawn.health.hediffSet.hediffs;
            float totalEfficiency = 0f;
            int addedPartsCount = 0;

            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h is Hediff_MissingPart)
                {
                    profile.missingParts++;
                    continue;
                }

                if (h.def.addedPartProps != null)
                {
                    addedPartsCount++;
                    totalEfficiency += h.def.addedPartProps.partEfficiency;

                    ModPath path = ClassifyHediff(h.def);
                    if (path == ModPath.Steel) profile.steelParts++;
                    else if (path == ModPath.Flesh) profile.fleshParts++;
                }
            }

            if (addedPartsCount > 0)
                profile.avgPartEfficiency = totalEfficiency / addedPartsCount;
            else
                profile.avgPartEfficiency = 1.0f;

            // Đếm bộ phận tự nhiên còn nguyên vẹn
            var allParts = pawn.RaceProps.body.AllParts;
            int missingOrReplaced = 0;
            for (int i = 0; i < allParts.Count; i++)
            {
                if (pawn.health.hediffSet.PartIsMissing(allParts[i]) ||
                    pawn.health.hediffSet.HasDirectlyAddedPartFor(allParts[i]))
                {
                    missingOrReplaced++;
                }
            }
            profile.intactNaturalParts = Mathf.Max(0, allParts.Count - missingOrReplaced);

            // Biotech Genes
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                profile.geneImpl = pawn.genes.Xenogenes?.Count ?? 0;
                profile.geneInher = pawn.genes.Endogenes?.Count ?? 0;
            }

            // Royalty Psylink
            if (ModsConfig.RoyaltyActive)
            {
                profile.psylinkLevel = pawn.GetPsylinkLevel();
            }

            return profile;
        }

        public static float CalculateColonyAverageEnhancement(Faction playerFaction)
        {
            var pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
            if (pawns == null || pawns.Count == 0) return 0f;

            int totalColonists = 0;
            float sum = 0f;

            for (int i = 0; i < pawns.Count; i++)
            {
                var p = pawns[i];
                if (p != null && p.RaceProps.Humanlike && !p.Dead && p.IsFreeColonist)
                {
                    var prof = SnapshotBody(p);
                    sum += prof.TotalEnhancements;
                    totalColonists++;
                }
            }

            return totalColonists > 0 ? (sum / totalColonists) : 0f;
        }
    }
}
