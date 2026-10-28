using Oculus.Interaction;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR.Meta
{
    // Optional SDK ActiveStateGroup input; combine with SDK's existing state using AND.
    public sealed class SdkTrackingActiveState : MonoBehaviour, IActiveState
    {
        public TrackingLossGuard Guard;
        public InteractionSource Source = InteractionSource.Hand;
        public bool Active => Guard && Guard.ActiveSource == Source;
    }
}
