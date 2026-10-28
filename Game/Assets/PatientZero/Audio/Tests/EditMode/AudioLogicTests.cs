using System;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Audio.Logic;
using UnityEngine;

namespace PatientZero.Audio.Tests
{
    public sealed class AudioLogicTests
    {
        private static CueDefinition Row(CueId id) { Assert.IsTrue(CueTable.TryGet(id, out var row)); return row; }
        [Test] public void EveryContractIdHasExactlyOneRow()
        {
            foreach (CueId id in Enum.GetValues(typeof(CueId)))
            {
                int count = 0;
                for (int i = 0; i < CueTable.Count; i++) if (CueTable.At(i).Id == id) count++;
                Assert.AreEqual(1, count, id.ToString());
            }
            Assert.AreEqual(Enum.GetValues(typeof(CueId)).Length, CueTable.Count);
        }
        [Test] public void WorldCuesReachSilenceAndCoverTheirGameplayRadius()
        {
            for (int i = 0; i < CueTable.Count; i++)
            {
                var r = CueTable.At(i); Assert.Less(r.Min, r.Max); Assert.LessOrEqual(r.NoiseRadius, r.Max);
                Assert.AreEqual(0, r.Doppler);
                if (!r.World) continue;
                Assert.AreEqual(1, r.GainAt(r.Min)); Assert.AreEqual(0, r.GainAt(r.Max));
                Assert.AreEqual(0, r.GainAt(r.Max + 10));
            }
        }
        [Test] public void ZombieCuesAreAudibleBeforeCommonPlayerNoise()
        {
            for (int i = 0; i < CueTable.Count; i++)
            { var r = CueTable.At(i); if ((int)r.Id >= 500 && (int)r.Id < 600) Assert.GreaterOrEqual(r.Max, 9, r.Id.ToString()); }
        }
        [Test] public void RequiredCaptionsAndAllSpeechHaveText()
        {
            for (int i = 0; i < CueTable.Count; i++)
            {
                var r = CueTable.At(i);
                if (r.CaptionPolicy == CaptionPolicy.Always || r.Bus == MixerBus.Voice) Assert.IsFalse(string.IsNullOrWhiteSpace(r.Caption));
            }
            var settings = ExperienceSettings.Defaults; settings.Subtitles = false; settings.SoundCueVisuals = false;
            Assert.IsTrue(Row(CueId.BeaconChirp).ShouldCaption(settings));
            Assert.IsFalse(Row(CueId.PaPatientZero).ShouldCaption(settings));
            Assert.IsFalse(Row(CueId.ImpactHeavy).ShouldCaption(settings));
        }
        [Test] public void ApprovedLoopsAndZeroNoiseLandmarksArePreserved()
        {
            foreach (var cue in new[] { CueId.FluorescentBuzz, CueId.PlankCreak, CueId.ValveSqueal, CueId.CableDrag,
                CueId.MonitorBeep, CueId.WaterDrip, CueId.AlarmKlaxon, CueId.RestraintStrain, CueId.ZombieBreath,
                CueId.ZombieShuffle, CueId.ReleaseBarGrind, CueId.RoomTone, CueId.Ventilation, CueId.TensionDrone, CueId.BlackoutTone, CueId.ExteriorWind })
                Assert.IsTrue(Row(cue).Loop, cue.ToString());
            foreach (var cue in new[] { CueId.PaGarbledBreach, CueId.MonitorBeep, CueId.BeaconChirp, CueId.AlarmKlaxon }) Assert.AreEqual(0, Row(cue).NoiseRadius);
            Assert.IsFalse(Row(CueId.BeaconChirp).Loop);
        }
        [Test] public void VoiceCapsAndHrtfFallbackAreBounded()
        {
            var budget = new VoiceBudget();
            for (int i = 0; i < 24; i++) Assert.IsTrue(budget.Start(Row(CueId.RoomTone), true).Accepted);
            Assert.AreEqual(24, budget.ActiveCount); Assert.AreEqual(0, budget.SpatialCount);
            budget = new VoiceBudget();
            for (int i = 0; i < 24; i++) budget.Start(Row(CueId.ZombieBreath), true);
            Assert.AreEqual(12, budget.SpatialCount); Assert.AreEqual(24, budget.ActiveCount);
        }
        [Test] public void StealingSelectsLowestPriorityThenOldestAndReportsCue()
        {
            var b = new VoiceBudget();
            var oldest = b.Start(Row(CueId.RoomTone), false);
            var second = b.Start(Row(CueId.Ventilation), false);
            for (int i = 2; i < 24; i++) b.Start(Row(CueId.ZombieBreath), true);
            var admitted = b.Start(Row(CueId.ZombieSnarl), true);
            Assert.IsTrue(admitted.Accepted); Assert.AreEqual(CueId.RoomTone, admitted.Stolen); Assert.AreEqual(-1, b.Find(oldest.Handle));
            Assert.GreaterOrEqual(b.Find(second.Handle), 0); Assert.AreEqual(24, b.ActiveCount);
        }
        [Test] public void UiAndSpeechCannotBeStolenAndRejectionIsReported()
        {
            var b = new VoiceBudget();
            for (int i = 0; i < 24; i++) b.Start(Row(i % 2 == 0 ? CueId.UiSelect : CueId.PaPatientZero), false);
            Assert.IsFalse(b.Start(Row(CueId.ZombieSnarl), false).Accepted);
            Assert.AreEqual(1, b.RejectedCount); Assert.AreEqual(0, b.StolenCount);
        }
        [Test] public void ReleasedHandleNeverAddressesAReplacement()
        {
            var b = new VoiceBudget(); var a = b.Start(Row(CueId.RoomTone), false); b.Release(a.Handle);
            var next = b.Start(Row(CueId.RoomTone), false); b.Release(a.Handle);
            Assert.AreNotEqual(a.Handle, next.Handle); Assert.GreaterOrEqual(b.Find(next.Handle), 0);
        }
        private static NoiseEvent Noise(float time = 0, float radius = 6) => new NoiseEvent(Vector3.zero, radius, CueId.ImpactHeavy, NoiseOrigin.Player, time);
        [Test] public void PortedNoiseCooldownAndNestedDispatchGuards()
        {
            var gate = new NoiseGate(); Assert.IsTrue(gate.TryEnter(Noise(), 1, 0));
            Assert.IsFalse(gate.TryEnter(Noise(), 2, 0)); gate.Exit();
            Assert.IsFalse(gate.TryEnter(Noise(.1f), 1, .1f));
            Assert.IsTrue(gate.TryEnter(Noise(.2f), 1, .2f)); gate.Exit();
        }
        [Test] public void PortedNoiseCapacityDoesNotEvictUnexpiredSources()
        {
            var gate = new NoiseGate(1); Assert.IsTrue(gate.TryEnter(Noise(), 1, 0)); gate.Exit();
            Assert.IsFalse(gate.TryEnter(Noise(.01f), 2, .01f)); Assert.IsFalse(gate.TryEnter(Noise(.02f), 1, .02f));
            Assert.IsTrue(gate.TryEnter(Noise(.2f), 2, .2f)); gate.Exit();
        }
        [Test] public void PortedNoiseRejectsStaleFutureZeroAndInvalidInputs()
        {
            var gate = new NoiseGate();
            Assert.IsFalse(gate.TryEnter(Noise(), 1, 2)); Assert.IsFalse(gate.TryEnter(Noise(2), 1, 1));
            Assert.IsFalse(gate.TryEnter(Noise(radius: 0), 1, 0)); Assert.IsFalse(gate.TryEnter(Noise(float.NaN), 1, 0));
            Assert.IsFalse(gate.TryEnter(Noise(), 1, float.NaN));
        }
        [Test] public void MissingCueHasNoDefinitionAndNoneIsSilent()
        { Assert.IsFalse(CueTable.TryGet((CueId)999999, out _)); Assert.AreEqual(0, Row(CueId.None).NoiseRadius); }
        [Test] public void GuidanceCurvesCoverApprovedWardAnchorsAndSilenceBeyondTheirRange()
        {
            var beacon = Row(CueId.BeaconChirp);
            foreach (var p in WardLayout.CriticalPath)
                if (p.Act == ActId.Darkness)
                    Assert.Greater(beacon.GainAt(Vector3.Distance(p.Position, WardLayout.A3_Beacon.Position)), 0, p.Id);
            var alarm = Row(CueId.AlarmKlaxon);
            Assert.Greater(alarm.GainAt(Vector3.Distance(WardLayout.A4_Release.Position, WardLayout.A4_ExitDoor.Position)), 0);
            foreach (var p in WardLayout.Points)
                Assert.AreEqual(0, beacon.GainAt(beacon.Max + Vector3.Distance(p.Position, WardLayout.A3_Beacon.Position) + 1));
        }
    }
}
