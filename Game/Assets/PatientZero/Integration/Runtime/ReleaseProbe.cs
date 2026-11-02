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
        // E-21: 300 (was 240): every new run now opens on the 64 s news bulletin.
        public const float RoundTimeoutSeconds = 300, RetryAfterSeconds = 12;
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
                bool acted = false, scareShot = false, newsChecked = false;
                float actedAt = 0;
                while (flow.Core.State != GameState.Complete && Time.realtimeSinceStartup < deadline)
                {
                    // Losing window focus pauses the game by design; an unattended run resumes and says so.
                    if (flow.Core.State == GameState.Paused) { Debug.Log("[PZ-PROBE] focus pause; resuming"); flow.Resume(); }
                    if (flow.Core.CurrentAct != previous) { previous = flow.Core.CurrentAct; acted = false; }
                    if (round == 1 && shots != null && !scareShot && flow.Core.State == GameState.Ending)
                    {
                        // E-21: the exit jump scare, photographed just before the cut to black.
                        scareShot = true;
                        yield return new WaitForSeconds(.55f);
                        yield return Shot("6-exit-scare");
                    }
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
                        // A new floor starts under the stairs fade (v1.3): wait for it to clear before the photo and the first grip.
                        bool stairs = previous != ActId.Awakening && WardLayout.StartsFloor(previous);
                        yield return new WaitForSeconds(stairs ? PlayerRig.StairFadeOutSeconds + PlayerRig.StairFadeInSeconds + .75f : 1.5f);
                        // E-21: on a flat screen the new floor is reached on foot, down the stairwell, as a player must.
                        if (stairs && Services<IPlayerQuery>.Instance is PlayerRig walker && walker.walkStairs) yield return WalkDown(previous, round == 1);
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
                        if (previous == ActId.Awakening) { yield return News(!newsChecked); newsChecked = true; }
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
                if (round == 1) yield return Shot("7-results");
            }

            bool pass = completed == rounds && problems == 0;
            Debug.Log("[PZ-PROBE] RESULT " + (pass ? "PASS" : "FAIL") + ": " + completed + "/" + rounds + " round(s) complete; " + problems +
                      " error(s) or exception(s) logged" + (firstProblem == null ? "" : "; first: " + firstProblem));
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(pass ? 0 : 1);
        }

        /// <summary>
        /// E-21: a new run opens on the outbreak bulletin, which holds the player until it ends. The probe sits through it as a
        /// player must (photographing it in round 1), and fails the run if the set never held the player or never let go.
        /// </summary>
        private IEnumerator News(bool startOfRun)
        {
            NewsBroadcast[] sets = Ward<NewsBroadcast>();
            if (sets.Length != 1) { Problem("[PZ-PROBE] expected one news television in Ward, found " + sets.Length); yield break; }
            NewsBroadcast news = sets[0];
            if (!news.Holding)
            {
                // A restored Act 1 does not replay it; a new run must.
                if (startOfRun) Problem("[PZ-PROBE] the news bulletin did not hold the player at the start of the run");
                yield break;
            }

            float began = Time.time;
            bool photographed = false;
            while (news.Holding && Time.time - began < news.duration + 15)
            {
                if (flow.Core.State == GameState.Paused) flow.Resume();
                if (round == 1 && !photographed && Time.time - began > 20) { photographed = true; yield return Shot("1-news"); }
                yield return null;
            }

            if (news.Holding) Problem("[PZ-PROBE] the news bulletin still held the player after " + (Time.time - began).ToString("0") + " s");
            else Debug.Log("[PZ-PROBE] the news bulletin held the player for another " + (Time.time - began).ToString("0.0") + " s");
            yield return new WaitForSeconds(1f); // the view is handed back
        }

        private void Problem(string message)
        {
            Debug.LogWarning(message);
            problems++;
            if (firstProblem == null) firstProblem = message;
        }

        /// <summary>
        /// One act's objectives, exactly as IntegrationPlayTests.AutoplayObjectivesReachCompleteWithoutBypassingDirectorGate.
        /// v1.3 (E-19): each puzzle is worked only in its own act, and a floor's stairs are taken by standing on the landing.
        /// </summary>
        private IEnumerator Act(ActId act)
        {
            PhysicalPuzzle[] puzzles = Ward<PhysicalPuzzle>();
            switch (act)
            {
                case ActId.Awakening:
                    foreach (PhysicalPuzzle p in puzzles)
                    {
                        if (p.mechanic != PhysicalPuzzle.Mechanic.Plank || p.BoundAct != act) continue;
                        Vector3 point = p.transform.position;
                        p.BeginGrip(point);
                        p.SampleGrip(point + p.transform.TransformDirection(p.axis) * .081f, .1f);
                        p.EndGrip();
                    }
                    break;
                case ActId.Machinery:
                    Valves(puzzles, act);
                    Plug(act);
                    break;
                case ActId.Darkness:
                    MoveTo(WardLayout.A3_End.Position);
                    break;
                case ActId.Exit:
                    yield return Levers(puzzles, act);
                    Stairs(act);
                    break;
                case ActId.Laboratory:
                    Plug(act);
                    yield return Levers(puzzles, act);
                    Stairs(act);
                    break;
                case ActId.Lobby:
                    Valves(puzzles, act);
                    yield return Levers(puzzles, act);
                    break;
            }
        }

        private static void Valves(PhysicalPuzzle[] puzzles, ActId act)
        {
            foreach (PhysicalPuzzle p in puzzles)
            {
                if (p.mechanic != PhysicalPuzzle.Mechanic.Valve || p.BoundAct != act) continue;
                Vector3 center = p.transform.position;
                p.BeginGrip(center + Vector3.right * .2f);
                for (int step = 1; step <= 6; step++)
                    p.SampleGrip(center + Quaternion.AngleAxis(step * 90, p.transform.TransformDirection(p.axis)) * Vector3.right * .2f, 1);
                p.EndGrip();
            }
        }

        private static IEnumerator Levers(PhysicalPuzzle[] puzzles, ActId act)
        {
            foreach (PhysicalPuzzle p in puzzles)
            {
                if (p.mechanic != PhysicalPuzzle.Mechanic.Lever || p.BoundAct != act) continue;
                Vector3 point = p.transform.position;
                p.BeginGrip(point);
                p.SampleGrip(point - Vector3.up * .05f, .1f);
                yield return new WaitForSeconds(2.1f);
                p.EndGrip();
            }
        }

        /// <summary>Carries the act's plug or fuse into its socket.</summary>
        private static void Plug(ActId act)
        {
            foreach (CableSocket socket in Ward<CableSocket>())
            {
                if (socket.socketAct != act || !socket.plug) continue;
                socket.plug.transform.SetPositionAndRotation(socket.transform.position, socket.transform.rotation);
                if (!socket.TryConnect()) Debug.LogWarning("[PZ-PROBE] " + act + " socket did not connect");
                return;
            }
            Debug.LogError("[PZ-PROBE] no CableSocket for " + act + " in Ward");
        }

        /// <summary>Stands on the act's stair landing; StairArrival completes the objective once the stair door is open.</summary>
        /// <summary>
        /// E-21: walks the player down the stairwell to the floor <paramref name="act"/> starts on, with the desktop capsule
        /// and nothing else: through the top door, down to the fire-door lobby (where the two halves are joined), on down, and
        /// out into the stair room below. A run that does not arrive is a problem, and the player is then placed there.
        /// </summary>
        private IEnumerator WalkDown(ActId act, bool photograph)
        {
            int floor = WardLayout.FloorOf(act);
            Stairwell well = null;
            foreach (Stairwell candidate in Ward<Stairwell>())
                if (candidate.lowerFloor == floor) well = candidate;
            var rig = Services<IPlayerQuery>.Instance as PlayerRig;
            CharacterController capsule = rig && rig.origin ? rig.origin.GetComponent<CharacterController>() : null;
            if (!well || !capsule || well.doors.Length < 6)
            {
                Problem("[PZ-PROBE] no walkable stairwell down to floor " + floor);
                MoveTo(WardLayout.ActStart(act).Position);
                yield break;
            }

            // Top door, the lobby's two doors (the second is where the halves meet), the bottom door, then the act's start.
            Vector3[] route = { well.doors[0].Centre, well.doors[1].Centre, well.doors[4].Centre, well.doors[5].Centre, WardLayout.ActStart(act).Position };
            float started = Time.time;
            for (int leg = 0; leg < route.Length; leg++)
            {
                float giveUp = Time.time + 15;
                while (Time.time < giveUp)
                {
                    Vector3 to = route[leg] - rig.FloorPosition;
                    to.y = 0;
                    if (to.magnitude < .2f) break;
                    if (flow.Core.State == GameState.Paused) flow.Resume();
                    Vector3 direction = to.normalized;
                    rig.origin.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    capsule.Move(direction * Mathf.Min(1.5f * Time.deltaTime, to.magnitude) + Vector3.down * Time.deltaTime);
                    yield return null;
                }

                // In the top doorway looking down the stairs, and in the bottom doorway looking into the room below. A level view
                // from the top doorway passes over the whole flight, so that shot looks down 40 degrees, as a player would.
                if (photograph && leg == 0)
                {
                    Transform head = rig.head;
                    Quaternion level = head ? head.localRotation : Quaternion.identity;
                    if (head) head.localRotation = Quaternion.Euler(40, 0, 0);
                    yield return null;
                    yield return Shot((int)act + "-stairwell-top");
                    yield return null;
                    yield return null;
                    if (head) head.localRotation = level;
                }
                if (photograph && leg == 3) yield return Shot((int)act + "-stairwell-bottom");
            }

            Vector3 feet = rig.FloorPosition;
            bool arrived = Mathf.Abs(feet.y) < .25f && WardLayout.HorizontalDistance(feet, route[route.Length - 1]) < .5f;
            if (!arrived)
            {
                Problem("[PZ-PROBE] the stairwell down to floor " + floor + " was not walked: stopped at " + feet);
                MoveTo(WardLayout.ActStart(act).Position);
            }
            else Debug.Log("[PZ-PROBE] walked down the stairwell to floor " + floor + " in " + (Time.time - started).ToString("0.0") + " s");
        }

        private static void Stairs(ActId act)
        {
            if (WardLayout.TryStairLanding(act, out LayoutPoint landing)) MoveTo(landing.Position);
        }

        private static void MoveTo(Vector3 floor)
        {
            if (!(Services<IPlayerQuery>.Instance is PlayerRig rig) || !rig.origin) return;
            rig.origin.MoveCameraToWorldLocation(floor + Vector3.up * (rig.HeadPosition.y - rig.FloorPosition.y));
            Physics.SyncTransforms();
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
