// Claude-owned integration (art pass, 2026-10-31; E-20).
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Procedural surface textures for the art pass: every texture tiles seamlessly, is generated from a fixed seed (the
    /// same bytes on every machine) and is written as PNG next to the generated materials. Each texture comes as an
    /// albedo map (smoothness in alpha) and a tangent-space normal map. Nothing is downloaded.
    /// A texture is regenerated only when <see cref="Version"/> changes, so composing does not rewrite them.
    /// </summary>
    public static class ArtTextures
    {
        public const string Folder = "Assets/PatientZero/Integration/Generated/Art";
        public const string Version = "pz-art-textures-2";

        public readonly struct Pair
        {
            public Pair(Texture2D albedo, Texture2D normal, float metres)
            {
                Albedo = albedo;
                Normal = normal;
                Metres = metres;
            }

            public Texture2D Albedo { get; }
            public Texture2D Normal { get; }
            /// <summary>How many metres one repeat of the texture covers along U.</summary>
            public float Metres { get; }
        }

        internal delegate void Painter(Canvas canvas);

        public static Pair Floor() => Ensure("Floor", 512, 512, 1.2f, 3f, PaintFloor);
        public static Pair Wall() => Ensure("Wall", 1024, 1024, WallWidth, 2f, PaintWall);
        public static Pair Ceiling() => Ensure("Ceiling", 1024, 1024, 2.4f, 2.5f, PaintCeiling);
        public static Pair Metal() => Ensure("Metal", 512, 512, .5f, 2f, PaintMetal);
        public static Pair Grime() => Ensure("Grime", 512, 512, 1f, 1.5f, PaintGrime);
        public static Pair Fabric() => Ensure("Fabric", 512, 512, .5f, 2.5f, PaintFabric);
        public static Pair Wood() => Ensure("Wood", 512, 512, 1f, 2f, PaintWood);

        /// <summary>The wall texture covers this many metres horizontally and <see cref="WallHeight"/> metres vertically.</summary>
        public const float WallWidth = 4f;
        public const float WallHeight = 2.8f;

        // ------------------------------------------------------------------ file handling

        /// <summary>
        /// Paints and imports a texture pair unless both files already carry <see cref="Version"/>. With
        /// <paramref name="cutout"/>, the albedo's alpha is coverage for alpha clipping (not smoothness), so its mipmaps
        /// keep their coverage and the colour bleeds into the clear areas.
        /// </summary>
        internal static Pair Ensure(string name, int width, int height, float metres, float normalStrength, Painter paint, bool cutout = false)
        {
            CreateFolders();
            string albedoPath = Folder + "/T_" + name + "_Albedo.png";
            string normalPath = Folder + "/T_" + name + "_Normal.png";
            if (!IsCurrent(albedoPath) || !IsCurrent(normalPath))
            {
                var canvas = new Canvas(width, height);
                paint(canvas);
                WritePng(albedoPath, canvas.AlbedoBytes(), width, height, TextureFormat.RGBA32);
                WritePng(normalPath, canvas.NormalBytes(normalStrength), width, height, TextureFormat.RGB24);
                Configure(albedoPath, normalMap: false, cutout);
                Configure(normalPath, normalMap: true, false);
            }

            return new Pair(AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath), AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath), metres);
        }

        public static void CreateFolders()
        {
            string folder = "Assets";
            foreach (string part in new[] { "PatientZero", "Integration", "Generated", "Art" })
            {
                string child = folder + "/" + part;
                if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(folder, part);
                folder = child;
            }
        }

        private static bool IsCurrent(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            return importer != null && importer.userData == Version && File.Exists(Absolute(path));
        }

        private static void WritePng(string path, byte[] pixels, int width, int height, TextureFormat format)
        {
            var texture = new Texture2D(width, height, format, false, false);
            try
            {
                texture.SetPixelData(pixels, 0);
                texture.Apply(false, false);
                byte[] png = texture.EncodeToPNG();
                string file = Absolute(path);
                if (!File.Exists(file) || !BytesEqual(File.ReadAllBytes(file), png)) File.WriteAllBytes(file, png);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Configure(string path, bool normalMap, bool cutout)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("No texture importer for " + path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap;
            importer.alphaSource = normalMap ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = cutout;
            importer.mipMapsPreserveCoverage = cutout;
            importer.alphaTestReferenceValue = .5f;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.userData = Version;
            importer.SaveAndReimport();
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        private static string Absolute(string assetPath) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        // ------------------------------------------------------------------ canvas and noise

        internal sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color[] Albedo;
            public readonly float[] Smooth, Height;

            public Canvas(int width, int height)
            {
                W = width;
                H = height;
                Albedo = new Color[width * height];
                Smooth = new float[width * height];
                Height = new float[width * height];
            }

            public byte[] AlbedoBytes()
            {
                var bytes = new byte[W * H * 4];
                for (int i = 0; i < Albedo.Length; i++)
                {
                    Color c = Albedo[i];
                    bytes[i * 4] = Byte(c.r);
                    bytes[i * 4 + 1] = Byte(c.g);
                    bytes[i * 4 + 2] = Byte(c.b);
                    bytes[i * 4 + 3] = Byte(Smooth[i]);
                }

                return bytes;
            }

            /// <summary>Tangent-space normals from the height field (OpenGL convention: green points along +V). Wraps at the edges.</summary>
            public byte[] NormalBytes(float strength)
            {
                var bytes = new byte[W * H * 3];
                for (int y = 0; y < H; y++)
                {
                    int up = (y + 1) % H, down = (y + H - 1) % H;
                    for (int x = 0; x < W; x++)
                    {
                        int right = (x + 1) % W, left = (x + W - 1) % W;
                        float dx = (Height[y * W + right] - Height[y * W + left]) * .5f * strength;
                        float dy = (Height[up * W + x] - Height[down * W + x]) * .5f * strength;
                        var n = new Vector3(-dx, -dy, 1f).normalized;
                        int i = (y * W + x) * 3;
                        bytes[i] = Byte(n.x * .5f + .5f);
                        bytes[i + 1] = Byte(n.y * .5f + .5f);
                        bytes[i + 2] = Byte(n.z * .5f + .5f);
                    }
                }

                return bytes;
            }

            private static byte Byte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
        }

        internal static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        internal static int Wrap(int v, int period)
        {
            int r = v % period;
            return r < 0 ? r + period : r;
        }

        /// <summary>Value noise that repeats every texture: u, v in [0, 1), fx × fy lattice cells across it.</summary>
        internal static float Value(float u, float v, int fx, int fy, int seed)
        {
            float x = u * fx, y = v * fy;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3 - 2 * tx);
            ty = ty * ty * (3 - 2 * ty);
            int ax = Wrap(x0, fx), bx = Wrap(x0 + 1, fx), ay = Wrap(y0, fy), by = Wrap(y0 + 1, fy);
            float a = Hash(ax, ay, seed), b = Hash(bx, ay, seed), c = Hash(ax, by, seed), d = Hash(bx, by, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        internal static float Fbm(float u, float v, int fx, int fy, int octaves, int seed)
        {
            float sum = 0, amplitude = .5f, total = 0;
            for (int o = 0; o < octaves; o++)
            {
                sum += amplitude * Value(u, v, fx, fy, seed + o * 101);
                total += amplitude;
                amplitude *= .5f;
                fx *= 2;
                fy *= 2;
            }

            return sum / total;
        }

        internal static float Step(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3 - 2 * t);
        }

        internal static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1);

        internal static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1);

        internal static Color Mul(Color c, Color k) => new Color(c.r * k.r, c.g * k.g, c.b * k.b, 1);

        // ------------------------------------------------------------------ painters

        /// <summary>Vinyl terrazzo floor tiles, 0.3 m, 4 × 4 per repeat: grout, chips, scuffs and dirt that gathers along the joints.</summary>
        private static void PaintFloor(Canvas k)
        {
            const int tiles = 4;
            int size = k.W / tiles;
            for (int y = 0; y < k.H; y++)
            {
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x, tx = x / size, ty = y / size, lx = x % size, ly = y % size;
                    float u = (x + .5f) / k.W, v = (y + .5f) / k.H;
                    float edge = Mathf.Min(Mathf.Min(lx, size - 1 - lx), Mathf.Min(ly, size - 1 - ly));
                    float grout = 1 - Step(.4f, 1.4f, edge);
                    float bevel = Step(1f, 4f, edge);
                    float tone = .9f + .16f * Hash(tx, ty, 11);
                    float hue = Hash(tx, ty, 12) - .5f;
                    float mottle = Fbm(u, v, 8, 8, 4, 21) - .5f;
                    float dirt = Step(.5f, .85f, Fbm(u, v, 3, 3, 5, 31));
                    float scuff = Step(.7f, .8f, Fbm(u, v, 6, 48, 3, 51)) * Step(.45f, .7f, Fbm(u, v, 4, 4, 2, 52));
                    float nearJoint = 1 - Step(1.5f, 8f, edge);
                    float chip = Hash(x / 2, y / 2, 41);

                    Color c = Mul(Rgb(132, 134, 122), tone * (1 + mottle * .14f));
                    c.r += hue * .03f;
                    c.b -= hue * .03f;
                    float chipHeight = 0;
                    if (chip > .945f) { c = Mul(c, .55f); chipHeight = -.03f; }
                    else if (chip < .035f) { c = Color.Lerp(c, Rgb(206, 202, 190), .6f); chipHeight = .02f; }

                    c = Mul(c, (1 - .45f * dirt) * (1 - .3f * nearJoint) * (1 - .35f * scuff));
                    k.Albedo[i] = Color.Lerp(c, Rgb(44, 42, 38), grout);
                    k.Smooth[i] = Mathf.Lerp(.66f * (1 - .55f * dirt) * (1 - .45f * scuff), .1f, grout);
                    k.Height[i] = grout > .5f ? 0 : .35f + .65f * bevel + chipHeight + mottle * .04f - scuff * .05f;
                }
            }
        }

        /// <summary>
        /// White glazed ceramic tiles, 0.2 m square, 20 × 14 per repeat (4 m × 2.8 m; V is the height above the floor):
        /// a grey coved skirting row, a pale green band at 1.0–1.2 m, dirty grout, crazed glaze, cracked and missing tiles
        /// showing the combed adhesive, grime rising from the floor and rust streaks running down from the ceiling.
        /// Blood is not painted here: it is placed as decals (ArtBlood), so it never repeats every 4 m.
        /// </summary>
        private static void PaintWall(Canvas k)
        {
            const float tile = .2f, joint = .0035f;
            const int columns = 20, rows = 14;
            Color white = Rgb(214, 216, 207), band = Rgb(150, 176, 160), cove = Rgb(118, 122, 114);
            Color grout = Rgb(96, 94, 86), adhesive = Rgb(126, 122, 112);
            for (int y = 0; y < k.H; y++)
            {
                float m = (y + .5f) / k.H * WallHeight;
                float v = (y + .5f) / k.H;
                int row = Mathf.Min(rows - 1, Mathf.FloorToInt(m / tile));
                float ly = m - row * tile;
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x;
                    float u = (x + .5f) / k.W, along = u * WallWidth;
                    int col = Mathf.Min(columns - 1, Mathf.FloorToInt(along / tile));
                    float lx = along - col * tile;
                    float edge = Mathf.Min(Mathf.Min(lx, tile - lx), Mathf.Min(ly, tile - ly));
                    float isJoint = 1 - Step(joint * .5f, joint * 1.5f, edge);
                    float bevel = Step(joint, joint + .01f, edge);

                    Color c = row == 0 ? cove : row == 5 ? band : white;
                    c = Mul(c, .94f + .08f * Hash(col, row, 201));
                    float smooth = .86f;
                    float height = .55f + .35f * bevel;

                    // Cracks through some tiles, and fine crazing in the glaze everywhere.
                    float crackRidge = 1 - Mathf.Abs(2 * Fbm(u, v, 40, 28, 3, 204) - 1);
                    float crack = (Hash(col, row, 203) > .88f ? 1 : 0) * Step(.95f, .99f, crackRidge);
                    float craze = Step(.975f, .995f, 1 - Mathf.Abs(2 * Fbm(u, v, 160, 112, 2, 205) - 1));
                    c = Mul(c, (1 - .55f * crack) * (1 - .12f * craze));
                    height -= .25f * crack;

                    // Dirt: broad blotches, grime rising from the floor, darker towards the joints.
                    float blotch = Step(.45f, .8f, Fbm(u, v, 6, 4, 5, 206));
                    float low = (1 - Step(.05f, .9f, m)) * (.4f + .6f * Fbm(u, v, 16, 8, 3, 81));
                    float nearJoint = 1 - Step(joint, joint + .02f, edge);
                    c = Mul(c, (1 - .3f * blotch) * (1 - .5f * low) * (1 - .25f * nearJoint * (blotch + low)));
                    smooth *= (1 - .55f * blotch) * (1 - .6f * low);

                    Color groutHere = Mul(grout, (.8f + .35f * Fbm(u, v, 64, 64, 2, 207)) * (1 - .5f * low) * (1 - .3f * blotch));
                    c = Color.Lerp(c, groutHere, isJoint);
                    smooth = Mathf.Lerp(smooth, .12f, isJoint);
                    height = Mathf.Lerp(height, .12f, isJoint);

                    // A few tiles have fallen off: grey adhesive with the trowel's combed ridges.
                    if (row > 0 && Hash(col, row, 202) > .982f)
                    {
                        float comb = .5f + .5f * Mathf.Sin(along * 260f + Fbm(u, v, 8, 8, 2, 208) * 6f);
                        c = Mul(adhesive, .82f + .2f * comb);
                        smooth = .05f;
                        height = .02f + .06f * comb;
                    }

                    // Rust streaks running down from the ceiling (V = 1 is the top of the wall).
                    float column = Step(.62f, .8f, Fbm(u, .5f, 96, 1, 2, 71));
                    float reach = 1 - (.2f + .55f * Fbm(u, .3f, 24, 1, 2, 72));
                    float stain = column * Step(reach, Mathf.Min(1f, reach + .25f), v);
                    float drip = Step(.74f, .86f, Fbm(u, v, 128, 4, 2, 73)) * Step(.55f, 1f, v) * .6f;
                    c = Color.Lerp(c, Mul(c, new Color(.72f, .58f, .42f)), Mathf.Clamp01(stain * .85f + drip));
                    smooth *= 1 - .3f * Mathf.Clamp01(stain + drip);

                    k.Albedo[i] = c;
                    k.Smooth[i] = Mathf.Clamp01(smooth);
                    k.Height[i] = height;
                }
            }
        }

        /// <summary>Suspended ceiling: 0.6 m mineral tiles in a painted T-bar grid, 4 × 4 per repeat, with fissures, pits and water stains.</summary>
        private static void PaintCeiling(Canvas k)
        {
            const int tiles = 4;
            int size = k.W / tiles;
            for (int y = 0; y < k.H; y++)
            {
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x, tx = x / size, ty = y / size, lx = x % size, ly = y % size;
                    float u = (x + .5f) / k.W, v = (y + .5f) / k.H;
                    float edge = Mathf.Min(Mathf.Min(lx, size - 1 - lx), Mathf.Min(ly, size - 1 - ly));
                    float grid = 1 - Step(4f, 5.5f, edge);
                    float gap = (1 - Step(5.5f, 7f, edge)) - grid;
                    float tone = .88f + .12f * Hash(tx, ty, 13);
                    float ridge = 1 - Mathf.Abs(2 * Fbm(u, v, 24, 24, 3, 161) - 1);
                    float fissure = Step(.9f, .96f, ridge);
                    float pit = Hash(x / 2, y / 2, 162) > .9f ? 1 : 0;

                    Color tile = Mul(Rgb(168, 163, 150), tone * (1 - .25f * fissure - .3f * pit));
                    float stained = Hash(tx, ty, 7) > .8f ? 1 : 0;
                    if (stained > 0)
                    {
                        float cx = (lx - size * .5f) / size, cy = (ly - size * .5f) / size;
                        float d = Mathf.Sqrt(cx * cx + cy * cy) + (Fbm(u, v, 16, 16, 3, 163) - .5f) * .18f;
                        float ring = Step(.26f, .36f, d) * (1 - Step(.36f, .42f, d));
                        float fill = (1 - Step(0, .36f, d)) * .4f;
                        tile = Color.Lerp(tile, Mul(tile, new Color(.74f, .6f, .42f)), Mathf.Clamp01(ring + fill));
                    }

                    Color grime = Mul(Rgb(176, 176, 168), .92f + .1f * Fbm(u, v, 64, 64, 2, 164));
                    Color c = Color.Lerp(tile, grime, grid);
                    c = Mul(c, 1 - .5f * gap);
                    c = Mul(c, 1 - .3f * Step(.55f, .85f, Fbm(u, v, 3, 3, 4, 165)));
                    k.Albedo[i] = c;
                    k.Smooth[i] = Mathf.Lerp(.08f, .5f, grid);
                    k.Height[i] = grid > .5f ? .9f + .1f * Step(4f, 1f, edge) : .5f - .08f * fissure - .15f * pit - .2f * gap;
                }
            }
        }

        /// <summary>Brushed steel with scratches, water spots and grime; smoothness follows the brushing.</summary>
        private static void PaintMetal(Canvas k)
        {
            var scratch = new float[k.W * k.H];
            for (int s = 0; s < 280; s++)
            {
                float sx = Hash(s, 1, 201) * k.W, sy = Hash(s, 2, 201) * k.H;
                float angle = Hash(s, 3, 201) < .6f ? (Hash(s, 4, 201) - .5f) * .5f : Hash(s, 5, 201) * Mathf.PI;
                float length = 8 + Hash(s, 6, 201) * 90, strength = .35f + .65f * Hash(s, 7, 201);
                float dx = Mathf.Cos(angle), dy = Mathf.Sin(angle);
                for (float t = 0; t < length; t += .5f)
                {
                    int px = Wrap(Mathf.RoundToInt(sx + dx * t), k.W), py = Wrap(Mathf.RoundToInt(sy + dy * t), k.H);
                    float fade = strength * (1 - Mathf.Abs(t / length - .5f) * 1.2f);
                    int i = py * k.W + px;
                    scratch[i] = Mathf.Max(scratch[i], fade);
                }
            }

            for (int y = 0; y < k.H; y++)
            {
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x;
                    float u = (x + .5f) / k.W, v = (y + .5f) / k.H;
                    float brushed = Fbm(u, v, 4, 256, 2, 211);
                    float grime = Step(.55f, .82f, Fbm(u, v, 4, 4, 5, 212));
                    float spot = Hash(x / 3, y / 3, 213) > .986f ? 1 : 0;
                    float sc = Mathf.Clamp01(scratch[i]);
                    Color c = Mul(Rgb(158, 160, 164), .92f + .14f * brushed);
                    c = Color.Lerp(c, Rgb(196, 198, 202), sc * .5f);
                    c = Mul(c, (1 - .55f * grime) * (1 - .3f * spot));
                    k.Albedo[i] = c;
                    k.Smooth[i] = Mathf.Clamp01((.6f + .16f * (brushed - .5f) - .25f * sc) * (1 - .6f * grime));
                    k.Height[i] = .5f + .03f * brushed - .25f * sc;
                }
            }
        }

        /// <summary>Near-white base with dirt patches, blotches and drips. Materials tint it, so one texture serves many surfaces.</summary>
        private static void PaintGrime(Canvas k)
        {
            for (int y = 0; y < k.H; y++)
            {
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x;
                    float u = (x + .5f) / k.W, v = (y + .5f) / k.H;
                    float patches = Step(.45f, .85f, Fbm(u, v, 4, 4, 5, 301));
                    float blotch = Step(.6f, .9f, Fbm(u, v, 16, 16, 3, 302));
                    float drips = Step(.75f, .9f, Fbm(u, v, 48, 3, 2, 303)) * Fbm(u, v, 2, 2, 2, 304);
                    float speck = Hash(x, y, 305) > .975f ? .5f : 0;
                    float dirt = Mathf.Clamp01(.6f * patches + .4f * blotch + .5f * drips + .3f * speck);
                    k.Albedo[i] = Color.Lerp(Rgb(236, 234, 228), Rgb(98, 88, 74), dirt * .8f);
                    k.Smooth[i] = .55f * (1 - .7f * dirt);
                    k.Height[i] = .5f + (Fbm(u, v, 32, 32, 3, 306) - .5f) * .12f - .1f * blotch;
                }
            }
        }

        /// <summary>Plain-weave curtain fabric, 4 mm threads, with faint stains.</summary>
        private static void PaintFabric(Canvas k)
        {
            const int thread = 4;
            for (int y = 0; y < k.H; y++)
            {
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x, tx = x / thread, ty = y / thread;
                    float u = (x + .5f) / k.W, v = (y + .5f) / k.H;
                    bool warpOver = ((tx + ty) & 1) == 0;
                    float across = Mathf.Sin(((warpOver ? x : y) % thread + .5f) / thread * Mathf.PI);
                    float fibre = Hash(x, y, 401) * .12f;
                    float threadTone = .9f + .1f * Hash(warpOver ? tx : ty, warpOver ? 0 : 1, 402);
                    float stain = Step(.6f, .85f, Fbm(u, v, 4, 4, 4, 403));
                    Color c = Mul(Rgb(226, 228, 224), threadTone * (.78f + .22f * across) * (1 - .3f * stain) * (1 - fibre * .5f));
                    k.Albedo[i] = c;
                    k.Smooth[i] = .12f;
                    k.Height[i] = .3f + .6f * across + fibre;
                }
            }
        }

        /// <summary>Weathered pine boards: grain along U, rings, two knots per repeat.</summary>
        private static void PaintWood(Canvas k)
        {
            for (int y = 0; y < k.H; y++)
            {
                for (int x = 0; x < k.W; x++)
                {
                    int i = y * k.W + x;
                    float u = (x + .5f) / k.W, v = (y + .5f) / k.H;
                    float knot = 0;
                    for (int n = 0; n < 2; n++)
                    {
                        float kx = Hash(n, 1, 501), ky = Hash(n, 2, 501);
                        float ddx = Mathf.Repeat(u - kx + .5f, 1f) - .5f, ddy = Mathf.Repeat(v - ky + .5f, 1f) - .5f;
                        float d = Mathf.Sqrt(ddx * ddx * .25f + ddy * ddy * 4f);
                        knot = Mathf.Max(knot, 1 - Step(.0f, .05f, d));
                    }

                    float w = v * 24 + Fbm(u, v, 4, 2, 3, 502) * 3 + .4f * Fbm(u, v, 1, 8, 2, 503) + knot * 2;
                    float rings = w - Mathf.Floor(w);
                    float ring = Step(0, .6f, rings) * (1 - Step(.6f, 1, rings));
                    float streak = Fbm(u, v, 2, 64, 3, 504);
                    float weather = Step(.45f, .75f, Fbm(u, v, 3, 3, 4, 505));
                    Color c = Color.Lerp(Rgb(126, 90, 60), Rgb(84, 57, 37), ring * .7f);
                    c = Mul(c, .88f + .24f * streak);
                    c = Color.Lerp(c, Rgb(112, 106, 98), weather * .45f);
                    c = Mul(c, 1 - .5f * knot);
                    k.Albedo[i] = c;
                    k.Smooth[i] = .22f * (1 - .4f * weather);
                    k.Height[i] = .5f + .08f * ring + .06f * streak - .12f * knot;
                }
            }
        }
    }
}
