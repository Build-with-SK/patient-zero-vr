using System.Collections.Generic;
using NUnit.Framework;
using PatientZero.Atmosphere.Logic;
using PatientZero.Core;

namespace PatientZero.Atmosphere.Tests
{
    public sealed class FixturePolicyTests
    {
        [Test] public void EveryActLightsOnlyItsIntendedFixturesAndBlackoutHasNone()
        {
            var states = new[] { LightingState.Awakening, LightingState.WardDim, LightingState.Surge, LightingState.Blackout, LightingState.EmergencyRed, LightingState.ExitDaylight };
            var expected = new[] { 2, 3, 6, 0, 2, 2 };
            for (int n = 0; n < states.Length; n++)
            {
                int count = 0; for (int i = 0; i < 6; i++) if (FixturePolicy.Enabled(states[n], i)) count++;
                Assert.AreEqual(expected[n], count, states[n].ToString());
                Assert.IsFalse(FixturePolicy.Enabled(states[n], -1)); Assert.IsFalse(FixturePolicy.Enabled(states[n], 6));
            }
        }
        [Test] public void OneDamagedTubeFlickersWhileFillStaysSteadyAndReducedRemovesVariation()
        {
            var pulse = new LocalLightPulse(); var changes = new Queue<double>();
            float previous = FixturePolicy.Variation(LightingState.Awakening, 0, false, 1, pulse);
            for (int ms = 0; ms < 600000; ms++)
            {
                double time = ms / 1000d;
                Assert.AreEqual(.8f, FixturePolicy.Variation(LightingState.Awakening, time, false, 0, pulse));
                Assert.AreEqual(.8f, FixturePolicy.Variation(LightingState.Awakening, time, true, 1, pulse));
                float now = FixturePolicy.Variation(LightingState.Awakening, time, false, 1, pulse);
                if (now == previous) continue;
                previous = now; changes.Enqueue(time);
                while (changes.Count > 0 && time - changes.Peek() >= 1) changes.Dequeue();
                Assert.LessOrEqual(changes.Count, 3);
            }
        }
        [Test] public void HeadlampNeverOverridesBlackoutOrFinalRevealLighting()
        {
            Assert.IsTrue(FixturePolicy.HeadlampAllowed(LightingState.Awakening));
            Assert.IsTrue(FixturePolicy.HeadlampAllowed(LightingState.WardDim));
            foreach (var state in new[] { LightingState.Surge, LightingState.Blackout, LightingState.EmergencyRed, LightingState.ExitDaylight })
                Assert.IsFalse(FixturePolicy.HeadlampAllowed(state));
        }
        [Test] public void ReducedScaresSuppressOptionalSurfaceTraces()
        {
            var settings = ExperienceSettings.Defaults; Assert.IsTrue(FixturePolicy.ShowTraces(settings));
            settings.Scares = ScareIntensity.Reduced; Assert.IsFalse(FixturePolicy.ShowTraces(settings));
            settings.Scares = ScareIntensity.Standard; Assert.IsTrue(FixturePolicy.ShowTraces(settings));
        }
    }
}
