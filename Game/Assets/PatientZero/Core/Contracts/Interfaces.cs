using UnityEngine;

namespace PatientZero.Core
{
    // Cross-team services. Each interface has exactly one implementing team.
    // Look them up with Services<T>.TryGet; never with FindObjectOfType.

    /// <summary>
    /// Implemented by Codex B (PatientZero.Audio). The ONLY way any code plays a world sound.
    /// The cue bank decides clip, attenuation, mixer group, caption and noise radius, so
    /// every audible gameplay sound is also a noise zombies can hear (unless its radius is 0).
    /// Codex A never creates an AudioSource.
    /// </summary>
    public interface IAudioCuePlayer
    {
        /// <summary>One-shot at a fixed point. Intensity scales volume and noise radius.</summary>
        CueHandle Play(CueId cue, Vector3 position, float intensity01, NoiseOrigin origin);

        /// <summary>Loop or one-shot that follows a transform until stopped or finished.</summary>
        CueHandle PlayAttached(CueId cue, Transform follow, float intensity01, NoiseOrigin origin);

        /// <summary>For loops such as the valve squeal: volume and noise follow turn speed.</summary>
        void SetIntensity(CueHandle handle, float intensity01);

        void Stop(CueHandle handle, float fadeSeconds);
    }

    /// <summary>Implemented by Codex A (PatientZero.Player). What AI and the director may know about the player.</summary>
    public interface IPlayerQuery
    {
        Vector3 HeadPosition { get; }
        Vector3 HeadForward { get; }
        /// <summary>Head position projected to the floor.</summary>
        Vector3 FloorPosition { get; }
        InputModality Modality { get; }
        bool TryGetHandPosition(HandSide hand, out Vector3 position);

        /// <summary>Smoothed head speed in m/s. Zombie sight reacts to movement, not presence.</summary>
        float HeadSpeed { get; }

        /// <summary>Smoothed speed of the faster tracked hand or controller in m/s; 0 when neither is tracked.</summary>
        float FastestHandSpeed { get; }
    }

    /// <summary>Implemented by Codex A (PatientZero.Flow). Changes are broadcast on GameEvents.SettingsChanged.</summary>
    public interface ISettingsProvider
    {
        ExperienceSettings Current { get; }
    }

    /// <summary>Implemented by Codex A (PatientZero.Player). Full-view fade used for teleports, failure and the ending.</summary>
    public interface IScreenFader
    {
        bool IsFading { get; }
        /// <summary>0 = clear, 1 = black. Uses unscaled time so it works while paused.</summary>
        void FadeTo(float alpha01, float seconds);
    }

    /// <summary>
    /// Implemented by Codex B (PatientZero.Atmosphere). GameFlow (A) calls StartRun after the
    /// gameplay scene has loaded; the director then raises GameEvents.ActStarted.
    /// </summary>
    public interface IExperienceDirector
    {
        ActId CurrentAct { get; }
        /// <summary>Scaled seconds since the run started, across restores.</summary>
        float ExperienceTime { get; }
        void StartRun(ActId startAct, bool isRestore);
        /// <summary>
        /// True once the gate has been released in the current act (GameEvents.GateReleased).
        /// False for GateId.None, for a gate of another act, and after the act restarts until
        /// the beat releases it again (contract v1.1).
        /// </summary>
        bool IsGateReleased(GateId gate);
    }
}
