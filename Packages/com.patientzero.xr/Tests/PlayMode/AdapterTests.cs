using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.XR;
using UnityEngine;
using UnityEngine.TestTools;

namespace PatientZero.Tests
{
    public sealed class AdapterTests
    {
        [UnityTest]
        public IEnumerator RealNoiseManagerDeliversBottleAndThrottlesImpacts()
        {
            var root = new GameObject("TestNoise");
            try
            {
                var manager = root.AddComponent<NoiseManager>();
                var listener = new Listener(); manager.Bus.Register(listener);
                Assert.That(manager.Emit(Vector3.zero, 1, 10, NoiseCategory.Bottle, 42), Is.True);
                Assert.That(manager.Emit(Vector3.zero, 1, 10, NoiseCategory.Bottle, 42), Is.False);
                Assert.That(listener.Count, Is.EqualTo(1));
                yield return null;
            }
            finally { Object.Destroy(root); }
        }
        private sealed class Listener : INoiseListener
        {
            public int Count;
            public void Hear(in NoiseEvent noise) => Count++;
        }
        [UnityTest]
        public IEnumerator ActiveZombieBudgetIsIdempotentAndReleasesSlots()
        {
            var root = new GameObject("Budget"); root.SetActive(false);
            try
            {
                var manager = root.AddComponent<NoiseManager>(); manager.MaxActiveZombies = 1; root.SetActive(true);
                var a = new Listener(); var b = new Listener();
                Assert.That(manager.TryAdmitZombie(a), Is.True);
                Assert.That(manager.TryAdmitZombie(a), Is.True);
                Assert.That(manager.ActiveZombies, Is.EqualTo(1));
                Assert.That(manager.TryAdmitZombie(b), Is.False);
                manager.ReleaseZombie(b); Assert.That(manager.ActiveZombies, Is.EqualTo(1));
                manager.ReleaseZombie(a); Assert.That(manager.TryAdmitZombie(b), Is.True);
                yield return null;
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest]
        public IEnumerator GuardInvokesCancelAndDisablesSelectorsOnLoss()
        {
            var root = new GameObject("TestTracking");
            try
            {
                root.SetActive(false);
                var hand = root.AddComponent<Light>();
                var guard = root.AddComponent<TrackingLossGuard>();
                guard.HandInteractors = new Behaviour[] { hand }; guard.StableTrackingSeconds = 0;
                int canceled = 0; guard.OnCancelSelection.AddListener(() => canceled++);
                root.SetActive(true); guard.SetFocus(true); guard.Submit(true, false);
                Assert.That(hand.enabled, Is.True);
                guard.Submit(false, false);
                Assert.That(hand.enabled, Is.False); Assert.That(canceled, Is.EqualTo(1));
                guard.Submit(false, false); Assert.That(canceled, Is.EqualTo(1));
                yield return null;
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest]
        public IEnumerator UnsafeReleaseClearsThrowVelocity()
        {
            var root = new GameObject("TestHeld");
            try
            {
                var body = root.AddComponent<Rigidbody>(); body.useGravity = false;
                var safety = root.AddComponent<HeldObjectSafety>(); safety.SelectionBegan();
                body.linearVelocity = Vector3.one * 8;
                safety.CancelForTrackingLoss();
                Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(safety.Selected, Is.False);
                yield return new WaitForFixedUpdate();
                Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest]
        public IEnumerator StoppedTrackingProviderFailsClosed()
        {
            var root = new GameObject("TestStaleInput"); root.SetActive(false);
            try
            {
                var hand = root.AddComponent<Light>(); var guard = root.AddComponent<TrackingLossGuard>();
                guard.HandInteractors = new Behaviour[] { hand }; guard.StableTrackingSeconds = 0;
                root.SetActive(true); guard.SetFocus(true); guard.Submit(true, false);
                Assert.That(hand.enabled, Is.True);
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(hand.enabled, Is.False);
            }
            finally { Object.Destroy(root); }
        }
        [UnityTest]
        public IEnumerator CloseEncounterDoesNotInvokeReveal()
        {
            var root = new GameObject("Encounter"); var player = new GameObject("Player");
            try
            {
                var sequence = root.AddComponent<EncounterSequence>(); sequence.Listener = player.transform;
                sequence.Beats = new[] { new EncounterBeat { PlayAudio = false } };
                int count = 0; sequence.Beats[0].OnBeat.AddListener(() => count++);
                Assert.That(sequence.TryBegin(), Is.False); yield return null;
                Assert.That(count, Is.Zero);
            }
            finally { Object.Destroy(root); Object.Destroy(player); }
        }
        [UnityTest]
        public IEnumerator MissingAudioClipDoesNotThrow()
        {
            var root = new GameObject("TestAudio");
            try
            {
                var audio = root.AddComponent<SpatialAudioSystem>();
                LogAssert.Expect(LogType.Warning, "Patient Zero audio assets missing; cue skipped safely.");
                Assert.That(audio.Play(AudioCue.Growl, Vector3.zero), Is.False);
                yield return null;
            }
            finally { Object.Destroy(root); }
        }
    }
}
