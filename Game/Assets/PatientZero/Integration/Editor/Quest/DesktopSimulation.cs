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
    ///   Touch (E-15): the phone/iPad controls (PZ_TOUCH) on Windows, with the mouse turned into touches, to test the
    ///     touch layout in the Editor before a phone build.
    /// The Android (Quest) configuration is never touched; QuestBuild refuses PZ_DESKTOP_MOUSE and PZ_TOUCH on Android.
    /// Defines follow the active build target, so outside batch mode the flat modes also switch the Editor to Windows
    /// (after an Android build the Editor is on Android and the adapters would be compiled out; seen 2026-10-30).
    ///
    /// Menu:      PatientZero/Desktop Simulation/Use Keyboard And Mouse, .../Use XR Interaction Simulator, .../Use Headset (Link)
    /// Batchmode: -executeMethod PatientZero.Integration.Editor.Quest.DesktopSimulation.EnableBatch (keyboard and mouse)
    /// Then:      PatientZero/Play, or PatientZero/Set Up And Play from a fresh checkout (PlayPipeline)
    /// </summary>
    public static class DesktopSimulation
    {
        public enum Mode { KeyboardMouse, XriSimulator, Headset, Touch }

        public const string XriPackage = "com.unity.xr.interaction.toolkit";
        public const string DesktopMouseDefine = "PZ_DESKTOP_MOUSE";
        public const string TouchDefine = "PZ_TOUCH";
        public const string Controls = "WASD move, mouse to look, aim and hold left mouse to grab (drag to pull a plank, turn the valve or pull the release), Q throw, F flashlight, T teleport, Esc pause, Enter new game";
        private const string SettingsType = "UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings";
        private static readonly string[] Samples = { "Starter Assets", "XR Interaction Simulator" };
        private const string SimulatorPrefab = "XR Interaction Simulator";

        [MenuItem("PatientZero/Desktop Simulation/Use Keyboard And Mouse", priority = 80)]
        public static void EnableFromMenu() => Apply(Mode.KeyboardMouse, exit: false);

        [MenuItem("PatientZero/Desktop Simulation/Use XR Interaction Simulator", priority = 81)]
        public static void SimulatorFromMenu() => Apply(Mode.XriSimulator, exit: false);

        [MenuItem("PatientZero/Desktop Simulation/Use Headset (Link)", priority = 82)]
        public static void DisableFromMenu() => Apply(Mode.Headset, exit: false);

        [MenuItem("PatientZero/Desktop Simulation/Use Touch Controls (phone layout)", priority = 83)]
        public static void TouchFromMenu() => Apply(Mode.Touch, exit: false);

        public static void EnableBatch() => Apply(Mode.KeyboardMouse, exit: true);

        public static void SimulatorBatch() => Apply(Mode.XriSimulator, exit: true);

        /// <summary>Keyboard and mouse on, no dialog and no exit; for PlayPipeline.</summary>
        public static EditorReport Enable() => Apply(Mode.KeyboardMouse, exit: false);

        /// <summary>True when the Editor compiles the keyboard and mouse adapters now: Windows target and PZ_DESKTOP_MOUSE.</summary>
        public static bool KeyboardMouseEnabled() => OnStandalone() && HasDefine(DesktopMouseDefine);

        /// <summary>True when the Editor compiles a flat-screen adapter now (keyboard and mouse, or touch).</summary>
        public static bool FlatScreenReady() => OnStandalone() && (HasDefine(DesktopMouseDefine) || HasDefine(TouchDefine));

        private static bool OnStandalone() => EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Standalone;

        private static bool HasDefine(string define)
        {
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
            return Array.IndexOf(defines, define) >= 0;
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
                SetDesktopMouse(report, mode == Mode.KeyboardMouse, mode == Mode.Touch);
                QuestBuild.ConfigureLoader(report, BuildTargetGroup.Standalone, initOnStart: mode == Mode.Headset);
                AssetDatabase.SaveAssets();
                if (mode != Mode.Headset) SwitchToWindows(report);
                switch (mode)
                {
                    case Mode.KeyboardMouse:
                        report.Line("Keyboard and mouse on. Enter Play mode in Bootstrap.unity (PatientZero/Play). " + Controls + ".");
                        break;
                    case Mode.Touch:
                        report.Line("Touch controls on (mouse = one finger). Enter Play mode in Bootstrap.unity. Left 40% of the screen: move stick; right side: look, tap to grab.");
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

        /// <summary>Sets PZ_DESKTOP_MOUSE and PZ_TOUCH for Standalone only (at most one of them). A change recompiles scripts.</summary>
        private static void SetDesktopMouse(EditorReport report, bool mouse, bool touch)
        {
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
            var list = new List<string>(defines);
            bool changed = Set(list, DesktopMouseDefine, mouse) | Set(list, TouchDefine, touch);
            if (changed) PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, list.ToArray());
            report.Pass("flat-screen input for Standalone: " + DesktopMouseDefine + (mouse ? " defined" : " not defined") + ", " +
                        TouchDefine + (touch ? " defined" : " not defined") + (changed ? " (changed; scripts recompile)" : ""));
        }

        private static bool Set(List<string> list, string define, bool on)
        {
            bool has = list.Contains(define);
            if (on && !has) list.Add(define);
            if (!on && has) list.Remove(define);
            return on != has;
        }

        /// <summary>Outside batch mode, makes Windows the active target so the Standalone defines compile in the Editor.
        /// Batch runs choose the target with -buildTarget, and the release build passes its own defines.</summary>
        private static void SwitchToWindows(EditorReport report)
        {
            if (OnStandalone()) { report.Pass("editor target: Windows"); return; }
            if (Application.isBatchMode)
            {
                report.Line("editor target: " + EditorUserBuildSettings.activeBuildTarget + " (batch mode; not switched)");
                return;
            }

            bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            if (ok) report.Pass("editor target: switched to Windows so the flat-screen adapters compile (reimport may take a minute)");
            else report.Error("editor target: could not switch to Windows. File > Build Profiles > Windows > Switch Platform.");
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
