using UnityEngine;

namespace PatientZero.XR
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class HeldObjectSafety : MonoBehaviour
    {
        [Tooltip("Wire from SDK selection and unselection events. Normal throwing stays SDK-owned.")]
        public bool Selected;
        [Tooltip("SDK interactables/grabbable that must cancel on unsafe release.")]
        public Behaviour[] SelectionComponents = new Behaviour[0];
        private Rigidbody body;
        private bool[] previouslyEnabled;
        private bool suppressVelocity;
        private double suppressUntil;
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            previouslyEnabled = new bool[SelectionComponents.Length];
        }
        private void OnEnable()
        {
            if (!TrackingSafetyHub.Register(this))
            {
                Debug.LogError("Tracking safety registry full; interaction disabled for this prop.", this);
                for (int i = 0; i < SelectionComponents.Length; i++) if (SelectionComponents[i]) SelectionComponents[i].enabled = false;
            }
        }
        private void OnDisable() { TrackingSafetyHub.Unregister(this); Selected = false; }
        public void SelectionBegan() => Selected = true;
        public void SelectionEnded() => Selected = false;
        // Connect both hand guards' OnCancelSelection. Conservatively drop any selected prop.
        public void CancelForTrackingLoss()
        {
            if (!Selected) return;
            Selected = false;
            for (int i = 0; i < SelectionComponents.Length; i++)
            {
                previouslyEnabled[i] = SelectionComponents[i] && SelectionComponents[i].enabled;
                if (SelectionComponents[i]) SelectionComponents[i].enabled = false;
            }
            // SDK owns kinematic state. Never override a kinematic lock; wait until it releases.
            suppressVelocity = true;
            suppressUntil = Time.unscaledTimeAsDouble + .3;
            ClearVelocity();
        }
        private void LateUpdate() { if (suppressVelocity) ClearVelocity(); }
        private void FixedUpdate()
        {
            if (!suppressVelocity) return;
            ClearVelocity();
            if (Time.unscaledTimeAsDouble < suppressUntil) return;
            for (int i = 0; i < SelectionComponents.Length; i++)
                if (SelectionComponents[i]) SelectionComponents[i].enabled = previouslyEnabled[i];
            suppressVelocity = false;
        }
        private void ClearVelocity()
        {
            if (!body || body.isKinematic) return;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        }
    }
}
