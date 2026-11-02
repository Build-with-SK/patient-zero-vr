// Claude-owned integration (horror pass, 2026-11-02; E-21). Owner request: "there should be step I don't want to teleport to
// different floor".
using System;
using System.Collections.Generic;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Builds the two walkable stairwells (Floor 3 to 2, Floor 2 to 1) that <see cref="Stairwell"/> runs. Each opens through a
    /// new doorway in the east wall of a floor's stair room and arrives through a new doorway in the west wall of the stair
    /// room below. Unrolled, one stairwell is: a landing, nine steps and a half landing down (1.8 m), a fire-door lobby, a half
    /// landing and nine steps down (1.8 m), a landing. The top half and the lobby are built where the upper floor is; the lobby
    /// again and the bottom half 3.6 m higher, so that the bottom half ends level with the lower floor (see Stairwell).
    /// Runs after the art pass (980) and gives every piece the art pass's materials itself; the two lobbies take their texture
    /// coordinates from their own copy's origin, so they match exactly. The steps are for the eye; the feet walk an invisible
    /// ramp along the nosings. No light is added (Atmosphere owns lights): glow-in-the-dark strips mark every step, and the
    /// lobbies stand more than a light's range from every fixture, so both copies are lit alike (by the player's torch).
    /// </summary>
    public static class StairwellStep
    {
        public const float Rise = .18f, Run = .28f, Width = 1.8f, Wall = .1f, DoorWidth = 1.2f, DoorHeight = 2.2f;
        public const int Steps = 9;                       // and the tenth riser onto the half landing
        public const float HalfLanding = .6f, LobbyDepth = 2.4f, Headroom = 2.6f, RoomCeiling = 2.775f;
        public const float Drop = (Steps + 1) * Rise;      // 1.8 m each half
        public const float Copy = 2 * Drop;                // the lower copy stands this much higher

        private sealed class StairKit
        {
            public Material Floor, Wall, Ceiling, Metal, Door, Glow;
            public Transform Parent;
            public Vector3 Origin;   // the copy's origin: texture coordinates count from here
            public Vector3 Frame;    // world position of local (0, 0, 0): the top doorway in the upper room's wall
            public int Pieces;
        }

        [SceneBuildStep(BuildScene.Ward, 986)]
        public static void Build(Transform root)
        {
            GameObject[] sceneRoots = root.gameObject.scene.GetRootGameObjects();
            var art = new ArtSurfaces();
            var kit = new StairKit { Floor = art.Floor, Wall = art.Wall, Ceiling = art.Ceiling, Metal = art.Metal, Door = art.DoorPaint, Glow = Glow() };
            int pieces = 0;
            pieces += BuildStairwell(root, sceneRoots, kit, "Stair3", "Stair2", 2);
            pieces += BuildStairwell(root, sceneRoots, kit, "Stair2Down", "Stair1", 1);
            AssetDatabase.SaveAssets();
            Debug.Log("[PZ] stairwells: 2 walkable (Floor 3 to 2, Floor 2 to 1), " + pieces + " pieces, " + (Steps + 1) * 2 + " risers each");
        }

        private static int BuildStairwell(Transform root, GameObject[] sceneRoots, StairKit kit, string upperRoom, string lowerRoom, int lowerFloor)
        {
            RoomRect upper = Room(upperRoom), lower = Room(lowerRoom);
            float zc = (upper.MinZ + upper.MaxZ) * .5f;
            if (Mathf.Abs(zc - (lower.MinZ + lower.MaxZ) * .5f) > .01f) throw new InvalidOperationException(upperRoom + " and " + lowerRoom + " are not in line.");
            float span = lower.MinX - upper.MaxX;
            float flight = Steps * Run + HalfLanding;
            float landing = (span - 2 * flight - LobbyDepth) * .5f;
            if (landing < .45f) throw new InvalidOperationException("No room for a stairwell between " + upperRoom + " and " + lowerRoom + ": " + span + " m.");

            // Local x along the stairwell, from the upper room's wall.
            float s1 = landing;                     // first step
            float xb = s1 + flight;                 // lobby: first door
            float xe = xb + LobbyDepth;             // lobby: second door
            float s2 = xe + HalfLanding;            // first step of the bottom half
            float xl = span;                        // the lower room's wall

            var well = new GameObject("Stairwell " + upperRoom + " to " + lowerRoom).transform;
            well.SetParent(root, false);
            kit.Parent = well;
            kit.Frame = new Vector3(upper.MaxX, 0, zc);
            kit.Pieces = 0;

            Doorway(sceneRoots, kit.Frame, true);
            Doorway(sceneRoots, kit.Frame + Vector3.right * span, false);

            var doors = new List<Stairwell.Door>();
            Vector3 up = Vector3.up * Copy;

            // ---- the top half, where the upper floor is
            kit.Origin = kit.Frame;
            Slab(kit, "Landing", 0, s1, 0, kit.Floor, true);
            for (int i = 1; i <= Steps; i++) Step(kit, s1 + (i - 1) * Run, -i * Rise, -Drop - .1f);
            Slab(kit, "Half landing", s1 + Steps * Run, xb, -Drop, kit.Floor, true);
            Nosing(kit, s1, 0);
            for (int i = 1; i <= Steps; i++) Nosing(kit, s1 + i * Run, -i * Rise);
            Ramp(kit, s1, 0);
            Rail(kit, s1, 0);
            Sides(kit, "Shaft", 0, xb, -Drop - .1f, RoomCeiling + .1f);
            Ceiling(kit, 0, xb, RoomCeiling);
            Piece(kit, "Shaft end", new Vector3(xb, (-Drop + Headroom + RoomCeiling + .1f) * .5f, 0), new Vector3(Wall, RoomCeiling + .1f + Drop - Headroom, Width + 2 * Wall), kit.Wall, true);
            doors.Add(Door(kit, "Top door", .07f, 0, Stairwell.Opens.OnceDown));
            int upperLive = doors.Count; doors.Add(Default);
            int upperDead = doors.Count; doors.Add(Default);
            Lobby(kit, xb, xe, doors, upperLive, upperDead, true);

            // ---- the bottom half, Copy metres higher, ending level with the lower floor
            kit.Origin = kit.Frame + up;
            int lowerDead = doors.Count; doors.Add(Default);
            int lowerLive = doors.Count; doors.Add(Default);
            Lobby(kit, xb, xe, doors, lowerDead, lowerLive, false);
            Slab(kit, "Half landing", xe, s2, -Drop, kit.Floor, true);
            for (int i = 1; i <= Steps; i++) Step(kit, s2 + (i - 1) * Run, -Drop - i * Rise, -Copy - .1f);
            Slab(kit, "Landing", s2 + Steps * Run, xl, -Copy, kit.Floor, true);
            Nosing(kit, s2, -Drop);
            for (int i = 1; i <= Steps; i++) Nosing(kit, s2 + i * Run, -Drop - i * Rise);
            Ramp(kit, s2, -Drop);
            Rail(kit, s2, -Drop);
            Sides(kit, "Shaft", xe, xl, -Copy - .1f, -Drop + Headroom + .1f);
            Ceiling(kit, xe, xl, -Drop + Headroom);
            Piece(kit, "Shaft end", new Vector3(xe, (-Copy - .1f - Drop - .1f) * .5f, 0), new Vector3(Wall, Copy - Drop, Width + 2 * Wall), kit.Wall, true);
            doors.Add(Door(kit, "Bottom door", xl - .07f, -Copy, Stairwell.Opens.Always));

            var runner = well.gameObject.AddComponent<Stairwell>();
            runner.doors = doors.ToArray();
            runner.lowerFloor = lowerFloor;
            runner.Rise = Copy;
            runner.upper = LobbyArea(kit.Frame, xb, xe, upperLive, upperDead);
            runner.lower = LobbyArea(kit.Frame + up, xb, xe, lowerLive, lowerDead);
            return kit.Pieces;
        }

        private static readonly Stairwell.Door Default = default;

        private static Stairwell.Lobby LobbyArea(Vector3 origin, float xb, float xe, int live, int dead) => new Stairwell.Lobby
        {
            Min = origin + new Vector3(xb + .05f, -Drop, -Width * .5f),
            Max = origin + new Vector3(xe - .05f, -Drop, Width * .5f),
            Live = live,
            Dead = dead,
        };

        /// <summary>The fire-door lobby, the same in both copies: only which door works differs.</summary>
        private static void Lobby(StairKit kit, float xb, float xe, List<Stairwell.Door> doors, int west, int east, bool upperCopy)
        {
            float floor = -Drop, top = -Drop + Headroom;
            Slab(kit, "Lobby floor", xb, xe, floor, kit.Floor, true);
            Ceiling(kit, xb, xe, top);
            Sides(kit, "Lobby", xb, xe, floor - .1f, top + .1f);
            Frame(kit, xb, floor, top);
            Frame(kit, xe, floor, top);
            // In the top copy the west door (from the stairs above) works; in the bottom copy the east one (to the stairs below).
            doors[west] = Door(kit, "Lobby door west", xb, floor, upperCopy ? Stairwell.Opens.Always : Stairwell.Opens.Never);
            doors[east] = Door(kit, "Lobby door east", xe, floor, upperCopy ? Stairwell.Opens.Never : Stairwell.Opens.Always);
            // A running-man sign over each door, lit from inside (emissive), the same in both copies.
            Piece(kit, "Exit sign", new Vector3(xb + .06f, floor + DoorHeight + .16f, 0), new Vector3(.04f, .14f, .36f), kit.Glow, false);
            Piece(kit, "Exit sign", new Vector3(xe - .06f, floor + DoorHeight + .16f, 0), new Vector3(.04f, .14f, .36f), kit.Glow, false);
        }

        // ------------------------------------------------------------------ pieces

        private static Transform Piece(StairKit kit, string name, Vector3 local, Vector3 size, Material material, bool collide)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(kit.Parent, false);
            go.transform.position = kit.Origin + local;
            go.transform.localScale = size;
            if (!collide) Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.GetComponent<MeshFilter>().sharedMesh = ArtSurfaces.UvBox(go.transform, kit.Origin);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            kit.Pieces++;
            return go.transform;
        }

        /// <summary>A floor slab from x0 to x1 whose top is at <paramref name="top"/>.</summary>
        private static void Slab(StairKit kit, string name, float x0, float x1, float top, Material material, bool collide) =>
            Piece(kit, name, new Vector3((x0 + x1) * .5f, top - .075f, 0), new Vector3(x1 - x0, .15f, Width), material, collide);

        private static void Ceiling(StairKit kit, float x0, float x1, float underside) =>
            Piece(kit, "Stairwell_Ceiling", new Vector3((x0 + x1) * .5f, underside + .075f, 0), new Vector3(x1 - x0, .15f, Width + 2 * Wall), kit.Ceiling, false);

        private static void Sides(StairKit kit, string name, float x0, float x1, float bottom, float top)
        {
            foreach (float side in new[] { -1f, 1f })
                Piece(kit, name + " wall", new Vector3((x0 + x1) * .5f, (bottom + top) * .5f, side * (Width + Wall) * .5f), new Vector3(x1 - x0, top - bottom, Wall), kit.Wall, true);
        }

        /// <summary>One solid step, from its tread down to <paramref name="bottom"/> (nothing below it is ever seen).</summary>
        private static void Step(StairKit kit, float x0, float top, float bottom) =>
            Piece(kit, "Step", new Vector3(x0 + Run * .5f, (top + bottom) * .5f, 0), new Vector3(Run, top - bottom, Width), kit.Floor, false);

        /// <summary>A glow-in-the-dark strip along a step's edge, as real stairwells have for a power cut.</summary>
        private static void Nosing(StairKit kit, float x, float top) =>
            Piece(kit, "Glow strip", new Vector3(x - .03f, top + .004f, 0), new Vector3(.05f, .01f, Width - .16f), kit.Glow, false);

        /// <summary>The walking surface: one invisible slope through the step edges, from (x0, top) down one half.</summary>
        private static void Ramp(StairKit kit, float x0, float top)
        {
            float run = (Steps + 1) * Run, angle = Mathf.Atan2(Drop, run);
            var normal = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
            var ramp = new GameObject("Stair ramp (walkable)").transform;
            ramp.SetParent(kit.Parent, false);
            ramp.position = kit.Origin + new Vector3(x0 + run * .5f, top - Drop * .5f, 0) - normal * .1f;
            ramp.rotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
            var box = ramp.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(Mathf.Sqrt(run * run + Drop * Drop) + .1f, .2f, Width);
        }

        private static void Rail(StairKit kit, float x0, float top)
        {
            float run = (Steps + 1) * Run, angle = Mathf.Atan2(Drop, run);
            Transform rail = Piece(kit, "Handrail", new Vector3(x0 + run * .5f, top - Drop * .5f + .9f, Width * .5f - .06f),
                new Vector3(Mathf.Sqrt(run * run + Drop * Drop), .05f, .05f), kit.Metal, false);
            rail.rotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
            rail.GetComponent<MeshFilter>().sharedMesh = ArtSurfaces.UvBox(rail, kit.Origin);
        }

        /// <summary>A cross wall at x with a doorway in it, as high as the lobby.</summary>
        private static void Frame(StairKit kit, float x, float floor, float top)
        {
            float jamb = (Width - DoorWidth) * .5f + Wall;
            foreach (float side in new[] { -1f, 1f })
                Piece(kit, "Door jamb", new Vector3(x, (floor - .1f + top + .1f) * .5f, side * (DoorWidth * .5f + jamb * .5f)), new Vector3(Wall, top - floor + .2f, jamb), kit.Wall, true);
            Piece(kit, "Door header", new Vector3(x, (floor + DoorHeight + top + .1f) * .5f, 0), new Vector3(Wall, top + .1f - floor - DoorHeight, DoorWidth), kit.Wall, true);
            Piece(kit, "Door frame", new Vector3(x, floor + DoorHeight + .03f, 0), new Vector3(Wall + .04f, .06f, DoorWidth + .08f), kit.Metal, false);
        }

        /// <summary>A pair of sliding panels in the doorway at x; Stairwell slides them apart along z.</summary>
        private static Stairwell.Door Door(StairKit kit, string name, float x, float floor, Stairwell.Opens opens)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(kit.Parent, false);
            pivot.position = kit.Origin + new Vector3(x, floor, 0);
            Transform left = Piece(kit, "Door panel", new Vector3(x, floor + DoorHeight * .5f, -DoorWidth * .25f), new Vector3(.05f, DoorHeight - .01f, DoorWidth * .5f - .01f), kit.Door, true);
            Transform right = Piece(kit, "Door panel", new Vector3(x, floor + DoorHeight * .5f, DoorWidth * .25f), new Vector3(.05f, DoorHeight - .01f, DoorWidth * .5f - .01f), kit.Door, true);
            left.SetParent(pivot, true);
            right.SetParent(pivot, true);
            foreach (Transform panel in new[] { left, right })
                GameObjectUtility.SetStaticEditorFlags(panel.gameObject, 0); // they move
            return new Stairwell.Door { Left = left, Right = right, Centre = pivot.position, Slide = Vector3.forward, Opens = opens };
        }

        /// <summary>
        /// Cuts a doorway into a stair room's end wall: the generated wall is replaced by two jambs and a header in the same
        /// material and world texture, and any blood decal across the opening is lifted off. The painted "flight down" the
        /// landing used to fake (FloorDressing.StairLanding) gives way to the real one, and a sign in the opening moves up
        /// onto the header. Anything else in the way stops the build.
        /// </summary>
        private static void Doorway(GameObject[] sceneRoots, Vector3 centre, bool upperRoom)
        {
            MeshRenderer wall = null;
            var obstacles = new List<string>();
            var opening = new Bounds(centre + Vector3.up * DoorHeight * .5f, new Vector3(.5f, DoorHeight, DoorWidth));
            var decals = new List<GameObject>();
            var signs = new List<TextMesh>();
            foreach (GameObject sceneRoot in sceneRoots)
            {
                foreach (MeshRenderer renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Transform t = renderer.transform;
                    if (renderer.name == "Wall" && Mathf.Abs(t.position.x - centre.x) < .05f && Mathf.Abs(t.position.z - centre.z) < .05f &&
                        t.lossyScale.z > DoorWidth + .2f)
                    {
                        wall = renderer;
                        continue;
                    }

                    if (!renderer.bounds.Intersects(opening) || renderer.GetComponentInParent<Stairwell>()) continue;
                    if ((t.parent && t.parent.name == "Blood") || renderer.name.EndsWith("_FlightDown", StringComparison.Ordinal)) decals.Add(renderer.gameObject);
                    else if (renderer.TryGetComponent(out TextMesh sign)) signs.Add(sign);
                    else if (!renderer.name.EndsWith("_Floor", StringComparison.Ordinal)) obstacles.Add(Path(t));
                }
            }

            if (!wall) throw new InvalidOperationException("No generated wall to open at " + centre + " (" + (upperRoom ? "top" : "bottom") + " of a stairwell).");
            if (obstacles.Count > 0) throw new InvalidOperationException("Something stands in the new stairwell doorway at " + centre + ": " + string.Join(", ", obstacles));
            foreach (GameObject decal in decals) Object.DestroyImmediate(decal);

            Transform old = wall.transform;
            Material material = wall.sharedMaterial;
            Transform parent = old.parent;
            float height = old.lossyScale.y, thickness = old.lossyScale.x, length = old.lossyScale.z, bottom = old.position.y - height * .5f;
            foreach (TextMesh sign in signs)
            {
                // Two lines at .03 stand about .3 m tall: they fit the header between the door and the ceiling.
                Vector3 p = sign.transform.position;
                sign.transform.position = new Vector3(p.x, bottom + (DoorHeight + height) * .5f, p.z);
                sign.characterSize = Mathf.Min(sign.characterSize, .03f);
            }
            float z0 = old.position.z - length * .5f, z1 = old.position.z + length * .5f;
            float d0 = centre.z - DoorWidth * .5f, d1 = centre.z + DoorWidth * .5f;
            Object.DestroyImmediate(old.gameObject);
            Part(parent, material, new Vector3(centre.x, bottom + height * .5f, (z0 + d0) * .5f), new Vector3(thickness, height, d0 - z0));
            Part(parent, material, new Vector3(centre.x, bottom + height * .5f, (d1 + z1) * .5f), new Vector3(thickness, height, z1 - d1));
            Part(parent, material, new Vector3(centre.x, (bottom + DoorHeight + bottom + height) * .5f, centre.z), new Vector3(thickness, bottom + height - DoorHeight, DoorWidth));
        }

        private static void Part(Transform parent, Material material, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall";
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshFilter>().sharedMesh = ArtSurfaces.WorldUvBox(go.transform);
        }

        private static RoomRect Room(string id)
        {
            foreach (RoomRect room in WardLayout.Rooms)
                if (room.Id == id) return room;
            throw new InvalidOperationException("WardLayout has no room " + id);
        }

        private static string Path(Transform t) => t.parent ? Path(t.parent) + "/" + t.name : t.name;

        /// <summary>The glow strips' and exit signs' green: emissive, so the post pass blooms it on flat screens.</summary>
        private static Material Glow()
        {
            Material glow = ArtSurfaces.LitMaterial("StairGlow");
            glow.SetColor("_BaseColor", new Color(.36f, .62f, .3f, 1));
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", new Color(.3f, .85f, .32f) * 1.4f);
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            glow.SetFloat("_Smoothness", .2f);
            EditorUtility.SetDirty(glow);
            return glow;
        }
    }
}
