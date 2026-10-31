using System;
using NUnit.Framework;
using PatientZero.Audio.Logic;
using PatientZero.Core;

namespace PatientZero.Audio.Tests
{
    public sealed class ProceduralSoundTests
    {
        private static float[] Render(CueId id) { Assert.IsTrue(CueTable.TryGet(id, out var row)); return ProceduralSound.Render(row); }
        [Test] public void EveryPlayableCueHasFiniteBoundedAudioWithClickFreeEndpoints()
        {
            long bytes = 0;
            for (int i = 0; i < CueTable.Count; i++)
            {
                var row = CueTable.At(i); var data = ProceduralSound.Render(row); bytes += data.Length * 4L;
                if (row.Id == CueId.None) { Assert.AreEqual(0, data.Length); continue; }
                Assert.AreEqual((int)(ProceduralSound.Duration(row) * ProceduralSound.SampleRate), data.Length);
                double energy = 0;
                foreach (var sample in data)
                {
                    Assert.IsFalse(float.IsNaN(sample) || float.IsInfinity(sample), row.Id.ToString());
                    Assert.LessOrEqual(Math.Abs(sample), ProceduralSound.PeakLimit);
                    energy += sample * sample;
                }
                Assert.Greater(energy, .0001, row.Id.ToString());
                Assert.AreEqual(0, data[0]); Assert.AreEqual(0, data[data.Length - 1]);
            }
            Assert.Less(bytes, 48L * 1024 * 1024);
        }
        [Test] public void CreatureVentilationMetalAndElectricalSoundsAreDistinctAndRepeatable()
        {
            var ids = new[] { CueId.ZombieBreath, CueId.Ventilation, CueId.ZombieShuffle, CueId.FluorescentBuzz,
                CueId.AlarmKlaxon, CueId.ImpactHeavy, CueId.BeaconChirp };
            for (int a = 0; a < ids.Length; a++)
            {
                var first = Render(ids[a]); var repeat = Render(ids[a]);
                for (int i = 0; i < first.Length; i += 101) Assert.AreEqual(first[i], repeat[i]);
                for (int b = a + 1; b < ids.Length; b++)
                {
                    var other = Render(ids[b]); double difference = 0;
                    for (int i = 0; i < Math.Min(first.Length, other.Length); i += 101) difference += Math.Abs(first[i] - other[i]);
                    Assert.Greater(difference, .01, ids[a] + " / " + ids[b]);
                }
            }
        }
        [Test] public void SynthesizedAssetsPreserveGameplayNoiseAndMixerRouting()
        {
            Assert.IsTrue(CueTable.TryGet(CueId.ImpactHeavy, out var impact));
            Assert.AreEqual(6, impact.NoiseRadius); Assert.AreEqual(MixerBus.World, impact.Bus);
            Assert.IsTrue(CueTable.TryGet(CueId.PaPatientZero, out var speech));
            Assert.AreEqual(MixerBus.Voice, speech.Bus); Assert.AreEqual(0, speech.NoiseRadius);
            Assert.AreEqual(CaptionPolicy.Speech, speech.CaptionPolicy);
        }
    }
}
