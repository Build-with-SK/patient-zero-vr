// UNVERIFIED IN EDITOR: written 2026-10-28 before a Unity 6.3 Editor existed on any seat. First compiled at integration.
// XR Interaction Toolkit types are reached by name so a renamed field is a reported warning, not a compile error.
using System;
using System.Collections.Generic;
using PatientZero.Integration.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor.Quest
{
    /// <summary>
    /// Play the game on Windows without a headset, or with one over Link. Three modes, all Standalone-only:
    ///   Keyboard and mouse (default): seat A's desktop adapters (DesktopLocomotion, DesktopInteraction), compiled in by
    ///     the PZ_DESKTOP_MOUSE define (FIX-A-3). XR Interaction Simulator off, Standalone XR not started.
    ///   XR Interaction Simulator: XRI's simulated head, controllers and hands, for testing XRI interactions. The desktop
    ///     adapters are compiled out, because the simulator binds the same keys (WASD, Q/E, right and left mouse, R) and
    ///     disables mouse clicks on world-space UI while it runs.
    ///   Headset (Link): no simulator, no adapters, Standalone OpenXR started (Quest Link / Air Link).
    /// The Android (Quest) configuration is never touched; QuestBuild refuses PZ_DESKTOP_MOUSE on Android.
    ///
    /// Menu:      PatientZero/Desktop Simulation/Use Keyboard And Mouse, .../Use XR Interaction Simulator, .../Use Headset (Link)
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.Quest.DesktopSimulation.EnableBatch (keyboard and mouse)
    /// Then:      PatientZero/Play, or PatientZero/Set Up And Play from a fresh checkout (PlayPipeline)
    /// </summary>
    public static class DesktopSimulation
    {
        public enum Mode { KeyboardMouse, XriSimulator, Headset }

        public const string XriPackage = "com.unity.xr.interaction.toolkit";
        public const string DesktopMouseDefine = "PZ_DESKTOP_MOUSE";
        public const string Controls = "WASD move, hold right mouse to look, T teleport, left mouse grab, Q throw, Esc pause, Enter confirm calibration";
        private const string SettingsType = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings";
        private static readonly string[] Samples = { "Starter Assets", "XR Interaction Simulator" };
        private const string SimulatorPrefab = "XR Interaction Simulator";

        [MenuItem("PatientZero/Desktop Simulation/Use Keyboard And Mouse", priority = 80)]
        public static void EnableFromMenu() => Apply(Mode.KeyboardMouse, exit: false);

        [MenuItem("PatientZero/Desktop Simulation/Use XR Interaction Simulator", priority = 81)]
        public static void SimulatorFromMenu() => Apply(Mode.XriSimulator, exit: false);

        [MenuItem("PatientZero/Desktop Simulation/Use Headset (Link)", priority = 82)]
        public static void DisableFromMenu() => Apply(Mode.Headset, exit: false);

        public static void EnableBatch() => Apply(Mode.KeyboardMouse, exit: true);

        public static void SimulatorBatch() => Apply(Mode.XriSimulator, exit: true);

        /// <summary>Keyboard and mouse on, no dialog and no exit; for PlayPipeline.</summary>
        public static EditorReport Enable() => Apply(Mode.KeyboardMouse, exit: false);

        /// <summary>True when the Standalone target compiles seat A's keyboard and mouse adapters.</summary>
        public static bool KeyboardMouseEnabled()
        {
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
            return Array.IndexOf(defines, DesktopMouseDefine) >= 0;
        }

        private static EditorReport Apply(Mode mode, bool exit)
        {
            var report = new EditorReport(mode == Mode.Headset ? "headset-mode" : "desktop-simulation");
            try
            {
                if (mode != Mode.Headset) ImportSamples(report);
                QuestBuild.ConfigureRendering(report); // every mode renders with URP, as on the headset
                QuestBuild.ConfigureInput(report); // Input System only, as on the headset
                SetSimulator(report, mode == Mode.XriSimulator);
                SetDesktopMouse(report, mode == Mode.KeyboardMouse);
                QuestBuild.ConfigureLoader(report, BuildTargetGroup.Standalone, initOnStart: mode == Mode.Headset);
                AssetDatabase.SaveAssets();
                switch (mode)
                {
                    case Mode.KeyboardMouse:
                        report.Line("Keyboard and mouse on. Enter Play mode in Bootstrap.unity (PatientZero/Play). " + Controls + ".");
                        break;
                    case Mode.XriSimulator:
                        report.Line("XR Interaction Simulator on. Enter Play mode in Bootstrap.unity. Controls are in the simulator's on-screen panel.");
                        break;
                    default:
                        report.Line("Headset mode on. Connect the Quest with Link, then enter Play mode.");
                        break;
                }
            }
            catch (Exception e)
            {
                report.Error("exception: " + e.GetType().Name + ": " + e.Message);
                Debug.LogException(e);
            }

            report.Finish();
            if (exit) EditorApplication.Exit(report.Ok ? 0 : 1);
            return report;
        }

        private static void ImportSamples(EditorReport report)
        {
            foreach (string sample in Samples) PackageSamples.Import(report, XriPackage, sample, required: true);
            AssetDatabase.Refresh();
        }

        /// <summary>Adds or removes PZ_DESKTOP_MOUSE for Standalone only. A change recompiles scripts.</summary>
        private static void SetDesktopMouse(EditorReport report, bool on)
        {
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
            var list = new List<string>(defines);
            bool has = list.Contains(DesktopMouseDefine);
            if (on && !has) list.Add(DesktopMouseDefine);
            if (!on && has) list.Remove(DesktopMouseDefine);
            if (on != has) PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, list.ToArray());
            report.Pass("desktop input: " + DesktopMouseDefine + (on ? " defined" : " not defined") + " for Standalone" +
                        (on != has ? " (changed; scripts recompile)" : ""));
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
