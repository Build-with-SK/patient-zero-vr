// UNVERIFIED IN EDITOR
using PatientZero.Atmosphere.Logic;
using PatientZero.Core;
using PatientZero.Environment.Editor;
using UnityEngine;

namespace PatientZero.Atmosphere.Editor
{
    public static class AtmosphereBuildStep
    {
        /// <summary>Composer step: six shadowless, limited-range ward fixtures, the lower floors' ceiling lights (v1.3, E-19)
        /// and the Ward director.</summary>
        [SceneBuildStep(BuildScene.Ward, 200)]
        public static void Build(Transform root)
        {
            var controller = root.gameObject.AddComponent<LightingController>();
            var points = new[] { WardLayout.A1_Tray, WardLayout.A1_DoorWindow, WardLayout.A2_ValveWheel,
                WardLayout.A3_Mid1, WardLayout.A4_Start, WardLayout.Z4_Gurney,
                // v1.3 (E-19): fixture 6 and up, FixturePolicy.FloorFixtureEnabled. The ward stairwell, then floors 2 and 1.
                WardLayout.A4_Stairs,
                WardLayout.A5_Start, WardLayout.A5_Hall2, WardLayout.A5_Bench, WardLayout.A5_FuseBox, WardLayout.A5_Stairs, WardLayout.Z5_Dormant,
                WardLayout.A6_Start, WardLayout.A6_Hall1, WardLayout.A6_Generator, WardLayout.A6_Hall3, WardLayout.A6_Lockdown,
                WardLayout.A6_Exit, WardLayout.A6_Desk };
            controller.Fixtures = new Light[points.Length]; controller.FixtureFaces = new Renderer[points.Length];
            var housing = WardPrimitives.Material("FixtureHousing", new Color(.18f, .2f, .19f));
            var lens = WardPrimitives.Material("FixtureLens", new Color(.55f, .62f, .6f));
            lens.EnableKeyword("_EMISSION"); lens.SetColor("_EmissionColor", Color.black);
            UnityEditor.EditorUtility.SetDirty(lens); UnityEditor.AssetDatabase.SaveAssets();
            for (int i = 0; i < points.Length; i++)
            {
                var go = new GameObject("Fixture_" + points[i].Id); go.transform.SetParent(root, false);
                go.transform.position = new Vector3(points[i].Position.x, 2.5f, points[i].Position.z);
                var fixture = go.AddComponent<Light>(); fixture.type = LightType.Point; fixture.range = i == 1 || i == 2 ? 4.5f : i < 4 ? 3 : i < FixturePolicy.WardFixtures ? 2.8f : 4.5f;
                fixture.intensity = 0; fixture.shadows = LightShadows.None; controller.Fixtures[i] = fixture;
                bool emergency = i >= 4 && i < FixturePolicy.WardFixtures;
                WardPrimitives.Box(go.transform, emergency ? "EmergencyLampHousing" : "FluorescentHousing",
                    go.transform.position + Vector3.up * .12f,
                    emergency ? new Vector3(.2f, .08f, .2f) : new Vector3(.8f, .06f, .18f), housing, false);
                controller.FixtureFaces[i] = WardPrimitives.Box(go.transform, emergency ? "EmergencyLens" : i == 1 ? "DamagedTubeLens" : "TubeLens",
                    go.transform.position + Vector3.up * .075f,
                    emergency ? new Vector3(.15f, .03f, .15f) : new Vector3(i == 1 ? .45f : .7f, .025f, .1f), lens, false).GetComponent<Renderer>();
            }
            var director = root.gameObject.AddComponent<ExperienceDirector>(); director.Lighting = controller;
            var strips = new System.Collections.Generic.List<Renderer>();
            var emissives = new System.Collections.Generic.List<Renderer>();
            var traces = new System.Collections.Generic.List<Renderer>();
            foreach (var sceneRoot in root.gameObject.scene.GetRootGameObjects())
                foreach (var renderer in sceneRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.name == "SoundCueGuidance") strips.Add(renderer);
                    if (renderer.name == "ExitSign" || renderer.name == "FloorExitSign") emissives.Add(renderer); // v1.3: the stairwell exit signs
                    if (renderer.name.StartsWith("StoryTrace_", System.StringComparison.Ordinal)) traces.Add(renderer);
                }
            controller.GuidanceStrip = strips.ToArray(); controller.Emissives = emissives.ToArray(); controller.StoryTraces = traces.ToArray();
            // Static emitters (650) and zombies (500) bind here in their later scene steps.
        }
    }
}
