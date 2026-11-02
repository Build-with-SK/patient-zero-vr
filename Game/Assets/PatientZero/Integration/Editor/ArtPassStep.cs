// Claude-owned integration (art pass, 2026-10-31; E-20).
using System;
using System.Collections.Generic;
using PatientZero.AI;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// The art pass, done at compose time so no seat file changes:
    ///   Ward 980:      every zombie wears the CC0 Quaternius model (ZombieArt) instead of the box mannequin; every box
    ///                  built with a seat material gets a textured material and world-metre UVs (ArtSurfaces); blood on
    ///                  the white tiles, floors and ceilings (ArtBlood).
    ///   Bootstrap 980: the flat-screen post-processing volume (FlatScreenPost), off while a headset is active.
    /// Runs after every seat step (and after WorldTextStep 970), so it sees the finished scene.
    /// </summary>
    public static class ArtPassStep
    {
        public const string ProfilePath = ArtTextures.Folder + "/FlatScreenPost.asset";

        [SceneBuildStep(BuildScene.Ward, 980)]
        public static void BuildWard(Transform root)
        {
            GameObject[] sceneRoots = root.gameObject.scene.GetRootGameObjects();
            var zombies = new List<ZombieAnimationController>();
            foreach (GameObject sceneRoot in sceneRoots) zombies.AddRange(sceneRoot.GetComponentsInChildren<ZombieAnimationController>(true));
            if (zombies.Count == 0) throw new InvalidOperationException("No ZombieAnimationController in Ward: the AI step (500) must run before the art pass.");

            ZombieArt.Kit kit = ZombieArt.Prepare();
            foreach (ZombieAnimationController zombie in zombies) ZombieArt.Dress(zombie, kit);

            var surfaces = new ArtSurfaces();
            surfaces.Apply(sceneRoots, renderer => renderer.GetComponentInParent<ZombieAnimationController>(true));
            if (surfaces.Swapped == 0) throw new InvalidOperationException("The art pass recognised no material in Ward: seat material names changed?");
            ArtBlood.Counts blood = ArtBlood.Place(sceneRoots, root);
            Debug.Log("[PZ] art pass: " + zombies.Count + " zombies dressed, " + surfaces.Swapped + " renderers re-materialled, " +
                      surfaces.Meshes + " boxes given world-metre UVs; blood: " + blood);
        }

        [SceneBuildStep(BuildScene.Bootstrap, 980)]
        public static void BuildBootstrap(Transform root)
        {
            Camera view = PlayerCamera(root);
            UniversalAdditionalCameraData data = view.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; // FlatScreenPost turns it on when no headset is active
            data.dithering = true;             // no banding in the dark gradients (part of the post pass, so flat screens only)

            var volume = root.gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            volume.sharedProfile = Profile();
            volume.enabled = false;
            var post = root.gameObject.AddComponent<FlatScreenPost>();
            post.volume = volume;
            post.cameraData = data;
        }

        private static Camera PlayerCamera(Transform root)
        {
            var cameras = new List<Camera>();
            var tagged = new List<Camera>();
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
            {
                foreach (Camera c in sceneRoot.GetComponentsInChildren<Camera>(true))
                {
                    cameras.Add(c);
                    if (c.CompareTag("MainCamera")) tagged.Add(c);
                }
            }

            if (tagged.Count == 1) return tagged[0];
            if (cameras.Count == 1) return cameras[0];
            throw new InvalidOperationException("Bootstrap must hold exactly one player camera (found " + cameras.Count + ", " + tagged.Count + " tagged MainCamera).");
        }

        /// <summary>Bloom, ACES, grading, vignette and film grain. Updated in place, so its GUID never changes.</summary>
        private static VolumeProfile Profile()
        {
            ArtTextures.CreateFolders();
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var bloom = Component<Bloom>(profile);
            bloom.threshold.Override(.9f);
            bloom.intensity.Override(.7f);
            bloom.scatter.Override(.65f);
            bloom.tint.Override(new Color(1f, .95f, .88f));

            Component<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);

            var grade = Component<ColorAdjustments>(profile);
            grade.postExposure.Override(.45f);
            grade.contrast.Override(16f);
            grade.saturation.Override(-15f);
            grade.colorFilter.Override(new Color(.93f, 1f, .97f));

            // No lift in the blacks: lift works in linear light, so even 0.015 put a grey veil of about 33/255 over every
            // dark area (measured on the probe screenshots). ACES's toe keeps the shadow detail on its own.
            if (profile.TryGet(out LiftGammaGain lift))
            {
                profile.Remove<LiftGammaGain>();
                Object.DestroyImmediate(lift, true);
            }

            var vignette = Component<Vignette>(profile);
            vignette.intensity.Override(.38f);
            vignette.smoothness.Override(.45f);

            var grain = Component<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(.2f);
            grain.response.Override(.8f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static T Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>();
                component.name = typeof(T).Name;
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }
    }
}
