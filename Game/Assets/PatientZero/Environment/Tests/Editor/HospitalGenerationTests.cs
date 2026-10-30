using System.Linq;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Environment.Editor;
using PatientZero.Environment.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
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
        [Test] public void OwnedBuildStepsAreRegisteredOnceWithPublicComposerSignatures()
        {
            var expected = new System.Collections.Generic.HashSet<string> { "Ward:100", "Ward:150", "Ward:200", "Ward:500", "Bootstrap:600", "Ward:650" };
            int count = 0;
            foreach (var method in TypeCache.GetMethodsWithAttribute<SceneBuildStepAttribute>())
            {
                string space = method.DeclaringType.Namespace ?? "";
                if (!(space == "PatientZero.Environment.Editor" || space == "PatientZero.Atmosphere.Editor" || space == "PatientZero.AI.Editor" || space == "PatientZero.Audio.Editor")) continue;
                var step = method.GetCustomAttribute<SceneBuildStepAttribute>();
                Assert.IsTrue(expected.Remove(step.Scene + ":" + step.Order), method.Name);
                Assert.IsTrue(method.IsPublic && method.IsStatic); Assert.AreEqual(typeof(void), method.ReturnType);
                var parameters = method.GetParameters(); Assert.AreEqual(1, parameters.Length); Assert.AreEqual(typeof(Transform), parameters[0].ParameterType);
                count++;
            }
            Assert.AreEqual(6, count); Assert.AreEqual(0, expected.Count);
        }
        [Test] public void NewDetailsAreBuiltAndObjectiveSignsHaveReadableText()
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Assert.AreEqual(4, renderers.Count(r => r.name.StartsWith("PrivacyCurtain_")));
            Assert.AreEqual(1, renderers.Count(r => r.name == "OxygenCylinder"));
            var text = root.GetComponentsInChildren<TextMesh>();
            Assert.IsTrue(text.Any(t => t.text == "REMOVE THE 3 PLANKS"));
            Assert.IsTrue(text.Any(t => t.text == "EMERGENCY RELEASE"));
            Assert.IsFalse(text.Any(t => t.text.Contains("â") || t.text.Contains("�")));
            // No extra lights, hidden colliding trolley or global effect was added by environment dressing.
            Assert.AreEqual(0, root.GetComponentsInChildren<Light>().Length);
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
