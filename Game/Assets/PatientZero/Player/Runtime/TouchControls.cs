using PatientZero.Core;
using UnityEngine;
using UnityEngine.UI;
#if PZ_TOUCH
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
namespace PatientZero.Player
{
    /// <summary>
    /// Integration E-15: phone and iPad controls (PZ_TOUCH), filling FlatInput. Landscape layout inside Screen.safeArea:
    /// left thumb = floating move stick; right thumb = look; a short tap on the right = grab or drop what is under the
    /// centre dot; dragging while holding moves the held puzzle part. Buttons: GRAB (hold), THROW, JUMP (teleport to the
    /// next anchor), LIGHT (flashlight), II (pause). The HUD shows only while the run is live; menus use the world-space UI.
    /// DesktopLocomotion adds this component at runtime in PZ_TOUCH builds; it does nothing anywhere else.
    /// In the Editor and on Windows, mouse input is turned into touches (Input System TouchSimulation) for testing.
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        public static TouchControls Active { get; private set; }
#if PZ_TOUCH
        private const float TapSeconds = .3f, StickRadiusFraction = .12f, LookDegreesPerScreenHeight = 110f;
        private RectTransform safeRoot, grab, throwButton, jump, lightButton, pause, stickBase, stickKnob;
        private GameObject hud;
        private Rect appliedSafeArea;
        private int stickId = -1, lookId = -1, grabId = -1;
        private Vector2 stickOrigin, lookStart;
        private float lookStartTime;
        private bool holdToggle, live;

        private void Awake()
        {
            Active = this;
#if UNITY_EDITOR || UNITY_STANDALONE
            UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
#endif
            BuildHud();
        }

        private void OnEnable() { GameEvents.StateChanged.Subscribe(State); }
        private void OnDisable() { GameEvents.StateChanged.Unsubscribe(State); }
        private void OnDestroy() { if (Active == this) Active = null; }

        private void State(GameStateChanged state)
        {
            live = GameStateRules.IsSimulationLive(state.To);
            if (hud) hud.SetActive(live);
            if (!live) { stickId = lookId = grabId = -1; holdToggle = false; if (stickBase) stickBase.gameObject.SetActive(false); }
        }

        /// <summary>Called by FlatInput.Poll once per frame, after it cleared the previous frame's values.</summary>
        public void Fill()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null || !live) return;
            FitSafeArea();
            Rect safe = Screen.safeArea;
            float unit = Mathf.Min(safe.width, safe.height);
            float scale = 1080f / Mathf.Max(1, Screen.height);
            bool grabButtonDown = false;

            foreach (TouchControl touch in screen.touches)
            {
                bool began = touch.press.wasPressedThisFrame, ended = touch.press.wasReleasedThisFrame;
                if (!touch.press.isPressed && !ended) continue;
                int id = touch.touchId.ReadValue();
                Vector2 p = touch.position.ReadValue();
                Vector2 d = touch.delta.ReadValue();

                if (began)
                {
                    if (Hit(pause, p)) { FlatInput.Pause = true; continue; }
                    if (Hit(throwButton, p)) { FlatInput.Throw = true; holdToggle = false; continue; }
                    if (Hit(jump, p)) { FlatInput.Teleport = true; continue; }
                    if (Hit(lightButton, p)) { FlatInput.Flashlight = true; continue; }
                    if (Hit(grab, p)) { grabId = id; if (!holdToggle) FlatInput.GrabPressed = true; continue; }
                    if (p.x < safe.x + safe.width * .4f && stickId < 0)
                    {
                        stickId = id; stickOrigin = p;
                        stickBase.gameObject.SetActive(true);
                        stickBase.position = p;
                        continue;
                    }

                    if (lookId < 0) { lookId = id; lookStart = p; lookStartTime = Time.unscaledTime; }
                    continue;
                }

                if (id == grabId)
                {
                    if (ended) { grabId = -1; if (!holdToggle) FlatInput.GrabReleased = true; }
                    else grabButtonDown = true;
                }
                else if (id == stickId)
                {
                    if (ended) { stickId = -1; stickBase.gameObject.SetActive(false); continue; }
                    Vector2 offset = Vector2.ClampMagnitude((p - stickOrigin) / (unit * StickRadiusFraction), 1);
                    FlatInput.Move = offset;
                    stickKnob.anchoredPosition = offset * (stickBase.rect.width * .5f);
                }
                else if (id == lookId)
                {
                    if (ended)
                    {
                        lookId = -1;
                        bool tap = Time.unscaledTime - lookStartTime < TapSeconds && (p - lookStart).magnitude < unit * .03f;
                        if (tap) { if (holdToggle) { holdToggle = false; FlatInput.GrabReleased = true; } else { holdToggle = true; FlatInput.GrabPressed = true; } }
                        continue;
                    }

                    if (holdToggle || grabId >= 0) FlatInput.Drag += d * scale;
                    else FlatInput.Look += new Vector2(d.x, -d.y) * (LookDegreesPerScreenHeight / Mathf.Max(1, Screen.height));
                }
            }

            FlatInput.GrabHeld = holdToggle || grabButtonDown || grabId >= 0;
            if (FlatInput.Throw) holdToggle = false;
        }

        private static bool Hit(RectTransform button, Vector2 screenPoint) =>
            button && button.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(button, screenPoint, null);

        private void FitSafeArea()
        {
            Rect safe = Screen.safeArea;
            if (safe == appliedSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
            appliedSafeArea = safe;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }

        private void BuildHud()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud = new GameObject("Touch HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(hud);
            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            safeRoot = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(hud.transform, false);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            grab = Button("GRAB", new Vector2(1, 0), new Vector2(-190, 210), 230, font);
            throwButton = Button("THROW", new Vector2(1, 0), new Vector2(-430, 130), 160, font);
            lightButton = Button("LIGHT", new Vector2(1, 0), new Vector2(-430, 330), 160, font);
            jump = Button("JUMP", new Vector2(1, 0), new Vector2(-190, 460), 160, font);
            pause = Button("II", new Vector2(1, 1), new Vector2(-90, -90), 120, font);

            stickBase = Circle("Stick", safeRoot, 300, new Color(1, 1, 1, .12f));
            stickKnob = Circle("Knob", stickBase, 130, new Color(1, 1, 1, .35f));
            stickBase.gameObject.SetActive(false);
            hud.SetActive(false);
        }

        private RectTransform Button(string label, Vector2 anchor, Vector2 position, float size, Font font)
        {
            RectTransform button = Circle(label, safeRoot, size, new Color(.05f, .07f, .08f, .55f));
            button.anchorMin = button.anchorMax = anchor;
            button.anchoredPosition = position;
            var text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(button, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = font;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1, 1, 1, .9f);
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        private static RectTransform Circle(string name, Transform parent, float size, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image.rectTransform;
        }
#else
        public void Fill() { }
#endif
    }
}
