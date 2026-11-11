// Claude-owned integration (owner's playtest, 2026-11-11; E-24).
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// The zombie model's arms and head reach further than its NavMesh agent (0.25 m from the walls), so a zombie that faced a
    /// wall or stood beside one pushed its hands and head through into the next room (owner, 2026-11-11). This eases the model,
    /// never the agent, back from any wall within reach of its arms and head, and lets it return when the wall is gone. It
    /// looks every few frames, at chest and head height, ahead, to the front corners and to the sides. Players, loose props
    /// and zombies (Ignore Raycast) never push it: only fixed walls, doors and furniture. Added to every zombie by ZombieArt.
    /// </summary>
    public sealed class WallClearance : MonoBehaviour
    {
        [Tooltip("The zombie's root (its NavMesh agent): the rays start here, so they never begin inside a wall.")]
        public Transform actor;
        [Tooltip("The model's height in metres: reach and ray heights scale with it.")]
        public float height = 1.66f;
        [Tooltip("How far the arms reach ahead, to the front corners and to the sides, for a 1.66 m model.")]
        public float ahead = .6f, corner = .5f, side = .36f;
        [Tooltip("Furthest the model may be eased off its agent, metres; and how fast it moves, metres per second.")]
        public float maxOffset = .45f, speed = 2.2f;
        [Tooltip("Look for walls every this many frames (staggered between zombies).")]
        public int every = 4;

        private static readonly float[] Heights = { .62f, .86f };
        private const float CornerDegrees = 38f;
        private Vector3 rest, offset, target;
        private int phase;

        /// <summary>How far the model is eased off its agent now (world metres, horizontal).</summary>
        public Vector3 Offset => offset;

        private void Awake()
        {
            rest = transform.localPosition;
            phase = Mathf.Abs(GetInstanceID()) % Mathf.Max(1, every);
        }

        private void OnDisable()
        {
            offset = target = Vector3.zero;
            transform.localPosition = rest;
        }

        private void LateUpdate()
        {
            if (!actor) return;
            if ((Time.frameCount + phase) % Mathf.Max(1, every) == 0) target = Push();
            offset = Vector3.MoveTowards(offset, target, speed * Time.deltaTime);
            Transform parent = transform.parent;
            transform.localPosition = rest + (parent ? parent.InverseTransformVector(offset) : offset);
        }

        /// <summary>The smallest horizontal shift that keeps every reach clear of the walls hit, the deepest per side.</summary>
        private Vector3 Push()
        {
            float k = height / 1.66f;
            Vector3 forward = actor.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 1e-4f) return Vector3.zero;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 left = Quaternion.AngleAxis(-CornerDegrees, Vector3.up) * forward, rightCorner = Quaternion.AngleAxis(CornerDegrees, Vector3.up) * forward;
            float plusX = 0, minusX = 0, plusZ = 0, minusZ = 0;
            for (int h = 0; h < Heights.Length; h++)
            {
                Vector3 from = actor.position + Vector3.up * (Heights[h] * height);
                Probe(from, forward, ahead * k, ref plusX, ref minusX, ref plusZ, ref minusZ);
                Probe(from, left, corner * k, ref plusX, ref minusX, ref plusZ, ref minusZ);
                Probe(from, rightCorner, corner * k, ref plusX, ref minusX, ref plusZ, ref minusZ);
                Probe(from, right, side * k, ref plusX, ref minusX, ref plusZ, ref minusZ);
                Probe(from, -right, side * k, ref plusX, ref minusX, ref plusZ, ref minusZ);
            }

            return Vector3.ClampMagnitude(new Vector3(plusX + minusX, 0, plusZ + minusZ), maxOffset);
        }

        private static void Probe(Vector3 from, Vector3 direction, float reach, ref float plusX, ref float minusX, ref float plusZ, ref float minusZ)
        {
            if (!Physics.Raycast(from, direction, out RaycastHit hit, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            Collider c = hit.collider;
            if (c.attachedRigidbody || c is CharacterController) return; // a player or a loose prop: not a wall
            Vector3 normal = hit.normal;
            normal.y = 0;
            if (normal.sqrMagnitude < 1e-4f) return;
            normal.Normalize();
            // How far the reach goes into the wall, measured across the wall.
            float depth = (reach - hit.distance) * Mathf.Max(0, Vector3.Dot(-direction, normal));
            Vector3 push = normal * depth;
            if (push.x > plusX) plusX = push.x;
            if (push.x < minusX) minusX = push.x;
            if (push.z > plusZ) plusZ = push.z;
            if (push.z < minusZ) minusZ = push.z;
        }
    }
}
