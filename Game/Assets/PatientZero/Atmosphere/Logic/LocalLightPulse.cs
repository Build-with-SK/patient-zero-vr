using System;
using PatientZero.Core;

namespace PatientZero.Atmosphere.Logic
{
    /// <summary>Replaces a scheduled flicker edge with one local 250 ms spike, then holds other flicker stable.</summary>
    public sealed class LocalLightPulse
    {
        private double start = double.PositiveInfinity, end, resume;
        private long startStep = long.MaxValue;
        public void Reset() { start = double.PositiveInfinity; end = resume = 0; startStep = long.MaxValue; }
        public void Request(double seconds, bool reduceFlicker)
        {
            if (reduceFlicker || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds < resume) return;
            startStep = LightingPresets.FlickerStep(seconds) + 1;
            start = startStep * .55;
            end = start + .25; resume = end + 1.1;
        }
        public float Sample(double seconds, int fixture, bool reduceFlicker)
        {
            if (reduceFlicker) return .8f;
            if (LightingPresets.FlickerStep(seconds) >= startStep && seconds < resume)
                return fixture == 1 && seconds < end ? 1.3f : LightingPresets.Variation(LightingState.Awakening, (startStep - 1) * .55 + .1, false, fixture + 7);
            return LightingPresets.Variation(LightingState.Awakening, seconds, false, fixture + 7);
        }
    }
}
