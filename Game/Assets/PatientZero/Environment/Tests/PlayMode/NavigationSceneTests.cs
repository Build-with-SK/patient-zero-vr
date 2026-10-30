// UNVERIFIED IN EDITOR
using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using Unity.AI.Navigation;
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
            // Loading twice checks persistence across unload, not only a builder's in-memory bake.
            for (int load = 0; load < 2; load++)
            {
                yield return SceneManager.LoadSceneAsync("Ward", LoadSceneMode.Additive);
                var scene = SceneManager.GetSceneByName("Ward"); Assert.IsTrue(scene.IsValid());
                AsyncOperation unload = null;
                try
                {
                    int surfaces = 0;
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var surface in root.GetComponentsInChildren<NavMeshSurface>(true))
                        {
                            surfaces++; Assert.IsNotNull(surface.navMeshData);
#if UNITY_EDITOR
                            Assert.AreEqual("Assets/PatientZero/Environment/Generated/WardNavMesh.asset",
                                UnityEditor.AssetDatabase.GetAssetPath(surface.navMeshData));
#endif
                        }
                    Assert.AreEqual(1, surfaces);
                    var from = new[] { WardLayout.Z1_WalkFrom.Position, WardLayout.Z1_WalkFrom.Position,
                        WardLayout.Z2_Dormant.Position, WardLayout.Z2_Doorway.Position, WardLayout.Z3_PatrolNorth.Position, WardLayout.Z4_Gurney.Position };
                    var to = new[] { WardLayout.A1_DoorWindow.Position + Vector3.forward * .45f, WardLayout.Z1_WalkTo.Position,
                        WardLayout.Z2_Doorway.Position, WardLayout.Z1_WalkFrom.Position, WardLayout.Z3_PatrolSouth.Position, WardLayout.Z4_Gurney.Position };
                    to[0].y = from[0].y; // Window marker is eye height; Z1 walks on the corridor floor.
                    var path = new NavMeshPath();
                    for (int i = 0; i < from.Length; i++)
                    {
                        Assert.IsTrue(NavMesh.SamplePosition(from[i], out var start, 2, NavMesh.AllAreas));
                        Assert.IsTrue(NavMesh.SamplePosition(to[i], out var end, 2, NavMesh.AllAreas));
                        if (i == 0) Assert.LessOrEqual(WardLayout.HorizontalDistance(end.position, WardLayout.A1_DoorWindow.Position), .5f,
                            "Z1 must reach the window-arrival threshold; TD must set the bake agent radius to 0.25 m (INT-B-1)");
                        Assert.IsTrue(NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path));
                        Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, "route " + i);
                    }
                }
                finally { if (scene.isLoaded) unload = SceneManager.UnloadSceneAsync(scene); }
                if (unload != null) yield return unload;
            }
        }
    }
}
