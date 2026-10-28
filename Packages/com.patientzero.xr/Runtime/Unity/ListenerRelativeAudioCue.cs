using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    // Snapshot the player's orientation at cue time. No camera transform changes or chasing audio.
    public sealed class ListenerRelativeAudioCue : MonoBehaviour
    {
        public SpatialAudioSystem Audio;
        public Transform Listener;
        public AudioCue Cue = AudioCue.Breathing;
        [Min(2.5f)] public float BehindDistance = 3;
        public void PlayBehind()
        {
            if (!Audio || !Listener) return;
            Vector3 forward = Vector3.ProjectOnPlane(Listener.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Audio.Play(Cue, Listener.position - forward * Mathf.Max(2.5f, BehindDistance));
        }
    }
}
