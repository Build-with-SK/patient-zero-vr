// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
// APIs whose exact shape could not be checked without the packages (OpenXR project validation, feature refresh)
// are called by reflection, so a mismatch is reported as a validation warning instead of breaking the build script.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PatientZero.Integration.Logic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace PatientZero.Integration.Editor.Quest
{
    /// <summary>
    /// Puts the project into the Quest configuration ARCHITECTURE.md section 1 specifies, checks it, and builds the APK.
    ///
    /// Menu:      PatientZero/Configure Quest, PatientZero/Validate Project (Quest), PatientZero/Build Dev APK
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.Quest.QuestBuild.ConfigureBatch
    ///            -executeMethod PatientZero.Integration.Editor.Quest.QuestBuild.ValidateBatch
    ///            -executeMethod PatientZero.Integration.Editor.Quest.QuestBuild.BuildDevApkBatch
    /// tools/unity/unity_batch.py wraps these with the pinned editor and reads the [PZ] lines back.
    /// </summary>
    public static class QuestBuild
    {
        public const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
        public const string XRSettingsAsset = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        public const string UrpFolder = "Assets/PatientZero/Integration/Settings";
        public const string UrpAssetPath = UrpFolder + "/PZ_URP.asset";
        public const string UrpRendererPath = UrpFolder + "/PZ_URP_Renderer.asset";
        private const string CompanyName = "Build with SK";
        private const int Msaa = 4;

        [MenuItem("PatientZero/Configure Quest", priority = 40)]
        public static void ConfigureFromMenu() => Run("configure", Configure, exit: false);

        [MenuItem("PatientZero/Validate Project (Quest)", priority = 21)]
        public static void ValidateFromMenu() => Run("validate", Validate, exit: false);

        [MenuItem("PatientZero/Build Dev APK", priority = 60)]
        public static void BuildFromMenu() => Run("build-apk", BuildDevApk, exit: false);

        public static void ConfigureBatch() => Run("configure", Configure, exit: true);
        public static void ValidateBatch() => Run("validate", Validate, exit: true);
        public static void BuildDevApkBatch() => Run("build-apk", BuildDevApk, exit: true);

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

        // ---------------------------------------------------------------- configure

        public static void Configure(EditorReport report)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                report.Error("android: Android Build Support is not installed for this editor. Add it in Unity Hub (with OpenJDK and SDK/NDK tools).");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                report.Error("android: could not switch the active build target to Android.");
                return;
            }

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = QuestBuildConfig.ProductName;
            PlayerSettings.SetApplicationIdentifier(android, QuestBuildConfig.ApplicationId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)QuestBuildConfig.MinAndroidApi;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)QuestBuildConfig.TargetAndroidApi;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            report.Pass("player settings: " + QuestBuildConfig.ApplicationId + ", IL2CPP, ARM64, API " +
                        QuestBuildConfig.MinAndroidApi + "-" + QuestBuildConfig.TargetAndroidApi + ", Vulkan");

            // Desktop keyboard and mouse adapters are Standalone-only (DesktopSimulation); never ship them to the headset.
            PlayerSettings.GetScriptingDefineSymbols(android, out string[] androidDefines);
            if (Array.IndexOf(androidDefines, DesktopSimulation.DesktopMouseDefine) >= 0)
            {
                var kept = new List<string>(androidDefines);
                kept.Remove(DesktopSimulation.DesktopMouseDefine);
                PlayerSettings.SetScriptingDefineSymbols(android, kept.ToArray());
                report.Pass("android defines: removed " + DesktopSimulation.DesktopMouseDefine);
            }

            ConfigureRendering(report);
            ConfigureInput(report);
            ConfigureLoader(report, BuildTargetGroup.Android, initOnStart: true);
            ConfigureOpenXR(report);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Player > Active Input Handling, as stored in ProjectSettings.asset (no public API): 1 = Input System only.</summary>
        public const int InputSystemOnly = 1;

        /// <summary>
        /// Project-wide: Active Input Handling = Input System only. "Both" is unsupported on Android (Unity's build warning,
        /// 2026-10-30), and nothing in the project or its samples reads UnityEngine.Input; XRI and the desktop adapters use the
        /// Input System. The change takes effect at the next editor start. Also used by desktop simulation.
        /// </summary>
        public static void ConfigureInput(EditorReport report)
        {
            SerializedProperty handler = InputHandler(out SerializedObject settings);
            if (handler == null)
            {
                report.Warn("input handling: 'activeInputHandler' not found in ProjectSettings. Set Player > Active Input Handling to Input System Package (New) by hand.");
                return;
            }

            if (handler.intValue == InputSystemOnly)
            {
                report.Pass("input handling: Input System only");
                return;
            }

            handler.intValue = InputSystemOnly;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            report.Pass("input handling: set to Input System only (takes effect at the next editor start)");
        }

        private static SerializedProperty InputHandler(out SerializedObject settings)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            settings = assets.Length > 0 && assets[0] != null ? new SerializedObject(assets[0]) : null;
            return settings?.FindProperty("activeInputHandler");
        }

        /// <summary>Project-wide rendering for every platform: Linear colour space and the URP asset. Also used by desktop simulation.</summary>
        public static void ConfigureRendering(EditorReport report)
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
            report.Pass("colour space: Linear");
            ConfigureUrp(report);
        }

        private static void ConfigureUrp(EditorReport report)
        {
            var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null) asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (asset == null)
            {
                Directory.CreateDirectory(Absolute(UrpFolder));
                AssetDatabase.Refresh();
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, UrpRendererPath);
                asset = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(asset, UrpAssetPath);
                report.Line("created " + UrpAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = asset;
            asset.msaaSampleCount = Msaa;
            asset.supportsHDR = false;
            asset.renderScale = 1f;
            asset.supportsCameraDepthTexture = false;
            asset.supportsCameraOpaqueTexture = false;
            asset.useSRPBatcher = true;
            EditorUtility.SetDirty(asset);
            report.Pass("URP: " + AssetDatabase.GetAssetPath(asset) + ", MSAA " + Msaa + "x, HDR off, render scale 1, no depth/opaque texture");
        }

        /// <summary>Assigns the OpenXR loader as the only loader for a platform. Also used by desktop simulation.</summary>
        public static XRGeneralSettings ConfigureLoader(EditorReport report, BuildTargetGroup group, bool initOnStart)
        {
            XRGeneralSettings settings = SettingsFor(group, create: true);
            XRManagerSettings manager = settings.Manager;
            foreach (XRLoader loader in new List<XRLoader>(manager.activeLoaders))
            {
                string type = loader.GetType().FullName;
                if (type != OpenXRLoaderType && XRPackageMetadataStore.RemoveLoader(manager, type, group))
                    report.Line("removed loader " + type + " (" + group + ")");
            }

            if (!HasOnlyOpenXR(manager) && !XRPackageMetadataStore.AssignLoader(manager, OpenXRLoaderType, group))
            {
                report.Error("xr-loader: could not assign the OpenXR loader for " + group + ". Is com.unity.xr.openxr installed?");
                return settings;
            }

            settings.InitManagerOnStart = initOnStart;
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(manager);
            report.Pass("xr-loader: OpenXR only for " + group + ", initialise on start = " + initOnStart);
            return settings;
        }

        private static void ConfigureOpenXR(EditorReport report)
        {
            TryInvokeStatic(report, "UnityEditor.XR.OpenXR.Features.FeatureHelpers", "RefreshFeatures", BuildTargetGroup.Android);
            OpenXRSettings openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openxr == null)
            {
                report.Error("openxr: no OpenXR settings for Android. Assign the loader first.");
                return;
            }

            openxr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(openxr);
            OpenXRFeature[] features = openxr.GetFeatures();
            foreach (string typeName in QuestBuildConfig.RequiredOpenXRFeatures)
            {
                OpenXRFeature feature = FindFeature(features, typeName);
                if (feature == null)
                {
                    report.Error("openxr-feature: " + typeName + " is not available for Android. Check com.unity.xr.openxr and com.unity.xr.hands.");
                    continue;
                }

                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                report.Pass("openxr-feature: " + Short(typeName) + " enabled");
            }
        }

        // ---------------------------------------------------------------- validate

        public static void Validate(EditorReport report)
        {
            ProjectValidator.Check(report);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                report.Error("android: Android Build Support is not installed for this editor.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                report.Warn("android: the active build target is " + EditorUserBuildSettings.activeBuildTarget + "; the build switches it.");

            NamedBuildTarget android = NamedBuildTarget.Android;
            Expect(report, "application id", PlayerSettings.GetApplicationIdentifier(android), QuestBuildConfig.ApplicationId);
            PlayerSettings.GetScriptingDefineSymbols(android, out string[] androidDefines);
            if (Array.IndexOf(androidDefines, DesktopSimulation.DesktopMouseDefine) >= 0)
                report.Error("android defines: " + DesktopSimulation.DesktopMouseDefine + " is set for Android; desktop adapters must not ship to Quest. Run configure.");
            else
                report.Pass("android defines: no " + DesktopSimulation.DesktopMouseDefine);
            SerializedProperty inputHandler = InputHandler(out _);
            if (inputHandler == null) report.Warn("input handling: 'activeInputHandler' not found in ProjectSettings; check Player > Active Input Handling by hand.");
            else if (inputHandler.intValue == InputSystemOnly) report.Pass("input handling: Input System only");
            else report.Error("input handling: Active Input Handling is " + (inputHandler.intValue == 2 ? "Both" : "Input Manager (Old)") +
                              "; Android needs Input System only. Run configure.");
            Expect(report, "scripting backend", PlayerSettings.GetScriptingBackend(android).ToString(), ScriptingImplementation.IL2CPP.ToString());
            Expect(report, "architectures", PlayerSettings.Android.targetArchitectures.ToString(), AndroidArchitecture.ARM64.ToString());
            Expect(report, "min API", ((int)PlayerSettings.Android.minSdkVersion).ToString(), QuestBuildConfig.MinAndroidApi.ToString());
            Expect(report, "target API", ((int)PlayerSettings.Android.targetSdkVersion).ToString(), QuestBuildConfig.TargetAndroidApi.ToString());
            GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            bool vulkanOnly = !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) && apis.Length == 1 && apis[0] == GraphicsDeviceType.Vulkan;
            if (vulkanOnly) report.Pass("graphics API: Vulkan only");
            else report.Error("graphics API: expected Vulkan only, found " + string.Join(", ", apis));

            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                Expect(report, "URP MSAA", urp.msaaSampleCount.ToString(), Msaa.ToString());
                Expect(report, "URP HDR", urp.supportsHDR.ToString(), false.ToString());
            }

            XRGeneralSettings settings = SettingsFor(BuildTargetGroup.Android, create: false);
            if (settings == null || settings.Manager == null) report.Error("xr-loader: no XR Plug-in Management settings for Android.");
            else if (!HasOnlyOpenXR(settings.Manager)) report.Error("xr-loader: Android must use the OpenXR loader and nothing else, found " + LoaderNames(settings.Manager) + ".");
            else if (!settings.InitManagerOnStart) report.Error("xr-loader: Android 'Initialize XR on Startup' is off.");
            else report.Pass("xr-loader: OpenXR only for Android");

            OpenXRSettings openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openxr == null)
            {
                report.Error("openxr: no OpenXR settings for Android.");
                return;
            }

            if (openxr.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced) report.Pass("openxr render mode: single pass instanced (multiview)");
            else report.Error("openxr render mode is " + openxr.renderMode + "; the contract requires multiview.");

            OpenXRFeature[] features = openxr.GetFeatures();
            foreach (string typeName in QuestBuildConfig.RequiredOpenXRFeatures)
            {
                OpenXRFeature feature = FindFeature(features, typeName);
                if (feature == null) report.Error("openxr-feature: " + typeName + " is not available.");
                else if (!feature.enabled) report.Error("openxr-feature: " + Short(typeName) + " is disabled.");
                else report.Pass("openxr-feature: " + Short(typeName));
            }

            ReportOpenXRValidation(report);
        }

        /// <summary>Unity's own OpenXR project validation (the list in Project Settings > XR Plug-in Management > Project Validation).</summary>
        private static void ReportOpenXRValidation(EditorReport report)
        {
            Type validation = FindType("UnityEditor.XR.OpenXR.OpenXRProjectValidation");
            MethodInfo method = validation?.GetMethod("GetCurrentValidationIssues", BindingFlags.Public | BindingFlags.Static);
            ParameterInfo[] parameters = method?.GetParameters();
            if (parameters == null || parameters.Length != 2 || !parameters[0].ParameterType.IsGenericType)
            {
                report.Warn("openxr-validation: OpenXRProjectValidation.GetCurrentValidationIssues not found; check Project Validation by hand.");
                return;
            }

            var issues = (IList)Activator.CreateInstance(parameters[0].ParameterType);
            method.Invoke(null, new object[] { issues, BuildTargetGroup.Android });
            int errors = 0;
            foreach (object issue in issues)
            {
                string message = Member(issue, "message") as string ?? issue.ToString();
                bool isError = Member(issue, "error") is bool b && b;
                if (isError)
                {
                    errors++;
                    report.Error("openxr-validation: " + message);
                }
                else
                {
                    report.Warn("openxr-validation: " + message);
                }
            }

            if (issues.Count == 0) report.Pass("openxr-validation: no issues");
            else report.Line("openxr-validation: " + issues.Count + " issue(s), " + errors + " error(s)");
        }

        // ---------------------------------------------------------------- build

        public static void BuildDevApk(EditorReport report)
        {
            Configure(report);
            if (!report.Ok) return;

            EditorReport compose = SceneComposer.BuildAll();
            if (!compose.Ok)
            {
                report.Error("compose: scene composition failed with " + compose.Errors.Count + " error(s); see Logs/PatientZero/compose.txt.");
                return;
            }

            Validate(report);
            if (!report.Ok) return;

            string apk = Path.GetFullPath(Path.Combine(Application.dataPath, "..", QuestBuildConfig.DevApkPath));
            Directory.CreateDirectory(Path.GetDirectoryName(apk));
            var scenes = new string[QuestBuildConfig.Scenes.Count];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = QuestBuildConfig.Scenes[i];

            BuildReport build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            });

            BuildSummary summary = build.summary;
            report.Line("build: " + summary.result + ", " + summary.totalErrors + " error(s), " + summary.totalWarnings +
                        " warning(s), " + (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalTime);
            if (summary.result == BuildResult.Succeeded) report.Pass("apk: " + apk);
            else report.Error("build: " + summary.result + ". The editor log has the compiler or Gradle output.");
        }

        // ---------------------------------------------------------------- helpers

        public static XRGeneralSettings SettingsFor(BuildTargetGroup group, bool create)
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
            {
                if (!create) return null;
                Directory.CreateDirectory(Absolute(Path.GetDirectoryName(XRSettingsAsset)));
                AssetDatabase.Refresh();
                perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XRSettingsAsset);
                if (perTarget == null)
                {
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(perTarget, XRSettingsAsset);
                }

                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }

            XRGeneralSettings settings = perTarget.SettingsForBuildTarget(group);
            if (settings == null)
            {
                if (!create) return null;
                settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                settings.name = group + " Settings";
                perTarget.SetSettingsForBuildTarget(group, settings);
                AssetDatabase.AddObjectToAsset(settings, perTarget);
            }

            if (settings.Manager == null && create)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = group + " Providers";
                settings.Manager = manager;
                AssetDatabase.AddObjectToAsset(manager, perTarget);
            }

            EditorUtility.SetDirty(perTarget);
            return settings;
        }

        private static bool HasOnlyOpenXR(XRManagerSettings manager)
        {
            IReadOnlyList<XRLoader> loaders = manager.activeLoaders;
            return loaders.Count == 1 && loaders[0] != null && loaders[0].GetType().FullName == OpenXRLoaderType;
        }

        private static string LoaderNames(XRManagerSettings manager)
        {
            var names = new List<string>();
            foreach (XRLoader loader in manager.activeLoaders) names.Add(loader == null ? "(missing)" : loader.GetType().FullName);
            return names.Count == 0 ? "none" : string.Join(", ", names);
        }

        private static OpenXRFeature FindFeature(OpenXRFeature[] features, string typeName)
        {
            foreach (OpenXRFeature feature in features)
            {
                if (feature != null && feature.GetType().FullName == typeName) return feature;
            }

            return null;
        }

        private static void Expect(EditorReport report, string what, string actual, string expected)
        {
            if (actual == expected) report.Pass(what + ": " + actual);
            else report.Error(what + ": expected " + expected + ", found " + actual + ". Run PatientZero/Configure Quest.");
        }

        private static void TryInvokeStatic(EditorReport report, string typeName, string methodName, params object[] args)
        {
            MethodInfo method = FindType(typeName)?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                report.Warn("reflection: " + typeName + "." + methodName + " not found; continuing.");
                return;
            }

            method.Invoke(null, args);
        }

        public static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }

            return null;
        }

        private static object Member(object target, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;
            Type type = target.GetType();
            FieldInfo field = type.GetField(name, flags);
            if (field != null) return field.GetValue(target);
            PropertyInfo property = type.GetProperty(name, flags);
            return property != null ? property.GetValue(target) : null;
        }

        private static string Short(string typeName)
        {
            int dot = typeName.LastIndexOf('.');
            return dot >= 0 ? typeName.Substring(dot + 1) : typeName;
        }

        private static string Absolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
