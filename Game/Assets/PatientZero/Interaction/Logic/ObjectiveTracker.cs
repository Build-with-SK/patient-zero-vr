using System;
using PatientZero.Core;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Ward-local objective deduplication; the runtime publisher forwards the callback to Core.</summary>
    public sealed class ObjectiveTracker
    {
        private readonly bool[] complete=new bool[ObjectiveIds.Count];
        public event Action<ObjectiveId> Completed;
        public bool IsComplete(ObjectiveId id) { int index=Index(id);return index>=0 && complete[index]; }
        public bool Complete(ObjectiveId id)
        {
            if(id==ObjectiveId.Act2_PowerRestored && (!IsComplete(ObjectiveId.Act2_ValveVented) || !IsComplete(ObjectiveId.Act2_CablePlugged)))return false;
            int index=Index(id);if(index<0 || complete[index])return false;
            complete[index]=true;Completed?.Invoke(id);
            if((id==ObjectiveId.Act2_ValveVented || id==ObjectiveId.Act2_CablePlugged) && IsComplete(ObjectiveId.Act2_ValveVented) && IsComplete(ObjectiveId.Act2_CablePlugged))Complete(ObjectiveId.Act2_PowerRestored);
            return true;
        }
        public void Reset(ActId act)
        {
            Array.Clear(complete,0,complete.Length);
            // Restore previous acts silently: never replay their events into the director.
            // v1.3 (E-19): every objective of an earlier act, so floors 2 and 1 restore the same way.
            for(int i=0;i<complete.Length;i++)if(ObjectiveIds.ActOf(ObjectiveIds.All[i])<act)complete[i]=true;
        }
        private static int Index(ObjectiveId id)=>ObjectiveIds.IndexOf(id);
    }
}
