// UNVERIFIED IN EDITOR
using System;
using System.Reflection;
using PatientZero.Atmosphere;
using PatientZero.Core;
using PatientZero.Environment.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace PatientZero.Audio.Editor
{
    public static class AudioBuildSteps
    {
        [SceneBuildStep(BuildScene.Bootstrap, 600)]
        public static void Build(Transform root)
        {
            var audio = root.gameObject.AddComponent<AudioCuePlayer>();
            audio.EnableLicensedSpatializer = false;
#if PZ_MOBILE_TOUCH && PZ_MOBILE_NON_XR
            audio.VoiceLimit = 16; audio.SpatialVoiceLimit = 0;
#endif
            audio.Occluders = WardCollisionBinding.Resolve().Audio; audio.Mixer = CreateMixer();
        }
        [SceneBuildStep(BuildScene.Ward, 650)]
        public static void Emitters(Transform root)
        {
            var emitters = root.gameObject.AddComponent<WardAudioEmitters>();
            var director = WardPrimitives.FindInScene<ExperienceDirector>(root);
            if (!director) throw new InvalidOperationException("Atmosphere step 200 must run before audio step 650");
            director.Emitters = emitters;
        }
        /// <summary>Unity creates native assets/groups. Reflection isolates Editor-only native factory API differences.</summary>
        private static AudioMixer CreateMixer()
        {
            const string path = "Assets/PatientZero/Audio/Generated/PatientZero.mixer";
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (existing)
            {
                foreach (string group in new[] { "World", "Voice", "Ambience", "Music", "UI" })
                    if (existing.FindMatchingGroups(group).Length != 1)
                        throw new InvalidOperationException("Generated mixer has missing/duplicate group: " + group);
                return existing;
            }
            // Factory names checked against UnityCsReference 6000.3, Editor/Mono/Audio/Mixer/Bindings/AudioMixerController.cs.
            Type controller = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            { controller = assembly.GetType("UnityEditor.Audio.AudioMixerController"); if (controller != null) break; }
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
            var factory = controller?.GetMethod("CreateMixerControllerAtPath", flags | BindingFlags.Static);
            var createGroup = controller?.GetMethod("CreateNewGroup", flags | BindingFlags.Instance);
            var add = controller?.GetMethod("AddChildToParent", flags | BindingFlags.Instance);
            var master = controller?.GetProperty("masterGroup", flags | BindingFlags.Instance);
            if (factory == null || createGroup == null || add == null || master == null)
                throw new InvalidOperationException("Unity audio mixer factory API unavailable; integration must generate the five groups through the Unity Editor.");
            WardPrimitives.GeneratedFolder("Audio");
            var mixer = (AudioMixer)factory.Invoke(null, new object[] { path });
            object parent = master.GetValue(mixer);
            foreach (string name in new[] { "World", "Voice", "Ambience", "Music", "UI" })
            {
                object group = createGroup.Invoke(mixer, new object[] { name, false });
                add.Invoke(mixer, new[] { group, parent });
            }
            EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets(); return mixer;
        }
    }
}
