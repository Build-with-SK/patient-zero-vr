// Claude-owned integration (owner, 2026-11-13: visible hands for the player, "Yes, find a model"; E-27).
using System;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// E-27: the player's arms on a flat screen, built at compose time after the revolver (Ward 989). The CC0 "fps arms" by
    /// para (OpenGameArt, made with MakeHuman; Assets/ThirdParty/OpenGameArt/FpsArms) is put under an "Eyes" pivot with its
    /// shoulders below and behind the eyes; its fingers are curled round a torch's handle (left) and the revolver's grip
    /// (right); a torch made of plain shapes goes in the left hand. <see cref="FirstPersonArms"/> poses the arms every frame.
    /// No collider, no light, no shadow: nothing here can block a path, a ray or a grab.
    /// </summary>
    public static class FirstPersonArmsStep
    {
        public const string Folder = "Assets/ThirdParty/OpenGameArt/FpsArms";
        public const string ModelPath = Folder + "/FpsArms.fbx", SkinPath = Folder + "/FpsArms_Skin.png";
        public const string ModelName = "Arms model (para, CC0)";
        /// <summary>From the eyes: the middle of the shoulders, below the eyes and a little behind them.</summary>
        public static readonly Vector3 Shoulders = new Vector3(0, -.24f, -.07f);
        /// <summary>A human upper arm is about 0.29 m; a model far off that (a unit mistake) is scaled to it.</summary>
        public const float UpperArmMetres = .29f;

        private static readonly string[] Fingers = { "f_index", "f_middle", "f_ring", "f_pinky" };

        [SceneBuildStep(BuildScene.Ward, 989)]
        public static void Build(Transform root)
        {
            Revolver revolver = null;
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
                if (!revolver) revolver = sceneRoot.GetComponentInChildren<Revolver>(true);
            if (!revolver || !revolver.model) throw new InvalidOperationException("Ward has no revolver: RevolverStep (Ward 988) must come first.");
            FirstPersonArms arms = Create(root, revolver.model, out string report);
            arms.revolver = revolver;
            Debug.Log("[PZ] first-person arms: " + report);
        }

        /// <summary>Builds the arms and the torch under <paramref name="parent"/>; the right hand holds <paramref name="gun"/>'s grip.</summary>
        public static FirstPersonArms Create(Transform parent, Transform gun, out string report) => Create(parent, gun, new FirstPersonArms.Hold(), out report);

        /// <summary>As above, with the hands holding the torch and the gun as <paramref name="hold"/> says (ArmsPreview tries several).</summary>
        public static FirstPersonArms Create(Transform parent, Transform gun, FirstPersonArms.Hold hold, out string report)
        {
            GameObject asset = Model();
            var holder = new GameObject("First-person arms");
            holder.transform.SetParent(parent, false);
            var eyes = new GameObject("Eyes").transform;
            eyes.SetParent(holder.transform, false);
            var instance = PrefabUtility.InstantiatePrefab(asset, eyes) as GameObject;
            if (!instance) instance = Object.Instantiate(asset, eyes);
            instance.name = ModelName;
            Transform model = instance.transform;
            // The Generic rig gives the model an Animator; nothing animates it (FirstPersonArms poses the bones).
            var animator = instance.GetComponent<Animator>();
            if (animator) animator.enabled = false;

            // Square to the eyes: shoulder to shoulder along the eyes' right, the model's up along theirs.
            Transform upperL = Bone(model, "upper_arm.L"), upperR = Bone(model, "upper_arm.R");
            float upperLength = Vector3.Distance(upperR.position, Bone(model, "forearm.R").position);
            float scale = 1;
            if (upperLength < UpperArmMetres * .5f || upperLength > UpperArmMetres * 2)
            {
                scale = UpperArmMetres / upperLength;
                model.localScale *= scale;
            }

            Vector3 across = upperR.position - upperL.position;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, across).normalized;
            Quaternion frame = Quaternion.LookRotation(Vector3.Cross(across.normalized, up), up);
            model.rotation = eyes.rotation * Quaternion.Inverse(frame) * model.rotation;
            model.position += eyes.TransformPoint(Shoulders) - (upperL.position + upperR.position) * .5f;

            HandFrame(model, "L", false, out Vector3 fL, out Vector3 nL);
            HandFrame(model, "R", true, out Vector3 fR, out Vector3 nR);
            for (int i = 0; i < Fingers.Length; i++)
            {
                Curl(model, Fingers[i], "L", false, fL, nL, hold.torchRake, hold.torchFingers);
                Curl(model, Fingers[i], "R", true, fR, nR, hold.gunRake, i == 0 ? hold.gunIndex : hold.gunFingers);
            }

            Thumb(model, "L", false, nL, hold.torchThumb);
            Thumb(model, "R", true, nR, hold.gunThumb);

            var arms = holder.AddComponent<FirstPersonArms>();
            arms.hold = hold;
            arms.eyes = eyes;
            arms.left = Arm(model, "L", false, fL, nL);
            arms.right = Arm(model, "R", true, fR, nR);

            SkinnedMeshRenderer skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (!skin || !skin.sharedMesh) throw new InvalidOperationException(ModelPath + " has no skinned mesh.");
            skin.sharedMaterials = new[] { Skin() };
            skin.shadowCastingMode = ShadowCastingMode.Off;
            skin.receiveShadows = false;
            skin.updateWhenOffscreen = true; // the bones move far from the rest pose's bounds
            skin.skinnedMotionVectors = false;

            arms.torch = Torch(holder.transform, out Renderer lens, out Material on, out Material off, out Renderer[] torchParts);
            arms.lens = lens;
            arms.lensOn = on;
            arms.lensOff = off;
            lens.sharedMaterial = off;
            arms.gunGrip = GunGrip(gun);
            var shown = new Renderer[torchParts.Length + 1];
            shown[0] = skin;
            torchParts.CopyTo(shown, 1);
            arms.renderers = shown;

            report = skin.sharedMesh.vertexCount + " vertices, " + skin.bones.Length + " bones" + (scale != 1 ? ", scaled x" + scale.ToString("0.00") : "") +
                     "; upper arm " + Metres(arms.right.upper, arms.right.fore) + " m, forearm " + Metres(arms.right.fore, arms.right.wrist) +
                     " m; wrist to hand handle at rest " + Metres(arms.right.wrist, arms.right.hand) + " m (R), " + Metres(arms.left.wrist, arms.left.hand) + " m (L)";
            return arms;
        }

        // ------------------------------------------------------------------ the model

        /// <summary>
        /// The FBX imported with a Generic rig but no avatar (with no rig at all Unity drops the skinning), and without
        /// animation, materials, cameras or lights; the importer is set once and saved in its .meta.
        /// </summary>
        private static GameObject Model()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new InvalidOperationException(ModelPath + " is missing or is not a model (see " + Folder + "/LICENSE.md).");
            bool change = false;
            if (importer.animationType != ModelImporterAnimationType.Generic) { importer.animationType = ModelImporterAnimationType.Generic; change = true; }
            if (importer.avatarSetup != ModelImporterAvatarSetup.NoAvatar) { importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar; change = true; }
            if (importer.optimizeGameObjects) { importer.optimizeGameObjects = false; change = true; }
            if (importer.importAnimation) { importer.importAnimation = false; change = true; }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None) { importer.materialImportMode = ModelImporterMaterialImportMode.None; change = true; }
            if (importer.importCameras) { importer.importCameras = false; change = true; }
            if (importer.importLights) { importer.importLights = false; change = true; }
            if (importer.importBlendShapes) { importer.importBlendShapes = false; change = true; }
            if (importer.importVisibility) { importer.importVisibility = false; change = true; }
            if (change) importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!model) throw new InvalidOperationException(ModelPath + " did not import.");
            return model;
        }

        private static Material Skin()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SkinPath);
            if (!texture) throw new InvalidOperationException(SkinPath + " is missing (see " + Folder + "/LICENSE.md).");
            Material m = ArtSurfaces.LitMaterial("Arms_Skin");
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", new Color(.9f, .86f, .84f, 1));
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", .3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// A hand's frame in the rest pose: f from the wrist to the middle finger's knuckle; n the palm's normal (out of the palm),
        /// found from the knuckles' line t (little finger to index): t = n x f for a right hand, t = f x n for a left one.
        /// </summary>
        private static void HandFrame(Transform model, string side, bool right, out Vector3 f, out Vector3 n)
        {
            Vector3 wrist = Bone(model, "hand." + side).position;
            f = (Bone(model, "f_middle.01." + side).position - wrist).normalized;
            Vector3 t = Vector3.ProjectOnPlane(Bone(model, "f_index.01." + side).position - Bone(model, "f_pinky.01." + side).position, f).normalized;
            n = right ? Vector3.Cross(f, t) : Vector3.Cross(t, f);
        }

        /// <summary>
        /// Bends a finger's three joints toward the palm, the knuckle first, about the handle's own axis (the knuckles' line turned
        /// by the grip's rake toward the fingers), so the fingers wrap the handle across the palm as in a power grip.
        /// </summary>
        private static void Curl(Transform model, string finger, string side, bool right, Vector3 f, Vector3 n, float rake, Vector3 degrees)
        {
            Vector3 t = right ? Vector3.Cross(n, f) : Vector3.Cross(f, n);
            Vector3 handle = t * Mathf.Cos(rake * Mathf.Deg2Rad) + f * Mathf.Sin(rake * Mathf.Deg2Rad);
            // f x n is -t for a right hand and +t for a left one: the turn that brings the fingers toward the palm.
            Vector3 axis = (right ? -handle : handle).normalized;
            for (int j = 0; j < 3; j++)
            {
                Transform bone = Bone(model, finger + ".0" + (j + 1) + "." + side);
                bone.rotation = Quaternion.AngleAxis(degrees[j], axis) * bone.rotation;
            }
        }

        /// <summary>Turns the thumb across the palm toward the fingers, then bends its other two joints toward the palm.</summary>
        private static void Thumb(Transform model, string side, bool right, Vector3 n, Vector3 degrees)
        {
            Transform first = Bone(model, "thumb.01." + side), second = Bone(model, "thumb.02." + side), third = Bone(model, "thumb.03." + side);
            // Across the palm: from the knuckles' line toward the fingers, about the palm's normal (signs from t = n x f / f x n).
            first.rotation = Quaternion.AngleAxis(degrees.x, right ? -n : n) * first.rotation;
            Vector3 along = (second.position - first.position).normalized;
            Vector3 axis = Vector3.Cross(along, n).normalized;
            second.rotation = Quaternion.AngleAxis(degrees.y, axis) * second.rotation;
            third.rotation = Quaternion.AngleAxis(degrees.z, axis) * third.rotation;
        }

        private static FirstPersonArms.Arm Arm(Transform model, string side, bool right, Vector3 f, Vector3 n)
        {
            var arm = new FirstPersonArms.Arm
            {
                upper = Bone(model, "upper_arm." + side), fore = Bone(model, "forearm." + side), wrist = Bone(model, "forearm." + side + "_end"),
                hand = Bone(model, "hand." + side + ".control"), right = right,
            };
            arm.upperRest = arm.upper.localRotation;
            arm.foreRest = arm.fore.localRotation;
            arm.inGrip = Quaternion.Inverse(Quaternion.LookRotation(f, n)) * arm.hand.rotation;
            arm.inFore = Quaternion.Inverse(arm.fore.rotation) * arm.hand.rotation;
            arm.foreAxis = arm.fore.InverseTransformDirection(arm.wrist.position - arm.fore.position).normalized;
            Vector3 s = arm.upper.position, e = arm.fore.position, w = arm.wrist.position;
            Vector3 hinge = Vector3.Cross(e - s, w - e);
            if (hinge.sqrMagnitude < 1e-10f) throw new InvalidOperationException(ModelPath + ": the " + side + " elbow is straight at rest; its hinge is unknown.");
            arm.hinge = arm.upper.InverseTransformDirection(hinge.normalized);
            arm.wristInHand = Quaternion.Inverse(arm.hand.rotation) * (w - arm.hand.position);
            return arm;
        }

        // ------------------------------------------------------------------ the torch and the gun's grip

        /// <summary>A torch about 20 cm long made of plain shapes: the middle of the handle at its origin, the lens along +z.</summary>
        private static Transform Torch(Transform parent, out Renderer lens, out Material on, out Material off, out Renderer[] parts)
        {
            Material body = RevolverStep.Paint("Torch_Body", new Color(.045f, .045f, .05f, 1), .6f, .45f);
            Material bright = RevolverStep.Paint("Torch_Bezel", new Color(.42f, .42f, .44f, 1), 1, .75f);
            Material rubber = RevolverStep.Paint("Torch_Switch", new Color(.02f, .02f, .02f, 1), 0, .2f);
            off = RevolverStep.Paint("Torch_Lens_Off", new Color(.12f, .13f, .14f, 1), 0, .92f);
            on = LensOn();
            var torch = new GameObject("Torch").transform;
            torch.SetParent(parent, false);
            // Unity's cylinder is 2 m tall along y and 1 m across: scale (diameter, length / 2, diameter), turned to lie along z.
            var turn = new Vector3(90, 0, 0);
            RevolverStep.Part(torch, "Body", PrimitiveType.Cylinder, body, new Vector3(0, 0, .005f), turn, new Vector3(.032f, .075f, .032f));
            RevolverStep.Part(torch, "Head", PrimitiveType.Cylinder, body, new Vector3(0, 0, .1f), turn, new Vector3(.044f, .02f, .044f));
            RevolverStep.Part(torch, "Bezel", PrimitiveType.Cylinder, bright, new Vector3(0, 0, .1235f), turn, new Vector3(.046f, .0035f, .046f));
            Transform glass = RevolverStep.Part(torch, "Lens", PrimitiveType.Cylinder, off, new Vector3(0, 0, .127f), turn, new Vector3(.038f, .0015f, .038f));
            RevolverStep.Part(torch, "Tail cap", PrimitiveType.Cylinder, bright, new Vector3(0, 0, -.072f), turn, new Vector3(.034f, .005f, .034f));
            RevolverStep.Part(torch, "Switch", PrimitiveType.Cube, rubber, new Vector3(0, .017f, .045f), Vector3.zero, new Vector3(.01f, .004f, .016f));
            lens = glass.GetComponent<Renderer>();
            parts = torch.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in parts)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            return torch;
        }

        /// <summary>The lens while the torch is on: warm white and self-lit (the light itself is Atmosphere's HandheldLight).</summary>
        private static Material LensOn()
        {
            Material m = RevolverStep.Paint("Torch_Lens_On", new Color(1, .96f, .88f, 1), 0, .9f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1f, .95f, .86f, 1) * 4f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The revolver's wooden grip (RevolverStep names both it and the headset's attach point "Grip": this is the one drawn).</summary>
        private static Transform GunGrip(Transform gun)
        {
            foreach (Transform child in gun)
                if (child.name == "Grip" && child.GetComponent<MeshRenderer>()) return child;
            throw new InvalidOperationException("The revolver model has no drawn Grip (RevolverStep).");
        }

        // ------------------------------------------------------------------ helpers

        private static Transform Bone(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            throw new InvalidOperationException(ModelPath + " has no bone '" + name + "'.");
        }

        private static string Metres(Transform a, Transform b) => Vector3.Distance(a.position, b.position).ToString("0.000");
    }
}
