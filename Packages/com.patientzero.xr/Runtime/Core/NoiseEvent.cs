using System;

namespace PatientZero.Core
{
    public enum NoiseCategory { Bottle, Door, Footstep, Collision, Voice, Environment }

    [Serializable]
    public readonly struct WorldPoint
    {
        public readonly float X, Y, Z;
        public WorldPoint(float x, float y, float z) { X = x; Y = y; Z = z; }
        public bool IsFinite => Finite(X) && Finite(Y) && Finite(Z);
        public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static float Distance(WorldPoint a, WorldPoint b)
        {
            float x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
        public static float GroundDistance(WorldPoint a, WorldPoint b)
        {
            float x = a.X - b.X, z = a.Z - b.Z;
            return (float)Math.Sqrt(x * x + z * z);
        }
    }

    // World metres, normalized intensity [0,1], seconds on the monotonic game clock.
    public readonly struct NoiseEvent
    {
        public readonly WorldPoint Position;
        public readonly float Intensity, Radius;
        public readonly double Timestamp;
        public readonly NoiseCategory Category;
        public readonly int SourceId;
        public NoiseEvent(WorldPoint position, float intensity, float radius, double timestamp,
            NoiseCategory category, int sourceId)
        {
            Position = position; Intensity = intensity; Radius = radius;
            Timestamp = timestamp; Category = category; SourceId = sourceId;
        }
        public bool IsValid => Position.IsFinite && WorldPoint.Finite(Intensity) && Intensity > 0 &&
            Intensity <= 1 && WorldPoint.Finite(Radius) && Radius > 0 &&
            !double.IsNaN(Timestamp) && !double.IsInfinity(Timestamp) && Timestamp >= 0 &&
            Category >= NoiseCategory.Bottle && Category <= NoiseCategory.Environment;

        public float LevelAt(WorldPoint listener)
        {
            if (!IsValid || !listener.IsFinite) return 0;
            float distance = WorldPoint.Distance(Position, listener);
            return distance >= Radius ? 0 : Intensity * (1 - distance / Radius);
        }
    }

    public interface INoiseListener { void Hear(in NoiseEvent noise); }
}
