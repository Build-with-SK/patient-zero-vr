// UNVERIFIED IN EDITOR
using PatientZero.Core;
using PatientZero.Environment.Logic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace PatientZero.Environment.Editor
{
    /// <summary>Approved ward greybox: isolation B-7, connecting corridor, vestibule and ancillary rooms.</summary>
    public static class HospitalBuildSteps
    {
        [SceneBuildStep(BuildScene.Ward, 100)]
        public static void Build(Transform root)
        {
            var geometry = new RoomGeometry();
            var wall = WardPrimitives.Material("WardPaint", new Color(.28f, .34f, .32f));
            var floor = WardPrimitives.Material("WardTile", new Color(.24f, .25f, .23f));
            var metal = WardPrimitives.Material("MedicalMetal", new Color(.25f, .27f, .28f));
            foreach (var r in geometry.Floors)
            {
                var centre = new Vector3((r.MinX + r.MaxX) / 2, 0, (r.MinZ + r.MaxZ) / 2);
                var size = new Vector3(r.MaxX - r.MinX, .15f, r.MaxZ - r.MinZ);
                WardPrimitives.Box(root, r.Id + "_Floor", centre - Vector3.up * .075f, size, floor);
                WardPrimitives.Box(root, r.Id + "_Ceiling", centre + Vector3.up * 2.85f, size, wall);
            }
            foreach (var span in geometry.Walls)
            {
                var size = span.ColliderSize;
                WardPrimitives.Box(root, "Wall", (span.A + span.B) * .5f + Vector3.up * 1.4f, size, wall);
            }
            foreach (var door in geometry.Doors)
            {
                Vector3 size = door.AlongX ? new Vector3(door.Width, .4f, .12f) : new Vector3(.12f, .4f, door.Width);
                WardPrimitives.Box(root, "DoorHeader_" + door.RoomA + "_" + door.RoomB, door.Centre + Vector3.up * 2.6f, size, metal);
            }
            foreach (var p in WardLayout.Points)
                if (p.Kind == LayoutKind.ZombieMarker) WardPrimitives.Marker(root, p);
            var monitor = WardPrimitives.Marker(root, WardLayout.A2_Monitor);
            WardPrimitives.Box(monitor, "Monitor", monitor.position, new Vector3(.5f, .28f, .04f), metal);
            Label(monitor, "ISOLATION B-7\nSUBJECT P-0\nASYMPTOMATIC", monitor.position, .025f);
            var sign = WardPrimitives.Marker(root, WardLayout.A3_Beacon);
            var emissive = WardPrimitives.Material("ExitEmission", new Color(.08f, .5f, .12f));
            emissive.EnableKeyword("_EMISSION"); emissive.SetColor("_EmissionColor", new Color(.08f, .5f, .12f));
            EditorUtility.SetDirty(emissive); AssetDatabase.SaveAssets();
            WardPrimitives.Box(sign, "ExitSign", sign.position, new Vector3(.05f, .25f, .5f), emissive, false);
            Label(sign, "EXIT ->", sign.position - Vector3.right * .06f, .045f, 90);
            var route = new[] { WardLayout.A3_Start, WardLayout.A3_Mid1, WardLayout.A3_Mid2, WardLayout.A3_End };
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector3 a = route[i].Position, b = route[i + 1].Position;
                var strip = WardPrimitives.Box(root, "SoundCueGuidance", (a + b) * .5f + Vector3.up * .015f,
                    new Vector3(.05f, .01f, Vector3.Distance(a, b)), emissive, false);
                strip.transform.rotation = Quaternion.LookRotation(b - a); strip.GetComponent<Renderer>().enabled = false;
            }
            var drip = WardPrimitives.Marker(root, WardLayout.A3_Drip);
            WardPrimitives.Box(drip, "Pipe", drip.position, new Vector3(.08f, .3f, .08f), metal, false);
            // A supplies the only colliding/physical trolley. Keep our landmark, not overlapping geometry.
            WardPrimitives.Marker(root, WardLayout.A3_Cart);
            Gurney(root, metal);
            var dressingMaterials = new[] { metal,
                WardPrimitives.Material("PrivacyFabric", new Color(.18f, .3f, .29f)),
                WardPrimitives.Material("ClinicalCeramic", new Color(.55f, .58f, .52f)),
                WardPrimitives.Material("WardWayfinding", new Color(.18f, .42f, .35f)),
                WardPrimitives.Material("RestrainedTrace", new Color(.12f, .055f, .04f)),
                WardPrimitives.Material("SurfaceDamage", new Color(.13f, .15f, .14f)) };
            foreach (var prop in HospitalDressing.Create())
                WardPrimitives.Box(root, prop.Name, prop.Position, prop.Size, dressingMaterials[(int)prop.Style], prop.Collider);
            Label(root, "OPERATING THEATRE", WardLayout.Z2_Doorway.Position + Vector3.up * 2.45f, .045f, 180);
            Label(root, "EMERGENCY EXIT ->", WardLayout.A3_End.Position + Vector3.up * 2.4f, .045f, 90);
            // Seat A builds the functional door leaves and puzzle objects at the shared LayoutIds.
            // Theatre furniture adds no light, dynamic body, camera effect or encounter. All beats remain in DirectorCore.
            Label(root, "REMOVE THE 3 PLANKS", WardLayout.A1_DoorWindow.Position + Vector3.up * .6f - Vector3.forward * .08f, .04f);
            Label(root, "MOVE QUIETLY / LISTEN", WardLayout.A2_Monitor.Position - Vector3.up * .4f - Vector3.forward * .04f, .035f);
            Label(root, "B-7: NO FEVER\nCONTAINMENT CONTINUES", WardLayout.A1_Tray.Position + Vector3.up * .2f, .022f);
            Label(root, "EMERGENCY RELEASE", WardLayout.A4_ReleaseLever.Position + Vector3.up * .45f - Vector3.right * .07f, .035f, 90);
            Label(root, "THEY HEAR YOU", WardLayout.A1_PlankMid.Position - Vector3.forward * .08f, .04f);
            Label(root, "B-7", WardLayout.A1_DoorWindow.Position + Vector3.up * .45f, .055f);
            foreach (var p in new[] { WardLayout.A1_DoorWindow, WardLayout.A2_Monitor, WardLayout.A3_Drip, WardLayout.A4_ExitDoor })
                WardPrimitives.Box(root, "PA_" + p.Id, p.Position + Vector3.up * .3f, new Vector3(.18f, .1f, .1f), metal, false);
            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.agentTypeID = 0;
        }
        [SceneBuildStep(BuildScene.Ward, 150)]
        public static void BakeNavigation(Transform root)
        {
            var surface = WardPrimitives.FindInScene<NavMeshSurface>(root);
            if (!surface) throw new System.InvalidOperationException("Ward geometry must precede NavMesh baking");
            var bake = UnityEngine.AI.NavMesh.GetSettingsByID(surface.agentTypeID);
            if (Mathf.Abs(bake.agentRadius - .25f) > .001f)
                throw new System.InvalidOperationException("Ward requires bake radius 0.25 m (INT-B-1). Run Claude's NavigationSettings through PatientZero/Build Scenes; changing runtime agent.radius is insufficient.");
            surface.BuildNavMesh();
            if (!surface.navMeshData) throw new System.InvalidOperationException("Ward bake produced no NavMeshData");
            string path = WardPrimitives.GeneratedFolder("Environment") + "/WardNavMesh.asset";
            var previous = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(path);
            if (previous)
            {
                // Preserve the GUID referenced by existing generated scenes.
                EditorUtility.CopySerialized(surface.navMeshData, previous);
                surface.RemoveData(); surface.navMeshData = previous; surface.AddData();
                EditorUtility.SetDirty(previous);
            }
            else AssetDatabase.CreateAsset(surface.navMeshData, path);
            AssetDatabase.SaveAssets();
        }
        private static void Gurney(Transform root, Material metal)
        {
            Vector3 p = WardLayout.Z4_Gurney.Position;
            // Offset away from the critical path, with its head on the approved spawn marker.
            WardPrimitives.Box(root, "Gurney", p + Vector3.up * .65f, new Vector3(1.1f, .12f, .6f), metal);
            WardPrimitives.Box(root, "Mattress", p + Vector3.up * .75f, new Vector3(1, .1f, .55f), metal, false);
            Vector3 towards = (WardLayout.A4_Release.Position - p).normalized;
            for (int link = 1; link <= 13; link++)
            {
                Vector3 at = p + towards * (WardLayout.FinalPatientReach * link / 13) + Vector3.up * .08f;
                WardPrimitives.Box(root, "ChainLink", at, new Vector3(.08f, .025f, .025f), metal, false);
            }
            Label(root, "RESTRAINED - KEEP CLEAR", p + Vector3.up * .2f, .03f);
        }
        private static void Label(Transform root, string text, Vector3 p, float size, float yaw = 0)
        {
            var go = new GameObject("NarrativeText"); go.transform.SetParent(root, false); go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.characterSize = size; mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.color = Color.white;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<Renderer>().sharedMaterial = mesh.font.material;
        }
    }
}
