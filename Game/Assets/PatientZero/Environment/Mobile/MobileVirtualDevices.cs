// UNVERIFIED IN EDITOR
using System;
using PatientZero.Core;
using PatientZero.Environment.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PatientZero.Environment.Mobile
{
    /// <summary>Standard input-device boundary, shared by the opt-in adapter and native mapping tests.</summary>
    public sealed class MobileVirtualDevices : IDisposable
    {
        private readonly Keyboard previousKeyboard;
        private readonly Mouse previousMouse;
        public Keyboard KeyboardDevice { get; private set; }
        public Mouse MouseDevice { get; private set; }
        public MobileVirtualDevices()
        {
            previousKeyboard = Keyboard.current; previousMouse = Mouse.current;
            KeyboardDevice = InputSystem.AddDevice<Keyboard>("PatientZeroMobileKeyboard");
            MouseDevice = InputSystem.AddDevice<Mouse>("PatientZeroMobileMouse"); MakeCurrent();
        }
        public void MakeCurrent() { KeyboardDevice?.MakeCurrent(); MouseDevice?.MakeCurrent(); }
        public void Write(MobileInputFrame frame, GameState state, Vector2 screenCentre)
        {
            if (KeyboardDevice == null) return;
            Set(Key.W, frame.Move.y > .25f); Set(Key.S, frame.Move.y < -.25f); Set(Key.D, frame.Move.x > .25f); Set(Key.A, frame.Move.x < -.25f);
            Set(Key.Q, Has(frame, MobileActions.Throw)); Set(Key.F, Has(frame, MobileActions.Flashlight)); Set(Key.T, Has(frame, MobileActions.Step));
            Set(Key.Enter, Has(frame, MobileActions.Start) && state == GameState.Calibration);
            Set(Key.Escape, Has(frame, MobileActions.Pause) || Has(frame, MobileActions.Start) && state == GameState.Paused);
            Set(Key.R, Has(frame, MobileActions.Restart) && state == GameState.Paused);
            InputState.Change(MouseDevice.leftButton, frame.LeftHeld ? 1f : 0f); InputState.Change(MouseDevice.rightButton, frame.RightHeld ? 1f : 0f);
            InputState.Change(MouseDevice.delta, frame.Delta); InputState.Change(MouseDevice.position, screenCentre);
        }
        private static bool Has(MobileInputFrame f, MobileActions a) => (f.Actions & a) != 0;
        private void Set(Key key, bool value) => InputState.Change(KeyboardDevice[key], value ? 1f : 0f);
        public void Neutral() => Write(default, GameState.Boot, Vector2.zero);
        public void Dispose()
        {
            Neutral();
            if (KeyboardDevice != null && KeyboardDevice.added) InputSystem.RemoveDevice(KeyboardDevice);
            if (MouseDevice != null && MouseDevice.added) InputSystem.RemoveDevice(MouseDevice);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            KeyboardDevice = null; MouseDevice = null;
        }
    }
}
