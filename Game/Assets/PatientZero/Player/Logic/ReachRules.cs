using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Player.Logic
{
    /// <summary>Shared seated/standing reach check; layout height remains in world-floor metres.</summary>
    public static class ReachRules
    {
        public static bool CanReach(Vector3 floor,Vector3 target) => target.y>=WardLayout.MinInteractHeight && target.y<=WardLayout.MaxInteractHeight && WardLayout.HorizontalDistance(floor,target)<=WardLayout.MaxReachHorizontal+0.0001f;
        public static float CalibratedOffset(float measuredHeadHeight,ExperienceSettings settings)
        { return (settings.SeatedMode?Mathf.Clamp(1.6f-measuredHeadHeight,0,0.6f):0)+settings.Sanitized().HeightOffsetMeters; }
    }
}
