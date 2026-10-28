using UnityEngine;

namespace PatientZero.Core
{
    /// <summary>
    /// Shorthand for IAudioCuePlayer. If no audio service is registered the call is a no-op,
    /// and that is logged once: a missing cue player also means zombies hear nothing.
    /// </summary>
    public static class Cues
    {
        private static bool _warnedMissing;

        public static CueHandle Play(CueId cue, Vector3 position, NoiseOrigin origin, float intensity01 = 1f)
        {
            IAudioCuePlayer player;
            if (Services<IAudioCuePlayer>.TryGet(out player)) return player.Play(cue, position, intensity01, origin);
            WarnMissing(cue);
            return default(CueHandle);
        }

        public static CueHandle PlayAttached(CueId cue, Transform follow, NoiseOrigin origin, float intensity01 = 1f)
        {
            IAudioCuePlayer player;
            if (Services<IAudioCuePlayer>.TryGet(out player)) return player.PlayAttached(cue, follow, intensity01, origin);
            WarnMissing(cue);
            return default(CueHandle);
        }

        public static void SetIntensity(CueHandle handle, float intensity01)
        {
            IAudioCuePlayer player;
            if (handle.IsValid && Services<IAudioCuePlayer>.TryGet(out player)) player.SetIntensity(handle, intensity01);
        }

        public static void Stop(CueHandle handle, float fadeSeconds = 0.1f)
        {
            IAudioCuePlayer player;
            if (handle.IsValid && Services<IAudioCuePlayer>.TryGet(out player)) player.Stop(handle, fadeSeconds);
        }

        internal static void ResetWarning()
        {
            _warnedMissing = false;
        }

        private static void WarnMissing(CueId cue)
        {
            if (_warnedMissing) return;
            _warnedMissing = true;
            Debug.LogError("[PatientZero] No IAudioCuePlayer registered; cue " + cue + " dropped and no noise emitted.");
        }
    }
}
