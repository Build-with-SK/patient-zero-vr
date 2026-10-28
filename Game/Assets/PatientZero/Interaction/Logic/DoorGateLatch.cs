using PatientZero.Core;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Consumes an approved Act1 gate exactly once per act start, whether observed live or queried late.</summary>
    public sealed class DoorGateLatch
    {
        private ActId act;private bool consumed;
        public void Reset(ActId currentAct){act=currentAct;consumed=false;}
        public bool Observe(GateId gate,bool released,ActId directorAct)
        {
            if(act!=ActId.Awakening || directorAct!=act || gate!=GateId.Act1Door || !released || consumed)return false;
            consumed=true;return true;
        }
    }
}
