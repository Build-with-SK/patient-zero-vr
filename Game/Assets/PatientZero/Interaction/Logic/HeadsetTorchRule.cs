// Integration exception E-31 (Claude, owner "polish for vr", 2026-11-19): written in seat A's area because Interaction is the
// one writer of HandheldLightChanged (contract v1.2, D-1b). Seat A may rewrite it freely.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Interaction.Logic
{
    /// <summary>
    /// E-31: where a headset's torch shines from. Flat screens hold the torch in front of the eyes (DesktopInteraction); a
    /// headset holds it in the left hand, controller or tracked palm, its beam along the hand's forward from just ahead of it.
    /// With the left hand untracked it rides on the head, so losing the hand never leaves the player in the dark.
    /// </summary>
    public static class HeadsetTorchRule
    {
        /// <summary>How far ahead of the hand (or the eyes) the beam starts, so the hand's own model does not catch it.</summary>
        public const float LensAhead = .08f;

        /// <summary>Hands have no X button: changing from controllers must never leave them with an un-switchable dark torch.</summary>
        public static bool ShouldShine(bool switchedOn, InputModality modality) => switchedOn || modality == InputModality.Hands;

        public static bool Beam(bool handTracked, Vector3 handPosition, Vector3 handForward, Vector3 headPosition, Vector3 headForward,
                                out Vector3 position, out Vector3 forward)
        {
            bool hand = handTracked && Usable(handPosition, handForward);
            if (!hand && !Usable(headPosition, headForward)) { position = forward = Vector3.zero; return false; }
            forward = (hand ? handForward : headForward).normalized;
            position = (hand ? handPosition : headPosition) + forward * LensAhead;
            return true;
        }

        private static bool Usable(Vector3 position, Vector3 forward) =>
            Finite(position) && Finite(forward) && forward.sqrMagnitude > 1e-6f && !float.IsInfinity(forward.sqrMagnitude);

        private static bool Finite(Vector3 v) => !float.IsNaN(v.x + v.y + v.z) && !float.IsInfinity(v.x + v.y + v.z);
    }
}
