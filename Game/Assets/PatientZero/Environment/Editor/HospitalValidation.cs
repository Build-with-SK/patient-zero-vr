using System;
using System.Collections.Generic;
using PatientZero.AI;
using PatientZero.Atmosphere;
using PatientZero.Audio;
using PatientZero.Core;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace PatientZero.Environment.Editor
{
    /// <summary>Read-only checks on generated content. Never changes scenes or project settings.</summary>
    public static class HospitalValidation
    {
        [MenuItem("PatientZero/Hospital/Validate Generated Hospital")]
        public static void ValidateGenerated()
        {
            // Refuse to close unsaved work; the integrator can save it and retry.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save modified scenes before validating the generated hospital.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var bootstrap = EditorSceneManager.OpenScene("Assets/PatientZero/Integration/Generated/Bootstrap.unity", OpenSceneMode.Single);
                var ward = EditorSceneManager.OpenScene("Assets/PatientZero/Integration/Generated/Ward.unity", OpenSceneMode.Additive);
                var errors = Check(bootstrap, ward);
                foreach (string error in errors) Debug.LogError("Hospital validation: " + error);
                if (errors.Count != 0) throw new InvalidOperationException(errors.Count + " hospital validation error(s).");
                Debug.Log("Hospital validation passed: saved navigation, routes, actors, lighting and audio bindings.");
            }
            finally
            {
                if (setup.Length > 0 && System.Array.Exists(setup, s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [MenuItem("PatientZero/Hospital/Validate Saved Navigation Asset")]
        public static void ValidateNavigationAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/PatientZero/Environment/Generated/WardNavMesh.asset");
            if (!asset) throw new InvalidOperationException("Generate the Ward NavMesh asset first.");
            var instance = NavMesh.AddNavMeshData(asset);
            try
            {
                var errors = new List<string>();
                CheckRoute(WardLayout.Z1_WalkFrom.Position, WardLayout.A1_DoorWindow.Position + Vector3.forward * .45f, "window approach", errors);
                CheckRoute(WardLayout.Z2_Dormant.Position, WardLayout.Z2_Doorway.Position, "theatre doorway", errors);
                CheckRoute(WardLayout.Z3_PatrolNorth.Position, WardLayout.Z3_PatrolSouth.Position, "corridor crossing", errors);
                foreach (string error in errors) Debug.LogError(error);
                if (errors.Count > 0) throw new InvalidOperationException("Saved navigation has " + errors.Count + " unreachable route(s).");
                Debug.Log("Saved navigation routes passed.");
            }
            finally { instance.Remove(); }
        }

        public static List<string> Check(Scene bootstrap, Scene ward)
        {
            var errors = new List<string>();
            var audio = Find<AudioCuePlayer>(bootstrap);
            if (audio.Count != 1 || !audio[0].Mixer) errors.Add("Bootstrap needs one AudioCuePlayer with a mixer.");
            var directors = Find<ExperienceDirector>(ward);
            if (directors.Count != 1 || !directors[0].Lighting || !directors[0].Emitters)
                errors.Add("Ward director needs its lighting and audio emitter references.");
            var lights = Find<LightingController>(ward);
            if (lights.Count != 1 || lights[0].Fixtures.Length != 6) errors.Add("Expected six bound shadowless fixtures.");
            else foreach (var fixture in lights[0].Fixtures)
                if (!fixture || fixture.shadows != LightShadows.None) errors.Add("Fixture missing or dynamic shadows enabled.");
            var headlamps = Find<PatientZero.Atmosphere.Mobile.NonVrHeadlampInput>(ward);
            if (headlamps.Count != 1 || lights.Count != 1 || headlamps[0].Lighting != lights[0])
                errors.Add("Mobile headlamp input must bind the existing LightingController; regenerate Ward.");
            if (lights.Count == 1)
            {
                if (lights[0].FixtureFaces.Length != 6) errors.Add("Expected six visible fixture faces; regenerate Ward.");
                else foreach (var face in lights[0].FixtureFaces) if (!face) errors.Add("Fixture lens renderer missing.");
                if (lights[0].Fixtures.Length > 0 && lights[0].Fixtures[0] && lights[0].Fixtures[0].transform.childCount != 0)
                    errors.Add("Headlamp emitter must not carry ceiling meshes; regenerate Ward.");
            }
            var masks = WardCollisionBinding.Resolve();
            if (masks.Hearing == 0)
                Debug.LogWarning("Door-only layer PZ_ClosedDoor is unassigned (INT-B-2); hearing door attenuation remains unverified. Integrator must bind only door leaves to that layer and rebuild.");
            var actors = Find<ZombieAgentShell>(ward);
            var ids = new HashSet<int>();
            foreach (var actor in actors)
            {
                if (!ids.Add(actor.ZombieId) || actor.ZombieId < 1 || actor.ZombieId > 4) errors.Add("Duplicate/invalid zombie id.");
                if (!actor.Head || !actor.FootEmitter || !actor.Animation) errors.Add("Zombie visual/audio references missing.");
                var nav = actor.GetComponent<ZombieNavigation>();
                if (!nav || nav.Obstacles.value != masks.Movement || actor.HearingOccluders.value != masks.Hearing || actor.SightBlockers.value != masks.Sight)
                    errors.Add("Zombie collision masks differ from the generated door/geometry policy.");
            }
            if (actors.Count != 4) errors.Add("Expected four zombie actors.");
            if (audio.Count == 1 && audio[0].Occluders.value != masks.Audio) errors.Add("Audio occlusion mask differs from geometry/door policy.");
            var surfaces = Find<NavMeshSurface>(ward);
            if (surfaces.Count != 1 || !surfaces[0].navMeshData) errors.Add("Saved NavMeshData missing.");
            else
            {
                if (AssetDatabase.GetAssetPath(surfaces[0].navMeshData) != "Assets/PatientZero/Environment/Generated/WardNavMesh.asset")
                    errors.Add("Navigation data is not the persisted Ward asset.");
                if (Mathf.Abs(NavMesh.GetSettingsByID(surfaces[0].agentTypeID).agentRadius - .25f) > .001f)
                    errors.Add("Bake radius must be 0.25 m (INT-B-1).");
                CheckRoute(WardLayout.Z1_WalkFrom.Position, WardLayout.A1_DoorWindow.Position + Vector3.forward * .45f, "window approach", errors);
                CheckRoute(WardLayout.Z2_Dormant.Position, WardLayout.Z2_Doorway.Position, "theatre doorway", errors);
                CheckRoute(WardLayout.Z3_PatrolNorth.Position, WardLayout.Z3_PatrolSouth.Position, "corridor crossing", errors);
            }
            foreach (var tag in Find<LayoutTag>(ward))
                if (tag.LayoutId == WardLayout.A3_Cart.Id && tag.GetComponentsInChildren<Collider>(true).Length != 0)
                    errors.Add("B's A3.Cart landmark must have no static cart collider; A supplies the physics trolley.");
            return errors;
        }

        private static void CheckRoute(Vector3 start, Vector3 end, string name, List<string> errors)
        {
            end.y = start.y; // Landmark height is not a navigation destination height.
            var path = new NavMeshPath();
            bool hasStart = NavMesh.SamplePosition(start, out var a, .5f, NavMesh.AllAreas);
            bool hasEnd = NavMesh.SamplePosition(end, out var b, .5f, NavMesh.AllAreas);
            if (!hasStart || !hasEnd || !NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                errors.Add("No complete route: " + name + "; sampled start=" + hasStart + " " + a.position +
                    "; sampled end=" + hasEnd + " " + b.position + "; status=" + path.status);
        }
        private static List<T> Find<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }
    }
}
