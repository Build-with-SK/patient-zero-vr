// UNVERIFIED IN EDITOR
using System.Collections;
using NUnit.Framework;
using PatientZero.Audio;
using PatientZero.Core;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PatientZero.AI.Tests
{
    public sealed class HearingSceneTests
    {
        private sealed class QuietPlayer : IPlayerQuery
        {
            public Vector3 FloorPosition => WardLayout.Z2_Dormant.Position + Vector3.right * 10;
            public Vector3 HeadPosition => FloorPosition + Vector3.up * 1.6f;
            public Vector3 HeadForward => Vector3.right;
            public InputModality Modality => InputModality.Controllers;
            public float HeadSpeed => 0;
            public float FastestHandSpeed => 0;
            public bool TryGetHandPosition(HandSide hand, out Vector3 position) { position = default; return false; }
        }
        [UnityTest]
        public IEnumerator AudibleImpactInsideRadiusInvestigatesAndOutsideDoesNot()
        {
            CoreStaticReset.ResetStatics();
            var floorRoot = new GameObject("HearingFloor"); var audioRoot = new GameObject("Audio");
            GameObject actor = null;
            var query = new QuietPlayer(); Services<IPlayerQuery>.Register(query);
            try
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(floorRoot.transform);
                floor.transform.position = WardLayout.Z2_Dormant.Position - Vector3.up * .1f; floor.transform.localScale = new Vector3(20, .2f, 20);
                Physics.SyncTransforms(); var surface = floorRoot.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
                surface.BuildNavMesh(); yield return null;
                Assert.IsTrue(NavMesh.SamplePosition(WardLayout.Z2_Dormant.Position, out _, 1, NavMesh.AllAreas));
                audioRoot.AddComponent<AudioCuePlayer>();
                actor = new GameObject("Orderly"); actor.SetActive(false); actor.transform.position = WardLayout.Z2_Dormant.Position;
                actor.AddComponent<NavMeshAgent>(); actor.AddComponent<ZombieNavigation>().Obstacles = 0;
                var zombie = actor.AddComponent<ZombieAgentShell>(); zombie.ZombieId = ZombieIds.Orderly; zombie.Occluders = 0;
                actor.SetActive(true);
                GameEvents.StateChanged.Raise(new GameStateChanged(GameState.Boot, GameState.Playing, FailReason.None));
                GameEvents.ActStarted.Raise(new ActStarted(ActId.Machinery, false, 1));
                Cues.Play(CueId.ImpactHeavy, WardLayout.Z2_Dormant.Position + Vector3.right * 7, NoiseOrigin.Player);
                Assert.AreEqual(AlertLevel.Unaware, zombie.Brain.Level);
                Cues.Play(CueId.ImpactHeavy, WardLayout.Z2_Dormant.Position + Vector3.right * 4, NoiseOrigin.Player);
                Assert.AreEqual(AlertLevel.Suspicious, zombie.Brain.Level);
                yield return new WaitForSeconds(.75f);
                Assert.AreEqual(AlertLevel.Investigating, zombie.Brain.Level);
            }
            finally
            {
                if (actor) Object.Destroy(actor); Object.Destroy(audioRoot); Object.Destroy(floorRoot);
                Services<IPlayerQuery>.Unregister(query);
            }
        }
    }
}
