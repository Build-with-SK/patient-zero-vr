// Integration exception E-17 (Claude, owner final-release mission, 2026-10-30): written in seat B's area because the
// owner asked for flashlight gameplay tonight. It follows seat B's own request (CODEX_B_LOG 2026-10-30 18:50: "B owns the
// actual light shell after the contract lands"); seat B may rewrite it freely.
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Atmosphere
{
    /// <summary>
    /// The player's torch (D-1b, contract v1.2). Interaction raises HandheldLightChanged with the switch and the beam pose;
    /// this owns the light. The surge kills it (LightingChanged Surge, Blackout or EmergencyRed), so Act 3 stays the
    /// sound-navigation act; it works again once the lights are back (Awakening, WardDim, ExitDaylight), including after
    /// an act restart. One shadowless spot light, created on first use only, so the composed ward keeps its light budget
    /// and the Quest build (which never raises the event) never has it. Dies with a 0.6 s fade, not a flicker (comfort
    /// rule: no luminance change above 3 Hz). Added to Ward by the Integration build step HandheldLightStep.
    /// </summary>
    public sealed class HandheldLight : MonoBehaviour
    {
        public const float Intensity = 3f, FadeSeconds = .6f, Range = 11f;
        private Light beam;
        private bool wanted, dead;
        private float fade;

        public bool Shining => beam && beam.enabled;
        public bool Dead => dead;

        private void OnEnable()
        {
            GameEvents.HandheldLightChanged.Subscribe(Changed);
            GameEvents.LightingChanged.Subscribe(Lighting);
        }

        private void OnDisable()
        {
            GameEvents.HandheldLightChanged.Unsubscribe(Changed);
            GameEvents.LightingChanged.Unsubscribe(Lighting);
        }

        private void Changed(HandheldLightChanged change)
        {
            wanted = change.Enabled;
            if (wanted && !beam) beam = CreateBeam(transform);
            if (!beam) return;
            beam.transform.SetPositionAndRotation(change.Position,
                change.Forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(change.Forward) : beam.transform.rotation);
            if (!dead) { beam.intensity = Intensity; beam.enabled = wanted; }
        }

        private void Lighting(LightingChanged change)
        {
            bool powerCut = change.State == LightingState.Surge || change.State == LightingState.Blackout || change.State == LightingState.EmergencyRed;
            if (powerCut && !dead)
            {
                dead = true;
                fade = Shining ? FadeSeconds : 0;
            }
            else if (!powerCut && change.State != LightingState.None && dead)
            {
                dead = false;
                fade = 0;
                if (beam) { beam.intensity = Intensity; beam.enabled = wanted; }
            }
        }

        private void Update()
        {
            if (!dead || !Shining) return;
            fade -= Time.deltaTime;
            if (fade <= 0) { beam.enabled = false; beam.intensity = Intensity; }
            else beam.intensity = Intensity * (fade / FadeSeconds);
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
