using PatientZero.Core;
using UnityEngine;
using UnityEngine.Events;

namespace PatientZero.XR
{
    [DefaultExecutionOrder(-100)]
    public sealed class TrackingLossGuard : MonoBehaviour
    {
        [Tooltip("SDK hand interactors only. Never disable the hand data provider or rig.")]
        public Behaviour[] HandInteractors = new Behaviour[0];
        public Behaviour[] ControllerInteractors = new Behaviour[0];
        [Min(0)] public float StableTrackingSeconds = .25f;
        [Min(.02f)] public float StaleInputSeconds = .1f;
        public UnityEvent OnCancelSelection = new UnityEvent();
        public InteractionSource ActiveSource { get; private set; }
        private TrackingRecovery recovery;
        private bool focused = true;
        private bool paused;
        private double lastSubmission = double.NegativeInfinity;
        private void Awake()
        {
            recovery = new TrackingRecovery(StableTrackingSeconds);
            SetEnabled(HandInteractors, false); SetEnabled(ControllerInteractors, false);
        }
        public void Submit(bool handValid, bool controllerValid)
        {
            lastSubmission = Time.unscaledTimeAsDouble;
            var decision = recovery.Step(handValid, controllerValid, Time.unscaledTimeAsDouble, focused && !paused);
            // Cancel while selectors still exist; SDK disable releases selections immediately.
            if (decision.CancelSelection) OnCancelSelection.Invoke();
            ActiveSource = decision.Source;
            SetEnabled(HandInteractors, decision.Source == InteractionSource.Hand);
            SetEnabled(ControllerInteractors, decision.Source == InteractionSource.Controller);
        }
        private void LateUpdate()
        {
            if (Time.unscaledTimeAsDouble - lastSubmission > Mathf.Max(.02f, StaleInputSeconds)) Submit(false, false);
            // SDK active-state groups can refresh their children during Update; keep unsafe selectors off.
            if (ActiveSource != InteractionSource.Hand) SetEnabled(HandInteractors, false);
            if (ActiveSource != InteractionSource.Controller) SetEnabled(ControllerInteractors, false);
        }
        private static void SetEnabled(Behaviour[] list, bool value)
        {
            for (int i = 0; i < list.Length; i++) if (list[i] && list[i].enabled != value) list[i].enabled = value;
        }
        public void SetFocus(bool value) { focused = value; if (!value) Submit(false, false); }
        private void OnApplicationFocus(bool value) => SetFocus(value);
        private void OnApplicationPause(bool value) { paused = value; if (value) Submit(false, false); }
        private void OnDisable()
        {
            if (ActiveSource != InteractionSource.None) OnCancelSelection.Invoke();
            ActiveSource = InteractionSource.None;
            SetEnabled(HandInteractors, false); SetEnabled(ControllerInteractors, false);
            recovery = new TrackingRecovery(StableTrackingSeconds);
        }
    }
}
