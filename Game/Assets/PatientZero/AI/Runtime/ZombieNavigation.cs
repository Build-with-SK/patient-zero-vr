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
        private NavMeshPath candidatePath;
        private float blockedSince = -1;
        public bool PathFailed { get; private set; }
        public float Speed => agent && agent.enabled ? agent.velocity.magnitude : 0;
        private void Awake() { agent = GetComponent<NavMeshAgent>(); candidatePath = new NavMeshPath(); agent.enabled = false; }
        public void SetSimulationActive(bool value)
        {
            if (!agent) return;
            if (!value) Stop();
            agent.enabled = value;
        }
        public bool Warp(Vector3 position)
        {
            lastTarget = Vector3.positiveInfinity; nextPath = 0; blockedSince = -1;
            PathFailed = true;
            if (!agent || !agent.enabled || !NavMesh.SamplePosition(position, out var hit, 2, agent.areaMask)) return false;
            PathFailed = !agent.Warp(hit.position);
            return !PathFailed;
        }
        public void Stop()
        { if (agent && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }
        public void Drive(bool moving, Vector3 target, float speed, float now)
        {
            PathFailed = false;
            if (!agent || !agent.enabled || !agent.isOnNavMesh) { PathFailed = true; return; }
            if (!moving) { blockedSince = -1; Stop(); return; }
            // Cues/window landmarks are at ear height. This ward is planar: sample the
            // actor's floor, otherwise SamplePosition can choose the walkable ceiling.
            target.y = transform.position.y;
            // Ask the NavMesh for the complete route first. A direct line to a target can cross
            // a wall even when the mesh has a valid route around it.
            if (now >= nextPath && (!agent.hasPath || (lastTarget - target).sqrMagnitude > .04f))
            {
                nextPath = now + .35f;
                if (!NavMesh.SamplePosition(target, out var hit, 2, agent.areaMask) ||
                    !agent.CalculatePath(hit.position, candidatePath) || candidatePath.status != NavMeshPathStatus.PathComplete)
                { PathFailed = true; Stop(); return; }
                if ((lastTarget - target).sqrMagnitude > .04f) blockedSince = -1;
                lastTarget = target;
                if (!agent.SetPath(candidatePath)) { PathFailed = true; Stop(); return; }
            }
            Vector3 direction = agent.desiredVelocity.sqrMagnitude > .001f ? agent.desiredVelocity.normalized : (agent.steeringTarget - transform.position).normalized;
            if (Physics.CapsuleCast(transform.position + Vector3.up * .4f, transform.position + Vector3.up * 1.4f,
                .2f, direction, speed * .1f + .05f, Obstacles, QueryTriggerInteraction.Ignore))
            {
                if (blockedSince < 0) blockedSince = now;
                // Stop at moving door leaves without erasing the route. Give a briefly opening
                // door one second, then let the brain enter its bounded search/recovery state.
                agent.isStopped = true; PathFailed = now - blockedSince >= 1;
                return;
            }
            blockedSince = -1;
            agent.speed = speed; agent.isStopped = false;
            if (!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete) { PathFailed = true; Stop(); }
        }
    }
}
