// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace PatientZero.Interaction
{
    /// <summary>XRI physics prop safety and impact audio. Desktop uses the same Rigidbody and release policy.</summary>
    [RequireComponent(typeof(Rigidbody),typeof(XRGrabInteractable))]
    public sealed class PhysicsProp : MonoBehaviour
    {
        public bool heavy,requiredItem;public HandSide selectedHand;public float recoveryRange=1.2f;
        private PlayerRig rig;private Rigidbody body;private XRGrabInteractable grab;private Vector3 home;private Quaternion homeRotation;private float recovery,nextImpact,suppressUntil;private bool selected,cancelled,originalKinematic;private CueHandle drag;
        public Rigidbody Body=>body;public bool Held=>selected;
        private void Awake(){body=GetComponent<Rigidbody>();grab=GetComponent<XRGrabInteractable>();home=transform.position;homeRotation=transform.rotation;originalKinematic=body.isKinematic;body.interpolation=RigidbodyInterpolation.Interpolate;}
        private void OnEnable(){if(Services<IPlayerQuery>.TryGet(out var player)){rig=player as PlayerRig;if(rig){rig.cancelLeft.AddListener(CancelSelection);rig.cancelRight.AddListener(CancelSelection);}}grab.selectEntered.AddListener(Select);grab.selectExited.AddListener(Release);GameEvents.TrackingChanged.Subscribe(Tracking);GameEvents.StateChanged.Subscribe(State);}
        private void OnDisable(){if(rig){rig.cancelLeft.RemoveListener(CancelSelection);rig.cancelRight.RemoveListener(CancelSelection);}grab.selectEntered.RemoveListener(Select);grab.selectExited.RemoveListener(Release);GameEvents.TrackingChanged.Unsubscribe(Tracking);GameEvents.StateChanged.Unsubscribe(State);Cues.Stop(drag);}
        private void Select(SelectEnterEventArgs args){selected=true;cancelled=false;if(requiredItem)drag=Cues.PlayAttached(CueId.CableDrag,transform,NoiseOrigin.Player,.5f);}
        private void Release(SelectExitEventArgs args){selected=false;Cues.Stop(drag);body.linearVelocity=HeldObjectSafety.ReleaseVelocity(body.linearVelocity,true,cancelled);if(cancelled)body.angularVelocity=Vector3.zero;}
        public void DesktopGrab(){if(grab.isSelected)return;selected=true;cancelled=false;body.useGravity=false;}
        public void DesktopRelease(Vector3 velocity){selected=false;body.useGravity=true;body.linearVelocity=HeldObjectSafety.ReleaseVelocity(velocity,true,cancelled);}
        public void CancelSelection(){cancelled=true;selected=false;suppressUntil=Time.time+.3f;if(grab.isSelected && grab.interactionManager)grab.interactionManager.CancelInteractableSelection(grab);if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.useGravity=true;Cues.Stop(drag);}
        private InputModality lastMode;
        private void Tracking(TrackingChanged t){if(lastMode!=t.Modality)CancelSelection();lastMode=t.Modality;}
        private void State(GameStateChanged state){if(!GameStateRules.IsSimulationLive(state.To))CancelSelection();}
        private void FixedUpdate(){if(Time.time<suppressUntil && !body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}}
        private void Update(){if(!requiredItem || selected || body.isKinematic)return;bool unreachable=transform.position.y<.3f;if(Services<IPlayerQuery>.TryGet(out var p))unreachable|=WardLayout.HorizontalDistance(p.FloorPosition,transform.position)>recoveryRange;if(!unreachable){recovery=0;return;}recovery+=Time.deltaTime;if(recovery>=5)ReturnHome();}
        public void ReturnHome(){CancelSelection();body.isKinematic=originalKinematic;transform.SetPositionAndRotation(home,homeRotation);if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}recovery=0;}
        private void OnCollisionEnter(Collision collision){float speed=collision.relativeVelocity.magnitude;if(speed<.2f || Time.time<nextImpact)return;nextImpact=Time.time+.2f;Cues.Play(heavy?CueId.ImpactHeavy:CueId.ImpactLight,collision.GetContact(0).point,NoiseOrigin.Player,Mathf.Clamp01(speed/3));}
    }
}
