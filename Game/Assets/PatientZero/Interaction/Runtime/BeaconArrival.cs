// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Interaction
{
    /// <summary>Act3 arrival objective measured against the approved floor anchor.</summary>
    public sealed class BeaconArrival : MonoBehaviour
    {
        public ObjectivePublisher objectives;
        private void Update(){if(!objectives || !objectives.Live || objectives.Act!=ActId.Darkness || objectives.IsComplete(ObjectiveId.Act3_BeaconReached))return;if(Services<IPlayerQuery>.TryGet(out var player) && WardLayout.HorizontalDistance(player.FloorPosition,WardLayout.A3_End.Position)<=WardLayout.BeaconTriggerRadius)objectives.Complete(ObjectiveId.Act3_BeaconReached);}
    }
}
