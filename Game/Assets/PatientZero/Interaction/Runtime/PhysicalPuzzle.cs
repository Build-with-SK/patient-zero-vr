// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace PatientZero.Interaction
{
    /// <summary>Shared one-hand constrained mechanic API, driven by XRI selection or desktop grip simulation.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PhysicalPuzzle : MonoBehaviour
    {
        public enum Mechanic {Plank,Valve,Lever}
        public Mechanic mechanic;public ObjectivePublisher objectives;public int plankIndex;public Transform movingPart;public Vector3 axis=Vector3.forward;
        private ValveMath valve=new ValveMath();private PlankLogic plank=new PlankLogic();private LeverLogic lever=new LeverLogic();
        private PlayerRig rig;private float visualDegrees;private XRSimpleInteractable interactable;private Transform grip;private Vector3 gripStart,home;private Quaternion rotation;private bool held;private float lastProgress;private CueHandle loop;
        public float Progress => mechanic==Mechanic.Valve?valve.Progress:mechanic==Mechanic.Lever?lever.Progress:plank.Intensity;
        public string Hint => mechanic==Mechanic.Plank?"Grab and pull plank":mechanic==Mechanic.Valve?"Grab rim and turn; re-grip freely":"Pull down and hold release";
        private ObjectiveId Objective => mechanic==Mechanic.Plank?ObjectiveId.Act1_DoorUnbarred:mechanic==Mechanic.Valve?ObjectiveId.Act2_ValveVented:ObjectiveId.Act4_ReleasePulled;
        private void Awake(){interactable=GetComponent<XRSimpleInteractable>();if(!movingPart)movingPart=transform;home=movingPart.localPosition;rotation=movingPart.localRotation;}
        private void OnEnable(){if(Services<IPlayerQuery>.TryGet(out var player)){rig=player as PlayerRig;if(rig){rig.cancelLeft.AddListener(CancelGrip);rig.cancelRight.AddListener(CancelGrip);}}interactable.selectEntered.AddListener(Selected);interactable.selectExited.AddListener(Deselected);GameEvents.AssistChanged.Subscribe(Assist);GameEvents.ActStarted.Subscribe(Act);GameEvents.SettingsChanged.Subscribe(Settings);GameEvents.StateChanged.Subscribe(State);if(Services<ISettingsProvider>.TryGet(out var s))SetAssist(s.Current.PuzzleAssistAlwaysOn);}
        private void OnDisable(){if(rig){rig.cancelLeft.RemoveListener(CancelGrip);rig.cancelRight.RemoveListener(CancelGrip);}interactable.selectEntered.RemoveListener(Selected);interactable.selectExited.RemoveListener(Deselected);GameEvents.AssistChanged.Unsubscribe(Assist);GameEvents.ActStarted.Unsubscribe(Act);GameEvents.SettingsChanged.Unsubscribe(Settings);GameEvents.StateChanged.Unsubscribe(State);EndGrip();}
        private bool Available => objectives && objectives.Live && objectives.Act==(mechanic==Mechanic.Plank?ActId.Awakening:mechanic==Mechanic.Valve?ActId.Machinery:ActId.Exit);
        private void Selected(SelectEnterEventArgs args){grip=args.interactorObject.GetAttachTransform(interactable);BeginGrip(grip.position);}
        private void Deselected(SelectExitEventArgs args){EndGrip();}
        public void BeginGrip(Vector3 point){if(!Available)return;held=true;gripStart=point;if(mechanic==Mechanic.Valve)valve.BeginGrip(point-transform.position,transform.TransformDirection(axis));loop=Cues.PlayAttached(mechanic==Mechanic.Valve?CueId.ValveSqueal:mechanic==Mechanic.Plank?CueId.PlankCreak:CueId.ReleaseBarGrind,transform,NoiseOrigin.Player,0);}
        public void CancelGrip(){if(interactable && interactable.isSelected && interactable.interactionManager)interactable.interactionManager.CancelInteractableSelection(interactable);EndGrip();}
        public void EndGrip(){held=false;grip=null;valve.EndGrip();plank.Release();Cues.Stop(loop);loop=default;if(mechanic==Mechanic.Plank && !plank.Popped && movingPart)movingPart.localPosition=home;}
        public void SampleGrip(Vector3 point,float seconds)
        {
            if(!held || !Available)return;
            if(mechanic==Mechanic.Plank){float pull=Vector3.Dot(point-gripStart,transform.TransformDirection(axis));bool popped=plank.Pull(pull);movingPart.localPosition=home+axis*Mathf.Min(plank.PullMeters,plank.Threshold);Cues.SetIntensity(loop,plank.Intensity);if(popped){objectives.PlankRemoved(plankIndex);Cues.Play(CueId.PlankPop,transform.position,NoiseOrigin.Player);EndGrip();movingPart.gameObject.SetActive(false);}}
            else if(mechanic==Mechanic.Valve){float delta=valve.Sample(point-transform.position,transform.TransformDirection(axis),seconds);visualDegrees+=delta;movingPart.localRotation=Quaternion.AngleAxis(visualDegrees,axis)*rotation;Cues.SetIntensity(loop,valve.Intensity);PublishProgress();if(valve.Complete){objectives.Complete(Objective);Cues.Play(CueId.ValveVentHiss,transform.position,NoiseOrigin.Player);EndGrip();}}
            else {leverPulled=Vector3.Dot(gripStart-point,Vector3.up)>=.04f;}
        }
        private bool leverPulled;
        private void Update(){if(held && grip)SampleGrip(grip.position,Time.deltaTime);if(mechanic!=Mechanic.Lever || !Available)return;if(lever.Tick(held && leverPulled,Time.deltaTime))objectives.Progress(Objective,lever.Progress);movingPart.localRotation=rotation*Quaternion.AngleAxis(lever.Progress*55,Vector3.right);if(lever.Complete){objectives.Complete(Objective);EndGrip();}}
        private void PublishProgress(){if(Mathf.Abs(Progress-lastProgress)>=.05f || Progress>=1){lastProgress=Progress;objectives.Progress(Objective,Progress);}}
        private void Assist(AssistChanged assist){if(assist.Objective==Objective)SetAssist(assist.Level==AssistLevel.Assist);}
        private void Settings(SettingsChanged s){if(s.Settings.PuzzleAssistAlwaysOn)SetAssist(true);}
        private void SetAssist(bool enabled){plank.Assisted=enabled;valve.SetAssist(enabled);lever.SetAssist(enabled);if(Available && mechanic==Mechanic.Valve && valve.Complete){PublishProgress();objectives.Complete(Objective);EndGrip();}}
        private void State(GameStateChanged state){if(!GameStateRules.IsSimulationLive(state.To))CancelGrip();}
        private void Act(ActStarted act){EndGrip();valve=new ValveMath();plank=new PlankLogic();lever=new LeverLogic();lastProgress=0;visualDegrees=0;leverPulled=false;movingPart.localPosition=home;movingPart.localRotation=rotation;bool past=act.Act>(mechanic==Mechanic.Plank?ActId.Awakening:mechanic==Mechanic.Valve?ActId.Machinery:ActId.Exit);interactable.enabled=!past;movingPart.gameObject.SetActive(!(past && mechanic==Mechanic.Plank));if(past && mechanic==Mechanic.Valve)movingPart.localRotation=Quaternion.AngleAxis(540,axis)*rotation;if(Services<ISettingsProvider>.TryGet(out var s))SetAssist(s.Current.PuzzleAssistAlwaysOn);}
    }
}
