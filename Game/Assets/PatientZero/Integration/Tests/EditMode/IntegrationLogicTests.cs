using System;
using System.Collections.Generic;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Integration.Logic;
using UnityEngine;

namespace PatientZero.Integration.Tests
{
    public class BuildPlanTests
    {
        private static BuildStep Step(BuildScene scene, int order, string assembly, string method = "Build", string error = null)
        {
            return new BuildStep(scene, order, assembly, assembly + ".Steps", method, error);
        }

        private static bool AnyStartsWith(IReadOnlyList<string> errors, string prefix)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                if (errors[i].StartsWith(prefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        [Test]
        public void ValidSteps_AreOrderedByOrderPerScene()
        {
            var plan = BuildPlan.Create(new[]
            {
                Step(BuildScene.Ward, 500, "PatientZero.AI.Editor"),
                Step(BuildScene.Ward, 100, "PatientZero.Environment.Editor"),
                Step(BuildScene.Bootstrap, 600, "PatientZero.Audio.Editor"),
                Step(BuildScene.Ward, 300, "PatientZero.Interaction.Editor"),
                Step(BuildScene.Bootstrap, 400, "PatientZero.Player.Editor"),
                Step(BuildScene.Ward, 999, "PatientZero.Integration.Editor"),
            });

            Assert.IsTrue(plan.IsValid, string.Join("\n", plan.Errors));
            IReadOnlyList<BuildStep> ward = plan.StepsFor(BuildScene.Ward);
            Assert.AreEqual(4, ward.Count);
            Assert.AreEqual(100, ward[0].Order);
            Assert.AreEqual(300, ward[1].Order);
            Assert.AreEqual(500, ward[2].Order);
            Assert.AreEqual(999, ward[3].Order);
            IReadOnlyList<BuildStep> boot = plan.StepsFor(BuildScene.Bootstrap);
            Assert.AreEqual(400, boot[0].Order);
            Assert.AreEqual(600, boot[1].Order);
        }

        [Test]
        public void Order_IsDeterministicRegardlessOfInputOrder()
        {
            var a = Step(BuildScene.Ward, 110, "PatientZero.Environment.Editor", "Walls");
            var b = Step(BuildScene.Ward, 120, "PatientZero.Environment.Editor", "NavMesh");
            var c = Step(BuildScene.Ward, 210, "PatientZero.Atmosphere.Editor", "Lights");
            var first = BuildPlan.Create(new[] { c, b, a }).StepsFor(BuildScene.Ward);
            var second = BuildPlan.Create(new[] { a, c, b }).StepsFor(BuildScene.Ward);
            for (int i = 0; i < 3; i++) Assert.AreEqual(first[i].Name, second[i].Name);
            Assert.AreEqual("Walls", first[0].Method);
        }

        [Test]
        public void OrderOutsideTheAreasBand_IsAnError()
        {
            var plan = BuildPlan.Create(new[] { Step(BuildScene.Ward, 250, "PatientZero.AI.Editor") });
            Assert.IsFalse(plan.IsValid);
            Assert.IsTrue(AnyStartsWith(plan.Errors, "band:"), string.Join("\n", plan.Errors));
        }

        [Test]
        public void SeatBuildingIntoTheWrongScene_IsAnError()
        {
            var plan = BuildPlan.Create(new[]
            {
                Step(BuildScene.Bootstrap, 150, "PatientZero.Environment.Editor"),
                Step(BuildScene.Ward, 450, "PatientZero.Flow.Editor"),
            });
            Assert.IsFalse(plan.IsValid);
            int sceneErrors = 0;
            for (int i = 0; i < plan.Errors.Count; i++) if (plan.Errors[i].StartsWith("scene:", StringComparison.Ordinal)) sceneErrors++;
            Assert.AreEqual(2, sceneErrors, string.Join("\n", plan.Errors));
        }

        [Test]
        public void AudioAndIntegration_MayBuildBothScenes()
        {
            var plan = BuildPlan.Create(new[]
            {
                Step(BuildScene.Bootstrap, 600, "PatientZero.Audio.Editor", "CuePlayer"),
                Step(BuildScene.Ward, 610, "PatientZero.Audio.Editor", "Emitters"),
                Step(BuildScene.Bootstrap, 990, "PatientZero.Integration.Editor", "CheckBoot"),
                Step(BuildScene.Ward, 990, "PatientZero.Integration.Editor", "CheckWard"),
            });
            Assert.IsTrue(plan.IsValid, string.Join("\n", plan.Errors));
        }

        [Test]
        public void DuplicateOrderInOneScene_IsAnError()
        {
            var plan = BuildPlan.Create(new[]
            {
                Step(BuildScene.Ward, 110, "PatientZero.Environment.Editor", "Walls"),
                Step(BuildScene.Ward, 110, "PatientZero.Environment.Editor", "Floors"),
            });
            Assert.IsTrue(AnyStartsWith(plan.Errors, "duplicate-order:"), string.Join("\n", plan.Errors));
        }

        [Test]
        public void SameOrderInDifferentScenes_IsAllowed()
        {
            var plan = BuildPlan.Create(new[]
            {
                Step(BuildScene.Bootstrap, 600, "PatientZero.Audio.Editor", "CuePlayer"),
                Step(BuildScene.Ward, 600, "PatientZero.Audio.Editor", "Emitters"),
            });
            Assert.IsTrue(plan.IsValid, string.Join("\n", plan.Errors));
        }

        [Test]
        public void StepOutsideAnArea_IsAnError()
        {
            var plan = BuildPlan.Create(new[]
            {
                Step(BuildScene.Ward, 100, "Assembly-CSharp-Editor"),
                Step(BuildScene.Ward, 101, "PatientZero.Core"),
            });
            int areaErrors = 0;
            for (int i = 0; i < plan.Errors.Count; i++) if (plan.Errors[i].StartsWith("area:", StringComparison.Ordinal)) areaErrors++;
            Assert.AreEqual(2, areaErrors, string.Join("\n", plan.Errors));
        }

        [Test]
        public void BadSignature_IsReported()
        {
            var plan = BuildPlan.Create(new[] { Step(BuildScene.Ward, 100, "PatientZero.Environment.Editor", "Build", "must be static") });
            Assert.IsTrue(AnyStartsWith(plan.Errors, "signature:"), string.Join("\n", plan.Errors));
        }

        [Test]
        public void UnknownSceneValue_IsRejectedNotOrdered()
        {
            var plan = BuildPlan.Create(new[] { Step((BuildScene)7, 100, "PatientZero.Environment.Editor") });
            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual(0, plan.StepsFor(BuildScene.Ward).Count);
            Assert.AreEqual(0, plan.StepsFor(BuildScene.Bootstrap).Count);
        }

        [Test]
        public void AreaOfAssembly_ReadsTheSecondSegment()
        {
            Assert.AreEqual("Environment", BuildBands.AreaOfAssembly("PatientZero.Environment.Editor"));
            Assert.AreEqual("UI", BuildBands.AreaOfAssembly("PatientZero.UI.Editor"));
            Assert.IsNull(BuildBands.AreaOfAssembly("PatientZero.Core"));
            Assert.IsNull(BuildBands.AreaOfAssembly("Unity.XR.Hands"));
            Assert.IsNull(BuildBands.AreaOfAssembly(null));
        }
    }

    public class PlacementCheckTests
    {
        private static List<PlacedTag> EveryRequiredPoint()
        {
            var tags = new List<PlacedTag>();
            for (int i = 0; i < WardLayout.Points.Count; i++)
            {
                LayoutPoint p = WardLayout.Points[i];
                if (p.Required) tags.Add(new PlacedTag(p.Id, p.Position, "Ward/" + p.Id));
            }
            return tags;
        }

        private static bool AnyStartsWith(List<string> errors, string prefix)
        {
            for (int i = 0; i < errors.Count; i++) if (errors[i].StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        [Test]
        public void EveryRequiredPointPlacedExactly_Passes()
        {
            List<string> errors = PlacementCheck.Validate(EveryRequiredPoint());
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        }

        [Test]
        public void OptionalPointsMayBeAbsentOrPlaced()
        {
            List<PlacedTag> tags = EveryRequiredPoint();
            Assert.IsTrue(WardLayout.TryGet("A1.Tray", out LayoutPoint tray));
            Assert.IsFalse(tray.Required);
            tags.Add(new PlacedTag(tray.Id, tray.Position, "Ward/Tray"));
            Assert.AreEqual(0, PlacementCheck.Validate(tags).Count);
        }

        [Test]
        public void MissingRequiredPoint_IsAnError()
        {
            List<PlacedTag> tags = EveryRequiredPoint();
            tags.RemoveAt(0);
            Assert.IsTrue(AnyStartsWith(PlacementCheck.Validate(tags), "layout-missing:"));
        }

        [Test]
        public void DuplicatePoint_IsAnError()
        {
            List<PlacedTag> tags = EveryRequiredPoint();
            tags.Add(tags[0]);
            Assert.IsTrue(AnyStartsWith(PlacementCheck.Validate(tags), "layout-duplicate:"));
        }

        [Test]
        public void UnknownOrEmptyId_IsAnError()
        {
            List<PlacedTag> tags = EveryRequiredPoint();
            tags.Add(new PlacedTag("A9.Nowhere", Vector3.zero, "Ward/X"));
            tags.Add(new PlacedTag("", Vector3.zero, "Ward/Y"));
            List<string> errors = PlacementCheck.Validate(tags);
            Assert.IsTrue(AnyStartsWith(errors, "layout-unknown:"));
            Assert.IsTrue(AnyStartsWith(errors, "layout-empty:"));
        }

        [Test]
        public void OffsetJustInsideTolerance_Passes_JustOutside_Fails()
        {
            List<PlacedTag> tags = EveryRequiredPoint();
            PlacedTag first = tags[0];
            tags[0] = new PlacedTag(first.LayoutId, first.Position + new Vector3(0.049f, 0f, 0f), first.HierarchyPath);
            Assert.AreEqual(0, PlacementCheck.Validate(tags).Count);
            tags[0] = new PlacedTag(first.LayoutId, first.Position + new Vector3(0f, 0.051f, 0f), first.HierarchyPath);
            Assert.IsTrue(AnyStartsWith(PlacementCheck.Validate(tags), "layout-offset:"));
        }
    }

    public class ServiceCensusTests
    {
        private static Func<BuildScene, Type, int> Correct()
        {
            return (scene, service) =>
            {
                IReadOnlyList<Type> home = ServiceCensus.ExpectedIn(scene);
                for (int i = 0; i < home.Count; i++) if (home[i] == service) return 1;
                return 0;
            };
        }

        [Test]
        public void OneImplementerEachInItsHomeScene_Passes()
        {
            Assert.AreEqual(0, ServiceCensus.Validate(Correct()).Count);
        }

        [Test]
        public void EveryCoreServiceHasAHomeScene()
        {
            Assert.AreEqual(5, ServiceCensus.All().Count);
            Assert.Contains(typeof(IExperienceDirector), (System.Collections.ICollection)ServiceCensus.ExpectedIn(BuildScene.Ward));
            Assert.Contains(typeof(IAudioCuePlayer), (System.Collections.ICollection)ServiceCensus.ExpectedIn(BuildScene.Bootstrap));
        }

        [Test]
        public void MissingService_IsAnError()
        {
            Func<BuildScene, Type, int> correct = Correct();
            List<string> errors = ServiceCensus.Validate((scene, service) => service == typeof(IPlayerQuery) ? 0 : correct(scene, service));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors[0].StartsWith("service-count: IPlayerQuery", StringComparison.Ordinal));
        }

        [Test]
        public void TwoImplementers_IsAnError()
        {
            Func<BuildScene, Type, int> correct = Correct();
            List<string> errors = ServiceCensus.Validate((scene, service) =>
                service == typeof(IExperienceDirector) && scene == BuildScene.Ward ? 2 : correct(scene, service));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
        }

        [Test]
        public void ServiceInTheWrongScene_IsAnError()
        {
            Func<BuildScene, Type, int> correct = Correct();
            List<string> errors = ServiceCensus.Validate((scene, service) =>
                service == typeof(IAudioCuePlayer) ? (scene == BuildScene.Ward ? 1 : 0) : correct(scene, service));
            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
        }
    }

    public class QuestBuildConfigTests
    {
        [Test]
        public void ApiLevels_MeetMetaRequirements()
        {
            Assert.GreaterOrEqual(QuestBuildConfig.MinAndroidApi, 32);
            Assert.GreaterOrEqual(QuestBuildConfig.TargetAndroidApi, 34);
            Assert.LessOrEqual(QuestBuildConfig.MinAndroidApi, QuestBuildConfig.TargetAndroidApi);
        }

        [Test]
        public void Bootstrap_IsSceneZero()
        {
            Assert.AreEqual(2, QuestBuildConfig.Scenes.Count);
            Assert.AreEqual(QuestBuildConfig.BootstrapScenePath, QuestBuildConfig.Scenes[0]);
            Assert.AreEqual(QuestBuildConfig.WardScenePath, QuestBuildConfig.Scenes[1]);
        }

        [Test]
        public void RequiredFeatures_AreUnique()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < QuestBuildConfig.RequiredOpenXRFeatures.Count; i++)
            {
                Assert.IsTrue(seen.Add(QuestBuildConfig.RequiredOpenXRFeatures[i]), QuestBuildConfig.RequiredOpenXRFeatures[i]);
            }
            Assert.IsTrue(seen.Contains("UnityEngine.XR.Hands.OpenXR.HandTracking"));
        }

        [Test]
        public void ForbiddenPackages_MatchByPrefixOnly()
        {
            Assert.IsTrue(QuestBuildConfig.IsForbiddenPackage("com.unity.xr.oculus"));
            Assert.IsTrue(QuestBuildConfig.IsForbiddenPackage("com.meta.xr.sdk.interaction.ovr"));
            Assert.IsFalse(QuestBuildConfig.IsForbiddenPackage("com.meta.xr.sdk.audio"));
            Assert.IsFalse(QuestBuildConfig.IsForbiddenPackage("com.unity.xr.openxr"));
            Assert.IsFalse(QuestBuildConfig.IsForbiddenPackage("com.unity.xr.oculusish"));
            Assert.IsFalse(QuestBuildConfig.IsForbiddenPackage(null));
        }

        [Test]
        public void EditorVersion_IsA63FinalRelease()
        {
            Assert.IsTrue(QuestBuildConfig.IsSupportedEditor(QuestBuildConfig.EditorVersion));
            Assert.IsTrue(QuestBuildConfig.IsSupportedEditor("6000.3.0f1"));
            Assert.IsFalse(QuestBuildConfig.IsSupportedEditor("6000.0.66f2"));
            Assert.IsFalse(QuestBuildConfig.IsSupportedEditor("6000.3.1b2"));
            Assert.IsFalse(QuestBuildConfig.IsSupportedEditor("6000.3.f1"));
            Assert.IsFalse(QuestBuildConfig.IsSupportedEditor("6000.3.27f"));
            Assert.IsFalse(QuestBuildConfig.IsSupportedEditor(null));
        }
    }
}
