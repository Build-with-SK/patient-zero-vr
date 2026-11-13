// Claude-owned integration (owner's step 1 feedback, 2026-11-11; E-23).
using System;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// The owner asked for dead bodies on the floor, and for a place that is empty when you look and has a body hanging in it
    /// when you look back (owner, 2026-11-11). Built at compose time after the horror pass and the stairwells (Ward 987):
    ///   bodies lying on the floor, the zombie model frozen in the last frame of its Death animation, each in a pool of blood;
    ///   two hanged bodies that appear only once the player has looked away (LookAwayScare): one in the north-west corner of B-7,
    ///   shown while the news holds the player's eyes on the television, and one at the dead end of the corridor behind them.
    /// No collider, light, AI or navigation change: the NavMesh is already baked (Ward 150) and none of this can block a path.
    /// </summary>
    public static class DeadBodiesStep
    {
        /// <summary>Where each body lies (floor x, z), half a metre or more from the wall it lies along; every wall here runs along x.</summary>
        public static readonly Vector3[] Lying =
        {
            new Vector3(3.2f, 0, 4.0f),     // the corridor, along the north wall, on the way out of B-7
            new Vector3(14.4f, 0, 2.0f),    // the vestibule before the emergency exit, along the south wall
            new Vector3(66.5f, 0, -1.3f),   // the lobby, behind the reception desk
            new Vector3(71.0f, 0, 6.2f),    // the lobby, by the north wall
        };

        /// <summary>The B-7 corner: in view as the player wakes, out of view while the news holds them on the television.</summary>
        public static readonly Vector3 RoomHanging = new Vector3(-1.35f, 0, 1.35f);
        /// <summary>The corridor's west dead end, behind a player who leaves B-7 for the stairs.</summary>
        public static readonly Vector3 CorridorHanging = new Vector3(-1.45f, 0, 3.9f);
        /// <summary>How high the hanged bodies' feet hang over the floor (metres).</summary>
        public const float FeetAbove = .35f;

        [SceneBuildStep(BuildScene.Ward, 987)]
        public static void Build(Transform root)
        {
            ZombieArt.Kit kit = ZombieArt.Prepare();
            var parent = new GameObject("Dead bodies").transform;
            parent.SetParent(root, false);
            Material pool = AssetDatabase.LoadAssetAtPath<Material>(ArtTextures.Folder + "/M_Blood_Pool.mat");
            if (!pool) throw new InvalidOperationException("The blood pool material is missing: the art pass (Ward 980) must come first.");
            Material dead = Skin(kit, "Dead_Skin", new Color(.74f, .76f, .72f, 1));

            for (int i = 0; i < Lying.Length; i++)
            {
                Vector3 floor = Floor(Lying[i]);
                RequireRoom(floor, "body " + i);
                Transform body = ZombieArt.Posed(kit, parent, "Dead body " + (i + 1), "Death", 99f, i % 2 == 0 ? dead : kit.Skins[2], out Vector3 size);
                // Lengthways along the wall (x), head one way or the other, a few degrees off so they do not look laid out.
                float yaw = (size.x >= size.z ? 0 : 90) + (i % 2) * 180 + (i * 37 % 11 - 5);
                body.SetPositionAndRotation(floor, Quaternion.Euler(0, yaw, 0));
                // E-24 (owner, 2026-11-11: hands and heads through the walls): keep the whole pose inside its room.
                Vector3 shift = IntoRoom(ZombieArt.WorldBounds(body), floor);
                body.position += shift;
                if (shift.sqrMagnitude > 1e-6f) Debug.Log("[PZ] dead body " + (i + 1) + " moved " + shift.magnitude.ToString("0.00") + " m off the wall");
                Pool(body, pool, body.position, 1.25f + .15f * (i % 2));
            }

            Material rope = Rope();
            // Facing the bed, and facing back down the corridor.
            // E-24 (owner, 2026-11-11: "i didn't see the body hanging in the room"): the hanged bodies' skin glows faintly, so a
            // dark corner cannot swallow them. Material emission, not a light: lights belong to Atmosphere and Environment.
            Hang(root, kit, Glow(Skin(kit, "Hanged_Skin", new Color(.80f, .82f, .78f, 1))), rope, RoomHanging, WardLayout.A1_Start.Position - RoomHanging, "B-7 corner");
            Hang(root, kit, Glow(Copy(kit.Skins[1], "Hanged_Skin_2")), rope, CorridorHanging, Vector3.right, "corridor dead end");
            Debug.Log("[PZ] dead bodies: " + Lying.Length + " on the floor, 2 hanged (look-away)");
        }

        private static void Hang(Transform root, ZombieArt.Kit kit, Material skin, Material rope, Vector3 at, Vector3 facing, string label)
        {
            RequireRoom(at, label);
            float ceiling = Ceiling(at);
            Vector3 top = new Vector3(at.x, ceiling, at.z);
            var holder = new GameObject("Look-away scare (" + label + ")").transform;
            holder.SetParent(root, false);
            Transform swing = ZombieArt.Hanged(kit, holder, skin, rope, top, facing, Floor(at).y, FeetAbove, out Bounds hung);
            var scare = holder.gameObject.AddComponent<LookAwayScare>();
            scare.body = swing;
            scare.label = label;
            scare.seenFirst = true;
            // The chest: two thirds of the way up the body as it hangs.
            scare.lookAt = new Vector3(at.x, hung.min.y + hung.size.y * .66f, at.z);
            Debug.Log("[PZ] hanged body '" + label + "': feet " + (hung.min.y - Floor(at).y).ToString("0.00") + " m over the floor, " +
                      hung.size.y.ToString("0.00") + " m tall, ceiling " + ceiling.ToString("0.00") + " m, eyes on " + scare.lookAt.y.ToString("0.00") + " m");
            swing.gameObject.SetActive(false);
        }

        internal static void Pool(Transform body, Material material, Vector3 floor, float size)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BloodPool";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(body, false);
            quad.transform.SetPositionAndRotation(floor + Vector3.up * .004f, Quaternion.LookRotation(Vector3.down, body.forward));
            quad.transform.localScale = new Vector3(size, size, 1);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>The floor under (x, z), from the composed scene's colliders.</summary>
        internal static Vector3 Floor(Vector3 p)
        {
            Physics.SyncTransforms();
            if (!Physics.Raycast(new Vector3(p.x, 1.5f, p.z), Vector3.down, out RaycastHit hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("No floor under the dead body at " + p + " (layout changed?).");
            return hit.point;
        }

        private static float Ceiling(Vector3 p)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(new Vector3(p.x, 1.5f, p.z), Vector3.up, out RaycastHit hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y > 2.2f
                ? hit.point.y
                : 2.775f;
        }

        /// <summary>Wall faces stand this far inside a room's rectangle, plus a little air.</summary>
        private const float WallClear = .15f;

        /// <summary>The horizontal shift that brings <paramref name="b"/> inside the room holding <paramref name="p"/>.</summary>
        internal static Vector3 IntoRoom(Bounds b, Vector3 p)
        {
            foreach (RoomRect room in WardLayout.Rooms)
            {
                if (!room.Contains(p, 0)) continue;
                float dx = b.min.x < room.MinX + WallClear ? room.MinX + WallClear - b.min.x : b.max.x > room.MaxX - WallClear ? room.MaxX - WallClear - b.max.x : 0;
                float dz = b.min.z < room.MinZ + WallClear ? room.MinZ + WallClear - b.min.z : b.max.z > room.MaxZ - WallClear ? room.MaxZ - WallClear - b.max.z : 0;
                return new Vector3(dx, 0, dz);
            }

            return Vector3.zero;
        }

        internal static void RequireRoom(Vector3 p, string what)
        {
            foreach (RoomRect room in WardLayout.Rooms)
            {
                if (room.Contains(p, .3f)) return;
            }

            throw new InvalidOperationException("The " + what + " at " + p + " is in no room of WardLayout (layout changed?).");
        }

        internal static Material Skin(ZombieArt.Kit kit, string name, Color tint)
        {
            Material m = ArtSurfaces.LitMaterial(name);
            m.CopyPropertiesFromMaterial(kit.Skin);
            m.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Copy(Material source, string name)
        {
            Material m = ArtSurfaces.LitMaterial(name);
            m.CopyPropertiesFromMaterial(source);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>E-24: the skin's own texture, faintly self-lit (cold), so the body reads in the dark. Not baked into GI.</summary>
        private static Material Glow(Material m)
        {
            // A faint cold lift with no texture: the first try lit the skin texture itself, and the body glowed bright green.
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", null);
            m.SetColor("_EmissionColor", new Color(.05f, .055f, .065f, 1));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Rope()
        {
            Material m = ArtSurfaces.LitMaterial("Rope");
            m.SetTexture("_BaseMap", null);
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
            m.SetColor("_BaseColor", new Color(.30f, .24f, .16f, 1));
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", .1f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
