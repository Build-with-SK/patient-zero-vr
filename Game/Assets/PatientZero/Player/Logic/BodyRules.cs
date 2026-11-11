// Claude integration E-22 (two-minute version, owner-approved plan 2026-11-10).
using UnityEngine;
namespace PatientZero.Player.Logic
{
    public enum StepKind { None, Footstep, Landing }

    /// <summary>
    /// E-22: footsteps for a headset, where nothing presses a key to walk. The player's floor position is sampled every frame:
    /// walking (on the spot or with the stick) makes a footstep every <see cref="StrideMeters"/>; a jump of a metre or more in
    /// one frame is a teleport landing. Swaying and head turns while standing still stay silent, so freezing works in VR too.
    /// </summary>
    public sealed class FootstepCounter
    {
        public const float StrideMeters = .75f, WalkingSpeed = .35f, TeleportMeters = 1f;
        private float travelled;
        public void Reset() { travelled = 0; }
        public StepKind Sample(Vector3 moved, float seconds)
        {
            moved.y = 0;
            float distance = moved.magnitude;
            if (!(seconds > 0) || float.IsNaN(distance) || float.IsInfinity(distance) || float.IsInfinity(seconds)) return StepKind.None;
            if (distance >= TeleportMeters) { travelled = 0; return StepKind.Landing; }
            if (distance / seconds < WalkingSpeed) { travelled = Mathf.Max(0, travelled - distance); return StepKind.None; }
            travelled += distance;
            if (travelled < StrideMeters) return StepKind.None;
            travelled -= StrideMeters;
            return StepKind.Footstep;
        }
    }

    /// <summary>E-22: a shove out of a zombie's grab in a headset is one fast push of a hand; the hand must slow before the next.</summary>
    public sealed class ShoveDetector
    {
        public const float FastMetersPerSecond = 1.5f, RestMetersPerSecond = .5f;
        private bool armed = true;
        public void Reset() { armed = true; }
        public bool Sample(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed)) return false;
            if (armed && speed >= FastMetersPerSecond) { armed = false; return true; }
            if (speed <= RestMetersPerSecond) armed = true;
            return false;
        }
    }
}
