using NUnit.Framework;
using PatientZero.Audio.Logic;
using PatientZero.Core;

namespace PatientZero.Audio.Tests
{
    public sealed class ShuffleCadenceTests
    {
        [Test] public void ExplicitIntensityMakesTheRunCadenceFasterThanTheWalk()
        {
            Assert.AreEqual(1f, ShuffleCadence.PitchMultiplier(.6f), .0001f);
            Assert.AreEqual(1.325f, ShuffleCadence.PitchMultiplier(.8f), .0001f);
            Assert.AreEqual(1.65f, ShuffleCadence.PitchMultiplier(1), .0001f);
        }
        [Test] public void RateIsBoundedAndBadInputIsHarmless()
        {
            Assert.AreEqual(1f, ShuffleCadence.PitchMultiplier(-10));
            Assert.AreEqual(1.65f, ShuffleCadence.PitchMultiplier(10), .0001f);
            Assert.AreEqual(1f, ShuffleCadence.PitchMultiplier(float.NaN));
            Assert.AreEqual(1f, ShuffleCadence.PitchMultiplier(float.PositiveInfinity));
        }
        [Test] public void ShuffleRemainsANonGameplayNoiseLoop()
        {
            Assert.IsTrue(CueTable.TryGet(CueId.ZombieShuffle, out var cue));
            Assert.AreEqual(0f, cue.NoiseRadius); Assert.IsTrue(cue.Loop);
        }
    }
}
