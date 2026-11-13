// Claude-owned integration (owner, 2026-11-12: "Build the gun next"; plan step 4; E-25).
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PatientZero.Integration
{
    /// <summary>
    /// The hip holster of the owner's plan ("VR: draw it from a hip holster"): an XRI socket that takes only the revolver.
    /// The revolver moves it with the player's hips (<see cref="Revolver"/>) and switches it on only in a headset once the gun
    /// is taken; let go of the gun anywhere and it goes back here (<see cref="Stow"/>), so it is never lost on the floor.
    /// </summary>
    public sealed class HolsterSocket : XRSocketInteractor
    {
        /// <summary>The one thing this holster takes.</summary>
        public XRGrabInteractable accepts;

        public override bool CanHover(IXRHoverInteractable interactable) =>
            accepts && ReferenceEquals(interactable, accepts) && base.CanHover(interactable);

        public override bool CanSelect(IXRSelectInteractable interactable) =>
            accepts && ReferenceEquals(interactable, accepts) && base.CanSelect(interactable);

        /// <summary>Puts the gun in the holster unless a hand or the holster already holds it.</summary>
        public void Stow()
        {
            if (!accepts || accepts.isSelected || hasSelection || !interactionManager || !isActiveAndEnabled) return;
            interactionManager.SelectEnter((IXRSelectInteractor)this, (IXRSelectInteractable)accepts);
        }
    }
}
