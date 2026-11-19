// Integration exception E-31 (Claude, owner "polish for vr", 2026-11-19): written in seat A's area because Interaction is the
// one writer of HandheldLightChanged (contract v1.2, D-1b). Seat A may rewrite it freely.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace PatientZero.Interaction
{
    /// <summary>
    /// E-31: the torch in a headset, which had none (only DesktopInteraction raised HandheldLightChanged, and only on flat
    /// screens), so the dark acts were played blind. The beam comes from the left hand, controller or tracked palm
    /// (<see cref="HeadsetTorchRule"/>); Atmosphere's HandheldLight owns the light. As on flat screens (E-26) it is on from the
    /// start and turning it off is the player's wish: the left controller's X button switches it. With hand tracking it stays on.
    /// </summary>
    public sealed class HeadsetTorch : MonoBehaviour
    {
        public InputAction toggle = new InputAction("Torch", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
        private bool on = true, lastOn, live;

        public bool On => on;

        private void OnEnable() { GameEvents.StateChanged.Subscribe(State); toggle.performed += Toggle; toggle.Enable(); }

        private void OnDisable()
        {
            GameEvents.StateChanged.Unsubscribe(State); toggle.performed -= Toggle; toggle.Disable();
            if (lastOn) GameEvents.HandheldLightChanged.Raise(new HandheldLightChanged(false, Vector3.zero, Vector3.forward));
            lastOn = false;
        }

        private void OnDestroy() => toggle.Dispose();

        private void State(GameStateChanged state) => live = GameStateRules.IsSimulationLive(state.To);

        private void Toggle(InputAction.CallbackContext context)
        {
            if (!live || !XRSettings.isDeviceActive) return;
            on = !on;
        }

        private void Update()
        {
            if (!live || !XRSettings.isDeviceActive) return;
            if (!Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) return;
            bool shining = HeadsetTorchRule.ShouldShine(on, player.Modality);
            if (!shining && !lastOn) return;
            var rig = player as PlayerRig;
            Transform hand = rig ? rig.leftPose : null;
            bool tracked = hand && player.TryGetHandPosition(HandSide.Left, out _);
            if (!HeadsetTorchRule.Beam(tracked, hand ? hand.position : Vector3.zero, hand ? hand.forward : Vector3.zero,
                                       player.HeadPosition, player.HeadForward, out Vector3 position, out Vector3 forward)) return;
            lastOn = shining;
            GameEvents.HandheldLightChanged.Raise(new HandheldLightChanged(shining, position, forward));
        }
    }
}
