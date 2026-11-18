// Claude integration E-29 (2026-11-18). Owner's playtest: "while playing random zombies are disappering and appering after i
// plug the plug into the socket". Each act puts its own zombies in play and takes the last act's out, and until now that
// happened wherever the zombies stood, in full view. This is the rule for "could the player see it?".
using UnityEngine;

namespace PatientZero.AI.Logic
{
    /// <summary>
    /// When a zombie may come into play or leave it: only where the player cannot see. A zombie that should leave while the
    /// player could see it stands still until they cannot; one that should appear where the player could see waits until they
    /// cannot. The shell adds the line-of-sight test (walls and doors); this is the part that needs no physics.
    /// </summary>
    public static class ZombiePresence
    {
        /// <summary>
        /// Half the angle of the cone the player could see, measured from the way the head faces. Wider than half the field of
        /// view of a Quest headset (about 55 degrees across) and of a 16:9 monitor at the desktop camera's 60 degrees (about 51),
        /// so a zombie at the edge of the screen counts as seen.
        /// </summary>
        public const float SightHalfAngleDegrees = 65;

        /// <summary>Beyond this the player cannot make a zombie out in the hospital's light; no floor is this long.</summary>
        public const float SightMeters = 40;

        /// <summary>
        /// Within this of the head a zombie counts as seen whichever way the player faces (they would feel it). The shell also
        /// ignores whatever blocks the line this close to the head: that is the player's own hands, gun, torch or held prop.
        /// </summary>
        public const float NearMeters = .6f;

        /// <summary>How often a waiting zombie checks again (seconds).</summary>
        public const float CheckSeconds = .1f;

        /// <summary>The points of a zombie standing at <paramref name="feet"/> that the player would see first: chest and head.</summary>
        public static Vector3 Chest(Vector3 feet) => feet + Vector3.up * 1.2f;

        /// <inheritdoc cref="Chest"/>
        public static Vector3 Head(Vector3 feet) => feet + Vector3.up * 1.6f;

        /// <summary>
        /// Whether <paramref name="point"/> is close enough and inside the cone the player's head faces. A facing that is not
        /// known (zero or not finite) counts as facing it: when unsure, the zombie waits rather than pops.
        /// </summary>
        public static bool InSightCone(Vector3 head, Vector3 headForward, Vector3 point)
        {
            if (!Hearing.Finite(head) || !Hearing.Finite(point)) return false;
            Vector3 to = point - head;
            float distance = to.magnitude;
            if (distance > SightMeters) return false;
            if (distance <= NearMeters) return true;
            if (!Hearing.Finite(headForward) || headForward.sqrMagnitude < 1e-6f) return true;
            return Vector3.Angle(headForward, to) <= SightHalfAngleDegrees;
        }
    }
}
