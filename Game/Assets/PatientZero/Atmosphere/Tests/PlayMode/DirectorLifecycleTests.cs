// UNVERIFIED IN EDITOR
using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace PatientZero.Atmosphere.Tests
{
    public sealed class DirectorLifecycleTests
    {
        private sealed class AudioStub : IAudioCuePlayer
        {
            public CueHandle Play(CueId cue, Vector3 p, float i, NoiseOrigin o) => default;
            public CueHandle PlayAttached(CueId cue, Transform p, float i, NoiseOrigin o) => default;
            public void SetIntensity(CueHandle handle, float i) { }
            public void Stop(CueHandle handle, float fade) { }
        }
        [UnityTest]
        public IEnumerator TenDirectorLifetimesLeaveServicesAndSubscribersAtBaseline()
        {
            CoreStaticReset.ResetStatics(); var audio = new AudioStub(); Services<IAudioCuePlayer>.Register(audio);
            int noise = GameEvents.Noise.SubscriberCount, objectives = GameEvents.ObjectiveCompleted.SubscriberCount;
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    var root = new GameObject("DirectorLifecycle"); var director = root.AddComponent<ExperienceDirector>();
                    Assert.AreSame(director, Services<IExperienceDirector>.Instance); director.StartRun(ActId.Machinery, true);
                    Assert.AreEqual(ActId.Machinery, director.CurrentAct);
                    Object.Destroy(root); yield return null;
                    Assert.IsFalse(Services<IExperienceDirector>.TryGet(out _));
                    Assert.AreEqual(noise, GameEvents.Noise.SubscriberCount);
                    Assert.AreEqual(objectives, GameEvents.ObjectiveCompleted.SubscriberCount);
                }
            }
            finally { Services<IAudioCuePlayer>.Unregister(audio); }
        }
    }
}
