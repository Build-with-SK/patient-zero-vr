// UNVERIFIED IN EDITOR / ON DEVICE
using PatientZero.Core;
using PatientZero.Environment.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace PatientZero.Environment.Mobile
{
    /// <summary>Opt-in non-VR adapter for the existing desktop polling controls. Does not move a rig or implement puzzles.</summary>
    public sealed class MobileTouchAdapter : MonoBehaviour
    {
        private static MobileTouchAdapter active;
        public static bool IsActive => active && active.devices != null;
        public MobileTouchInput Input { get; private set; }
        private MobileVirtualDevices devices;
        private Rect safe;
        private int width, height;
        private GameState state;
        private MobileTouchHud hud;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => active = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInstall()
        {
#if PZ_MOBILE_TOUCH && PZ_MOBILE_NON_XR && PZ_MOBILE_SIMULATOR_OFF && PZ_DESKTOP_MOUSE
            if (!Application.isMobilePlatform && !Application.isEditor) return;
            var root = new GameObject("PatientZero Mobile Touch"); root.AddComponent<MobileTouchAdapter>();
#endif
        }
        private bool PlatformAllowed()
        {
            bool opted = false, nonVr = false, simulatorOff = false;
#if PZ_MOBILE_TOUCH && PZ_MOBILE_NON_XR && PZ_MOBILE_SIMULATOR_OFF && PZ_DESKTOP_MOUSE
            opted = nonVr = simulatorOff = true;
#endif
            for (int i = 0; i < InputSystem.devices.Count; i++)
                if (MobilePlatformPolicy.IsSimulatorLayout(InputSystem.devices[i].layout)) simulatorOff = false;
            var xr = XRGeneralSettings.Instance;
            bool xrStartup = xr && xr.InitManagerOnStart;
            bool loader = xr && xr.Manager && xr.Manager.activeLoader;
            return MobilePlatformPolicy.CanRun(opted, Application.isMobilePlatform, Application.isEditor, nonVr, simulatorOff,
                xrStartup || loader, XRSettings.isDeviceActive);
        }
        private void Awake()
        {
            if (active || !PlatformAllowed()) { enabled = false; return; }
            if (InputSystem.settings.updateMode != InputSettings.UpdateMode.ProcessEventsInDynamicUpdate)
            { Debug.LogError("Mobile touch requires Input System dynamic updates in the non-VR profile.", this); enabled = false; return; }
            active = this; DontDestroyOnLoad(gameObject);
            if (Application.isMobilePlatform)
            {
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
                Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            }
            Reflow(); Input = new MobileTouchInput(CreateLayout()) { FlashlightAvailable = true };
#if PZ_MOBILE_RESTART
            Input.RestartShortcutAvailable = true; // TD enables only after the documented A shortcut is integrated.
#endif
            devices = new MobileVirtualDevices();
            hud = gameObject.AddComponent<MobileTouchHud>(); hud.Bind(this);
        }
        private void OnEnable()
        {
            if (active != this || devices == null) return;
            GameEvents.StateChanged.Subscribe(State); GameEvents.ActStarted.Subscribe(Act); GameEvents.LightingChanged.Subscribe(Lighting);
            InputSystem.onAfterUpdate += Translate;
        }
        private void State(GameStateChanged e)
        { state = e.To; Input.SetState(state); Neutral(); }
        private void Act(ActStarted e) { Input.SetAct(e.Act); Neutral(); }
        private void Lighting(LightingChanged e) => Input.SetLighting(e.State);
        private MobileTouchLayout CreateLayout() => new MobileTouchLayout(width, height, new TouchBounds(safe.x, safe.y, safe.width, safe.height));
        private void Reflow()
        {
            width = Screen.width > 0 ? Screen.width : 640; height = Screen.height > 0 ? Screen.height : 360; safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, width, height);
        }
        private void Translate()
        {
            if (devices == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (!PlatformAllowed()) { enabled = false; return; }
            if (width != Screen.width || height != Screen.height || safe != Screen.safeArea)
            { Reflow(); Input.SetLayout(CreateLayout()); Neutral(); if (hud) hud.Reflow(); }
            Input.SetTransitioning(Services<IScreenFader>.TryGet(out var fader) && fader.IsFading);
            devices.MakeCurrent();
            if (Services<ISettingsProvider>.TryGet(out var settings)) Input.TeleportOnly = settings.Current.Locomotion == LocomotionMode.TeleportOnly;
            var screen = Touchscreen.current;
            if (screen != null)
            {
                double now = Time.realtimeSinceStartupAsDouble; // Input/menus continue during pause; never an AI/gameplay clock.
                for (int i = 0; i < screen.touches.Count; i++)
                {
                    var touch = screen.touches[i]; int id = touch.touchId.ReadValue(); var point = touch.position.ReadValue();
                    var phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began) Input.Down(id, point, now);
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began || phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary) Input.Move(id, point, now);
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled) Input.Up(id, point, now, phase == UnityEngine.InputSystem.TouchPhase.Canceled);
                }
            }
            var frame = Input.Read();
            devices.Write(frame, state, new Vector2(width * .5f, height * .5f));
            // Complete -> Playing is the approved Core full-restart path; pause restart uses the reviewed R shortcut.
            if (Has(frame, MobileActions.Restart) && state == GameState.Complete)
                GameEvents.StateRequested.Raise(new GameStateRequest(GameState.Playing, FailReason.None));
        }
        private static bool Has(MobileInputFrame f, MobileActions a) => (f.Actions & a) != 0;
        private void Neutral() => devices?.Neutral();
        private void Suspend()
        {
            Input?.Reset(); Neutral();
            if (GameStateRules.CanPauseFrom(state)) GameEvents.StateRequested.Raise(new GameStateRequest(GameState.Paused, FailReason.None));
        }
        private void OnApplicationFocus(bool focused) { if (!focused) Suspend(); }
        private void OnApplicationPause(bool paused) { if (paused) Suspend(); }
        private void OnDisable()
        {
            InputSystem.onAfterUpdate -= Translate;
            GameEvents.StateChanged.Unsubscribe(State); GameEvents.ActStarted.Unsubscribe(Act); GameEvents.LightingChanged.Unsubscribe(Lighting);
            if (active != this) return;
            Suspend();
            devices?.Dispose(); devices = null; active = null; if (hud) hud.enabled = false;
        }
    }
}
