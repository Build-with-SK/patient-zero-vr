// Claude integration E-29 (2026-11-18): zombies come into play and leave it only where the player cannot see.
using NUnit.Framework;
using PatientZero.AI.Logic;
using UnityEngine;

namespace PatientZero.AI.Tests
{
    public sealed class ZombiePresenceTests
    {
        private static readonly Vector3 Head = new Vector3(0, 1.6f, 0);

        /// <summary>A level direction <paramref name="degrees"/> to the right of forward.</summary>
        private static Vector3 Off(float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));
        }

        [Test]
        public void AZombieAheadIsSeenAndOneBehindIsNot()
        {
            Assert.IsTrue(ZombiePresence.InSightCone(Head, Vector3.forward, ZombiePresence.Chest(new Vector3(0, 0, 8))));
            Assert.IsFalse(ZombiePresence.InSightCone(Head, Vector3.forward, ZombiePresence.Chest(new Vector3(0, 0, -8))));
            Assert.IsFalse(ZombiePresence.InSightCone(Head, Vector3.forward, ZombiePresence.Chest(new Vector3(8, 0, 0))), "90 degrees to the side");
        }

        [Test]
        public void TheConeIsWiderThanTheScreen()
        {
            // 60 degrees off the facing: past the edge of a 16:9 monitor at 60 degrees vertical, still counted as seen.
            Vector3 at60 = Off(60) * 6, at70 = Off(70) * 6;
            Assert.IsTrue(ZombiePresence.InSightCone(Head, Vector3.forward, Head + at60));
            Assert.IsFalse(ZombiePresence.InSightCone(Head, Vector3.forward, Head + at70));
            Assert.Greater(ZombiePresence.SightHalfAngleDegrees, 55f, "wider than half a Quest headset's field of view");
        }

        [Test]
        public void FarAwayIsNotSeenAndRightNextToTheHeadAlwaysIs()
        {
            Assert.IsFalse(ZombiePresence.InSightCone(Head, Vector3.forward, Head + Vector3.forward * (ZombiePresence.SightMeters + 1)));
            Assert.IsTrue(ZombiePresence.InSightCone(Head, Vector3.forward, Head + Vector3.back * .4f), "behind, but close enough to feel");
        }

        [Test]
        public void AnUnknownFacingCountsAsSeenAndBadPointsAsNot()
        {
            Vector3 behind = Head + Vector3.back * 5;
            Assert.IsTrue(ZombiePresence.InSightCone(Head, Vector3.zero, behind), "unsure: wait rather than pop");
            Assert.IsTrue(ZombiePresence.InSightCone(Head, new Vector3(float.NaN, 0, 0), behind));
            Assert.IsFalse(ZombiePresence.InSightCone(new Vector3(float.NaN, 0, 0), Vector3.forward, behind));
            Assert.IsFalse(ZombiePresence.InSightCone(Head, Vector3.forward, new Vector3(0, float.PositiveInfinity, 3)));
        }

        [Test]
        public void ChestAndHeadSitAboveTheFeet()
        {
            Vector3 feet = new Vector3(3, 0, 2);
            Assert.AreEqual(feet + Vector3.up * 1.2f, ZombiePresence.Chest(feet));
            Assert.AreEqual(feet + Vector3.up * 1.6f, ZombiePresence.Head(feet));
        }
    }
}
