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
        // E-26 (owner, 2026-11-13: "make it open like an actual door"): the other leaf of a double door, on the opposite jamb;
        // it turns the other way, so both swing the same way. The emergency door and the floor doors used to be one leaf
        // turning about its middle.
        public Transform pair;
        // v1.3 (E-19): the lower floors' doors open on their own lever. The defaults are the ward's emergency door.
        public ActId doorAct=ActId.Exit; public ObjectiveId opensOn=ObjectiveId.Act4_ReleasePulled, completes=ObjectiveId.Act4_ExitOpened;
        private readonly DoorGateLatch gate=new DoorGateLatch();
        private bool opening,completed; private Quaternion shut,pairShut; private float degrees;
        public bool IsOpen => completed;
        private void Awake(){if(leaf)shut=leaf.localRotation;if(pair)pairShut=pair.localRotation;}
        private void Turn(){if(leaf)leaf.localRotation=shut*Quaternion.AngleAxis(degrees,Vector3.up);if(pair)pair.localRotation=pairShut*Quaternion.AngleAxis(-degrees,Vector3.up);}
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
        private void ResetForAct(ActId act){gate.Reset(act);opening=completed=isolationDoor?act>ActId.Awakening:act>doorAct;degrees=completed?openDegrees:0;Turn();}
        private void Released(GateReleased released){if(!isolationDoor || released.Gate!=GateId.Act1Door)return;if(Services<IExperienceDirector>.TryGet(out var director))ObserveGate(director);}
        private void ObserveGate(IExperienceDirector director)
        {
            if(!initialized && director.CurrentAct!=ActId.None){initialized=true;ResetForAct(director.CurrentAct);}
            if(!gate.Observe(GateId.Act1Door,director.IsGateReleased(GateId.Act1Door),director.CurrentAct))return;
            opening=true;Cues.Play(CueId.DoorUnlatch,transform.position,NoiseOrigin.Player);
        }
        private void Objective(ObjectiveCompleted objective){if(isolationDoor || objective.Objective!=opensOn || opening || completed)return;opening=true;Cues.Play(CueId.ExitDoorOpen,transform.position,NoiseOrigin.Player);}
        private void Update()
        {
            if(isolationDoor && !initialized && Services<IExperienceDirector>.TryGet(out var director))ObserveGate(director);
            if(!opening || completed || !leaf || !objectives || !objectives.Live)return;
            degrees=Mathf.MoveTowards(degrees,openDegrees,60*Time.deltaTime);Turn();
            if(degrees>=openDegrees){completed=true;objectives.Complete(isolationDoor?ObjectiveId.Act1_DoorOpened:completes);}
        }
    }
}
