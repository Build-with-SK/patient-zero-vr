// Test runner for the logic harness. Finds [Test] / [TestCase] methods by reflection,
// runs SetUp / TearDown around each, and applies Unity's log rules: an unexpected
// Error/Exception/Assert log fails the test, and so does a LogAssert.Expect that never happens.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace PatientZero.LogicHarness
{
    public static class Runner
    {
        public static int Main(string[] args)
        {
            string filter = args.Length > 0 ? args[0] : null;
            LogSink.Echo = args.Contains("--verbose");
            int passed = 0;
            var failures = new List<string>();

            var fixtures = typeof(Runner).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetMethods().Any(IsTest))
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

            foreach (var fixture in fixtures)
            {
                var setUps = fixture.GetMethods().Where(m => Has<SetUpAttribute>(m)).ToList();
                var tearDowns = fixture.GetMethods().Where(m => Has<TearDownAttribute>(m)).ToList();

                foreach (var method in fixture.GetMethods().Where(IsTest).OrderBy(m => m.Name, StringComparer.Ordinal))
                {
                    var cases = method.GetCustomAttributes(typeof(TestCaseAttribute), false).Cast<TestCaseAttribute>().ToList();
                    var argumentSets = cases.Count > 0 ? cases.Select(c => c.Arguments).ToList() : new List<object[]> { new object[0] };

                    foreach (var arguments in argumentSets)
                    {
                        string name = fixture.Name + "." + method.Name + (cases.Count > 0 ? "(" + string.Join(", ", arguments) + ")" : "");
                        if (filter != null && !filter.StartsWith("--") && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        string error = RunOne(fixture, method, arguments, setUps, tearDowns);
                        if (error == null)
                        {
                            passed++;
                            Console.WriteLine("PASS " + name);
                        }
                        else
                        {
                            failures.Add(name + "\n  " + error.Replace("\n", "\n  "));
                            Console.WriteLine("FAIL " + name + "\n  " + error.Replace("\n", "\n  "));
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Logic tests: " + passed + " passed, " + failures.Count + " failed.");
            return failures.Count == 0 ? 0 : 1;
        }

        private static string RunOne(Type fixture, MethodInfo method, object[] arguments, List<MethodInfo> setUps, List<MethodInfo> tearDowns)
        {
            LogSink.BeginTest();
            string error = null;
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(fixture, true);
                foreach (var s in setUps) s.Invoke(instance, null);
                method.Invoke(instance, Coerce(method, arguments));
            }
            catch (Exception e)
            {
                error = Describe(e);
            }
            finally
            {
                if (instance != null)
                {
                    foreach (var t in tearDowns)
                    {
                        try
                        {
                            t.Invoke(instance, null);
                        }
                        catch (Exception e)
                        {
                            error = (error == null ? "" : error + "\n") + "TearDown: " + Describe(e);
                        }
                    }
                }
            }

            var logFailures = LogSink.EndTest();
            if (logFailures.Count > 0) error = (error == null ? "" : error + "\n") + string.Join("\n", logFailures);
            return error;
        }

        private static object[] Coerce(MethodInfo method, object[] arguments)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != arguments.Length) throw new ArgumentException("TestCase has " + arguments.Length + " arguments, method takes " + parameters.Length);
            var result = new object[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                var target = parameters[i].ParameterType;
                var value = arguments[i];
                result[i] = value == null || target.IsInstanceOfType(value) ? value
                    : target.IsEnum ? Enum.ToObject(target, value)
                    : Convert.ChangeType(value, target);
            }
            return result;
        }

        private static string Describe(Exception e)
        {
            while (e is TargetInvocationException && e.InnerException != null) e = e.InnerException;
            if (e is AssertionException) return e.Message;
            return e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace;
        }

        private static bool IsTest(MethodInfo m) => Has<TestAttribute>(m) || Has<TestCaseAttribute>(m);

        private static bool Has<T>(MethodInfo m) where T : Attribute => m.GetCustomAttributes(typeof(T), false).Length > 0;
    }
}
