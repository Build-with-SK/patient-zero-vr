// Claude-owned release tooling (owner final-release mission, 2026-10-30).
using System;
using System.IO;
using PatientZero.Integration.Logic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

namespace PatientZero.Integration.Editor.Quest
{
    /// <summary>
    /// The two flat-screen release builds. Neither changes what the Quest build ships.
    ///   Windows x64: keyboard and mouse (PZ_DESKTOP_MOUSE passed to this build only), fullscreen window at the desktop
    ///     resolution, XR not started, Mono, non-development. Builds/Windows/PatientZero/PatientZero.exe
    ///   Android phone: touch controls (PZ_TOUCH passed to this build only), its own application id so it installs next
    ///     to the Quest app, landscape only, Vulkan with OpenGL ES 3 fallback, Android 10+, no XR loader (so no VR
    ///     manifest entries), debug-signed for sideloading. Builds/Android/PatientZero-phone.apk
    ///     Android player settings are shared with Quest, so they are saved first and the Quest configuration
    ///     (QuestBuild.Configure) is put back afterwards, even when the build fails.
    ///   iPad: touch controls (PZ_TOUCH), landscape only, full screen, Metal, exported as an Xcode project
    ///     (Builds/iOS/PatientZero-Xcode). Needs Unity's iOS Build Support module; turning the project into an
    ///     installable app then needs a Mac with Xcode and the owner's Apple developer team (signing is left to Xcode).
    /// Menu:      PatientZero/Release/Build Windows, PatientZero/Release/Build Android Phone, PatientZero/Release/Build iPad (Xcode)
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.Quest.ReleaseBuild.BuildWindowsBatch (-buildTarget Win64)
    ///            -executeMethod PatientZero.Integration.Editor.Quest.ReleaseBuild.BuildPhoneBatch (-buildTarget Android)
    ///            -executeMethod PatientZero.Integration.Editor.Quest.ReleaseBuild.BuildIPadBatch (-buildTarget iOS)
    /// </summary>
    public static class ReleaseBuild
    {
        public const string Version = "1.0.0";
        public const string WindowsExePath = "Builds/Windows/PatientZero/PatientZero.exe";
        public const string PhoneApkPath = "Builds/Android/PatientZero-phone.apk";
        public const string IPadXcodePath = "Builds/iOS/PatientZero-Xcode";
        public const string IPadBundleId = "com.buildwithsk.patientzero";
        public const string PhoneApplicationId = "com.buildwithsk.patientzero.mobile";
        public const int PhoneMinAndroidApi = 29; // Android 10
        private const string CompanyName = "Build with SK";

        [MenuItem("PatientZero/Release/Build Windows", priority = 70)]
        public static void WindowsFromMenu() => Run("build-windows", BuildWindows, exit: false);

        [MenuItem("PatientZero/Release/Build Android Phone", priority = 71)]
        public static void PhoneFromMenu() => Run("build-phone", BuildPhone, exit: false);

        [MenuItem("PatientZero/Release/Build iPad (Xcode)", priority = 72)]
        public static void IPadFromMenu() => Run("build-ipad", BuildIPad, exit: false);

        public static void BuildWindowsBatch() => Run("build-windows", BuildWindows, exit: true);
        public static void BuildIPadBatch() => Run("build-ipad", BuildIPad, exit: true);
        public static void BuildPhoneBatch() => Run("build-phone", BuildPhone, exit: true);

        private static void Run(string name, Action<EditorReport> body, bool exit)
        {
            var report = new EditorReport(name);
            try
            {
                body(report);
            }
            catch (Exception e)
            {
                report.Error("exception: " + e.GetType().Name + ": " + e.Message);
                Debug.LogException(e);
            }

            report.Finish();
            if (exit) EditorApplication.Exit(report.Ok ? 0 : 1);
        }

        private static void Common(EditorReport report)
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = QuestBuildConfig.ProductName;
            PlayerSettings.bundleVersion = Version;
            QuestBuild.ConfigureRendering(report);
            QuestBuild.ConfigureInput(report);
            report.Pass("player: " + CompanyName + " / " + QuestBuildConfig.ProductName + " " + Version);
        }

        private static bool Compose(EditorReport report)
        {
            EditorReport compose = SceneComposer.BuildAll();
            if (compose.Ok) return true;
            report.Error("compose: scene composition failed with " + compose.Errors.Count + " error(s); see Logs/PatientZero/compose.txt.");
            return false;
        }

        // ---------------------------------------------------------------- Windows

        public static void BuildWindows(EditorReport report)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                report.Error("windows: Windows Build Support is not available in this editor.");
                return;
            }

            Common(report);
            NamedBuildTarget standalone = NamedBuildTarget.Standalone;
            PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false; // unfocused = paused, as the game's own focus rule expects
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, true);
            QuestBuild.ConfigureLoader(report, BuildTargetGroup.Standalone, initOnStart: false);
            AssetDatabase.SaveAssets();
            report.Pass("windows: Mono, x64, fullscreen window at desktop resolution (Alt+Enter toggles), XR not started");
            if (!report.Ok || !Compose(report)) return;

            string exe = Absolute(WindowsExePath);
            string folder = Path.GetDirectoryName(exe);
            if (Directory.Exists(folder)) Directory.Delete(folder, true); // a clean folder: no stale data from older builds
            Directory.CreateDirectory(folder);
            Build(report, exe, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, DesktopSimulation.DesktopMouseDefine);
        }

        // ---------------------------------------------------------------- Android phone

        public static void BuildPhone(EditorReport report)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                report.Error("android: Android Build Support is not installed for this editor.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                report.Error("android: could not switch the active build target to Android.");
                return;
            }

            var saved = new SavedOrientation();
            try
            {
                Common(report);
                NamedBuildTarget android = NamedBuildTarget.Android;
                PlayerSettings.SetApplicationIdentifier(android, PhoneApplicationId);
                PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)PhoneMinAndroidApi;
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)QuestBuildConfig.TargetAndroidApi;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                RemoveAndroidXR(report);
                AssetDatabase.SaveAssets();
                report.Pass("phone: " + PhoneApplicationId + ", IL2CPP ARM64, API " + PhoneMinAndroidApi + "-" + QuestBuildConfig.TargetAndroidApi +
                            ", Vulkan + OpenGL ES 3, landscape only, no XR loader");
                if (!report.Ok || !Compose(report)) return;

                string apk = Absolute(PhoneApkPath);
                Directory.CreateDirectory(Path.GetDirectoryName(apk));
                Build(report, apk, BuildTarget.Android, BuildTargetGroup.Android, DesktopSimulation.TouchDefine);
            }
            finally
            {
                saved.Restore();
                var restore = new EditorReport("build-phone-restore-quest");
                QuestBuild.Configure(restore);
                restore.Finish();
                if (restore.Ok) report.Pass("quest: Android settings put back to the Quest configuration");
                else report.Error("quest: putting the Quest configuration back failed; run PatientZero/Configure Quest. See Logs/PatientZero/build-phone-restore-quest.txt.");
            }
        }

        // ---------------------------------------------------------------- iPad

        public static void BuildIPad(EditorReport report)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                report.Error("ipad: BLOCKED. iOS Build Support is not installed for this editor (Unity Hub > Installs > 6000.3.27f1 > " +
                             "Add modules > iOS Build Support). An installable app also needs a Mac with Xcode and an Apple developer team.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                report.Error("ipad: could not switch the active build target to iOS.");
                return;
            }

            var saved = new SavedOrientation();
            try
            {
                Common(report);
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IPadBundleId);
                PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
                PlayerSettings.iOS.requiresFullScreen = true; // landscape only is allowed without iPad multitasking
                PlayerSettings.iOS.appleEnableAutomaticSigning = false; // the owner signs in Xcode with their own team
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                XRGeneralSettings xr = QuestBuild.SettingsFor(BuildTargetGroup.iOS, create: false);
                if (xr != null && xr.Manager != null && xr.Manager.activeLoaders.Count > 0)
                    report.Error("ipad: an XR loader is assigned for iOS; remove it in XR Plug-in Management.");
                AssetDatabase.SaveAssets();
                report.Pass("ipad: " + IPadBundleId + ", iPad only, full screen, landscape only, Metal, no XR loader, signing left to Xcode");
                if (!report.Ok || !Compose(report)) return;

                string folder = Absolute(IPadXcodePath);
                Directory.CreateDirectory(folder);
                Build(report, folder, BuildTarget.iOS, BuildTargetGroup.iOS, DesktopSimulation.TouchDefine);
            }
            finally
            {
                saved.Restore();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>No XR loader on Android for the phone build; QuestBuild.Configure assigns OpenXR again afterwards.</summary>
        private static void RemoveAndroidXR(EditorReport report)
        {
            XRGeneralSettings settings = QuestBuild.SettingsFor(BuildTargetGroup.Android, create: true);
            XRManagerSettings manager = settings.Manager;
            foreach (XRLoader loader in new System.Collections.Generic.List<XRLoader>(manager.activeLoaders))
            {
                if (loader == null) continue;
                string type = loader.GetType().FullName;
                if (XRPackageMetadataStore.RemoveLoader(manager, type, BuildTargetGroup.Android)) report.Line("phone: removed XR loader " + type);
            }

            settings.InitManagerOnStart = false;
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(manager);
            if (manager.activeLoaders.Count == 0) report.Pass("phone: no XR loader for Android");
            else report.Error("phone: an XR loader is still assigned for Android; the phone APK would start as a VR app.");
        }

        private static void Build(EditorReport report, string path, BuildTarget target, BuildTargetGroup group, string define)
        {
            var scenes = new string[QuestBuildConfig.Scenes.Count];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = QuestBuildConfig.Scenes[i];
            BuildReport build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { define },
            });

            BuildSummary summary = build.summary;
            report.Line("build: " + summary.result + ", " + summary.totalErrors + " error(s), " + summary.totalWarnings + " warning(s), " +
                        (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalTime + ", define " + define);
            if (summary.result == BuildResult.Succeeded && (File.Exists(path) || Directory.Exists(path))) report.Pass("artifact: " + path);
            else report.Error("build: " + summary.result + ". The editor log has the compiler or Gradle output.");
        }

        private static string Absolute(string projectRelative) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelative));

        /// <summary>Orientation is shared by every Android build and QuestBuild.Configure does not set it, so it is saved here.</summary>
        private sealed class SavedOrientation
        {
            private readonly UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
            private readonly bool portrait = PlayerSettings.allowedAutorotateToPortrait;
            private readonly bool upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            private readonly bool left = PlayerSettings.allowedAutorotateToLandscapeLeft;
            private readonly bool right = PlayerSettings.allowedAutorotateToLandscapeRight;

            public void Restore()
            {
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = upsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = left;
                PlayerSettings.allowedAutorotateToLandscapeRight = right;
            }
        }
    }
}
