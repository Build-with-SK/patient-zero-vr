using System;
using NUnit.Framework;
using PatientZero.AI.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI.Tests
{
    public sealed class ZombieLogicTests
    {
        private static NoiseEvent Noise(float x, float radius = 6, float time = 0, NoiseOrigin origin = NoiseOrigin.Player) =>
            new NoiseEvent(Vector3.right * x, radius, CueId.ImpactHeavy, origin, time);
        private static ZombieBrain Brain(int id = 2) => new ZombieBrain(id, Vector3.zero, new System.Random(42), Vector3.forward * 3, Vector3.back * 3);
        private static PerceptionFrame Frame(float playerX = 5, bool failed = false, bool clear = true, float speed = 0, LightingState lighting = LightingState.WardDim) =>
            new PerceptionFrame(Vector3.zero, Vector3.right, Vector3.right * playerX, speed, 0, clear, failed, lighting);
        [Test] public void HearingBoundaryMultiplierAndDoorOcclusion()
        {
            Assert.IsTrue(Hearing.CanHear(Noise(6), Vector3.zero, 1, 1, 0));
            Assert.IsFalse(Hearing.CanHear(Noise(6.01f), Vector3.zero, 1, 1, 0));
            Assert.IsTrue(Hearing.CanHear(Noise(7), Vector3.zero, 1.2f, 1, 0));
            Assert.IsFalse(Hearing.CanHear(Noise(4), Vector3.zero, 1, .5f, 0));
        }
        [Test] public void HarmlessOriginsInvalidAndStaleNoiseAreIgnored()
        {
            Assert.IsFalse(Hearing.CanHear(Noise(0, origin: NoiseOrigin.Zombie), Vector3.zero, 1, 1, 0));
            Assert.IsFalse(Hearing.CanHear(Noise(0, origin: NoiseOrigin.Ambient), Vector3.zero, 1, 1, 0));
            Assert.IsFalse(Hearing.CanHear(Noise(0, 0), Vector3.zero, 1, 1, 0));
            Assert.IsFalse(Hearing.CanHear(Noise(float.NaN), Vector3.zero, 1, 1, 0));
            Assert.IsFalse(Hearing.CanHear(Noise(0), Vector3.zero, 1, 1, 2));
            Assert.IsFalse(Hearing.CanHear(Noise(0, time: 2), Vector3.zero, 1, 1, 1));
        }
        [Test] public void SoundRequiresSniffTelegraphBeforeInvestigating()
        {
            var b = Brain(); Assert.IsTrue(b.Hear(Noise(4), Vector3.zero, 1, 0, true));
            Assert.AreEqual(AlertLevel.Suspicious, b.Level);
            var output = b.Tick(.1f, .1f, true, Frame()); Assert.IsTrue((output & ZombieSignals.Sniff) != 0);
            b.Tick(.59f, .49f, true, Frame()); Assert.AreEqual(AlertLevel.Suspicious, b.Level);
            b.Tick(.61f, .02f, true, Frame()); Assert.AreEqual(AlertLevel.Investigating, b.Level);
        }
        [Test] public void ReducedTelegraphIsLongerAndSightIsOff()
        {
            var b = Brain(); b.Reduced = true; b.Hear(Noise(4), Vector3.zero, 1, 0, true);
            b.Tick(.7f, .7f, true, Frame(speed: 5)); Assert.AreEqual(AlertLevel.Suspicious, b.Level);
            b.Tick(1.21f, .51f, true, Frame(speed: 5)); Assert.AreEqual(AlertLevel.Investigating, b.Level);
        }
        [Test] public void SearchTimeoutReturnsHomeWithGroan()
        {
            var b = Brain(); b.Hear(Noise(4), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame());
            b.Tick(.7f, .1f, true, Frame(failed: true)); Assert.AreEqual(AlertLevel.Searching, b.Level);
            var output = b.Tick(5.8f, 5.1f, true, Frame()); Assert.AreEqual(AlertLevel.Unaware, b.Level);
            Assert.IsTrue((output & ZombieSignals.Groan) != 0); Assert.AreEqual(Vector3.zero, b.Target);
        }
        [Test] public void InvestigationHasBoundedTimeoutEvenWithoutArrival()
        {
            var b = Brain(); b.Hear(Noise(4), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame());
            b.Tick(10.7f, 10.1f, true, Frame()); Assert.AreEqual(AlertLevel.Searching, b.Level);
        }
        [Test] public void RetargetHysteresisSuppressesJitterButAcceptsLouderOrLater()
        {
            var b = Brain(); b.Hear(Noise(4), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame());
            Assert.IsFalse(b.Hear(Noise(-5, time: .7f), Vector3.zero, 1, .7f, true)); Assert.AreEqual(4, b.Target.x);
            Assert.IsTrue(b.Hear(Noise(-3, time: .8f), Vector3.zero, 1, .8f, true)); Assert.AreEqual(-3, b.Target.x);
            Assert.IsTrue(b.Hear(Noise(5, time: 2.4f), Vector3.zero, 1, 2.4f, true));
        }
        [Test] public void SnarlPrecedesAnyChaseAndCatchIsOneShot()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame(.5f));
            var output = b.Tick(.7f, .1f, true, Frame(.5f));
            Assert.AreEqual(AlertLevel.Chasing, b.Level); Assert.IsTrue((output & ZombieSignals.Snarl) != 0);
            Assert.IsFalse((b.Tick(.8f, .1f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.IsTrue((b.Tick(1, .2f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.AreEqual(ZombieSignals.None, b.Tick(1.1f, .1f, true, Frame(.5f)));
        }
        [Test] public void WallOrRetreatBreaksContinuousCatchTimer()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            b.Tick(.8f, .1f, true, Frame(.5f)); b.Tick(.9f, .1f, true, Frame(.5f, clear: false));
            Assert.IsFalse((b.Tick(1, .1f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.IsFalse((b.Tick(1.1f, .1f, true, Frame(3)) & ZombieSignals.Caught) != 0);
        }
        [Test] public void FrozenPlayerAndBlackoutAreNeverSeen()
        {
            var b = Brain(); b.Tick(.1f, .1f, true, Frame()); Assert.AreEqual(AlertLevel.Unaware, b.Level);
            b.Tick(.2f, .1f, true, Frame(speed: 5, lighting: LightingState.Blackout)); Assert.AreEqual(AlertLevel.Unaware, b.Level);
            b.Tick(.3f, .1f, true, Frame(speed: 5)); Assert.AreEqual(AlertLevel.Suspicious, b.Level);
        }
        [Test] public void MovingPlayerOutsideViewConeIsNotSeen()
        {
            var b = Brain(); var frame = new PerceptionFrame(Vector3.zero, Vector3.forward, Vector3.right * 3, 2);
            b.Tick(.1f, .1f, true, frame); Assert.AreEqual(AlertLevel.Unaware, b.Level);
        }
        [Test] public void ChaseExpiresWhenSilentPlayerTeleportsAway()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            b.Tick(3.1f, 2.4f, true, Frame(12)); Assert.AreEqual(AlertLevel.Searching, b.Level);
        }
        [Test] public void ScriptedObjectDistractionRedirectsChase()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            Assert.IsTrue(b.Hear(Noise(-4, 20, .8f, NoiseOrigin.Scripted), Vector3.zero, 1, .8f, true));
            Assert.AreEqual(AlertLevel.Investigating, b.Level); Assert.AreEqual(-4, b.Target.x);
        }
        [Test] public void PauseStunAndPassiveFreezeSensesAndTimers()
        {
            var b = Brain(); Assert.IsFalse(b.Hear(Noise(1), Vector3.zero, 1, 0, false));
            b.Hear(Noise(4), Vector3.zero, 1, 0, true); b.Tick(.5f, .5f, false, Frame()); Assert.AreEqual(AlertLevel.Suspicious, b.Level);
            b.Stun(2.5f); Assert.IsFalse(b.Hear(Noise(1, time: .5f), Vector3.zero, 1, .5f, true));
            b.Tick(1, .5f, true, Frame()); Assert.AreEqual(AlertLevel.Suspicious, b.Level);
            b.SetMode(ZombieMode.Passive); Assert.IsFalse(b.Hear(Noise(1, time: 1), Vector3.zero, 1, 1, true));
            Assert.AreEqual(ZombieSignals.None, b.Tick(2, 1, true, Frame(speed: 5)));
        }
        [Test] public void RestrainedRootNeverMovesBeyondChainAndCatchRequiresRelease()
        {
            var b = Brain(4); var frame = Frame(.5f);
            Assert.IsFalse((b.Tick(1, 1, true, frame) & ZombieSignals.Caught) != 0);
            Assert.IsFalse(b.HasMoveTarget); Assert.AreEqual(Vector3.zero, b.Target);
            b.ReleaseRestraint(); Assert.IsTrue((b.Tick(2, 1, true, frame) & ZombieSignals.Caught) != 0);
        }
        [Test] public void ReducedCatchNeedsHalfSecondAndSmallerRadius()
        {
            var b = Brain(4); b.Reduced = true; b.ReleaseRestraint();
            Assert.IsFalse((b.Tick(.3f, .3f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.IsTrue((b.Tick(.51f, .21f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
        }
        [Test] public void BackwardClockAndInvalidPositionsCannotRetarget()
        {
            var b = Brain(); b.Hear(Noise(4), Vector3.zero, 1, 0, true); b.Tick(1, 1, true, Frame());
            Assert.IsFalse(b.Hear(Noise(-3, time: .5f), Vector3.zero, 1, .5f, true));
            Assert.IsFalse(b.Hear(Noise(float.NaN, time: 1), Vector3.zero, 1, 1, true)); Assert.AreEqual(4, b.Target.x);
        }
        [Test] public void HeadHeightSoundUsesGroundDistanceForArrival()
        {
            var b = Brain(); var noise = new NoiseEvent(Vector3.up * 1.7f, 6, CueId.PlugSpark, NoiseOrigin.Player, 0);
            b.Hear(noise, Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            Assert.AreEqual(AlertLevel.Searching, b.Level);
        }
    }
}
