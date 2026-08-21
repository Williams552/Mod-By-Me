using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public enum LoyaltyTier
    {
        Devoted,    // 70 - 100
        Normal,     // 40 - 69
        Discontent, // 25 - 39 (Cảnh báo bất mãn)
        Critical,   // 10 - 24 (Chuẩn bị rời đi, mở decision cứu vãn)
        Leaving     // < 10 liên tiếp 3 ngày -> Rời đi
    }

    public class HeroState : IExposable
    {
        public Pawn pawn;
        public HeroCreedDef creed;
        public float loyalty = 65f;
        public float targetLoyalty = 65f;
        public float lastDriftDelta = 0f;
        public List<HeroMemory> memories = new List<HeroMemory>();
        public HeroDispositionDef disposition;

        public int daysBelowDiscontent = 0;
        public int ticksBelowCritical = 0;
        public int disasterImmunityUntilTick = -1;

        public bool warnedDiscontent = false;
        public bool warnedCritical = false;
        public bool hasDeparted = false;
        public string departureReason;
        public int activeTensionResolvedTick = -1;

        public HeroState() { }

        public HeroState(Pawn pawn, HeroCreedDef creed)
        {
            this.pawn = pawn;
            this.creed = creed;
            this.loyalty = 65f;
            this.targetLoyalty = 65f;
            this.memories = new List<HeroMemory>();
        }

        public LoyaltyTier CurrentTier
        {
            get
            {
                if (hasDeparted) return LoyaltyTier.Leaving;
                if (loyalty >= 70f) return LoyaltyTier.Devoted;
                if (loyalty >= 40f) return LoyaltyTier.Normal;
                if (loyalty >= 25f) return LoyaltyTier.Discontent;
                if (loyalty >= 10f) return LoyaltyTier.Critical;
                return LoyaltyTier.Leaving;
            }
        }

        public bool IsImmuneToDeparture(int currentTick)
        {
            return currentTick < disasterImmunityUntilTick;
        }

        public void TriggerDisasterImmunity(int durationTicks = 300000) // 5 ngày in-game
        {
            int now = Find.TickManager.TicksGame;
            disasterImmunityUntilTick = Mathf.Max(disasterImmunityUntilTick, now + durationTicks);
        }

        public void AddMemory(HeroMemory memory)
        {
            if (memory == null) return;
            if (memories == null) memories = new List<HeroMemory>();
            memories.Add(memory);
        }

        public float GetTotalMemoryWeight(int currentTick)
        {
            if (memories == null || memories.Count == 0) return 0f;

            float positiveSum = 0f;
            float negativeSum = 0f;

            for (int i = 0; i < memories.Count; i++)
            {
                float w = memories[i].GetCurrentWeight(currentTick);
                if (w > 0f) positiveSum += w;
                else negativeSum += w;
            }

            // Cap theo tài liệu 02: Dương tối đa +30, Âm tối đa -40
            positiveSum = Mathf.Min(positiveSum, 30f);
            negativeSum = Mathf.Max(negativeSum, -40f);

            return positiveSum + negativeSum;
        }

        public void CleanExpiredMemories(int currentTick)
        {
            if (memories == null) return;
            memories.RemoveAll(m => m.decayable && Mathf.Abs(m.GetCurrentWeight(currentTick)) < 0.2f);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref creed, "creed");
            Scribe_Values.Look(ref loyalty, "loyalty", 65f);
            Scribe_Values.Look(ref targetLoyalty, "targetLoyalty", 65f);
            Scribe_Values.Look(ref lastDriftDelta, "lastDriftDelta", 0f);
            Scribe_Collections.Look(ref memories, "memories", LookMode.Deep);
            Scribe_Defs.Look(ref disposition, "disposition");

            Scribe_Values.Look(ref daysBelowDiscontent, "daysBelowDiscontent", 0);
            Scribe_Values.Look(ref ticksBelowCritical, "ticksBelowCritical", 0);
            Scribe_Values.Look(ref disasterImmunityUntilTick, "disasterImmunityUntilTick", -1);

            Scribe_Values.Look(ref warnedDiscontent, "warnedDiscontent", false);
            Scribe_Values.Look(ref warnedCritical, "warnedCritical", false);
            Scribe_Values.Look(ref hasDeparted, "hasDeparted", false);
            Scribe_Values.Look(ref departureReason, "departureReason");
            Scribe_Values.Look(ref activeTensionResolvedTick, "activeTensionResolvedTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (memories == null) memories = new List<HeroMemory>();
            }
        }
    }
}
