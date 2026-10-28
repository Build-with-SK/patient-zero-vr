// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using PatientZero.Flow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace PatientZero.Player
{
    /// <summary>Explicit non-headset simulator. Disabled when an XR device is active; user input alone moves the camera.</summary>
    public sealed class DesktopLocomotion : MonoBehaviour
    {
        public PlayerRig rig;public GameFlow flow;public CharacterController capsule;public Camera cameraView;public bool desktopSimulation=true;private float yaw,pitch;private bool live;private float footstep;
        private void OnEnable(){GameEvents.StateChanged.Subscribe(State);}
        private void OnDisable(){GameEvents.StateChanged.Unsubscribe(State);}
        private void State(GameStateChanged state){live=GameStateRules.IsSimulationLive(state.To);}
        private void Update()
        {
            if(!desktopSimulation || XRSettings.isDeviceActive || !rig || !cameraView)return;
            var keys=Keyboard.current;var mouse=Mouse.current;if(keys==null || mouse==null)return;
            rig.SetTracking(InputModality.Controllers,true,true);
            if(keys.escapeKey.wasPressedThisFrame && flow){if(flow.Core.State==GameState.Paused)flow.Resume();else flow.Pause();}
            if(keys.enterKey.wasPressedThisFrame && flow && flow.Core.State==GameState.Calibration)flow.ConfirmCalibration();
            if(!live || !flow || flow.Core.IsTransitioning)return;
            if(mouse.rightButton.isPressed){var delta=mouse.delta.ReadValue();yaw+=delta.x*.12f;pitch=Mathf.Clamp(pitch-delta.y*.12f,-75,75);rig.origin.transform.rotation=Quaternion.Euler(0,yaw,0);cameraView.transform.localRotation=Quaternion.Euler(pitch,0,0);}
            if(keys.tKey.wasPressedThisFrame)TeleportNext();
            if(!Services<ISettingsProvider>.TryGet(out var settings) || settings.Current.Locomotion!=LocomotionMode.TeleportAndSmooth || !capsule)return;
            float x=(keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0),z=(keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0);var move=rig.origin.transform.TransformDirection(new Vector3(x,0,z).normalized);capsule.Move(move*1.5f*Time.deltaTime+Vector3.down*Time.deltaTime);
            if(move.sqrMagnitude>0){footstep+=Time.deltaTime;if(footstep>=.6f){footstep=0;Cues.Play(CueId.PlayerFootstep,rig.FloorPosition,NoiseOrigin.Player);}}
        }
        private void TeleportNext(){int nearest=-1;float best=float.MaxValue;for(int i=0;i<WardLayout.CriticalPath.Count;i++){var a=WardLayout.CriticalPath[i];if(a.Act!=flow.Core.CurrentAct)continue;float d=WardLayout.HorizontalDistance(rig.FloorPosition,a.Position);if(d<best){best=d;nearest=i;}}int next=nearest+1;if(nearest<0 || next>=WardLayout.CriticalPath.Count)return;var anchor=WardLayout.CriticalPath[next];if(anchor.Act!=flow.Core.CurrentAct || WardLayout.HorizontalDistance(rig.FloorPosition,anchor.Position)>WardLayout.TeleportMaxDistance)return;rig.origin.MoveCameraToWorldLocation(anchor.Position+Vector3.up*(rig.HeadPosition.y-rig.FloorPosition.y));Cues.Play(CueId.TeleportLand,anchor.Position,NoiseOrigin.Player);}
    }
}
