using System;
using System.Collections.Generic;
using NUnit.Framework;
using PatientZero.Atmosphere.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Atmosphere.Tests
{
    public sealed class AtmosphereLogicTests
    {
        private static DirectorCore New(ScareIntensity scares = ScareIntensity.Standard)
        {
            var d = new DirectorCore(new DirectorMemory()); var settings = ExperienceSettings.Defaults; settings.Scares = scares; d.Settings = settings;
            d.StartRun(ActId.Awakening, false); return d;
        }
        private static List<DirectorCommand> Drain(DirectorCore d)
        { var list = new List<DirectorCommand>(); while (d.TryRead(out var c)) list.Add(c); return list; }
        private static bool Contains(List<DirectorCommand> items, DirectorCommandKind kind, int id)
        { foreach (var c in items) if (c.Kind == kind && c.Id == id) return true; return false; }
        // E-22 (two-minute version): the player pushes the door bar; the door opens onto the zombie at once.
        private static void PushBar(DirectorCore d) { d.Progress(ObjectiveId.Act1_DoorUnbarred, 1); d.Complete(ObjectiveId.Act1_DoorUnbarred); }
        /// <summary>The door zombie's search ran out: it has lost a player who froze.</summary>
        private static void WalkerLosesPlayer(DirectorCore d)
        {
            d.Alert(ZombieIds.Walker, AlertLevel.Unaware, DirectorCore.DoorFront, WardLayout.A1_Door.Position);
            d.Tick(.1f, true, WardLayout.A1_Door.Position);
        }
        private static void WalkerGone(DirectorCore d)
        { d.ReportZombiePosition(ZombieIds.Walker, WardLayout.Z1_WalkTo.Position); d.Tick(.01f, true, WardLayout.A1_Door.Position); }
        private static bool Sent(List<DirectorCommand> items, ZombieDirection direction)
        { return items.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Walker && c.Detail == (int)direction); }
        private static void Machinery(DirectorCore d)
        {
            PushBar(d); d.Complete(ObjectiveId.Act1_DoorOpened); Assert.AreEqual(ActId.Awakening, d.Act, "the act waits for the zombie to go");
            WalkerLosesPlayer(d); WalkerGone(d); Assert.AreEqual(ActId.Machinery, d.Act);
        }
        private static List<CueId> GoldenRun(ScareIntensity scares, out DirectorCore d)
        {
            d = New(scares); var cues = new List<CueId>(); Collect(d, cues);
            d.Tick(3, true, WardLayout.A1_Start.Position); Collect(d, cues);
            Machinery(d); Collect(d, cues);
            d.Complete(ObjectiveId.Act2_ValveVented); d.Complete(ObjectiveId.Act2_CablePlugged); d.Complete(ObjectiveId.Act2_PowerRestored);
            Assert.AreEqual(ActId.Machinery, d.Act); Collect(d, cues);
            d.Tick(12, true, WardLayout.A2_Start.Position); Collect(d, cues);
            Assert.IsTrue(d.InvestigationStarted);
            d.Tick(.61f, true, WardLayout.A2_Start.Position); Collect(d, cues);
            d.Tick(1.9f, true, WardLayout.A2_Start.Position); Collect(d, cues); Assert.AreEqual(ActId.Darkness, d.Act);
            d.Tick(3, true, WardLayout.A3_Start.Position); Collect(d, cues);
            d.Tick(7, true, WardLayout.A3_Mid1.Position); Collect(d, cues);
            d.Complete(ObjectiveId.Act3_BeaconReached); Collect(d, cues); Assert.AreEqual(ActId.Exit, d.Act);
            d.Progress(ObjectiveId.Act4_ReleasePulled, .6f); d.Complete(ObjectiveId.Act4_ReleasePulled); d.Complete(ObjectiveId.Act4_ExitOpened); Collect(d, cues);
            // v1.3 (E-19): the stairs, then the lower floors; the ending plays in the lobby. E-26: floor 2 is skipped.
            Assert.AreEqual(ActId.Exit, d.Act, "the open door alone must not leave the ward");
            d.Complete(ObjectiveId.Act4_StairsReached); Collect(d, cues); Assert.AreEqual(ActId.Lobby, d.Act);
            d.Tick(2, true, WardLayout.A6_Start.Position); Collect(d, cues);
            d.Complete(ObjectiveId.Act6_GeneratorPrimed); d.Complete(ObjectiveId.Act6_LockdownReleased); Collect(d, cues);
            d.Complete(ObjectiveId.Act6_MainDoorsOpened); Collect(d, cues);
            d.Tick(2, true, WardLayout.A6_Lockdown.Position); Collect(d, cues);
            d.Tick(4, true, WardLayout.A6_Lockdown.Position); Collect(d, cues);
            d.Tick(3, true, WardLayout.A6_Lockdown.Position); Collect(d, cues);
            d.Tick(3, true, WardLayout.A6_Lockdown.Position); Collect(d, cues);
            return cues;
        }
        private static void Collect(DirectorCore d, List<CueId> cues)
        { foreach (var c in Drain(d)) if (c.Kind == DirectorCommandKind.Cue) cues.Add(c.Cue); }
        [Test] public void GateSignalIsOneShotQueryPersistsAndResetClosesBeforeActStart()
        {
            var d = New(); Drain(d);
            Assert.IsFalse(d.IsGateReleased(GateId.None)); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            PushBar(d);
            var released = Drain(d);
            Assert.AreEqual(1, released.FindAll(c => c.Kind == DirectorCommandKind.Gate && c.Id == (int)GateId.Act1Door).Count);
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door)); // Same query used by a late-loaded door.
            for (int i = 0; i < 20; i++) d.Tick(.1f, true, Vector3.zero);
            Assert.IsFalse(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Gate));
            d.StartRun(ActId.Awakening, true);
            Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            var reset = Drain(d); Assert.AreEqual(DirectorCommandKind.Act, reset[0].Kind);
            PushBar(d);
            Assert.AreEqual(1, Drain(d).FindAll(c => c.Kind == DirectorCommandKind.Gate).Count);
            d.Complete(ObjectiveId.Act1_DoorOpened); WalkerLosesPlayer(d); WalkerGone(d);
            Assert.AreEqual(ActId.Machinery, d.Act); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.Act, (int)ActId.Machinery));
            d.StartRun(ActId.Awakening, false); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
        }
        [Test] public void TheBarAloneOpensTheDoorOntoTheScreamingZombieInReducedModeToo()
        {
            foreach (var mode in new[] { ScareIntensity.Standard, ScareIntensity.Reduced })
            {
                var d = New(mode); Drain(d);
                d.Progress(ObjectiveId.Act1_DoorUnbarred, .5f); d.Tick(20, true, WardLayout.A1_Door.Position);
                Assert.IsFalse(d.IsGateReleased(GateId.Act1Door), "half a push opens nothing");
                Assert.IsFalse(Sent(Drain(d), ZombieDirection.Confront), "no zombie before the door opens");
                d.Complete(ObjectiveId.Act1_DoorUnbarred);
                Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
                var commands = Drain(d);
                Assert.AreEqual(1, commands.FindAll(c => c.Kind == DirectorCommandKind.Gate).Count);
                var confront = commands.Find(c => c.Kind == DirectorCommandKind.Zombie && c.Detail == (int)ZombieDirection.Confront);
                Assert.AreEqual(ZombieIds.Walker, confront.Id); Assert.AreEqual(DirectorCore.DoorFront, confront.Position);
                var scream = commands.Find(c => c.Kind == DirectorCommandKind.Cue && c.Cue == CueId.JumpScareScream);
                Assert.AreEqual(CueId.JumpScareScream, scream.Cue, "it screams in the player's face");
                Assert.AreEqual(mode == ScareIntensity.Standard ? 1 : .355f, scream.Value, 1e-5f);
                bool standard = mode == ScareIntensity.Standard;
                Assert.AreEqual(standard, commands.Exists(c => c.Kind == DirectorCommandKind.Cue && c.Cue == CueId.JumpScareStinger));
                Assert.AreEqual(standard, commands.Exists(c => c.Kind == DirectorCommandKind.LocalLightPulse));
                Assert.AreEqual(standard, commands.Exists(c => c.Kind == DirectorCommandKind.Silence));
                Assert.AreEqual(standard ? 1 : 0, d.HardScares);
            }
        }
        [Test] public void TheDoorZombieStandsOutsideClearOfTheDoorSwingAndWithinEarshot()
        {
            Vector3 front = DirectorCore.DoorFront, door = WardLayout.A1_DoorWindow.Position, hinge = door + Vector3.right * .5f;
            Assert.Greater(front.z, door.z + .5f, "outside the room"); Assert.Less(front.z, WardLayout.Z1_WalkFrom.Position.z, "inside the corridor");
            Assert.AreEqual(WardLayout.Z1_WalkFrom.Position.y, front.y, "on the corridor floor");
            Assert.Greater(WardLayout.HorizontalDistance(front, hinge), 1.25f, "the 1 m door leaf swings clear of it");
            float toPlayer = WardLayout.HorizontalDistance(front, WardLayout.A1_Door.Position);
            Assert.Greater(toPlayer, .8f, "out of reach of a player standing at the door"); Assert.Less(toPlayer, 2.5f, "a footstep there is heard");
        }
        private static void AssertAssistFloorSurvivesProgress(DirectorCore d)
        {
            Drain(d); d.Progress(ObjectiveId.Act2_ValveVented, .1f);
            var commands = Drain(d);
            Assert.IsTrue(commands.Exists(c => c.Kind == DirectorCommandKind.Assist && c.Id == (int)ObjectiveId.Act2_ValveVented && c.Detail == (int)AssistLevel.Assist));
            Assert.IsFalse(commands.Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail < (int)AssistLevel.Assist));
            d.Tick(31, true, Vector3.zero); commands = Drain(d);
            Assert.IsFalse(commands.Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail < (int)AssistLevel.Assist));
        }
        [Test] public void AlwaysOnAssistSurvivesProgressAndHints()
        {
            var d = New(); Drain(d); var settings = d.Settings; settings.PuzzleAssistAlwaysOn = true; d.Settings = settings;
            d.StartRun(ActId.Machinery, false); AssertAssistFloorSurvivesProgress(d);
        }
        [Test] public void AdaptiveAssistSurvivesProgressAfterRestore()
        {
            var memory = new DirectorMemory(); memory.Catches[(int)ActId.Machinery] = 2;
            var d = new DirectorCore(memory); d.StartRun(ActId.Machinery, true); AssertAssistFloorSurvivesProgress(d);
        }
        [Test] public void EnablingPuzzleAssistMidActEmitsFloorImmediately()
        {
            var d = New(); d.StartRun(ActId.Machinery, false); Drain(d);
            var settings = d.Settings; settings.PuzzleAssistAlwaysOn = true; d.Settings = settings;
            var commands = Drain(d);
            Assert.IsTrue(commands.Exists(c => c.Kind == DirectorCommandKind.Assist && c.Id == (int)ObjectiveId.Act2_ValveVented && c.Detail == (int)AssistLevel.Assist));
            AssertAssistFloorSurvivesProgress(d);
        }
        [Test] public void GoldenRunFollowsApprovedBeatOrderAndTwoHardScares()
        {
            var cues = GoldenRun(ScareIntensity.Standard, out var d);
            var expected = new[] { CueId.HeartbeatWake, CueId.PaGarbledBreach, CueId.JumpScareStinger, CueId.JumpScareScream,
                CueId.DistantCrashLure, CueId.PowerThrum, CueId.BreakerOverload, CueId.PaCarrierWarning,
                CueId.BeaconChirp, CueId.BeaconChirp, CueId.RestraintSnap, CueId.ZombieLunge,
                // E-26: no floor 2 beats (its PA, the fuse spark and the crash that lures Z5); the ward's stairs lead to the lobby.
                CueId.PaGarbledBreach, CueId.PaPatientZero, CueId.RevealDrone };
            Assert.AreEqual(expected.Length, cues.Count);
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], cues[i], "beat " + i);
            Assert.AreEqual(2, d.HardScares);
        }
        [Test] public void ReducedRunHasZeroHardScaresAndNoLunge()
        {
            var cues = GoldenRun(ScareIntensity.Reduced, out var d);
            Assert.AreEqual(0, d.HardScares); Assert.IsFalse(cues.Contains(CueId.JumpScareStinger));
            Assert.IsFalse(cues.Contains(CueId.ZombieLunge)); Assert.IsTrue(cues.Contains(CueId.JumpScareScream));
        }
        [Test] public void ActOneEndsOnlyOnceTheDoorZombieHasLostThePlayerAndGone()
        {
            var d = New(); Drain(d); d.Complete(ObjectiveId.Act1_DoorOpened);
            d.Alert(ZombieIds.Walker, AlertLevel.Unaware, WardLayout.Z1_WalkFrom.Position, WardLayout.A1_Start.Position);
            d.Tick(16, true, WardLayout.A1_Start.Position);
            Assert.IsFalse(Sent(Drain(d), ZombieDirection.LeaveWindow), "nothing to leave before the door opens");
            PushBar(d); Assert.IsTrue(d.CanOpenAct1Door); Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            Assert.AreEqual(ActId.Awakening, d.Act, "an early door completion must be reported again after release");
            d.Complete(ObjectiveId.Act1_DoorOpened); Assert.AreEqual(ActId.Awakening, d.Act, "the zombie is still at the door");
            // While it hunts the player it stays, however long that takes.
            d.Alert(ZombieIds.Walker, AlertLevel.Searching, DirectorCore.DoorFront, WardLayout.A1_Door.Position);
            d.Alert(ZombieIds.Walker, AlertLevel.Chasing, DirectorCore.DoorFront, WardLayout.A1_Door.Position);
            d.Tick(30, true, WardLayout.A1_Door.Position);
            Assert.IsFalse(Sent(Drain(d), ZombieDirection.LeaveWindow)); Assert.AreEqual(ActId.Awakening, d.Act);
            // It loses the player: it shuffles off down the corridor, and the act ends when it is gone.
            WalkerLosesPlayer(d); var commands = Drain(d);
            var leave = commands.Find(c => c.Kind == DirectorCommandKind.Zombie && c.Detail == (int)ZombieDirection.LeaveWindow);
            Assert.AreEqual(ZombieIds.Walker, leave.Id); Assert.AreEqual(WardLayout.Z1_WalkTo.Position, leave.Position);
            d.ReportZombiePosition(ZombieIds.Walker, WardLayout.Z1_WalkTo.Position - Vector3.right); d.Tick(.1f, true, WardLayout.A1_Door.Position);
            Assert.AreEqual(ActId.Awakening, d.Act); Assert.IsFalse(Sent(Drain(d), ZombieDirection.LeaveWindow), "sent off once");
            WalkerGone(d); Assert.AreEqual(ActId.Machinery, d.Act);
        }
        [Test] public void AHeldUpDoorZombieIsTakenAwayAfterTwelveSeconds()
        {
            var d = New(); PushBar(d); d.Complete(ObjectiveId.Act1_DoorOpened); WalkerLosesPlayer(d); Drain(d);
            d.Tick(DirectorCore.WalkerLeaveSeconds - .5f, true, WardLayout.A1_Door.Position); Assert.AreEqual(ActId.Awakening, d.Act);
            d.Tick(.6f, true, WardLayout.A1_Door.Position); Assert.AreEqual(ActId.Machinery, d.Act);
        }
        [Test] public void ActTwoInvestigationIsGuaranteedBeforeSurge()
        {
            var d = New(); Machinery(d); Drain(d);
            d.Complete(ObjectiveId.Act2_ValveVented); d.Complete(ObjectiveId.Act2_CablePlugged); d.Complete(ObjectiveId.Act2_PowerRestored); Drain(d);
            d.Tick(11.9f, true, WardLayout.A2_Start.Position); Assert.IsFalse(d.InvestigationStarted); Drain(d);
            d.Tick(.2f, true, WardLayout.A2_Start.Position); Assert.IsTrue(d.InvestigationStarted);
            Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.Lighting, (int)LightingState.Surge));
        }
        [Test] public void HintsFireAtTenTwentyThirtyAndResetOnActualProgress()
        {
            var d = New(); Drain(d); d.Tick(9.9f, true, Vector3.zero); Assert.IsFalse(Contains(Drain(d), DirectorCommandKind.Assist, 100));
            d.Tick(.2f, true, Vector3.zero); var hints = Drain(d); Assert.IsTrue(Contains(hints, DirectorCommandKind.Assist, 100));
            d.Progress(ObjectiveId.Act1_DoorUnbarred, .34f); Drain(d);
            d.Tick(9.9f, true, Vector3.zero); Assert.IsFalse(Contains(Drain(d), DirectorCommandKind.Assist, 100));
            d.Tick(.2f, true, Vector3.zero); Drain(d);
            d.Tick(10, true, Vector3.zero); hints = Drain(d); Assert.IsTrue(hints.Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail == 2));
            d.Tick(10, true, Vector3.zero); hints = Drain(d); Assert.IsTrue(hints.Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail == 3));
        }
        [Test] public void TrackingLossDefersHintsWithoutAutoPausingStory()
        {
            var d = New(); Drain(d); d.Tracking(InputModality.Hands, false, false);
            d.Tick(20, true, Vector3.zero); Assert.AreEqual(20, d.ActTime); Assert.IsFalse(Contains(Drain(d), DirectorCommandKind.Assist, 100));
            d.Tracking(InputModality.Controllers, false, false); d.Tick(10.1f, true, Vector3.zero);
            Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.Assist, 100));
        }
        [Test] public void PausingFreezesEveryDirectorTimer()
        {
            var d = New(); Drain(d); d.Tick(50, false, Vector3.zero); Assert.AreEqual(0, d.ActTime); Assert.AreEqual(0, d.ExperienceTime); Assert.AreEqual(0, Drain(d).Count);
        }
        [Test] public void AmbientRequiresDistanceWallAndScareSilenceWindow()
        {
            var d = New(); Drain(d); PushBar(d); Drain(d);
            Assert.IsFalse(d.TryAmbient(Vector3.right * 10, Vector3.zero, true));
            d.Tick(6.1f, true, Vector3.zero); Drain(d);
            Assert.IsFalse(d.TryAmbient(Vector3.right * 10, Vector3.zero, false));
            Assert.IsFalse(d.TryAmbient(Vector3.right * 7, Vector3.zero, true));
            Assert.IsTrue(d.TryAmbient(Vector3.right * 10, Vector3.zero, true)); Drain(d);
            Assert.IsFalse(d.TryAmbient(Vector3.right * 10, Vector3.zero, true));
        }
        [Test] public void SecondCatchEnablesAdaptiveAssistOnRestore()
        {
            var memory = new DirectorMemory(); var d = new DirectorCore(memory); d.StartRun(ActId.Machinery, true); Drain(d);
            d.Caught(); d.Caught(); d = new DirectorCore(memory); d.StartRun(ActId.Machinery, true);
            Assert.IsTrue(d.AdaptiveAssist); Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.Assist, 200));
            Assert.IsFalse(d.InvestigationStarted); Assert.AreEqual(0, d.ActTime);
        }
        [Test] public void RestoreDoesNotSpendHardScareBudgetTwice()
        {
            var d = New(); PushBar(d); Drain(d); Assert.AreEqual(1, d.HardScares);
            d.StartRun(ActId.Awakening, true); Drain(d); PushBar(d); var again = Drain(d); Assert.AreEqual(1, d.HardScares);
            Assert.IsTrue(Sent(again, ZombieDirection.Confront), "the zombie is behind the door in every life");
            Assert.IsTrue(again.Exists(c => c.Kind == DirectorCommandKind.Cue && c.Cue == CueId.JumpScareScream));
            Assert.IsFalse(again.Exists(c => c.Kind == DirectorCommandKind.Cue && c.Cue == CueId.JumpScareStinger), "the full scare is spent once a run");
        }
        // v1.3 (E-19): the lower floors.
        [Test] public void WardEndsOnTheStairLandingNotTheOpenDoor()
        {
            var d = New(); d.StartRun(ActId.Exit, true); Drain(d);
            d.Complete(ObjectiveId.Act4_ReleasePulled); d.Complete(ObjectiveId.Act4_ExitOpened); var opened = Drain(d);
            Assert.AreEqual(ActId.Exit, d.Act); Assert.IsFalse(Contains(opened, DirectorCommandKind.State, (int)GameState.Ending));
            Assert.IsFalse(opened.Exists(c => c.Kind == DirectorCommandKind.Lighting && c.Id == (int)LightingState.ExitDaylight), "daylight waits for the lobby");
            // E-26 (owner, 2026-11-13): the two-minute version skips floor 2; the ward's stairs lead to the lobby.
            d.Complete(ObjectiveId.Act4_StairsReached); var down = Drain(d);
            Assert.AreEqual(ActId.Lobby, d.Act); Assert.IsTrue(Contains(down, DirectorCommandKind.Act, (int)ActId.Lobby));
            Assert.IsFalse(Contains(down, DirectorCommandKind.Act, (int)ActId.Laboratory));
            Assert.IsTrue(down.Exists(c => c.Kind == DirectorCommandKind.Lighting && c.Id == (int)LightingState.EmergencyRed));
            Assert.IsTrue(down.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Guard && c.Detail == (int)ZombieDirection.Patrol));
            Assert.IsFalse(down.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Researcher && c.Detail != (int)ZombieDirection.Hide));
            Assert.IsTrue(down.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.FinalPatient && c.Detail == (int)ZombieDirection.Hide));
        }
        [Test] public void StairsBeforeTheDoorOpensDoNotLeaveTheWard()
        {
            var d = New(); d.StartRun(ActId.Exit, true); Drain(d);
            d.Complete(ObjectiveId.Act4_StairsReached); Drain(d); Assert.AreEqual(ActId.Exit, d.Act);
            d.Complete(ObjectiveId.Act4_ReleasePulled); d.Complete(ObjectiveId.Act4_ExitOpened); Drain(d);
            Assert.AreEqual(ActId.Lobby, d.Act, "a banked arrival counts once the door is open");
        }
        [Test] public void FuseSparkLuresTheResearcherOnce()
        {
            var d = New(); d.StartRun(ActId.Laboratory, true); Drain(d);
            d.Complete(ObjectiveId.Act5_FuseInserted); var spark = Drain(d);
            Assert.IsTrue(spark.Exists(c => c.Cue == CueId.DistantCrashLure && WardLayout.HorizontalDistance(c.Position, WardLayout.A5_Start.Position) < .01f));
            Assert.IsTrue(spark.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Researcher && c.Detail == (int)ZombieDirection.Investigate));
            d.Complete(ObjectiveId.Act5_FuseInserted); Assert.AreEqual(0, Drain(d).Count);
            d.Complete(ObjectiveId.Act5_OverridePulled); d.Complete(ObjectiveId.Act5_StairDoorOpened); Drain(d); Assert.AreEqual(ActId.Laboratory, d.Act);
            d.Complete(ObjectiveId.Act5_StairsReached); var lobby = Drain(d); Assert.AreEqual(ActId.Lobby, d.Act);
            Assert.IsTrue(lobby.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Guard && c.Detail == (int)ZombieDirection.Patrol));
            Assert.IsTrue(lobby.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Porter && c.Detail == (int)ZombieDirection.Dormant));
            Assert.IsTrue(lobby.Exists(c => c.Kind == DirectorCommandKind.Lighting && c.Id == (int)LightingState.EmergencyRed));
        }
        [Test] public void LockdownReleaseBringsDaylightAndCatchesCountPerFloor()
        {
            var memory = new DirectorMemory(); var d = new DirectorCore(memory); d.StartRun(ActId.Lobby, true); Drain(d);
            d.Caught(); d.Caught(); Assert.AreEqual(2, memory.Catches[(int)ActId.Lobby]); Assert.IsTrue(d.AdaptiveAssist);
            d.Complete(ObjectiveId.Act6_LockdownReleased);
            Assert.IsTrue(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Lighting && c.Id == (int)LightingState.ExitDaylight));
            Assert.Throws<ArgumentOutOfRangeException>(() => d.StartRun((ActId)7, false));
        }
        [Test] public void EndingOrdersKneelPaRevealDroneThenComplete()
        {
            var d = New(); d.StartRun(ActId.Lobby, true); Drain(d);
            d.Complete(ObjectiveId.Act6_GeneratorPrimed); d.Complete(ObjectiveId.Act6_LockdownReleased); d.Complete(ObjectiveId.Act6_MainDoorsOpened);
            Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.State, (int)GameState.Ending));
            d.Tick(1, true, Vector3.zero); var kneel = Drain(d);
            Assert.IsTrue(kneel.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Guard && c.Detail == (int)ZombieDirection.Kneel));
            Assert.IsTrue(kneel.Exists(c => c.Kind == DirectorCommandKind.Zombie && c.Id == ZombieIds.Porter && c.Detail == (int)ZombieDirection.Kneel));
            d.Tick(1, true, Vector3.zero); Assert.IsTrue(Drain(d).Exists(c => c.Cue == CueId.PaPatientZero));
            d.Tick(4, true, Vector3.zero); var six = Drain(d); Assert.IsTrue(six.Exists(c => c.Cue == CueId.RevealDrone));
            Assert.IsTrue(six.Exists(c => c.Kind == DirectorCommandKind.Reveal && Math.Abs(c.Value - .5f) < .01f));
            d.Tick(6, true, Vector3.zero); Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.State, (int)GameState.Complete));
        }
        [Test] public void FlickerAndPulseStayBelowThreeDiscreteChangesPerSecondForTenMinutes()
        {
            foreach (var state in new[] { LightingState.Awakening, LightingState.EmergencyRed })
            {
                var changes = new Queue<double>(); float before = LightingPresets.Variation(state, 0, false, 42);
                for (int ms = 1; ms <= 600000; ms++)
                {
                    double time = ms / 1000.0; float value = LightingPresets.Variation(state, time, false, 42);
                    if (value != before) { changes.Enqueue(time); before = value; }
                    while (changes.Count > 0 && time - changes.Peek() > 1) changes.Dequeue();
                    Assert.LessOrEqual(changes.Count, 3);
                }
            }
        }
        [Test] public void ReducedFlickerIsConstantAndSurgeIsMonotonic()
        {
            for (int ms = 0; ms < 10000; ms++)
            {
                Assert.AreEqual(.8f, LightingPresets.Variation(LightingState.Awakening, ms / 1000.0, true, 1));
                Assert.AreEqual(.75f, LightingPresets.Variation(LightingState.EmergencyRed, ms / 1000.0, true, 1));
            }
            float before = .4f;
            for (int ms = 0; ms <= 600; ms++) { float gain = LightingPresets.Surge(.4f, ms / 1000f); Assert.GreaterOrEqual(gain, before); before = gain; }
            Assert.AreEqual(1.3, before, .0001);
        }
        [Test] public void WindowPulseAndCombinedFixturesNeverExceedThreeEdgesInAnySecond()
        {
            var pulse = new LocalLightPulse(); var changes = new Queue<double>();
            float before = pulse.Sample(0, 0, false) + pulse.Sample(0, 1, false);
            int brightSamples = 0;
            for (int ms = 1; ms <= 600000; ms++)
            {
                double time = ms / 1000.0;
                if (ms % 12347 == 0) pulse.Request(time, false);
                float first = pulse.Sample(time, 0, false), second = pulse.Sample(time, 1, false), sum = first + second;
                if (second > 1) brightSamples++;
                if (sum != before) { changes.Enqueue(time); before = sum; }
                while (changes.Count > 0 && time - changes.Peek() > 1) changes.Dequeue();
                if (changes.Count > 3) Assert.Fail("Pulse at " + time + ": " + string.Join(",", changes));
            }
            Assert.Greater(brightSamples, 0);
            pulse.Request(1000, true); Assert.AreEqual(.8f, pulse.Sample(1000.1, 1, true));
        }
        [Test] public void GateEventOccursOnceAndLateDoorCanQueryUntilActChanges()
        {
            var d = New(); Drain(d);
            Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            PushBar(d);
            var events = Drain(d);
            Assert.AreEqual(1, events.FindAll(c => c.Kind == DirectorCommandKind.Gate && c.Id == (int)GateId.Act1Door).Count);
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            Assert.IsFalse(d.IsGateReleased(GateId.None));
            for (int n = 0; n < 20; n++) { d.Tick(.1f, true, Vector3.zero); Assert.IsFalse(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Gate)); }
            // Models a door enabled after the signal: the query remains true without replaying an event.
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            d.Complete(ObjectiveId.Act1_DoorOpened); WalkerLosesPlayer(d); WalkerGone(d);
            Assert.AreEqual(ActId.Machinery, d.Act); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
        }
        [Test] public void RestoreClosesGateAndAllowsExactlyOneFreshRelease()
        {
            var d = New(); Drain(d);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (attempt != 0) { d.StartRun(ActId.Awakening, true); Drain(d); }
                Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
                PushBar(d); d.Tick(.1f, true, Vector3.zero); var commands = Drain(d);
                Assert.AreEqual(1, commands.FindAll(c => c.Kind == DirectorCommandKind.Gate).Count);
                Assert.AreEqual(1, commands.FindAll(c => c.Kind == DirectorCommandKind.Zombie && c.Detail == (int)ZombieDirection.Confront).Count);
                Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            }
            d.StartRun(ActId.Awakening, false); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
        }
        [Test] public void PausedTimeNeverSendsTheDoorZombieOffOrEndsTheAct()
        {
            var d = New(); PushBar(d); d.Complete(ObjectiveId.Act1_DoorOpened);
            d.Alert(ZombieIds.Walker, AlertLevel.Unaware, DirectorCore.DoorFront, WardLayout.A1_Door.Position); Drain(d);
            d.Tick(20, false, Vector3.zero); Assert.IsFalse(Sent(Drain(d), ZombieDirection.LeaveWindow));
            d.ReportZombiePosition(ZombieIds.Walker, WardLayout.Z1_WalkTo.Position); d.Tick(20, false, Vector3.zero);
            Assert.AreEqual(ActId.Awakening, d.Act);
            d.Tick(.1f, true, Vector3.zero); Assert.IsTrue(Sent(Drain(d), ZombieDirection.LeaveWindow) || d.Act == ActId.Machinery);
            Assert.AreEqual(ActId.Machinery, d.Act);
        }
        [Test] public void AlwaysOnAssistSurvivesProgressAndAppliesImmediatelyMidAct()
        {
            var d = New(); Drain(d); var s = d.Settings; s.PuzzleAssistAlwaysOn = true; d.Settings = s;
            Assert.IsTrue(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail == (int)AssistLevel.Assist));
            d.Progress(ObjectiveId.Act1_DoorUnbarred, .1f);
            var c = Drain(d); Assert.IsTrue(c.Exists(x => x.Kind == DirectorCommandKind.Assist && x.Detail == (int)AssistLevel.Assist));
            Assert.IsFalse(c.Exists(x => x.Kind == DirectorCommandKind.Assist && x.Detail == 0));
        }
        [Test] public void AdaptiveAssistSurvivesProgressAndSettingChanges()
        {
            var m = new DirectorMemory(); m.Catches[2] = 2; var d = new DirectorCore(m);
            d.StartRun(ActId.Machinery, true); Drain(d); d.Progress(ObjectiveId.Act2_ValveVented, .1f);
            Assert.IsTrue(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail == (int)AssistLevel.Assist));
            var s = d.Settings; s.PuzzleAssistAlwaysOn = true; d.Settings = s; Drain(d);
            s.PuzzleAssistAlwaysOn = false; d.Settings = s;
            Assert.IsFalse(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Assist && c.Detail == 0));
        }
    }
}
