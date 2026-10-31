using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Interaction
{
    /// <summary>
    /// Contract v1.3 (Claude integration E-19): completes a floor's stairs objective when the player stands on its landing
    /// after the stair door has opened. The director then takes the player down a floor. Built by
    /// <c>InteractionBuildStep.Floors</c>; the Act 3 beacon (<see cref="BeaconArrival"/>) is the same idea.
    /// </summary>
    public sealed class StairArrival : MonoBehaviour
    {
        public ObjectivePublisher objectives;
        public ActId act;
        public ObjectiveId objective;
        public ObjectiveId requires;
        public Vector3 landing;
        public float radius = WardLayout.StairTriggerRadius;

        private void Update()
        {
            if (!objectives || !objectives.Live || objectives.Act != act || objectives.IsComplete(objective)) return;
            if (requires != ObjectiveId.None && !objectives.IsComplete(requires)) return;
            if (Services<IPlayerQuery>.TryGet(out var player) && WardLayout.HorizontalDistance(player.FloorPosition, landing) <= radius)
                objectives.Complete(objective);
        }
    }
}
