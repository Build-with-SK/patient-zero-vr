// UNVERIFIED IN EDITOR
using PatientZero.Atmosphere.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Atmosphere
{
    /// <summary>The only runtime lighting writer; six no-shadow fixtures, fog, ambient and emission.</summary>
    public sealed class LightingController : MonoBehaviour
    {
        public Light[] Fixtures = new Light[0];
        public Renderer[] Emissives = new Renderer[0];
        public Renderer[] FixtureFaces = new Renderer[0];
        public Renderer[] StoryTraces = new Renderer[0];
        public Renderer[] GuidanceStrip = new Renderer[0];
        private MaterialPropertyBlock block;
        private LightingState current;
        private LightingPreset target;
        private ExperienceSettings settings = ExperienceSettings.Defaults;
        private float entered, fromAmbient, fromGain, gain;
        private Color fromFog, fromAmbientColour;
        private readonly LocalLightPulse pulse = new LocalLightPulse();
        private float beaconUntil, headlampGain;
        private bool mobileHeadlamp;
        private Vector3 headlampHome;
        private Quaternion headlampRotation;
        public bool MobileHeadlampRequested { get; private set; }
        public bool HeadlampUsable => mobileHeadlamp && FixturePolicy.HeadlampAllowed(current);
        public void ConfigureMobileHeadlamp(bool value)
        {
            if (mobileHeadlamp == value || Fixtures.Length == 0 || !Fixtures[0]) return;
            var lamp = Fixtures[0];
            if (value) { headlampHome = lamp.transform.position; headlampRotation = lamp.transform.rotation; }
            else { lamp.transform.SetPositionAndRotation(headlampHome, headlampRotation); lamp.type = LightType.Point; lamp.range = 3; }
            mobileHeadlamp = value; MobileHeadlampRequested = false; headlampGain = 0;
            if (FixtureFaces.Length > 0 && FixtureFaces[0]) FixtureFaces[0].enabled = !value;
        }
        public void SetMobileHeadlamp(bool value) => MobileHeadlampRequested = mobileHeadlamp && FixturePolicy.HeadlampAllowed(current) && value;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private void Awake() => block = new MaterialPropertyBlock();
        private void OnEnable()
        {
            GameEvents.SettingsChanged.Subscribe(SettingsChanged);
            if (Services<ISettingsProvider>.TryGet(out var provider)) settings = provider.Current;
        }
        private void OnDisable() => GameEvents.SettingsChanged.Unsubscribe(SettingsChanged);
        private void SettingsChanged(SettingsChanged e)
        { settings = e.Settings; target = LightingPresets.Get(current, settings.DarknessBrightness01); }
        /// <summary>Called by the director; never flashes or changes camera exposure.</summary>
        public void SetState(LightingState state)
        {
            fromAmbient = RenderSettings.ambientIntensity; fromGain = gain; current = state; entered = Time.time;
            fromFog = RenderSettings.fogColor; fromAmbientColour = RenderSettings.ambientLight;
            pulse.Reset();
            if (!FixturePolicy.HeadlampAllowed(state)) MobileHeadlampRequested = false;
            target = LightingPresets.Get(state, settings.DarknessBrightness01);
            RenderSettings.fog = true; RenderSettings.fogMode = target.ExponentialFog ? FogMode.Exponential : FogMode.Linear;
            RenderSettings.fogStartDistance = target.FogNear; RenderSettings.fogEndDistance = target.FogFar; RenderSettings.fogDensity = target.FogDensity;
            GameEvents.LightingChanged.Raise(new LightingChanged(state, target.Transition));
        }
        /// <summary>Director requests a local window light pulse; the tested policy suppresses unsafe extra edges.</summary>
        public void WindowPulse() { if (current == LightingState.Awakening) pulse.Request(Time.time - entered, settings.ReduceFlicker); }
        /// <summary>Director beacon cue drives optional sound-cue visuals without creating another light.</summary>
        public void BeaconPulse() => beaconUntil = Time.time + .5f;
        private void Update()
        {
            float age = Time.time - entered, blend = target.Transition <= 0 ? 1 : Mathf.Clamp01(age / target.Transition);
            RenderSettings.ambientIntensity = Mathf.Lerp(fromAmbient, target.Ambient, blend);
            gain = Mathf.Lerp(fromGain, target.FixtureGain, blend);
            bool red = current == LightingState.EmergencyRed;
            Color colour = red ? new Color(1, .08f, .05f) : current == LightingState.ExitDaylight ? Color.white : new Color(.65f, .8f, 1);
            RenderSettings.ambientLight = Color.Lerp(fromAmbientColour, colour, blend);
            RenderSettings.fogColor = Color.Lerp(fromFog, colour * (current == LightingState.ExitDaylight ? 1 : .05f), blend);
            for (int i = 0; i < Fixtures.Length; i++)
            {
                var fixture = Fixtures[i]; if (!fixture) continue;
                bool zone = FixturePolicy.Enabled(current, i);
                fixture.color = colour; fixture.shadows = LightShadows.None;
                float variation = FixturePolicy.Variation(current, age, settings.ReduceFlicker, i, pulse);
                fixture.intensity = zone ? gain * variation : 0;
                if (i == 0 && mobileHeadlamp)
                {
                    bool on = MobileHeadlampRequested && FixturePolicy.HeadlampAllowed(current);
                    headlampGain = Mathf.MoveTowards(headlampGain, on ? .9f : 0, Time.deltaTime * 6);
                    fixture.type = LightType.Spot; fixture.range = 7.5f; fixture.spotAngle = 48; fixture.color = new Color(.9f, .95f, 1);
                    if (Services<IPlayerQuery>.TryGet(out var player) && player.HeadForward.sqrMagnitude > .001f)
                    {
                        fixture.transform.SetPositionAndRotation(player.HeadPosition + player.HeadForward * .05f, Quaternion.LookRotation(player.HeadForward));
                        fixture.intensity = headlampGain;
                    }
                    else fixture.intensity = 0;
                }
                if (i < FixtureFaces.Length && FixtureFaces[i])
                {
                    FixtureFaces[i].GetPropertyBlock(block);
                    block.SetColor(EmissionId, colour * fixture.intensity * .6f);
                    FixtureFaces[i].SetPropertyBlock(block);
                }
            }
            bool showTraces = FixturePolicy.ShowTraces(settings);
            for (int i = 0; i < StoryTraces.Length; i++) if (StoryTraces[i]) StoryTraces[i].enabled = showTraces;
            for (int i = 0; i < Emissives.Length; i++)
            {
                if (!Emissives[i]) continue;
                Emissives[i].GetPropertyBlock(block); block.SetColor(EmissionId, new Color(.08f, .5f, .12f) * (current == LightingState.Blackout ? 1 : Mathf.Max(.2f, gain)));
                Emissives[i].SetPropertyBlock(block);
            }
            for (int i = 0; i < GuidanceStrip.Length; i++)
                if (GuidanceStrip[i]) GuidanceStrip[i].enabled = settings.SoundCueVisuals && current == LightingState.Blackout && Time.time < beaconUntil;
        }
    }
}
