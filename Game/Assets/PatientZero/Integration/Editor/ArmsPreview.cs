// Claude-owned integration (E-27: the player's arms on a flat screen).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// A quick look at the first-person arms without composing the ward or building the game: an empty scene with the arms,
    /// the torch and the revolver (FirstPersonArmsStep, RevolverStep), photographed from the eyes and from outside in four poses.
    /// Run from the command line WITHOUT -nographics (it renders):
    ///   Unity -batchmode -projectPath Game -executeMethod PatientZero.Integration.Editor.ArmsPreview.RenderBatch -logFile ...
    /// Pictures go to Game/Logs/PatientZero/shots/arms-*.png. Nothing is saved to any scene.
    /// </summary>
    public static class ArmsPreview
    {
        private const int Width = 1280, Height = 720;

        public static void RenderBatch()
        {
            int code = 0;
            try { Render(); }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Arms preview").transform;
            Transform gun = RevolverStep.Gun(root, out _, out Renderer flash, out _, out BoxCollider pickup);
            Object.DestroyImmediate(pickup);
            flash.enabled = false;
            FirstPersonArms arms = FirstPersonArmsStep.Create(root, gun, out string report);
            Debug.Log("[PZ-PREVIEW] arms: " + report);
            // Every picture here is taken within one Editor frame: the skin must follow the bones at each render, not once a frame.
            ((SkinnedMeshRenderer)arms.renderers[0]).forceMatrixRecalculationPerRender = true;

            // Lights, fog and ambient belong to Atmosphere and Environment (tools/review/check_rules.py), so this scene has none:
            // everything here is drawn unlit, in its own colour and texture (copies in memory: the preview leaves no asset behind).
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var wall = new Material(unlit);
            wall.SetColor("_BaseColor", new Color(.35f, .36f, .38f, 1));
            RevolverStep.Part(root, "Backdrop", PrimitiveType.Quad, wall, new Vector3(0, 1.6f, 4), Vector3.zero, new Vector3(12, 8, 1));
            foreach (Renderer drawn in root.GetComponentsInChildren<Renderer>(true))
            {
                if (drawn.sharedMaterial == wall) continue;
                Material[] flat = drawn.sharedMaterials;
                for (int i = 0; i < flat.Length; i++)
                {
                    if (!flat[i]) continue;
                    var copy = new Material(unlit);
                    if (flat[i].HasProperty("_BaseMap")) copy.SetTexture("_BaseMap", flat[i].GetTexture("_BaseMap"));
                    if (flat[i].HasProperty("_BaseColor")) copy.SetColor("_BaseColor", flat[i].GetColor("_BaseColor"));
                    flat[i] = copy;
                }
                drawn.sharedMaterials = flat;
            }

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.fieldOfView = 60;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 50;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.08f, .09f, .1f);
            var target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;

            var eye = new Vector3(0, 1.6f, 0);
            Quaternion look = Quaternion.identity;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "PatientZero", "shots"));
            Directory.CreateDirectory(folder);

            (string name, float share, float aim)[] poses = { ("torch", 0, 0), ("hip", 1, 0), ("aim", 1, 1) };
            foreach (var (name, share, aim) in poses)
            {
                gun.SetPositionAndRotation(eye + look * Vector3.Lerp(Revolver.HipPose, Revolver.AimPose, aim), look);
                gun.gameObject.SetActive(share > 0);
                arms.Pose(eye, look, share, aim, 0);
                Describe(name, arms, eye, look);
                Shoot(camera, target, eye, look, folder, "arms-" + name + "-eyes");
                Shoot(camera, target, eye + new Vector3(.9f, -.1f, .25f), Quaternion.LookRotation(new Vector3(-1, -.1f, .05f)), folder, "arms-" + name + "-right");
                Shoot(camera, target, eye + new Vector3(-.9f, -.1f, .25f), Quaternion.LookRotation(new Vector3(1, -.1f, .05f)), folder, "arms-" + name + "-left");
                Shoot(camera, target, eye + new Vector3(0, -.05f, 1.1f), Quaternion.LookRotation(new Vector3(0, -.12f, -1)), folder, "arms-" + name + "-facing");
            }

            // Close-ups of the two hands in the aim pose.
            Shoot(camera, target, arms.torch.position + new Vector3(.12f, .05f, -.02f), Quaternion.LookRotation(arms.torch.position - (arms.torch.position + new Vector3(.12f, .05f, -.02f))), folder, "arms-hand-left");
            Vector3 grip = arms.gunGrip.position;
            Shoot(camera, target, grip + new Vector3(-.12f, .04f, .03f), Quaternion.LookRotation(grip - (grip + new Vector3(-.12f, .04f, .03f))), folder, "arms-hand-right");
            Shoot(camera, target, grip + new Vector3(.12f, .04f, .03f), Quaternion.LookRotation(grip - (grip + new Vector3(.12f, .04f, .03f))), folder, "arms-hand-right-out");
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
        }

        private static void Describe(string pose, FirstPersonArms arms, Vector3 eye, Quaternion look)
        {
            foreach (FirstPersonArms.Arm arm in new[] { arms.left, arms.right })
            {
                Quaternion back = Quaternion.Inverse(look);
                string P(Transform t) => Round(back * (t.position - eye));
                Vector3 wristWant = arm.hand.position + arm.hand.rotation * arm.wristInHand;
                Debug.Log("[PZ-PREVIEW] " + pose + " " + (arm.right ? "right" : "left") + ": shoulder " + P(arm.upper) + " elbow " + P(arm.fore) +
                          " wrist " + P(arm.wrist) + " hand " + P(arm.hand) + "; wrist off its target by " + Vector3.Distance(arm.wrist.position, wristWant).ToString("0.000") + " m");
            }
        }

        private static void Shoot(Camera camera, RenderTexture target, Vector3 at, Quaternion facing, string folder, string name)
        {
            camera.transform.SetPositionAndRotation(at, facing);
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
            else camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var picture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), picture.EncodeToPNG());
            Object.DestroyImmediate(picture);
        }

        private static string Round(Vector3 p) => "(" + p.x.ToString("0.00") + ", " + p.y.ToString("0.00") + ", " + p.z.ToString("0.00") + ")";
    }
}
