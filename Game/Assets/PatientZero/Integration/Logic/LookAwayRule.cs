// Claude-owned integration (owner's step 1 feedback, 2026-11-11; E-23).
namespace PatientZero.Integration.Logic
{
    /// <summary>What a <see cref="LookAwayRule"/> tick changed.</summary>
    public enum LookAwayEvent { None = 0, Revealed = 1, Spotted = 2 }

    /// <summary>
    /// "It was not there a moment ago." The owner asked for a place that is empty when you look at it and has a dead body
    /// hanging in it when you look back. The spot stays empty until the player has looked at it (unless <see cref="SeenFirst"/>
    /// is off) and then had it out of sight for <see cref="AwaySeconds"/>; then the body is there (<see cref="LookAwayEvent.Revealed"/>),
    /// and the next time it is in view it is spotted. Engine-free: the caller measures the angle between the view and the spot,
    /// the distance, and whether a wall is in the way.
    /// </summary>
    public sealed class LookAwayRule
    {
        /// <summary>Within this many degrees of the centre of the view the spot is seen (inside a 90 degree flat view and a headset's).</summary>
        public const float InViewDegrees = 40;
        /// <summary>Beyond this many degrees the spot is surely out of sight, on a flat screen or in a headset.</summary>
        public const float OutOfViewDegrees = 65;
        /// <summary>How long the empty spot must be looked at before it counts as seen.</summary>
        public const float SeenSeconds = .4f;
        /// <summary>How long it must be out of sight before the body is there.</summary>
        public const float AwaySeconds = 1.5f;
        /// <summary>Further away than this the player is not near enough for the trick: nothing changes.</summary>
        public const float MaxMeters = 12;

        public enum Phase { Empty = 0, SeenEmpty = 1, Revealed = 2, Spotted = 3 }

        private float seen, away;

        public LookAwayRule(bool seenFirst = true)
        {
            SeenFirst = seenFirst;
            Reset();
        }

        /// <summary>When true the player must see the spot empty before the body can appear.</summary>
        public bool SeenFirst { get; }
        public Phase State { get; private set; }
        public bool BodyShown => State == Phase.Revealed || State == Phase.Spotted;

        /// <summary>Empty again: a new life starts from the bed.</summary>
        public void Reset()
        {
            State = SeenFirst ? Phase.Empty : Phase.SeenEmpty;
            seen = away = 0;
        }

        public static bool InView(float degrees, float meters, bool clear) => clear && meters <= MaxMeters && degrees <= InViewDegrees;

        public static bool OutOfView(float degrees, float meters, bool clear) => meters <= MaxMeters && (!clear || degrees >= OutOfViewDegrees);

        /// <param name="seconds">Time since the last tick.</param>
        /// <param name="degrees">Angle between the direction of view and the direction to the spot.</param>
        /// <param name="meters">Distance from the eyes to the spot.</param>
        /// <param name="clear">False when a wall or a door is between the eyes and the spot.</param>
        public LookAwayEvent Tick(float seconds, float degrees, float meters, bool clear)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds) || float.IsNaN(degrees) || float.IsNaN(meters)) return LookAwayEvent.None;
            switch (State)
            {
                case Phase.Empty:
                    seen = InView(degrees, meters, clear) ? seen + seconds : 0;
                    if (seen >= SeenSeconds) State = Phase.SeenEmpty;
                    return LookAwayEvent.None;
                case Phase.SeenEmpty:
                    away = OutOfView(degrees, meters, clear) ? away + seconds : 0;
                    if (away < AwaySeconds) return LookAwayEvent.None;
                    State = Phase.Revealed;
                    return LookAwayEvent.Revealed;
                case Phase.Revealed:
                    if (!InView(degrees, meters, clear)) return LookAwayEvent.None;
                    State = Phase.Spotted;
                    return LookAwayEvent.Spotted;
                default:
                    return LookAwayEvent.None;
            }
        }
    }
}
