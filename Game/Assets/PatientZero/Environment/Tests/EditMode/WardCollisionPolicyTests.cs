using NUnit.Framework;
using PatientZero.Environment.Logic;

namespace PatientZero.Environment.Tests
{
    public sealed class WardCollisionPolicyTests
    {
        [Test] public void MissingAndBuiltinLayersDoNotTurnWallsIntoHearingDoors()
        {
            foreach (int layer in new[] { -1, 0, 2, 7, 32 })
            {
                var p = new WardCollisionPolicy(layer);
                Assert.AreEqual(0, p.Hearing);
                Assert.AreEqual(1, p.Sight);
                Assert.AreEqual(1, p.Movement);
            }
        }
        [Test] public void ExplicitDoorLayerBlocksMovementSightAndAudioButAloneAttenuatesHearing()
        {
            foreach (int layer in new[] { 8, 12, 31 })
            {
                var p = new WardCollisionPolicy(layer);
                Assert.AreEqual(1 << layer, p.Hearing);
                Assert.AreEqual(0, p.Hearing & 1);
                Assert.AreEqual(1 | (1 << layer), p.Sight);
                Assert.AreEqual(p.Sight, p.Movement);
                Assert.AreEqual(p.Sight, p.Audio);
            }
        }
    }
}
