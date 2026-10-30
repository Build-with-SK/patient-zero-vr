// UNVERIFIED IN EDITOR
using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Environment.Logic;
using PatientZero.Environment.Mobile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PatientZero.Environment.Tests
{
    public sealed class MobileMappingSceneTests
    {
        [UnityTest] public IEnumerator StandardVirtualInputMapsTouchAndDisposesWithoutRemovingHardware()
        {
            var priorKeyboard = Keyboard.current; var priorMouse = Mouse.current;
            var devices = new MobileVirtualDevices(); var keyboard = devices.KeyboardDevice; var mouse = devices.MouseDevice;
            try
            {
                devices.Write(new MobileInputFrame(Vector2.one, new Vector2(20, 10), true, false, MobileActions.Throw), GameState.Playing, new Vector2(800, 450));
                Assert.IsTrue(keyboard.wKey.isPressed); Assert.IsTrue(keyboard.dKey.isPressed);
                Assert.IsTrue(keyboard.qKey.isPressed); Assert.IsTrue(keyboard.qKey.wasPressedThisFrame); Assert.IsTrue(mouse.leftButton.isPressed); Assert.IsFalse(mouse.rightButton.isPressed);
                Assert.AreEqual(new Vector2(20, 10), mouse.delta.ReadValue());
                devices.Write(default, GameState.Playing, Vector2.zero);
                Assert.IsFalse(keyboard.qKey.isPressed); Assert.IsFalse(mouse.leftButton.isPressed);
                devices.Write(new MobileInputFrame(Vector2.zero, Vector2.zero, false, false, MobileActions.Start), GameState.Paused, Vector2.zero);
                Assert.IsTrue(keyboard.escapeKey.isPressed); Assert.IsFalse(keyboard.enterKey.isPressed);
                devices.Write(new MobileInputFrame(Vector2.zero, Vector2.zero, false, false, MobileActions.Start), GameState.Calibration, Vector2.zero);
                Assert.IsFalse(keyboard.escapeKey.isPressed); Assert.IsTrue(keyboard.enterKey.isPressed);
                devices.Neutral(); Assert.IsFalse(keyboard.enterKey.isPressed);
            }
            finally { devices.Dispose(); }
            Assert.IsFalse(keyboard.added); Assert.IsFalse(mouse.added);
            if (priorKeyboard != null && priorKeyboard.added) Assert.AreSame(priorKeyboard, Keyboard.current);
            if (priorMouse != null && priorMouse.added) Assert.AreSame(priorMouse, Mouse.current);
            yield return null;
        }
    }
}
