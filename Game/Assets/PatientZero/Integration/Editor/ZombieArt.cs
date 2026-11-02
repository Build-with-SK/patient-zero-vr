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
        public const string Version = "pz-art-zombie-1";
        public const float TargetHeight = 1.75f;
        public const string SpeedParameter = "Speed";
        public const string AlertParameter = "AlertLevel";

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
                Model = model, Controller = Controller(clips), Skin = skin,
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

        private static AnimatorController Controller(AnimationClip[] clips)
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
            controller.layers[0].stateMachine.defaultState = state;
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
            bool speed = false, alert = false;
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == SpeedParameter && p.type == AnimatorControllerParameterType.Float) speed = true;
                if (p.name == AlertParameter && p.type == AnimatorControllerParameterType.Int) alert = true;
            }

            return speed && alert;
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
