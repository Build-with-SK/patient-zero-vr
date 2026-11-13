// Integration exception E-17 (Claude, owner final-release mission, 2026-10-30): written in seat B's area because the
// owner asked for flashlight gameplay tonight. It follows seat B's own request (CODEX_B_LOG 2026-10-30 18:50: "B owns the
// actual light shell after the contract lands"); seat B may rewrite it freely.
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Atmosphere
{
    /// <summary>
    /// The player's torch (D-1b, contract v1.2). Interaction raises HandheldLightChanged with the switch and the beam pose;
    /// this owns the light. E-26 (owner, 2026-11-13: "keep the flashlight on default all the time, and turning it off it's the
    /// players wish"): the torch is on from the start and only the player turns it off. The power cut no longer kills it (it
    /// used to, from the surge until the lights came back, so that Act 3 was played by sound alone); the blind zombies hunt
    /// by sound, so the torch shows the way without giving the player away. One shadowless spot light, created on first use
    /// only, so the composed ward keeps its light budget and the Quest build (which never raises the event) never has it.
    /// Added to Ward by the Integration build step HandheldLightStep.
    /// </summary>
    public sealed class HandheldLight : MonoBehaviour
    {
        public const float Intensity = 3f, Range = 11f;
        private Light beam;

        public bool Shining => beam && beam.enabled;

        private void OnEnable() => GameEvents.HandheldLightChanged.Subscribe(Changed);

        private void OnDisable() => GameEvents.HandheldLightChanged.Unsubscribe(Changed);

        private void Changed(HandheldLightChanged change)
        {
            if (change.Enabled && !beam) beam = CreateBeam(transform);
            if (!beam) return;
            beam.transform.SetPositionAndRotation(change.Position,
                change.Forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(change.Forward) : beam.transform.rotation);
            beam.enabled = change.Enabled;
        }

        private static Light CreateBeam(Transform parent)
        {
            var torch = new GameObject("Handheld light");
            torch.transform.SetParent(parent, false);
            var light = torch.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 55;
            light.innerSpotAngle = 28;
            light.range = Range;
            light.intensity = Intensity;
            light.color = new Color(1f, .95f, .86f);
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }
    }
}
