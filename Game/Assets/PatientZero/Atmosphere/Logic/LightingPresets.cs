using System;
using PatientZero.Core;

namespace PatientZero.Atmosphere.Logic
{
    public readonly struct LightingPreset
    {
        public readonly float Ambient, FogNear, FogFar, FogDensity, FixtureGain, Transition;
        public readonly bool ExponentialFog;
        public LightingPreset(float ambient, float near, float far, float density, float gain, float transition)
        { Ambient = ambient; FogNear = near; FogFar = far; FogDensity = density; ExponentialFog = density > 0; FixtureGain = gain; Transition = transition; }
    }
    /// <summary>Quest-safe lighting targets. Variation has at most two discrete transitions per second.</summary>
    public static class LightingPresets
    {
        public static LightingPreset Get(LightingState state, float brightness = .5f)
        {
            switch (state)
            {
                case LightingState.Awakening: return new LightingPreset(.10f, 2, 12, 0, 1, 0);
                case LightingState.WardDim: return new LightingPreset(.08f, 3, 15, 0, .4f, 1);
                case LightingState.Surge: return new LightingPreset(.08f, 3, 15, 0, 1.3f, .6f);
                case LightingState.Blackout: return new LightingPreset(.015f + Math.Max(0, Math.Min(1, brightness)) * .02f, 0, 0, .12f, 0, 1.2f);
                case LightingState.EmergencyRed: return new LightingPreset(.03f, 2, 10, 0, 1, 1.5f);
                case LightingState.ExitDaylight: return new LightingPreset(1, 2, 30, 0, 1, 2);
                default: return new LightingPreset(.08f, 3, 15, 0, 0, 0);
            }
        }
        public static float Variation(LightingState state, double seconds, bool reduceFlicker, int seed)
        {
            if (state == LightingState.Awakening)
            {
                if (reduceFlicker) return .8f;
                // Stateless seeded hash is stable across frame rates, with 550 ms between changes.
                long phase = (long)Math.Floor(Math.Max(0, seconds) / .55);
                uint hash = unchecked((uint)(phase * 747796405L + seed * 2891336453L));
                hash = (hash ^ (hash >> 16)) * 2246822519u;
                return .6f + (hash & 1023) / 1023f * .4f;
            }
            if (state == LightingState.EmergencyRed)
                return reduceFlicker ? .75f : ((long)Math.Floor(Math.Max(0, seconds)) % 2 == 0 ? .5f : 1);
            return 1;
        }
        public static float Surge(float from, float seconds) => from + (1.3f - from) * Math.Max(0, Math.Min(1, seconds / .6f));
    }
}
