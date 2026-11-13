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
        /// <summary>The sight rules, kept for the later lab strain (E-22); every zombie in the two-minute version is blind.</summary>
        private static ZombieBrain Sighted(int id = 2) { var b = Brain(id); b.Sighted = true; return b; }
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
        [Test] public void GunshotIsRunToAndFollowedLonger()
        {
            // E-28: an ordinary sound is walked to and given up after 10 s; a gunshot is answered with a snarl and run to, for up
            // to GunshotRushSeconds, and a second shot sends it on to the new spot with the time started again.
            var walk = Brain(); walk.Hear(Noise(5), Vector3.zero, 1, 0, true);
            walk.Tick(.7f, .7f, true, Frame(30));
            Assert.AreEqual(AlertLevel.Investigating, walk.Level); Assert.IsFalse(walk.Rushing);
            walk.Tick(11, 10.3f, true, Frame(30)); Assert.AreEqual(AlertLevel.Searching, walk.Level);

            var run = Brain(); var shot = new NoiseEvent(Vector3.right * 15, 20, CueId.RevolverShot, NoiseOrigin.Player, 0);
            Assert.IsTrue(run.Hear(in shot, Vector3.zero, 1, 0, true));
            Assert.AreEqual(AlertLevel.Suspicious, run.Level); Assert.IsFalse(run.Rushing, "the snarl comes first");
            var output = run.Tick(.1f, .1f, true, Frame(30)); Assert.IsTrue((output & ZombieSignals.Snarl) != 0);
            run.Tick(.7f, .6f, true, Frame(30));
            Assert.AreEqual(AlertLevel.Investigating, run.Level); Assert.IsTrue(run.Rushing);
            run.Tick(12, 11.3f, true, Frame(30)); Assert.AreEqual(AlertLevel.Investigating, run.Level);

            var second = new NoiseEvent(Vector3.right * -10, 20, CueId.RevolverShot, NoiseOrigin.Player, 12);
            Assert.IsTrue(run.Hear(in second, Vector3.zero, 1, 12, true));
            Assert.AreEqual(Vector3.right * -10, run.Target);
            run.Tick(30, 18, true, Frame(30)); Assert.AreEqual(AlertLevel.Investigating, run.Level); Assert.IsTrue(run.Rushing);
            run.Tick(38, 8, true, Frame(30)); Assert.AreEqual(AlertLevel.Searching, run.Level); Assert.IsFalse(run.Rushing);
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
            b.Hear(Noise(.5f, time: .75f), Vector3.zero, 1, .75f, true); // a footstep: the player is still moving
            Assert.IsFalse((b.Tick(.8f, .1f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.IsTrue((b.Tick(1, .2f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.AreEqual(ZombieSignals.None, b.Tick(1.1f, .1f, true, Frame(.5f)));
        }
        /// <summary>E-22 freeze or die: in reach and chasing, but the player has made no sound for over a second.</summary>
        [Test] public void AFrozenPlayerInReachIsNeverGrabbedAndIsLost()
        {
            // It hears one step at 1 m, then closes in; the player has stood still since that step.
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame(1.5f));
            b.Tick(.7f, .1f, true, Frame(1.5f)); Assert.AreEqual(AlertLevel.Chasing, b.Level);
            for (int i = 8; i <= 30; i++)
            {
                float t = i * .1f;
                var signals = b.Tick(t, .1f, true, Frame(t < 1.2f ? 1.5f : .5f));
                Assert.IsFalse((signals & ZombieSignals.Caught) != 0, "grabbed at " + t);
            }
            Assert.AreEqual(AlertLevel.Searching, b.Level, "three silent seconds and the chase is over");
        }
        [Test] public void MovingAgainInReachIsGrabbed()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame(.5f));
            b.Tick(.7f, .1f, true, Frame(.5f)); b.Tick(1.8f, 1.1f, true, Frame(.5f));
            Assert.AreEqual(AlertLevel.Chasing, b.Level);
            Assert.IsTrue(b.Hear(Noise(.5f, 2.5f, 1.85f), Vector3.zero, 1, 1.85f, true));
            b.Tick(1.9f, .1f, true, Frame(.5f));
            Assert.IsTrue((b.Tick(2.2f, .3f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
        }
        /// <summary>E-22: sight is off for every zombie, so a moving player in plain view is not noticed until heard.</summary>
        [Test] public void BlindZombiesNeverNoticeAMovingPlayerInView()
        {
            var b = Brain();
            for (int i = 1; i <= 30; i++) b.Tick(i * .1f, .1f, true, Frame(2, speed: 5));
            Assert.AreEqual(AlertLevel.Unaware, b.Level);
        }
        /// <summary>E-22 door zombie: deaf while it screams, then searches; quiet means it groans and gives up.</summary>
        [Test] public void ConfrontIgnoresTheScreamThenLosesAQuietPlayer()
        {
            var b = Brain(1); var door = Vector3.forward * 1.5f;
            b.Confront(door, 0); Assert.AreEqual(AlertLevel.Searching, b.Level); Assert.AreEqual(door, b.Target);
            Assert.IsFalse(b.Hear(Noise(.2f, 3, .5f), Vector3.zero, 1, .5f, true), "deaf while screaming");
            var groan = ZombieSignals.None;
            for (int i = 1; i <= 60; i++) groan |= b.Tick(i * .1f, .1f, true, Frame(.2f));
            Assert.AreEqual(AlertLevel.Unaware, b.Level); Assert.IsTrue((groan & ZombieSignals.Groan) != 0);
            Assert.IsFalse((groan & ZombieSignals.Caught) != 0);
        }
        [Test] public void ConfrontAttacksAPlayerWhoMoves()
        {
            var b = Brain(1); b.Confront(Vector3.forward * 1.2f, 0); b.Tick(.9f, .9f, true, Frame(.5f));
            Assert.IsTrue(b.Hear(Noise(.5f, 2.5f, 1), Vector3.zero, 1, 1, true));
            Assert.AreEqual(AlertLevel.Chasing, b.Level, "a step right in front of it is an attack, not a sniff");
            var caught = ZombieSignals.None;
            for (int i = 1; i <= 5; i++) { b.Hear(Noise(.5f, 2.5f, 1 + i * .1f), Vector3.zero, 1, 1 + i * .1f, true); caught |= b.Tick(1 + i * .1f, .1f, true, Frame(.5f)); }
            Assert.IsTrue((caught & ZombieSignals.Caught) != 0);
        }
        /// <summary>E-22 struggle: the zombie lets go, is stunned, and must hear the player again before it can grab again.</summary>
        [Test] public void BreakFreeReleasesStunsAndNeedsAFreshNoise()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame(.5f)); b.Tick(.7f, .1f, true, Frame(.5f));
            b.Hear(Noise(.5f, time: .8f), Vector3.zero, 1, .8f, true); b.Tick(.8f, .1f, true, Frame(.5f));
            Assert.IsTrue((b.Tick(1.1f, .3f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            b.BreakFree(3); Assert.IsTrue(b.IsStunned); Assert.AreEqual(AlertLevel.Searching, b.Level);
            Assert.IsFalse(b.Hear(Noise(.5f, time: 1.5f), Vector3.zero, 1, 1.5f, true), "stunned");
            for (float t = 1.2f; t < 4.6f; t += .1f) Assert.IsFalse((b.Tick(t, .1f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.IsFalse(b.IsStunned);
            b.BreakFree(3); Assert.IsFalse(b.IsStunned, "only a zombie that is holding the player can be fought off");
        }
        [Test] public void WallOrRetreatBreaksContinuousCatchTimer()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            b.Hear(Noise(.5f, time: .75f), Vector3.zero, 1, .75f, true); // still moving: only the wall and the retreat stop the grab
            b.Tick(.8f, .1f, true, Frame(.5f)); b.Tick(.9f, .1f, true, Frame(.5f, clear: false));
            Assert.IsFalse((b.Tick(1, .1f, true, Frame(.5f)) & ZombieSignals.Caught) != 0);
            Assert.IsFalse((b.Tick(1.1f, .1f, true, Frame(3)) & ZombieSignals.Caught) != 0);
        }
        [Test] public void FrozenPlayerAndBlackoutAreNeverSeen()
        {
            var b = Sighted(); b.Tick(.1f, .1f, true, Frame()); Assert.AreEqual(AlertLevel.Unaware, b.Level);
            b.Tick(.2f, .1f, true, Frame(speed: 5, lighting: LightingState.Blackout)); Assert.AreEqual(AlertLevel.Unaware, b.Level);
            b.Tick(.3f, .1f, true, Frame(speed: 5)); Assert.AreEqual(AlertLevel.Suspicious, b.Level);
        }
        [Test] public void MovingPlayerOutsideViewConeIsNotSeen()
        {
            var b = Sighted(); var frame = new PerceptionFrame(Vector3.zero, Vector3.forward, Vector3.right * 3, 2);
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
        [Test] public void PatrolPausesAtBothEndsAndResumes()
        {
            var b = Brain(3); b.SetMode(ZombieMode.Patrol);
            var south = new PerceptionFrame(Vector3.back * 3, Vector3.forward, Vector3.right * 20);
            b.Tick(.1f, .1f, true, south); Assert.IsFalse(b.HasMoveTarget);
            b.Tick(3.1f, 3, true, south); b.Tick(3.2f, .1f, true, south);
            Assert.AreEqual(Vector3.forward * 3, b.Target); Assert.IsTrue(b.HasMoveTarget);
        }
        [Test] public void MovingSightEscalationIncludesSnarl()
        {
            var b = Sighted(); b.Hear(Noise(4), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame());
            var signals = b.Tick(.7f, .1f, true, Frame(speed: 1));
            Assert.AreEqual(AlertLevel.Chasing, b.Level); Assert.IsTrue((signals & ZombieSignals.Snarl) != 0);
        }
        [Test] public void ChasePathFailureCannotCatchThroughAnObstacle()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            var output = b.Tick(.8f, .1f, true, Frame(.2f, failed: true));
            Assert.AreEqual(AlertLevel.Searching, b.Level); Assert.IsFalse((output & ZombieSignals.Caught) != 0);
        }
        [Test] public void PlayerOnAnotherHeightCannotBeCaughtFromSameGroundPoint()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            var high = new PerceptionFrame(Vector3.zero, Vector3.forward, Vector3.up * 4);
            Assert.IsFalse((b.Tick(1.2f, .5f, true, high) & ZombieSignals.Caught) != 0);
        }
        [Test] public void RestrainedPatientCannotCatchOnAnotherHeight()
        {
            var b = Brain(4); b.ReleaseRestraint();
            var high = new PerceptionFrame(Vector3.zero, Vector3.forward, Vector3.up * 4);
            Assert.IsFalse((b.Tick(1, 1, true, high) & ZombieSignals.Caught) != 0);
        }
        [Test] public void APlayerThrownImpactRedirectsTheLastHeardPoint()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true); b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            Assert.IsTrue(b.Hear(Noise(-4, 12, .8f), Vector3.zero, 1, .8f, true));
            b.Tick(.9f, .1f, true, Frame(8)); Assert.AreEqual(-4, b.Target.x);
        }
        [Test] public void BottleDistractsChaseWithoutRevealingTheStationaryThrower()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true);
            b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            Assert.AreEqual(AlertLevel.Chasing, b.Level);
            Assert.IsTrue(b.Hear(Noise(-4, 6, 2.3f), Vector3.zero, 1, 2.3f, true));
            Assert.AreEqual(AlertLevel.Investigating, b.Level);
            var landing = new PerceptionFrame(Vector3.left * 3, Vector3.left, Vector3.right * .2f, lineClear: false);
            b.Tick(2.4f, .1f, true, landing);
            Assert.AreEqual(AlertLevel.Investigating, b.Level); Assert.AreEqual(Vector3.left * 4, b.Target);
            landing = new PerceptionFrame(Vector3.left * 4, Vector3.left, Vector3.right * .2f, lineClear: false);
            b.Tick(2.5f, .1f, true, landing); Assert.AreEqual(AlertLevel.Searching, b.Level);
            b.Tick(7.6f, 5.1f, true, landing); Assert.AreEqual(AlertLevel.Unaware, b.Level);
        }
        [Test] public void ARemoteImpactDuringInvestigationClearsStalePlayerStimulus()
        {
            var b = Brain(); b.Hear(Noise(1), Vector3.zero, 1, 0, true);
            b.Tick(.6f, .6f, true, Frame()); Assert.AreEqual(AlertLevel.Investigating, b.Level);
            Assert.IsTrue(b.Hear(Noise(-4, 12, .65f), Vector3.zero, 1, .65f, true));
            var nearLanding = new PerceptionFrame(Vector3.left * 3, Vector3.left, Vector3.right * 5, lineClear: false);
            b.Tick(.7f, .1f, true, nearLanding); Assert.AreEqual(AlertLevel.Investigating, b.Level);
        }
        [Test] public void DistractionDoesNotOverrideHysteresisOrFreshVisibleMovement()
        {
            var b = Sighted(); b.Hear(Noise(1), Vector3.zero, 1, 0, true);
            b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            Assert.IsFalse(b.Hear(Noise(-5, 6, .8f), Vector3.zero, 1, .8f, true));
            Assert.IsTrue(b.Hear(Noise(-4, 6, 2.3f), Vector3.zero, 1, 2.3f, true));
            var seen = b.Tick(2.4f, .1f, true, Frame(4, speed: 1));
            Assert.AreEqual(AlertLevel.Chasing, b.Level); Assert.IsTrue((seen & ZombieSignals.Snarl) != 0);
        }
        [Test] public void InterruptedCatchMustBuildACompleteNewCatchTimer()
        {
            var b = Sighted(); b.Hear(Noise(1), Vector3.zero, 1, 0, true);
            b.Tick(.6f, .6f, true, Frame()); b.Tick(.7f, .1f, true, Frame());
            b.Tick(.9f, .2f, true, Frame(.2f));
            b.Hear(Noise(-4, 12, 1), Vector3.zero, 1, 1, true);
            Assert.AreEqual(AlertLevel.Investigating, b.Level);
            b.Tick(1.1f, .1f, true, Frame(4, speed: 1));
            var reacquired = b.Tick(1.2f, .1f, true, Frame(.2f));
            Assert.IsFalse((reacquired & ZombieSignals.Caught) != 0);
            b.Reset(ZombieMode.Dormant); Assert.AreEqual(AlertLevel.Unaware, b.Level); Assert.IsFalse(b.HasMoveTarget);
        }
        [Test] public void AQuietPlayerAtCatchDistanceStillDoesNotTriggerDetection()
        {
            var b = Brain(); b.Tick(1, 1, true, Frame(.2f)); Assert.AreEqual(AlertLevel.Unaware, b.Level);
        }
        [Test] public void ActiveBrainTicksAllocateZeroBytesAfterWarmup()
        {
            var b = Brain(); var f = Frame(8);
            for (int i = 0; i < 200; i++) { b.Hear(Noise(4, time: i * .1f), Vector3.zero, 1, i * .1f, true); b.Tick(i * .1f, .1f, true, f); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 200; i < 10200; i++) { b.Hear(Noise(4, time: i * .1f), Vector3.zero, 1, i * .1f, true); b.Tick(i * .1f, .1f, true, f); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0L, allocated);
        }
        [Test] public void WallsDoNotReduceGameplayHearingButClosedDoorsDo()
        {
            Assert.AreEqual(1f, Hearing.Occlusion(false, true));
            Assert.AreEqual(.5f, Hearing.Occlusion(true, true));
            var n = new NoiseEvent(Vector3.zero, 6, CueId.ImpactHeavy, NoiseOrigin.Player, 0);
            Assert.IsTrue(Hearing.CanHear(in n, Vector3.right * 5, 1, Hearing.Occlusion(false, true), 0));
            Assert.IsFalse(Hearing.CanHear(in n, Vector3.right * 5, 1, Hearing.Occlusion(true), 0));
        }
    }
}
