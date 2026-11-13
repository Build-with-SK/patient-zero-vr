// Claude-owned integration (owner, 2026-11-12: "Build the gun next"; plan step 4; E-25).
using UnityEngine;

namespace PatientZero.Integration.Logic
{
    /// <summary>What a pull of the trigger did.</summary>
    public enum ShotResult { None = 0, Fired = 1, Empty = 2 }

    /// <summary>What a shot hit on a zombie.</summary>
    public enum HitPart { None = 0, Body = 1, Head = 2 }

    /// <summary>
    /// The revolver of the owner's two-minute plan (agreed 2026-11-10): six rounds fill it once for the whole game and it never
    /// reloads; a headshot kills, a body shot staggers for a moment; every shot draws every zombie within about 20 m. It is found
    /// on the dead police officer, and a new life (the bed) puts it back there, full. Engine-free.
    /// </summary>
    public sealed class RevolverRules
    {
        public const int Capacity = 6;
        /// <summary>A body hit staggers the zombie this long.</summary>
        public const float StaggerSeconds = 1.2f;
        /// <summary>Every zombie in play within this many metres comes to where the shot was fired (the shot cue's noise radius).</summary>
        public const float ShotNoiseMeters = 20;
        /// <summary>The bullet reaches at most this far.</summary>
        public const float RangeMeters = 40;
        /// <summary>The shortest time between two pulls of the trigger (a double-action revolver: the hammer has to come back).</summary>
        public const float CycleSeconds = .45f;

        private float ready = float.NegativeInfinity;

        /// <summary>The player has it.</summary>
        public bool Held { get; private set; }
        /// <summary>Rounds left in the cylinder.</summary>
        public int Rounds { get; private set; } = Capacity;

        public void Take() => Held = true;

        /// <summary>A new life: the gun is back on the officer, full.</summary>
        public void Reset()
        {
            Held = false;
            Rounds = Capacity;
            ready = float.NegativeInfinity;
        }

        /// <summary>
        /// The trigger is pulled at <paramref name="now"/>: <see cref="ShotResult.None"/> when the player has no gun or the hammer
        /// has not come back yet, <see cref="ShotResult.Empty"/> (a click) once the six are spent, otherwise a shot.
        /// </summary>
        public ShotResult Pull(float now)
        {
            if (!Held || float.IsNaN(now) || now < ready) return ShotResult.None;
            ready = now + CycleSeconds;
            if (Rounds <= 0) return ShotResult.Empty;
            Rounds--;
            return ShotResult.Fired;
        }
    }

    /// <summary>
    /// Where a shot meets a zombie. A zombie is a head (a sphere, or an ellipsoid in the head bone's own space) and a capsule from
    /// the hips to the neck for the body; arms and legs do not stop a bullet. World directions must be unit length. Every test
    /// returns the distance along the ray to where the bullet enters, 0 when it starts inside, or -1 for a miss.
    /// </summary>
    public static class ShotGeometry
    {
        public static float Sphere(Vector3 origin, Vector3 direction, Vector3 centre, float radius)
        {
            if (radius <= 0) return -1;
            Vector3 m = origin - centre;
            float b = Vector3.Dot(m, direction), c = Vector3.Dot(m, m) - radius * radius;
            if (c <= 0) return 0;
            if (b > 0) return -1;
            float disc = b * b - c;
            if (disc < 0) return -1;
            return -b - Mathf.Sqrt(disc);
        }

        public static float Capsule(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, float radius)
        {
            if (radius <= 0) return -1;
            Vector3 ba = b - a, oa = origin - a;
            float baba = Vector3.Dot(ba, ba);
            if (baba < 1e-8f) return Sphere(origin, direction, a, radius);
            float along = Mathf.Clamp(Vector3.Dot(oa, ba) / baba, 0, 1);
            if ((oa - ba * along).sqrMagnitude <= radius * radius) return 0;

            float bard = Vector3.Dot(ba, direction), baoa = Vector3.Dot(ba, oa), rdoa = Vector3.Dot(direction, oa), oaoa = Vector3.Dot(oa, oa);
            float qa = baba - bard * bard;
            if (qa > 1e-8f)
            {
                float qb = baba * rdoa - baoa * bard, qc = baba * oaoa - baoa * baoa - radius * radius * baba;
                float h = qb * qb - qa * qc;
                if (h < 0) return -1; // the capsule lies inside the infinite cylinder: missing one misses both
                float t = (-qb - Mathf.Sqrt(h)) / qa;
                float y = baoa + t * bard;
                if (y > 0 && y < baba && t >= 0) return t;
                // Otherwise the ray can only enter through a rounded end.
            }

            // The rounded ends.
            float ta = Sphere(origin, direction, a, radius), tb = Sphere(origin, direction, b, radius);
            if (ta < 0) return tb;
            if (tb < 0) return ta;
            return Mathf.Min(ta, tb);
        }

        /// <summary>
        /// An ellipsoid with the given half-axes, centred on the origin of its own space. <paramref name="origin"/> and
        /// <paramref name="direction"/> are in that space; the direction need not be unit length (an affine map keeps the ray's
        /// parameter, so the result is in whatever units the direction was before the map: world metres for a unit world ray).
        /// </summary>
        public static float Ellipsoid(Vector3 origin, Vector3 direction, Vector3 radii)
        {
            if (radii.x <= 0 || radii.y <= 0 || radii.z <= 0) return -1;
            var o = new Vector3(origin.x / radii.x, origin.y / radii.y, origin.z / radii.z);
            var d = new Vector3(direction.x / radii.x, direction.y / radii.y, direction.z / radii.z);
            float a = Vector3.Dot(d, d), b = Vector3.Dot(o, d), c = Vector3.Dot(o, o) - 1;
            if (c <= 0) return 0;
            if (b > 0 || a < 1e-12f) return -1;
            float disc = b * b - a * c;
            if (disc < 0) return -1;
            return (-b - Mathf.Sqrt(disc)) / a;
        }

        /// <summary>
        /// The part of one zombie a shot hits before <paramref name="maxDistance"/> (the first wall), and how far along the ray.
        /// The nearer of head and body wins; a tie is the head.
        /// </summary>
        public static HitPart Classify(Vector3 origin, Vector3 direction, float maxDistance, Vector3 headCentre, float headRadius,
                                       Vector3 hips, Vector3 neck, float bodyRadius, out float distance) =>
            Classify(Sphere(origin, direction, headCentre, headRadius), Capsule(origin, direction, hips, neck, bodyRadius), maxDistance, out distance);

        /// <summary>The same choice from the two distances (-1 for a miss), however the head was shaped.</summary>
        public static HitPart Classify(float head, float body, float maxDistance, out float distance)
        {
            bool headHit = head >= 0 && head <= maxDistance, bodyHit = body >= 0 && body <= maxDistance;
            if (headHit && (!bodyHit || head <= body)) { distance = head; return HitPart.Head; }
            if (bodyHit) { distance = body; return HitPart.Body; }
            distance = -1;
            return HitPart.None;
        }
    }
}
