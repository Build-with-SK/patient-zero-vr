// Claude-owned integration (E-27, E-28: the player's arms on a flat screen).
using System;
using System.Collections.Generic;
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
    /// the torch and the revolver (FirstPersonArmsStep, RevolverStep), photographed from the eyes and up close, once for each
    /// way of holding them in <see cref="Variants"/> (the first is the game's own). Run from the command line WITHOUT
    /// -nographics (it renders):
    ///   Unity -batchmode -projectPath Game -executeMethod PatientZero.Integration.Editor.ArmsPreview.RenderBatch -logFile ...
    /// Pictures go to Game/Logs/PatientZero/shots/arms-{variant}-{view}.png. Nothing is saved to any scene. The scene has no
    /// lights (they belong to Atmosphere and Environment): everything is drawn with ArmsPreviewShade, which shades by a fixed
    /// direction of its own.
    /// </summary>
    public static class ArmsPreview
    {
        private const int Width = 960, Height = 540;

        public static void RenderBatch()
        {
            int code = 0;
            try { Render(); }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        /// <summary>Ways of holding the torch and the gun to compare; the first is the game's own (FirstPersonArms.Hold's defaults).</summary>
        private static IEnumerable<(string name, FirstPersonArms.Hold hold)> Variants()
        {
            yield return ("game", new FirstPersonArms.Hold());
            // To compare another hold, add a line such as:
            //   yield return ("index40", new FirstPersonArms.Hold { gunIndex = new Vector3(40, 60, 30) });
        }

        private static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Arms preview").transform;
            Transform gun = RevolverStep.Gun(root, out _, out Renderer flash, out _, out BoxCollider pickup);
            Object.DestroyImmediate(pickup);
            flash.enabled = false;
            Shader shade = Shader.Find("Hidden/PatientZero/ArmsPreviewShade");
            if (!shade) throw new InvalidOperationException("ArmsPreviewShade.shader did not import.");
            var wall = new Material(shade);
            wall.SetColor("_BaseColor", new Color(.42f, .43f, .45f, 1));
            RevolverStep.Part(root, "Backdrop", PrimitiveType.Quad, wall, new Vector3(0, 1.6f, 4), Vector3.zero, new Vector3(12, 8, 1));
            Flatten(gun, shade);

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

            foreach (var (name, hold) in Variants())
            {
                FirstPersonArms arms = FirstPersonArmsStep.Create(root, gun, hold, out string report);
                Debug.Log("[PZ-PREVIEW] " + name + ": " + report);
                // Every picture here is taken within one Editor frame: the skin must follow the bones at each render, not once a frame.
                ((SkinnedMeshRenderer)arms.renderers[0]).forceMatrixRecalculationPerRender = true;
                Flatten(arms.transform, shade);
                string stem = "arms-" + name + "-";

                gun.SetPositionAndRotation(eye + look * Revolver.HipPose, look);
                arms.Pose(eye, look, 1, 0, 0);
                Describe(name + " hip", arms, eye, look);
                Shoot(camera, target, eye, look, folder, stem + "eyes-hip");
                Shoot(camera, target, eye + new Vector3(-.9f, -.1f, .25f), Quaternion.LookRotation(new Vector3(1, -.1f, .05f)), folder, stem + "left-side");
                Vector3 torch = arms.torch.position;
                Look(camera, target, torch + new Vector3(-.16f, .07f, .04f), torch, folder, stem + "torch-above");
                Look(camera, target, torch + new Vector3(.06f, -.1f, .17f), torch, folder, stem + "torch-front");
                Vector3 grip = arms.gunGrip.position;
                Look(camera, target, grip + new Vector3(.22f, .03f, .07f), grip, folder, stem + "gun-out");
                Look(camera, target, grip + new Vector3(-.22f, .03f, .07f), grip, folder, stem + "gun-in");

                gun.SetPositionAndRotation(eye + look * Revolver.AimPose, look);
                arms.Pose(eye, look, 1, 1, 0);
                Describe(name + " aim", arms, eye, look);
                Shoot(camera, target, eye, look, folder, stem + "eyes-aim");
                Object.DestroyImmediate(arms.gameObject);
            }

            camera.targetTexture = null;
            Object.DestroyImmediate(target);
        }

        /// <summary>Draws everything under <paramref name="root"/> with in-memory copies of its materials on the preview's shader.</summary>
        private static void Flatten(Transform root, Shader shade)
        {
            foreach (Renderer drawn in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] flat = drawn.sharedMaterials;
                for (int i = 0; i < flat.Length; i++)
                {
                    if (!flat[i] || flat[i].shader == shade) continue;
                    var copy = new Material(shade);
                    if (flat[i].HasProperty("_BaseMap")) copy.SetTexture("_BaseMap", flat[i].GetTexture("_BaseMap"));
                    if (flat[i].HasProperty("_BaseColor")) copy.SetColor("_BaseColor", flat[i].GetColor("_BaseColor"));
                    flat[i] = copy;
                }

                drawn.sharedMaterials = flat;
            }
        }

        private static void Describe(string pose, FirstPersonArms arms, Vector3 eye, Quaternion look)
        {
            foreach (FirstPersonArms.Arm arm in new[] { arms.left, arms.right })
            {
                Quaternion back = Quaternion.Inverse(look);
                string P(Transform t) => Round(back * (t.position - eye));
                Debug.Log("[PZ-PREVIEW] " + pose + " " + (arm.right ? "right" : "left") + ": shoulder " + P(arm.upper) + " elbow " + P(arm.fore) +
                          " wrist " + P(arm.wrist) + " hand " + P(arm.hand) + "; wrist off its target by " + FirstPersonArms.WristGap(arm).ToString("0.000") +
                          " m; forearm turned " + Twist(arm).ToString("0") + " degrees from its rest");
            }
        }

        /// <summary>How far the forearm has turned about its own length from the rest pose (the wringing the skin shows).</summary>
        private static float Twist(FirstPersonArms.Arm arm)
        {
            Quaternion now = arm.fore.localRotation, rest = arm.foreRest;
            Quaternion delta = Quaternion.Inverse(rest) * now;
            Vector3 v = Vector3.Project(new Vector3(delta.x, delta.y, delta.z), arm.foreAxis);
            var twist = new Quaternion(v.x, v.y, v.z, delta.w);
            float m = Mathf.Sqrt(v.sqrMagnitude + delta.w * delta.w);
            if (m < 1e-6f) return 0;
            twist.x /= m; twist.y /= m; twist.z /= m; twist.w /= m;
            twist.ToAngleAxis(out float degrees, out _);
            return degrees > 180 ? 360 - degrees : degrees;
        }

        private static void Look(Camera camera, RenderTexture target, Vector3 at, Vector3 on, string folder, string name) =>
            Shoot(camera, target, at, Quaternion.LookRotation(on - at), folder, name);

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
