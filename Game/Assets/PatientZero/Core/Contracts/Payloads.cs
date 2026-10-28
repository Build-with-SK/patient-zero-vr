using UnityEngine;

namespace PatientZero.Core
{
    // Event payloads. All are small readonly structs so raising an event never allocates.
    // The writer of each event is listed on its channel in GameEvents.cs.

    public readonly struct GameStateChanged
    {
        public readonly GameState From;
        public readonly GameState To;
        public readonly FailReason Reason;

        public GameStateChanged(GameState from, GameState to, FailReason reason)
        {
            From = from;
            To = to;
            Reason = reason;
        }
    }

    public readonly struct GameStateRequest
    {
        public readonly GameState Requested;
        public readonly FailReason Reason;

        public GameStateRequest(GameState requested, FailReason reason)
        {
            Requested = requested;
            Reason = reason;
        }
    }

    public readonly struct ActStarted
    {
        public readonly ActId Act;
        /// <summary>True when the act restarts after the player was caught.</summary>
        public readonly bool IsRestore;
        /// <summary>1 on a fresh run, then +1 per restore of any act.</summary>
        public readonly int Attempt;

        public ActStarted(ActId act, bool isRestore, int attempt)
        {
            Act = act;
            IsRestore = isRestore;
            Attempt = attempt;
        }
    }

    public readonly struct ObjectiveProgress
    {
        public readonly ObjectiveId Objective;
        public readonly float Progress01;

        public ObjectiveProgress(ObjectiveId objective, float progress01)
        {
            Objective = objective;
            Progress01 = Mathf.Clamp01(progress01);
        }
    }

    public readonly struct ObjectiveCompleted
    {
        public readonly ObjectiveId Objective;

        public ObjectiveCompleted(ObjectiveId objective)
        {
            Objective = objective;
        }
    }

    public readonly struct AssistChanged
    {
        public readonly ObjectiveId Objective;
        public readonly AssistLevel Level;

        public AssistChanged(ObjectiveId objective, AssistLevel level)
        {
            Objective = objective;
            Level = level;
        }
    }

    /// <summary>
    /// A sound zombies can hear. Gameplay hearing uses RadiusMeters, not the audible
    /// volume, so it stays deterministic and testable.
    /// </summary>
    public readonly struct NoiseEvent
    {
        public readonly Vector3 Position;
        public readonly float RadiusMeters;
        public readonly CueId Source;
        public readonly NoiseOrigin Origin;
        public readonly float Time;

        public NoiseEvent(Vector3 position, float radiusMeters, CueId source, NoiseOrigin origin, float time)
        {
            Position = position;
            RadiusMeters = radiusMeters < 0f ? 0f : radiusMeters;
            Source = source;
            Origin = origin;
            Time = time;
        }
    }

    public readonly struct ZombieAlertChanged
    {
        public readonly int ZombieId;
        public readonly AlertLevel Level;
        public readonly Vector3 Position;

        public ZombieAlertChanged(int zombieId, AlertLevel level, Vector3 position)
        {
            ZombieId = zombieId;
            Level = level;
            Position = position;
        }
    }

    public readonly struct PlayerCaught
    {
        public readonly int ZombieId;
        public readonly Vector3 ZombiePosition;

        public PlayerCaught(int zombieId, Vector3 zombiePosition)
        {
            ZombieId = zombieId;
            ZombiePosition = zombiePosition;
        }
    }

    public readonly struct LightingChanged
    {
        public readonly LightingState State;
        public readonly float TransitionSeconds;

        public LightingChanged(LightingState state, float transitionSeconds)
        {
            State = state;
            TransitionSeconds = transitionSeconds;
        }
    }

    /// <summary>Progress of the Patient Zero reveal on the player's own hands (0 = clean, 1 = full).</summary>
    public readonly struct InfectionReveal
    {
        public readonly float Amount01;

        public InfectionReveal(float amount01)
        {
            Amount01 = Mathf.Clamp01(amount01);
        }
    }

    /// <summary>
    /// A caption for a sound or line. Text points at a string already held by the cue
    /// bank, so raising this does not allocate.
    /// </summary>
    public readonly struct CaptionRequested
    {
        public readonly string Text;
        public readonly CaptionKind Kind;
        public readonly bool IsPositional;
        public readonly Vector3 WorldPosition;
        public readonly float DurationSeconds;

        public CaptionRequested(string text, CaptionKind kind, bool isPositional, Vector3 worldPosition, float durationSeconds)
        {
            Text = text;
            Kind = kind;
            IsPositional = isPositional;
            WorldPosition = worldPosition;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct SettingsChanged
    {
        public readonly ExperienceSettings Settings;

        public SettingsChanged(ExperienceSettings settings)
        {
            Settings = settings;
        }
    }

    public readonly struct TrackingChanged
    {
        public readonly InputModality Modality;
        public readonly bool LeftTracked;
        public readonly bool RightTracked;

        public TrackingChanged(InputModality modality, bool leftTracked, bool rightTracked)
        {
            Modality = modality;
            LeftTracked = leftTracked;
            RightTracked = rightTracked;
        }
    }

    public readonly struct HapticRequested
    {
        public readonly HandSide Hand;
        public readonly float Amplitude01;
        public readonly float DurationSeconds;

        public HapticRequested(HandSide hand, float amplitude01, float durationSeconds)
        {
            Hand = hand;
            Amplitude01 = Mathf.Clamp01(amplitude01);
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds;
        }
    }

    /// <summary>Handle to a playing cue, used to change or stop loops. Default is invalid.</summary>
    public readonly struct CueHandle
    {
        public readonly int Id;

        public CueHandle(int id)
        {
            Id = id;
        }

        public bool IsValid => Id != 0;
    }
}
