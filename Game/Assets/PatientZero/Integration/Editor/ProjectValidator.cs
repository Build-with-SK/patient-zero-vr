// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
using System;
using System.Collections.Generic;
using System.IO;
using PatientZero.Integration.Logic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Engine-side project checks that need no XR package: editor version, resolved packages against
    /// tools/unity/package_policy.json, forbidden SDKs (including transitive ones), render pipeline and colour space.
    /// The Quest assembly adds the XR and Android checks on top (QuestBuild.ValidateBatch).
    ///
    /// Menu:      PatientZero/Validate Project (core)
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.ProjectValidator.ValidateBatch
    /// </summary>
    public static class ProjectValidator
    {
        private const string UrpAssetType = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";

        [Serializable]
        private sealed class Pin
        {
            public string name;
            public string version;
        }

        [Serializable]
        private sealed class Policy
        {
            public string editor;
            public Pin[] required;
            public string[] forbiddenPrefixes;
        }

        [MenuItem("PatientZero/Validate Project (core)", priority = 20)]
        public static void ValidateFromMenu()
        {
            var report = new EditorReport("validate-core");
            Check(report);
            report.Finish();
        }

        public static void ValidateBatch()
        {
            var report = new EditorReport("validate-core");
            Check(report);
            report.Finish();
            EditorApplication.Exit(report.Ok ? 0 : 1);
        }

        public static void Check(EditorReport report)
        {
            CheckEditor(report);
            CheckPackages(report);
            CheckRendering(report);
        }

        private static void CheckEditor(EditorReport report)
        {
            string version = Application.unityVersion;
            if (version == QuestBuildConfig.EditorVersion) report.Pass("editor " + version + " (pinned)");
            else if (QuestBuildConfig.IsSupportedEditor(version))
                report.Warn("editor " + version + " is Unity 6.3 but not the pinned " + QuestBuildConfig.EditorVersion + "; record any upgrade in ARCHITECTURE.md section 1.");
            else report.Error("editor " + version + " is not Unity 6.3 LTS (pinned " + QuestBuildConfig.EditorVersion + ").");
        }

        private static void CheckPackages(EditorReport report)
        {
            Policy policy = LoadPolicy(report);
            var resolved = new Dictionary<string, PackageInfo>(StringComparer.Ordinal);
            foreach (PackageInfo info in PackageInfo.GetAllRegisteredPackages()) resolved[info.name] = info;
            report.Line("resolved packages: " + resolved.Count);

            if (policy != null && policy.required != null)
            {
                foreach (Pin pin in policy.required)
                {
                    if (!resolved.TryGetValue(pin.name, out PackageInfo info))
                        report.Error("package-missing: " + pin.name + " " + pin.version + " is not installed.");
                    else if (info.version != pin.version)
                        report.Error("package-version: " + pin.name + " resolved to " + info.version + ", policy pins " + pin.version + ".");
                    else
                        report.Pass("package " + pin.name + " " + info.version);
                }
            }

            foreach (PackageInfo info in resolved.Values)
            {
                if (QuestBuildConfig.IsForbiddenPackage(info.name))
                    report.Error("package-forbidden: " + info.name + " " + info.version + " is installed" +
                                 (info.isDirectDependency ? "" : " (pulled in by another package)") + ". The project uses Unity OpenXR, not the Oculus/Meta integration SDK.");

                if (info.source == PackageSource.Local || info.source == PackageSource.LocalTarball || info.source == PackageSource.Git)
                    report.Warn("package-source: " + info.name + " comes from " + info.source + " (" + info.resolvedPath + "); CI and other seats cannot reproduce it.");
            }
        }

        private static Policy LoadPolicy(EditorReport report)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", "unity", "package_policy.json"));
            if (!File.Exists(path))
            {
                report.Error("policy: " + path + " not found; package pins cannot be checked.");
                return null;
            }

            var policy = JsonUtility.FromJson<Policy>(File.ReadAllText(path));
            if (policy == null || policy.required == null || policy.required.Length == 0)
            {
                report.Error("policy: " + path + " has no required packages.");
                return null;
            }

            if (policy.editor != QuestBuildConfig.EditorVersion)
                report.Error("policy: package_policy.json pins editor " + policy.editor + " but QuestBuildConfig pins " + QuestBuildConfig.EditorVersion + ".");
            return policy;
        }

        private static void CheckRendering(EditorReport report)
        {
            RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline == null) report.Error("render: no render pipeline asset is set (built-in renderer). Run PatientZero/Configure Quest.");
            else if (pipeline.GetType().FullName != UrpAssetType) report.Error("render: default pipeline is " + pipeline.GetType().FullName + ", not URP.");
            else report.Pass("render: URP asset " + AssetDatabase.GetAssetPath(pipeline));

            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                RenderPipelineAsset level = QualitySettings.GetRenderPipelineAssetAt(i);
                if (level != null && level != pipeline)
                    report.Warn("render: quality level '" + QualitySettings.names[i] + "' overrides the pipeline with " + AssetDatabase.GetAssetPath(level) + ".");
            }

            if (PlayerSettings.colorSpace == ColorSpace.Linear) report.Pass("colour space: Linear");
            else report.Error("colour space is " + PlayerSettings.colorSpace + "; the contract requires Linear.");
        }
    }
}
