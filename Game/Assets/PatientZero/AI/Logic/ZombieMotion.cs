using System;
using PatientZero.Core;

namespace PatientZero.AI.Logic
{
    /// <summary>Presentation only. No perception, navigation, brain timers or engine calls.</summary>
    public sealed class ZombieMotion
    {
        public const float MaxYaw = 70, MaxPitch = 30, ListeningRoll = 20, RunLean = 15;
        public const float TwitchOutSeconds = .1f, TwitchBackSeconds = .3f;
        private readonly uint seed;
        private uint random;
        private float wait, twitchAge = -1, twitchSign, yaw, pitch, roll, lean;
        private bool shoulder;

        public ZombieMotion(int zombieId)
        {
            seed = unchecked((uint)zombieId * 747796405u + 2891336453u);
            Reset();
        }

        public void Reset()
        {
            random = seed == 0 ? 1u : seed;
            wait = NextWait(); twitchAge = -1;
            yaw = pitch = roll = lean = 0;
        }

        public static bool AllowsMotion(ZombieMode mode, bool stunned, bool dead, bool kneeling) =>
            !stunned && !dead && !kneeling && mode != ZombieMode.Restrained && mode != ZombieMode.Passive;

        public static bool LooksAtTarget(AlertLevel level) => level == AlertLevel.Suspicious ||
            level == AlertLevel.Investigating || level == AlertLevel.Searching || level == AlertLevel.Chasing;

        public static bool LooksAtPlayer(AlertLevel level) => level == AlertLevel.Chasing;

        public static bool IsListening(AlertLevel level, ZombieMode mode, bool stunned, bool dead, bool kneeling) =>
            level == AlertLevel.Suspicious && AllowsMotion(mode, stunned, dead, kneeling);

        public static bool AllowsTwitch(AlertLevel level, ZombieMode mode, float speed, float walkSpeed,
            bool stunned = false, bool dead = false, bool kneeling = false) =>
            AllowsMotion(mode, stunned, dead, kneeling) &&
            (level == AlertLevel.Unaware || level == AlertLevel.Searching) && Running01(speed, walkSpeed) == 0;

        public static float Running01(float speed, float walkSpeed) =>
            Finite(speed) && Finite(walkSpeed) && walkSpeed > 0 ? Clamp((speed - walkSpeed - .05f) / .2f, 0, 1) : 0;

        // Existing cue intensity API: a quieter walk, full-volume run. Generic rigs never opt in.
        public static float ShuffleIntensity(float speed, float walkSpeed) => .6f + .4f * Running01(speed, walkSpeed);

        /// <summary>Exponential easing composes across frame rates, cannot overshoot, and ignores bad time.</summary>
        public static float Ease(float current, float desired, float delta, float rate = 8)
        {
            if (!Finite(current)) current = 0;
            if (!Finite(desired) || !Finite(delta) || !Finite(rate) || delta <= 0 || rate <= 0) return current;
            return current + (desired - current) * (float)(1 - Math.Exp(-rate * delta));
        }

        public Pose Step(float delta, AlertLevel level, ZombieMode mode, float speed, float walkSpeed,
            float desiredYaw, float desiredPitch, bool stunned = false, bool dead = false, bool kneeling = false)
        {
            if (!Finite(delta) || delta < 0) delta = 0;
            if (!AllowsMotion(mode, stunned, dead, kneeling))
            {
                // Hit/death/kneel/restraint own the entire pose; never leave a procedural residual over them.
                yaw = pitch = roll = lean = 0; twitchAge = -1;
                return default;
            }
            bool listening = IsListening(level, mode, stunned, dead, kneeling);
            bool looking = LooksAtTarget(level);
            yaw = Ease(yaw, looking ? ClampAngle(desiredYaw, MaxYaw) : 0, delta);
            pitch = Ease(pitch, looking ? ClampAngle(desiredPitch, MaxPitch) : 0, delta);
            roll = Ease(roll, listening ? ListeningRoll : 0, delta, 14);
            lean = Ease(lean, listening ? 0 : RunLean * Running01(speed, walkSpeed), delta);

            float twitch = 0;
            if (!AllowsTwitch(level, mode, speed, walkSpeed)) twitchAge = -1;
            else if (delta > 0)
            {
                if (twitchAge < 0)
                {
                    wait -= delta;
                    if (wait <= 0)
                    {
                        twitchAge = 0; wait = NextWait();
                        shoulder = (Next() & 1) != 0; twitchSign = (Next() & 1) != 0 ? 1 : -1;
                    }
                }
                else
                {
                    twitchAge += delta;
                    if (twitchAge >= TwitchOutSeconds + TwitchBackSeconds) twitchAge = -1;
                }
            }
            if (twitchAge >= 0) twitch = TwitchEnvelope(twitchAge) * twitchSign;
            return new Pose(yaw, pitch, roll, lean, shoulder ? 0 : twitch * 7,
                shoulder ? twitch * 9 : 0, listening);
        }

        public static float TwitchEnvelope(float age)
        {
            if (!Finite(age) || age < 0 || age >= TwitchOutSeconds + TwitchBackSeconds) return 0;
            float t = age <= TwitchOutSeconds ? age / TwitchOutSeconds :
                1 - (age - TwitchOutSeconds) / TwitchBackSeconds;
            return t * t * (3 - 2 * t);
        }

        private uint Next() { random ^= random << 13; random ^= random >> 17; random ^= random << 5; return random; }
        private float NextWait() => 3 + Next() / (float)uint.MaxValue * 4;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        private static float ClampAngle(float value, float limit) => Finite(value) ? Clamp(value, -limit, limit) : 0;

        public readonly struct Pose
        {
            public readonly float Yaw, Pitch, Roll, Lean, HeadTwitch, ShoulderTwitch;
            public readonly bool Listening;
            public Pose(float yaw, float pitch, float roll, float lean, float headTwitch, float shoulderTwitch, bool listening)
            { Yaw = yaw; Pitch = pitch; Roll = roll; Lean = lean; HeadTwitch = headTwitch; ShoulderTwitch = shoulderTwitch; Listening = listening; }
        }
    }
}
