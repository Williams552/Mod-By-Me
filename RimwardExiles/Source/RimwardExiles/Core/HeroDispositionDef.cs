using System.Collections.Generic;
using Verse;

namespace RimwardExiles.Core
{
    public class HeroDispositionDef : Def
    {
        public string reason;
        public float gainMultiplier = 1.0f;
        public float lossMultiplier = 1.0f;
        public List<string> gatedOptions = new List<string>();
        public List<string> bonusOptions = new List<string>();
        public List<string> replaceableBy = new List<string>();
    }
}
