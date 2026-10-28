using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace PatientZero.XR
{
    [DefaultExecutionOrder(-90)]
    public sealed class OpenXRTrackingInput : MonoBehaviour
    {
        public TrackingLossGuard Left, Right;
        private readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>(2);
        private XRHandSubsystem hands;
        private double nextDiscovery;
        private void Update()
        {
            if ((hands == null || !hands.running) && Time.unscaledTimeAsDouble >= nextDiscovery)
            {
                nextDiscovery = Time.unscaledTimeAsDouble + 1;
                SubsystemManager.GetSubsystems(subsystems);
                hands = null;
                for (int i = 0; i < subsystems.Count; i++) if (subsystems[i].running) { hands = subsystems[i]; break; }
            }
            if (Left) Left.Submit(hands != null && hands.running && HandValid(hands.leftHand), ControllerValid(XRNode.LeftHand));
            if (Right) Right.Submit(hands != null && hands.running && HandValid(hands.rightHand), ControllerValid(XRNode.RightHand));
        }
        private static bool HandValid(XRHand hand) => hand.isTracked &&
            hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out _) && hand.GetJoint(XRHandJointID.Palm).TryGetPose(out _);
        private static bool ControllerValid(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            return device.isValid && (device.characteristics & InputDeviceCharacteristics.Controller) != 0 &&
                device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked &&
                device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state) &&
                (state & (InputTrackingState.Position | InputTrackingState.Rotation)) == (InputTrackingState.Position | InputTrackingState.Rotation);
        }
    }
}
