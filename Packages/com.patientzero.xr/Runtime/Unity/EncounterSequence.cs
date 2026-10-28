using System;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.Events;

namespace PatientZero.XR
{
    public enum EncounterKind { BehindGlass, ThrownDistraction, OperatingRoom, DarkCorridor, InfectedReveal }
    [Serializable]
    public sealed class EncounterBeat
    {
        [Min(0)] public float Delay;
        public AudioCue Cue;
        public Transform AudioAnchor;
        public bool PlayAudio = true;
        public UnityEvent OnBeat = new UnityEvent();
    }
    public sealed class EncounterSequence : MonoBehaviour
    {
        public EncounterKind Kind;
        public Transform Listener;
        public SpatialAudioSystem Audio;
        [Min(2.5f)] public float MinimumDistance = 2.5f;
        public bool AllowIntenseEncounters;
        public bool OneShot = true;
        public EncounterBeat[] Beats = new EncounterBeat[0];
        public UnityEvent OnComplete = new UnityEvent();
        private int beat;
        private double nextBeat;
        private bool running, consumed;
        public bool TryBegin()
        {
            if (running || (OneShot && consumed) || !Listener || Beats.Length == 0 ||
                (Kind == EncounterKind.InfectedReveal && !AllowIntenseEncounters)) return false;
            // Each authored anchor must be comfortably distant, including behind-player audio anchors.
            for (int i = 0; i < Beats.Length; i++)
            {
                var anchor = Beats[i].AudioAnchor ? Beats[i].AudioAnchor : transform;
                if (!AudioPolicy.SafeEncounter(NoiseManager.ToPoint(Listener.position), NoiseManager.ToPoint(anchor.position), Mathf.Max(2.5f, MinimumDistance))) return false;
            }
            beat = 0; running = true; consumed = true;
            nextBeat = Time.timeAsDouble + Mathf.Max(0, Beats[0].Delay);
            return true;
        }
        public void Begin() => TryBegin();
        public void Cancel() => running = false;
        public void ResetSequence() { running = false; consumed = false; }
        private void OnDisable() => Cancel();
        private void Update()
        {
            if (!running || Time.timeAsDouble < nextBeat) return;
            if (!Listener) { Cancel(); return; }
            var item = Beats[beat]; var anchor = item.AudioAnchor ? item.AudioAnchor : transform;
            // Player movement can make a previously safe cue unsafe: cancel rather than force it.
            if (!AudioPolicy.SafeEncounter(NoiseManager.ToPoint(Listener.position), NoiseManager.ToPoint(anchor.position), Mathf.Max(2.5f, MinimumDistance))) { Cancel(); return; }
            if (item.PlayAudio && Audio) Audio.Play(item.Cue, anchor.position);
            item.OnBeat.Invoke();
            // Callback may have canceled or disabled this component.
            if (!running || !isActiveAndEnabled) return;
            beat++;
            if (beat == Beats.Length) { running = false; OnComplete.Invoke(); }
            else nextBeat = Time.timeAsDouble + Mathf.Max(0, Beats[beat].Delay);
        }
    }
}
