// UNVERIFIED IN EDITOR
using System.Collections;
using NUnit.Framework;
using PatientZero.Audio.Logic;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PatientZero.Audio.Tests
{
    public sealed class AudioSceneTests
    {
        [UnityTest]
        public IEnumerator EveryWorldCueHasMonoClipZeroDopplerAndSilenceAtMaximum()
        {
            CoreStaticReset.ResetStatics(); var root = new GameObject("CueTest");
            try
            {
                var player = root.AddComponent<AudioCuePlayer>();
                for (int i = 0; i < CueTable.Count; i++)
                {
                    var row = CueTable.At(i); if (!row.World) continue;
                    var handle = player.Play(row.Id, WardLayout.A2_Start.Position, 1, NoiseOrigin.Ambient);
                    Assert.IsTrue(handle.IsValid, row.Id.ToString());
                    AudioSource active = null;
                    foreach (var source in root.GetComponentsInChildren<AudioSource>()) if (source.clip && source.clip.name == "Placeholder_" + row.Id) active = source;
                    Assert.IsNotNull(active); Assert.AreEqual(1, active.clip.channels); Assert.AreEqual(48000, active.clip.frequency);
                    Assert.AreEqual(0, active.dopplerLevel); Assert.AreEqual(1, active.spatialBlend); Assert.IsFalse(active.spatialize);
                    // AudioSource.volume is source gain, not post-distance volume. Sample the actual assigned curve.
                    Assert.AreEqual(0, active.GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(1), .00001);
                    player.Stop(handle, 0);
                }
                yield return null;
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest]
        public IEnumerator MissingAudioAssetEmitsNoNoiseAndReturnsInvalidHandle()
        {
            CoreStaticReset.ResetStatics(); var root = new GameObject("MissingClip"); root.SetActive(false);
            int heard = 0; System.Action<NoiseEvent> listener = _ => heard++;
            try
            {
                var player = root.AddComponent<AudioCuePlayer>(); player.BuildPlaceholders = false; root.SetActive(true);
                GameEvents.Noise.Subscribe(listener);
                Assert.IsFalse(player.Play(CueId.ImpactHeavy, WardLayout.A2_Start.Position, 1, NoiseOrigin.Player).IsValid);
                Assert.AreEqual(1, player.MissingAssets); Assert.AreEqual(0, heard); yield return null;
            }
            finally { GameEvents.Noise.Unsubscribe(listener); Object.Destroy(root); }
        }
        [UnityTest]
        public IEnumerator ZeroIntensityIsSilentAndEmitsNoGameplayNoise()
        {
            CoreStaticReset.ResetStatics(); var root = new GameObject("SilentCue"); int heard = 0;
            System.Action<NoiseEvent> listener = _ => heard++;
            try
            {
                var player = root.AddComponent<AudioCuePlayer>(); GameEvents.Noise.Subscribe(listener);
                Assert.IsFalse(player.Play(CueId.ImpactHeavy, WardLayout.A2_Start.Position, 0, NoiseOrigin.Player).IsValid);
                Assert.AreEqual(0, heard); Assert.AreEqual(0, player.ActiveVoices); yield return null;
            }
            finally { GameEvents.Noise.Unsubscribe(listener); Object.Destroy(root); }
        }
    }
}
