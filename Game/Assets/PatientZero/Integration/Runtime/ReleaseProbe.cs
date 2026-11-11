// Claude-owned release QA (owner final-release mission, 2026-10-30).
using System;
using System.Collections;
using System.Collections.Generic;
using PatientZero.Core;
using PatientZero.Flow;
using PatientZero.Flow.Logic;
using PatientZero.Integration.Logic;
using PatientZero.Interaction;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatientZero.Integration
{
    /// <summary>
    /// Release QA on a built game, not the Editor: `PatientZero.exe -pz-autoplay 3` starts a new game from the main menu and
    /// plays 3 full rounds with the PlayMode autoplay test's own objective steps (the door bar, valve, cable, the Act 3 walk, the
    /// lever), using Play again between rounds, then quits: exit code 0 when every round reached Complete and nothing logged
    /// an error or exception, 1 otherwise. "[PZ-PROBE]" lines in Player.log carry the per-round times and the result.
    /// Without the argument it disables itself on the first frame and does nothing. Added to Bootstrap by
    /// ReleaseProbeStep (Integration, order 950).
    /// E-22 (two-minute version): in Act 1 the probe stands at the room door, pushes the bar and meets the door zombie. In
    /// round 1 it stands still (the zombie must lose it and leave); in round 2 it keeps stepping until grabbed and wins the
    /// struggle; in round 3 it is grabbed, loses the struggle and must wake in the bed again before it stands still.
    /// </summary>
    public sealed class ReleaseProbe : MonoBehaviour
    {
        public const string Argument = "-pz-autoplay";
        /// <summary>`-pz-shots &lt;folder&gt;`: also save a screenshot of the menu, each act and the results, for visual review.</summary>
        public const string ShotsArgument = "-pz-shots";
        // E-21: 300 (was 240): every new run now opens on the 64 s news bulletin.
        public const float RoundTimeoutSeconds = 300, RetryAfterSeconds = 12;
        /// <summary>E-22: how long a still player may wait at the open door for the door zombie to lose them and go.</summary>
        public const float DoorZombieLeaveSeconds = 30;
        private enum DoorPlay { StandStill, BreakFree, Die }
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
            GameEvents.PlayerCaught.Subscribe(AnyGrab);
            StartCoroutine(Run());
        }

        private void OnDestroy() { Application.logMessageReceived -= Logged; GameEvents.PlayerCaught.Unsubscribe(AnyGrab); }

        /// <summary>E-22: every grab of the run, in any act, so a stalled act can be read against them.</summary>
        private void AnyGrab(PlayerCaught caught)
        {
            // GameFlow has already taken the grab when this runs: a first grab shows as a struggle, a second as a death.
            Debug.Log("[PZ-PROBE] round " + round + ": grab by zombie " + caught.ZombieId + " in " + flow.Core.CurrentAct + ": " +
                      (flow.Core.Struggling ? "struggle" : flow.Core.State == GameState.Caught ? "death" : flow.Core.State.ToString()));
        }

        /// <summary>E-22: what an act is waiting for, written when the probe has to do its objectives again.</summary>
        private void Describe(ActId act)
        {
            var text = new System.Text.StringBuilder("[PZ-PROBE] ").Append(act).Append(" waits: flow ").Append(flow.Core.State)
                .Append(", ").Append(Mathf.RoundToInt(1f / Mathf.Max(1e-4f, Time.smoothDeltaTime))).Append(" fps")
                .Append(Application.isFocused ? ", window focused" : ", window NOT focused");
            if (Services<IExperienceDirector>.TryGet(out IExperienceDirector director)) text.Append(", director in ").Append(director.CurrentAct);
            foreach (ObjectivePublisher objectives in Ward<ObjectivePublisher>())
            {
                text.Append("; objectives for ").Append(objectives.Act).Append(objectives.Live ? " (live)" : " (NOT live)");
                foreach (ObjectiveId id in ObjectiveIds.All)
                    if ((int)id / 100 == (int)act) text.Append(", ").Append(id).Append(objectives.IsComplete(id) ? " done" : " open");
            }
            foreach (PhysicalPuzzle p in Ward<PhysicalPuzzle>())
                if (p.BoundAct == act) text.Append("; ").Append(p.mechanic).Append(" at ").Append(p.Progress.ToString("0.00"));
            foreach (ObjectiveDoor door in Ward<ObjectiveDoor>())
                if (!door.isolationDoor && door.doorAct == act) text.Append("; door ").Append(door.IsOpen ? "open" : "shut");
            IPlayerQuery player = Services<IPlayerQuery>.Instance;
            if (player != null)
            {
                text.Append("; player at ").Append(player.FloorPosition);
                if (WardLayout.TryStairLanding(act, out LayoutPoint landing))
                    text.Append(", ").Append(WardLayout.HorizontalDistance(player.FloorPosition, landing.Position).ToString("0.00")).Append(" m from the landing");
            }
            Debug.Log(text.ToString());
        }

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

                ActId previous = ActId.None, retried = ActId.None;
                bool acted = false, scareShot = false, newsChecked = false, roomLooked = false, corridorLooked = false;
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
                        if (retried != previous) { retried = previous; Describe(previous); }
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
                        if (round == 1) ClearanceNote(previous);
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
                        // E-23: the hanged body in B-7 comes once the probe has looked at the corner and turned away (E-24: inside the
                        // room); the corridor's once it has looked at the dead end and turned away. Round 1 checks both.
                        if (round == 1 && previous == ActId.Awakening && !roomLooked) { roomLooked = true; yield return LookBack(RoomScare, false); }
                        if (round == 1 && previous == ActId.Machinery && !corridorLooked) { corridorLooked = true; yield return LookBack(CorridorScare, true); }
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
        /// E-21: a new run opens on the outbreak bulletin. E-24: it no longer holds the player (owner, 2026-11-11), so the probe
        /// turns to the set and watches it to the end, as a player would, and fails the run if the bulletin never played, never
        /// ended, or locked the view.
        /// </summary>
        private IEnumerator News(bool startOfRun)
        {
            NewsBroadcast[] sets = Ward<NewsBroadcast>();
            if (sets.Length != 1) { Problem("[PZ-PROBE] expected one news television in Ward, found " + sets.Length); yield break; }
            NewsBroadcast news = sets[0];
            if (!news.Playing)
            {
                // A restored Act 1 does not replay it; a new run must.
                if (startOfRun) Problem("[PZ-PROBE] the news bulletin did not play at the start of the run");
                yield break;
            }

            Face(news.screen.position);
            float began = Time.time;
            bool photographed = false, locked = false;
            while (news.Playing && Time.time - began < news.duration + 10)
            {
                if (flow.Core.State == GameState.Paused) flow.Resume();
                if (Services<IPlayerQuery>.Instance is PlayerRig rig && rig.GetComponent<DesktopLocomotion>() is DesktopLocomotion desk && desk.viewLocked) locked = true;
                if (round == 1 && !photographed && Time.time - began > 5) { photographed = true; yield return Shot("1-news"); }
                yield return null;
            }

            if (locked) Problem("[PZ-PROBE] the news bulletin locked the view");
            if (news.Playing) Problem("[PZ-PROBE] the news bulletin still played after " + (Time.time - began).ToString("0") + " s");
            else Debug.Log("[PZ-PROBE] the news bulletin played for another " + (Time.time - began).ToString("0.0") + " s; the view was free");
        }

        /// <summary>E-24: how many zombie models are eased off a wall right now, and the largest shift (WallClearance).</summary>
        private void ClearanceNote(ActId act)
        {
            WallClearance[] all = Ward<WallClearance>();
            int eased = 0;
            float most = 0;
            foreach (WallClearance c in all)
            {
                float m = c.Offset.magnitude;
                if (m > .01f) eased++;
                if (m > most) most = m;
            }

            if (all.Length == 0) Problem("[PZ-PROBE] no zombie model has WallClearance");
            else Debug.Log("[PZ-PROBE] wall clearance at " + act + ": " + eased + " of " + all.Length + " zombie models eased off a wall, largest " + most.ToString("0.00") + " m");
        }

        private const string RoomScare = "B-7 corner", CorridorScare = "corridor dead end";
        /// <summary>E-23: where a player leaving B-7 for the stairs stands in the corridor, and where the corridor's dead body lies.</summary>
        private static readonly Vector3 CorridorWatch = new Vector3(.4f, 0, 3.1f), CorridorBody = new Vector3(3.2f, 0, 4f);

        /// <summary>
        /// E-23: the look-away scares (LookAwayScare). In B-7 the probe looks at the corner from where it stands, turns its back on
        /// it, and turns round. In the corridor it stands where a player leaving B-7 stands, looks at the dead end, turns to the
        /// stairs, and turns back. Either way the body must be there and spotted. Round 1 photographs it, and in the corridor the
        /// dead body on the floor too, from far enough back that a level view takes it in.
        /// </summary>
        private IEnumerator LookBack(string label, bool corridor)
        {
            LookAwayScare scare = null;
            foreach (LookAwayScare s in Ward<LookAwayScare>()) if (s.label == label) scare = s;
            if (!scare) { Problem("[PZ-PROBE] no look-away scare '" + label + "' in Ward"); yield break; }
            Vector3 back = Services<IPlayerQuery>.TryGet(out IPlayerQuery player) ? player.FloorPosition : CorridorWatch;
            if (corridor) MoveTo(CorridorWatch);
            Vector3 from = corridor ? CorridorWatch : back, away = scare.lookAt - from;
            away.y = 0;
            Face(scare.lookAt);
            yield return GameSeconds(.8f);
            Face(corridor ? CorridorWatch + Vector3.right * 10 : from - away * 10);
            yield return GameSeconds(LookAwayRule.AwaySeconds + 1f);
            Face(scare.lookAt);
            yield return GameSeconds(.5f);
            Debug.Log("[PZ-PROBE] look-away scare '" + label + "': " + scare.Phase);
            if (scare.Phase != LookAwayRule.Phase.Spotted) Problem("[PZ-PROBE] the look-away scare '" + label + "' ended " + scare.Phase + ", not Spotted");
            if (round == 1) yield return Shot("1-hanging-" + (corridor ? "corridor" : "room"));
            if (!corridor) yield break;
            if (round == 1)
            {
                MoveTo(new Vector3(-1f, 0, 3f));
                Face(CorridorBody);
                yield return GameSeconds(.3f);
                yield return Shot("2-dead-body");
            }

            MoveTo(back);
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
                    yield return DoorScene(round == 2 ? DoorPlay.BreakFree : round == 3 ? DoorPlay.Die : DoorPlay.StandStill);
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

        /// <summary>
        /// E-22: the door zombie. The probe stands at the room door facing it and pushes the bar; the door opens onto the zombie.
        /// Then, by <paramref name="play"/>: stand still; or step until grabbed and win the struggle; or step until grabbed, lose
        /// the struggle, and check the run starts again from the bed. Every way ends standing still until the zombie has gone.
        /// </summary>
        private IEnumerator DoorScene(DoorPlay play)
        {
            // Every sound the game counts as the player's, and every grab, is logged while the door scene runs: a grab of a
            // player standing still must be explained by the log.
            GameEvents.Noise.Subscribe(HeardAtTheDoor);
            GameEvents.PlayerCaught.Subscribe(GrabbedAtTheDoor);
            try { yield return DoorSceneSteps(play); }
            finally { GameEvents.Noise.Unsubscribe(HeardAtTheDoor); GameEvents.PlayerCaught.Unsubscribe(GrabbedAtTheDoor); }
        }

        private float doorOpenedAt;
        private string SinceDoor => "t+" + (Time.time - doorOpenedAt).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        private void HeardAtTheDoor(NoiseEvent noise)
        {
            if (noise.Origin != NoiseOrigin.Player) return;
            Debug.Log("[PZ-PROBE] player noise " + noise.Source + " radius " + noise.RadiusMeters.ToString("0.0") + " at " + noise.Position + " " + SinceDoor);
        }
        private void GrabbedAtTheDoor(PlayerCaught caught)
        {
            var player = Services<IPlayerQuery>.Instance;
            Debug.Log("[PZ-PROBE] grabbed by zombie " + caught.ZombieId + " at " + caught.ZombiePosition + ", player at " +
                      (player != null ? player.FloorPosition.ToString() : "?") + " " + SinceDoor);
        }

        /// <summary>
        /// Game time that keeps going through a focus pause (the probe resumes it), as the main loop does. A grab ends the wait at
        /// once: the world is held then, game time stands still, and the struggle runs on the real clock.
        /// </summary>
        private IEnumerator GameSeconds(float seconds)
        {
            float waited = 0, giveUp = Time.realtimeSinceStartup + seconds + 30;
            while (waited < seconds && Time.realtimeSinceStartup < giveUp && flow.Core.State != GameState.Caught)
            {
                if (flow.Core.State == GameState.Paused) { Debug.Log("[PZ-PROBE] focus pause; resuming"); flow.Resume(); }
                waited += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator DoorSceneSteps(DoorPlay play)
        {
            MoveTo(WardLayout.A1_Door.Position);
            Face(WardLayout.A1_DoorWindow.Position);
            yield return null;
            bool pushed = false;
            foreach (PhysicalPuzzle p in Ward<PhysicalPuzzle>())
            {
                if (p.mechanic != PhysicalPuzzle.Mechanic.Handle || p.BoundAct != ActId.Awakening) continue;
                Vector3 point = p.transform.position;
                p.BeginGrip(point);
                p.SampleGrip(point + p.transform.TransformDirection(p.axis) * .081f, .1f);
                p.EndGrip();
                pushed = true;
            }
            if (!pushed) { Problem("[PZ-PROBE] no door bar in Act 1"); yield break; }
            float opened = doorOpenedAt = Time.time;
            if (round == 1)
            {
                yield return GameSeconds(1.7f); // the door has swung open
                yield return Shot("1-door-zombie");
            }

            if (play != DoorPlay.StandStill)
            {
                yield return GameSeconds(1.2f); // past the scream, when it hears nothing
                var rig = Services<IPlayerQuery>.Instance as PlayerRig;
                float until = Time.time + 8;
                while (flow.Core.State == GameState.Playing && Time.time < until)
                {
                    if (rig) Cues.Play(CueId.PlayerFootstep, rig.FloorPosition, NoiseOrigin.Player); // a step, as walking makes
                    yield return GameSeconds(.4f);
                }
                if (flow.Core.State != GameState.Caught) { Problem("[PZ-PROBE] the door zombie never attacked a player who kept stepping"); yield break; }
                if (!flow.Core.Struggling) { Problem("[PZ-PROBE] the first grab was not a struggle"); yield break; }
                Debug.Log("[PZ-PROBE] grabbed by the door zombie " + (Time.time - opened).ToString("0.0") + " s after the door opened");
                if (round == 2) yield return Shot("1-struggle");
                if (play == DoorPlay.BreakFree)
                {
                    for (int i = 0; i < GameFlowCore.StrugglePresses; i++) { flow.Struggle(); yield return null; }
                    if (flow.Core.State != GameState.Playing) { Problem("[PZ-PROBE] winning the struggle did not free the player"); yield break; }
                    Debug.Log("[PZ-PROBE] broke free of the grab");
                }
                else
                {
                    float dead = Time.realtimeSinceStartup;
                    while ((flow.Core.State != GameState.Playing || flow.Core.IsTransitioning) &&
                           Time.realtimeSinceStartup - dead < GameFlowCore.StruggleSeconds + 15)
                    {
                        // An unfocused window holds the struggle's clock (E-22), so a lost struggle would never end.
                        if (!Application.isFocused && flow.Core.Struggling)
                        {
                            if (!struggleFocusNoted) Debug.Log("[PZ-PROBE] the game window has no focus; telling the struggle it has, so the clock runs");
                            struggleFocusNoted = true;
                            flow.Core.Focus(true);
                        }
                        yield return null;
                    }
                    var back = Services<IPlayerQuery>.Instance;
                    bool inBed = back != null && WardLayout.HorizontalDistance(back.FloorPosition, WardLayout.A1_Start.Position) < .5f;
                    if (flow.Core.State != GameState.Playing || flow.Core.CurrentAct != ActId.Awakening || !inBed)
                    { Problem("[PZ-PROBE] a lost struggle did not restart the run from the bed (" + flow.Core.CurrentAct + " / " + flow.Core.State + ")"); yield break; }
                    Debug.Log("[PZ-PROBE] lost the struggle: back in bed " + (Time.realtimeSinceStartup - dead).ToString("0.0") + " s later");
                    yield return GameSeconds(1.5f);
                    yield return DoorSceneSteps(DoorPlay.StandStill); // the next life, this time standing still
                    yield break;
                }
            }

            // Stand still: the zombie must lose the player, never grab them, and leave; then the act ends. A grab is a problem,
            // and the probe then breaks free so the run goes on and shows what else happens.
            float giveUp = Time.realtimeSinceStartup + DoorZombieLeaveSeconds + 15;
            bool reported = false;
            while (flow.Core.CurrentAct == ActId.Awakening && Time.realtimeSinceStartup < giveUp)
            {
                if (flow.Core.State == GameState.Paused) { Debug.Log("[PZ-PROBE] focus pause; resuming"); flow.Resume(); }
                if (flow.Core.State == GameState.Caught && flow.Core.Struggling)
                {
                    if (!reported) Problem("[PZ-PROBE] the door zombie grabbed a player who stood still " + SinceDoor);
                    reported = true;
                    for (int i = 0; i < GameFlowCore.StrugglePresses; i++) { flow.Struggle(); yield return null; }
                }
                yield return null;
            }
            if (flow.Core.CurrentAct == ActId.Awakening) Problem("[PZ-PROBE] the door zombie never left a player who stood still");
            else Debug.Log("[PZ-PROBE] act 1 ended " + (Time.time - opened).ToString("0.0") + " s after the door opened");
        }

        /// <summary>Turns the player (and the view) toward <paramref name="point"/>, level.</summary>
        private static void Face(Vector3 point)
        {
            if (!(Services<IPlayerQuery>.Instance is PlayerRig rig) || !rig.origin) return;
            Vector3 feet = rig.FloorPosition, to = point - feet;
            to.y = 0;
            if (to.sqrMagnitude < 1e-4f) return;
            rig.origin.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
            MoveTo(feet);
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

        /// <summary>
        /// Pulls each of the act's levers down and holds it until it is all the way down, as a player does. E-22: the rig drops
        /// every held grip while the game window has no focus (by design: an alt-tab lets go), and an unattended run's window often
        /// has none, so the probe tells the rig it has (KeepFocus); a pause in the middle drops the grip, so the probe resumes and
        /// grips again.
        /// </summary>
        private IEnumerator Levers(PhysicalPuzzle[] puzzles, ActId act)
        {
            foreach (PhysicalPuzzle p in puzzles)
            {
                if (p.mechanic != PhysicalPuzzle.Mechanic.Lever || p.BoundAct != act) continue;
                Vector3 point = p.transform.position;
                KeepFocus();
                p.BeginGrip(point);
                p.SampleGrip(point - Vector3.up * .05f, .1f);
                float giveUp = Time.realtimeSinceStartup + 10;
                while (p.Progress < 1 && Time.realtimeSinceStartup < giveUp)
                {
                    yield return null;
                    if (flow.Core.State != GameState.Paused) continue;
                    Debug.Log("[PZ-PROBE] focus pause while holding a lever; resuming");
                    flow.Resume();
                    yield return null;
                    KeepFocus();
                    p.BeginGrip(point);
                    p.SampleGrip(point - Vector3.up * .05f, .1f);
                }
                p.EndGrip();
            }
        }

        private bool focusNoted, struggleFocusNoted;

        /// <summary>
        /// E-22: the game window of a run started from a background process often never gets focus, and the rig then drops every
        /// held grip each frame, so no lever can be held. The probe already resumes the focus pause; here it also tells the rig the
        /// window has focus (the rig's own focus callback, the only one on its object), and says so once per run.
        /// </summary>
        private void KeepFocus()
        {
            if (Application.isFocused || !(Services<IPlayerQuery>.Instance is PlayerRig rig)) return;
            if (!focusNoted)
            {
                focusNoted = true;
                Debug.Log("[PZ-PROBE] the game window has no focus; telling the rig it has, so a held lever stays held");
            }
            rig.SendMessage("OnApplicationFocus", true, SendMessageOptions.DontRequireReceiver);
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
