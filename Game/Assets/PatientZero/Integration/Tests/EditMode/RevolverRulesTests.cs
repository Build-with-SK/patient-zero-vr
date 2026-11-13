// Claude-owned integration (owner, 2026-11-12: the revolver; E-25).
using NUnit.Framework;
using PatientZero.Integration.Logic;
using UnityEngine;

namespace PatientZero.Integration.Tests
{
    public class RevolverRulesTests
    {
        [Test]
        public void SixShotsThenOnlyClicksAndNoShotWithoutTheGun()
        {
            var gun = new RevolverRules();
            Assert.AreEqual(ShotResult.None, gun.Pull(0), "not picked up yet");
            gun.Take();
            float t = 0;
            for (int i = 0; i < RevolverRules.Capacity; i++, t += 1) Assert.AreEqual(ShotResult.Fired, gun.Pull(t), "round " + (i + 1));
            Assert.AreEqual(0, gun.Rounds);
            Assert.AreEqual(ShotResult.Empty, gun.Pull(t));
            Assert.AreEqual(ShotResult.Empty, gun.Pull(t + 1), "it never reloads");
        }

        [Test]
        public void TheHammerMustComeBackBetweenShots()
        {
            var gun = new RevolverRules();
            gun.Take();
            Assert.AreEqual(ShotResult.Fired, gun.Pull(10));
            Assert.AreEqual(ShotResult.None, gun.Pull(10 + RevolverRules.CycleSeconds * .5f), "too soon: nothing happens");
            Assert.AreEqual(5, gun.Rounds, "a pull that was too soon spends nothing");
            Assert.AreEqual(ShotResult.Fired, gun.Pull(10 + RevolverRules.CycleSeconds));
        }

        [Test]
        public void ANewLifePutsItBackOnTheOfficerFull()
        {
            var gun = new RevolverRules();
            gun.Take();
            gun.Pull(0);
            gun.Pull(1);
            gun.Reset();
            Assert.IsFalse(gun.Held);
            Assert.AreEqual(RevolverRules.Capacity, gun.Rounds);
            Assert.AreEqual(ShotResult.None, gun.Pull(2));
        }

        private static readonly Vector3 Head = new Vector3(0, 1.55f, 5), Hips = new Vector3(0, .9f, 5), Neck = new Vector3(0, 1.4f, 5);

        private static HitPart Shoot(Vector3 from, Vector3 at, float wall, out float distance) =>
            ShotGeometry.Classify(from, (at - from).normalized, wall, Head, .14f, Hips, Neck, .2f, out distance);

        [Test]
        public void AimedAtTheHeadItIsAHeadshotAtTheRightDistance()
        {
            Assert.AreEqual(HitPart.Head, Shoot(new Vector3(0, 1.55f, 0), Head, 40, out float d));
            Assert.AreEqual(5 - .14f, d, 1e-3f);
        }

        [Test]
        public void AimedAtTheChestItIsABodyShot()
        {
            Assert.AreEqual(HitPart.Body, Shoot(new Vector3(0, 1.6f, 0), new Vector3(0, 1.1f, 5), 40, out float d));
            Assert.Greater(d, 4.5f);
            Assert.Less(d, 5f);
        }

        [Test]
        public void ArmsLegsAndTheAirAroundItAreMisses()
        {
            Assert.AreEqual(HitPart.None, Shoot(new Vector3(0, 1.55f, 0), new Vector3(.5f, 1.55f, 5), 40, out _), "beside the head");
            Assert.AreEqual(HitPart.None, Shoot(new Vector3(0, 1.6f, 0), new Vector3(0, .3f, 5), 40, out _), "at the shins");
            Assert.AreEqual(HitPart.None, Shoot(new Vector3(0, 1.55f, 0), new Vector3(0, 1.55f, -5), 40, out _), "the other way");
        }

        [Test]
        public void AWallInFrontStopsTheBullet()
        {
            Assert.AreEqual(HitPart.None, Shoot(new Vector3(0, 1.55f, 0), Head, 3, out _));
            Assert.AreEqual(HitPart.Head, Shoot(new Vector3(0, 1.55f, 0), Head, 4.9f, out _));
        }

        [Test]
        public void ShotFromAboveTheHeadComesFirst()
        {
            // Looking steeply down onto the zombie's head: the ray meets the head before the body below it.
            Vector3 from = new Vector3(0, 3.2f, 4.2f);
            Assert.AreEqual(HitPart.Head, Shoot(from, Head, 40, out _));
        }

        [Test]
        public void TheHeadEllipsoidIsMetAtItsOwnSurface()
        {
            var radii = new Vector3(.4f, .3f, .2f);
            Assert.AreEqual(4.6f, ShotGeometry.Ellipsoid(new Vector3(-5, 0, 0), Vector3.right, radii), 1e-3f, "along x");
            Assert.AreEqual(4.7f, ShotGeometry.Ellipsoid(new Vector3(0, 5, 0), Vector3.down, radii), 1e-3f, "along y");
            Assert.AreEqual(4.8f, ShotGeometry.Ellipsoid(new Vector3(0, 0, -5), Vector3.forward, radii), 1e-3f, "along z");
            Assert.AreEqual(-1, ShotGeometry.Ellipsoid(new Vector3(-5, .31f, 0), Vector3.right, radii), "just over the top");
            Assert.AreEqual(-1, ShotGeometry.Ellipsoid(new Vector3(-5, 0, 0), Vector3.left, radii), "the other way");
            Assert.AreEqual(0, ShotGeometry.Ellipsoid(new Vector3(.1f, 0, 0), Vector3.right, radii), "from inside");
            // In a bone's scaled space the ray's parameter is kept: twice the length per unit, half the parameter.
            Assert.AreEqual(2.3f, ShotGeometry.Ellipsoid(new Vector3(-5, 0, 0), Vector3.right * 2, radii), 1e-3f, "scaled");
        }

        [Test]
        public void TheNearerPartWinsATieIsTheHeadAndAWallStopsBoth()
        {
            Assert.AreEqual(HitPart.Head, ShotGeometry.Classify(2, 2, 10, out float d));
            Assert.AreEqual(2, d, 1e-6f);
            Assert.AreEqual(HitPart.Body, ShotGeometry.Classify(3, 2, 10, out d));
            Assert.AreEqual(HitPart.Head, ShotGeometry.Classify(1, -1, 10, out d));
            Assert.AreEqual(HitPart.Body, ShotGeometry.Classify(11, 4, 10, out d), "the head beyond the wall");
            Assert.AreEqual(HitPart.None, ShotGeometry.Classify(11, 12, 10, out d));
            Assert.AreEqual(-1, d, 1e-6f);
        }

        [Test]
        public void TheCapsuleHasRoundedEndsAndAStartInsideIsZero()
        {
            Vector3 a = new Vector3(0, 0, 0), b = new Vector3(0, 1, 0);
            Assert.AreEqual(.8f, ShotGeometry.Capsule(new Vector3(0, -1, 0), Vector3.up, a, b, .2f), 1e-3f, "straight up into the bottom end");
            Assert.AreEqual(.8f, ShotGeometry.Capsule(new Vector3(0, 2, 0), Vector3.down, a, b, .2f), 1e-3f, "straight down into the top end");
            Assert.AreEqual(.8f, ShotGeometry.Capsule(new Vector3(-1, .5f, 0), Vector3.right, a, b, .2f), 1e-3f, "into the side");
            Assert.AreEqual(0, ShotGeometry.Capsule(new Vector3(0, .5f, .1f), Vector3.right, a, b, .2f), "from inside");
            Assert.AreEqual(-1, ShotGeometry.Capsule(new Vector3(-1, 1.5f, 0), Vector3.right, a, b, .2f), "over the top");
        }
    }
}
