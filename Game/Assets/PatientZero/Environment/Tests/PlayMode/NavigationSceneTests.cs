// UNVERIFIED IN EDITOR
using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PatientZero.Environment.Tests
{
    /// <summary>Requires Claude's generated Ward in build settings. Missing scenes fail rather than becoming skipped passes.</summary>
    public sealed class NavigationSceneTests
    {
        [UnityTest]
        public IEnumerator ApprovedZombieRoutesHaveCompletePathsInGeneratedWard()
        {
            yield return SceneManager.LoadSceneAsync("Ward", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName("Ward"); Assert.IsTrue(scene.IsValid());
            AsyncOperation unload = null;
            try
            {
                var from = new[] { WardLayout.Z1_WalkFrom.Position, WardLayout.Z1_WalkFrom.Position,
                    WardLayout.Z2_Dormant.Position, WardLayout.Z2_Doorway.Position, WardLayout.Z3_PatrolNorth.Position, WardLayout.Z4_Gurney.Position };
                var to = new[] { WardLayout.A1_DoorWindow.Position + Vector3.forward * .45f, WardLayout.Z1_WalkTo.Position,
                    WardLayout.Z2_Doorway.Position, WardLayout.Z1_WalkFrom.Position, WardLayout.Z3_PatrolSouth.Position, WardLayout.Z4_Gurney.Position };
                var path = new NavMeshPath();
                for (int i = 0; i < from.Length; i++)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(from[i], out var start, 2, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.SamplePosition(to[i], out var end, 2, NavMesh.AllAreas));
                    Assert.IsTrue(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "route " + i);
                }
            }
            finally { if (scene.isLoaded) unload = SceneManager.UnloadSceneAsync(scene); }
            if (unload != null) yield return unload;
        }
    }
}
