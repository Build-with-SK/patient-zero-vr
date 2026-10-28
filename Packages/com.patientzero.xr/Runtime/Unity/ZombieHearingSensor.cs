using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    [RequireComponent(typeof(ZombieStateMachine))]
    public sealed class ZombieHearingSensor : MonoBehaviour, INoiseListener
    {
        public NoiseManager Manager;
        private ZombieStateMachine machine;
        private bool registered;
        private void Awake() => machine = GetComponent<ZombieStateMachine>();
        private void Start() => Register();
        private void OnEnable() { if (machine) Register(); }
        public void Bind(NoiseManager manager)
        {
            Unregister(); Manager = manager;
            if (isActiveAndEnabled) Register();
        }
        private void Register()
        {
            if (registered) return;
            if (!Manager || Manager.Bus == null)
            {
                Debug.LogError("Zombie hearing requires an initialized NoiseManager binding.", this); return;
            }
            registered = Manager.TryAdmitZombie(this);
            machine.SetBudgetAdmission(registered);
            if (!registered) Debug.LogWarning("Active zombie budget reached; zombie is suspended.", this);
        }
        private void Unregister()
        {
            if (registered && Manager && Manager.Bus != null) Manager.ReleaseZombie(this);
            registered = false;
            if (machine) machine.SetBudgetAdmission(false);
        }
        private void OnDisable() => Unregister();
        public void RetryAdmission() => Register();
        public void Hear(in NoiseEvent noise)
        {
            if (noise.SourceId != gameObject.GetInstanceID()) machine.Hear(in noise);
        }
    }
}
