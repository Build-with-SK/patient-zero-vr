using System;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Environment.Logic;
using UnityEngine;

namespace PatientZero.Environment.Tests
{
    public sealed class MobileTouchTests
    {
        private static MobileTouchLayout Layout(float w = 1600, float h = 900) => new MobileTouchLayout(w, h, new TouchBounds(40, 20, w - 80, h - 40));
        private static MobileTouchInput Playing()
        { var input = new MobileTouchInput(Layout()) { FlashlightAvailable = true }; input.SetState(GameState.Playing); input.SetLighting(LightingState.Awakening); return input; }
        private static void Tap(MobileTouchInput input, MobileControl button, int id = 1, double at = 1)
        { var p = input.Layout.At(button).Centre; Assert.IsTrue(input.Down(id, p, at)); input.Up(id, p, at + .1); }
        [Test] public void SafeAreaContainsEveryButtonOnPhoneAndIpadLandscape()
        {
            foreach (var layout in new[] { Layout(), Layout(2360, 1640) })
                for (int i = (int)MobileControl.Interact; i <= (int)MobileControl.Aim; i++)
                {
                    var b = layout.At((MobileControl)i);
                    Assert.GreaterOrEqual(b.X, layout.Safe.X); Assert.GreaterOrEqual(b.Y, layout.Safe.Y);
                    Assert.LessOrEqual(b.X + b.Width, layout.Safe.X + layout.Safe.Width);
                    Assert.LessOrEqual(b.Y + b.Height, layout.Safe.Y + layout.Safe.Height);
                }
        }
        [Test] public void MobilePlatformRequiresAllOptInsAndRejectsXrOrSimulatorConfiguration()
        {
            Assert.IsTrue(MobilePlatformPolicy.CanRun(true, true, false, true, true, false, false));
            Assert.IsTrue(MobilePlatformPolicy.CanRun(true, false, true, true, true, false, false));
            Assert.IsFalse(MobilePlatformPolicy.CanRun(false, true, false, true, true, false, false));
            Assert.IsFalse(MobilePlatformPolicy.CanRun(true, false, false, true, true, false, false));
            Assert.IsFalse(MobilePlatformPolicy.CanRun(true, true, false, false, true, false, false));
            Assert.IsFalse(MobilePlatformPolicy.CanRun(true, true, false, true, false, false, false));
            Assert.IsFalse(MobilePlatformPolicy.CanRun(true, true, false, true, true, true, false));
            Assert.IsFalse(MobilePlatformPolicy.CanRun(true, true, false, true, true, false, true));
        }
        [Test] public void SimulatorDevicesAreDetectedWithoutImportingAnyXriTypes()
        {
            Assert.IsTrue(MobilePlatformPolicy.IsSimulatorLayout("XRSimulatedHMD"));
            Assert.IsTrue(MobilePlatformPolicy.IsSimulatorLayout("XRSimulatedController"));
            Assert.IsFalse(MobilePlatformPolicy.IsSimulatorLayout("Mouse"));
            Assert.IsFalse(MobilePlatformPolicy.IsSimulatorLayout("Touchscreen"));
            Assert.IsFalse(MobilePlatformPolicy.IsSimulatorLayout(null));
        }
        [Test] public void JoystickAndLookUseIndependentPointersWithoutFirstTouchCameraJump()
        {
            var input = Playing(); var move = input.Layout.At(MobileControl.Move).Centre; var look = input.Layout.At(MobileControl.Look).Centre;
            Assert.IsTrue(input.Down(1, move, 1)); Assert.IsTrue(input.Down(2, look, 1));
            Assert.AreEqual(Vector2.zero, input.Read().Delta);
            input.Move(1, move + Vector2.one * 1000, 1.1); input.Move(2, look + new Vector2(50, 20), 1.1);
            var frame = input.Read(); Assert.LessOrEqual(frame.Move.magnitude, 1.0001f); Assert.Greater(frame.Move.y, 0);
            Assert.Greater(frame.Delta.x, 0); Assert.IsTrue(frame.RightHeld); Assert.IsFalse(frame.LeftHeld);
            Assert.AreEqual(Vector2.zero, input.Read().Delta);
        }
        [Test] public void ThrowKeepsLeftHeldForQFrameThenReleasesOnNextFrame()
        {
            var input = Playing(); Tap(input, MobileControl.Interact); Assert.IsTrue(input.Read().LeftHeld);
            Tap(input, MobileControl.Throw, 2, 2); var throwing = input.Read();
            Assert.IsTrue((throwing.Actions & MobileActions.Throw) != 0); Assert.IsTrue(throwing.LeftHeld);
            var released = input.Read(); Assert.IsFalse(released.LeftHeld); Assert.AreEqual(MobileActions.None, released.Actions);
        }
        [Test] public void GrippingRoutesDragToPuzzleWithoutRightMouseCameraRotation()
        {
            var input = Playing(); Tap(input, MobileControl.Interact); input.Read();
            var look = input.Layout.At(MobileControl.Look).Centre; input.Down(2, look, 2); input.Move(2, look + Vector2.one * 50, 2.1);
            var frame = input.Read(); Assert.IsTrue(frame.LeftHeld); Assert.IsFalse(frame.RightHeld); Assert.Greater(frame.Delta.x, 0);
            input.Up(2, look + Vector2.one * 50, 2.2); Assert.IsTrue(input.GripHeld);
        }
        [Test] public void HeldBottleOrCableCanBeAimedWithoutReleasingIt()
        {
            var input = Playing(); Tap(input, MobileControl.Interact); input.Read();
            Tap(input, MobileControl.Aim, 2, 2); Assert.IsTrue(input.AimMode);
            var look = input.Layout.At(MobileControl.Look).Centre;
            input.Down(3, look, 3); input.Move(3, look + Vector2.one * 50, 3.1);
            var aiming = input.Read(); Assert.IsTrue(aiming.LeftHeld); Assert.IsTrue(aiming.RightHeld);
            input.Up(3, look + Vector2.one * 50, 3.2); Tap(input, MobileControl.Aim, 4, 4); Assert.IsFalse(input.AimMode);
            Tap(input, MobileControl.Throw, 5, 5); Assert.IsFalse(input.AimMode); Assert.IsFalse(input.GripHeld);
            var throwing = input.Read(); Assert.IsTrue(throwing.LeftHeld); Assert.IsFalse(throwing.RightHeld);
        }
        [Test] public void TapInteractIsDistinctFromLongHoldOrDragAndCancelledButtonsDoNotFire()
        {
            var input = Playing(); var look = input.Layout.At(MobileControl.Look).Centre;
            input.Down(1, look, 1); input.Up(1, look, 1.1); Assert.IsTrue(input.GripHeld);
            input.Down(1, look, 2); input.Up(1, look, 3); Assert.IsTrue(input.GripHeld);
            var button = input.Layout.At(MobileControl.Interact).Centre;
            input.Down(2, button, 4); input.Up(2, button, 4.1, true); Assert.IsTrue(input.GripHeld);
            input.Down(2, button, 5); input.Up(2, Vector2.zero, 5.1); Assert.IsTrue(input.GripHeld);
        }
        [Test] public void ButtonsCannotTurnIntoLookGesturesAndDuplicateIdsAreIgnored()
        {
            var input = Playing(); var button = input.Layout.At(MobileControl.Pause).Centre;
            Assert.IsTrue(input.Down(1, button, 1)); Assert.IsFalse(input.Down(1, input.Layout.At(MobileControl.Move).Centre, 1));
            input.Move(1, input.Layout.At(MobileControl.Look).Centre, 1.1);
            var frame = input.Read(); Assert.AreEqual(Vector2.zero, frame.Delta); Assert.IsFalse(frame.RightHeld);
            input.Up(1, input.Layout.At(MobileControl.Look).Centre, 1.2); Assert.AreEqual(MobileActions.None, input.Read().Actions);
        }
        [Test] public void PauseFocusResetAndResizeClearGripMovementAndPendingActions()
        {
            var input = Playing(); Tap(input, MobileControl.Interact); Tap(input, MobileControl.Step, 2, 2);
            input.SetState(GameState.Paused); var paused = input.Read(); Assert.IsFalse(paused.LeftHeld); Assert.AreEqual(MobileActions.None, paused.Actions);
            Assert.IsFalse(input.Available(MobileControl.Move)); Assert.IsFalse(input.Available(MobileControl.Interact)); Assert.IsTrue(input.Available(MobileControl.Start));
            input.SetState(GameState.Playing); Tap(input, MobileControl.Interact); input.Reset(); Assert.IsFalse(input.Read().LeftHeld);
            Tap(input, MobileControl.Interact); input.SetLayout(Layout(2360, 1640)); Assert.IsFalse(input.Read().LeftHeld);
        }
        [Test] public void FadeReleasesGameplayInputWhilePausedResumeRemainsUsable()
        {
            var input = Playing(); Tap(input, MobileControl.Interact); input.Read();
            input.SetTransitioning(true); Assert.IsFalse(input.Read().LeftHeld);
            Assert.IsFalse(input.Available(MobileControl.Move)); Assert.IsFalse(input.Available(MobileControl.Interact));
            Assert.IsFalse(input.Available(MobileControl.Look)); Assert.IsTrue(input.Available(MobileControl.Pause));
            input.SetState(GameState.Paused); Assert.IsTrue(input.Available(MobileControl.Start));
            input.SetState(GameState.Playing); input.SetTransitioning(false); Assert.IsTrue(input.Available(MobileControl.Move));
        }
        [Test] public void PhoneDefaultTeleportModeHasPredictableForwardJoystickStep()
        {
            var input = Playing(); input.TeleportOnly = true; var p = input.Layout.At(MobileControl.Move).Centre;
            input.Down(1, p, 1); input.Move(1, p + Vector2.one * input.Layout.JoystickRadius, 1.1);
            Assert.AreEqual(Vector2.zero, input.Read().Move);
            input.Up(1, p, 1.2); Assert.IsTrue((input.Read().Actions & MobileActions.Step) != 0);
            input.Down(2, p, 2); input.Move(2, p - Vector2.one * input.Layout.JoystickRadius, 2.1);
            input.Up(2, p, 2.2); Assert.AreEqual(MobileActions.None, input.Read().Actions);
        }
        [Test] public void FlashlightLocksAtSurgeAndBlackoutAndResetsForNewAct()
        {
            var input = Playing(); Tap(input, MobileControl.Flashlight); Assert.IsTrue(input.LightOn); input.Read();
            input.SetLighting(LightingState.Surge); Assert.IsFalse(input.LightOn); Assert.IsFalse(input.LightAvailable);
            input.SetLighting(LightingState.Blackout); Assert.IsFalse(input.Available(MobileControl.Flashlight));
            input.SetLighting(LightingState.WardDim); Tap(input, MobileControl.Flashlight, 2, 2); input.RestartAct(); Assert.IsFalse(input.LightOn);
        }
        [Test] public void RepeatedHeadlampTapsCannotStrobeAndCancelledThrowKeepsHold()
        {
            var input = Playing(); Tap(input, MobileControl.Flashlight, 1, 1); input.Read(); Assert.IsTrue(input.LightOn);
            Tap(input, MobileControl.Flashlight, 2, 1.2); Assert.AreEqual(MobileActions.None, input.Read().Actions); Assert.IsTrue(input.LightOn);
            Tap(input, MobileControl.Flashlight, 3, 1.7); Assert.IsFalse(input.LightOn);
            Tap(input, MobileControl.Interact, 4, 2); input.Read();
            var button = input.Layout.At(MobileControl.Throw).Centre; input.Down(5, button, 3); input.Up(5, button, 3.1, true);
            var frame = input.Read(); Assert.IsTrue(frame.LeftHeld); Assert.AreEqual(MobileActions.None, frame.Actions);
        }
        [Test] public void EmptyOrInvalidSafeAreasAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new MobileTouchLayout(1600, 900, new TouchBounds(0, 0, 0, 0)));
            Assert.Throws<ArgumentException>(() => new MobileTouchLayout(float.NaN, 900, new TouchBounds(0, 0, 100, 100)));
            Assert.Throws<ArgumentException>(() => new MobileTouchLayout(1600, 900, new TouchBounds(5000, 0, 100, 100)));
        }
        [Test] public void RestartAndCalibrationRemainDistinctAndUnavailableActionsFailClosed()
        {
            var input = Playing(); input.SetState(GameState.Paused); Assert.IsFalse(input.Available(MobileControl.Restart));
            input.RestartShortcutAvailable = true; Tap(input, MobileControl.Restart); Assert.IsTrue((input.Read().Actions & MobileActions.Restart) != 0);
            input.SetState(GameState.Complete); Assert.IsTrue(input.Available(MobileControl.Restart)); Assert.IsFalse(input.Available(MobileControl.Interact));
            input.SetState(GameState.Calibration); Tap(input, MobileControl.Start, 2, 2); Assert.IsTrue((input.Read().Actions & MobileActions.Start) != 0);
        }
        [Test] public void PortraitInvalidAndExcessPointersCannotMoveOrRetainInput()
        {
            var input = Playing(); Assert.IsFalse(input.Down(1, new Vector2(float.NaN, 0), 1));
            input.SetLayout(Layout(900, 1600)); Assert.IsFalse(input.Available(MobileControl.Look));
            Assert.IsFalse(input.Down(1, input.Layout.At(MobileControl.Look).Centre, 1));
            input.SetLayout(Layout()); var look = input.Layout.At(MobileControl.Look).Centre;
            Assert.IsTrue(input.Down(1, look, 1)); Assert.IsFalse(input.Down(2, look, 1));
            input.Up(1, look, 2, true); Assert.IsTrue(input.Down(2, look, 2));
            input.Move(2, new Vector2(100000, -100000), 2.1); var frame = input.Read();
            Assert.LessOrEqual(Math.Abs(frame.Delta.x), 100); Assert.LessOrEqual(Math.Abs(frame.Delta.y), 100);
        }
        [Test] public void WarmTouchTranslationAllocatesNoBytes()
        {
            var input = Playing(); var p = input.Layout.At(MobileControl.Move).Centre;
            for (int i = 0; i < 100; i++) { input.Down(1, p, i); input.Move(1, p + Vector2.one * 50, i + .1); input.Read(); input.Up(1, p, i + .2); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 100; i < 10100; i++) { input.Down(1, p, i); input.Move(1, p + Vector2.one * 50, i + .1); input.Read(); input.Up(1, p, i + .2); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before; Assert.AreEqual(0L, allocated);
        }
    }
}
