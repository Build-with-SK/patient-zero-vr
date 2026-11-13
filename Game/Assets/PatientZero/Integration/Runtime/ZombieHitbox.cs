// Claude-owned integration (owner, 2026-11-12: "Build the gun next"; plan step 4; E-25).
using PatientZero.AI;
using PatientZero.Integration.Logic;
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// Where a bullet can meet one zombie: an ellipsoid round its head, fixed in the head bone's own space, and a capsule from
    /// its hips to its neck, following the animated bones of its model. Zombies have no colliders (their actors sit on Ignore
    /// Raycast for their own senses), so the revolver tests these shapes directly (<see cref="ShotGeometry"/>). The sizes are
    /// measured from the model's own mesh at compose time (RevolverStep, Ward 988); the model's big cartoon head, ears, jaw
    /// and tongue included. Arms and legs do not stop a bullet.
    /// </summary>
    public sealed class ZombieHitbox : MonoBehaviour
    {
        public ZombieAgentShell zombie;
        public Transform head, hips, neck;
        [Tooltip("The centre of the head ellipsoid, in the head bone's own space.")]
        public Vector3 headCentre;
        [Tooltip("The head ellipsoid's half-axes, in the head bone's own space.")]
        public Vector3 headRadii = new Vector3(.1f, .1f, .1f);
        [Tooltip("World metres.")]
        public float bodyRadius = .2f;

        /// <summary>In play, in view and not dead: only such a zombie can be shot.</summary>
        public bool Shootable => zombie && head && hips && neck && zombie.Active && zombie.Shown && !zombie.Dead;

        public Vector3 HeadCentre => head ? head.TransformPoint(headCentre) : transform.position;

        /// <summary>A point low on the body, for the test bot's body shot: below the hanging jaw, above the legs.</summary>
        public Vector3 Belly => hips && neck ? Vector3.Lerp(hips.position, neck.position, .3f) : transform.position;

        /// <summary>What a shot along <paramref name="direction"/> (unit length) hits on this zombie before <paramref name="maxDistance"/>.</summary>
        public HitPart Test(Vector3 origin, Vector3 direction, float maxDistance, out float distance)
        {
            if (!Shootable)
            {
                distance = -1;
                return HitPart.None;
            }

            float onHead = ShotGeometry.Ellipsoid(head.InverseTransformPoint(origin) - headCentre, head.InverseTransformVector(direction), headRadii);
            float onBody = ShotGeometry.Capsule(origin, direction, hips.position, neck.position, bodyRadius);
            return ShotGeometry.Classify(onHead, onBody, maxDistance, out distance);
        }
    }
}
