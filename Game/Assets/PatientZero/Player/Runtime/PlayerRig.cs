// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Player.Logic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
namespace PatientZero.Player
{
    /// <summary>Core player query and calibration; XRI remains responsible for hand/controller selection.</summary>
    public sealed class PlayerRig : MonoBehaviour,IPlayerQuery
    {
        public XROrigin origin;public Transform head,leftPose,rightPose,heightRoot;
        public UnityEvent cancelLeft=new UnityEvent(),cancelRight=new UnityEvent();
        private readonly SpeedEstimator headSpeed=new SpeedEstimator(),leftSpeed=new SpeedEstimator(),rightSpeed=new SpeedEstimator();
        private readonly TrackingRecovery recovery=new TrackingRecovery();
        private bool left,right;private bool focused=true;private Vector3 baseHeight;private bool placed;
        public Vector3 HeadPosition => head?head.position:transform.position;
        public Vector3 HeadForward => head?head.forward:transform.forward;
        public Vector3 FloorPosition => new Vector3(HeadPosition.x,origin?origin.transform.position.y:0,HeadPosition.z);
        public InputModality Modality {get;private set;}
        public float HeadSpeed => headSpeed.Speed;
        public float FastestHandSpeed => Mathf.Max(leftSpeed.Speed,rightSpeed.Speed);
        public bool HandsNotVisible => recovery.ShowHandsLost;
        public bool InteractionReady => recovery.Ready;
        public bool TryGetHandPosition(HandSide side,out Vector3 position){var pose=side==HandSide.Left?leftPose:rightPose;bool tracked=side==HandSide.Left?left:right;position=pose?pose.position:Vector3.zero;return tracked && pose;}
        private void Awake(){if(heightRoot)baseHeight=heightRoot.localPosition;}
        private void OnEnable(){if(!head || !origin){Debug.LogError("Rig requires XR Origin and head",this);enabled=false;return;}Services<IPlayerQuery>.Register(this);GameEvents.SettingsChanged.Subscribe(OnSettings);GameEvents.ActStarted.Subscribe(OnAct);GameEvents.StateChanged.Subscribe(OnState);if(Services<ISettingsProvider>.TryGet(out var s))ApplySettings(s.Current);}
        private void OnDisable(){cancelLeft.Invoke();cancelRight.Invoke();Services<IPlayerQuery>.Unregister(this);GameEvents.SettingsChanged.Unsubscribe(OnSettings);GameEvents.ActStarted.Unsubscribe(OnAct);GameEvents.StateChanged.Unsubscribe(OnState);}
        /// <summary>XRI modality adapter calls on pose/modality changes, including controller fallback.</summary>
        public void SetTracking(InputModality modality,bool leftTracked,bool rightTracked){if(Modality==modality && left==leftTracked && right==rightTracked)return;Modality=modality;left=leftTracked;right=rightTracked;GameEvents.TrackingChanged.Raise(new TrackingChanged(modality,left,right));}
        private void Update(){recovery.Tick(Modality,left,right,focused,Time.unscaledDeltaTime);if(recovery.DropLeft)cancelLeft.Invoke();if(recovery.DropRight)cancelRight.Invoke();headSpeed.Sample(HeadPosition,true,Time.deltaTime);leftSpeed.Sample(leftPose?leftPose.position:Vector3.zero,left,Time.deltaTime);rightSpeed.Sample(rightPose?rightPose.position:Vector3.zero,right,Time.deltaTime);}
        private void OnSettings(SettingsChanged s){ApplySettings(s.Settings);}
        private void ApplySettings(ExperienceSettings s){if(heightRoot && head)heightRoot.localPosition=baseHeight+Vector3.up*ReachRules.CalibratedOffset(head.localPosition.y,s);}
        private void OnAct(ActStarted act){if(placed && !act.IsRestore && act.Act!=ActId.Awakening)return;placed=true;var point=WardLayout.ActStart(act.Act);origin.MoveCameraToWorldLocation(point.Position+Vector3.up*(HeadPosition.y-FloorPosition.y));origin.MatchOriginUpCameraForward(Vector3.up,Quaternion.Euler(0,point.YawDegrees,0)*Vector3.forward);}
        private void OnState(GameStateChanged state){if(!GameStateRules.IsSimulationLive(state.To)){cancelLeft.Invoke();cancelRight.Invoke();}}
        private void OnApplicationFocus(bool focus){focused=focus;}
    }
}
