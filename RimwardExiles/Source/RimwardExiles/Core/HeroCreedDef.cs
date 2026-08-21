using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public class CreedValueEntry
    {
        public HeroValueDef value;
        public float weight; // -1.0 .. +1.0
    }

    public class CreedTensionEntry
    {
        public HeroValueDef between;
        public HeroValueDef and;
        public string note;

        public bool Involves(HeroValueDef a, HeroValueDef b)
        {
            return (between == a && and == b) || (between == b && and == a);
        }
    }

    public class HeroCreedDef : Def
    {
        public List<CreedValueEntry> values = new List<CreedValueEntry>();
        public List<CreedTensionEntry> tensions = new List<CreedTensionEntry>();

        public float GetWeight(HeroValueDef valueDef)
        {
            if (valueDef == null || values == null) return 0f;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].value == valueDef)
                    return Mathf.Clamp(values[i].weight, -1f, 1f);
            }
            return 0f;
        }

        public void SetOrUpdateWeight(HeroValueDef valueDef, float deltaWeight)
        {
            if (valueDef == null) return;
            if (values == null) values = new List<CreedValueEntry>();

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].value == valueDef)
                {
                    values[i].weight = Mathf.Clamp(values[i].weight + deltaWeight, -1f, 1f);
                    return;
                }
            }

            values.Add(new CreedValueEntry
            {
                value = valueDef,
                weight = Mathf.Clamp(deltaWeight, -1f, 1f)
            });
        }

        public bool HasTension(HeroValueDef a, HeroValueDef b, out CreedTensionEntry matchedTension)
        {
            matchedTension = null;
            if (tensions == null) return false;
            for (int i = 0; i < tensions.Count; i++)
            {
                if (tensions[i].Involves(a, b))
                {
                    matchedTension = tensions[i];
                    return true;
                }
            }
            return false;
        }
    }
}
