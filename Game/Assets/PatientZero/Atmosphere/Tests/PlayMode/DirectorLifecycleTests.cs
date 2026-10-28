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
        [UnityTest]
        public IEnumerator GateIsQueryableDuringSignalAndByLateDoorThenResetsBeforeActStarted()
        {
            CoreStaticReset.ResetStatics(); var audio = new AudioStub(); Services<IAudioCuePlayer>.Register(audio);
            var root = new GameObject("GateLifecycle");
            int signals = 0; bool queriedDuringSignal = false, closedDuringRestart = false;
            var director = root.AddComponent<ExperienceDirector>();
            System.Action<GateReleased> onGate = e => { signals++; queriedDuringSignal = director.IsGateReleased(e.Gate); };
            System.Action<ActStarted> onAct = e => { if (e.Act == ActId.Awakening) closedDuringRestart = !director.IsGateReleased(GateId.Act1Door); };
            GameEvents.GateReleased.Subscribe(onGate); GameEvents.ActStarted.Subscribe(onAct);
            try
            {
                director.StartRun(ActId.Awakening, false);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                // Supply deterministic positions/time instead of requiring a NavMesh for this service test.
                var core = (Logic.DirectorCore)typeof(ExperienceDirector).GetField("core", flags).GetValue(director);
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorUnbarred));
                core.ReportZombiePosition(ZombieIds.Walker, WardLayout.A1_DoorWindow.Position);
                core.Tick(.1f, true, Vector3.zero); core.Tick(2, true, Vector3.zero);
                core.ReportZombiePosition(ZombieIds.Walker, WardLayout.Z1_WalkTo.Position);
                core.Tick(.1f, true, Vector3.zero);
                typeof(ExperienceDirector).GetMethod("Pump", flags).Invoke(director, null);
                Assert.AreEqual(1, signals); Assert.IsTrue(queriedDuringSignal);
                GameEvents.GateReleased.Unsubscribe(onGate); // A door loaded now has missed the original event.
                Assert.IsTrue(Services<IExperienceDirector>.Instance.IsGateReleased(GateId.Act1Door));
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorOpened));
                Assert.AreEqual(ActId.Machinery, director.CurrentAct);
                Assert.IsFalse(director.IsGateReleased(GateId.Act1Door));
                director.StartRun(ActId.Awakening, true);
                Assert.IsTrue(closedDuringRestart); Assert.IsFalse(director.IsGateReleased(GateId.Act1Door));
            }
            finally
            {
                GameEvents.GateReleased.Unsubscribe(onGate); GameEvents.ActStarted.Unsubscribe(onAct);
                Object.DestroyImmediate(root); Services<IAudioCuePlayer>.Unregister(audio);
            }
            yield return null;
        }
    }
}
