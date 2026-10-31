// Claude-owned release QA (owner final-release mission, 2026-10-30).
using System;
using System.Collections;
using System.Collections.Generic;
using PatientZero.Core;
using PatientZero.Flow;
using PatientZero.Interaction;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatientZero.Integration
{
    /// <summary>
    /// Release QA on a built game, not the Editor: `PatientZero.exe -pz-autoplay 3` starts a new game from the main menu and
    /// plays 3 full rounds with the PlayMode autoplay test's own objective steps (planks, valve, cable, the Act 3 walk, the
    /// lever), using Play again between rounds, then quits: exit code 0 when every round reached Complete and nothing logged
    /// an error or exception, 1 otherwise. "[PZ-PROBE]" lines in Player.log carry the per-round times and the result.
    /// Without the argument it disables itself on the first frame and does nothing. Added to Bootstrap by
    /// ReleaseProbeStep (Integration, order 950).
    /// </summary>
    public sealed class ReleaseProbe : MonoBehaviour
    {
        public const string Argument = "-pz-autoplay";
        /// <summary>`-pz-shots &lt;folder&gt;`: also save a screenshot of the menu, each act and the results, for visual review.</summary>
        public const string ShotsArgument = "-pz-shots";
        public const float RoundTimeoutSeconds = 240, RetryAfterSeconds = 12;
        public GameFlow flow;
        private int rounds, problems, round;
        private string firstProblem, shots;

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            rounds = RequestedRounds(args);
            if (rounds <= 0 || !flow) { enabled = false; return; }
            shots = Value(args, ShotsArgument);
            if (shots != null) System.IO.Directory.CreateDirectory(shots);
            Application.runInBackground = true; // an unattended run keeps going when another window takes focus
            Application.logMessageReceived += Logged;
            StartCoroutine(Run());
        }

        private void OnDestroy() { Application.logMessageReceived -= Logged; }

        /// <summary>0 without the argument; the number after it (1 when missing), clamped to 1..50.</summary>
        public static int RequestedRounds(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], Argument, StringComparison.OrdinalIgnoreCase)) continue;
                return i + 1 < args.Length && int.TryParse(args[i + 1], out int n) ? Mathf.Clamp(n, 1, 50) : 1;
            }

            return 0;
        }

        private static string Value(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private IEnumerator Shot(string name)
        {
            if (shots == null) yield break;
            yield return new WaitForEndOfFrame();
            string path = System.IO.Path.Combine(shots, "r" + round + "-" + name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[PZ-PROBE] screenshot " + path);
        }

        private void Logged(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.StartsWith("[PZ-PROBE]", StringComparison.Ordinal)) return;
            problems++;
            if (firstProblem == null) firstProblem = type + ": " + message;
        }

        private IEnumerator Run()
        {
            Debug.Log("[PZ-PROBE] start: " + rounds + " round(s); " + Application.platform + "; version " + Application.version + "; " +
                      Screen.width + "x" + Screen.height);
            float boot = Time.realtimeSinceStartup + 30;
            while (flow.Core.State == GameState.Boot && Time.realtimeSinceStartup < boot) yield return null;
            Debug.Log("[PZ-PROBE] first screen: " + flow.Core.State);
            round = 1;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("0-menu");

            int completed = 0;
            for (round = 1; round <= rounds; round++)
            {
                float start = Time.realtimeSinceStartup, deadline = start + RoundTimeoutSeconds;
                if (flow.Core.State == GameState.Calibration) flow.ConfirmCalibration();       // main menu: New game
                else if (flow.Core.State == GameState.Complete) flow.RestartRun();             // results: Play again
                while (flow.Core.State != GameState.Playing && Time.realtimeSinceStartup < deadline) yield return null;

                ActId previous = ActId.None;
                bool acted = false;
                float actedAt = 0;
                while (flow.Core.State != GameState.Complete && Time.realtimeSinceStartup < deadline)
                {
                    // Losing window focus pauses the game by design; an unattended run resumes and says so.
                    if (flow.Core.State == GameState.Paused) { Debug.Log("[PZ-PROBE] focus pause; resuming"); flow.Resume(); }
                    if (flow.Core.CurrentAct != previous) { previous = flow.Core.CurrentAct; acted = false; }
                    bool ready = flow.Core.State == GameState.Playing && !flow.Core.IsTransitioning;
                    if (ready && acted && Time.realtimeSinceStartup - actedAt > RetryAfterSeconds)
                    {
                        // An interrupted grip (pause, focus loss) leaves the act where it was; doing it again tells an
                        // interruption from a real fault, and the retry is logged so the report shows it.
                        Debug.Log("[PZ-PROBE] act " + previous + " did not advance in " + RetryAfterSeconds + " s; doing its objectives again");
                        acted = false;
                    }

                    if (!acted && ready)
                    {
                        acted = true;
                        yield return new WaitForSeconds(1.5f);
                        if (round == 1) yield return Shot((int)previous + "-" + previous);
                        if (round == 1 && previous == ActId.Awakening && shots != null)
                        {
                            // The pause menu, photographed too: menus are checked by eye, not only by state.
                            flow.Pause();
                            yield return new WaitForSecondsRealtime(1f);
                            yield return Shot("1-pause");
                            flow.Resume();
                            yield return new WaitForSecondsRealtime(.5f);
                        }
                        yield return Act(previous);
                        actedAt = Time.realtimeSinceStartup;
                        if (round == 1 && previous == ActId.Darkness) yield return Shot("3-darkness-end");
                    }

                    yield return null;
                }

                bool ok = flow.Core.State == GameState.Complete;
                Debug.Log("[PZ-PROBE] round " + round + ": " + (ok ? "COMPLETE" : "NOT COMPLETE, stopped in " + flow.Core.CurrentAct + " / " + flow.Core.State) +
                          " after " + (Time.realtimeSinceStartup - start).ToString("0.0") + " s");
                if (!ok) break;
                completed++;
                yield return new WaitForSecondsRealtime(2f); // the results screen
                if (round == 1) yield return Shot("5-results");
            }

            bool pass = completed == rounds && problems == 0;
            Debug.Log("[PZ-PROBE] RESULT " + (pass ? "PASS" : "FAIL") + ": " + completed + "/" + rounds + " round(s) complete; " + problems +
                      " error(s) or exception(s) logged" + (firstProblem == null ? "" : "; first: " + firstProblem));
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(pass ? 0 : 1);
        }

        /// <summary>One act's objectives, exactly as IntegrationPlayTests.AutoplayObjectivesReachCompleteWithoutBypassingDirectorGate.</summary>
        private IEnumerator Act(ActId act)
        {
            PhysicalPuzzle[] puzzles = Ward<PhysicalPuzzle>();
            switch (act)
            {
                case ActId.Awakening:
                    foreach (PhysicalPuzzle p in puzzles)
                    {
                        if (p.mechanic != PhysicalPuzzle.Mechanic.Plank) continue;
                        Vector3 point = p.transform.position;
                        p.BeginGrip(point);
                        p.SampleGrip(point + p.transform.TransformDirection(p.axis) * .081f, .1f);
                        p.EndGrip();
                    }
                    break;
                case ActId.Machinery:
                    foreach (PhysicalPuzzle p in puzzles)
                    {
                        if (p.mechanic != PhysicalPuzzle.Mechanic.Valve) continue;
                        Vector3 center = p.transform.position;
                        p.BeginGrip(center + Vector3.right * .2f);
                        for (int step = 1; step <= 6; step++)
                            p.SampleGrip(center + Quaternion.AngleAxis(step * 90, p.transform.TransformDirection(p.axis)) * Vector3.right * .2f, 1);
                        p.EndGrip();
                    }

                    CableSocket[] sockets = Ward<CableSocket>();
                    if (sockets.Length == 0) { Debug.LogError("[PZ-PROBE] no CableSocket in Ward"); break; }
                    sockets[0].plug.transform.SetPositionAndRotation(sockets[0].transform.position, sockets[0].transform.rotation);
                    if (!sockets[0].TryConnect()) Debug.LogWarning("[PZ-PROBE] cable did not connect");
                    break;
                case ActId.Darkness:
                    if (Services<IPlayerQuery>.Instance is PlayerRig rig && rig.origin)
                        rig.origin.MoveCameraToWorldLocation(WardLayout.A3_End.Position + Vector3.up * (rig.HeadPosition.y - rig.FloorPosition.y));
                    break;
                case ActId.Exit:
                    foreach (PhysicalPuzzle p in puzzles)
                    {
                        if (p.mechanic != PhysicalPuzzle.Mechanic.Lever) continue;
                        Vector3 point = p.transform.position;
                        p.BeginGrip(point);
                        p.SampleGrip(point - Vector3.up * .05f, .1f);
                        yield return new WaitForSeconds(2.1f);
                        p.EndGrip();
                    }
                    break;
            }
        }

        private static T[] Ward<T>() where T : Component
        {
            Scene ward = SceneManager.GetSceneByName("Ward");
            var all = new List<T>();
            if (!ward.IsValid() || !ward.isLoaded) return all.ToArray();
            foreach (GameObject root in ward.GetRootGameObjects()) all.AddRange(root.GetComponentsInChildren<T>(true));
            return all.ToArray();
        }
    }
}
