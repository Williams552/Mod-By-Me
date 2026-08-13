using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using UnityEngine;
using EchoResonance.Buildings;

namespace EchoResonance.Core
{
    public class EchoWorldComponent : WorldComponent
    {
        private float storedEcho = 0f;
        private int lastAccrualTick = 0;
        private float cachedMultiplier = 1.0f;
        private bool hasFirstLevel20Occurred = false;

        public float StoredEcho => storedEcho;
        public float CachedMultiplier => cachedMultiplier;
        public bool HasFirstLevel20Occurred => hasFirstLevel20Occurred;

        public EchoWorldComponent(World world) : base(world)
        {
        }

        public static EchoWorldComponent Instance => Find.World.GetComponent<EchoWorldComponent>();

        public bool IsResonatorActiveOnAnyMap(out Building_Resonator activeResonator)
        {
            activeResonator = null;
            if (Find.Maps == null) return false;

            foreach (var map in Find.Maps)
            {
                var resonator = map.listerBuildings.AllBuildingsColonistOfClass<Building_Resonator>().Cast<Building_Resonator>().FirstOrDefault();
                if (resonator != null && resonator.IsActive)
                {
                    activeResonator = resonator;
                    return true;
                }
            }
            return false;
        }

        public void AddEcho(float amount, string sourceReason = null, bool force = false)
        {
            if (!force && !IsResonatorActiveOnAnyMap(out var resonator))
            {
                // Cannot accumulate Echo without an active Powered Resonator
                return;
            }

            float finalAmount = amount * (force ? 1.0f : cachedMultiplier);
            storedEcho += finalAmount;

            if (!sourceReason.NullOrEmpty() && finalAmount > 0.1f)
            {
                Messages.Message($"[Echo Resonance] +{finalAmount:F1} Echo gained ({sourceReason}). Total: {storedEcho:F1}", MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public bool TrySpendEcho(float amount)
        {
            if (storedEcho >= amount)
            {
                storedEcho -= amount;
                return true;
            }
            return false;
        }

        public void WipePool(string reason)
        {
            if (storedEcho > 0f)
            {
                Find.LetterStack.ReceiveLetter(
                    "Echo Resonance Disrupted!",
                    $"The Archotech Resonator was destroyed! {storedEcho:F1} Echo points have dissolved into thin air. Existing pawn perks remain intact.",
                    LetterDefOf.ThreatBig
                );
                storedEcho = 0f;
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (Find.TickManager.TicksGame % 250 == 0)
            {
                RecalculateMultiplier();
            }

            // Daily Passive Tick (60,000 ticks = 1 in-game day)
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                if (IsResonatorActiveOnAnyMap(out _))
                {
                    float dailyBase = EchoResonanceMod.Settings?.baseDailyPassiveEcho ?? EchoTuning.EchoSkillLevel1_10;
                    AddEcho(dailyBase, "Daily Passive Resonance");
                }
            }
        }

        public void RecalculateMultiplier()
        {
            if (IsResonatorActiveOnAnyMap(out var resonator))
            {
                cachedMultiplier = resonator.CalculatePylonMultiplier();
            }
            else
            {
                cachedMultiplier = 1.0f;
            }
        }

        public void SetFirstLevel20Reached()
        {
            hasFirstLevel20Occurred = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref storedEcho, "storedEcho", 0f);
            Scribe_Values.Look(ref lastAccrualTick, "lastAccrualTick", 0);
            Scribe_Values.Look(ref cachedMultiplier, "cachedMultiplier", 1.0f);
            Scribe_Values.Look(ref hasFirstLevel20Occurred, "hasFirstLevel20Occurred", false);
        }
    }
}
