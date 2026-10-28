// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using System.Collections.Generic;
using PatientZero.Core;
using PatientZero.Flow;
using PatientZero.Player.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
namespace PatientZero.Player
{
    /// <summary>Adapts Unity XRI modality events and XR Hands tracking to Core; no replacement hand framework.</summary>
    public sealed class XriTrackingBridge : MonoBehaviour
    {
        public PlayerRig rig;public XRInputModalityManager modalityManager;public GameFlow flow;public InputAction pauseAction=new InputAction("Pause",InputActionType.Button,"<XRController>{LeftHand}/menuButton");
        public Transform leftHandPose,rightHandPose,leftControllerPose,rightControllerPose;
        private readonly MenuGesture gesture=new MenuGesture();
        private readonly List<XRHandSubsystem> systems=new List<XRHandSubsystem>();private InputModality mode;
        private void OnEnable(){SubsystemManager.GetSubsystems(systems);if(modalityManager){if(modalityManager.leftHand && modalityManager.leftHand.activeInHierarchy)Hands();else Controllers();modalityManager.trackedHandModeStarted.AddListener(Hands);modalityManager.motionControllerModeStarted.AddListener(Controllers);}if(pauseAction!=null){pauseAction.performed+=PauseInput;pauseAction.Enable();}GameEvents.HapticRequested.Subscribe(Haptic);}
        private void OnDisable(){if(modalityManager){modalityManager.trackedHandModeStarted.RemoveListener(Hands);modalityManager.motionControllerModeStarted.RemoveListener(Controllers);}if(pauseAction!=null){pauseAction.performed-=PauseInput;pauseAction.Disable();}GameEvents.HapticRequested.Unsubscribe(Haptic);}
        private void OnDestroy(){pauseAction?.Dispose();}
        private void Hands(){mode=InputModality.Hands;if(rig){rig.leftPose=leftHandPose;rig.rightPose=rightHandPose;}}
        private void Controllers(){mode=InputModality.Controllers;if(rig){rig.leftPose=leftControllerPose;rig.rightPose=rightControllerPose;}}
        public void HandMenuGesture(){if(flow)flow.Pause();}
        private void PauseInput(InputAction.CallbackContext context){if(flow)flow.Pause();}
        private void Update(){if(!rig || !XRSettings.isDeviceActive)return;bool l=false,r=false;if(mode==InputModality.Hands){for(int i=0;i<systems.Count;i++){if(!systems[i].running)continue;l|=systems[i].leftHand.isTracked;r|=systems[i].rightHand.isTracked;var hand=systems[i].leftHand;bool palmValid=hand.GetJoint(XRHandJointID.Palm).TryGetPose(out var palm);bool thumbValid=hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumb);bool indexValid=hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var index);if(gesture.Sample(palmValid && Vector3.Dot(palm.rotation*Vector3.up,Vector3.up)>.7f,thumbValid && indexValid && Vector3.Distance(thumb.position,index.position)<.025f,hand.isTracked))HandMenuGesture();}}else if(mode==InputModality.Controllers){InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out l);InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out r);}rig.SetTracking(mode,l,r);}
        private void Haptic(HapticRequested h){if(!rig || rig.Modality!=InputModality.Controllers)return;float strength=Services<ISettingsProvider>.TryGet(out var s)?s.Current.HapticsStrength01:1;var d=InputDevices.GetDeviceAtXRNode(h.Hand==HandSide.Left?XRNode.LeftHand:XRNode.RightHand);d.SendHapticImpulse(0,h.Amplitude01*strength,h.DurationSeconds);}
    }
}
