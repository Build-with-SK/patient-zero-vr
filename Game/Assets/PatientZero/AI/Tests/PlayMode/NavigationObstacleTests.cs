using System.Collections;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace PatientZero.AI.Tests
{
    public sealed class NavigationObstacleTests
    {
        private static GameObject Box(Transform parent, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            return go;
        }
        [UnityTest] public IEnumerator NavigationFollowsACompleteRouteAroundAnOccludingWall()
        {
            var root = new GameObject("ObstacleNavigationFixture"); GameObject actor = null;
            try
            {
                Box(root.transform, Vector3.down * .1f, new Vector3(10, .2f, 10));
                Box(root.transform, Vector3.up, new Vector3(.2f, 2, 3));
                Physics.SyncTransforms();
                var surface = root.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
                actor = new GameObject("NavigationActor"); actor.transform.position = Vector3.left * 2;
                var agent = actor.AddComponent<NavMeshAgent>(); agent.radius = .22f; agent.stoppingDistance = .1f;
                var navigation = actor.AddComponent<ZombieNavigation>(); navigation.SetSimulationActive(true);
                Assert.IsTrue(navigation.Warp(Vector3.left * 2));
                var destination = Vector3.right * 2;
                float until = Time.time + 8;
                while (Time.time < until && Vector3.Distance(actor.transform.position, destination) > .3f)
                {
                    navigation.Drive(true, destination, 2, Time.time);
                    Assert.IsFalse(navigation.PathFailed, "Static wall should be routed around, not treated as an impossible path.");
                    yield return new WaitForFixedUpdate();
                }
                Assert.Less(Vector3.Distance(actor.transform.position, destination), .3f);
            }
            finally { if (actor) Object.DestroyImmediate(actor); Object.DestroyImmediate(root); }
        }
        [UnityTest] public IEnumerator DynamicDoorStopsMotionAndOpeningItAllowsRecovery()
        {
            var root = new GameObject("DynamicDoorFixture"); GameObject actor = null;
            try
            {
                Box(root.transform, Vector3.down * .1f, new Vector3(10, .2f, 10)); Physics.SyncTransforms();
                var surface = root.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
                // This collider is deliberately added after the static bake, like a moving gameplay door.
                var door = Box(root.transform, Vector3.up, new Vector3(.15f, 2, 6)); Physics.SyncTransforms();
                actor = new GameObject("DoorActor"); actor.transform.position = Vector3.left;
                actor.AddComponent<NavMeshAgent>().stoppingDistance = .1f;
                var navigation = actor.AddComponent<ZombieNavigation>(); navigation.SetSimulationActive(true);
                Assert.IsTrue(navigation.Warp(Vector3.left));
                float until = Time.time + 2; bool failedSafely = false;
                while (Time.time < until)
                {
                    navigation.Drive(true, Vector3.right, 1, Time.time); failedSafely |= navigation.PathFailed;
                    Assert.Less(actor.transform.position.x, 0, "Must not walk through the closed door.");
                    yield return new WaitForFixedUpdate();
                }
                Assert.IsTrue(failedSafely, "A permanently blocked door must report failure within a bounded wait.");
                door.SetActive(false); Physics.SyncTransforms(); until = Time.time + 5;
                while (Time.time < until && Vector3.Distance(actor.transform.position, Vector3.right) > .3f)
                { navigation.Drive(true, Vector3.right, 1, Time.time); yield return new WaitForFixedUpdate(); }
                Assert.Less(Vector3.Distance(actor.transform.position, Vector3.right), .3f);
                Assert.IsFalse(navigation.PathFailed);
            }
            finally { if (actor) Object.DestroyImmediate(actor); Object.DestroyImmediate(root); }
        }
    }
}
