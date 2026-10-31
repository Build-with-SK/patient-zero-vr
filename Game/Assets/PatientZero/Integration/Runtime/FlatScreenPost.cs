// Claude-owned integration (art pass, 2026-10-31; E-20).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace PatientZero.Integration
{
    /// <summary>
    /// Integration E-20: the flat-screen look (bloom, vignette, film grain, colour grading, ACES). The volume and the
    /// camera's post-processing start off and are switched on only while no headset is active, so Quest and Link never
    /// pay for them. Added to Bootstrap by ArtPassStep, which also builds the volume profile.
    /// </summary>
    public sealed class FlatScreenPost : MonoBehaviour
    {
        public Volume volume;
        public UniversalAdditionalCameraData cameraData;
        private bool applied, flat;

        private void OnEnable() => Apply();

        private void Update()
        {
            // A headset can start or stop after load (Link). Checking twice a second is enough and costs nothing.
            if ((Time.frameCount & 31) == 0) Apply();
        }

        private void Apply()
        {
            bool wantFlat = !XRSettings.isDeviceActive;
            if (applied && wantFlat == flat) return;
            applied = true;
            flat = wantFlat;
            if (volume) volume.enabled = flat;
            if (cameraData) cameraData.renderPostProcessing = flat;
        }
    }
}
