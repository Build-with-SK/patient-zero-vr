// UNVERIFIED IN EDITOR
using PatientZero.Audio.Logic;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace PatientZero.Audio
{
    /// <summary>Bootstrap audio service; all seats play only through Core Cues. Pool/occlusion port from SpatialAudioSystem@72be6d8.</summary>
    [DefaultExecutionOrder(-400)]
    public sealed class AudioCuePlayer : MonoBehaviour, IAudioCuePlayer
    {
        public AudioMixer Mixer;
        public bool BuildPlaceholders = true;
        public bool EnableLicensedSpatializer;
        public LayerMask Occluders = 1;
        private sealed class Voice
        {
            public AudioSource Source;
            public AudioLowPassFilter Filter;
            public Transform Follow;
            public CueDefinition Cue;
            public int Handle;
            public NoiseOrigin Origin;
            public float Intensity, Fade = 1, FadeRate, Occlusion = 1, NextNoise, NextCaption;
        }
        private readonly VoiceBudget budget = new VoiceBudget();
        private readonly NoiseGate noiseGate = new NoiseGate();
        private readonly Voice[] voices = new Voice[24];
        private AudioClip[] clips;
        private AnimationCurve[] curves;
        private AudioMixerGroup[] groups;
        private ExperienceSettings settings = ExperienceSettings.Defaults;
        private GameState state = GameState.Boot;
        private float nextOcclusion, silenceUntil;
        private bool initialized;
        private bool spatializerAvailable;
        public int MissingAssets { get; private set; }
        public int RejectedCues { get; private set; }
        public CueId LastStolenCue { get; private set; }
        public int ActiveVoices => budget.ActiveCount;
        public int SpatializedVoices => budget.SpatialCount;
        private void Awake()
        {
            spatializerAvailable = EnableLicensedSpatializer && !string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName());
            clips = new AudioClip[CueTable.Count]; curves = new AnimationCurve[CueTable.Count]; groups = new AudioMixerGroup[5];
            if (Mixer)
                for (int i = 0; i < groups.Length; i++)
                { var found = Mixer.FindMatchingGroups(((MixerBus)i).ToString()); groups[i] = found.Length > 0 ? found[0] : null; }
            for (int i = 0; i < CueTable.Count; i++)
            {
                var row = CueTable.At(i); if (BuildPlaceholders) clips[i] = PlaceholderClipFactory.Create(row);
                if (row.World)
                {
                    float midpoint = row.Min / row.Max, slope = -1 / (1 - midpoint);
                    curves[i] = new AnimationCurve(new Keyframe(0, 1, 0, 0), new Keyframe(midpoint, 1, 0, slope), new Keyframe(1, 0, slope, 0));
                    curves[i].postWrapMode = WrapMode.ClampForever;
                }
            }
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("Voice_" + i); child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>(); source.playOnAwake = false; source.dopplerLevel = 0;
                var filter = child.AddComponent<AudioLowPassFilter>(); filter.cutoffFrequency = 22000;
                voices[i] = new Voice { Source = source, Filter = filter };
            }
            initialized = true;
        }
        private void OnEnable()
        {
            if (!Services<IAudioCuePlayer>.Register(this)) { enabled = false; return; }
            GameEvents.SettingsChanged.Subscribe(SettingsChanged); GameEvents.StateChanged.Subscribe(StateChanged);
            GameEvents.ActStarted.Subscribe(ActStarted);
            if (Services<ISettingsProvider>.TryGet(out var provider)) settings = provider.Current.Sanitized();
        }
        private void OnDisable()
        {
            Services<IAudioCuePlayer>.Unregister(this); GameEvents.SettingsChanged.Unsubscribe(SettingsChanged);
            GameEvents.StateChanged.Unsubscribe(StateChanged); GameEvents.ActStarted.Unsubscribe(ActStarted);
            StopAll();
        }
        private void OnDestroy() { if (clips != null) for (int i = 0; i < clips.Length; i++) if (clips[i]) Destroy(clips[i]); }
        private void SettingsChanged(SettingsChanged e) => settings = e.Settings.Sanitized();
        private void StateChanged(GameStateChanged e) => state = e.To;
        private void ActStarted(ActStarted e)
        {
            for (int i = 0; i < voices.Length; i++) if (voices[i].Handle != 0 && voices[i].Cue.Bus != MixerBus.UI) Release(i);
        }
        public CueHandle Play(CueId cue, Vector3 position, float intensity01, NoiseOrigin origin) => StartCue(cue, position, null, intensity01, origin);
        public CueHandle PlayAttached(CueId cue, Transform follow, float intensity01, NoiseOrigin origin) =>
            follow ? StartCue(cue, follow.position, follow, intensity01, origin) : default;
        private CueHandle StartCue(CueId id, Vector3 position, Transform follow, float intensity, NoiseOrigin origin)
        {
            if (!initialized || id == CueId.None || !NoiseGate.Finite(position) || !NoiseGate.Finite(intensity) || intensity <= 0 || !CueTable.TryGet(id, out var row))
            { RejectedCues++; return default; }
            int index = Index(id);
            if (!clips[index]) { MissingAssets++; return default; }
            intensity = Mathf.Clamp01(intensity);
            if (id == CueId.DistantCrashLure) origin = NoiseOrigin.Scripted;
            var noise = new NoiseEvent(position, row.NoiseRadius * intensity, id, origin, Time.time);
            bool gated = noise.RadiusMeters > 0;
            if (gated && !noiseGate.TryEnter(in noise, SourceKey(id, position, origin), Time.time)) { RejectedCues++; return default; }
            try
            {
                var admission = budget.Start(row, spatializerAvailable);
                if (!admission.Accepted) { RejectedCues++; return default; }
                LastStolenCue = admission.Stolen;
                var v = voices[admission.Slot]; v.Source.Stop(); v.Handle = admission.Handle; v.Cue = row; v.Follow = follow;
                v.Intensity = intensity; v.Origin = origin; v.Fade = 1; v.FadeRate = 0; v.Occlusion = 1;
                v.NextNoise = Time.time + .2f; v.NextCaption = Time.time + 4;
                v.Source.transform.position = position; v.Source.clip = clips[index]; v.Source.loop = row.Loop;
                v.Source.priority = row.Priority; v.Source.spatialBlend = row.World ? 1 : 0;
                v.Source.spatialize = admission.Spatialized; v.Source.ignoreListenerPause = row.Bus == MixerBus.UI;
                v.Source.outputAudioMixerGroup = groups[(int)row.Bus]; v.Source.minDistance = row.Min; v.Source.maxDistance = row.Max;
                if (row.World) { v.Source.rolloffMode = AudioRolloffMode.Custom; v.Source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curves[index]); }
                v.Filter.cutoffFrequency = 22000; v.Source.volume = Gain(v, HasSpeech()); v.Source.Play();
                if (gated) GameEvents.Noise.Raise(noise);
                Caption(v); return new CueHandle(admission.Handle);
            }
            finally { if (gated) noiseGate.Exit(); }
        }
        public void SetIntensity(CueHandle handle, float intensity01)
        {
            int i = budget.Find(handle.Id); if (i < 0 || !NoiseGate.Finite(intensity01)) return;
            voices[i].Intensity = Mathf.Clamp01(intensity01);
            if (voices[i].Intensity == 0) voices[i].Source.volume = 0;
        }
        public void Stop(CueHandle handle, float fadeSeconds)
        {
            int i = budget.Find(handle.Id); if (i < 0) return;
            if (!NoiseGate.Finite(fadeSeconds) || fadeSeconds <= 0) Release(i);
            else voices[i].FadeRate = voices[i].Fade / fadeSeconds;
        }
        /// <summary>Director silence window after a hard scare, without changing player volume settings.</summary>
        public void DuckAmbience(float seconds) => silenceUntil = Mathf.Max(silenceUntil, Time.time + seconds);
        /// <summary>Director ending: fade all zombie loops once, leaving PA/UI protected.</summary>
        public void FadeZombieLoops(float seconds)
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i].Handle != 0 && (int)voices[i].Cue.Id >= 500 && (int)voices[i].Cue.Id < 600)
                    Stop(new CueHandle(voices[i].Handle), seconds);
        }
        private void Update()
        {
            if (!initialized) return;
            bool live = GameStateRules.IsSimulationLive(state), speech = HasSpeech();
            bool occlude = live && Time.time >= nextOcclusion;
            if (occlude) nextOcclusion = Time.time + .15f;
            Services<IPlayerQuery>.TryGet(out var player);
            for (int i = 0; i < voices.Length; i++)
            {
                var v = voices[i]; if (v.Handle == 0 || !live && v.Cue.Bus != MixerBus.UI) continue;
                if (!v.Source.isPlaying && !v.Cue.Loop) { Release(i); continue; }
                if (v.Follow) v.Source.transform.position = v.Follow.position;
                if (v.FadeRate > 0) { v.Fade = Mathf.Max(0, v.Fade - v.FadeRate * Time.deltaTime); if (v.Fade == 0) { Release(i); continue; } }
                if (occlude && player != null && v.Cue.World)
                {
                    bool blocked = Physics.Linecast(player.HeadPosition, v.Source.transform.position, Occluders, QueryTriggerInteraction.Ignore);
                    v.Occlusion = blocked ? .5f : 1; v.Filter.cutoffFrequency = blocked ? 1600 : 22000;
                }
                v.Source.volume = Gain(v, speech);
                if (v.Cue.Loop && v.Intensity > 0 && v.Cue.NoiseRadius > 0 && Time.time >= v.NextNoise)
                { v.NextNoise = Time.time + .2f; EmitLoopNoise(v); }
                if (v.Cue.Loop && v.Intensity > 0 && Time.time >= v.NextCaption)
                { v.NextCaption = Time.time + 4; Caption(v); }
            }
        }
        private bool HasSpeech()
        { for (int i = 0; i < voices.Length; i++) if (voices[i].Handle != 0 && voices[i].Cue.Bus == MixerBus.Voice && voices[i].Intensity > 0) return true; return false; }
        private float Gain(Voice v, bool speech)
        {
            float bus = v.Cue.Bus == MixerBus.Voice ? settings.VoiceVolume01 :
                v.Cue.Bus == MixerBus.World || v.Cue.Bus == MixerBus.Ambience ? settings.EffectsVolume01 : 1;
            if (speech && (v.Cue.Bus == MixerBus.World || v.Cue.Bus == MixerBus.Ambience)) bus *= .501187f;
            if (Time.time < silenceUntil && v.Cue.Bus == MixerBus.Ambience) bus *= .354813f;
            return settings.MasterVolume01 * bus * v.Intensity * v.Fade * v.Occlusion;
        }
        private void EmitLoopNoise(Voice v)
        {
            var noise = new NoiseEvent(v.Source.transform.position, v.Cue.NoiseRadius * v.Intensity, v.Cue.Id, v.Origin, Time.time);
            if (!noiseGate.TryEnter(in noise, v.Handle, Time.time)) return;
            try { GameEvents.Noise.Raise(noise); } finally { noiseGate.Exit(); }
        }
        private void Caption(Voice v)
        {
            if (!v.Cue.ShouldCaption(settings) || string.IsNullOrEmpty(v.Cue.Caption)) return;
            GameEvents.CaptionRequested.Raise(new CaptionRequested(v.Cue.Caption,
                v.Cue.CaptionPolicy == CaptionPolicy.Speech ? CaptionKind.Speech : CaptionKind.Sound,
                v.Cue.World, v.Source.transform.position, v.Cue.Bus == MixerBus.Voice ? 4 : 2));
        }
        private int Index(CueId cue) { for (int i = 0; i < CueTable.Count; i++) if (CueTable.At(i).Id == cue) return i; return -1; }
        private static int SourceKey(CueId cue, Vector3 p, NoiseOrigin origin) => unchecked((int)cue * 397 ^ (int)origin * 7919 ^
            Mathf.RoundToInt(p.x * 10) * 31 ^ Mathf.RoundToInt(p.y * 10) * 17 ^ Mathf.RoundToInt(p.z * 10));
        private void Release(int i) { var v = voices[i]; v.Source.Stop(); budget.Release(v.Handle); v.Handle = 0; v.Follow = null; }
        private void StopAll() { if (initialized) for (int i = 0; i < voices.Length; i++) if (voices[i].Handle != 0) Release(i); }
    }
}
