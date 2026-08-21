using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public static class ReactionResolver
    {
        private static Dictionary<string, List<HeroReactionDef>> cachedIncidentReactions;
        private static Dictionary<string, List<HeroReactionDef>> cachedActionReactions;

        public static void ClearCache()
        {
            cachedIncidentReactions = null;
            cachedActionReactions = null;
        }

        private static void EnsureCache()
        {
            if (cachedIncidentReactions != null && cachedActionReactions != null) return;

            cachedIncidentReactions = new Dictionary<string, List<HeroReactionDef>>(StringComparer.OrdinalIgnoreCase);
            cachedActionReactions = new Dictionary<string, List<HeroReactionDef>>(StringComparer.OrdinalIgnoreCase);

            var allDefs = DefDatabase<HeroReactionDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                var rDef = allDefs[i];
                if (rDef.triggers == null) continue;

                for (int t = 0; t < rDef.triggers.Count; t++)
                {
                    var trigger = rDef.triggers[t];
                    if (!string.IsNullOrEmpty(trigger.incident))
                    {
                        if (!cachedIncidentReactions.TryGetValue(trigger.incident, out var list))
                        {
                            list = new List<HeroReactionDef>();
                            cachedIncidentReactions[trigger.incident] = list;
                        }
                        list.Add(rDef);
                    }

                    if (!string.IsNullOrEmpty(trigger.action))
                    {
                        if (!cachedActionReactions.TryGetValue(trigger.action, out var list))
                        {
                            list = new List<HeroReactionDef>();
                            cachedActionReactions[trigger.action] = list;
                        }
                        list.Add(rDef);
                    }
                }
            }
        }

        public static void HandleIncident(string incidentDefName, IncidentParms parms)
        {
            if (string.IsNullOrEmpty(incidentDefName)) return;
            EnsureCache();

            if (!cachedIncidentReactions.TryGetValue(incidentDefName, out var reactions))
                return;

            for (int i = 0; i < reactions.Count; i++)
            {
                ApplyReactionToAllHeroes(reactions[i], null, -1);
            }
        }

        public static void HandleAction(string actionName, Pawn actor, Pawn victim)
        {
            if (string.IsNullOrEmpty(actionName)) return;
            EnsureCache();

            if (!cachedActionReactions.TryGetValue(actionName, out var reactions))
                return;

            int targetPawnID = victim != null ? victim.thingIDNumber : -1;
            for (int i = 0; i < reactions.Count; i++)
            {
                ApplyReactionToAllHeroes(reactions[i], actor, targetPawnID);
            }
        }

        private static void ApplyReactionToAllHeroes(HeroReactionDef reactionDef, Pawn actor, int targetPawnID)
        {
            var comp = GameComponent_Exiles.Instance;
            if (comp == null || comp.AllHeroes == null) return;

            var deltaDict = reactionDef.GetDeltaDictionary();
            if (deltaDict.Count == 0) return;

            int now = Find.TickManager.TicksGame;

            for (int i = 0; i < comp.AllHeroes.Count; i++)
            {
                var hero = comp.AllHeroes[i];
                if (hero == null || hero.pawn == null || hero.pawn.Dead || hero.hasDeparted)
                    continue;

                float effectiveDelta = CreedEvaluator.EvaluateDelta(hero.creed, deltaDict);

                // Áp dụng modifier từ Disposition nếu có
                if (hero.disposition != null)
                {
                    if (effectiveDelta > 0)
                        effectiveDelta *= hero.disposition.gainMultiplier;
                    else
                        effectiveDelta *= hero.disposition.lossMultiplier;
                }

                // Phanh P3: Chống nhiễu (|delta| >= 3.0 sau khi nhân weight)
                if (Mathf.Abs(effectiveDelta) >= 3.0f)
                {
                    string label = reactionDef.memoryLabel ?? reactionDef.label;
                    var mem = new HeroMemory(
                        reactionDef.defName,
                        label,
                        effectiveDelta,
                        now,
                        reactionDef.memoryDecayable,
                        reactionDef.memoryHalfLifeDays,
                        targetPawnID
                    );

                    hero.AddMemory(mem);

                    // Nếu có targetPawnID cụ thể và là hành vi tiêu cực nặng, nối sang Social thought
                    if (targetPawnID >= 0 && effectiveDelta < -8f && actor != null && hero.pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        var socialThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("RWX_Thought_Resentment");
                        if (socialThought != null)
                        {
                            hero.pawn.needs.mood.thoughts.memories.TryGainMemory(socialThought, actor);
                        }
                    }
                }
            }
        }
    }
}
