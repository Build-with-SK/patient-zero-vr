// Minimal stand-in for the parts of UnityEngine that PatientZero logic code may use.
// It exists so pure-logic code and its EditMode tests can be compiled and run without the
// Unity Editor (no agent on this project had Unity installed when it started).
//
// Deliberately MISSING: Time, Random, GameObject, Physics, Input, any rendering or audio type.
// Logic code that reaches for those will not compile here, which is the point: time,
// randomness and scene access are passed in by the MonoBehaviour shell.
//
// Behaviour copies Unity's documented semantics (Vector3 equality tolerance, Mathf.Sign(0) == 1,
// destroyed-object == null). If you need more API, ask the Technical Director to add it.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEngine
{
    public enum LogType
    {
        Error = 0,
        Assert = 1,
        Warning = 2,
        Log = 3,
        Exception = 4,
    }

    public static class Debug
    {
        public static void Log(object message) { LogSink.Receive(LogType.Log, Convert.ToString(message)); }
        public static void LogWarning(object message) { LogSink.Receive(LogType.Warning, Convert.ToString(message)); }
        public static void LogError(object message) { LogSink.Receive(LogType.Error, Convert.ToString(message)); }
        public static void LogException(Exception exception) { LogSink.Receive(LogType.Exception, exception == null ? "null" : exception.GetType().Name + ": " + exception.Message); }
        public static void Assert(bool condition) { if (!condition) LogSink.Receive(LogType.Assert, "Assertion failed"); }
        public static void Assert(bool condition, object message) { if (!condition) LogSink.Receive(LogType.Assert, Convert.ToString(message)); }
    }

    /// <summary>Harness-only: collects logs so the runner can apply Unity's LogAssert rules.</summary>
    public static class LogSink
    {
        internal sealed class Expectation
        {
            public LogType Type;
            public string Exact;
            public Regex Pattern;
        }

        internal static readonly List<Expectation> Expected = new List<Expectation>();
        public static readonly List<string> Unexpected = new List<string>();
        public static bool Echo;

        public static void BeginTest()
        {
            Expected.Clear();
            Unexpected.Clear();
        }

        /// <summary>Failures in Unity Test Framework terms: unexpected error logs and expected logs that never came.</summary>
        public static List<string> EndTest()
        {
            var failures = new List<string>(Unexpected);
            foreach (var e in Expected) failures.Add("Expected " + e.Type + " log did not appear: " + (e.Exact ?? e.Pattern.ToString()));
            Expected.Clear();
            Unexpected.Clear();
            return failures;
        }

        internal static void Receive(LogType type, string message)
        {
            if (Echo) Console.WriteLine("    [" + type + "] " + message);
            for (int i = 0; i < Expected.Count; i++)
            {
                var e = Expected[i];
                if (e.Type != type) continue;
                bool match = e.Exact != null ? e.Exact == message : e.Pattern.IsMatch(message);
                if (!match) continue;
                Expected.RemoveAt(i);
                return;
            }
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                Unexpected.Add("Unhandled log message: [" + type + "] " + message);
            }
        }
    }

    public struct Vector2 : IEquatable<Vector2>
    {
        public float x;
        public float y;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 one => new Vector2(1f, 1f);
        public float sqrMagnitude => x * x + y * y;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-5f * 1e-5f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

        public bool Equals(Vector2 other) => x.Equals(other.x) && y.Equals(other.y);
        public override bool Equals(object other) => other is Vector2 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override string ToString() => "(" + x.ToString("F2") + ", " + y.ToString("F2") + ")";
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public const float kEpsilon = 0.00001f;
        public const float kEpsilonNormalSqrt = 1e-15f;

        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0f; }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 up => new Vector3(0f, 1f, 0f);
        public static Vector3 down => new Vector3(0f, -1f, 0f);
        public static Vector3 forward => new Vector3(0f, 0f, 1f);
        public static Vector3 back => new Vector3(0f, 0f, -1f);
        public static Vector3 right => new Vector3(1f, 0f, 0f);
        public static Vector3 left => new Vector3(-1f, 0f, 0f);

        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized => Normalize(this);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < kEpsilon * kEpsilon;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

        public static Vector3 Normalize(Vector3 v)
        {
            float mag = v.magnitude;
            return mag > kEpsilon ? v / mag : zero;
        }

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3 Cross(Vector3 a, Vector3 b) =>
            new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            float sqrMag = Dot(planeNormal, planeNormal);
            if (sqrMag < Mathf.Epsilon) return vector;
            float dot = Dot(vector, planeNormal);
            return vector - planeNormal * (dot / sqrMag);
        }

        public static float Angle(Vector3 from, Vector3 to)
        {
            float denominator = (float)Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (denominator < kEpsilonNormalSqrt) return 0f;
            float dot = Mathf.Clamp(Dot(from, to) / denominator, -1f, 1f);
            return (float)Math.Acos(dot) * Mathf.Rad2Deg;
        }

        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsigned = Angle(from, to);
            Vector3 cross = Cross(from, to);
            return unsigned * Mathf.Sign(Dot(axis, cross));
        }

        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            Vector3 delta = target - current;
            float sqDist = delta.sqrMagnitude;
            if (sqDist == 0f || (maxDistanceDelta >= 0f && sqDist <= maxDistanceDelta * maxDistanceDelta)) return target;
            float dist = (float)Math.Sqrt(sqDist);
            return current + delta / dist * maxDistanceDelta;
        }

        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength)
        {
            float sqr = vector.sqrMagnitude;
            if (sqr > maxLength * maxLength) return vector / (float)Math.Sqrt(sqr) * maxLength;
            return vector;
        }

        public bool Equals(Vector3 other) => x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z);
        public override bool Equals(object other) => other is Vector3 v && Equals(v);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => "(" + x.ToString("F2") + ", " + y.ToString("F2") + ", " + z.ToString("F2") + ")";
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = PI / 180f;
        public const float Rad2Deg = 180f / PI;
        public const float Infinity = float.PositiveInfinity;
        public const float NegativeInfinity = float.NegativeInfinity;
        public static readonly float Epsilon = float.Epsilon;

        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float value) => a != b ? Clamp01((value - a) / (b - a)) : 0f;
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int value) => Math.Abs(value);
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Exp(float power) => (float)Math.Exp(power);
        public static float Sign(float f) => f >= 0f ? 1f : -1f;
        public static float Round(float f) => (float)Math.Round(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static float Floor(float f) => (float)Math.Floor(f);
        public static float Repeat(float t, float length) => Clamp(t - Floor(t / length) * length, 0f, length);

        public static bool Approximately(float a, float b) =>
            Math.Abs(b - a) < Math.Max(0.000001f * Math.Max(Math.Abs(a), Math.Abs(b)), Epsilon * 8f);

        public static float MoveTowards(float current, float target, float maxDelta) =>
            Math.Abs(target - current) <= maxDelta ? target : current + Sign(target - current) * maxDelta;

        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            return delta;
        }

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }
    }

    public class Object
    {
        private bool _destroyed;

        public string name;

        public static bool operator ==(Object x, Object y)
        {
            bool xNull = IsNullOrDestroyed(x);
            bool yNull = IsNullOrDestroyed(y);
            if (xNull && yNull) return true;
            if (xNull || yNull) return false;
            return ReferenceEquals(x, y);
        }

        public static bool operator !=(Object x, Object y) => !(x == y);

        public static implicit operator bool(Object exists) => !IsNullOrDestroyed(exists);

        public override bool Equals(object other)
        {
            if (other == null) return IsNullOrDestroyed(this);
            Object o = other as Object;
            return !ReferenceEquals(o, null) && this == o;
        }

        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

        public static void Destroy(Object obj)
        {
            if (!ReferenceEquals(obj, null)) obj._destroyed = true;
        }

        public static void DestroyImmediate(Object obj)
        {
            if (!ReferenceEquals(obj, null)) obj._destroyed = true;
        }

        private static bool IsNullOrDestroyed(Object o) => ReferenceEquals(o, null) || o._destroyed;
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject => (T)Activator.CreateInstance(typeof(T), true);
    }

    public class Component : Object
    {
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public class Transform : Component
    {
        public Vector3 position;
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad = 1,
        AfterAssembliesLoaded = 2,
        BeforeSplashScreen = 3,
        SubsystemRegistration = 4,
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { LoadType = loadType; }
        public RuntimeInitializeLoadType LoadType { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute
    {
        public MinAttribute(float min) { }
    }
}

namespace UnityEngine.TestTools
{
    public static class LogAssert
    {
        public static void Expect(LogType type, string message)
        {
            LogSink.Expected.Add(new LogSink.Expectation { Type = type, Exact = message });
        }

        public static void Expect(LogType type, Regex message)
        {
            LogSink.Expected.Add(new LogSink.Expectation { Type = type, Pattern = message });
        }
    }
}
