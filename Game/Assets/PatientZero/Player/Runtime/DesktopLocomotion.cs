// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Flow;
using UnityEngine;
using UnityEngine.XR;
namespace PatientZero.Player
{
    /// <summary>Explicit non-headset simulator. Disabled when an XR device is active; user input alone moves the camera.</summary>
    /// <remarks>Integration E-15: reads FlatInput, so Windows (keyboard and mouse) and phone/iPad (touch) share this path;
    /// adds the touch controls in touch builds. The torch is DesktopInteraction's (E-17). Runs before DesktopInteraction.</remarks>
    [DefaultExecutionOrder(-50)]
    public sealed class DesktopLocomotion : MonoBehaviour
    {
        public PlayerRig rig;public GameFlow flow;public CharacterController capsule;public Camera cameraView;public bool desktopSimulation=true;
        /// <summary>Integration E-21: while set, look, move and teleport are ignored (pause still works); a broadcast holds the view.</summary>
        public bool viewLocked;
        private float pitch;private bool live;private float footstep;private Vector3 viewBase;private float bob;
        private void OnEnable(){GameEvents.StateChanged.Subscribe(State);}
        private void OnDisable(){GameEvents.StateChanged.Unsubscribe(State);}
        private void State(GameStateChanged state){live=GameStateRules.IsSimulationLive(state.To);}
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
        private void Start()
        {
            if(!desktopSimulation || XRSettings.isDeviceActive || !cameraView)return;
            // Owner report 2026-10-30, "I can't look around": with no headset nothing should pose the camera but this
            // script, so the XR pose driver on the camera is switched off on flat screens.
            foreach(var behaviour in cameraView.GetComponents<Behaviour>())if(behaviour && behaviour.GetType().Name=="TrackedPoseDriver")behaviour.enabled=false;
            viewBase=cameraView.transform.localPosition;
#if PZ_TOUCH
            if(!GetComponent<TouchControls>())gameObject.AddComponent<TouchControls>();
            Application.targetFrameRate=60;Screen.sleepTimeout=SleepTimeout.NeverSleep;
#endif
        }
#endif
        private void Update()
        {
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
            if(!desktopSimulation || XRSettings.isDeviceActive || !rig || !cameraView)return;
#if PZ_DESKTOP_MOUSE
            // Mouse look without a button while the run is live: the cursor is captured then, and freed for every menu.
            bool capture=live && Application.isFocused && flow && flow.Core.State==GameState.Playing;
            var wanted=capture?CursorLockMode.Locked:CursorLockMode.None;
            if(Cursor.lockState!=wanted){Cursor.lockState=wanted;Cursor.visible=!capture;}
#endif
            FlatInput.Poll();
            rig.SetTracking(InputModality.Controllers,true,true);
            if(FlatInput.Pause && flow){if(flow.Core.State==GameState.Paused)flow.Resume();else flow.Pause();}
            if(FlatInput.Confirm && flow && flow.Core.State==GameState.Calibration)flow.ConfirmCalibration();
            if(!live || !flow || flow.Core.IsTransitioning || viewLocked)return;
            // E-11: look starts from the current facing (no snap); yaw is read back from the rig every frame.
            if(FlatInput.Look!=Vector2.zero){float yaw=rig.origin.transform.eulerAngles.y+FlatInput.Look.x;pitch=Mathf.Clamp(pitch+FlatInput.Look.y,-75,75);rig.origin.transform.rotation=Quaternion.Euler(0,yaw,0);cameraView.transform.localRotation=Quaternion.Euler(pitch,0,0);}
            if(FlatInput.Teleport)TeleportNext();
            // E-11: flat-screen movement does not depend on the headset comfort setting (TeleportOnly by default).
            if(!capsule)return;
            var move=rig.origin.transform.TransformDirection(new Vector3(FlatInput.Move.x,0,FlatInput.Move.y));capsule.Move(move*1.5f*Time.deltaTime+Vector3.down*Time.deltaTime);
            if(move.sqrMagnitude>.04f){footstep+=Time.deltaTime;if(footstep>=.6f){footstep=0;Cues.Play(CueId.PlayerFootstep,rig.FloorPosition,NoiseOrigin.Player);}}
            // A small walking bob and an idle breath, flat screens only (no comfort rule applies off the headset).
            bob+=Time.deltaTime*(move.sqrMagnitude>.04f?10.5f:1.6f);
            float amplitude=move.sqrMagnitude>.04f?.022f:.004f;
            cameraView.transform.localPosition=viewBase+new Vector3(0,Mathf.Sin(bob)*amplitude,0);
#endif
        }
        private void TeleportNext(){int nearest=-1;float best=float.MaxValue;for(int i=0;i<WardLayout.CriticalPath.Count;i++){var a=WardLayout.CriticalPath[i];if(a.Act!=flow.Core.CurrentAct)continue;float d=WardLayout.HorizontalDistance(rig.FloorPosition,a.Position);if(d<best){best=d;nearest=i;}}int next=nearest+1;if(nearest<0 || next>=WardLayout.CriticalPath.Count)return;var anchor=WardLayout.CriticalPath[next];if(anchor.Act!=flow.Core.CurrentAct || WardLayout.HorizontalDistance(rig.FloorPosition,anchor.Position)>WardLayout.TeleportMaxDistance)return;rig.origin.MoveCameraToWorldLocation(anchor.Position+Vector3.up*(rig.HeadPosition.y-rig.FloorPosition.y));Cues.Play(CueId.TeleportLand,anchor.Position,NoiseOrigin.Player);}
    }
}
