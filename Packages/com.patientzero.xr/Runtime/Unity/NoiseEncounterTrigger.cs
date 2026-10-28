using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    [RequireComponent(typeof(EncounterSequence))]
    public sealed class NoiseEncounterTrigger : MonoBehaviour, INoiseListener
    {
        public NoiseManager Manager;
        public NoiseCategory Category = NoiseCategory.Bottle;
        [Range(0, 1)] public float HearingThreshold = .15f;
        private EncounterSequence sequence;
        private bool registered;
        private void Awake() => sequence = GetComponent<EncounterSequence>();
        private void Start() => Register();
        private void OnEnable() { if (sequence) Register(); }
        public void Bind(NoiseManager value)
        {
            Unregister(); Manager = value; if (isActiveAndEnabled) Register();
        }
        private void Register()
        {
            if (!registered && Manager && Manager.Bus != null) registered = Manager.Bus.Register(this);
        }
        private void Unregister()
        {
            if (registered && Manager && Manager.Bus != null) Manager.Bus.Unregister(this);
            registered = false;
        }
        private void OnDisable() => Unregister();
        public void Hear(in NoiseEvent noise)
        {
            if (noise.Category == Category && noise.LevelAt(NoiseManager.ToPoint(transform.position)) >= HearingThreshold)
                sequence.TryBegin();
        }
    }
}
