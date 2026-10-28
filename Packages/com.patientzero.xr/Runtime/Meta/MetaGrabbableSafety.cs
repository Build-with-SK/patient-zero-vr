using Oculus.Interaction;
using UnityEngine;

namespace PatientZero.XR.Meta
{
    // Only observes SDK selection. Pose recognition, pinch and throw estimation remain inside ISDK.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(HeldObjectSafety))]
    public sealed class MetaGrabbableSafety : MonoBehaviour
    {
        public Grabbable Grabbable;
        private HeldObjectSafety safety;
        private void Awake() => safety = GetComponent<HeldObjectSafety>();
        private void LateUpdate()
        {
            if (!Grabbable || !Grabbable.isActiveAndEnabled) { safety.SelectionEnded(); return; }
            if (Grabbable.SelectingPointsCount > 0) safety.SelectionBegan();
            else safety.SelectionEnded();
        }
    }
}
