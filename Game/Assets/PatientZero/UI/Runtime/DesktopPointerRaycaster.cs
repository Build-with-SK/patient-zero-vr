using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
namespace PatientZero.UI
{
    /// <summary>
    /// Integration E-12: XRI's TrackedDeviceGraphicRaycaster answers only tracked-device events, so a mouse cannot click
    /// the world-space menus. UiBuildStep gives every menu panel a standard GraphicRaycaster, disabled, plus this switch.
    /// Only the flat-screen builds (keyboard and mouse PZ_DESKTOP_MOUSE, touch PZ_TOUCH) turn it on, and only while no headset is active. Quest
    /// builds never do: an enabled GraphicRaycaster would also answer tracked-device events at a stale screen position.
    /// </summary>
    public sealed class DesktopPointerRaycaster : MonoBehaviour
    {
        public GraphicRaycaster raycaster;
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
        private void OnEnable(){if(raycaster)raycaster.enabled=!XRSettings.isDeviceActive;}
#endif
    }
}
