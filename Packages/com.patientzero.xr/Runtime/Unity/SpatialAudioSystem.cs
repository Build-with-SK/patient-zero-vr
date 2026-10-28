using PatientZero.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Audio;

namespace PatientZero.XR
{
    public sealed class SpatialAudioSystem : MonoBehaviour
    {
        public AudioCueBank Bank;
        public Transform Listener;
        public AudioMixer Mixer;
        [Tooltip("Enable after choosing a Quest-compatible HRTF spatializer in Project Settings > Audio.")]
        public bool UseSpatializer;
        public LayerMask Occluders;
        [Range(0, 1)] public float MasterVolume = .8f;
        [Range(0, 1)] public float ComfortVolume = .65f;
        [Range(1, 32)] public int VoiceCount = 12;
        [Min(.05f)] public float OcclusionInterval = .15f;
        [Range(0, 1)] public float OccludedGain = .4f;
        private Voice[] voices;
        private readonly double[] nextCue = new double[6];
        private double nextOcclusion;
        private bool warnedAssets;
        private static readonly ProfilerMarker OcclusionMarker = new ProfilerMarker("PatientZero.Audio.Occlusion");
        private sealed class Voice
        {
            public AudioSource Source;
            public AudioLowPassFilter Filter;
            public float Gain, Occlusion = 1;
            public bool Sensitive;
        }
        private void Awake()
        {
            voices = new Voice[Mathf.Clamp(VoiceCount, 1, 32)];
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject("PooledSpatialVoice" + i);
                child.transform.SetParent(transform, false);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 1; source.dopplerLevel = 0;
                source.spatialize = UseSpatializer;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                var filter = child.AddComponent<AudioLowPassFilter>(); filter.cutoffFrequency = 22000;
                voices[i] = new Voice { Source = source, Filter = filter };
            }
        }
        // Pool exhaustion drops the new sound rather than allocating or stealing important cues.
        public bool Play(AudioCue cue, Vector3 position)
        {
            int index = (int)cue;
            if (index < 0 || index >= nextCue.Length || !NoiseManager.ToPoint(position).IsFinite) return false;
            var entry = Bank ? Bank.Find(cue) : null;
            if (entry == null || !entry.Clip)
            {
                if (!warnedAssets) { Debug.LogWarning("Patient Zero audio assets missing; cue skipped safely.", this); warnedAssets = true; }
                return false;
            }
            float comfort = entry.ComfortSensitive ? ComfortVolume : 1;
            if (!AudioPolicy.CanPlay(true, MasterVolume, comfort) || Time.unscaledTimeAsDouble < nextCue[index]) return false;
            for (int i = 0; i < voices.Length; i++)
            {
                var voice = voices[i];
                if (voice.Source.isPlaying) continue;
                voice.Gain = Mathf.Clamp01(entry.Volume); voice.Sensitive = entry.ComfortSensitive;
                voice.Source.transform.position = position; voice.Source.clip = entry.Clip;
                voice.Source.minDistance = Mathf.Max(.1f, entry.MinDistance);
                voice.Source.maxDistance = Mathf.Max(voice.Source.minDistance + .1f, entry.MaxDistance);
                voice.Source.outputAudioMixerGroup = entry.Group;
                ApplyOcclusion(voice); ApplyVolume(voice);
                voice.Source.Play(); nextCue[index] = Time.unscaledTimeAsDouble + Mathf.Max(.01f, entry.Cooldown);
                return true;
            }
            return false;
        }
        public void SetMasterVolume(float value) { MasterVolume = Mathf.Clamp01(value); RefreshVolumes(); }
        public void SetComfortVolume(float value) { ComfortVolume = Mathf.Clamp01(value); RefreshVolumes(); }
        private void RefreshVolumes()
        {
            if (voices == null) return;
            for (int i = 0; i < voices.Length; i++) ApplyVolume(voices[i]);
        }
        private void ApplyVolume(Voice voice)
        {
            // Master applies once through the source; mixer routing remains available for project settings.
            voice.Source.volume = Mathf.Clamp01(MasterVolume) * voice.Gain * voice.Occlusion *
                (voice.Sensitive ? Mathf.Clamp01(ComfortVolume) : 1);
        }
        private void ApplyOcclusion(Voice voice)
        {
            bool blocked = Listener && Physics.Linecast(Listener.position, voice.Source.transform.position, Occluders, QueryTriggerInteraction.Ignore);
            voice.Occlusion = blocked ? OccludedGain : 1;
            voice.Filter.cutoffFrequency = blocked ? 1600 : 22000;
        }
        private void Update()
        {
            if (Time.unscaledTimeAsDouble < nextOcclusion) return;
            nextOcclusion = Time.unscaledTimeAsDouble + Mathf.Max(.05f, OcclusionInterval);
            using (OcclusionMarker.Auto())
                for (int i = 0; i < voices.Length; i++)
                    if (voices[i].Source.isPlaying) { ApplyOcclusion(voices[i]); ApplyVolume(voices[i]); }
        }
        public void StopAll()
        {
            if (voices == null) return;
            for (int i = 0; i < voices.Length; i++) voices[i].Source.Stop();
        }
        private void OnDisable() => StopAll();
    }
}
