using UnityEngine;

namespace PatientZero.XR
{
    // One fixed registry, including props instantiated after the rig. Cleared with domain reload disabled.
    public sealed class TrackingSafetyHub : MonoBehaviour
    {
        private static readonly HeldObjectSafety[] Objects = new HeldObjectSafety[128];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => System.Array.Clear(Objects, 0, Objects.Length);
        public static bool Register(HeldObjectSafety value)
        {
            for (int i = 0; i < Objects.Length; i++) if (Objects[i] == value) return true;
            for (int i = 0; i < Objects.Length; i++) if (!Objects[i]) { Objects[i] = value; return true; }
            return false;
        }
        public static void Unregister(HeldObjectSafety value)
        {
            for (int i = 0; i < Objects.Length; i++) if (Objects[i] == value) Objects[i] = null;
        }
        public void CancelHeldObjects()
        {
            for (int i = 0; i < Objects.Length; i++) if (Objects[i]) Objects[i].CancelForTrackingLoss();
        }
    }
}
