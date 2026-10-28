// UNVERIFIED IN EDITOR
using UnityEngine;
using UnityEngine.AI;

namespace PatientZero.AI
{
    /// <summary>Bounded NavMesh adapter. Environment/door-only mask must exclude player and zombie layers.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZombieNavigation : MonoBehaviour
    {
        public LayerMask Obstacles = 1;
        private NavMeshAgent agent;
        private float nextPath;
        private Vector3 lastTarget = Vector3.positiveInfinity;
        public bool PathFailed { get; private set; }
        public float Speed => agent && agent.enabled ? agent.velocity.magnitude : 0;
        private void Awake() => agent = GetComponent<NavMeshAgent>();
        public bool Warp(Vector3 position)
        {
            lastTarget = Vector3.positiveInfinity; nextPath = 0;
            if (!agent || !NavMesh.SamplePosition(position, out var hit, 2, agent.areaMask)) return false;
            return agent.Warp(hit.position);
        }
        public void Stop()
        { if (agent && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }
        public void Drive(bool moving, Vector3 target, float speed, float now)
        {
            PathFailed = false;
            if (!agent || !agent.enabled || !agent.isOnNavMesh) { PathFailed = true; return; }
            if (!moving) { Stop(); return; }
            Vector3 direction = agent.desiredVelocity.sqrMagnitude > .001f ? agent.desiredVelocity.normalized : (target - transform.position).normalized;
            if (Physics.CapsuleCast(transform.position + Vector3.up * .4f, transform.position + Vector3.up * 1.4f,
                .2f, direction, speed * .1f + .05f, Obstacles, QueryTriggerInteraction.Ignore))
            { PathFailed = true; Stop(); return; }
            agent.speed = speed; agent.isStopped = false;
            if (now >= nextPath && (!agent.hasPath || (lastTarget - target).sqrMagnitude > .04f))
            {
                nextPath = now + .35f;
                if (!NavMesh.SamplePosition(target, out var hit, 2, agent.areaMask)) { PathFailed = true; Stop(); return; }
                lastTarget = target; PathFailed = !agent.SetDestination(hit.position);
            }
            if (!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete) { PathFailed = true; Stop(); }
        }
    }
}
