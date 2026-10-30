// UNVERIFIED IN EDITOR
using PatientZero.Core;
using PatientZero.Environment.Editor;
using UnityEngine;

namespace PatientZero.Atmosphere.Editor
{
    public static class AtmosphereBuildStep
    {
        /// <summary>Composer step: six shadowless, limited-range fixtures and the Ward director.</summary>
        [SceneBuildStep(BuildScene.Ward, 200)]
        public static void Build(Transform root)
        {
            var controller = root.gameObject.AddComponent<LightingController>();
            controller.Fixtures = new Light[6];
            var points = new[] { WardLayout.A1_Tray, WardLayout.A1_DoorWindow, WardLayout.A2_ValveWheel,
                WardLayout.A3_Mid1, WardLayout.A4_Start, WardLayout.Z4_Gurney };
            var housing = WardPrimitives.Material("FixtureHousing", new Color(.18f, .2f, .19f));
            for (int i = 0; i < points.Length; i++)
            {
                var go = new GameObject("Fixture_" + points[i].Id); go.transform.SetParent(root, false);
                go.transform.position = new Vector3(points[i].Position.x, 2.5f, points[i].Position.z);
                var fixture = go.AddComponent<Light>(); fixture.type = LightType.Point; fixture.range = i < 2 ? 3 : 2.8f;
                fixture.intensity = 0; fixture.shadows = LightShadows.None; controller.Fixtures[i] = fixture;
                WardPrimitives.Box(go.transform, i >= 4 ? "EmergencyLampHousing" : "FluorescentHousing",
                    go.transform.position + Vector3.up * .12f,
                    i >= 4 ? new Vector3(.2f, .08f, .2f) : new Vector3(.8f, .06f, .18f), housing, false);
            }
            var director = root.gameObject.AddComponent<ExperienceDirector>(); director.Lighting = controller;
            var strips = new System.Collections.Generic.List<Renderer>();
            var emissives = new System.Collections.Generic.List<Renderer>();
            foreach (var sceneRoot in root.gameObject.scene.GetRootGameObjects())
                foreach (var renderer in sceneRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.name == "SoundCueGuidance") strips.Add(renderer);
                    if (renderer.name == "ExitSign") emissives.Add(renderer);
                }
            controller.GuidanceStrip = strips.ToArray(); controller.Emissives = emissives.ToArray();
            // Static emitters (650) and zombies (500) bind here in their later scene steps.
        }
    }
}
