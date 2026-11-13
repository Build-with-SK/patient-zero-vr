// Claude-owned integration (art pass, 2026-10-31; E-20).
using System;
using PatientZero.AI;
using PatientZero.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// The CC0 Quaternius "Animated Zombie" (Assets/ThirdParty/Quaternius/AnimatedZombie, imported by glTFast) in place of
    /// seat B's box mannequin. Builds the looping clips and the Animator controller ZombieAnimationController drives
    /// (float Speed: Idle 0, Walk 0.85, Run_Arms 1.5 m/s; int AlertLevel), then dresses each zombie at compose time.
    /// </summary>
    public static class ZombieArt
    {
        public const string ModelPath = "Assets/ThirdParty/Quaternius/AnimatedZombie/Zombie.glb";
        public const string ControllerPath = ArtTextures.Folder + "/Zombie.controller";
        /// <summary>E-21: the single-clip controller of the scripted jump-scare zombie.</summary>
        public const string PuppetControllerPath = ArtTextures.Folder + "/ZombieJumpScare.controller";
        public const string ModelName = "Zombie model (Quaternius, CC0)";
        public const string Version = "pz-art-zombie-2"; // E-25: HitReact and Death states
        public const float TargetHeight = 1.75f;
        public const string SpeedParameter = "Speed";
        public const string AlertParameter = "AlertLevel";
        /// <summary>E-25 (the revolver): triggers ZombieAnimationController sets for a body shot and a headshot.</summary>
        public const string HitParameter = "Hit", DieParameter = "Die";

        private static readonly string[] Clips = { "Idle", "Walk", "Run_Arms" };
        private static readonly float[] Thresholds = { 0f, .85f, 1.5f };

        public sealed class Kit
        {
            public GameObject Model;
            public AnimatorController Controller;
            public Material Skin;
            /// <summary>E-21: the horde wears these in turn (the first is Skin), so a pack is not a row of twins.</summary>
            public Material[] Skins;
            public float Scale;
            /// <summary>Height of the lowest vertex in the Idle pose, model units (scale 1).</summary>
            public float IdleFloor;
        }

        public static Kit Prepare()
        {
            ArtTextures.CreateFolders();
            GameObject model = Model();
            var clips = new AnimationClip[Clips.Length];
            for (int i = 0; i < Clips.Length; i++) clips[i] = LoopingCopy(Clips[i]);
            Material skin = Skin("Zombie_Skin", new Color(.8f, .84f, .76f, 1));
            var kit = new Kit
            {
                Model = model, Controller = Controller(clips, OnceCopy("HitReact"), OnceCopy("Death")), Skin = skin,
                Skins = new[] { skin, Skin("Zombie_Skin_Pale", new Color(.9f, .94f, .96f, 1)), Skin("Zombie_Skin_Dark", new Color(.6f, .55f, .5f, 1)) },
            };
            Measure(kit, clips[0]);
            AssetDatabase.SaveAssets();
            return kit;
        }

        /// <summary>Puts the model under the zombie's Body, wires Animator and Visual, and switches the mannequin off.</summary>
        public static void Dress(ZombieAnimationController zombie, Kit kit)
        {
            if (!zombie.Body) throw new InvalidOperationException(zombie.name + ": ZombieAnimationController.Body is not set (AI step 500).");
            if (zombie.Body.Find(ModelName)) throw new InvalidOperationException(zombie.name + " already has the zombie model.");
            SkinnedMeshRenderer mannequin = zombie.Visual;
            bool visible = mannequin && mannequin.enabled;

            var instance = PrefabUtility.InstantiatePrefab(kit.Model, zombie.Body) as GameObject;
            if (!instance) instance = Object.Instantiate(kit.Model, zombie.Body);
            instance.name = ModelName;
            Transform t = instance.transform;
            Transform actor = zombie.transform;
            // E-21: a horde zombie gets one of the skins and a height between 92% and 108%; the story zombies stay as they were.
            var shell = zombie.GetComponent<ZombieAgentShell>();
            int id = shell ? shell.ZombieId : 0;
            bool horde = ZombieIds.IsHorde(id);
            Material skin = horde ? kit.Skins[id % kit.Skins.Length] : kit.Skin;
            float scale = kit.Scale * (horde ? .92f + id * 37 % 17 / 100f : 1f);
            t.rotation = actor.rotation;
            t.localScale = Vector3.one * (scale / Mathf.Max(1e-4f, Mathf.Abs(zombie.Body.lossyScale.y)));
            t.position = actor.position - actor.up * (kit.IdleFloor * scale);

            var animator = instance.GetComponent<Animator>();
            if (!animator) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = kit.Controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            zombie.Animator = animator;

            SkinnedMeshRenderer body = null;
            var followers = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = skin;
                renderer.enabled = visible;
                if (!body && renderer is SkinnedMeshRenderer skinned && renderer.name == "Zombie") body = skinned;
                else followers.Add(renderer);
            }

            if (!body) throw new InvalidOperationException(ModelPath + " has no skinned mesh named 'Zombie'.");
            zombie.Visual = body;
            var mirror = instance.AddComponent<RendererEnabledMirror>();
            mirror.source = body;
            mirror.followers = followers.ToArray();
            // E-24 (owner, 2026-11-11): hands and head went through walls; the model now eases back from them.
            var clearance = instance.AddComponent<WallClearance>();
            clearance.actor = actor;
            clearance.height = StandingHeight * scale / Mathf.Max(1e-4f, kit.Scale);

            if (mannequin) mannequin.enabled = false; // the box mannequin stays in the hierarchy (its bones carry Head and the foot emitter) but never draws
        }

        /// <summary>
        /// E-21: an AI-free copy of the zombie for a scripted scene, under a pivot that stands on the floor (its forward is the
        /// way the model faces): the same model and skin, playing one looping clip from the model.
        /// </summary>
        public static Transform Puppet(Kit kit, Transform parent, string clip)
        {
            var pivot = new GameObject("Jump scare zombie").transform;
            pivot.SetParent(parent, false);
            var instance = PrefabUtility.InstantiatePrefab(kit.Model, pivot) as GameObject;
            if (!instance) instance = Object.Instantiate(kit.Model, pivot);
            instance.name = ModelName;
            instance.transform.localPosition = Vector3.down * (kit.IdleFloor * kit.Scale);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * kit.Scale;

            var animator = instance.GetComponent<Animator>();
            if (!animator) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = SingleClip(PuppetControllerPath, LoopingCopy(clip));
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = kit.Skin;
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true; // it ends up inside the camera's near view
            }

            return pivot;
        }

        /// <summary>
        /// E-23 (owner's step 1 feedback): a dead body. The same model, posed once at compose time from <paramref name="clip"/> at
        /// <paramref name="time"/> seconds (past the end means its last frame) and then frozen: no Animator, no AI, no collider.
        /// The pivot sits on the floor under the middle of the posed body; <paramref name="size"/> is the posed body's size along
        /// the pivot's own axes.
        /// </summary>
        /// <summary>The model's height at <see cref="Kit.Scale"/>, metres (measured at compose by DeadBodiesStep: 1.66 m).</summary>
        public const float StandingHeight = 1.66f;

        /// <summary>E-24: a posed body's world bounds, its pose included (CPU skinning).</summary>
        public static Bounds WorldBounds(Transform pivot)
        {
            foreach (SkinnedMeshRenderer s in pivot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s.name == "Zombie") return PosedBounds(s);
            throw new InvalidOperationException(pivot.name + " has no skinned mesh named 'Zombie'.");
        }

        public static Transform Posed(Kit kit, Transform parent, string name, string clip, float time, Material skin, out Vector3 size)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            Quaternion turned = pivot.rotation;
            pivot.rotation = Quaternion.identity;
            // A plain copy, not a prefab instance: its Animator is removed, which a prefab instance would keep as an override.
            GameObject instance = Object.Instantiate(kit.Model, pivot);
            instance.name = ModelName;
            Transform t = instance.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * kit.Scale;
            AnimationClip source = SourceClip(clip);
            source.SampleAnimation(instance, Mathf.Clamp(time, 0, source.length));
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
            SkinnedMeshRenderer body = null;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = skin;
                if (!(renderer is SkinnedMeshRenderer skinned)) continue;
                skinned.updateWhenOffscreen = true; // the bind-pose bounds do not cover a body lying down
                if (skinned.name == "Zombie") body = skinned;
            }

            if (!body) throw new InvalidOperationException(ModelPath + " has no skinned mesh named 'Zombie'.");
            Bounds b = PosedBounds(body);
            Vector3 p = pivot.position;
            t.position -= new Vector3(b.center.x - p.x, b.min.y - p.y, b.center.z - p.z);
            size = b.size;
            pivot.rotation = turned;
            return pivot;
        }

        /// <summary>
        /// E-23: a body hanging from the ceiling at <paramref name="top"/>: arms and head slack, the feet
        /// <paramref name="feetAbove"/> metres over the floor at <paramref name="floorY"/> (the rope 0.3 to 1.6 m long). The returned
        /// transform is the top of the rope, so turning it swings the whole body; <paramref name="bounds"/> is the body as it hangs.
        /// </summary>
        public static Transform Hanged(Kit kit, Transform parent, Material skin, Material ropeMaterial, Vector3 top, Vector3 facing,
                                       float floorY, float feetAbove, out Bounds bounds)
        {
            var swing = new GameObject("Hanged body").transform;
            swing.SetParent(parent, false);
            facing.y = 0;
            swing.SetPositionAndRotation(top, Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward, Vector3.up));
            Transform pivot = Posed(kit, swing, "Body", "Idle", 0, skin, out _);
            Transform model = pivot.Find(ModelName);
            Vector3 right = swing.right, forward = swing.forward;
            Slack(Bone(model, "UpperArm.L"), Bone(model, "LowerArm.L"), Vector3.down - right * .12f);
            Slack(Bone(model, "LowerArm.L"), Bone(model, "Middle1.L"), Vector3.down + forward * .08f);
            Slack(Bone(model, "UpperArm.R"), Bone(model, "LowerArm.R"), Vector3.down + right * .12f);
            Slack(Bone(model, "LowerArm.R"), Bone(model, "Middle1.R"), Vector3.down + forward * .08f);
            Transform neck = Bone(model, "Neck"), head = Bone(model, "Head");
            neck.rotation = Quaternion.AngleAxis(14, right) * neck.rotation;
            head.rotation = Quaternion.AngleAxis(30, right) * Quaternion.AngleAxis(-22, forward) * head.rotation;
            SkinnedMeshRenderer mesh = null;
            foreach (SkinnedMeshRenderer s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "Zombie") mesh = s;
            Bounds posed = PosedBounds(mesh);
            float rope = Mathf.Clamp(top.y - floorY - feetAbove - (neck.position.y - posed.min.y), .3f, 1.6f);
            Vector3 move = top + Vector3.down * rope - neck.position;
            pivot.position += move;
            posed.center += move;
            bounds = posed;

            var cord = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cord.name = "Rope";
            Object.DestroyImmediate(cord.GetComponent<Collider>());
            cord.transform.SetParent(swing, false);
            float length = Mathf.Max(.05f, top.y - neck.position.y + .04f);
            cord.transform.position = new Vector3(neck.position.x, top.y - length * .5f, neck.position.z);
            cord.transform.localScale = new Vector3(.028f, length, .028f);
            var renderer = cord.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ropeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return swing;
        }

        /// <summary>Turns <paramref name="bone"/> so the way to <paramref name="child"/> points along <paramref name="direction"/>.</summary>
        private static void Slack(Transform bone, Transform child, Vector3 direction)
        {
            Vector3 now = child.position - bone.position;
            if (now.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(now, direction) * bone.rotation;
        }

        private static Transform Bone(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }

            throw new InvalidOperationException(ModelPath + " has no bone '" + name + "'.");
        }

        /// <summary>World bounds of the posed mesh, skinned on the CPU like <see cref="VerticalExtent"/>.</summary>
        private static Bounds PosedBounds(SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer.sharedMesh;
            Transform[] bones = renderer.bones;
            Matrix4x4[] bindposes = mesh.bindposes;
            BoneWeight[] weights = mesh.boneWeights;
            Vector3[] vertices = mesh.vertices;
            var skin = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                if (!bones[i]) throw new InvalidOperationException(ModelPath + ": bone " + i + " of '" + renderer.name + "' is missing.");
                skin[i] = bones[i].localToWorldMatrix * bindposes[i];
            }

            bool any = false;
            var bounds = new Bounds();
            for (int v = 0; v < vertices.Length && v < weights.Length; v++)
            {
                BoneWeight w = weights[v];
                float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                if (sum <= 0) continue;
                Vector3 p = vertices[v];
                Vector3 world = (skin[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 + skin[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 +
                                 skin[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 + skin[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3) / sum;
                if (!any) bounds = new Bounds(world, Vector3.zero);
                else bounds.Encapsulate(world);
                any = true;
            }

            if (!any) throw new InvalidOperationException(ModelPath + ": '" + renderer.name + "' has no weighted vertices.");
            return bounds;
        }

        // ------------------------------------------------------------------ assets

        private static GameObject Model()
        {
            AssetImporter importer = AssetImporter.GetAtPath(ModelPath);
            if (importer == null)
                throw new InvalidOperationException(ModelPath + " is missing.");
            if (importer.GetType().Name != "GltfImporter")
                throw new InvalidOperationException(ModelPath + " is imported by " + importer.GetType().Name +
                                                    ", not glTFast. Is com.unity.cloud.gltfast in Packages/manifest.json?");

            var settings = new SerializedObject(importer);
            SerializedProperty method = settings.FindProperty("importSettings.animationMethod");
            if (method == null) throw new InvalidOperationException("glTFast importer has no importSettings.animationMethod (package version changed?).");
            int mecanim = Array.IndexOf(method.enumNames, "Mecanim");
            if (mecanim < 0) throw new InvalidOperationException("glTFast AnimationMethod has no Mecanim value.");
            if (method.enumValueIndex != mecanim)
            {
                method.enumValueIndex = mecanim;
                settings.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!model) throw new InvalidOperationException(ModelPath + " did not import a GameObject.");
            return model;
        }

        private static AnimationClip SourceClip(string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is AnimationClip clip && clip.name == name) return clip;
            }

            throw new InvalidOperationException(ModelPath + " has no animation '" + name + "'.");
        }

        /// <summary>A looping .anim copy of one imported clip. The asset keeps its GUID across composes.</summary>
        private static AnimationClip LoopingCopy(string name)
        {
            AnimationClip source = SourceClip(name);
            if (source.legacy) throw new InvalidOperationException("Clip '" + name + "' imported as Legacy; the importer must use Mecanim.");
            string path = ArtTextures.Folder + "/Zombie_" + name + ".anim";
            var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!copy)
            {
                copy = new AnimationClip();
                AssetDatabase.CreateAsset(copy, path);
            }

            EditorUtility.CopySerialized(source, copy);
            copy.name = "Zombie_" + name;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            EditorUtility.SetDirty(copy);
            return copy;
        }

        /// <summary>E-25: a play-once .anim copy of one imported clip (it holds its last frame).</summary>
        private static AnimationClip OnceCopy(string name)
        {
            AnimationClip copy = LoopingCopy(name);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static AnimatorController Controller(AnimationClip[] clips, AnimationClip hit, AnimationClip death)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AssetImporter importer = controller ? AssetImporter.GetAtPath(ControllerPath) : null;
            if (controller && importer != null && importer.userData == Version && HasParameters(controller)) return controller;

            if (controller) AssetDatabase.DeleteAsset(ControllerPath);
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(AlertParameter, AnimatorControllerParameterType.Int);
            AnimatorState state = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = SpeedParameter;
            tree.useAutomaticThresholds = false;
            for (int i = 0; i < clips.Length; i++) tree.AddChild(clips[i], Thresholds[i]);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.defaultState = state;

            // E-25: a body shot plays HitReact once and returns to walking; a headshot plays Death and holds the last frame.
            controller.AddParameter(HitParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(DieParameter, AnimatorControllerParameterType.Trigger);
            AnimatorState flinch = machine.AddState("HitReact");
            flinch.motion = hit;
            AnimatorStateTransition back = flinch.AddTransition(state);
            back.hasExitTime = true; back.exitTime = .9f; back.duration = .12f;
            AnimatorStateTransition toFlinch = machine.AddAnyStateTransition(flinch);
            toFlinch.AddCondition(AnimatorConditionMode.If, 0, HitParameter);
            toFlinch.duration = .05f; toFlinch.canTransitionToSelf = true;
            AnimatorState fall = machine.AddState("Death");
            fall.motion = death;
            AnimatorStateTransition toFall = machine.AddAnyStateTransition(fall);
            toFall.AddCondition(AnimatorConditionMode.If, 0, DieParameter);
            toFall.duration = .1f; toFall.canTransitionToSelf = false;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            importer = AssetImporter.GetAtPath(ControllerPath);
            importer.userData = Version;
            AssetDatabase.WriteImportSettingsIfDirty(ControllerPath);
            if (!HasParameters(controller)) throw new InvalidOperationException(ControllerPath + " lacks Speed/AlertLevel after creation.");
            return controller;
        }

        private static AnimatorController SingleClip(string path, AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            AssetImporter importer = controller ? AssetImporter.GetAtPath(path) : null;
            if (controller && importer != null && importer.userData == Version + "/" + clip.name) return controller;

            if (controller) AssetDatabase.DeleteAsset(path);
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddMotion(clip);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            importer = AssetImporter.GetAtPath(path);
            importer.userData = Version + "/" + clip.name;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            return controller;
        }

        private static bool HasParameters(AnimatorController controller)
        {
            bool speed = false, alert = false, hit = false, die = false;
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == SpeedParameter && p.type == AnimatorControllerParameterType.Float) speed = true;
                if (p.name == AlertParameter && p.type == AnimatorControllerParameterType.Int) alert = true;
                if (p.name == HitParameter && p.type == AnimatorControllerParameterType.Trigger) hit = true;
                if (p.name == DieParameter && p.type == AnimatorControllerParameterType.Trigger) die = true;
            }

            return speed && alert && hit && die;
        }

        /// <summary>URP Lit on the model's colour atlas, a little desaturated and damp, so it sits in the ward's light.</summary>
        private static Material Skin(string name, Color tint)
        {
            Texture2D atlas = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is Texture2D texture) { atlas = texture; break; }
            }

            if (!atlas) throw new InvalidOperationException(ModelPath + " has no texture atlas.");
            Material skin = ArtSurfaces.LitMaterial(name);
            skin.SetTexture("_BaseMap", atlas);
            skin.SetColor("_BaseColor", tint);
            skin.SetTexture("_BumpMap", null);
            skin.DisableKeyword("_NORMALMAP");
            skin.SetFloat("_Metallic", 0);
            skin.SetFloat("_Smoothness", .28f);
            skin.SetFloat("_SmoothnessTextureChannel", 0);
            skin.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            skin.SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(skin);
            return skin;
        }

        /// <summary>Scale to <see cref="TargetHeight"/> from the bind pose, and where the feet are in the Idle pose.</summary>
        private static void Measure(Kit kit, AnimationClip idle)
        {
            var probe = Object.Instantiate(kit.Model);
            try
            {
                probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                probe.transform.localScale = Vector3.one;
                SkinnedMeshRenderer body = null;
                foreach (SkinnedMeshRenderer s in probe.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (s.name == "Zombie") body = s;
                }

                if (!body) throw new InvalidOperationException(ModelPath + " has no skinned mesh named 'Zombie'.");
                Vector2 bind = VerticalExtent(body);
                float height = bind.y - bind.x;
                if (height < .5f || height > 5f) throw new InvalidOperationException("Zombie model height " + height + " m is implausible.");
                kit.Scale = TargetHeight / height;
                idle.SampleAnimation(probe, 0);
                kit.IdleFloor = VerticalExtent(body).x;
            }
            finally
            {
                Object.DestroyImmediate(probe);
            }
        }

        /// <summary>
        /// Lowest and highest vertex of the posed mesh, world Y, skinned on the CPU from the bones' current transforms.
        /// Not BakeMesh: it ignores the scale of the renderer's parents, and this armature is scaled by 100.
        /// </summary>
        private static Vector2 VerticalExtent(SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer.sharedMesh;
            Transform[] bones = renderer.bones;
            Matrix4x4[] bindposes = mesh.bindposes;
            BoneWeight[] weights = mesh.boneWeights;
            Vector3[] vertices = mesh.vertices;
            if (bones.Length == 0 || bindposes.Length != bones.Length || weights.Length != vertices.Length)
                throw new InvalidOperationException(ModelPath + ": '" + renderer.name + "' has " + bones.Length + " bones, " +
                                                    bindposes.Length + " bind poses and " + weights.Length + " weights for " +
                                                    vertices.Length + " vertices.");

            var skin = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                if (!bones[i]) throw new InvalidOperationException(ModelPath + ": bone " + i + " of '" + renderer.name + "' is missing.");
                skin[i] = bones[i].localToWorldMatrix * bindposes[i];
            }

            float min = float.MaxValue, max = float.MinValue;
            for (int v = 0; v < vertices.Length; v++)
            {
                BoneWeight w = weights[v];
                float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                if (sum <= 0) continue;
                Vector3 p = vertices[v];
                Vector3 world = (skin[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 + skin[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 +
                                 skin[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 + skin[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3) / sum;
                if (world.y < min) min = world.y;
                if (world.y > max) max = world.y;
            }

            if (min > max) throw new InvalidOperationException(ModelPath + ": '" + renderer.name + "' has no weighted vertices.");
            return new Vector2(min, max);
        }
    }
}
