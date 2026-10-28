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
        public IEnumerator GateAdapterPublishesOnceSupportsLateQueryAndClosesOnRestore()
        {
            CoreStaticReset.ResetStatics(); var audio = new AudioStub(); Services<IAudioCuePlayer>.Register(audio);
            var root = new GameObject("GateAdapter"); var director = root.AddComponent<ExperienceDirector>();
            int releases = 0; bool queriedAtSignal = false, closedAtStart = false;
            System.Action<GateReleased> released = e =>
            { releases++; queriedAtSignal = Services<IExperienceDirector>.Instance.IsGateReleased(e.Gate); };
            System.Action<ActStarted> started = e =>
            { if (e.Act == ActId.Awakening) closedAtStart = !director.IsGateReleased(GateId.Act1Door); };
            GameEvents.GateReleased.Subscribe(released); GameEvents.ActStarted.Subscribe(started);
            try
            {
                director.StartRun(ActId.Awakening, false); Assert.IsTrue(closedAtStart);
                // Drive the engine-free clock/actor input without sleeping for the full scene beat.
                // The public objective entry points and runtime Pump still publish the actual Core event.
                var field = typeof(ExperienceDirector).GetField("core", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var core = (PatientZero.Atmosphere.Logic.DirectorCore)field.GetValue(director);
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorUnbarred));
                core.ReportZombiePosition(1, WardLayout.A1_DoorWindow.Position);
                core.Tick(.1f, true, WardLayout.A1_Start.Position); core.Tick(2, true, WardLayout.A1_Start.Position);
                core.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position);
                // Any public event drains the director queue; the duplicate completion is harmless.
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorUnbarred));
                Assert.AreEqual(1, releases); Assert.IsTrue(queriedAtSignal);
                Assert.IsTrue(Services<IExperienceDirector>.Instance.IsGateReleased(GateId.Act1Door));
                Assert.IsFalse(director.IsGateReleased(GateId.None));
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorOpened));
                Assert.AreEqual(ActId.Machinery, director.CurrentAct); Assert.IsFalse(director.IsGateReleased(GateId.Act1Door));
                closedAtStart = false; director.StartRun(ActId.Awakening, true);
                Assert.IsTrue(closedAtStart); Assert.AreEqual(1, releases);
                yield return null;
            }
            finally
            {
                GameEvents.GateReleased.Unsubscribe(released); GameEvents.ActStarted.Unsubscribe(started);
                Object.Destroy(root); Services<IAudioCuePlayer>.Unregister(audio);
            }
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
