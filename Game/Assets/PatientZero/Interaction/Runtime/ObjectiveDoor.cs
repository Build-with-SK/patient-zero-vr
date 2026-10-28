// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Interaction
{
    /// <summary>Door animation and objective completion. Act1 stays locked pending approved director gate.</summary>
    public sealed class ObjectiveDoor : MonoBehaviour
    {
        public ObjectivePublisher objectives;public Transform leaf;public bool isolationDoor;public float openDegrees=90;
        private bool opening,completed;private Quaternion shut;private float degrees;
        private void Awake(){if(leaf)shut=leaf.localRotation;}
        private void OnEnable(){GameEvents.ObjectiveCompleted.Subscribe(Objective);GameEvents.ActStarted.Subscribe(Act);}
        private void OnDisable(){GameEvents.ObjectiveCompleted.Unsubscribe(Objective);GameEvents.ActStarted.Unsubscribe(Act);}
        private void Act(ActStarted act){opening=completed=isolationDoor && act.Act>ActId.Awakening;degrees=completed?openDegrees:0;if(leaf)leaf.localRotation=shut*Quaternion.AngleAxis(degrees,Vector3.up);}
        private void Objective(ObjectiveCompleted objective){if(!isolationDoor && objective.Objective==ObjectiveId.Act4_ReleasePulled){opening=true;Cues.Play(CueId.ExitDoorOpen,transform.position,NoiseOrigin.Player);}}
        /// <summary>Integration hook only: wire after Claude approves a Core director gate signal, never a local timer.</summary>
        public bool ReleaseIsolationGate(){if(!isolationDoor || !objectives || !objectives.IsComplete(ObjectiveId.Act1_DoorUnbarred))return false;opening=true;Cues.Play(CueId.DoorUnlatch,transform.position,NoiseOrigin.Player);return true;}
        private void Update(){if(!opening || completed || !leaf || !objectives || !objectives.Live)return;degrees=Mathf.MoveTowards(degrees,openDegrees,60*Time.deltaTime);leaf.localRotation=shut*Quaternion.AngleAxis(degrees,Vector3.up);if(degrees>=openDegrees){completed=true;objectives.Complete(isolationDoor?ObjectiveId.Act1_DoorOpened:ObjectiveId.Act4_ExitOpened);}}
    }
}
