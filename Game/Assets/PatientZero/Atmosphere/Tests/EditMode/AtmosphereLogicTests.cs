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
        private static void Window(DirectorCore d)
        {
            d.Progress(ObjectiveId.Act1_DoorUnbarred, .34f); d.Complete(ObjectiveId.Act1_DoorUnbarred);
            d.ReportZombiePosition(1, WardLayout.A1_DoorWindow.Position); d.Tick(.1f, true, WardLayout.A1_Start.Position);
        }
        private static void Machinery(DirectorCore d)
        {
            Window(d); d.Tick(2, true, WardLayout.A1_Start.Position); d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position); d.Tick(.01f, true, WardLayout.A1_Start.Position);
            d.Complete(ObjectiveId.Act1_DoorOpened); Assert.AreEqual(ActId.Machinery, d.Act);
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
            d.Tick(2, true, WardLayout.A4_Start.Position); Collect(d, cues);
            d.Tick(4, true, WardLayout.A4_Start.Position); Collect(d, cues);
            d.Tick(3, true, WardLayout.A4_Start.Position); Collect(d, cues);
            d.Tick(3, true, WardLayout.A4_Start.Position); Collect(d, cues);
            return cues;
        }
        private static void Collect(DirectorCore d, List<CueId> cues)
        { foreach (var c in Drain(d)) if (c.Kind == DirectorCommandKind.Cue) cues.Add(c.Cue); }
        [Test] public void GateSignalIsOneShotQueryPersistsAndResetClosesBeforeActStart()
        {
            var d = New(); Drain(d);
            Assert.IsFalse(d.IsGateReleased(GateId.None)); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            Window(d); Drain(d); d.Tick(2, true, WardLayout.A1_Start.Position); Drain(d);
            d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position); d.Tick(.01f, true, Vector3.zero);
            var released = Drain(d);
            Assert.AreEqual(1, released.FindAll(c => c.Kind == DirectorCommandKind.Gate && c.Id == (int)GateId.Act1Door).Count);
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door)); // Same query used by a late-loaded door.
            for (int i = 0; i < 20; i++) { d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position); d.Tick(.1f, true, Vector3.zero); }
            Assert.IsFalse(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Gate));
            d.StartRun(ActId.Awakening, true);
            Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            var reset = Drain(d); Assert.AreEqual(DirectorCommandKind.Act, reset[0].Kind);
            Window(d); Drain(d); d.Tick(2, true, Vector3.zero); Drain(d); d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position); d.Tick(.01f, true, Vector3.zero);
            Assert.AreEqual(1, Drain(d).FindAll(c => c.Kind == DirectorCommandKind.Gate).Count);
            d.Complete(ObjectiveId.Act1_DoorOpened);
            Assert.AreEqual(ActId.Machinery, d.Act); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.Act, (int)ActId.Machinery));
            d.StartRun(ActId.Awakening, false); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
        }
        [Test] public void GateWaitsForPlanksLureAndFullClearanceInReducedModeToo()
        {
            foreach (var mode in new[] { ScareIntensity.Standard, ScareIntensity.Reduced })
            {
                var d = New(mode); Drain(d); Window(d); Drain(d);
                d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position);
                Assert.IsFalse(d.IsGateReleased(GateId.Act1Door)); // No lure yet.
                d.Tick(2, true, Vector3.zero); Drain(d);
                var window = WardLayout.A1_DoorWindow.Position; window.y = 0;
                d.ReportZombiePosition(1, window + Vector3.right * 3.99f);
                Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
                d.ReportZombiePosition(1, window + Vector3.right * 4); d.Tick(.01f, true, Vector3.zero);
                Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
                Assert.AreEqual(1, Drain(d).FindAll(c => c.Kind == DirectorCommandKind.Gate).Count);
            }
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
            var expected = new[] { CueId.HeartbeatWake, CueId.PaGarbledBreach, CueId.ZombieGlassTap, CueId.WindowSlam, CueId.DistantCrashLure,
                CueId.DistantCrashLure, CueId.PowerThrum, CueId.BreakerOverload, CueId.PaCarrierWarning,
                CueId.BeaconChirp, CueId.BeaconChirp, CueId.RestraintSnap, CueId.ZombieLunge, CueId.PaPatientZero, CueId.RevealDrone };
            Assert.AreEqual(expected.Length, cues.Count);
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], cues[i], "beat " + i);
            Assert.AreEqual(2, d.HardScares);
        }
        [Test] public void ReducedRunHasZeroHardScaresAndNoLunge()
        {
            var cues = GoldenRun(ScareIntensity.Reduced, out var d);
            Assert.AreEqual(0, d.HardScares); Assert.IsFalse(cues.Contains(CueId.WindowSlam));
            Assert.IsFalse(cues.Contains(CueId.ZombieLunge)); Assert.IsTrue(cues.Contains(CueId.WindowBreath));
        }
        [Test] public void ActOneDoorGateNeedsScareLureAndFourMetreClearance()
        {
            var d = New(); Drain(d); d.Complete(ObjectiveId.Act1_DoorOpened);
            d.Complete(ObjectiveId.Act1_DoorUnbarred); Assert.IsFalse(d.CanOpenAct1Door);
            d.Tick(16, true, WardLayout.A1_Start.Position); Assert.AreEqual(ActId.Awakening, d.Act); Drain(d);
            d.ReportZombiePosition(1, WardLayout.A1_DoorWindow.Position); d.Tick(.1f, true, WardLayout.A1_Start.Position); Drain(d);
            d.Tick(1.9f, true, WardLayout.A1_Start.Position); Assert.IsFalse(d.CanOpenAct1Door); Drain(d);
            d.Tick(.2f, true, WardLayout.A1_Start.Position); d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position);
            Assert.IsTrue(d.CanOpenAct1Door); d.Tick(.1f, true, WardLayout.A1_Start.Position);
            Assert.AreEqual(ActId.Awakening, d.Act, "An early door completion must be reported again after release");
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            d.Complete(ObjectiveId.Act1_DoorOpened); Assert.AreEqual(ActId.Machinery, d.Act);
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
            var d = New(); Drain(d); Window(d); Drain(d);
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
            var d = New(); Window(d); Drain(d); Assert.AreEqual(1, d.HardScares);
            d.StartRun(ActId.Awakening, true); Drain(d); Window(d); Drain(d); Assert.AreEqual(1, d.HardScares);
        }
        [Test] public void EndingOrdersKneelPaRevealDroneThenComplete()
        {
            var d = New(); d.StartRun(ActId.Exit, true); Drain(d);
            d.Complete(ObjectiveId.Act4_ReleasePulled); d.Complete(ObjectiveId.Act4_ExitOpened);
            Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.State, (int)GameState.Ending));
            d.Tick(1, true, Vector3.zero); Assert.IsTrue(Contains(Drain(d), DirectorCommandKind.Zombie, 4));
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
            Window(d); Drain(d); d.Tick(2, true, WardLayout.A1_Start.Position); Drain(d);
            d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position);
            d.Tick(.1f, true, WardLayout.A1_Start.Position);
            var events = Drain(d);
            Assert.AreEqual(1, events.FindAll(c => c.Kind == DirectorCommandKind.Gate && c.Id == (int)GateId.Act1Door).Count);
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            Assert.IsFalse(d.IsGateReleased(GateId.None));
            for (int n = 0; n < 20; n++) { d.Tick(.1f, true, Vector3.zero); Assert.IsFalse(Drain(d).Exists(c => c.Kind == DirectorCommandKind.Gate)); }
            // Models a door enabled after the signal: the query remains true without replaying an event.
            Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            d.Complete(ObjectiveId.Act1_DoorOpened);
            Assert.AreEqual(ActId.Machinery, d.Act); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
        }
        [Test] public void RestoreClosesGateAndAllowsExactlyOneFreshRelease()
        {
            var d = New(); Drain(d);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (attempt != 0) { d.StartRun(ActId.Awakening, true); Drain(d); }
                Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
                Window(d); Drain(d); d.Tick(2, true, Vector3.zero); Drain(d);
                d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position); d.Tick(.1f, true, Vector3.zero);
                Assert.AreEqual(1, Drain(d).FindAll(c => c.Kind == DirectorCommandKind.Gate).Count);
                Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
            }
            d.StartRun(ActId.Awakening, false); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
        }
        [Test] public void GateCannotReleaseBeforeFourMetreClearanceOrWhilePaused()
        {
            var d = New(); Drain(d); Window(d); Drain(d); d.Tick(2, true, Vector3.zero); Drain(d);
            d.ReportZombiePosition(1, WardLayout.A1_DoorWindow.Position + Vector3.right * 3.99f);
            d.Tick(.1f, true, Vector3.zero); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door)); Drain(d);
            d.ReportZombiePosition(1, WardLayout.Z1_WalkTo.Position);
            d.Tick(20, false, Vector3.zero); Assert.IsFalse(d.IsGateReleased(GateId.Act1Door));
            d.Tick(.1f, true, Vector3.zero); Assert.IsTrue(d.IsGateReleased(GateId.Act1Door));
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
