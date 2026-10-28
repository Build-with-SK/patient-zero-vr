using System;
using PatientZero.Core;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Ward-local objective deduplication; the runtime publisher forwards the callback to Core.</summary>
    public sealed class ObjectiveTracker
    {
        private readonly bool[] complete=new bool[8];
        public event Action<ObjectiveId> Completed;
        public bool IsComplete(ObjectiveId id) { int index=Index(id);return index>=0 && complete[index]; }
        public bool Complete(ObjectiveId id)
        {
            int index=Index(id);if(index<0 || complete[index])return false;
            complete[index]=true;Completed?.Invoke(id);
            if((id==ObjectiveId.Act2_ValveVented || id==ObjectiveId.Act2_CablePlugged) && IsComplete(ObjectiveId.Act2_ValveVented) && IsComplete(ObjectiveId.Act2_CablePlugged))Complete(ObjectiveId.Act2_PowerRestored);
            return true;
        }
        public void Reset(ActId act)
        {
            Array.Clear(complete,0,complete.Length);
            // Restore previous acts silently: never replay their events into the director.
            if(act>ActId.Awakening){complete[0]=complete[1]=true;}
            if(act>ActId.Machinery){complete[2]=complete[3]=complete[4]=true;}
            if(act>ActId.Darkness)complete[5]=true;
        }
        private static int Index(ObjectiveId id)
        {
            switch(id){case ObjectiveId.Act1_DoorUnbarred:return 0;case ObjectiveId.Act1_DoorOpened:return 1;case ObjectiveId.Act2_ValveVented:return 2;case ObjectiveId.Act2_CablePlugged:return 3;case ObjectiveId.Act2_PowerRestored:return 4;case ObjectiveId.Act3_BeaconReached:return 5;case ObjectiveId.Act4_ReleasePulled:return 6;case ObjectiveId.Act4_ExitOpened:return 7;default:return -1;}
        }
    }
}
