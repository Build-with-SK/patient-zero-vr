using System;
using UnityEngine;

namespace PatientZero.Environment.Logic
{
    public enum MobileControl { None, Move, Look, Interact, Throw, Flashlight, Pause, Restart, Step, Start, Aim }
    public readonly struct TouchBounds
    {
        public readonly float X, Y, Width, Height;
        public TouchBounds(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
        public Vector2 Centre => new Vector2(X + Width / 2, Y + Height / 2);
        public bool Contains(Vector2 p) => p.x >= X && p.x < X + Width && p.y >= Y && p.y < Y + Height;
    }
    /// <summary>Pixel-space layout clipped to a landscape safe area. No camera or engine/UI dependencies.</summary>
    public sealed class MobileTouchLayout
    {
        public readonly TouchBounds Safe;
        public readonly bool Landscape;
        public readonly float ButtonSize, JoystickRadius, TapSlop;
        private readonly TouchBounds[] bounds = new TouchBounds[11];
        public MobileTouchLayout(float screenWidth, float screenHeight, TouchBounds safe)
        {
            if (!Finite(screenWidth) || !Finite(screenHeight) || screenWidth <= 0 || screenHeight <= 0 ||
                !Finite(safe.X) || !Finite(safe.Y) || !Finite(safe.Width) || !Finite(safe.Height) || safe.Width <= 0 || safe.Height <= 0)
                throw new ArgumentException("A finite screen and non-empty safe area are required");
            float x = Math.Max(0, safe.X), y = Math.Max(0, safe.Y);
            float w = Math.Min(screenWidth, safe.X + safe.Width) - x, h = Math.Min(screenHeight, safe.Y + safe.Height) - y;
            if (w <= 0 || h <= 0) throw new ArgumentException("Safe area must intersect the screen");
            Safe = new TouchBounds(x, y, w, h); Landscape = w >= h;
            ButtonSize = Math.Min(h * .14f, w * .095f); float gap = ButtonSize * .15f;
            JoystickRadius = Math.Min(h * .19f, w * .13f); TapSlop = h * .018f;
            bounds[(int)MobileControl.Move] = new TouchBounds(x + gap, y + gap, JoystickRadius * 2, JoystickRadius * 2);
            bounds[(int)MobileControl.Look] = new TouchBounds(x + w * .4f, y + h * .22f, w * .6f, h * .6f);
            bounds[(int)MobileControl.Interact] = new TouchBounds(x + w - ButtonSize - gap, y + gap, ButtonSize, ButtonSize);
            bounds[(int)MobileControl.Throw] = new TouchBounds(x + w - ButtonSize * 2 - gap * 2, y + gap, ButtonSize, ButtonSize);
            bounds[(int)MobileControl.Step] = new TouchBounds(x + w - ButtonSize * 3 - gap * 3, y + gap, ButtonSize, ButtonSize);
            bounds[(int)MobileControl.Flashlight] = new TouchBounds(x + gap, y + h - ButtonSize - gap, ButtonSize, ButtonSize);
            bounds[(int)MobileControl.Aim] = new TouchBounds(x + ButtonSize + gap * 2, y + h - ButtonSize - gap, ButtonSize, ButtonSize);
            bounds[(int)MobileControl.Pause] = new TouchBounds(x + w - ButtonSize - gap, y + h - ButtonSize - gap, ButtonSize, ButtonSize);
            bounds[(int)MobileControl.Restart] = new TouchBounds(x + w / 2 - ButtonSize * 1.5f, y + h / 2 - ButtonSize / 2, ButtonSize * 1.4f, ButtonSize);
            bounds[(int)MobileControl.Start] = new TouchBounds(x + w / 2 + ButtonSize * .1f, y + h / 2 - ButtonSize / 2, ButtonSize * 1.4f, ButtonSize);
        }
        public TouchBounds At(MobileControl control) => bounds[(int)control];
        public static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    }
    public static class MobilePlatformPolicy
    {
        public static bool IsSimulatorLayout(string name) => !string.IsNullOrEmpty(name) && name.StartsWith("XRSimulated", StringComparison.OrdinalIgnoreCase);
        public static bool CanRun(bool optedIn, bool phoneOrTablet, bool editorPreview, bool nonVrConfigured, bool simulatorDisabled, bool xrStartup, bool xrActive)
            => optedIn && (phoneOrTablet || editorPreview) && nonVrConfigured && simulatorDisabled && !xrStartup && !xrActive;
    }
}
