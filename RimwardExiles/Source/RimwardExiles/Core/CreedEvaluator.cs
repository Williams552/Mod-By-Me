using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimwardExiles.Core
{
    public static class CreedEvaluator
    {
        public const float TensionDeltaThreshold = 8.0f;

        /// <summary>
        /// Calculates dot product of deltaVector and creed weights: Σ (delta[axis] * weight[axis])
        /// </summary>
        public static float EvaluateDelta(HeroCreedDef creed, Dictionary<HeroValueDef, float> deltaVector)
        {
            if (creed == null || deltaVector == null || deltaVector.Count == 0)
                return 0f;

            float total = 0f;
            foreach (var kvp in deltaVector)
            {
                if (kvp.Key == null) continue;
                float weight = creed.GetWeight(kvp.Key);
                total += kvp.Value * weight;
            }

            return total;
        }

        /// <summary>
        /// Evaluates single axis impact on loyalty.
        /// </summary>
        public static float EvaluateSingle(HeroCreedDef creed, HeroValueDef axis, float delta)
        {
            if (creed == null || axis == null) return 0f;
            return delta * creed.GetWeight(axis);
        }

        /// <summary>
        /// Checks if an event triggers any tension in the hero's creed.
        /// Tension triggers when two axes marked in tension have opposing effective deltas (one positive, one negative)
        /// and both exceed the absolute threshold (|delta * weight| >= 8).
        /// </summary>
        public static bool CheckTension(
            HeroCreedDef creed,
            Dictionary<HeroValueDef, float> deltaVector,
            out CreedTensionEntry triggeredTension,
            out HeroValueDef axisA,
            out HeroValueDef axisB)
        {
            triggeredTension = null;
            axisA = null;
            axisB = null;

            if (creed == null || creed.tensions == null || deltaVector == null || deltaVector.Count < 2)
                return false;

            for (int i = 0; i < creed.tensions.Count; i++)
            {
                var tension = creed.tensions[i];
                if (tension.between == null || tension.and == null) continue;

                if (deltaVector.TryGetValue(tension.between, out float delta1) &&
                    deltaVector.TryGetValue(tension.and, out float delta2))
                {
                    float eff1 = delta1 * creed.GetWeight(tension.between);
                    float eff2 = delta2 * creed.GetWeight(tension.and);

                    // Opposing signs and both >= TensionDeltaThreshold
                    if (Mathf.Abs(eff1) >= TensionDeltaThreshold &&
                        Mathf.Abs(eff2) >= TensionDeltaThreshold &&
                        Math.Sign(eff1) != Math.Sign(eff2))
                    {
                        triggeredTension = tension;
                        axisA = tension.between;
                        axisB = tension.and;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
