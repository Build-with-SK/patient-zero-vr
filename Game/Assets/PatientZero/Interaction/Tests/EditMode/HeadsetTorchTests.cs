using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using UnityEngine;

namespace PatientZero.Interaction.Tests
{
    public class HeadsetTorchTests
    {
        [Test] public void SwitchingToHandsRestoresLightAndControllersKeepTheirSwitchChoice()
        {
            Assert.IsTrue(HeadsetTorchRule.ShouldShine(false, InputModality.Hands));
            Assert.IsTrue(HeadsetTorchRule.ShouldShine(true, InputModality.Hands));
            Assert.IsFalse(HeadsetTorchRule.ShouldShine(false, InputModality.Controllers));
            Assert.IsTrue(HeadsetTorchRule.ShouldShine(true, InputModality.Controllers));
        }

        [Test] public void TrackedHandOwnsTheBeamAndItsDirectionIsNormalized()
        {
            Assert.IsTrue(HeadsetTorchRule.Beam(true, new Vector3(1, 2, 3), Vector3.right * 2,
                Vector3.zero, Vector3.forward, out var position, out var forward));
            Assert.AreEqual(1 + HeadsetTorchRule.LensAhead, position.x, .0001);
            Assert.AreEqual(2, position.y, .0001);
            Assert.AreEqual(3, position.z, .0001);
            Assert.AreEqual(1, forward.x, .0001);
            Assert.AreEqual(0, forward.z, .0001);
        }

        [Test] public void LosingTheHandFallsBackToTheHead()
        {
            Assert.IsTrue(HeadsetTorchRule.Beam(false, Vector3.one, Vector3.right,
                new Vector3(0, 1, 2), Vector3.forward, out var position, out var forward));
            Assert.AreEqual(1, position.y, .0001);
            Assert.AreEqual(2 + HeadsetTorchRule.LensAhead, position.z, .0001);
            Assert.AreEqual(1, forward.z, .0001);
        }

        [Test] public void InvalidTrackedPosesUseTheHeadInsteadOfSendingBadLightData()
        {
            foreach (var direction in new[] { Vector3.zero, new Vector3(float.NaN, 0, 1),
                         new Vector3(float.PositiveInfinity, 0, 1), new Vector3(float.MaxValue, 0, 1) })
            {
                Assert.IsTrue(HeadsetTorchRule.Beam(true, Vector3.one, direction,
                    Vector3.zero, Vector3.forward, out var position, out var forward));
                Assert.AreEqual(1, forward.z, .0001);
                Assert.AreEqual(HeadsetTorchRule.LensAhead, position.z, .0001);
            }
        }

        [Test] public void WithNeitherPoseUsableNoLightEventShouldBeSent()
        {
            Assert.IsFalse(HeadsetTorchRule.Beam(false, Vector3.zero, Vector3.forward,
                Vector3.zero, Vector3.zero, out var position, out var forward));
            Assert.AreEqual(0, position.sqrMagnitude);
            Assert.AreEqual(0, forward.sqrMagnitude);
        }
    }
}
