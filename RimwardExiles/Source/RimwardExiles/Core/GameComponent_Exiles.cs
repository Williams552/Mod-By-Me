using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public class GameComponent_Exiles : GameComponent
    {
        public const int TickInterval = 2500; // ~1 giờ in-game
        public const int CurrentSaveVersion = 1;

        private int saveVersion = CurrentSaveVersion;
        private List<HeroState> heroStates = new List<HeroState>();

        public static GameComponent_Exiles Instance => Current.Game?.GetComponent<GameComponent_Exiles>();

        public List<HeroState> AllHeroes => heroStates;

        public GameComponent_Exiles(Game game) : base()
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // Validate & cache
            BodyPathClassifier.ClearCache();
            EffectResolver.ClearCache();

            // Dọn dẹp trạng thái lỗi sau khi load
            if (heroStates == null)
            {
                heroStates = new List<HeroState>();
            }
            else
            {
                heroStates.RemoveAll(h => h == null || h.pawn == null);
            }

            Log.Message($"[Rimward Exiles] GameComponent initialized with {heroStates.Count} tracked heroes.");
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager.TicksGame % TickInterval != 0) return;

            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < heroStates.Count; i++)
            {
                var hero = heroStates[i];
                if (hero == null || hero.pawn == null || hero.pawn.Dead || hero.hasDeparted)
                    continue;

                TickHeroLoyalty(hero, now);
            }
        }

        private void TickHeroLoyalty(HeroState hero, int currentTick)
        {
            // Dọn dẹp memory hết hạn
            hero.CleanExpiredMemories(currentTick);

            // Tính target loyalty và áp dụng trôi (drift)
            // Sẽ được mở rộng chi tiết ở Milestone 3
            float calculatedTarget = LoyaltyCalculator.CalculateTargetLoyalty(hero);
            hero.targetLoyalty = calculatedTarget;

            // Áp dụng công thức trôi: loyalty += clamp((target - loyalty) * 0.08, -0.8, +0.5)
            float diff = hero.targetLoyalty - hero.loyalty;
            float step = diff * 0.08f;
            step = Mathf.Clamp(step, -0.8f, 0.5f);

            hero.loyalty = Mathf.Clamp(hero.loyalty + step, 0f, 100f);
            hero.lastDriftDelta = step;

            // Kiểm tra ngưỡng bất mãn và rời đi
            CheckThresholds(hero, currentTick);
        }

        private void CheckThresholds(HeroState hero, int currentTick)
        {
            if (hero.loyalty < 10f)
            {
                hero.ticksBelowCritical += TickInterval;

                // Nếu < 10 liên tiếp 3 ngày (180,000 ticks) và không trong thời gian miễn nhiễm thảm hoạ
                if (hero.ticksBelowCritical >= 180000 && !hero.IsImmuneToDeparture(currentTick))
                {
                    ExecuteDeparture(hero);
                }
            }
            else
            {
                hero.ticksBelowCritical = 0;
            }
        }

        private void ExecuteDeparture(HeroState hero)
        {
            if (hero.hasDeparted || hero.pawn == null) return;

            Pawn p = hero.pawn;

            // Không cho rời đi nếu đang bị bắt, đang downed hoặc đang trong caravan (02 mục 10)
            if (p.Downed || p.IsPrisoner || RimWorld.Planet.CaravanUtility.IsCaravanMember(p))
                return;

            hero.hasDeparted = true;
            hero.departureReason = "Lòng trung thành suy kiệt dưới mức tới hạn trong 3 ngày liên tiếp.";

            Messages.Message($"[Rimward Exiles] {p.LabelShort} đã hoàn toàn mất niềm tin vào thuộc địa và quyết định ra đi.", MessageTypeDefOf.NegativeEvent);

            if (p.Faction == Faction.OfPlayer)
            {
                p.SetFaction(null);
            }

            // Ghi nhận Thought cho toàn colony: [Hero] đã rời đi
            var colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;
            var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("RWX_Thought_HeroDeparted");
            if (colonists != null && thoughtDef != null)
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    var c = colonists[i];
                    if (c != null && c != p && c.needs?.mood?.thoughts?.memories != null)
                    {
                        c.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                    }
                }
            }

            if (p.Spawned && p.Map != null)
            {
                p.jobs?.StopAll();
            }
        }

        public bool IsHero(Pawn pawn)
        {
            if (pawn == null || heroStates == null) return false;
            for (int i = 0; i < heroStates.Count; i++)
            {
                if (heroStates[i].pawn == pawn && !heroStates[i].hasDeparted)
                    return true;
            }
            return false;
        }

        public HeroState GetHeroState(Pawn pawn)
        {
            if (pawn == null || heroStates == null) return null;
            for (int i = 0; i < heroStates.Count; i++)
            {
                if (heroStates[i].pawn == pawn)
                    return heroStates[i];
            }
            return null;
        }

        public HeroState RegisterHero(Pawn pawn, HeroCreedDef creed)
        {
            if (pawn == null) return null;

            var existing = GetHeroState(pawn);
            if (existing != null)
            {
                if (creed != null) existing.creed = creed;
                existing.hasDeparted = false;
                return existing;
            }

            var newState = new HeroState(pawn, creed);
            heroStates.Add(newState);
            Log.Message($"[Rimward Exiles] Registered new Hero: {pawn.LabelShort} with creed {creed?.defName ?? "None"}");
            return newState;
        }

        public void UnregisterHero(Pawn pawn)
        {
            if (pawn == null || heroStates == null) return;
            heroStates.RemoveAll(h => h.pawn == pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref saveVersion, "saveVersion", CurrentSaveVersion);
            Scribe_Collections.Look(ref heroStates, "heroStates", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (heroStates == null) heroStates = new List<HeroState>();
                Migrate();
            }
        }

        private void Migrate()
        {
            if (saveVersion < CurrentSaveVersion)
            {
                Log.Message($"[Rimward Exiles] Migrating save from v{saveVersion} to v{CurrentSaveVersion}");
                saveVersion = CurrentSaveVersion;
            }
        }
    }
}
