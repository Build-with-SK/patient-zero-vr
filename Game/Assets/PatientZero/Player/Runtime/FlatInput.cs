using UnityEngine;
#if PZ_DESKTOP_MOUSE
using UnityEngine.InputSystem;
#endif
namespace PatientZero.Player
{
    /// <summary>
    /// Integration E-15 (owner final-release mission, 2026-10-30): one input state for the flat-screen builds. Windows reads
    /// the keyboard and mouse (PZ_DESKTOP_MOUSE); phones and iPad read the on-screen touch controls (PZ_TOUCH, TouchControls).
    /// DesktopLocomotion and DesktopInteraction read only this, so every flat build plays through the same code. Quest builds
    /// define neither symbol: nothing fills it and the adapters are compiled out.
    /// </summary>
    public static class FlatInput
    {
        public const float MouseDegreesPerPixel = .12f;

        /// <summary>
        /// True while a puzzle part (plank, valve, lever) is gripped: mouse movement then works the part instead of turning
        /// the view. Set by DesktopInteraction; kept across frames.
        /// </summary>
        public static bool PuzzleGripped;

        /// <summary>Strafe (x) and forward (y), magnitude at most 1.</summary>
        public static Vector2 Move;
        /// <summary>Yaw (x) and pitch (y) in degrees this frame.</summary>
        public static Vector2 Look;
        /// <summary>Screen pixels this frame while grabbing; moves a held puzzle part.</summary>
        public static Vector2 Drag;
        public static bool GrabPressed, GrabHeld, GrabReleased, Throw, Teleport, Flashlight, Pause, Confirm;
        /// <summary>
        /// E-22: one press of the struggle out of a zombie's grab. E-23: nothing on screen says which key, so any key but Esc and
        /// any mouse button counts (a tap on a touch screen).
        /// </summary>
        public static bool Struggle;
        private static int polledFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { polledFrame = -1; PuzzleGripped = false; Clear(); }

        /// <summary>Reads the devices once per frame; every reader calls it, the first call of a frame does the work.</summary>
        public static void Poll()
        {
            if (polledFrame == Time.frameCount) return;
            polledFrame = Time.frameCount;
            Clear();
#if PZ_DESKTOP_MOUSE
            Keyboard keys = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keys != null)
            {
                Move = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
                if (Move.sqrMagnitude > 1) Move.Normalize();
                Throw = keys.qKey.wasPressedThisFrame;
                Teleport = keys.tKey.wasPressedThisFrame;
                Flashlight = keys.fKey.wasPressedThisFrame;
                Pause = keys.escapeKey.wasPressedThisFrame;
                Confirm = keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame;
                Struggle = keys.anyKey.wasPressedThisFrame && !keys.escapeKey.wasPressedThisFrame;
            }

            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                // Free mouse look while the cursor is captured (the run is live); holding the right button also looks when
                // it is not (a window without focus capture). A gripped puzzle part takes the mouse instead.
                bool look = Cursor.lockState == CursorLockMode.Locked || mouse.rightButton.isPressed;
                if (look && !PuzzleGripped) Look = new Vector2(delta.x, -delta.y) * MouseDegreesPerPixel;
                GrabPressed = mouse.leftButton.wasPressedThisFrame;
                GrabHeld = mouse.leftButton.isPressed;
                GrabReleased = mouse.leftButton.wasReleasedThisFrame;
                if (GrabHeld) Drag = delta;
                Struggle |= mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
            }
#elif PZ_TOUCH
            if (TouchControls.Active != null) TouchControls.Active.Fill();
#endif
            Struggle |= GrabPressed;
        }

        public static void Clear()
        {
            Move = Look = Drag = Vector2.zero;
            GrabPressed = GrabHeld = GrabReleased = Throw = Teleport = Flashlight = Pause = Confirm = Struggle = false;
        }
    }
}
