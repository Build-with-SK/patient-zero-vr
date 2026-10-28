// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PatientZero.Core;
using PatientZero.Integration.Logic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Builds Bootstrap.unity and Ward.unity from every seat's [SceneBuildStep] methods (ARCHITECTURE.md section 7).
    /// Nobody edits these scenes by hand: they are regenerated, validated and saved by this class only.
    ///
    /// Menu:      PatientZero/Build Scenes
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.SceneComposer.BuildAllBatch
    /// </summary>
    public static class SceneComposer
    {
        [MenuItem("PatientZero/Build Scenes", priority = 0)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorReport report = BuildAll();
            EditorUtility.DisplayDialog("PatientZero: Build Scenes",
                report.Ok ? "Scenes built and validated." : report.Errors.Count + " error(s). See the Console or Logs/PatientZero/compose.txt.",
                "OK");
        }

        public static void BuildAllBatch()
        {
            EditorReport report = BuildAll();
            EditorApplication.Exit(report.Ok ? 0 : 1);
        }

        public static EditorReport BuildAll()
        {
            var report = new EditorReport("compose");
            // Preconditions, checked before any scene is touched (round 2 integration).
            if (!PackageSamples.RequiredAreImported(report) | !NavigationSettings.EnsureZombieAgentRadius(report))
            {
                report.Line("Preconditions failed: no scene was touched.");
                report.Finish();
                return report;
            }

            var methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
            BuildPlan plan = BuildPlan.Create(DiscoverSteps(methods, report));
            ListPlan(plan, report);
            if (!plan.IsValid)
            {
                report.AddErrors(plan.Errors);
                report.Line("Plan invalid: no scene was touched.");
                report.Finish();
                return report;
            }

            report.Pass("build plan: " + plan.StepsFor(BuildScene.Bootstrap).Count + " Bootstrap step(s), " +
                        plan.StepsFor(BuildScene.Ward).Count + " Ward step(s)");

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", QuestBuildConfig.GeneratedSceneFolder));
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();

            var counts = new Dictionary<BuildScene, Dictionary<Type, int>>();
            var wardTags = new List<PlacedTag>();
            Compose(BuildScene.Bootstrap, QuestBuildConfig.BootstrapScenePath, plan, methods, report, counts, wardTags);
            Compose(BuildScene.Ward, QuestBuildConfig.WardScenePath, plan, methods, report, counts, wardTags);

            List<string> placement = PlacementCheck.Validate(wardTags);
            if (placement.Count == 0) report.Pass("layout: " + wardTags.Count + " LayoutTag(s), every required WardLayout id placed once within " + PlacementCheck.ToleranceMeters + " m");
            report.AddErrors(placement);

            List<string> census = ServiceCensus.Validate((scene, service) =>
                counts.TryGetValue(scene, out Dictionary<Type, int> byType) && byType.TryGetValue(service, out int n) ? n : 0);
            if (census.Count == 0) report.Pass("services: one implementer of each Core service, in its home scene");
            report.AddErrors(census);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(QuestBuildConfig.BootstrapScenePath, true),
                new EditorBuildSettingsScene(QuestBuildConfig.WardScenePath, true),
            };
            report.Pass("build settings: Bootstrap (0), Ward (1)");
            report.Finish();
            return report;
        }

        private static List<BuildStep> DiscoverSteps(Dictionary<string, MethodInfo> methods, EditorReport report)
        {
            var steps = new List<BuildStep>();
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<SceneBuildStepAttribute>())
            {
                var attribute = method.GetCustomAttribute<SceneBuildStepAttribute>();
                Type declaring = method.DeclaringType;
                string type = declaring != null ? declaring.FullName : "?";
                string assembly = declaring != null ? declaring.Assembly.GetName().Name : "?";
                var step = new BuildStep(attribute.Scene, attribute.Order, assembly, type, method.Name, SignatureProblem(method));
                if (methods.ContainsKey(step.Name))
                {
                    report.Error("signature: " + step.Name + " is overloaded; a build step name must be unique.");
                    continue;
                }

                methods.Add(step.Name, method);
                steps.Add(step);
            }

            return steps;
        }

        private static string SignatureProblem(MethodInfo method)
        {
            if (!method.IsStatic) return "must be static.";
            if (!method.IsPublic) return "must be public.";
            if (method.ContainsGenericParameters) return "must not be generic.";
            if (method.ReturnType != typeof(void)) return "must return void.";
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(Transform))
            {
                return "must take exactly one UnityEngine.Transform (the step's root).";
            }

            return null;
        }

        private static void ListPlan(BuildPlan plan, EditorReport report)
        {
            foreach (BuildScene scene in new[] { BuildScene.Bootstrap, BuildScene.Ward })
            {
                IReadOnlyList<BuildStep> steps = plan.StepsFor(scene);
                for (int i = 0; i < steps.Count; i++)
                {
                    report.Line("step  " + scene + " " + steps[i].Order + "  " + steps[i].Name + "  (" + steps[i].Assembly + ")");
                }
            }
        }

        private static void Compose(BuildScene which, string path, BuildPlan plan, Dictionary<string, MethodInfo> methods,
            EditorReport report, Dictionary<BuildScene, Dictionary<Type, int>> counts, List<PlacedTag> wardTags)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            IReadOnlyList<BuildStep> steps = plan.StepsFor(which);
            for (int i = 0; i < steps.Count; i++)
            {
                BuildStep step = steps[i];
                var root = new GameObject(step.Order + " " + ShortName(step));
                SceneManager.MoveGameObjectToScene(root, scene);
                try
                {
                    methods[step.Name].Invoke(null, new object[] { root.transform });
                }
                catch (TargetInvocationException e)
                {
                    Exception inner = e.InnerException ?? e;
                    report.Error("step-threw: " + step.Name + ": " + inner.GetType().Name + ": " + inner.Message);
                    Debug.LogException(inner);
                }
            }

            var byType = new Dictionary<Type, int>();
            foreach (GameObject top in scene.GetRootGameObjects())
            {
                CheckMissing(top, which, report);
                CountServices(top, byType);
                foreach (LayoutTag tag in top.GetComponentsInChildren<LayoutTag>(true))
                {
                    if (which == BuildScene.Ward)
                    {
                        wardTags.Add(new PlacedTag(tag.LayoutId, tag.transform.position, PathOf(tag.transform)));
                    }
                    else
                    {
                        report.Error("layout-scene: LayoutTag '" + tag.LayoutId + "' at " + PathOf(tag.transform) +
                                     " is in Bootstrap; WardLayout content belongs in Ward.");
                    }
                }
            }

            counts[which] = byType;
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                report.Error("save: could not save " + path);
                return;
            }

            report.Pass("saved " + path + " (" + scene.rootCount + " root object(s))");
        }

        private static void CountServices(GameObject top, Dictionary<Type, int> byType)
        {
            IReadOnlyList<Type> services = ServiceCensus.All();
            foreach (MonoBehaviour behaviour in top.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                for (int i = 0; i < services.Count; i++)
                {
                    if (!services[i].IsInstanceOfType(behaviour)) continue;
                    byType.TryGetValue(services[i], out int n);
                    byType[services[i]] = n + 1;
                }
            }
        }

        private static void CheckMissing(GameObject top, BuildScene which, EditorReport report)
        {
            foreach (Transform t in top.GetComponentsInChildren<Transform>(true))
            {
                int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missingScripts > 0)
                {
                    report.Error("missing-script: " + which + " " + PathOf(t) + " has " + missingScripts + " missing script(s).");
                }

                foreach (Component component in t.GetComponents<Component>())
                {
                    if (component == null) continue;
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        {
                            report.Error("missing-reference: " + which + " " + PathOf(t) + " " + component.GetType().Name +
                                         "." + property.propertyPath + " points at a missing object.");
                        }
                    }
                }
            }
        }

        private static string ShortName(BuildStep step)
        {
            int dot = step.Type.LastIndexOf('.');
            return (dot >= 0 ? step.Type.Substring(dot + 1) : step.Type) + "." + step.Method;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
