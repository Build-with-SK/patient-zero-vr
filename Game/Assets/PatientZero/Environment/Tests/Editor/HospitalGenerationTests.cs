using System.Linq;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Environment.Editor;
using PatientZero.Environment.Logic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatientZero.Environment.Tests
{
    public sealed class HospitalGenerationTests
    {
        private Scene scene;
        private GameObject root;
        [SetUp] public void Generate()
        {
            scene = EditorSceneManager.NewPreviewScene();
            root = new GameObject("HospitalGenerationTest"); SceneManager.MoveGameObjectToScene(root, scene);
            HospitalBuildSteps.Build(root.transform);
            Physics.SyncTransforms();
        }
        [TearDown] public void Close() { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
        [Test] public void ProceduralHospitalHasEveryRoomAndNoDuplicatePhysicsCart()
        {
            var colliders = root.GetComponentsInChildren<BoxCollider>();
            foreach (var room in WardLayout.Rooms)
                Assert.IsTrue(colliders.Any(c => c.name == room.Id + "_Floor"), room.Id);
            var cart = root.GetComponentsInChildren<LayoutTag>().Single(t => t.LayoutId == WardLayout.A3_Cart.Id);
            Assert.AreEqual(0, cart.GetComponentsInChildren<Collider>().Length);
            foreach (string name in new[] { "PatientBed", "OperatingTable", "SterileCabinet", "ExitSign" })
                Assert.AreEqual(1, root.GetComponentsInChildren<Renderer>().Count(r => r.name == name), name);
        }
        [Test] public void BuiltWallCollidersMatchTheExactDoorCuts()
        {
            var spans = new RoomGeometry().Walls;
            var walls = root.GetComponentsInChildren<BoxCollider>().Where(c => c.name == "Wall").ToArray();
            Assert.AreEqual(spans.Length, walls.Length);
            for (int i = 0; i < spans.Length; i++)
            {
                Assert.Less(Vector3.Distance(spans[i].ColliderSize, walls[i].bounds.size), .001f);
                Assert.Less(Vector3.Distance((spans[i].A + spans[i].B) * .5f + Vector3.up * 1.4f, walls[i].bounds.center), .001f);
            }
        }
    }
}
