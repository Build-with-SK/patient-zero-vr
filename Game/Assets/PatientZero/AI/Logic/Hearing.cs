using System;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI.Logic
{
    /// <summary>Radius hearing from contract v1; robustness ported from NoiseEvent.LevelAt@72be6d8.</summary>
    public static class Hearing
    {
        /// <summary>Only a closed door reduces gameplay radius; walls affect sight/audio, not hearing.</summary>
        public static float Occlusion(bool closedDoor, bool wall = false) => closedDoor ? .5f : 1f;
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Finite(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);
        public static bool CanHear(in NoiseEvent noise, Vector3 listener, float multiplier, float occlusion, float now)
        {
            if (noise.Origin != NoiseOrigin.Player && noise.Origin != NoiseOrigin.Scripted) return false;
            if (!Finite(noise.Position) || !Finite(listener) || !Finite(multiplier) || !Finite(occlusion) ||
                !Finite(noise.RadiusMeters) || !Finite(noise.Time) || !Finite(now) || noise.Time < 0 ||
                noise.Time > now || now - noise.Time > 1 || multiplier <= 0 || occlusion <= 0 || occlusion > 1 || noise.RadiusMeters <= 0) return false;
            return Vector3.Distance(listener, noise.Position) <= noise.RadiusMeters * multiplier * occlusion;
        }
        public static float Salience(in NoiseEvent noise, Vector3 listener, float multiplier, float occlusion) =>
            noise.RadiusMeters * multiplier * occlusion - Vector3.Distance(listener, noise.Position);
    }
}
