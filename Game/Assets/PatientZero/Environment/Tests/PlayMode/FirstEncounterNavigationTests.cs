using System.Collections;
using NUnit.Framework;
using PatientZero.AI;
using PatientZero.Atmosphere;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PatientZero.Environment.Tests
{
    public sealed class FirstEncounterNavigationTests
    {
        private sealed class Player : IPlayerQuery
        {
            public Vector3 FloorPosition => WardLayout.A1_Door.Position; // E-22: standing still at the opened door
            public Vector3 HeadPosition => FloorPosition + Vector3.up * 1.6f;
            public Vector3 HeadForward => Vector3.forward;
            public InputModality Modality => InputModality.Controllers;
            public float HeadSpeed => 0;
            public float FastestHandSpeed => 0;
            public bool TryGetHandPosition(HandSide side, out Vector3 p) { p = HeadPosition; return true; }
        }
        private sealed class SilentAudio : IAudioCuePlayer
        {
            public CueHandle Play(CueId cue, Vector3 p, float gain, NoiseOrigin origin) => default;
            public CueHandle PlayAttached(CueId cue, Transform p, float gain, NoiseOrigin origin) => default;
            public void SetIntensity(CueHandle handle, float gain) { }
            public void Stop(CueHandle handle, float seconds) { }
        }
        /// <summary>
        /// E-22 (two-minute version): the door opens onto the Walker. A player who stands still is never grabbed; the Walker loses
        /// them, walks off down the corridor on the saved navigation (not the director's fallback timer), and the act ends.
        /// </summary>
        [UnityTest] public IEnumerator DoorZombieLosesAStillPlayerAndWalksOffOnSavedFloorNavigation()
        {
#if UNITY_EDITOR
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/PatientZero/Environment/Generated/WardNavMesh.asset");
            Assert.IsNotNull(data, "Generate the saved Ward navigation before this integration test.");
            CoreStaticReset.ResetStatics();
            var navData = NavMesh.AddNavMeshData(data); var player = new Player(); var audio = new SilentAudio();
            Services<IPlayerQuery>.Register(player); Services<IAudioCuePlayer>.Register(audio);
            var actor = new GameObject("FirstEncounterWalker"); actor.SetActive(false); actor.transform.position = WardLayout.Z1_WalkFrom.Position;
            actor.AddComponent<NavMeshAgent>().stoppingDistance = .05f;
            actor.AddComponent<ZombieNavigation>(); var shell = actor.AddComponent<ZombieAgentShell>(); shell.ZombieId = ZombieIds.Walker;
            var root = new GameObject("FirstEncounterDirector"); float oldScale = Time.timeScale; Time.timeScale = 1;
            int grabs = 0; System.Action<PlayerCaught> grabbed = e => grabs++;
            GameEvents.PlayerCaught.Subscribe(grabbed);
            try
            {
                actor.SetActive(true); var director = root.AddComponent<ExperienceDirector>(); director.Zombies = new[] { shell };
                director.StartRun(ActId.Awakening, false);
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorUnbarred));
                Assert.IsTrue(director.IsGateReleased(GateId.Act1Door), "the bar alone opens the door");
                Assert.IsTrue(shell.Shown, "the door opens onto the Walker");
                Assert.Greater(actor.transform.position.z, WardLayout.A1_DoorWindow.Position.z + .5f, "outside the room, in the corridor");
                Assert.Less(WardLayout.HorizontalDistance(actor.transform.position, shell.Brain.Target), .3f, "it sniffs about the spot it appeared on");
                GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(ObjectiveId.Act1_DoorOpened));
                float deadline = Time.realtimeSinceStartup + 30;
                while (director.CurrentAct == ActId.Awakening && Time.realtimeSinceStartup < deadline) yield return null;
                string where = "Walker=" + actor.transform.position + "; target=" + shell.Brain.Target + "; level=" + shell.Brain.Level +
                    "; path failed=" + actor.GetComponent<ZombieNavigation>().PathFailed + "; clock=" + director.ExperienceTime;
                Assert.AreEqual(ActId.Machinery, director.CurrentAct, where);
                Assert.AreEqual(0, grabs, "a player who stands still is never grabbed; " + where);
                Assert.Less(WardLayout.HorizontalDistance(actor.transform.position, WardLayout.Z1_WalkTo.Position), .6f, "walked off, not timed out; " + where);
            }
            finally
            {
                GameEvents.PlayerCaught.Unsubscribe(grabbed);
                Object.DestroyImmediate(root); Object.DestroyImmediate(actor); navData.Remove();
                Services<IPlayerQuery>.Unregister(player); Services<IAudioCuePlayer>.Unregister(audio); Time.timeScale = oldScale;
            }
#else
            Assert.Fail("This saved-asset integration test runs in the Unity Editor.");
            yield return null;
#endif
        }
    }
}
