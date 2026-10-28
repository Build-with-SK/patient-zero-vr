// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Player
{
    /// <summary>Limits a standard XRI teleport anchor to the current act and approved 3m range.</summary>
    public sealed class AnchorGate : MonoBehaviour
    {
        public ActId act;public Collider target;public Renderer marker;private ActId current;private bool live;
        private void OnEnable(){GameEvents.ActStarted.Subscribe(Act);GameEvents.StateChanged.Subscribe(State);}
        private void OnDisable(){GameEvents.ActStarted.Unsubscribe(Act);GameEvents.StateChanged.Unsubscribe(State);}
        private void Act(ActStarted started){current=started.Act;live=true;}
        private void State(GameStateChanged changed){live=GameStateRules.IsSimulationLive(changed.To);}
        private void Update(){bool available=live && current==act && Services<IPlayerQuery>.TryGet(out var player) && WardLayout.HorizontalDistance(player.FloorPosition,transform.position)<=WardLayout.TeleportMaxDistance;if(target)target.enabled=available;if(marker)marker.enabled=available && aimVisible;}
        private bool aimVisible;
        /// <summary>XRI teleport ray activation events set marker visibility; no always-on beacon lighting.</summary>
        public void SetAimVisible(bool visible){aimVisible=visible;}
    }
}
