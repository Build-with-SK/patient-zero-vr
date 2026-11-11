// Claude-owned integration (owner's playtest, 2026-11-11; E-24).
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PatientZero.Integration.PlayModeTests
{
    /// <summary>The zombie model eases back from walls its arms and head would go through, and only from walls.</summary>
    public sealed class WallClearanceTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root) Object.Destroy(root);
        }

        private static GameObject Box(Transform parent, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            return go;
        }

        /// <summary>A zombie stand-in at the origin facing +z: the agent, and under it the model that WallClearance moves.</summary>
        private WallClearance Zombie(Quaternion facing)
        {
            var actor = new GameObject("Actor");
            actor.transform.SetParent(root.transform);
            actor.transform.rotation = facing;
            actor.layer = 2; // Ignore Raycast, as ZombieBuildStep sets it
            var model = new GameObject("Model");
            model.transform.SetParent(actor.transform, false);
            model.transform.localPosition = new Vector3(0, -.1f, 0);
            var clearance = model.AddComponent<WallClearance>();
            clearance.actor = actor.transform;
            clearance.every = 1;
            return clearance;
        }

        /// <summary>Long enough for the model to ease all the way (it moves at 2.2 m/s).</summary>
        private static IEnumerator Settle()
        {
            Physics.SyncTransforms();
            float until = Time.time + .6f;
            while (Time.time < until) yield return null;
        }

        [UnityTest]
        public IEnumerator AWallAheadPushesTheModelBackByWhatTheArmsWouldGoThrough()
        {
            root = new GameObject("WallClearanceFixture");
            Box(root.transform, new Vector3(0, 1.5f, .6f), new Vector3(4, 3, .2f)); // wall face at z = 0.5
            WallClearance clearance = Zombie(Quaternion.identity);
            yield return Settle();
            // The arms reach 0.6 m: 0.1 m of them were in the wall.
            Assert.AreEqual(-.1f, clearance.Offset.z, .015f);
            Assert.AreEqual(0, clearance.Offset.x, .015f);
            Assert.AreEqual(-.1f, clearance.transform.localPosition.z, .015f, "the model, not the agent, moves");
            Assert.AreEqual(-.1f, clearance.transform.localPosition.y, 1e-4f, "only sideways");
            Assert.AreEqual(Vector3.zero, clearance.actor.position);
        }

        [UnityTest]
        public IEnumerator AWallBesideAndAWallAheadPushBothWaysAndItComesBackWhenTheyGo()
        {
            root = new GameObject("WallClearanceFixture");
            GameObject ahead = Box(root.transform, new Vector3(0, 1.5f, .6f), new Vector3(4, 3, .2f));
            GameObject beside = Box(root.transform, new Vector3(.4f, 1.5f, 0), new Vector3(.2f, 3, 4)); // face at x = 0.3
            WallClearance clearance = Zombie(Quaternion.identity);
            yield return Settle();
            Assert.Less(clearance.Offset.x, -.04f, "pushed off the wall at its side");
            Assert.Less(clearance.Offset.z, -.05f, "pushed off the wall ahead");
            Object.Destroy(ahead);
            Object.Destroy(beside);
            yield return Settle();
            Assert.Less(clearance.Offset.magnitude, .01f, "back on its agent once the walls are gone");
        }

        [UnityTest]
        public IEnumerator APlayerInFrontNeverPushesIt()
        {
            root = new GameObject("WallClearanceFixture");
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0, 1, .7f);
            var capsule = player.AddComponent<CharacterController>();
            capsule.radius = .3f; // its front at z = 0.4, inside the arms' reach
            capsule.height = 2;
            WallClearance clearance = Zombie(Quaternion.identity);
            yield return Settle();
            Assert.Less(clearance.Offset.magnitude, .01f);
        }

        [UnityTest]
        public IEnumerator ALoosePropInFrontNeverPushesIt()
        {
            root = new GameObject("WallClearanceFixture");
            GameObject prop = Box(root.transform, new Vector3(0, 1.2f, .45f), new Vector3(.6f, .8f, .1f)); // across both rays
            prop.AddComponent<Rigidbody>().isKinematic = true;
            WallClearance clearance = Zombie(Quaternion.identity);
            yield return Settle();
            Assert.Less(clearance.Offset.magnitude, .01f);
        }

        [UnityTest]
        public IEnumerator ItTurnsWithTheZombie()
        {
            root = new GameObject("WallClearanceFixture");
            Box(root.transform, new Vector3(.6f, 1.5f, 0), new Vector3(.2f, 3, 4)); // face at x = 0.5, ahead of a zombie facing +x
            WallClearance clearance = Zombie(Quaternion.Euler(0, 90, 0));
            yield return Settle();
            Assert.AreEqual(-.1f, clearance.Offset.x, .015f);
        }
    }
}
