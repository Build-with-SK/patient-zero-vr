// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
namespace PatientZero.Player
{
    /// <summary>Applies comfort choices to the standard XRI providers; input remains XRI-owned.</summary>
    public sealed class MotionSettings : MonoBehaviour
    {
        public SnapTurnProvider snap;public ContinuousTurnProvider smoothTurn;public ContinuousMoveProvider smoothMove;public Behaviour vignette;
        private void OnEnable(){GameEvents.SettingsChanged.Subscribe(Settings);if(Services<ISettingsProvider>.TryGet(out var s))Apply(s.Current);}
        private void OnDisable(){GameEvents.SettingsChanged.Unsubscribe(Settings);}
        private void Settings(SettingsChanged changed){Apply(changed.Settings);}
        private void Apply(ExperienceSettings s){if(snap){snap.enabled=s.Turn==TurnMode.Snap;snap.turnAmount=s.SnapTurnDegrees;}if(smoothTurn)smoothTurn.enabled=s.Turn==TurnMode.Smooth;if(smoothMove)smoothMove.enabled=s.Locomotion==LocomotionMode.TeleportAndSmooth;if(vignette)vignette.enabled=s.ComfortVignette;}
    }
}
