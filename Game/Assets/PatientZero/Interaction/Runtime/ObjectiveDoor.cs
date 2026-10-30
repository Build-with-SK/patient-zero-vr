// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using UnityEngine;
namespace PatientZero.Interaction
{
    /// <summary>Animates objective doors; v1.1 director gate is authoritative for the isolation door.</summary>
    public sealed class ObjectiveDoor : MonoBehaviour
    {
        public ObjectivePublisher objectives; public Transform leaf; public bool isolationDoor; public float openDegrees=90;
        private readonly DoorGateLatch gate=new DoorGateLatch();
        private bool opening,completed; private Quaternion shut; private float degrees;
        public bool IsOpen => completed;
        private void Awake(){if(leaf)shut=leaf.localRotation;}
        private void OnEnable()
        {
            GameEvents.ObjectiveCompleted.Subscribe(Objective);GameEvents.ActStarted.Subscribe(Act);GameEvents.GateReleased.Subscribe(Released);
            // Read the retained state AFTER subscribing, so late-enable does not lose the release.
            if(isolationDoor && Services<IExperienceDirector>.TryGet(out var director))
            {
                if(!initialized){initialized=true;ResetForAct(director.CurrentAct);}
                ObserveGate(director);
            }
        }
        private bool initialized;
        private void OnDisable(){GameEvents.ObjectiveCompleted.Unsubscribe(Objective);GameEvents.ActStarted.Unsubscribe(Act);GameEvents.GateReleased.Unsubscribe(Released);}
        private void Act(ActStarted started){initialized=true;ResetForAct(started.Act);if(isolationDoor && Services<IExperienceDirector>.TryGet(out var director))ObserveGate(director);}
        private void ResetForAct(ActId act){gate.Reset(act);opening=completed=isolationDoor && act>ActId.Awakening;degrees=completed?openDegrees:0;if(leaf)leaf.localRotation=shut*Quaternion.AngleAxis(degrees,Vector3.up);}
        private void Released(GateReleased released){if(!isolationDoor || released.Gate!=GateId.Act1Door)return;if(Services<IExperienceDirector>.TryGet(out var director))ObserveGate(director);}
        private void ObserveGate(IExperienceDirector director)
        {
            if(!initialized && director.CurrentAct!=ActId.None){initialized=true;ResetForAct(director.CurrentAct);}
            if(!gate.Observe(GateId.Act1Door,director.IsGateReleased(GateId.Act1Door),director.CurrentAct))return;
            opening=true;Cues.Play(CueId.DoorUnlatch,transform.position,NoiseOrigin.Player);
        }
        private void Objective(ObjectiveCompleted objective){if(isolationDoor || objective.Objective!=ObjectiveId.Act4_ReleasePulled || opening || completed)return;opening=true;Cues.Play(CueId.ExitDoorOpen,transform.position,NoiseOrigin.Player);}
        private void Update()
        {
            if(isolationDoor && !initialized && Services<IExperienceDirector>.TryGet(out var director))ObserveGate(director);
            if(!opening || completed || !leaf || !objectives || !objectives.Live)return;
            degrees=Mathf.MoveTowards(degrees,openDegrees,60*Time.deltaTime);leaf.localRotation=shut*Quaternion.AngleAxis(degrees,Vector3.up);
            if(degrees>=openDegrees){completed=true;objectives.Complete(isolationDoor?ObjectiveId.Act1_DoorOpened:ObjectiveId.Act4_ExitOpened);}
        }
    }
}
