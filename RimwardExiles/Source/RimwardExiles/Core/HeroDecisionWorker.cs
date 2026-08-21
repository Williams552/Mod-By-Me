using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public static class HeroDecisionWorker
    {
        public static bool CanFire(HeroDecisionDef def, Map map)
        {
            if (def == null) return false;
            var comp = GameComponent_Exiles.Instance;
            if (comp == null || comp.AllHeroes == null || comp.AllHeroes.Count == 0)
                return false;

            return true;
        }

        public static void FireDecision(HeroDecisionDef def, Map map)
        {
            if (def == null) return;

            var node = new DiaNode(def.letterText ?? def.description ?? "Một quyết định quan trọng cần bạn giải quyết.");

            for (int i = 0; i < def.options.Count; i++)
            {
                var opt = def.options[i];
                var diaOpt = new DiaOption(opt.label)
                {
                    action = () => ApplyOption(def, opt, map),
                    resolveTree = true
                };

                // Kiểm tra điều kiện chi phí bạc
                if (opt.silverCost > 0 && map != null)
                {
                    int totalSilver = 0;
                    var silverThings = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
                    for (int s = 0; s < silverThings.Count; s++) totalSilver += silverThings[s].stackCount;

                    if (totalSilver < opt.silverCost)
                    {
                        diaOpt.Disable($"Không đủ bạc (Yêu cầu: {opt.silverCost}, Hiện có: {totalSilver})");
                    }
                }

                node.options.Add(diaOpt);
            }

            var dialog = new Dialog_NodeTree(node, true, true, def.letterLabel ?? def.label);
            Find.WindowStack.Add(dialog);
        }

        public static void ApplyOption(HeroDecisionDef def, DecisionOption opt, Map map)
        {
            if (opt == null) return;
            var comp = GameComponent_Exiles.Instance;
            if (comp == null) return;

            int now = Find.TickManager.TicksGame;

            // 1. Trừ bạc nếu có
            if (opt.silverCost > 0 && map != null)
            {
                int remainingToPay = opt.silverCost;
                var silverThings = new List<Thing>(map.listerThings.ThingsOfDef(ThingDefOf.Silver));
                for (int i = 0; i < silverThings.Count && remainingToPay > 0; i++)
                {
                    int take = Mathf.Min(silverThings[i].stackCount, remainingToPay);
                    silverThings[i].SplitOff(take).Destroy();
                    remainingToPay -= take;
                }
                Messages.Message($"Đã chi trả {opt.silverCost} Bạc cho quyết định.", MessageTypeDefOf.TaskCompletion);
            }

            // 2. Cập nhật Goodwill Faction nếu có
            if (opt.factionGoodwill != null)
            {
                for (int i = 0; i < opt.factionGoodwill.Count; i++)
                {
                    var fw = opt.factionGoodwill[i];
                    if (fw.faction != null)
                    {
                        Faction f = Find.FactionManager.FirstFactionOfDef(fw.faction);
                        if (f != null && !f.IsPlayer)
                        {
                            f.TryAffectGoodwillWith(Faction.OfPlayer, fw.amount, true, true, null);
                        }
                    }
                }
            }

            // 3. Chuyển đổi delta thành Dictionary
            var deltaDict = new Dictionary<HeroValueDef, float>();
            for (int i = 0; i < opt.delta.Count; i++)
            {
                if (opt.delta[i].axis != null)
                {
                    deltaDict[opt.delta[i].axis] = opt.delta[i].amount;
                }
            }

            // 4. Áp dụng cho từng Hero
            for (int i = 0; i < comp.AllHeroes.Count; i++)
            {
                var hero = comp.AllHeroes[i];
                if (hero == null || hero.pawn == null || hero.hasDeparted) continue;

                // Gán Disposition nếu có
                if (opt.givesDisposition != null)
                {
                    hero.disposition = opt.givesDisposition;
                }

                // Xử lý giải toả Tension
                if (opt.tensionBoostAxis != null && hero.creed != null)
                {
                    hero.creed.SetOrUpdateWeight(opt.tensionBoostAxis, +0.15f);
                    if (opt.tensionReduceAxis != null)
                    {
                        hero.creed.SetOrUpdateWeight(opt.tensionReduceAxis, -0.10f);
                    }

                    // Xoá hediff Conflicted và thưởng loyalty
                    var conflictedHediff = DefDatabase<HediffDef>.GetNamedSilentFail("RWX_Hediff_Conflicted");
                    if (conflictedHediff != null && hero.pawn.health?.hediffSet != null)
                    {
                        var h = hero.pawn.health.hediffSet.GetFirstHediffOfDef(conflictedHediff);
                        if (h != null) hero.pawn.health.RemoveHediff(h);
                    }

                    hero.loyalty = Mathf.Clamp(hero.loyalty + 8f, 0f, 100f);
                    hero.activeTensionResolvedTick = now;
                }

                // Tính toán thay đổi Loyalty qua Creed
                float effectiveDelta = CreedEvaluator.EvaluateDelta(hero.creed, deltaDict);
                if (hero.disposition != null)
                {
                    if (effectiveDelta > 0) effectiveDelta *= hero.disposition.gainMultiplier;
                    else effectiveDelta *= hero.disposition.lossMultiplier;
                }

                if (opt.createsMemory && Mathf.Abs(effectiveDelta) >= 1.0f)
                {
                    var mem = new HeroMemory(
                        def.defName,
                        opt.label,
                        effectiveDelta,
                        now,
                        opt.memoryDecayable,
                        opt.memoryHalfLifeDays,
                        -1
                    );
                    hero.AddMemory(mem);
                }
            }

            Messages.Message($"Quyết định đã được thực thi: {opt.label}", MessageTypeDefOf.NeutralEvent);
        }
    }

    public class IncidentWorker_HeroDecision : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            var comp = GameComponent_Exiles.Instance;
            return comp != null && comp.AllHeroes != null && comp.AllHeroes.Count > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map ?? Find.CurrentMap;
            var allDecisions = DefDatabase<HeroDecisionDef>.AllDefsListForReading;
            if (allDecisions == null || allDecisions.Count == 0) return false;

            var validDecisions = new List<HeroDecisionDef>();
            for (int i = 0; i < allDecisions.Count; i++)
            {
                if (HeroDecisionWorker.CanFire(allDecisions[i], map))
                {
                    validDecisions.Add(allDecisions[i]);
                }
            }

            if (validDecisions.Count == 0) return false;

            var decision = validDecisions.RandomElementByWeight(d => d.baseWeight);
            HeroDecisionWorker.FireDecision(decision, map);
            return true;
        }
    }
}
