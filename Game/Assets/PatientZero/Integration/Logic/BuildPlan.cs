using System;
using System.Collections.Generic;
using PatientZero.Core;

namespace PatientZero.Integration.Logic
{
    /// <summary>One [SceneBuildStep] method found by the composer, described without engine types.</summary>
    public readonly struct BuildStep
    {
        public BuildStep(BuildScene scene, int order, string assembly, string type, string method, string signatureError = null)
        {
            Scene = scene;
            Order = order;
            Assembly = assembly ?? string.Empty;
            Type = type ?? string.Empty;
            Method = method ?? string.Empty;
            SignatureError = signatureError;
        }

        public BuildScene Scene { get; }
        public int Order { get; }
        /// <summary>Assembly name, e.g. "PatientZero.Environment.Editor". Decides the owning area.</summary>
        public string Assembly { get; }
        public string Type { get; }
        public string Method { get; }
        /// <summary>Null when the method is public static void Build(Transform root).</summary>
        public string SignatureError { get; }
        public string Name => Type + "." + Method;
    }

    /// <summary>The order bands and scene rules from SceneBuildStep.cs, as data.</summary>
    public static class BuildBands
    {
        private struct Band
        {
            public string Area;
            public int Min;
            public int Max;
            public bool Bootstrap;
            public bool Ward;
        }

        private static readonly Band[] Bands =
        {
            new Band { Area = "Environment", Min = 100, Max = 199, Ward = true },
            new Band { Area = "Atmosphere", Min = 200, Max = 299, Ward = true },
            new Band { Area = "Interaction", Min = 300, Max = 399, Ward = true },
            new Band { Area = "Player", Min = 400, Max = 499, Bootstrap = true },
            new Band { Area = "Flow", Min = 400, Max = 499, Bootstrap = true },
            new Band { Area = "UI", Min = 400, Max = 499, Bootstrap = true },
            new Band { Area = "AI", Min = 500, Max = 599, Ward = true },
            new Band { Area = "Audio", Min = 600, Max = 699, Bootstrap = true, Ward = true },
            new Band { Area = "Integration", Min = 900, Max = 999, Bootstrap = true, Ward = true },
        };

        /// <summary>"PatientZero.Environment.Editor" → "Environment". Null for anything that is not a PatientZero area.</summary>
        public static string AreaOfAssembly(string assembly)
        {
            if (string.IsNullOrEmpty(assembly)) return null;
            string[] parts = assembly.Split('.');
            if (parts.Length < 2 || parts[0] != "PatientZero") return null;
            for (int i = 0; i < Bands.Length; i++)
            {
                if (Bands[i].Area == parts[1]) return parts[1];
            }
            return null;
        }

        public static bool TryGetBand(string area, out int min, out int max)
        {
            for (int i = 0; i < Bands.Length; i++)
            {
                if (Bands[i].Area == area)
                {
                    min = Bands[i].Min;
                    max = Bands[i].Max;
                    return true;
                }
            }
            min = 0;
            max = -1;
            return false;
        }

        public static bool MayBuild(string area, BuildScene scene)
        {
            for (int i = 0; i < Bands.Length; i++)
            {
                if (Bands[i].Area != area) continue;
                return scene == BuildScene.Bootstrap ? Bands[i].Bootstrap : scene == BuildScene.Ward && Bands[i].Ward;
            }
            return false;
        }
    }

    /// <summary>
    /// Validates and orders every scene build step. A plan with errors must not be executed:
    /// the composer refuses to build, so a broken step never produces a half-built scene.
    /// </summary>
    public sealed class BuildPlan
    {
        private readonly List<BuildStep> _bootstrap = new List<BuildStep>();
        private readonly List<BuildStep> _ward = new List<BuildStep>();
        private readonly List<string> _errors = new List<string>();

        private BuildPlan()
        {
        }

        public IReadOnlyList<string> Errors => _errors;
        public bool IsValid => _errors.Count == 0;

        public IReadOnlyList<BuildStep> StepsFor(BuildScene scene)
        {
            return scene == BuildScene.Bootstrap ? _bootstrap : _ward;
        }

        public static BuildPlan Create(IEnumerable<BuildStep> steps)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            var plan = new BuildPlan();

            foreach (BuildStep step in steps)
            {
                if (step.Scene != BuildScene.Bootstrap && step.Scene != BuildScene.Ward)
                {
                    plan._errors.Add("scene: " + step.Name + " targets unknown scene " + (int)step.Scene + ".");
                    continue;
                }

                if (step.SignatureError != null)
                {
                    plan._errors.Add("signature: " + step.Name + " " + step.SignatureError);
                }

                string area = BuildBands.AreaOfAssembly(step.Assembly);
                if (area == null)
                {
                    plan._errors.Add("area: " + step.Name + " is in assembly '" + step.Assembly +
                                     "', which is not a PatientZero seat area or Integration.");
                }
                else
                {
                    BuildBands.TryGetBand(area, out int min, out int max);
                    if (step.Order < min || step.Order > max)
                    {
                        plan._errors.Add("band: " + step.Name + " uses order " + step.Order + "; " + area +
                                         " owns " + min + "-" + max + ".");
                    }

                    if (!BuildBands.MayBuild(area, step.Scene))
                    {
                        plan._errors.Add("scene: " + area + " may not build into " + step.Scene + " (" + step.Name +
                                         "). See SceneBuildStep.cs; ask for a contract change if this is needed.");
                    }
                }

                (step.Scene == BuildScene.Bootstrap ? plan._bootstrap : plan._ward).Add(step);
            }

            plan._bootstrap.Sort(Compare);
            plan._ward.Sort(Compare);
            plan.CheckDuplicateOrders(plan._bootstrap);
            plan.CheckDuplicateOrders(plan._ward);
            return plan;
        }

        private void CheckDuplicateOrders(List<BuildStep> sorted)
        {
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Order == sorted[i - 1].Order)
                {
                    _errors.Add("duplicate-order: " + sorted[i - 1].Name + " and " + sorted[i].Name + " both use " +
                                sorted[i].Scene + " order " + sorted[i].Order + ". Give each step its own order.");
                }
            }
        }

        private static int Compare(BuildStep a, BuildStep b)
        {
            int byOrder = a.Order.CompareTo(b.Order);
            return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Name, b.Name);
        }
    }
}
