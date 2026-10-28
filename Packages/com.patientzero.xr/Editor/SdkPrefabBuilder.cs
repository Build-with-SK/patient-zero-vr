using System;
using System.IO;
using System.Linq;
using PatientZero.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

namespace PatientZero.XR.Editor
{
    // Imports SDK-owned configurations intact instead of reconstructing hand/controller models.
    public static class SdkPrefabBuilder
    {
        private const string Output = "Assets/PatientZeroGenerated";
        [MenuItem("Patient Zero/Build SDK Prefabs From Selected Sources")]
        private static void BuildSelected()
        {
            if (!(Selection.activeObject is XrPrefabSources sources)) throw new InvalidOperationException("Select a Patient Zero SDK Prefab Sources asset first.");
            Build(sources, Output);
        }
        public static void Build(XrPrefabSources sources, string output)
        {
            Validate(sources);
            if (!output.StartsWith("Assets/", StringComparison.Ordinal) || output.Contains(".."))
                throw new ArgumentException("Output must be a directory under Assets.");
            if (Directory.Exists(output)) throw new IOException("Output exists. Use a new output directory to preserve user assets.");
            Directory.CreateDirectory(output);
            try
            {
                SaveRig(sources, output);
                SaveProp(sources.Throwable, "PZ_Throwable", output, null, Vector3.forward, 0, 90);
                SaveProp(sources.TwoHanded, "PZ_TwoHanded", output, null, Vector3.forward, 0, 90);
                SaveProp(sources.Valve, "PZ_Valve", output, sources.ValveMovingPart, sources.ValveAxis, sources.ValveMin, sources.ValveMax);
                SaveProp(sources.Lever, "PZ_Lever", output, sources.LeverMovingPart, sources.LeverAxis, sources.LeverMin, sources.LeverMax);
            }
            finally { AssetDatabase.Refresh(); }
            Debug.Log("Patient Zero prefabs created without opening or modifying a scene. Wire scene service bindings, then run the XR acceptance checklist.");
        }
        private static void Validate(XrPrefabSources s)
        {
            if (!s || !s.Rig || !s.Throwable || !s.TwoHanded || !s.Valve || !s.Lever)
                throw new ArgumentException("Assign all five SDK prefab assets.");
            if (FindType("PatientZero.XR.Meta.MetaGrabbableSafety") == null)
                throw new InvalidOperationException("Install Meta Interaction SDK 207.x and wait for compilation before building.");
            var templates = new[] { s.Rig, s.Throwable, s.TwoHanded, s.Valve, s.Lever };
            foreach (var template in templates)
                if (!PrefabUtility.IsPartOfPrefabAsset(template) || template.transform.parent)
                    throw new ArgumentException("Templates must be root prefab assets.");
            if (s.Rig.GetComponentsInChildren<Camera>(true).Count(c => c.enabled) != 1)
                throw new ArgumentException("The rig must have exactly one center-eye Camera.");
            if (s.Rig.GetComponentsInChildren<AudioListener>(true).Count(c => c.enabled) != 1)
                throw new ArgumentException("The rig must have exactly one AudioListener.");
            var seen = new System.Collections.Generic.HashSet<Behaviour>();
            foreach (var group in new[] { s.LeftHand, s.RightHand, s.LeftController, s.RightController })
            {
                if (group.Length == 0) throw new ArgumentException("All four selector lists must be populated.");
                foreach (var selector in group)
                {
                    if (!selector || !selector.transform.IsChildOf(s.Rig.transform) || !IsSdkInteractor(selector) || !seen.Add(selector))
                        throw new ArgumentException("Each selector must be a unique SDK IInteractorView inside the rig prefab.");
                }
            }
            foreach (var selector in s.Rig.GetComponentsInChildren<Behaviour>(true))
                if (selector && IsSdkInteractor(selector) && !seen.Contains(selector))
                    throw new ArgumentException("An SDK interactor is ungated: " + selector.name + "/" + selector.GetType().Name);
            foreach (var template in templates.Skip(1))
            {
                if (template.GetComponentsInChildren<Rigidbody>(true).Length != 1 || !template.GetComponentsInChildren<Behaviour>(true).Any(b => b && b.GetType().FullName == "Oculus.Interaction.Grabbable"))
                    throw new ArgumentException("Each prop template needs exactly one Rigidbody and an SDK Grabbable.");
            }
            if (!s.ValveMovingPart || !s.ValveMovingPart.IsChildOf(s.Valve.transform) ||
                !s.LeverMovingPart || !s.LeverMovingPart.IsChildOf(s.Lever.transform))
                throw new ArgumentException("Assign moving-part transforms from the valve/lever prefab assets.");
        }
        private static bool IsSdkInteractor(Behaviour component) => component.GetType().GetInterfaces().Any(i => i.FullName == "Oculus.Interaction.IInteractorView");
        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName); if (type != null) return type;
            }
            return null;
        }
        private static T Map<T>(T source, GameObject sourceRoot, GameObject clone) where T : Component
        {
            string path = AnimationUtility.CalculateTransformPath(source.transform, sourceRoot.transform);
            Transform target = string.IsNullOrEmpty(path) ? clone.transform : clone.transform.Find(path);
            if (!target) throw new InvalidOperationException("SDK template mapping failed: " + path);
            var sameType = source.GetComponents(source.GetType());
            int index = Array.IndexOf(sameType, source);
            return (T)target.GetComponents(source.GetType())[index];
        }
        private static Behaviour[] MapList(Behaviour[] list, GameObject source, GameObject clone) => list.Select(b => Map(b, source, clone)).ToArray();
        private static void SaveRig(XrPrefabSources s, string output)
        {
            var root = new GameObject("PZ_XRRig");
            try
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(s.Rig); rig.transform.SetParent(root.transform, false);
                var hub = root.AddComponent<TrackingSafetyHub>();
                var left = new GameObject("LeftTrackingGuard"); left.transform.SetParent(root.transform, false);
                var right = new GameObject("RightTrackingGuard"); right.transform.SetParent(root.transform, false);
                var l = left.AddComponent<TrackingLossGuard>(); var r = right.AddComponent<TrackingLossGuard>();
                l.HandInteractors = MapList(s.LeftHand, s.Rig, rig); l.ControllerInteractors = MapList(s.LeftController, s.Rig, rig);
                r.HandInteractors = MapList(s.RightHand, s.Rig, rig); r.ControllerInteractors = MapList(s.RightController, s.Rig, rig);
                UnityEventTools.AddPersistentListener(l.OnCancelSelection, hub.CancelHeldObjects);
                UnityEventTools.AddPersistentListener(r.OnCancelSelection, hub.CancelHeldObjects);
                var input = root.AddComponent<OpenXRTrackingInput>(); input.Left = l; input.Right = r;
                var footsteps = root.AddComponent<PlayerFootstepNoise>();
                footsteps.Head = rig.GetComponentsInChildren<Camera>(true).First(c => c.enabled).transform;
                footsteps.FloorReference = root.transform;
                PrefabUtility.SaveAsPrefabAsset(root, output + "/PZ_XRRig.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void SaveProp(GameObject template, string name, string output, Transform moving, Vector3 axis, float min, float max)
        {
            var root = new GameObject(name);
            try
            {
                var clone = (GameObject)PrefabUtility.InstantiatePrefab(template); clone.transform.SetParent(root.transform, false);
                var body = clone.GetComponentInChildren<Rigidbody>(true);
                var safety = body.gameObject.AddComponent<HeldObjectSafety>();
                var components = clone.GetComponentsInChildren<Behaviour>(true);
                safety.SelectionComponents = components.Where(b => b && (IsSdkInteractor(b) ||
                    b.GetType().GetInterfaces().Any(i => i.FullName == "Oculus.Interaction.IInteractableView") || b.GetType().FullName == "Oculus.Interaction.Grabbable")).ToArray();
                var grabbable = components.First(b => b && b.GetType().FullName == "Oculus.Interaction.Grabbable");
                var bridge = body.gameObject.AddComponent(FindType("PatientZero.XR.Meta.MetaGrabbableSafety"));
                var serialized = new SerializedObject(bridge); serialized.FindProperty("Grabbable").objectReferenceValue = grabbable; serialized.ApplyModifiedPropertiesWithoutUndo();
                var noise = body.gameObject.AddComponent<NoiseEmitter>();
                noise.Category = moving ? NoiseCategory.Door : NoiseCategory.Bottle;
                noise.EmitOnCollision = !moving;
                if (moving)
                {
                    var feedback = root.AddComponent<MechanismFeedback>(); feedback.MovingPart = Map(moving, template, clone);
                    feedback.LocalAxis = axis; feedback.MinDegrees = min; feedback.MaxDegrees = max; feedback.Noise = noise;
                }
                PrefabUtility.SaveAsPrefabAsset(root, output + "/" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
