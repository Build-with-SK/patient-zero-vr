// Native validation pending; this helper writes B-owned component masks only.
using PatientZero.Environment.Logic;
using UnityEngine;

namespace PatientZero.Environment.Editor
{
    public static class WardCollisionBinding
    {
        // Proposed integration name, not a new Core contract. No layer is created or renumbered here.
        public const string DoorLayerName = "PZ_ClosedDoor";
        public static WardCollisionPolicy Resolve() => new WardCollisionPolicy(LayerMask.NameToLayer(DoorLayerName));
    }
}
