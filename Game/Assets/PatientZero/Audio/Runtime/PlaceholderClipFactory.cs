// UNVERIFIED IN EDITOR
using System;
using PatientZero.Audio.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Audio
{
    /// <summary>Load-time original mono 48 kHz synthesis. Port of generate_audio()@72be6d8; no external assets.</summary>
    public static class PlaceholderClipFactory
    {
        public static AudioClip Create(CueDefinition row)
        {
            if (row.Id == CueId.None) return null;
            float seconds = row.Id == CueId.HeartbeatWake || row.Id == CueId.RevealDrone ? 6 :
                row.Id == CueId.CaughtHeartbeat ? 2.5f : row.Bus == MixerBus.UI ? .15f : row.Bus == MixerBus.Voice ? 4 : row.Loop ? 2 : .8f;
            const int rate = 48000;
            var samples = new float[(int)(rate * seconds)];
            var random = new System.Random((int)row.Id + 207);
            double frequency = row.Id == CueId.BeaconChirp ? 880 : row.Id == CueId.AlarmKlaxon ? 660 :
                (int)row.Id >= 500 && (int)row.Id < 600 ? 70 : row.Bus == MixerBus.Music ? 55 : 120;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)rate;
                double envelope = Math.Min(1, Math.Min(t / .02, (seconds - t) / .04));
                if (row.Id == CueId.HeartbeatWake) envelope *= Math.Max(0, 1 - t / 6);
                double noise = random.NextDouble() * 2 - 1;
                double tone = Math.Sin(t * frequency * Math.PI * 2);
                double pulse = row.Id == CueId.ZombieShuffle ? Math.Pow(Math.Sin(t * Math.PI * 2), 8) : 1;
                // Speech placeholders are filtered-sounding bursts; captions carry the actual approved words.
                samples[i] = (float)((tone * .55 + noise * .45) * envelope * pulse * .1);
            }
            var clip = AudioClip.Create("Placeholder_" + row.Id, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
    }
}
