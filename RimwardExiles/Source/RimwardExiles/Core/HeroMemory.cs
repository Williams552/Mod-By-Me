using Verse;

namespace RimwardExiles.Core
{
    public class HeroMemory : IExposable
    {
        public string sourceDefName;
        public int targetPawnID = -1;
        public int tickOccurred;
        public float initialWeight;
        public bool decayable = true;
        public float halfLifeDays = 20f;
        public string label;

        public HeroMemory() { }

        public HeroMemory(string sourceDefName, string label, float initialWeight, int tickOccurred, bool decayable = true, float halfLifeDays = 20f, int targetPawnID = -1)
        {
            this.sourceDefName = sourceDefName;
            this.label = label;
            this.initialWeight = initialWeight;
            this.tickOccurred = tickOccurred;
            this.decayable = decayable;
            this.halfLifeDays = halfLifeDays;
            this.targetPawnID = targetPawnID;
        }

        public float GetCurrentWeight(int currentTick)
        {
            if (!decayable || halfLifeDays <= 0f)
                return initialWeight;

            float daysElapsed = (currentTick - tickOccurred) / 60000f;
            if (daysElapsed <= 0f) return initialWeight;

            return initialWeight * UnityEngine.Mathf.Pow(0.5f, daysElapsed / halfLifeDays);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref sourceDefName, "sourceDefName");
            Scribe_Values.Look(ref targetPawnID, "targetPawnID", -1);
            Scribe_Values.Look(ref tickOccurred, "tickOccurred");
            Scribe_Values.Look(ref initialWeight, "initialWeight");
            Scribe_Values.Look(ref decayable, "decayable", true);
            Scribe_Values.Look(ref halfLifeDays, "halfLifeDays", 20f);
            Scribe_Values.Look(ref label, "label");
        }
    }
}
