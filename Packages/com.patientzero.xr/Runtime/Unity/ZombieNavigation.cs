using PatientZero.Core;
using UnityEngine;
using UnityEngine.AI;

namespace PatientZero.XR
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ZombieNavigation : MonoBehaviour
    {
        [Min(.1f)] public float WalkSpeed = 1.2f;
        [Min(.1f)] public float ChaseSpeed = 2.4f;
        [Min(.1f)] public float RepathSeconds = .35f;
        public NavMeshAgent Agent { get; private set; }
        public bool PathFailed { get; private set; }
        private double nextPath;
        private Vector3 lastTarget = Vector3.positiveInfinity;
        private void Awake() => Agent = GetComponent<NavMeshAgent>();
        public void Stop()
        {
            if (Agent && Agent.isOnNavMesh) { Agent.isStopped = true; Agent.ResetPath(); }
        }
        public void Tick(ZombieState state, Vector3 target, double now)
        {
            if (!Agent || !Agent.enabled || !Agent.isOnNavMesh) { PathFailed = true; return; }
            bool moving = state == ZombieState.Investigate || state == ZombieState.Chase;
            if (!moving) { Stop(); PathFailed = false; return; }
            Agent.isStopped = false;
            Agent.speed = state == ZombieState.Chase ? ChaseSpeed : WalkSpeed;
            if (now >= nextPath && (!Agent.hasPath || (target - lastTarget).sqrMagnitude > .04f))
            {
                nextPath = now + RepathSeconds;
                if (!NavMesh.SamplePosition(target, out var hit, 2f, Agent.areaMask)) { PathFailed = true; Stop(); return; }
                lastTarget = target;
                PathFailed = !Agent.SetDestination(hit.position);
            }
            if (!Agent.pathPending && Agent.hasPath && Agent.pathStatus != NavMeshPathStatus.PathComplete) PathFailed = true;
        }
    }
}
