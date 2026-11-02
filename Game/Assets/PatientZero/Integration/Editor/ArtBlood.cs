// Claude-owned integration (horror pass, 2026-11-02; E-21).
using System;
using System.Collections.Generic;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Blood on the ward, as alpha-clipped quads laid over the tiles at compose time: splatters with runs, hands that slid
    /// down the wall, smears where someone was dragged along it, pools and drag trails on the floor, sprays on the
    /// ceiling, and two messages written in blood. Every texture is painted here (no downloaded art). Placement is seeded
    /// by position, so every compose builds the same ward, and keeps clear of puzzle objects and signs.
    /// Cost: one material per atlas cell, no shadows, static-batched; alpha-to-coverage gives soft edges under MSAA.
    /// </summary>
    public static class ArtBlood
    {
        private enum Cell { Splatter = 0, Hands = 1, Smear = 2, Pool = 3 }

        /// <summary>The metres each atlas cell covers (width, height); quads keep these proportions.</summary>
        private static readonly Vector2[] CellMetres = { new Vector2(1.2f, 1.2f), new Vector2(1f, 1f), new Vector2(2.4f, .8f), new Vector2(1.6f, 1.6f) };

        private const string HelpUs = "HELP US";
        private const float HelpUsWidth = 2.4f;
        private const string Accusation = "B-7 DID THIS";
        private const float AccusationWidth = 3f;

        private readonly struct Face
        {
            public Face(Vector3 centre, Vector3 normal, float length, string room)
            {
                Centre = centre;
                Normal = normal;
                Right = Vector3.Cross(Vector3.up, -normal);
                Length = length;
                Room = room;
            }

            /// <summary>The middle of the face at floor level, on the tile surface.</summary>
            public Vector3 Centre { get; }
            /// <summary>Points into the room.</summary>
            public Vector3 Normal { get; }
            /// <summary>To the right of someone facing the wall.</summary>
            public Vector3 Right { get; }
            public float Length { get; }
            public string Room { get; }
        }

        public readonly struct Counts
        {
            public Counts(int walls, int floors, int ceilings, int messages)
            {
                Walls = walls;
                Floors = floors;
                Ceilings = ceilings;
                Messages = messages;
            }

            public int Walls { get; }
            public int Floors { get; }
            public int Ceilings { get; }
            public int Messages { get; }
            public override string ToString() => Walls + " on walls, " + Floors + " on floors, " + Ceilings + " on ceilings, " + Messages + " messages";
        }

        // ------------------------------------------------------------------ placement

        public static Counts Place(GameObject[] sceneRoots, Transform root)
        {
            ArtTextures.Pair atlas = ArtTextures.Ensure("Blood", 1024, 1024, 1f, 4f, PaintAtlas, cutout: true);
            var cells = new Material[4];
            for (int c = 0; c < 4; c++) cells[c] = Decal("Blood_" + (Cell)c, atlas, new Vector2(.5f, .5f), new Vector2(c % 2 * .5f, c / 2 * .5f));
            Material helpUs = Decal("Blood_HelpUs", ArtTextures.Ensure("Writing_HelpUs", 1024, 256, HelpUsWidth, 4f, k => PaintWriting(k, HelpUs, HelpUsWidth, 501), cutout: true), Vector2.one, Vector2.zero);
            Material accusation = Decal("Blood_B7", ArtTextures.Ensure("Writing_B7", 1024, 256, AccusationWidth, 4f, k => PaintWriting(k, Accusation, AccusationWidth, 502), cutout: true), Vector2.one, Vector2.zero);

            var parent = new GameObject("Blood").transform;
            parent.SetParent(root, false);
            List<Vector3> keepClear = KeepClear(sceneRoots);
            List<Face> faces = WallFaces(sceneRoots);
            var used = new Dictionary<int, List<Vector2>>();

            // The two messages: opposite the door of B-7, where the player first steps out, and beside the lobby's lockdown
            // lever, just before the ending says who the carrier was.
            Message(parent, faces, used, keepClear, "Corridor", new Vector3(0, 0, -1), new Vector3(-.7f, 2.05f, 0), HelpUsWidth, helpUs);
            Message(parent, faces, used, keepClear, "Lobby", new Vector3(-1, 0, 0), new Vector3(0, 1.85f, -.3f), AccusationWidth, accusation);

            int walls = 0;
            for (int f = 0; f < faces.Count; f++) walls += Walls(parent, faces[f], f, used, keepClear, cells);

            Dictionary<string, Bounds> floors = Named(sceneRoots, "_Floor"), ceilings = Named(sceneRoots, "_Ceiling");
            int floorCount = 0, ceilingCount = 0;
            foreach (RoomRect room in WardLayout.Rooms)
            {
                if (floors.TryGetValue(room.Id, out Bounds floor)) floorCount += Floors(parent, room, floor.max.y, cells);
                if (ceilings.TryGetValue(room.Id, out Bounds ceiling)) ceilingCount += Ceiling(parent, room, ceiling.min.y, cells[(int)Cell.Splatter]);
            }

            AssetDatabase.SaveAssets();
            return new Counts(walls, floorCount, ceilingCount, 2);
        }

        private static float Intensity(string room)
        {
            switch (room)
            {
                case "IsolationB7": return .2f; // the player's own room stays almost clean
                case "Corridor": case "Vestibule": case "Lobby": return 1f;
                case "SideRoom": case "LabCorridor": return .9f;
                case "SupplyRoom": case "LabA": case "LabB": case "Security": case "Entrance": return .8f;
                case "LinenRoom": return .7f;
                default: return .6f; // stairwells, and any room added later
            }
        }

        private static List<Face> WallFaces(GameObject[] sceneRoots)
        {
            var faces = new List<Face>();
            foreach (GameObject sceneRoot in sceneRoots)
            {
                foreach (MeshRenderer renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.gameObject.name != "Wall") continue;
                    Bounds b = renderer.bounds;
                    bool alongX = b.size.x >= b.size.z;
                    float length = alongX ? b.size.x : b.size.z;
                    if (length < .9f || b.size.y < 2f) continue;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 n = alongX ? new Vector3(0, 0, side) : new Vector3(side, 0, 0);
                        Vector3 surface = new Vector3(b.center.x, b.min.y, b.center.z) + n * (alongX ? b.extents.z : b.extents.x);
                        string room = RoomAt(surface + n * .3f);
                        if (room != null) faces.Add(new Face(surface, n, length, room));
                    }
                }
            }

            faces.Sort((a, b) => a.Centre.x != b.Centre.x ? a.Centre.x.CompareTo(b.Centre.x) : a.Centre.z != b.Centre.z ? a.Centre.z.CompareTo(b.Centre.z) : a.Normal.x + a.Normal.z < b.Normal.x + b.Normal.z ? -1 : 1);
            if (faces.Count == 0) throw new InvalidOperationException("No ward walls found for blood: the 'Wall' boxes were renamed?");
            return faces;
        }

        private static string RoomAt(Vector3 p)
        {
            foreach (RoomRect room in WardLayout.Rooms)
            {
                if (room.Contains(p, 0)) return room.Id;
            }

            return null;
        }

        /// <summary>Puzzle objects, landmarks and every sign: blood never covers them.</summary>
        private static List<Vector3> KeepClear(GameObject[] sceneRoots)
        {
            var points = new List<Vector3>();
            foreach (LayoutPoint p in WardLayout.Points)
            {
                if (p.Kind == LayoutKind.Interactable || p.Kind == LayoutKind.Landmark) points.Add(p.Position);
            }

            foreach (GameObject sceneRoot in sceneRoots)
            {
                foreach (TextMesh text in sceneRoot.GetComponentsInChildren<TextMesh>(true)) points.Add(text.transform.position);
            }

            return points;
        }

        private static bool Clear(Face face, float s, float h, float w, float height, List<Vector3> keepClear)
        {
            foreach (Vector3 p in keepClear)
            {
                Vector3 d = p - face.Centre;
                if (Mathf.Abs(Vector3.Dot(d, face.Normal)) > .6f) continue;
                if (Mathf.Abs(Vector3.Dot(d, face.Right) - s) < w / 2 + .25f && Mathf.Abs(d.y - h) < height / 2 + .25f) return false;
            }

            return true;
        }

        private static bool Free(Dictionary<int, List<Vector2>> used, int face, float from, float to)
        {
            if (!used.TryGetValue(face, out List<Vector2> spans)) return true;
            foreach (Vector2 span in spans)
            {
                if (from < span.y && to > span.x) return false;
            }

            return true;
        }

        private static void Use(Dictionary<int, List<Vector2>> used, int face, float from, float to)
        {
            if (!used.TryGetValue(face, out List<Vector2> spans)) used[face] = spans = new List<Vector2>();
            spans.Add(new Vector2(from, to));
        }

        /// <summary>
        /// Writes a message on the <paramref name="room"/> wall facing <paramref name="normal"/>: <paramref name="at"/> gives
        /// the position along that wall (x for a wall facing ±z, z for one facing ±x) and the height of the text's middle (y).
        /// </summary>
        private static void Message(Transform parent, List<Face> faces, Dictionary<int, List<Vector2>> used, List<Vector3> keepClear, string room, Vector3 normal, Vector3 at, float width, Material material)
        {
            float height = width / 4f;
            for (int f = 0; f < faces.Count; f++)
            {
                Face face = faces[f];
                if (face.Room != room || Vector3.Dot(face.Normal, normal) < .99f) continue;
                Vector3 target = Mathf.Abs(normal.z) > .5f ? new Vector3(at.x, face.Centre.y, face.Centre.z) : new Vector3(face.Centre.x, face.Centre.y, at.z);
                float s = Vector3.Dot(target - face.Centre, face.Right);
                if (Mathf.Abs(s) + width / 2 > face.Length / 2 - .05f) continue;
                if (!Clear(face, s, at.y, width, height, keepClear))
                    throw new InvalidOperationException("The blood message in " + room + " would cover a puzzle object or sign: the layout changed, move the message.");
                Quad(parent, "BloodMessage", material, face.Centre + face.Right * s + Vector3.up * at.y + face.Normal * .004f, Quaternion.LookRotation(-face.Normal, Vector3.up), width, height);
                Use(used, f, s - width / 2 - .1f, s + width / 2 + .1f);
                return;
            }

            throw new InvalidOperationException("No " + room + " wall facing " + normal + " holds the blood message at " + at + ": the layout changed.");
        }

        private static int Walls(Transform parent, Face face, int index, Dictionary<int, List<Vector2>> used, List<Vector3> keepClear, Material[] cells)
        {
            float intensity = Intensity(face.Room);
            var rng = new System.Random(Seed(face.Centre, face.Normal));
            float end = face.Length / 2 - .3f;
            float s = -end + (float)rng.NextDouble() * .8f;
            int placed = 0;
            while (s < end)
            {
                double pick = rng.NextDouble();
                Cell cell = pick < .4 ? Cell.Splatter : pick < .7 ? Cell.Hands : Cell.Smear;
                float scale = cell == Cell.Splatter ? .85f + (float)rng.NextDouble() * .25f : cell == Cell.Hands ? .8f + (float)rng.NextDouble() * .2f : .85f + (float)rng.NextDouble() * .15f;
                float w = CellMetres[(int)cell].x * scale, height = CellMetres[(int)cell].y * scale;
                float h = cell == Cell.Splatter ? 1.1f + (float)rng.NextDouble() * .5f : cell == Cell.Hands ? .95f + (float)rng.NextDouble() * .25f : .75f + (float)rng.NextDouble() * .35f;
                h = Mathf.Min(h, 2.72f - height / 2);
                bool roll = rng.NextDouble() < intensity * .75;
                float centre = s + w / 2;
                if (roll && s + w <= end && Free(used, index, s, s + w) && Clear(face, centre, h, w, height, keepClear))
                {
                    float tilt = cell == Cell.Smear ? ((float)rng.NextDouble() - .5f) * 4f : ((float)rng.NextDouble() - .5f) * 10f;
                    Quaternion rotation = Quaternion.LookRotation(-face.Normal, Vector3.up) * Quaternion.Euler(0, 0, tilt);
                    Quad(parent, "Blood" + cell, cells[(int)cell], face.Centre + face.Right * centre + Vector3.up * h + face.Normal * (.004f + placed % 3 * .0005f), rotation, w, height);
                    Use(used, index, s, s + w);
                    placed++;
                    s += w + .3f + (float)rng.NextDouble() * 1.2f;
                }
                else
                {
                    s += roll ? .4f : 1f + (float)rng.NextDouble() * 1.5f;
                }
            }

            return placed;
        }

        private static int Floors(Transform parent, RoomRect room, float y, Material[] cells)
        {
            float intensity = room.Id == "IsolationB7" ? 0 : Intensity(room.Id);
            float area = (room.MaxX - room.MinX) * (room.MaxZ - room.MinZ);
            var spots = new List<Vector3>();
            var rng = new System.Random(Seed(new Vector3(room.MinX, y, room.MinZ), Vector3.up));

            // Where a zombie waits or fed, there is a pool.
            foreach (LayoutPoint p in WardLayout.Points)
            {
                if (p.Kind == LayoutKind.ZombieMarker && intensity > 0 && room.Contains(p.Position, .7f)) spots.Add(new Vector3(p.Position.x, 1.3f, p.Position.z));
            }

            int wanted = spots.Count + Mathf.RoundToInt(area * intensity / 9f);
            for (int attempt = 0; attempt < 60 && spots.Count < wanted; attempt++)
            {
                float size = 1f + (float)rng.NextDouble() * .5f;
                float clearance = size / 2 + .05f;
                if (room.MaxX - room.MinX < 2 * clearance || room.MaxZ - room.MinZ < 2 * clearance) break;
                var p = new Vector3(Mathf.Lerp(room.MinX + clearance, room.MaxX - clearance, (float)rng.NextDouble()), size, Mathf.Lerp(room.MinZ + clearance, room.MaxZ - clearance, (float)rng.NextDouble()));
                bool apart = true;
                foreach (Vector3 other in spots)
                {
                    if (new Vector2(p.x - other.x, p.z - other.z).magnitude < (p.y + other.y) * .45f) apart = false;
                }

                if (apart) spots.Add(p);
            }

            for (int i = 0; i < spots.Count; i++)
            {
                Vector3 p = spots[i];
                float yaw = (float)rng.NextDouble() * 360f;
                Vector3 along = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                bool pool = i == 0 || rng.NextDouble() < .65;
                Material material = cells[(int)(pool ? Cell.Pool : Cell.Splatter)];
                float size = pool ? p.y : p.y * .8f;
                Quad(parent, pool ? "BloodPool" : "BloodSpatter", material, new Vector3(p.x, y + .003f + i % 3 * .0005f, p.z), Quaternion.LookRotation(Vector3.down, along), size, size);
            }

            return spots.Count;
        }

        private static int Ceiling(Transform parent, RoomRect room, float y, Material splatter)
        {
            if (Intensity(room.Id) < .9f) return 0;
            var rng = new System.Random(Seed(new Vector3(room.MaxX, y, room.MaxZ), Vector3.down));
            if (rng.NextDouble() < .35) return 0;
            const float size = 1.1f;
            float clearance = size / 2 + .1f;
            if (room.MaxX - room.MinX < 2 * clearance || room.MaxZ - room.MinZ < 2 * clearance) return 0;
            var p = new Vector3(Mathf.Lerp(room.MinX + clearance, room.MaxX - clearance, (float)rng.NextDouble()), y - .003f, Mathf.Lerp(room.MinZ + clearance, room.MaxZ - clearance, (float)rng.NextDouble()));
            Quad(parent, "BloodSpray", splatter, p, Quaternion.LookRotation(Vector3.up, Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward), size, size);
            return 1;
        }

        private static Dictionary<string, Bounds> Named(GameObject[] sceneRoots, string suffix)
        {
            var found = new Dictionary<string, Bounds>();
            foreach (GameObject sceneRoot in sceneRoots)
            {
                foreach (MeshRenderer renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string name = renderer.gameObject.name;
                    if (name.EndsWith(suffix, StringComparison.Ordinal)) found[name.Substring(0, name.Length - suffix.Length)] = renderer.bounds;
                }
            }

            return found;
        }

        private static int Seed(Vector3 p, Vector3 n)
        {
            unchecked
            {
                int h = Mathf.RoundToInt(p.x * 100) * 73856093 ^ Mathf.RoundToInt(p.y * 100) * 19349663 ^ Mathf.RoundToInt(p.z * 100) * 83492791;
                return h ^ Mathf.RoundToInt(n.x * 3 + n.y * 7 + n.z * 11) * 2654435;
            }
        }

        private static Mesh quadMesh;

        private static void Quad(Transform parent, string name, Material material, Vector3 centre, Quaternion rotation, float width, float height)
        {
            if (!quadMesh) quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, rotation);
            go.transform.localScale = new Vector3(width, height, 1);
            go.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>URP/Lit, opaque with alpha clipping and alpha-to-coverage; constant wet smoothness (alpha is coverage).</summary>
        private static Material Decal(string name, ArtTextures.Pair pair, Vector2 scale, Vector2 offset)
        {
            Material m = ArtSurfaces.LitMaterial(name);
            m.SetTexture("_BaseMap", pair.Albedo);
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureOffset("_BaseMap", offset);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", pair.Normal);
            m.SetFloat("_BumpScale", 1f);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", .62f);
            m.SetFloat("_SmoothnessTextureChannel", 0);
            m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            m.SetFloat("_Surface", 0);
            m.SetFloat("_AlphaClip", 1);
            m.SetFloat("_Cutoff", .5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_AlphaToMask", 1);
            m.SetFloat("_Cull", (float)CullMode.Back);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ painting

        /// <summary>
        /// A rectangle of a canvas that blood is painted into, addressed in metres (x right, y up). Shapes take the
        /// maximum of what is already there; Commit turns the amount into colour, coverage (alpha) and a raised wet surface.
        /// </summary>
        private sealed class Field
        {
            private const int Margin = 6; // pixels kept clear at the edge, so mipmaps never bleed into a neighbouring cell
            private readonly ArtTextures.Canvas k;
            private readonly int x0, y0, w, h;
            private readonly float pxX, pxY;
            private readonly float[] amount;

            public Field(ArtTextures.Canvas canvas, int x0, int y0, int w, int h, float metresW, float metresH)
            {
                k = canvas;
                this.x0 = x0;
                this.y0 = y0;
                this.w = w;
                this.h = h;
                pxX = w / metresW;
                pxY = h / metresH;
                amount = new float[w * h];
            }

            private float Noise(int x, int y, float featurePx, int seed) =>
                ArtTextures.Fbm((x0 + x + .5f) / k.W, (y0 + y + .5f) / k.H, Mathf.Max(1, Mathf.RoundToInt(k.W / featurePx)), Mathf.Max(1, Mathf.RoundToInt(k.H / featurePx)), 3, seed);

            private delegate float Shape(float mx, float my, int x, int y);

            private void Each(float minX, float minY, float maxX, float maxY, Shape shape)
            {
                int ax = Mathf.Max(Margin, Mathf.FloorToInt(minX * pxX)), bx = Mathf.Min(w - Margin, Mathf.CeilToInt(maxX * pxX));
                int ay = Mathf.Max(Margin, Mathf.FloorToInt(minY * pxY)), by = Mathf.Min(h - Margin, Mathf.CeilToInt(maxY * pxY));
                for (int y = ay; y < by; y++)
                {
                    for (int x = ax; x < bx; x++)
                    {
                        float v = shape((x + .5f) / pxX, (y + .5f) / pxY, x, y);
                        int i = y * w + x;
                        if (v > amount[i]) amount[i] = v;
                    }
                }
            }

            public void Disc(Vector2 c, float r, float strength, float rough, int seed)
            {
                float reach = r * (1.2f + rough);
                Each(c.x - reach, c.y - reach, c.x + reach, c.y + reach, (mx, my, x, y) =>
                {
                    float d = new Vector2(mx - c.x, my - c.y).magnitude / r + (Noise(x, y, 14, seed) - .5f) * rough;
                    return strength * (1 - ArtTextures.Step(.85f, 1f, d));
                });
            }

            public void Ellipse(Vector2 c, float rx, float ry, float angle, float strength, float rough, int seed)
            {
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle), reach = Mathf.Max(rx, ry) * (1.2f + rough);
                Each(c.x - reach, c.y - reach, c.x + reach, c.y + reach, (mx, my, x, y) =>
                {
                    float dx = mx - c.x, dy = my - c.y;
                    float a = (dx * cos + dy * sin) / rx, b = (-dx * sin + dy * cos) / ry;
                    float d = Mathf.Sqrt(a * a + b * b) + (Noise(x, y, 10, seed) - .5f) * rough;
                    return strength * (1 - ArtTextures.Step(.85f, 1f, d));
                });
            }

            /// <summary>A tapered capsule from a to b; with <paramref name="streaks"/> &gt; 0 it is dragged (fine lines along it, drying out towards b).</summary>
            public void Stroke(Vector2 a, Vector2 b, float ra, float rb, float sa, float sb, float rough, int seed, float streaks = 0, float t0 = 0, float t1 = 1)
            {
                float reach = Mathf.Max(ra, rb) * (1.2f + rough);
                Vector2 ab = b - a;
                float len2 = Mathf.Max(1e-8f, ab.sqrMagnitude);
                Vector2 side = new Vector2(-ab.y, ab.x).normalized;
                Each(Mathf.Min(a.x, b.x) - reach, Mathf.Min(a.y, b.y) - reach, Mathf.Max(a.x, b.x) + reach, Mathf.Max(a.y, b.y) + reach, (mx, my, x, y) =>
                {
                    var p = new Vector2(mx, my);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float r = Mathf.Lerp(ra, rb, t);
                    float d = (p - (a + ab * t)).magnitude / r + (Noise(x, y, 12, seed) - .5f) * rough;
                    float s = Mathf.Lerp(sa, sb, t);
                    if (streaks > 0)
                    {
                        float along = Mathf.Lerp(t0, t1, t);
                        float across = Vector2.Dot(p - a, side) / r * .5f + .5f;
                        float lines = ArtTextures.Value(along * 3f, across, 3, 22, seed + 7);
                        s *= 1 - streaks * (1 - lines) * (.35f + .65f * along);
                    }

                    return s * (1 - ArtTextures.Step(.85f, 1f, d));
                });
            }

            /// <summary>A run of blood down the wall from (x, top), ending in a bead.</summary>
            public void Drip(float x, float top, float length, float width, int seed)
            {
                float wobble = (ArtTextures.Hash(seed, 1, 77) - .5f) * width * 2f;
                var a = new Vector2(x, top);
                var mid = new Vector2(x + wobble, top - length * .55f);
                var b = new Vector2(x + wobble * .6f, top - length);
                Stroke(a, mid, width, width * .8f, 1f, 1f, .15f, seed);
                Stroke(mid, b, width * .8f, width * .65f, 1f, 1f, .15f, seed + 1);
                Disc(b, width * 1.35f, 1f, .15f, seed + 2);
            }

            public void Commit(int seed)
            {
                Color thin = ArtTextures.Rgb(122, 24, 16), deep = ArtTextures.Rgb(72, 4, 3), dried = ArtTextures.Rgb(66, 24, 14);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float a = amount[y * w + x];
                        float coverage = ArtTextures.Step(.36f, .5f, a);
                        float thick = Mathf.Clamp01((a - .45f) / .55f);
                        Color c = Color.Lerp(thin, deep, Mathf.Sqrt(thick));
                        c = Color.Lerp(c, dried, ArtTextures.Step(.5f, .75f, Noise(x, y, 40, seed)) * (1 - thick) * .8f);
                        c = ArtTextures.Mul(c, .85f + .3f * Noise(x, y, 6, seed + 1));
                        int i = (y0 + y) * k.W + x0 + x;
                        k.Albedo[i] = c;
                        k.Smooth[i] = coverage;
                        k.Height[i] = coverage * (.2f + .6f * Mathf.Sqrt(thick)) + coverage * (Noise(x, y, 5, seed + 2) - .5f) * .05f;
                    }
                }
            }
        }

        private static float Next(System.Random rng, float from, float to) => from + (float)rng.NextDouble() * (to - from);

        private static float Gauss(System.Random rng)
        {
            double u1 = 1 - rng.NextDouble(), u2 = rng.NextDouble();
            return (float)(Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2));
        }

        private static Vector2 Dir(float radians) => new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

        /// <summary>The 2 × 2 atlas: splatter, sliding hands, wall smear, floor pool with a drag trail.</summary>
        private static void PaintAtlas(ArtTextures.Canvas k)
        {
            int half = k.W / 2;
            var splatter = new Field(k, 0, 0, half, half, CellMetres[0].x, CellMetres[0].y);
            Splatter(splatter, new System.Random(11));
            splatter.Commit(31);
            var hands = new Field(k, half, 0, half, half, CellMetres[1].x, CellMetres[1].y);
            Hands(hands, new System.Random(12));
            hands.Commit(32);
            var smear = new Field(k, 0, half, half, half, CellMetres[2].x, CellMetres[2].y);
            Smear(smear, new System.Random(13));
            smear.Commit(33);
            var pool = new Field(k, half, half, half, half, CellMetres[3].x, CellMetres[3].y);
            Pool(pool, new System.Random(14));
            pool.Commit(34);
        }

        /// <summary>An impact high on the wall, its spray thrown up and left, and runs down from it (cell: 1.2 × 1.2 m).</summary>
        private static void Splatter(Field f, System.Random rng)
        {
            var c = new Vector2(.62f, .84f);
            f.Disc(c, .075f, 1f, .6f, 101);
            for (int i = 0; i < 7; i++) f.Disc(c + Dir(Next(rng, 0, 6.283f)) * Next(rng, .03f, .09f), Next(rng, .018f, .045f), 1f, .6f, 102 + i);
            for (int i = 0; i < 150; i++)
            {
                float angle = 2.5f + Gauss(rng) * 1.1f;
                float dist = .08f + .42f * Mathf.Pow((float)rng.NextDouble(), 1.6f);
                float size = Mathf.Lerp(.016f, .0035f, dist / .5f) * Next(rng, .6f, 1.4f);
                Vector2 p = c + Dir(angle) * dist;
                f.Ellipse(p, size * (1.2f + dist * 5f), size, angle, 1f, .3f, 120 + i);
                if (size > .008f && rng.NextDouble() < .3) f.Drip(p.x, p.y - size * .5f, Next(rng, .03f, .15f), size * .45f, 300 + i * 3);
            }

            for (int i = 0; i < 6; i++)
            {
                float top = c.y - Next(rng, .02f, .05f);
                f.Drip(c.x + Next(rng, -.07f, .07f), top, Mathf.Min(Next(rng, .25f, .8f), top - .06f), Next(rng, .006f, .012f), 200 + i * 3);
            }
        }

        /// <summary>Two hands pressed to the wall that slid down it, fingers trailing (cell: 1 × 1 m).</summary>
        private static void Hands(Field f, System.Random rng)
        {
            Hand(f, new Vector2(.34f, .74f), -12f, true, .34f, 300, rng);
            Hand(f, new Vector2(.64f, .8f), 9f, false, .46f, 360, rng);
            for (int i = 0; i < 25; i++) f.Disc(new Vector2(Next(rng, .15f, .85f), Next(rng, .25f, .92f)), Next(rng, .003f, .009f), 1f, .3f, 420 + i);
        }

        private static void Hand(Field f, Vector2 palm, float degrees, bool left, float slide, int seed, System.Random rng)
        {
            float rad = degrees * Mathf.Deg2Rad, sx = left ? -1 : 1;
            Vector2 P(float x, float y)
            {
                x *= sx;
                return palm + new Vector2(x * Mathf.Cos(rad) - y * Mathf.Sin(rad), x * Mathf.Sin(rad) + y * Mathf.Cos(rad));
            }

            // Finger base, tip and radius in hand space (metres; fingers up, thumb out to the -x side of a right hand).
            float[,] fingers =
            {
                { -.03f, .045f, -.036f, .125f, .0095f },
                { -.008f, .05f, -.009f, .14f, .01f },
                { .014f, .048f, .018f, .13f, .0095f },
                { .034f, .036f, .046f, .1f, .008f },
                { -.04f, -.005f, -.085f, .04f, .011f },
            };

            f.Ellipse(palm, .045f, .052f, rad, 1f, .35f, seed);
            for (int i = 0; i < 5; i++)
            {
                Vector2 b = P(fingers[i, 0], fingers[i, 1]), t = P(fingers[i, 2], fingers[i, 3]);
                f.Stroke(b, t, fingers[i, 4], fingers[i, 4] * .9f, 1f, .95f, .3f, seed + 1 + i);
                if (i == 4) continue; // the thumb lifts off; the fingers drag
                Vector2 down = new Vector2(0, -slide * Next(rng, .85f, 1.05f));
                Vector2 mid = (b + t) * .5f;
                f.Stroke(mid, mid + down, fingers[i, 4] * .95f, fingers[i, 4] * .7f, .9f, .42f, .25f, seed + 10 + i, .8f);
            }

            Vector2 heel = P(0, .01f);
            f.Stroke(heel, heel + new Vector2(0, -slide * .8f), .04f, .032f, .85f, .4f, .3f, seed + 20, .9f);
            for (int i = 0; i < 3; i++)
            {
                float top = heel.y - slide * .8f - .02f;
                f.Drip(heel.x + Next(rng, -.03f, .03f), top, Mathf.Min(Next(rng, .08f, .3f), top - .05f), Next(rng, .005f, .009f), seed + 30 + i * 3);
            }
        }

        /// <summary>Someone dragged along the wall at waist height, a hand smear where it starts, runs below (cell: 2.4 × 0.8 m).</summary>
        private static void Smear(Field f, System.Random rng)
        {
            const int segments = 28;
            Vector2 Path(float t)
            {
                float x = .12f + t * 2.12f;
                return new Vector2(x, .46f + .05f * Mathf.Sin(x * 2.1f + 1f) + .025f * Mathf.Sin(x * 5.3f));
            }

            float Width(float t) => .12f * (1 - .4f * t);
            float Strength(float t) => Mathf.Lerp(1f, .5f, t);

            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
                f.Stroke(Path(t0), Path(t1), Width(t0), Width(t1), Strength(t0), Strength(t1), .35f, 400, .85f, t0, t1);
            }

            f.Disc(Path(0) + new Vector2(.02f, 0), .1f, 1f, .7f, 430);
            for (int i = 0; i < 8; i++)
            {
                float t = Next(rng, 0, .7f);
                Vector2 p = Path(t);
                float top = p.y - Width(t) * .7f;
                f.Drip(p.x, top, Mathf.Min(Next(rng, .06f, .3f), top - .05f), Next(rng, .005f, .009f), 440 + i * 3);
            }
        }

        /// <summary>A pool on the floor with a body's drag trail leaving it, and drops around it (cell: 1.6 × 1.6 m).</summary>
        private static void Pool(Field f, System.Random rng)
        {
            var c = new Vector2(.55f, .5f);
            f.Disc(c, .22f, 1f, .7f, 500);
            for (int i = 0; i < 7; i++) f.Disc(c + Dir(Next(rng, 0, 6.283f)) * Next(rng, .1f, .24f), Next(rng, .07f, .16f), 1f, .7f, 501 + i);
            Vector2 from = c + new Vector2(.05f, .1f);
            const int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
                Vector2 Trail(float t) => Vector2.Lerp(from, new Vector2(1.18f, 1.45f), t) + new Vector2(Mathf.Sin(t * 5f) * .04f, 0);
                f.Stroke(Trail(t0), Trail(t1), Mathf.Lerp(.16f, .1f, t0), Mathf.Lerp(.16f, .1f, t1), Mathf.Lerp(.85f, .38f, t0), Mathf.Lerp(.85f, .38f, t1), .3f, 520, .9f, t0, t1);
            }

            for (int i = 0; i < 40; i++) f.Disc(c + Dir(Next(rng, 0, 6.283f)) * Next(rng, .28f, .6f), Next(rng, .005f, .02f), 1f, .3f, 540 + i);
        }

        // ------------------------------------------------------------------ writing

        /// <summary>Finger-painted capitals: strokes on a 4 × 6 grid, y up.</summary>
        private static readonly Dictionary<char, float[][]> Glyphs = new Dictionary<char, float[][]>
        {
            ['H'] = new[] { new float[] { 0, 6, 0, 0 }, new float[] { 4, 6, 4, 0 }, new float[] { 0, 3, 4, 3 } },
            ['E'] = new[] { new float[] { 4, 6, 0, 6, 0, 0, 4, 0 }, new float[] { 0, 3, 3, 3 } },
            ['L'] = new[] { new float[] { 0, 6, 0, 0, 4, 0 } },
            ['P'] = new[] { new float[] { 0, 0, 0, 6, 3, 6, 4, 5, 4, 4, 3, 3, 0, 3 } },
            ['U'] = new[] { new float[] { 0, 6, 0, 1, 1, 0, 3, 0, 4, 1, 4, 6 } },
            ['S'] = new[] { new float[] { 4, 5, 3, 6, 1, 6, 0, 5, 0, 4, 1, 3, 3, 3, 4, 2, 4, 1, 3, 0, 1, 0, 0, 1 } },
            ['B'] = new[] { new float[] { 0, 0, 0, 6, 3, 6, 4, 5, 4, 4, 3, 3, 0, 3 }, new float[] { 3, 3, 4, 2, 4, 1, 3, 0, 0, 0 } },
            ['-'] = new[] { new float[] { .5f, 3, 3.5f, 3 } },
            ['7'] = new[] { new float[] { 0, 6, 4, 6, 1.5f, 0 } },
            ['D'] = new[] { new float[] { 0, 0, 0, 6, 2, 6, 4, 4, 4, 2, 2, 0, 0, 0 } },
            ['I'] = new[] { new float[] { 0, 6, 0, 0 } },
            ['T'] = new[] { new float[] { 0, 6, 4, 6 }, new float[] { 2, 6, 2, 0 } },
        };

        private static float GlyphWidth(char c)
        {
            if (c == ' ') return 1;
            float width = 0;
            foreach (float[] stroke in Glyphs[c])
            {
                for (int i = 0; i < stroke.Length; i += 2) width = Mathf.Max(width, stroke[i]);
            }

            return width;
        }

        /// <summary>One line of text written with a finger in blood, runs dripping from it (canvas: width × width/4 metres).</summary>
        private static void PaintWriting(ArtTextures.Canvas k, string text, float widthMetres, int seed)
        {
            float heightMetres = widthMetres * k.H / k.W;
            var f = new Field(k, 0, 0, k.W, k.H, widthMetres, heightMetres);
            var rng = new System.Random(seed);
            float units = -2;
            foreach (char c in text) units += GlyphWidth(c) + 2;
            float unit = Mathf.Min(.06f, (widthMetres - .24f) / units);
            float baseline = heightMetres * .3f;
            float x = (widthMetres - units * unit) / 2;
            float radius = unit * .38f;
            int n = 0;
            foreach (char c in text)
            {
                if (c != ' ')
                {
                    float lean = Next(rng, -.06f, .06f), lift = Next(rng, -.15f, .15f) * unit;
                    foreach (float[] stroke in Glyphs[c])
                    {
                        Vector2 Point(int i) => new Vector2(
                            x + (stroke[i] + stroke[i + 1] * (.12f + lean) + (ArtTextures.Hash(n, i, seed) - .5f) * .35f) * unit,
                            baseline + lift + (stroke[i + 1] + (ArtTextures.Hash(n, i, seed + 1) - .5f) * .35f) * unit);

                        float strength = 1f;
                        for (int i = 0; i + 3 < stroke.Length; i += 2)
                        {
                            Vector2 a = Point(i), b = Point(i + 2);
                            float ra = radius * Next(rng, .85f, 1.15f), rb = radius * Next(rng, .85f, 1.15f);
                            float next = Mathf.Max(.62f, strength - .09f);
                            f.Stroke(a, b, ra, rb, strength, next, .35f, seed + n * 17 + i, .5f);
                            strength = next;

                            // Runs: from the bottom of a stroke, and now and then from the middle of a level one.
                            bool low = stroke[i + 3] < .5f && stroke[i + 1] >= stroke[i + 3];
                            bool level = Mathf.Abs(stroke[i + 1] - stroke[i + 3]) < .5f && rng.NextDouble() < .45;
                            if (low || level)
                            {
                                Vector2 from = low ? b : (a + b) * .5f;
                                float top = from.y - radius * .6f;
                                f.Drip(from.x + Next(rng, -.3f, .3f) * radius, top, Mathf.Min(Next(rng, .04f, .2f), top - .03f), radius * Next(rng, .35f, .5f), seed + 900 + n * 13 + i);
                            }
                        }

                        n++;
                    }
                }

                x += (GlyphWidth(c) + 2) * unit;
            }

            for (int i = 0; i < 30; i++) f.Disc(new Vector2(Next(rng, .1f, widthMetres - .1f), Next(rng, baseline - unit, baseline + 7 * unit)), Next(rng, .002f, .006f), 1f, .3f, seed + 2000 + i);
            f.Commit(seed + 3000);
        }
    }
}
