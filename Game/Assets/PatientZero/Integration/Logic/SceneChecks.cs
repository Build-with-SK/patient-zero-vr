using System;
using System.Collections.Generic;
using System.Globalization;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Integration.Logic
{
    /// <summary>A LayoutTag found in a generated scene.</summary>
    public readonly struct PlacedTag
    {
        public PlacedTag(string layoutId, Vector3 position, string hierarchyPath)
        {
            LayoutId = layoutId;
            Position = position;
            HierarchyPath = hierarchyPath ?? string.Empty;
        }

        public string LayoutId { get; }
        public Vector3 Position { get; }
        public string HierarchyPath { get; }
    }

    /// <summary>Every WardLayout point placed once, where the layout says. ARCHITECTURE.md section 7, step 4.</summary>
    public static class PlacementCheck
    {
        public const float ToleranceMeters = 0.05f;

        public static List<string> Validate(IReadOnlyList<PlacedTag> tags)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            var errors = new List<string>();
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < tags.Count; i++)
            {
                PlacedTag tag = tags[i];
                if (string.IsNullOrEmpty(tag.LayoutId))
                {
                    errors.Add("layout-empty: LayoutTag at " + tag.HierarchyPath + " has no id.");
                    continue;
                }

                if (!WardLayout.TryGet(tag.LayoutId, out LayoutPoint point))
                {
                    errors.Add("layout-unknown: '" + tag.LayoutId + "' at " + tag.HierarchyPath + " is not a WardLayout id.");
                    continue;
                }

                if (seen.TryGetValue(tag.LayoutId, out string firstPath))
                {
                    errors.Add("layout-duplicate: '" + tag.LayoutId + "' is placed twice: " + firstPath + " and " + tag.HierarchyPath + ".");
                    continue;
                }

                seen.Add(tag.LayoutId, tag.HierarchyPath);
                float off = Distance(tag.Position, point.Position);
                if (off > ToleranceMeters)
                {
                    errors.Add("layout-offset: '" + tag.LayoutId + "' at " + tag.HierarchyPath + " is " +
                               off.ToString("0.000", CultureInfo.InvariantCulture) + " m from its layout position (limit " +
                               ToleranceMeters.ToString("0.00", CultureInfo.InvariantCulture) + " m).");
                }
            }

            for (int i = 0; i < WardLayout.Points.Count; i++)
            {
                LayoutPoint point = WardLayout.Points[i];
                if (point.Required && !seen.ContainsKey(point.Id))
                {
                    errors.Add("layout-missing: required '" + point.Id + "' (" + point.Kind + ", act " + point.Act + ") is not placed.");
                }
            }

            return errors;
        }

        private static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    /// <summary>Exactly one implementer of each Core service, in the scene ARCHITECTURE.md section 6 puts it in.</summary>
    public static class ServiceCensus
    {
        private static readonly Type[] BootstrapServices =
        {
            typeof(IAudioCuePlayer), typeof(IPlayerQuery), typeof(ISettingsProvider), typeof(IScreenFader),
        };

        private static readonly Type[] WardServices =
        {
            typeof(IExperienceDirector),
        };

        public static IReadOnlyList<Type> ExpectedIn(BuildScene scene)
        {
            return Array.AsReadOnly(scene == BuildScene.Bootstrap ? BootstrapServices : WardServices);
        }

        public static IReadOnlyList<Type> All()
        {
            var all = new Type[BootstrapServices.Length + WardServices.Length];
            BootstrapServices.CopyTo(all, 0);
            WardServices.CopyTo(all, BootstrapServices.Length);
            return Array.AsReadOnly(all);
        }

        /// <param name="countIn">How many components in the given scene implement the given service.</param>
        public static List<string> Validate(Func<BuildScene, Type, int> countIn)
        {
            if (countIn == null) throw new ArgumentNullException(nameof(countIn));
            var errors = new List<string>();
            CheckScene(BuildScene.Bootstrap, BuildScene.Ward, countIn, errors);
            CheckScene(BuildScene.Ward, BuildScene.Bootstrap, countIn, errors);
            return errors;
        }

        private static void CheckScene(BuildScene home, BuildScene other, Func<BuildScene, Type, int> countIn, List<string> errors)
        {
            IReadOnlyList<Type> expected = ExpectedIn(home);
            for (int i = 0; i < expected.Count; i++)
            {
                Type service = expected[i];
                int here = countIn(home, service);
                int there = countIn(other, service);
                if (here != 1)
                {
                    errors.Add("service-count: " + service.Name + " needs exactly 1 implementer in " + home + ", found " + here + ".");
                }

                if (there != 0)
                {
                    errors.Add("service-scene: " + service.Name + " belongs in " + home + " but " + there + " implementer(s) are in " + other + ".");
                }
            }
        }
    }
}
