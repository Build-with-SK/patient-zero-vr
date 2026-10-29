using System;
using UnityEngine;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Ratchet rotation sampled by the valve shell; re-gripping preserves accumulated travel.</summary>
    public sealed class ValveMath
    {
        private Vector3 previous; private bool gripping; private bool assisted;
        public float Degrees { get; private set; }
        public float RequiredDegrees => assisted ? 270f : 540f;
        public float Progress => Mathf.Clamp01(Degrees / RequiredDegrees);
        public bool Complete => Progress >= 1f;
        public float Intensity { get; private set; }
        public void SetAssist(bool enabled) { assisted = enabled; }
        public void BeginGrip(Vector3 radial, Vector3 axis) { gripping = TryRadial(radial, axis, out previous); Intensity=0; }
        public void EndGrip() { gripping=false; Intensity=0; }
        public float Sample(Vector3 radial, Vector3 axis, float deltaSeconds)
        {
            if (!gripping || deltaSeconds <= 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return 0;
            Vector3 current;
            if (!TryRadial(radial, axis, out current)) { gripping=false; Intensity=0; return 0; }
            float signed = Vector3.SignedAngle(previous, current, axis.normalized);
            previous=current;
            Degrees += Math.Abs(signed);
            Intensity = Mathf.Clamp01((Math.Abs(signed)/deltaSeconds - 30f)/150f);
            return signed;
        }
        private static bool TryRadial(Vector3 radial, Vector3 axis, out Vector3 projected)
        {
            projected=Vector3.zero;
            if (!Finite(radial) || !Finite(axis) || axis.sqrMagnitude<1e-8f) return false;
            projected=Vector3.ProjectOnPlane(radial,axis).normalized;
            return projected.sqrMagnitude>0.5f;
        }
        private static bool Finite(Vector3 v) => !float.IsNaN(v.x+v.y+v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
}
