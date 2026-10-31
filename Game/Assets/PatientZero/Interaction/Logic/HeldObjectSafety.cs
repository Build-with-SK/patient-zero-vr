using UnityEngine;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Release velocity filter used by the grab adapter; cancellation never throws.</summary>
    public static class HeldObjectSafety
    {
        public static Vector3 ReleaseVelocity(Vector3 velocity,bool tracked,bool cancelled,float maximumSpeed=8f)
        {
            if(!tracked || cancelled || float.IsNaN(velocity.x+velocity.y+velocity.z) || float.IsInfinity(velocity.sqrMagnitude))return Vector3.zero;
            return Vector3.ClampMagnitude(velocity,Mathf.Max(0,maximumSpeed));
        }
    }
}
