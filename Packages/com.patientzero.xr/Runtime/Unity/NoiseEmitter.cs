using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    // Call Emit() from door/puzzle/player-footstep events. Physics contacts use impact thresholds.
    public sealed class NoiseEmitter : MonoBehaviour
    {
        public NoiseManager Manager;
        public NoiseCategory Category = NoiseCategory.Collision;
        [Range(0, 1)] public float Intensity = .7f;
        [Min(.1f)] public float Radius = 12;
        [Min(.01f)] public float Cooldown = .3f;
        [Min(0)] public float MinimumImpactSpeed = .8f;
        public bool EmitOnCollision = true;
        private double nextAllowed;
        private int sourceId;
        private void Awake() => sourceId = GetInstanceID();
        public void Emit() => TryEmit(Intensity);
        public bool TryEmit(float intensity)
        {
            if (!Manager || Time.timeAsDouble < nextAllowed || intensity <= 0) return false;
            if (!Manager.Emit(transform.position, Mathf.Clamp01(intensity), Radius, Category, sourceId)) return false;
            nextAllowed = Time.timeAsDouble + Mathf.Max(.01f, Cooldown);
            return true;
        }
        private void OnCollisionEnter(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (EmitOnCollision && speed >= MinimumImpactSpeed)
                TryEmit(Intensity * Mathf.Clamp01(speed / Mathf.Max(.1f, MinimumImpactSpeed * 3)));
        }
    }
}
