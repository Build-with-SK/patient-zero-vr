using System;
using System.Collections.Generic;

namespace PatientZero.Integration.Logic
{
    /// <summary>
    /// The Quest Android build settings, as data, so they are reviewed and tested without Unity.
    /// Sources (checked 2026-10-28):
    ///   min API 32: Meta's mobile manifest guidance for Quest 2, Pro, 3 and 3S.
    ///   target API 34: Meta requires Android 14 for apps created after 2026-03-01.
    ///   Unity 6000.3.27f1 and package pins: Unity release API and package registry, see docs/collab/INTEGRATION_CHECKLIST.md.
    /// </summary>
    public static class QuestBuildConfig
    {
        public const string EditorVersion = "6000.3.27f1";
        public const int MinAndroidApi = 32;
        public const int TargetAndroidApi = 34;
        public const string ApplicationId = "com.buildwithsk.patientzero";
        public const string ProductName = "PATIENT ZERO";
        public const string GeneratedSceneFolder = "Assets/PatientZero/Integration/Generated";
        public const string BootstrapScenePath = GeneratedSceneFolder + "/Bootstrap.unity";
        public const string WardScenePath = GeneratedSceneFolder + "/Ward.unity";
        public const string DevApkPath = "Builds/Android/PatientZero-dev.apk";

        /// <summary>
        /// NavMesh agent type the ward is baked for (0 = Unity's built-in Humanoid) and its radius. Humanoid defaults
        /// to 0.5 m, which leaves about 0.1 m of walkable width in a 1.1 m doorway; zombies are 0.22 m agents
        /// (REVIEW_XR_SYSTEMS_6bc4c42.md, INT-B-1). The composer sets this before Environment bakes.
        /// </summary>
        public const int ZombieAgentTypeId = 0;
        public const float ZombieAgentRadius = 0.25f;

        /// <summary>Build order matters: Bootstrap is scene 0 and loads Ward additively.</summary>
        public static IReadOnlyList<string> Scenes { get; } = Array.AsReadOnly(new[] { BootstrapScenePath, WardScenePath });

        /// <summary>
        /// OpenXR features the Android build needs, by type full name. Matched by name so a missing
        /// package is a validation error, not a compile error in the build script.
        /// </summary>
        public static IReadOnlyList<string> RequiredOpenXRFeatures { get; } = Array.AsReadOnly(new[]
        {
            "UnityEngine.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature",
            "UnityEngine.XR.Hands.OpenXR.HandTracking",
            "UnityEngine.XR.Hands.OpenXR.MetaHandTrackingAim",
            "UnityEngine.XR.OpenXR.Features.Interactions.HandInteractionProfile",
            "UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile",
            "UnityEngine.XR.OpenXR.Features.Interactions.MetaQuestTouchPlusControllerProfile",
        });

        /// <summary>Packages that must never be installed (ARCHITECTURE.md section 1, "Not used").</summary>
        public static IReadOnlyList<string> ForbiddenPackagePrefixes { get; } = Array.AsReadOnly(new[]
        {
            "com.unity.xr.oculus",
            "com.meta.xr.sdk.core",
            "com.meta.xr.sdk.interaction",
            "com.meta.xr.sdk.all",
            "com.meta.xr.sdk.platform",
        });

        public static bool IsForbiddenPackage(string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return false;
            for (int i = 0; i < ForbiddenPackagePrefixes.Count; i++)
            {
                string prefix = ForbiddenPackagePrefixes[i];
                if (packageName == prefix || packageName.StartsWith(prefix + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>"6000.3.27f1" → true for any 6000.3 final (f) release.</summary>
        public static bool IsSupportedEditor(string version)
        {
            if (string.IsNullOrEmpty(version) || !version.StartsWith("6000.3.", StringComparison.Ordinal)) return false;
            string rest = version.Substring("6000.3.".Length);
            int f = rest.IndexOf('f');
            if (f <= 0 || f == rest.Length - 1) return false;
            for (int i = 0; i < rest.Length; i++)
            {
                if (i != f && (rest[i] < '0' || rest[i] > '9')) return false;
            }
            return true;
        }
    }
}
