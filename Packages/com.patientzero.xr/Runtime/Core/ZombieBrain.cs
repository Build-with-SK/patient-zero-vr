using System;

namespace PatientZero.Core
{
    public enum ZombieState { Idle, Investigate, Search, Chase, Attack, Recover }

    [Serializable]
    public sealed class ZombieSettings
    {
        public float HearingThreshold = .12f;
        public float ChaseThreshold = .6f;
        public float ArrivalDistance = .6f;
        public float AttackDistance = 1.3f;
        public float InvestigateSeconds = 7;
        public float SearchSeconds = 4;
        public float HearingMemorySeconds = 2;
        public float AttackWindupSeconds = .7f;
        public float RecoverSeconds = 1.8f;
        public bool IsValid => Positive(HearingThreshold) && HearingThreshold <= 1 &&
            Positive(ChaseThreshold) && ChaseThreshold >= HearingThreshold && ChaseThreshold <= 1 &&
            Positive(ArrivalDistance) && Positive(AttackDistance) && Positive(InvestigateSeconds) &&
            Positive(SearchSeconds) && Positive(HearingMemorySeconds) && Positive(AttackWindupSeconds) && Positive(RecoverSeconds);
        private static bool Positive(float x) => WorldPoint.Finite(x) && x > 0;
    }

    // Pure deterministic FSM. Never reads the player's live position or a visual target.
    public sealed class ZombieBrain
    {
        private readonly ZombieSettings settings;
        private double enteredAt, lastHeardAt = double.NegativeInfinity, clock;
        private float lastLevel;
        private NoiseCategory lastCategory;
        private bool hasTarget;
        public ZombieState State { get; private set; }
        public WorldPoint Target { get; private set; }
        public int TargetSource { get; private set; }
        public int Transitions { get; private set; }
        public ZombieBrain(ZombieSettings settings, double now = 0)
        {
            if (settings == null || !settings.IsValid || double.IsNaN(now) || double.IsInfinity(now) || now < 0)
                throw new ArgumentException("Invalid zombie settings or clock");
            this.settings = settings; enteredAt = clock = now;
        }
        public bool Hear(in NoiseEvent noise, WorldPoint position, double now)
        {
            if (!ValidTime(now) || !noise.IsValid || noise.Timestamp > now ||
                now - noise.Timestamp > settings.HearingMemorySeconds || noise.Timestamp < lastHeardAt) return false;
            float level = noise.LevelAt(position);
            if (level < settings.HearingThreshold) return false;
            clock = now;
            Target = noise.Position; TargetSource = noise.SourceId; hasTarget = true;
            lastHeardAt = noise.Timestamp; lastLevel = level; lastCategory = noise.Category;
            // An attack/recovery cannot be restarted by a burst of sounds.
            if (State != ZombieState.Attack && State != ZombieState.Recover)
            {
                if (State == ZombieState.Idle || State == ZombieState.Search) SetState(ZombieState.Investigate, now);
                else if (State == ZombieState.Chase && !CanHunt) SetState(ZombieState.Investigate, now);
            }
            return true;
        }
        private bool CanHunt => lastLevel >= settings.ChaseThreshold &&
            (lastCategory == NoiseCategory.Footstep || lastCategory == NoiseCategory.Voice);
        // Returns true exactly once per completed windup; the Unity adapter validates physical damage range.
        public bool Tick(double now, WorldPoint position, bool pathFailed = false)
        {
            if (!ValidTime(now) || !position.IsFinite) return false;
            clock = now;
            bool fresh = hasTarget && now - lastHeardAt <= settings.HearingMemorySeconds;
            // Navigation/attack use ground distance: a voice at head height must be reachable.
            float distance = hasTarget ? WorldPoint.GroundDistance(position, Target) : float.MaxValue;
            double elapsed = now - enteredAt;
            switch (State)
            {
                case ZombieState.Idle: break;
                case ZombieState.Investigate:
                    if (pathFailed || elapsed >= settings.InvestigateSeconds) SetState(ZombieState.Search, now);
                    else if (fresh && CanHunt) SetState(ZombieState.Chase, now);
                    else if (distance <= settings.ArrivalDistance) SetState(ZombieState.Search, now);
                    break;
                case ZombieState.Search:
                    if (elapsed >= settings.SearchSeconds) { hasTarget = false; SetState(ZombieState.Idle, now); }
                    break;
                case ZombieState.Chase:
                    if (pathFailed || !fresh || !CanHunt) SetState(ZombieState.Investigate, now);
                    else if (distance <= settings.AttackDistance) SetState(ZombieState.Attack, now);
                    break;
                case ZombieState.Attack:
                    if (elapsed >= settings.AttackWindupSeconds)
                    {
                        SetState(ZombieState.Recover, now);
                        return fresh && CanHunt && distance <= settings.AttackDistance;
                    }
                    break;
                case ZombieState.Recover:
                    if (elapsed >= settings.RecoverSeconds) SetState(fresh ? ZombieState.Investigate : ZombieState.Search, now);
                    break;
            }
            return false;
        }
        private bool ValidTime(double now) => !double.IsNaN(now) && !double.IsInfinity(now) && now >= clock;
        private void SetState(ZombieState state, double now)
        {
            if (State == state) return;
            State = state; enteredAt = now; Transitions++;
        }
    }
}
