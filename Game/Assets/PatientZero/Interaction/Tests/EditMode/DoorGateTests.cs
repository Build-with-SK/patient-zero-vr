using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Interaction.Logic;
namespace PatientZero.Interaction.Tests
{
    public sealed class DoorGateTests
    {
        [Test] public void SignalAndRetainedQueryConsumeOnlyOnce(){var l=new DoorGateLatch();l.Reset(ActId.Awakening);Assert.IsTrue(l.Observe(GateId.Act1Door,true,ActId.Awakening));Assert.IsFalse(l.Observe(GateId.Act1Door,true,ActId.Awakening));}
        [Test] public void LateSubscriberCanConsumeReleasedGate(){var l=new DoorGateLatch();l.Reset(ActId.Awakening);Assert.IsTrue(l.Observe(GateId.Act1Door,true,ActId.Awakening));}
        [Test] public void RestartClosesUntilFreshRelease(){var l=new DoorGateLatch();l.Reset(ActId.Awakening);l.Observe(GateId.Act1Door,true,ActId.Awakening);l.Reset(ActId.Awakening);Assert.IsFalse(l.Observe(GateId.Act1Door,false,ActId.Awakening));Assert.IsTrue(l.Observe(GateId.Act1Door,true,ActId.Awakening));}
        [Test] public void WrongGateAndActCannotReleaseDoor(){var l=new DoorGateLatch();l.Reset(ActId.Awakening);Assert.IsFalse(l.Observe(GateId.None,true,ActId.Awakening));Assert.IsFalse(l.Observe(GateId.Act1Door,true,ActId.Machinery));l.Reset(ActId.Machinery);Assert.IsFalse(l.Observe(GateId.Act1Door,true,ActId.Awakening));}
        [Test] public void ClosedQueryDoesNotConsumeFutureRelease(){var l=new DoorGateLatch();l.Reset(ActId.Awakening);Assert.IsFalse(l.Observe(GateId.Act1Door,false,ActId.Awakening));Assert.IsTrue(l.Observe(GateId.Act1Door,true,ActId.Awakening));}
    }
}
