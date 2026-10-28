// UNVERIFIED IN EDITOR
using System;
using System.IO;
using PatientZero.Atmosphere;
using PatientZero.Core;
using PatientZero.Environment.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace PatientZero.AI.Editor
{
    /// <summary>Composer creates four low-poly seven-bone mannequin prefabs; only the director's current actor is visible.</summary>
    public static class ZombieBuildStep
    {
        [SceneBuildStep(BuildScene.Ward, 500)]
        public static void Build(Transform root)
        {
            var director = WardPrimitives.FindInScene<ExperienceDirector>(root);
            if (!director) throw new InvalidOperationException("Atmosphere step 200 must precede AI step 500");
            var material = WardPrimitives.Material("GeneratedPatient", new Color(.24f, .28f, .22f));
            var actors = new ZombieAgentShell[4];
            for (int i = 0; i < actors.Length; i++)
            {
                int id = i + 1;
                var marker = id == 1 ? WardLayout.Z1_WalkFrom : id == 2 ? WardLayout.Z2_Dormant : id == 3 ? WardLayout.Z3_PatrolNorth : WardLayout.Z4_Gurney;
                var actor = new GameObject("Zombie_" + id); actor.transform.SetParent(root, false); actor.transform.position = marker.Position;
                actor.layer = 2; // Ignore Raycast: the environment mask must not count the zombie itself.
                var agent = actor.AddComponent<NavMeshAgent>(); agent.radius = .22f; agent.height = 1.7f;
                agent.stoppingDistance = id == 1 ? .05f : .25f; agent.speed = .9f; agent.acceleration = 4;
                agent.angularSpeed = 120; agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                actor.AddComponent<ZombieNavigation>();
                var animation = actor.AddComponent<ZombieAnimationController>(); Mannequin(actor.transform, animation, material);
                var shell = actor.AddComponent<ZombieAgentShell>(); shell.ZombieId = id; shell.Animation = animation;
                shell.Head = animation.Head; shell.FootEmitter = animation.FootEmitter; actors[i] = shell;
                string folder = "Assets/PatientZero/AI/Generated";
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "PatientZero/AI/Generated"));
                PrefabUtility.SaveAsPrefabAsset(actor, folder + "/Zombie_" + id + ".prefab");
            }
            director.Zombies = actors;
        }
        private static Transform Bone(Transform parent, string name, Vector3 local)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = local; return go.transform; }
        private static void Mannequin(Transform actor, ZombieAnimationController animation, Material material)
        {
            var visual = Bone(actor, "GeneratedMannequin", Vector3.zero);
            var body = Bone(visual, "Torso", Vector3.up * 1.1f);
            var head = Bone(body, "Head", Vector3.up * .55f);
            var left = Bone(body, "LeftLeg", new Vector3(-.14f, -.65f, 0));
            var right = Bone(body, "RightLeg", new Vector3(.14f, -.65f, 0));
            var leftArm = Bone(body, "LeftArm", Vector3.left * .34f);
            var rightArm = Bone(body, "RightArm", Vector3.right * .34f);
            var hips = Bone(body, "Hips", Vector3.down * .35f);
            Transform[] bones = { body, head, left, right, leftArm, rightArm, hips };
            Vector3[] sizes = { new Vector3(.45f, .6f, .25f), Vector3.one * .3f,
                new Vector3(.17f, .85f, .2f), new Vector3(.17f, .85f, .2f),
                new Vector3(.14f, .8f, .14f), new Vector3(.14f, .8f, .14f), new Vector3(.4f, .2f, .25f) };
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); var source = cube.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] cubeVertices = source.vertices; int[] cubeTriangles = source.triangles;
            var vertices = new Vector3[cubeVertices.Length * bones.Length];
            var triangles = new int[cubeTriangles.Length * bones.Length];
            var weights = new BoneWeight[vertices.Length]; var bindposes = new Matrix4x4[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                bindposes[b] = bones[b].worldToLocalMatrix * visual.localToWorldMatrix;
                for (int v = 0; v < cubeVertices.Length; v++)
                {
                    int index = b * cubeVertices.Length + v;
                    vertices[index] = visual.InverseTransformPoint(bones[b].TransformPoint(Vector3.Scale(cubeVertices[v], sizes[b])));
                    weights[index] = new BoneWeight { boneIndex0 = b, weight0 = 1 };
                }
                for (int t = 0; t < cubeTriangles.Length; t++) triangles[b * cubeTriangles.Length + t] = cubeTriangles[t] + b * cubeVertices.Length;
            }
            UnityEngine.Object.DestroyImmediate(cube);
            const string path = "Assets/PatientZero/AI/Generated/Mannequin.asset";
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "PatientZero/AI/Generated"));
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = new Mesh { name = "GeneratedSevenBonePatient" }; mesh.vertices = vertices; mesh.triangles = triangles;
                mesh.boneWeights = weights; mesh.bindposes = bindposes; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, path);
            }
            var skin = visual.gameObject.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh = mesh; skin.sharedMaterial = material;
            skin.bones = bones; skin.rootBone = body; skin.enabled = false;
            skin.localBounds = new Bounds(Vector3.up, new Vector3(2, 3, 2));
            animation.Body = body; animation.Head = head; animation.LeftLeg = left; animation.RightLeg = right;
            animation.LeftArm = leftArm; animation.RightArm = rightArm; animation.Visual = skin;
            animation.FootEmitter = Bone(actor, "AlternatingFootEmitter", Vector3.up * .1f);
        }
    }
}
