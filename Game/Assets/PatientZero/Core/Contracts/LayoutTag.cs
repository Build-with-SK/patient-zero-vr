using UnityEngine;

namespace PatientZero.Core
{
    /// <summary>
    /// Marks the object a build step placed for a WardLayout id. The scene composer's
    /// validation requires exactly one tag per required id, within 5 cm of its layout position.
    /// </summary>
    public sealed class LayoutTag : MonoBehaviour
    {
        [Tooltip("A WardLayout id, e.g. \"A2.ValveWheel\".")]
        public string LayoutId;
    }
}
