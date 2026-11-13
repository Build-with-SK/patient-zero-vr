using System;
using NUnit.Framework;
using PatientZero.AI.Logic;
using PatientZero.Core;

namespace PatientZero.AI.Tests
{
    public sealed class ZombieMotionTests
    {
        private static ZombieMotion.Pose Step(ZombieMotion motion, AlertLevel level = AlertLevel.Searching,
            ZombieMode mode = ZombieMode.Idle, float speed = 0, bool stunned = false, bool dead = false, bool kneeling = false,
            float delta = .02f, float yaw = 0, float pitch = 0) =>
            motion.Step(delta, level, mode, speed, .9f, yaw, pitch, stunned, dead, kneeling);

        [Test] public void OnlyAChaseUsesThePlayerAsALookTarget()
        {
            foreach (AlertLevel level in Enum.GetValues(typeof(AlertLevel)))
            {
                Assert.AreEqual(level == AlertLevel.Chasing, ZombieMotion.LooksAtPlayer(level));
                Assert.AreEqual(level != AlertLevel.Unaware, ZombieMotion.LooksAtTarget(level));
            }
        }

        [Test] public void ListeningFreezesClipsAndHoldsTheCockedHeadForTheWholeTelegraph()
        {
            var m = new ZombieMotion(2);
            for (int i = 0; i < 150; i++)
            {
                var p = Step(m, AlertLevel.Suspicious, delta: .02f, yaw: 45, pitch: -15);
                Assert.IsTrue(p.Listening); Assert.AreEqual(0f, p.HeadTwitch); Assert.AreEqual(0f, p.ShoulderTwitch);
                Assert.IsTrue(p.Roll >= 0 && p.Roll <= 20);
                if (i > 25) Assert.AreEqual(20f, p.Roll, .02f);
            }
            Assert.IsFalse(Step(m, AlertLevel.Investigating).Listening);
        }

        [Test] public void HeadAnglesClampAndEaseWithoutSnappingOrOvershooting()
        {
            var m = new ZombieMotion(2);
            var first = Step(m, AlertLevel.Investigating, yaw: 180, pitch: -90);
            Assert.IsTrue(first.Yaw > 0 && first.Yaw < 70); Assert.IsTrue(first.Pitch < 0 && first.Pitch > -30);
            ZombieMotion.Pose p = first;
            for (int i = 0; i < 200; i++)
            {
                p = Step(m, AlertLevel.Investigating, yaw: 180, pitch: -90);
                Assert.IsTrue(p.Yaw <= 70 && p.Pitch >= -30);
            }
            Assert.AreEqual(70f, p.Yaw, .001f); Assert.AreEqual(-30f, p.Pitch, .001f);
            var reverse = Step(m, AlertLevel.Chasing, yaw: -180, pitch: 90);
            Assert.IsTrue(reverse.Yaw < p.Yaw && reverse.Yaw > -70);
            Assert.IsTrue(reverse.Pitch > p.Pitch && reverse.Pitch < 30);
        }

        [Test] public void EasingComposesAcrossFrameRatesAndRejectsInvalidTime()
        {
            float coarse = 0, fine = 0;
            for (int i = 0; i < 30; i++) coarse = ZombieMotion.Ease(coarse, 70, 1f / 30);
            for (int i = 0; i < 120; i++) fine = ZombieMotion.Ease(fine, 70, 1f / 120);
            Assert.AreEqual(coarse, fine, .001f);
            Assert.AreEqual(12f, ZombieMotion.Ease(12, 70, 0));
            Assert.AreEqual(12f, ZombieMotion.Ease(12, 70, -1));
            Assert.AreEqual(12f, ZombieMotion.Ease(12, 70, float.NaN));
            Assert.AreEqual(12f, ZombieMotion.Ease(12, float.PositiveInfinity, 1));
            var p = Step(new ZombieMotion(2), yaw: float.NaN, pitch: float.PositiveInfinity);
            Assert.AreEqual(0f, p.Yaw); Assert.AreEqual(0f, p.Pitch);
        }

        [Test] public void TwitchEnvelopeJerksOutInATenthAndEasesBackToZero()
        {
            Assert.AreEqual(0f, ZombieMotion.TwitchEnvelope(0));
            Assert.AreEqual(1f, ZombieMotion.TwitchEnvelope(.1f), .0001f);
            Assert.AreEqual(.5f, ZombieMotion.TwitchEnvelope(.25f), .0001f);
            Assert.AreEqual(0f, ZombieMotion.TwitchEnvelope(.4f));
            Assert.AreEqual(0f, ZombieMotion.TwitchEnvelope(-1));
            Assert.AreEqual(0f, ZombieMotion.TwitchEnvelope(float.NaN));
        }

        [Test] public void TwitchScheduleRepeatsFromIdAndDiffersBetweenZombiesAndResets()
        {
            var a = new ZombieMotion(2); var b = new ZombieMotion(2); var other = new ZombieMotion(3);
            int twitches = 0, different = 0, starts = 0, lastStart = -1000;
            bool wasTwitching = false;
            for (int i = 0; i < 6000; i++)
            {
                var p = Step(a); var same = Step(b); var q = Step(other);
                Assert.AreEqual(p.HeadTwitch, same.HeadTwitch); Assert.AreEqual(p.ShoulderTwitch, same.ShoulderTwitch);
                bool twitching = p.HeadTwitch != 0 || p.ShoulderTwitch != 0;
                if (twitching)
                {
                    twitches++;
                    if (!wasTwitching) { Assert.IsTrue(i - lastStart >= 140); lastStart = i; starts++; }
                }
                if (p.HeadTwitch != q.HeadTwitch || p.ShoulderTwitch != q.ShoulderTwitch) different++;
                Assert.IsTrue(Math.Abs(p.HeadTwitch) <= 7 && Math.Abs(p.ShoulderTwitch) <= 9);
                wasTwitching = twitching;
            }
            Assert.IsTrue(twitches > 0 && different > 0 && starts > 10);
            a.Reset(); b = new ZombieMotion(2);
            for (int i = 0; i < 500; i++)
            {
                var p = Step(a); var q = Step(b);
                Assert.AreEqual(p.HeadTwitch, q.HeadTwitch); Assert.AreEqual(p.ShoulderTwitch, q.ShoulderTwitch);
            }
        }

        [Test] public void OnlyUnawareAndSearchingAtWalkingSpeedAllowTwitches()
        {
            foreach (AlertLevel level in Enum.GetValues(typeof(AlertLevel)))
                Assert.AreEqual(level == AlertLevel.Unaware || level == AlertLevel.Searching,
                    ZombieMotion.AllowsTwitch(level, ZombieMode.Idle, .8f, .9f));
            Assert.IsFalse(ZombieMotion.AllowsTwitch(AlertLevel.Searching, ZombieMode.Idle, 1.6f, .9f));
        }

        [Test] public void PausingHoldsATwitchAndAChaseOrStaggerCancelsItImmediately()
        {
            var m = new ZombieMotion(2);
            ZombieMotion.Pose twitch = default;
            for (int i = 0; i < 1000; i++)
            {
                twitch = Step(m);
                if (twitch.HeadTwitch != 0 || twitch.ShoulderTwitch != 0) break;
            }
            Assert.IsTrue(twitch.HeadTwitch != 0 || twitch.ShoulderTwitch != 0);
            var paused = Step(m, delta: 0);
            Assert.AreEqual(twitch.HeadTwitch, paused.HeadTwitch); Assert.AreEqual(twitch.ShoulderTwitch, paused.ShoulderTwitch);
            var chase = Step(m, AlertLevel.Chasing, speed: 1.6f);
            Assert.AreEqual(0f, chase.HeadTwitch); Assert.AreEqual(0f, chase.ShoulderTwitch);
            AssertZero(Step(m, stunned: true));
        }

        [Test] public void StaggerDeathKneelingRestraintAndPassiveOwnTheWholePose()
        {
            foreach (ZombieMode mode in new[] { ZombieMode.Restrained, ZombieMode.Passive })
            {
                Assert.IsFalse(ZombieMotion.AllowsMotion(mode, false, false, false));
                AssertZero(Step(new ZombieMotion(2), mode: mode, speed: 1.6f, yaw: 60));
            }
            var m = new ZombieMotion(2);
            for (int i = 0; i < 100; i++) Step(m, AlertLevel.Chasing, speed: 1.6f, yaw: 60);
            AssertZero(Step(m, stunned: true));
            AssertZero(Step(m, dead: true));
            AssertZero(Step(m, kneeling: true));
            Assert.IsFalse(ZombieMotion.AllowsTwitch(AlertLevel.Searching, ZombieMode.Idle, 0, .9f, stunned: true));
            Assert.IsFalse(ZombieMotion.AllowsTwitch(AlertLevel.Searching, ZombieMode.Idle, 0, .9f, dead: true));
            Assert.IsFalse(ZombieMotion.AllowsTwitch(AlertLevel.Searching, ZombieMode.Idle, 0, .9f, kneeling: true));
        }

        [Test] public void GunshotInvestigationAndChaseLeanByActualSpeedAndEaseOut()
        {
            var m = new ZombieMotion(2);
            Assert.AreEqual(0f, Step(m, speed: .9f).Lean);
            var first = Step(m, AlertLevel.Investigating, speed: 1.6f);
            Assert.IsTrue(first.Lean > 0 && first.Lean < 15);
            ZombieMotion.Pose p = first;
            for (int i = 0; i < 100; i++) p = Step(m, AlertLevel.Investigating, speed: 1.6f);
            Assert.AreEqual(15f, p.Lean, .001f);
            var slow = Step(m, speed: .9f); Assert.IsTrue(slow.Lean < p.Lean && slow.Lean > 0);
            Assert.AreEqual(.6f, ZombieMotion.ShuffleIntensity(.8f, .8f), .0001f);
            Assert.AreEqual(1f, ZombieMotion.ShuffleIntensity(1.3f, .85f), .0001f);
            Assert.AreEqual(0f, ZombieMotion.Running01(float.NaN, .9f));
        }

        [Test] public void MotionTicksAllocateZeroBytesAfterWarmup()
        {
            var m = new ZombieMotion(2);
            for (int i = 0; i < 200; i++) Step(m);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) Step(m);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(0L, allocated);
        }

        private static void AssertZero(ZombieMotion.Pose p)
        {
            Assert.AreEqual(0f, p.Yaw); Assert.AreEqual(0f, p.Pitch); Assert.AreEqual(0f, p.Roll);
            Assert.AreEqual(0f, p.Lean); Assert.AreEqual(0f, p.HeadTwitch); Assert.AreEqual(0f, p.ShoulderTwitch);
            Assert.IsFalse(p.Listening);
        }
    }
}
