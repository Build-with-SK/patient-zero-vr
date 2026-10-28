using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    // Optional physical/controller locomotion distance sampler. Disable if Codex 1 publishes authored steps.
    [DefaultExecutionOrder(200)]
    public sealed class PlayerFootstepNoise : MonoBehaviour
    {
        public NoiseManager Manager;
        public Transform Head, FloorReference;
        [Min(.1f)] public float StrideMetres = .65f;
        [Min(.1f)] public float MinimumInterval = .35f;
        [Range(0, 1)] public float Intensity = .85f;
        [Min(1)] public float Radius = 10;
        public bool Grounded = true;
        private Vector3 previous;
        private bool initialized;
        private float distance;
        private double nextStep;
        private void LateUpdate()
        {
            if (!Head) return;
            Vector3 position = Head.position;
            if (!initialized) { previous = position; initialized = true; return; }
            float moved = Vector3.ProjectOnPlane(position - previous, Vector3.up).magnitude;
            previous = position;
            if (!Grounded || moved > 1.5f) { distance = 0; return; } // ignore teleport discontinuities
            distance += moved;
            if (distance < Mathf.Max(.1f, StrideMetres) || Time.timeAsDouble < nextStep || !Manager) return;
            Vector3 feet = new Vector3(position.x, FloorReference ? FloorReference.position.y : position.y - 1.6f, position.z);
            if (Manager.Emit(feet, Intensity, Radius, NoiseCategory.Footstep, GetInstanceID()))
            {
                distance = 0; nextStep = Time.timeAsDouble + MinimumInterval;
            }
        }
        public void ResetAfterTeleport() { initialized = false; distance = 0; }
        private void OnDisable() => ResetAfterTeleport();
    }
}
