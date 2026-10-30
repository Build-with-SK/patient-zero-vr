using System;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Environment.Logic
{
    [Flags] public enum MobileActions { None = 0, Throw = 1, Flashlight = 2, Pause = 4, Restart = 8, Step = 16, Start = 32 }
    public readonly struct MobileInputFrame
    {
        public readonly Vector2 Move, Delta;
        public readonly bool LeftHeld, RightHeld;
        public readonly MobileActions Actions;
        public MobileInputFrame(Vector2 move, Vector2 delta, bool left, bool right, MobileActions actions)
        { Move = move; Delta = delta; LeftHeld = left; RightHeld = right; Actions = actions; }
    }
    /// <summary>Fixed ten-pointer capture; buttons never become camera drags. All gesture rules are portable.</summary>
    public sealed class MobileTouchInput
    {
        private struct Pointer { public bool Active, Dragged; public int Id; public MobileControl Control; public Vector2 Start, Last; public double Began; }
        private readonly Pointer[] pointers = new Pointer[10];
        private MobileTouchLayout layout;
        private MobileActions actions;
        private Vector2 delta, movement;
        private bool throwLeft, transitioning;
        private GameState state;
        private double nextLightToggle;
        private LightingState lighting;
        public bool GripHeld { get; private set; }
        public bool AimMode { get; private set; }
        public bool LightOn { get; private set; }
        public bool TeleportOnly { get; set; }
        public Vector2 Movement => movement;
        public bool RestartShortcutAvailable { get; set; }
        public bool FlashlightAvailable { get; set; }
        public MobileTouchLayout Layout => layout;
        public GameState State => state;
        public bool Transitioning => transitioning;
        public void SetTransitioning(bool value) { if (transitioning != value) Reset(); transitioning = value; }
        public ActId Act { get; private set; }
        public void SetAct(ActId act) { Act = act; RestartAct(); }
        public MobileTouchInput(MobileTouchLayout layout) { this.layout = layout ?? throw new ArgumentNullException(nameof(layout)); }
        public void SetLayout(MobileTouchLayout value) { Reset(); layout = value ?? throw new ArgumentNullException(nameof(value)); }
        public void SetState(GameState value) { if (value != state) Reset(); state = value; }
        public void SetLighting(LightingState value) { lighting = value; if (!LightAvailable) LightOn = false; }
        public void Reset()
        { Array.Clear(pointers, 0, pointers.Length); GripHeld = AimMode = throwLeft = false; actions = MobileActions.None; movement = delta = Vector2.zero; }
        public void RestartAct() { Reset(); LightOn = false; nextLightToggle = 0; }
        private bool Playing => state == GameState.Playing && !transitioning;
        public bool LightAvailable => Playing && FlashlightAvailable && (lighting == LightingState.Awakening || lighting == LightingState.WardDim);
        public bool Available(MobileControl control)
        {
            if (!layout.Landscape) return false;
            switch (control)
            {
                case MobileControl.Move: return Playing;
                case MobileControl.Look: return !transitioning && (Playing || state == GameState.Ending);
                case MobileControl.Interact: return Playing;
                case MobileControl.Aim: return Playing && GripHeld;
                case MobileControl.Throw: return Playing && GripHeld;
                case MobileControl.Flashlight: return LightAvailable;
                case MobileControl.Pause: return state == GameState.Playing || state == GameState.Ending || state == GameState.Paused;
                case MobileControl.Restart: return !transitioning && (state == GameState.Complete || state == GameState.Paused && RestartShortcutAvailable);
                case MobileControl.Step: return Playing;
                case MobileControl.Start: return state == GameState.Calibration && !transitioning || state == GameState.Paused;
                default: return false;
            }
        }
        public bool Down(int id, Vector2 position, double now)
        {
            if (!Valid(position, now) || Find(id) >= 0 || !layout.Safe.Contains(position)) return false;
            int slot = -1; for (int i = 0; i < pointers.Length; i++) if (!pointers[i].Active) { slot = i; break; }
            if (slot < 0) return false;
            MobileControl control = MobileControl.None;
            // Buttons have priority over broad look/movement regions.
            for (int i = (int)MobileControl.Interact; i <= (int)MobileControl.Aim; i++)
                if (Available((MobileControl)i) && layout.At((MobileControl)i).Contains(position)) { control = (MobileControl)i; break; }
            if (control == MobileControl.None && Available(MobileControl.Move) && layout.At(MobileControl.Move).Contains(position)) control = MobileControl.Move;
            if (control == MobileControl.None && Available(MobileControl.Look) && layout.At(MobileControl.Look).Contains(position)) control = MobileControl.Look;
            if (control == MobileControl.None) return false;
            if (control == MobileControl.Move || control == MobileControl.Look)
                for (int i = 0; i < pointers.Length; i++) if (pointers[i].Active && pointers[i].Control == control) return false;
            pointers[slot] = new Pointer { Active = true, Id = id, Control = control, Start = position, Last = position, Began = now };
            return true;
        }
        public void Move(int id, Vector2 position, double now)
        {
            int i = Find(id); if (i < 0 || !Valid(position, now) || now < pointers[i].Began) return;
            var p = pointers[i];
            if (p.Control == MobileControl.Move)
            {
                movement = (position - layout.At(MobileControl.Move).Centre) / layout.JoystickRadius;
                float length = movement.magnitude; if (length > 1) movement /= length;
                if (length < .18f) movement = Vector2.zero;
            }
            else if (p.Control == MobileControl.Look)
            {
                if ((position - p.Start).magnitude > layout.TapSlop) p.Dragged = true;
                if (p.Dragged)
                {
                    delta += (position - p.Last) * (900 / layout.Safe.Height);
                    delta.x = Math.Max(-100, Math.Min(100, delta.x)); delta.y = Math.Max(-100, Math.Min(100, delta.y));
                }
            }
            p.Last = position; pointers[i] = p;
        }
        public void Up(int id, Vector2 position, double now, bool cancelled = false)
        {
            int i = Find(id); if (i < 0) return;
            var p = pointers[i]; pointers[i] = default;
            var moveAtRelease = movement;
            if (p.Control == MobileControl.Move) movement = Vector2.zero;
            if (cancelled || !Valid(position, now) || now < p.Began || !Available(p.Control)) return;
            if (p.Control == MobileControl.Move)
            { if (TeleportOnly && moveAtRelease.y > .6f && Playing) actions |= MobileActions.Step; return; }
            if (p.Control == MobileControl.Look)
            {
                if (!p.Dragged && (position - p.Start).magnitude <= layout.TapSlop && now - p.Began <= .3 && Playing) { GripHeld = !GripHeld; AimMode = false; }
                return;
            }
            if (!layout.At(p.Control).Contains(position)) return;
            switch (p.Control)
            {
                case MobileControl.Interact: GripHeld = !GripHeld; AimMode = false; break;
                case MobileControl.Aim: AimMode = !AimMode; break;
                case MobileControl.Throw: if (GripHeld) { throwLeft = true; GripHeld = AimMode = false; actions |= MobileActions.Throw; } break;
                case MobileControl.Flashlight:
                    if (now >= nextLightToggle) { nextLightToggle = now + .55; LightOn = !LightOn; actions |= MobileActions.Flashlight; }
                    break;
                case MobileControl.Pause: actions |= MobileActions.Pause; break;
                case MobileControl.Restart: actions |= MobileActions.Restart; break;
                case MobileControl.Step: actions |= MobileActions.Step; break;
                case MobileControl.Start: actions |= MobileActions.Start; break;
            }
        }
        public MobileInputFrame Read()
        {
            bool looking = false;
            for (int i = 0; i < pointers.Length; i++) if (pointers[i].Active && pointers[i].Control == MobileControl.Look && pointers[i].Dragged) looking = true;
            var result = new MobileInputFrame(Playing && !TeleportOnly ? movement : Vector2.zero, delta, Playing && (GripHeld || throwLeft), looking && (!GripHeld || AimMode) && !throwLeft, actions);
            actions = MobileActions.None; delta = Vector2.zero; throwLeft = false; return result;
        }
        private int Find(int id) { for (int i = 0; i < pointers.Length; i++) if (pointers[i].Active && pointers[i].Id == id) return i; return -1; }
        private static bool Valid(Vector2 p, double now) => MobileTouchLayout.Finite(p.x) && MobileTouchLayout.Finite(p.y) && !double.IsNaN(now) && !double.IsInfinity(now) && now >= 0;
    }
}
