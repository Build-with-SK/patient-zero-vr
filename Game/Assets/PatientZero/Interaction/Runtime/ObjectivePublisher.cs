// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using UnityEngine;
namespace PatientZero.Interaction
{
    /// <summary>Ward-local objective aggregation. Puzzle APIs call Complete/Progress/PlankRemoved.</summary>
    public sealed class ObjectivePublisher : MonoBehaviour
    {
        private readonly ObjectiveTracker tracker=new ObjectiveTracker();private readonly bool[] planks=new bool[3];private int plankCount;
        public ActId Act {get;private set;}
        public bool Live {get;private set;}
        private void OnEnable(){tracker.Completed+=Publish;GameEvents.ActStarted.Subscribe(OnAct);GameEvents.StateChanged.Subscribe(OnState);}
        private void OnDisable(){tracker.Completed-=Publish;GameEvents.ActStarted.Unsubscribe(OnAct);GameEvents.StateChanged.Unsubscribe(OnState);}
        private void OnAct(ActStarted act){Act=act.Act;tracker.Reset(act.Act);plankCount=0;for(int i=0;i<3;i++)planks[i]=false;Live=true;}
        private void OnState(GameStateChanged state){Live=GameStateRules.IsSimulationLive(state.To);}
        public bool IsComplete(ObjectiveId id)=>tracker.IsComplete(id);
        public void Complete(ObjectiveId id){if(Live)tracker.Complete(id);}
        public void Progress(ObjectiveId id,float progress){if(Live)GameEvents.ObjectiveProgress.Raise(new ObjectiveProgress(id,progress));}
        public void PlankRemoved(int index){if(!Live || Act!=ActId.Awakening || index<0 || index>=3 || planks[index])return;planks[index]=true;plankCount++;Progress(ObjectiveId.Act1_DoorUnbarred,plankCount/3f);if(plankCount==3)Complete(ObjectiveId.Act1_DoorUnbarred);}
        private void Publish(ObjectiveId id){GameEvents.ObjectiveCompleted.Raise(new ObjectiveCompleted(id));}
    }
}
