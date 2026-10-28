using System;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace PatientZero.XR
{
    [CreateAssetMenu(menuName = "Patient Zero/Audio Cue Bank")]
    public sealed class AudioCueBank : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public AudioCue Cue;
            public AudioClip Clip;
            public AudioMixerGroup Group;
            [Range(0, 1)] public float Volume = .65f;
            [Min(.1f)] public float MinDistance = 1;
            [Min(1)] public float MaxDistance = 25;
            [Min(.01f)] public float Cooldown = .15f;
            public bool ComfortSensitive = true;
        }
        public Entry[] Entries = new Entry[0];
        public Entry Find(AudioCue cue)
        {
            for (int i = 0; i < Entries.Length; i++) if (Entries[i] != null && Entries[i].Cue == cue) return Entries[i];
            return null;
        }
        public bool HasAllAssets(out AudioCue firstMissing)
        {
            for (int i = 0; i <= (int)AudioCue.Growl; i++)
            {
                var cue = (AudioCue)i; var entry = Find(cue);
                if (entry == null || !entry.Clip) { firstMissing = cue; return false; }
            }
            firstMissing = default; return true;
        }
    }
}
