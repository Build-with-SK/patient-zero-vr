using UnityEngine;
using UnityEngine.Events;

namespace PatientZero.XR
{
    // Read-only output from an SDK OneGrabRotateTransformer constraint. Never drives a hand pose.
    public sealed class MechanismFeedback : MonoBehaviour
    {
        public Transform MovingPart;
        public Vector3 LocalAxis = Vector3.forward;
        public float MinDegrees = 0, MaxDegrees = 90;
        public NoiseEmitter Noise;
        public UnityEvent<float> OnValueChanged = new UnityEvent<float>();
        [Range(.001f, .2f)] public float ChangeThreshold = .02f;
        private Quaternion initial;
        private Vector3 reference;
        private float lastValue = -1;
        private void Awake()
        {
            if (!MovingPart) MovingPart = transform;
            initial = MovingPart.localRotation;
            var axis = LocalAxis.normalized;
            reference = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > .9f ? Vector3.right : Vector3.up).normalized;
        }
        private void Update()
        {
            Vector3 rotated = (Quaternion.Inverse(initial) * MovingPart.localRotation) * reference;
            float angle = Vector3.SignedAngle(reference, rotated, LocalAxis.normalized);
            float value = Mathf.InverseLerp(MinDegrees, MaxDegrees, angle);
            if (Mathf.Abs(value - lastValue) < ChangeThreshold) return;
            bool initialized = lastValue >= 0;
            lastValue = value; OnValueChanged.Invoke(value);
            if (initialized && Noise) Noise.Emit();
        }
    }
}
