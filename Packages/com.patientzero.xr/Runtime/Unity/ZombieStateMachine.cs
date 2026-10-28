using PatientZero.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Events;

namespace PatientZero.XR
{
    [RequireComponent(typeof(ZombieNavigation))]
    public sealed class ZombieStateMachine : MonoBehaviour
    {
        public ZombieSettings Settings = new ZombieSettings();
        [Range(2, 20)] public float TickHz = 10;
        public LayerMask PlayerMask;
        public LayerMask AttackBlockers;
        [Min(0)] public float Damage = 10;
        public UnityEvent<int> OnStateChanged = new UnityEvent<int>();
        public ZombieBrain Brain { get; private set; }
        private ZombieNavigation navigation;
        private readonly Collider[] hits = new Collider[8];
        private double nextTick;
        private bool admitted;
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("PatientZero.AI.Tick");
        private void Awake()
        {
            Brain = new ZombieBrain(Settings, Time.timeAsDouble);
            navigation = GetComponent<ZombieNavigation>();
            nextTick = Time.timeAsDouble + (Mathf.Abs(GetInstanceID() % 100) / 100f) / Mathf.Max(2, TickHz);
        }
        public void SetBudgetAdmission(bool value) { admitted = value; if (!value && navigation) navigation.Stop(); }
        public void Hear(in NoiseEvent noise)
        {
            if (!admitted || Brain == null) return;
            var before = Brain.State;
            Brain.Hear(in noise, NoiseManager.ToPoint(transform.position), Time.timeAsDouble);
            if (before != Brain.State) OnStateChanged.Invoke((int)Brain.State);
        }
        private void Update()
        {
            double now = Time.timeAsDouble;
            if (!admitted || now < nextTick) return;
            nextTick = now + 1 / Mathf.Clamp(TickHz, 2, 20);
            using (TickMarker.Auto())
            {
                var before = Brain.State;
                bool strike = Brain.Tick(now, NoiseManager.ToPoint(transform.position), navigation.PathFailed);
                if (before != Brain.State) OnStateChanged.Invoke((int)Brain.State);
                navigation.Tick(Brain.State, NoiseManager.ToVector(Brain.Target), now);
                if (strike) Strike();
            }
        }
        private void Strike()
        {
            Vector3 origin = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin, Settings.AttackDistance, hits, PlayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var receiver = hits[i].GetComponentInParent<IDamageReceiver>();
                if (receiver == null) continue;
                Vector3 contact = hits[i].ClosestPoint(origin);
                if (Physics.Linecast(origin, contact, AttackBlockers, QueryTriggerInteraction.Ignore)) continue;
                if (receiver.TryApplyDamage(Damage, transform.position)) break;
            }
        }
        private void OnDisable() { if (navigation) navigation.Stop(); }
    }
}
