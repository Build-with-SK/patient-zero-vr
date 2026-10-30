// UNVERIFIED IN EDITOR / ON DEVICE
using PatientZero.Core;
using PatientZero.Environment.Mobile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace PatientZero.Atmosphere.Mobile
{
    /// <summary>Opt-in non-VR headlamp input; the existing lighting controller remains the sole light writer.</summary>
    public sealed class NonVrHeadlampInput : MonoBehaviour
    {
        public LightingController Lighting;
        private bool configured, paused;
        private double nextToggle;
        private void OnEnable()
        {
            bool profile = false;
#if PZ_MOBILE_TOUCH && PZ_MOBILE_NON_XR && PZ_MOBILE_SIMULATOR_OFF || PZ_NON_VR_HEADLAMP && PZ_DESKTOP_MOUSE
            profile = true;
#endif
            if (!profile) { enabled = false; return; }
            GameEvents.ActStarted.Subscribe(Act); GameEvents.StateChanged.Subscribe(State);
        }
        private void State(GameStateChanged e) => paused = !GameStateRules.IsSimulationLive(e.To);
        private void OnDisable() { GameEvents.ActStarted.Unsubscribe(Act); GameEvents.StateChanged.Unsubscribe(State); if (Lighting) Lighting.ConfigureMobileHeadlamp(false); configured = false; }
        private void Act(ActStarted e) { if (Lighting) Lighting.SetMobileHeadlamp(false); }
        private void Update()
        {
            if (!Lighting) return;
            bool mobile = MobileTouchAdapter.IsActive;
#if PZ_NON_VR_HEADLAMP && PZ_DESKTOP_MOUSE
            var xr = XRGeneralSettings.Instance;
            bool vr = XRSettings.isDeviceActive || xr && (xr.InitManagerOnStart || xr.Manager && xr.Manager.activeLoader);
            mobile |= !Application.isMobilePlatform && !vr;
#endif
            if (configured != mobile) { Lighting.ConfigureMobileHeadlamp(mobile); configured = mobile; }
            double now = Time.realtimeSinceStartupAsDouble;
            if (mobile && !paused && Lighting.HeadlampUsable && now >= nextToggle && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            { nextToggle = now + .5; Lighting.SetMobileHeadlamp(!Lighting.MobileHeadlampRequested); Cues.Play(CueId.UiSelect, Vector3.zero, NoiseOrigin.Ambient); }
        }
    }
}
