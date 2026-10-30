// Claude-owned integration tooling (round 2), first run in Unity 6000.3.27f1 on 2026-10-30.
using System;
using PatientZero.Integration.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PatientZero.Integration.Editor.Quest
{
    /// <summary>
    /// One command from a fresh checkout to Play mode in desktop simulation, in the windowed Editor:
    /// import samples, desktop simulation on, build both scenes, open Bootstrap, enter Play mode.
    /// The windowed counterpart of `unity_batch.py first-run` for a Unity Personal licence that has no
    /// batch-mode (headless) entitlement. Sample imports recompile scripts and reload the domain, so the
    /// next step is kept in SessionState and resumed after the reload. A failed step stops the chain.
    ///
    /// Menu:          PatientZero/Set Up And Play, PatientZero/Play (scenes already built)
    /// Command line:  Unity.exe -projectPath Game -executeMethod PatientZero.Integration.Editor.Quest.PlayPipeline.SetUpAndPlay
    /// Results:       "[PZ]" lines in the Editor log, Logs/PatientZero/play.txt and the per-step reports
    /// </summary>
    [InitializeOnLoad]
    public static class PlayPipeline
    {
        private const string StepKey = "PatientZero.PlayPipeline.Step";
        private const string Samples = "samples", Desktop = "desktop", Compose = "compose", Enter = "play";

        static PlayPipeline()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(StepKey, ""))) EditorApplication.delayCall += Continue;
        }

        [MenuItem("PatientZero/Set Up And Play", priority = 3)]
        public static void SetUpAndPlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Debug.Log("[PZ] play pipeline: start");
            SessionState.SetString(StepKey, Samples);
            EditorApplication.delayCall += Continue;
        }

        [MenuItem("PatientZero/Play", priority = 2)]
        public static void Play()
        {
            var report = new EditorReport("play");
            EnterPlay(report);
            report.Finish();
        }

        private static void Continue()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Continue;
                return;
            }

            string step = SessionState.GetString(StepKey, "");
            if (step.Length == 0) return;
            Debug.Log("[PZ] play pipeline: step " + step);
            EditorReport report;
            string next;
            try
            {
                switch (step)
                {
                    case Samples:
                        next = Desktop;
                        SessionState.SetString(StepKey, next); // set before the import: it may reload the domain
                        report = PackageSamples.ImportRequired();
                        break;
                    case Desktop:
                        next = Compose;
                        SessionState.SetString(StepKey, next);
                        report = DesktopSimulation.Enable();
                        break;
                    case Compose:
                        next = Enter;
                        SessionState.SetString(StepKey, next);
                        report = SceneComposer.BuildAll();
                        break;
                    case Enter:
                        next = "";
                        report = new EditorReport("play");
                        EnterPlay(report);
                        report.Finish();
                        break;
                    default:
                        Debug.LogError("[PZ] FAIL  play pipeline: unknown step '" + step + "'");
                        SessionState.EraseString(StepKey);
                        return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PZ] FAIL  play pipeline: step " + step + " threw " + e.GetType().Name + ": " + e.Message);
                Debug.LogException(e);
                SessionState.EraseString(StepKey);
                return;
            }

            if (!report.Ok)
            {
                Debug.LogError("[PZ] FAIL  play pipeline: step " + step + " failed with " + report.Errors.Count + " error(s); stopped. See the Console.");
                SessionState.EraseString(StepKey);
                return;
            }

            if (next.Length == 0)
            {
                SessionState.EraseString(StepKey);
                return;
            }

            // A reload after this step resumes from the static constructor; without one, carry on here.
            EditorApplication.delayCall += Continue;
        }

        private static void EnterPlay(EditorReport report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { report.Pass("play: already in Play mode"); return; }
            if (!System.IO.File.Exists(QuestBuildConfig.BootstrapScenePath))
            {
                report.Error("play: " + QuestBuildConfig.BootstrapScenePath + " does not exist. Run PatientZero/Build Scenes first.");
                return;
            }

            EditorSceneManager.OpenScene(QuestBuildConfig.BootstrapScenePath, OpenSceneMode.Single);
            report.Pass("play: opened " + QuestBuildConfig.BootstrapScenePath + ", entering Play mode");
            EditorApplication.EnterPlaymode();
        }
    }
}
