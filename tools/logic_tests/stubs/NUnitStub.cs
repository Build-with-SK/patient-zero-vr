// Minimal NUnit 3 surface for the logic-test harness. Classic asserts only:
// write tests with Assert.AreEqual / IsTrue / Throws etc., not Assert.That(...), so the
// same file runs here and in Unity's Test Runner.

using System;
using System.Collections;

namespace NUnit.Framework
{
    public delegate void TestDelegate();

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestFixtureAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestCaseAttribute : Attribute
    {
        public TestCaseAttribute(params object[] arguments) { Arguments = arguments ?? new object[] { null }; }
        public object[] Arguments { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TearDownAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OneTimeSetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OneTimeTearDownAttribute : Attribute
    {
    }

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    public static class Assert
    {
        public static void Fail(string message = null) => throw new AssertionException(message ?? "Assert.Fail");

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition) Fail(Prefix(message) + "Expected: True  But was: False");
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition) Fail(Prefix(message) + "Expected: False  But was: True");
        }

        public static void IsNull(object value, string message = null)
        {
            if (value != null) Fail(Prefix(message) + "Expected: null  But was: " + value);
        }

        public static void IsNotNull(object value, string message = null)
        {
            if (value == null) Fail(Prefix(message) + "Expected: not null");
        }

        public static void AreSame(object expected, object actual, string message = null)
        {
            if (!ReferenceEquals(expected, actual)) Fail(Prefix(message) + "Expected the same instance");
        }

        public static void AreEqual(object expected, object actual, string message = null)
        {
            if (!ValuesEqual(expected, actual)) Fail(Prefix(message) + "Expected: " + Show(expected) + "  But was: " + Show(actual));
        }

        public static void AreNotEqual(object expected, object actual, string message = null)
        {
            if (ValuesEqual(expected, actual)) Fail(Prefix(message) + "Expected not: " + Show(expected));
        }

        public static void AreEqual(double expected, double actual, double delta, string message = null)
        {
            if (double.IsNaN(expected) && double.IsNaN(actual)) return;
            if (Math.Abs(expected - actual) > delta) Fail(Prefix(message) + "Expected: " + expected + " +/- " + delta + "  But was: " + actual);
        }

        public static void Greater(double a, double b, string message = null)
        {
            if (!(a > b)) Fail(Prefix(message) + "Expected " + a + " > " + b);
        }

        public static void GreaterOrEqual(double a, double b, string message = null)
        {
            if (!(a >= b)) Fail(Prefix(message) + "Expected " + a + " >= " + b);
        }

        public static void Less(double a, double b, string message = null)
        {
            if (!(a < b)) Fail(Prefix(message) + "Expected " + a + " < " + b);
        }

        public static void LessOrEqual(double a, double b, string message = null)
        {
            if (!(a <= b)) Fail(Prefix(message) + "Expected " + a + " <= " + b);
        }

        public static T Throws<T>(TestDelegate code, string message = null) where T : Exception
        {
            try
            {
                code();
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(T)) return (T)e;
                Fail(Prefix(message) + "Expected " + typeof(T).Name + " but got " + e.GetType().Name + ": " + e.Message);
            }
            Fail(Prefix(message) + "Expected " + typeof(T).Name + " but nothing was thrown");
            return null;
        }

        public static void DoesNotThrow(TestDelegate code, string message = null)
        {
            try
            {
                code();
            }
            catch (Exception e)
            {
                Fail(Prefix(message) + "Unexpected " + e.GetType().Name + ": " + e.Message);
            }
        }

        public static void Contains(object expected, ICollection actual, string message = null)
        {
            foreach (var item in actual)
            {
                if (ValuesEqual(expected, item)) return;
            }
            Fail(Prefix(message) + "Collection does not contain " + Show(expected));
        }

        private static bool ValuesEqual(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (IsNumeric(a) && IsNumeric(b)) return Convert.ToDouble(a).Equals(Convert.ToDouble(b));
            return a.Equals(b);
        }

        private static bool IsNumeric(object o) =>
            o is sbyte || o is byte || o is short || o is ushort || o is int || o is uint ||
            o is long || o is ulong || o is float || o is double || o is decimal;

        private static string Show(object o) => o == null ? "null" : o is string ? "\"" + o + "\"" : o.ToString();

        private static string Prefix(string message) => string.IsNullOrEmpty(message) ? "" : message + "\n  ";
    }
}
