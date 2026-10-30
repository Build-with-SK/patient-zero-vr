using PatientZero.Core;

namespace PatientZero.Atmosphere.Logic
{
    /// <summary>Six existing fixtures only: B-7 fill/window, two corridor fixtures, two emergency lamps.</summary>
    public static class FixturePolicy
    {
        public static bool Enabled(LightingState state, int fixture)
        {
            if (fixture < 0 || fixture >= 6) return false;
            switch (state)
            {
                case LightingState.Awakening: return fixture < 2;
                case LightingState.WardDim: return fixture >= 1 && fixture <= 3;
                case LightingState.Surge: return true;
                case LightingState.EmergencyRed: case LightingState.ExitDaylight: return fixture >= 4;
                default: return false;
            }
        }
        public static float Variation(LightingState state, double age, bool reduced, int fixture, LocalLightPulse pulse)
        {
            if (state == LightingState.Awakening)
                return fixture == 1 ? pulse.Sample(age, fixture, reduced) : .8f;
            return LightingPresets.Variation(state, age, reduced, fixture + 7);
        }
        public static bool HeadlampAllowed(LightingState state) => state == LightingState.Awakening || state == LightingState.WardDim;
        public static bool ShowTraces(ExperienceSettings settings) => settings.Scares == ScareIntensity.Standard;
    }
}
