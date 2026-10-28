// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
// XR Interaction Toolkit types are reached by name so a renamed field is a reported warning, not a compile error.
using System;
using PatientZero.Integration.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor.Quest
{
    /// <summary>
    /// Desktop simulation: play the game in the Editor with keyboard and mouse, no headset.
    ///
    /// Uses XR Interaction Toolkit's own XR Interaction Simulator (simulated head, controllers and hands),
    /// so the same rig, interactables and hand poses run as on device. Two modes:
    ///   Desktop: simulator on, Standalone XR not started (no OpenXR runtime needed).
    ///   Headset: simulator off, Standalone OpenXR started (Quest Link / Air Link on Windows).
    /// The Android (Quest) configuration is never touched by either mode.
    ///
    /// Menu:      PatientZero/Desktop Simulation/Use Desktop Simulation, .../Use Headset (Link)
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.Quest.DesktopSimulation.EnableBatch
    /// </summary>
    public static class DesktopSimulation
    {
        public const string XriPackage = "com.unity.xr.interaction.toolkit";
        private const string SettingsType = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings";
        private static readonly string[] Samples = { "Starter Assets", "XR Interaction Simulator" };
        private const string SimulatorPrefab = "XR Interaction Simulator";

        [MenuItem("PatientZero/Desktop Simulation/Use Desktop Simulation", priority = 80)]
        public static void EnableFromMenu() => Apply(true, exit: false);

        [MenuItem("PatientZero/Desktop Simulation/Use Headset (Link)", priority = 81)]
        public static void DisableFromMenu() => Apply(false, exit: false);

        public static void EnableBatch() => Apply(true, exit: true);

        private static void Apply(bool desktop, bool exit)
        {
            var report = new EditorReport(desktop ? "desktop-simulation" : "headset-mode");
            try
            {
                if (desktop) ImportSamples(report);
                SetSimulator(report, desktop);
                QuestBuild.ConfigureLoader(report, BuildTargetGroup.Standalone, initOnStart: !desktop);
                AssetDatabase.SaveAssets();
                report.Line(desktop
                    ? "Desktop simulation on. Enter Play mode in Bootstrap.unity. Controls are listed in the simulator's on-screen panel."
                    : "Headset mode on. Connect the Quest with Link, then enter Play mode.");
            }
            catch (Exception e)
            {
                report.Error("exception: " + e.GetType().Name + ": " + e.Message);
                Debug.LogException(e);
            }

            report.Finish();
            if (exit) EditorApplication.Exit(report.Ok ? 0 : 1);
        }

        private static void ImportSamples(EditorReport report)
        {
            foreach (string sample in Samples) PackageSamples.Import(report, XriPackage, sample, required: true);
            AssetDatabase.Refresh();
        }

        private static void SetSimulator(EditorReport report, bool on)
        {
            Type type = QuestBuild.FindType(SettingsType);
            Object settings = type == null ? null : FindInstance(type);
            if (settings == null)
            {
                report.Warn("simulation: " + SettingsType + " not found. Set it by hand: Project Settings > XR Plug-in Management > " +
                            "XR Interaction Toolkit > 'Use XR Interaction Simulator in scenes' = " + (on ? "on" : "off") + ".");
                return;
            }

            var serialized = new SerializedObject(settings);
            int changed = 0;
            SerializedProperty property = serialized.GetIterator();
            while (property.Next(true))
            {
                string name = property.name;
                if (property.propertyType == SerializedPropertyType.Boolean)
                {
                    if (name.IndexOf("AutomaticallyInstantiate", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // "...SimulatorPrefab" follows the mode; "...InEditorOnly" stays on so a device build never carries the simulator.
                        bool value = name.IndexOf("EditorOnly", StringComparison.OrdinalIgnoreCase) >= 0 || on;
                        if (property.boolValue != value) { property.boolValue = value; changed++; }
                    }
                    else if (name.IndexOf("UseClassic", StringComparison.OrdinalIgnoreCase) >= 0 && property.boolValue)
                    {
                        property.boolValue = false;
                        changed++;
                    }
                }
                else if (on && property.propertyType == SerializedPropertyType.ObjectReference &&
                         property.objectReferenceValue == null &&
                         name.IndexOf("Prefab", StringComparison.OrdinalIgnoreCase) >= 0 &&
                         name.IndexOf("Classic", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    GameObject prefab = FindPrefab(SimulatorPrefab);
                    if (prefab != null) { property.objectReferenceValue = prefab; changed++; }
                    else report.Warn("simulation: prefab '" + SimulatorPrefab + "' not found; import the XRI sample first.");
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);
            if (changed == 0 && on) report.Warn("simulation: no simulator setting changed; check field names in " + SettingsType + ".");
            report.Pass("simulation: XR Interaction Simulator auto-instantiate " + (on ? "on (Editor only)" : "off") + ", " + changed + " field(s) changed");
        }

        private static Object FindInstance(Type type)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                                                         System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy;
            System.Reflection.PropertyInfo instance = type.GetProperty("Instance", flags);
            if (instance != null && instance.GetValue(null) is Object fromProperty) return fromProperty;

            foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name))
            {
                Object asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
                if (asset != null) return asset;
            }

            return null;
        }

        private static GameObject FindPrefab(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }
    }
}
