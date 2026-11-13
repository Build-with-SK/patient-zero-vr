using System;

namespace PatientZero.Audio.Logic
{
    /// <summary>Explicit intensity changes on zombie shuffle loops also change their cadence.
    /// .6 is the quiet walking bed; 1 is a heavier run at 1.65 times the original rate.
    /// Voices never updated through SetIntensity retain their original playback rate.</summary>
    public static class ShuffleCadence
    {
        public static float PitchMultiplier(float intensity)
        {
            if (float.IsNaN(intensity) || float.IsInfinity(intensity)) return 1;
            return 1 + .65f * Math.Max(0, Math.Min(1, (intensity - .6f) / .4f));
        }
    }
}
