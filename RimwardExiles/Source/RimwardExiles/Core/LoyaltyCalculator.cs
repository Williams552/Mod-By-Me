using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public static class LoyaltyCalculator
    {
        public static float CalculateTargetLoyalty(HeroState hero)
        {
            if (hero == null || hero.pawn == null) return 50f;

            var factors = GatherFactors(hero);
            float total = 50f;

            for (int i = 0; i < factors.Count; i++)
            {
                total += factors[i].delta;
            }

            return Mathf.Clamp(total, 0f, 100f);
        }

        public static List<LoyaltyFactor> GatherFactors(HeroState hero)
        {
            var list = new List<LoyaltyFactor>();
            if (hero == null || hero.pawn == null) return list;

            Pawn pawn = hero.pawn;
            HeroCreedDef creed = hero.creed;
            int now = Find.TickManager.TicksGame;

            // 1. Tâm trạng (Mood Factor: -25 .. +20)
            if (pawn.needs?.mood != null)
            {
                float moodPct = pawn.needs.mood.CurLevelPercentage;
                float moodFactor = (moodPct - 0.55f) * 45f;
                moodFactor = Mathf.Clamp(moodFactor, -25f, 20f);

                // Kiểm tra hediff điều hoà hormone (02 mục 8)
                if (HasMoodRegulator(pawn))
                {
                    moodFactor *= 0.20f;
                }

                if (Mathf.Abs(moodFactor) >= 0.5f)
                {
                    list.Add(new LoyaltyFactor("Tâm trạng", Mathf.Round(moodFactor), "Tâm lý"));
                }
            }

            // 2. Quan hệ xã hội với thuộc địa (-15 .. +15)
            if (pawn.relations != null)
            {
                var colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
                if (colonists != null && colonists.Count > 1)
                {
                    float totalOpinion = 0f;
                    int count = 0;
                    for (int i = 0; i < colonists.Count; i++)
                    {
                        var c = colonists[i];
                        if (c != null && c != pawn && c.RaceProps.Humanlike && !c.Dead && c.IsFreeColonist)
                        {
                            totalOpinion += pawn.relations.OpinionOf(c);
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        float avg = totalOpinion / count;
                        float opFactor = Mathf.Clamp(avg * 0.25f, -15f, 15f);
                        if (Mathf.Abs(opFactor) >= 0.5f)
                        {
                            list.Add(new LoyaltyFactor("Quan hệ với thuộc địa", Mathf.Round(opFactor), "Xã hội"));
                        }
                    }
                }
            }

            // 3. Trạng thái thân thể × Trục thân thể (-30 .. +25)
            if (creed != null)
            {
                var profile = BodyPathClassifier.SnapshotBody(pawn);
                float colonyEnhancement = BodyPathClassifier.CalculateColonyAverageEnhancement(Faction.OfPlayer);

                CalculateBodyAxisFactors(hero, profile, colonyEnhancement, list);
            }

            // 4. Ký ức (Memories)
            if (hero.memories != null)
            {
                for (int i = 0; i < hero.memories.Count; i++)
                {
                    var mem = hero.memories[i];
                    float w = mem.GetCurrentWeight(now);
                    if (Mathf.Abs(w) >= 0.5f)
                    {
                        string memLabel = mem.label;
                        if (mem.decayable)
                        {
                            float daysLeft = Mathf.Max(0, (mem.halfLifeDays * 2) - ((now - mem.tickOccurred) / 60000f));
                            memLabel = $"{mem.label} ({Mathf.CeilToInt(daysLeft)}n)";
                        }
                        else
                        {
                            memLabel = $"{mem.label} (vĩnh viễn)";
                        }
                        list.Add(new LoyaltyFactor(memLabel, Mathf.Round(w), "Ký ức"));
                    }
                }
            }

            // 5. Mâu thuẫn nội tâm đang mở (Tension Hediff: -10)
            if (pawn.health?.hediffSet != null)
            {
                var conflictedHediff = DefDatabase<HediffDef>.GetNamedSilentFail("RWX_Hediff_Conflicted");
                if (conflictedHediff != null && pawn.health.hediffSet.HasHediff(conflictedHediff))
                {
                    list.Add(new LoyaltyFactor("Bị bỏ mặc trong lúc giằng xé", -10f, "Tâm lý"));
                }
            }

            return list;
        }

        private static void CalculateBodyAxisFactors(HeroState hero, BodyProfile profile, float colonyEnhancement, List<LoyaltyFactor> list)
        {
            var creed = hero.creed;
            if (creed == null) return;

            var allValues = DefDatabase<HeroValueDef>.AllDefsListForReading;
            for (int i = 0; i < allValues.Count; i++)
            {
                var axis = allValues[i];
                float weight = creed.GetWeight(axis);
                if (Mathf.Abs(weight) < 0.05f) continue;

                // Tính tổng tác động của các nguồn lên trục này qua EffectResolver
                float axisDelta = 0f;

                axisDelta += EffectResolver.CalculateContribution(axis, "BodyPart_Steel", profile.steelParts);
                axisDelta += EffectResolver.CalculateContribution(axis, "BodyPart_Flesh", profile.fleshParts);
                axisDelta += EffectResolver.CalculateContribution(axis, "BodyPart_Intact", profile.intactNaturalParts);
                axisDelta += EffectResolver.CalculateContribution(axis, "BodyPart_Missing", profile.missingParts);
                axisDelta += EffectResolver.CalculateContribution(axis, "Gene_Implanted", profile.geneImpl);
                axisDelta += EffectResolver.CalculateContribution(axis, "Gene_Inherited", profile.geneInher);
                axisDelta += EffectResolver.CalculateContribution(axis, "PsylinkLevel", profile.psylinkLevel);
                axisDelta += EffectResolver.CalculateContribution(axis, "ColonyEnhancement", colonyEnhancement);

                float effective = axisDelta * weight;

                // Áp dụng phanh chống nhiễu P3: chỉ hiện nếu |effective| >= 1.0f
                if (Mathf.Abs(effective) >= 1.0f)
                {
                    string label = effective >= 0 ? (axis.positiveLabel ?? axis.label) : (axis.negativeLabel ?? axis.label);
                    list.Add(new LoyaltyFactor(label, Mathf.Round(effective), "Lý tưởng"));
                }
            }
        }

        private static bool HasMoodRegulator(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null) return false;
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                string name = hediffs[i].def.defName.ToLowerInvariant();
                if (name.Contains("hormoneregulator") || name.Contains("moodregulator") || name.Contains("joywire"))
                    return true;
            }
            return false;
        }
    }
}
