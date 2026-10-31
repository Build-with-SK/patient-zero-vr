using PatientZero.Core;

namespace PatientZero.Atmosphere.Logic
{
    /// <summary>Six ward fixtures: B-7 fill/window, two corridor fixtures, two emergency lamps. v1.3 (E-19): fixtures from
    /// index <see cref="WardFixtures"/> on light the lower floors, see <see cref="FloorFixtureEnabled"/>.</summary>
    public static class FixturePolicy
    {
        public const int WardFixtures = 6;
        /// <summary>Lower-floor fixtures: lit in every lit preset, dark in a blackout. They are too far from the ward to reach it.</summary>
        public static bool FloorFixtureEnabled(LightingState state) =>
            state == LightingState.WardDim || state == LightingState.Surge || state == LightingState.EmergencyRed || state == LightingState.ExitDaylight;
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
        public static bool ShowTraces(ExperienceSettings settings) => settings.Scares == ScareIntensity.Standard;
    }
}
