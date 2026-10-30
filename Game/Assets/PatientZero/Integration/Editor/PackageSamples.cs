// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Imports the package samples the scene build steps use. PlayerBuildStep instantiates the XR rig from
    /// XRI's "Starter Assets"; the hand-tracking rig variant comes from "Hands Interaction Demo", whose hand
    /// meshes come from XR Hands' "HandVisualizer".
    ///
    /// Run it in its own editor session before composing: the samples contain scripts, and imported scripts
    /// compile only after the current editor call returns. The composer checks the required ones and stops
    /// before touching a scene if they are missing.
    ///
    /// Menu:      PatientZero/Import Required Samples
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.PackageSamples.ImportRequiredBatch
    /// </summary>
    public static class PackageSamples
    {
        public const string Xri = "com.unity.xr.interaction.toolkit";
        public const string XrHands = "com.unity.xr.hands";

        // TextMesh Pro essential resources ship inside com.unity.ugui. The Hands Interaction Demo sample needs them: its
        // project validation rule is an error without them, and the sample then opens Project Validation after every
        // domain reload, including every entry into Play mode (seen 2026-10-30). Imported here, not committed: they are
        // regenerated like the samples.
        public const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        public const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private static bool tmpPending;

        public readonly struct Wanted
        {
            public readonly string Package;
            public readonly string Sample;
            public readonly bool Required;

            public Wanted(string package, string sample, bool required)
            {
                Package = package;
                Sample = sample;
                Required = required;
            }
        }

        /// <summary>Import order matters: Hands Interaction Demo builds on Starter Assets.</summary>
        public static readonly Wanted[] ForScenes =
        {
            new Wanted(Xri, "Starter Assets", true),
            new Wanted(Xri, "Hands Interaction Demo", false),
            new Wanted(XrHands, "HandVisualizer", false),
        };

        [MenuItem("PatientZero/Import Required Samples", priority = 1)]
        public static void ImportFromMenu()
        {
            EditorReport report = ImportRequired();
            EditorUtility.DisplayDialog("PatientZero: Import Required Samples",
                report.Ok ? "Samples imported. Wait for scripts to compile, then run PatientZero/Build Scenes." : report.Errors.Count + " error(s). See the Console.",
                "OK");
        }

        public static void ImportRequiredBatch()
        {
            EditorReport report = ImportRequired();
            if (!tmpPending)
            {
                EditorApplication.Exit(report.Ok ? 0 : 1);
                return;
            }

            // AssetDatabase.ImportPackage runs after this call returns; wait for it before exiting.
            double deadline = EditorApplication.timeSinceStartup + 300;
            void WaitForTmp()
            {
                if (tmpPending && EditorApplication.timeSinceStartup < deadline) return;
                EditorApplication.update -= WaitForTmp;
                bool ok = File.Exists(TmpSettingsPath);
                Debug.Log("[PZ] " + (ok ? "PASS  TMP essentials: imported" : "FAIL  TMP essentials: import did not finish in 300 s"));
                EditorApplication.Exit(report.Ok && ok ? 0 : 1);
            }

            EditorApplication.update += WaitForTmp;
        }

        public static EditorReport ImportRequired()
        {
            var report = new EditorReport("import-samples");
            for (int i = 0; i < ForScenes.Length; i++)
            {
                Import(report, ForScenes[i].Package, ForScenes[i].Sample, ForScenes[i].Required);
            }

            ImportTmpEssentials(report);
            AssetDatabase.Refresh();
            report.Finish();
            return report;
        }

        /// <summary>Starts the TMP essential resources import when they are missing. The import completes asynchronously.</summary>
        public static void ImportTmpEssentials(EditorReport report)
        {
            if (File.Exists(TmpSettingsPath))
            {
                report.Pass("TMP essentials: present at " + Path.GetDirectoryName(TmpSettingsPath).Replace('\\', '/'));
                return;
            }

            string package = Path.GetFullPath(TmpEssentialsPackage);
            if (!File.Exists(package))
            {
                report.Warn("TMP essentials: " + TmpEssentialsPackage + " not found. Import them with Window > TextMeshPro > Import TMP Essential Resources.");
                return;
            }

            tmpPending = true;
            AssetDatabase.importPackageCompleted += TmpDone;
            AssetDatabase.importPackageFailed += TmpFailed;
            AssetDatabase.importPackageCancelled += TmpDone;
            AssetDatabase.ImportPackage(package, false);
            report.Pass("TMP essentials: import started from " + TmpEssentialsPackage);
        }

        private static void TmpDone(string packageName) => TmpFinished();

        private static void TmpFailed(string packageName, string error)
        {
            Debug.LogError("[PZ] FAIL  TMP essentials: " + error);
            TmpFinished();
        }

        private static void TmpFinished()
        {
            tmpPending = false;
            AssetDatabase.importPackageCompleted -= TmpDone;
            AssetDatabase.importPackageFailed -= TmpFailed;
            AssetDatabase.importPackageCancelled -= TmpDone;
        }

        /// <summary>Imports one sample if it is not imported yet. A missing required sample is an error, an optional one a warning.</summary>
        public static void Import(EditorReport report, string package, string sampleName, bool required)
        {
            Sample? sample = Find(report, package, sampleName, required);
            if (sample == null) return;
            Sample s = sample.Value;
            if (s.isImported)
            {
                report.Pass("sample '" + sampleName + "' (" + package + ") already imported at " + s.importPath);
            }
            else if (s.Import(Sample.ImportOptions.HideImportWindow))
            {
                report.Pass("sample '" + sampleName + "' (" + package + ") imported to " + s.importPath);
            }
            else
            {
                Missing(report, required, "importing sample '" + sampleName + "' (" + package + ") failed.");
            }
        }

        /// <summary>For the composer: true when every required sample is imported. Imports nothing.</summary>
        public static bool RequiredAreImported(EditorReport report)
        {
            bool ok = true;
            for (int i = 0; i < ForScenes.Length; i++)
            {
                Wanted w = ForScenes[i];
                if (!w.Required) continue;
                Sample? sample = Find(report, w.Package, w.Sample, true);
                if (sample == null)
                {
                    ok = false;
                }
                else if (!sample.Value.isImported)
                {
                    report.Error("samples: '" + w.Sample + "' (" + w.Package + ") is not imported. Run `python tools/unity/unity_batch.py import-samples` " +
                                 "or PatientZero/Import Required Samples, let scripts compile, then build scenes.");
                    ok = false;
                }
                else
                {
                    report.Pass("samples: '" + w.Sample + "' imported at " + sample.Value.importPath);
                }
            }

            return ok;
        }

        private static Sample? Find(EditorReport report, string package, string sampleName, bool required)
        {
            string version = null;
            foreach (PackageInfo info in PackageInfo.GetAllRegisteredPackages())
            {
                if (info.name == package) version = info.version;
            }

            if (version == null)
            {
                Missing(report, required, "samples: package " + package + " is not installed.");
                return null;
            }

            var names = new List<string>();
            foreach (Sample sample in Sample.FindByPackage(package, version))
            {
                if (string.Equals(sample.displayName, sampleName, StringComparison.Ordinal)) return sample;
                names.Add(sample.displayName);
            }

            Missing(report, required, "samples: " + package + " " + version + " has no sample named '" + sampleName + "'. It has: " +
                                      (names.Count == 0 ? "none" : string.Join(", ", names)) + ".");
            return null;
        }

        private static void Missing(EditorReport report, bool required, string message)
        {
            if (required) report.Error(message);
            else report.Warn(message);
        }
    }
}
