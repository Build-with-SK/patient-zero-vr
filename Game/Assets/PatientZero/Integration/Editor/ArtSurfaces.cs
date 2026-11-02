// Claude-owned integration (art pass, 2026-10-31; E-20).
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Textured URP/Lit materials for the ward, chosen by the name of the material a seat built a box with, and box meshes
    /// whose UVs are in world metres, so a texture keeps its real size on every box and the wall's chair rail lines up at
    /// the same height everywhere. Seat B's and seat A's material assets are never edited: renderers are pointed at these
    /// materials in the composed scene instead.
    /// </summary>
    public sealed class ArtSurfaces
    {
        public const string LitShader = "Universal Render Pipeline/Lit";

        private Material floor, wall, ceiling, metal, paintedMetal, fabric, ceramic, wayfinding, trace, damage, wood, doorPaint, redPaint, plastic;

        public int Swapped { get; private set; }
        public int Meshes { get; private set; }

        internal Material Floor => floor ? floor : floor = Textured("Floor", ArtTextures.Floor(), Color.white, 0, 1, 1);
        internal Material Wall => wall ? wall : wall = Textured("Wall", ArtTextures.Wall(), Color.white, 0, 1, 1, ArtTextures.WallHeight);
        internal Material Ceiling => ceiling ? ceiling : ceiling = Textured("Ceiling", ArtTextures.Ceiling(), Color.white, 0, 1, 1);
        internal Material Metal => metal ? metal : metal = Textured("Metal", ArtTextures.Metal(), Color.white, .6f, 1, .6f);
        private Material PaintedMetal => paintedMetal ? paintedMetal : paintedMetal = Textured("PaintedMetal", ArtTextures.Grime(), Rgb(70, 78, 74), .3f, .9f, .8f);
        private Material Fabric => fabric ? fabric : fabric = Textured("Fabric", ArtTextures.Fabric(), Rgb(84, 122, 116), 0, 1, 1);
        private Material Ceramic => ceramic ? ceramic : ceramic = Textured("Ceramic", ArtTextures.Grime(), Rgb(214, 216, 206), 0, 1, .5f);
        private Material Wayfinding => wayfinding ? wayfinding : wayfinding = Textured("Wayfinding", ArtTextures.Grime(), Rgb(58, 132, 108), 0, .8f, .8f);
        private Material Trace => trace ? trace : trace = Textured("Trace", ArtTextures.Grime(), Rgb(96, 20, 14), 0, 1, .6f);
        private Material Damage => damage ? damage : damage = Textured("Damage", ArtTextures.Grime(), Rgb(50, 54, 52), 0, .6f, 1.2f);
        private Material Wood => wood ? wood : wood = Textured("Wood", ArtTextures.Wood(), Color.white, 0, 1, 1);
        internal Material DoorPaint => doorPaint ? doorPaint : doorPaint = Textured("DoorPaint", ArtTextures.Grime(), Rgb(122, 138, 142), .15f, .9f, .8f);
        private Material RedPaint => redPaint ? redPaint : redPaint = Textured("RedPaint", ArtTextures.Grime(), Rgb(150, 30, 24), .2f, .9f, .8f);
        private Material Plastic => plastic ? plastic : plastic = Textured("Plastic", ArtTextures.Grime(), Rgb(150, 152, 146), 0, .8f, .8f);

        /// <summary>Re-materials every MeshRenderer under the scene roots it recognises, and gives its cube boxes world-metre UVs.</summary>
        public void Apply(GameObject[] sceneRoots, Func<Renderer, bool> skip)
        {
            Material pipelineDefault = GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.defaultMaterial : null;
            foreach (GameObject sceneRoot in sceneRoots)
            {
                foreach (MeshRenderer renderer in sceneRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (skip(renderer) || renderer.GetComponent<TextMesh>()) continue;
                    Material[] materials = renderer.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material target = For(materials[i], renderer.gameObject.name, pipelineDefault);
                        if (!target) continue;
                        materials[i] = target;
                        changed = true;
                    }

                    if (!changed) continue;
                    renderer.sharedMaterials = materials;
                    Swapped++;
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter && IsBuiltinCube(filter.sharedMesh))
                    {
                        filter.sharedMesh = WorldUvBox(renderer.transform);
                        Meshes++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        private Material For(Material current, string objectName, Material pipelineDefault)
        {
            if (!current) return null;
            switch (current.name)
            {
                case "WardTile": return Floor;
                case "WardPaint": return objectName.EndsWith("_Ceiling", StringComparison.Ordinal) ? Ceiling : Wall;
                case "MedicalMetal": return Metal;
                case "FixtureHousing": return PaintedMetal;
                case "PrivacyFabric": return Fabric;
                case "ClinicalCeramic": return Ceramic;
                case "WardWayfinding": return Wayfinding;
                case "RestrainedTrace": return Trace;
                case "SurfaceDamage": return Damage;
            }

            // Seat A's puzzle parts and props keep the pipeline's default grey; dress them by what they are.
            if (current != pipelineDefault) return null;
            string n = objectName.ToLowerInvariant();
            if (n.Contains("aim-only")) return null;
            if (n.Contains("plank") || n.Contains("clipboard")) return Wood;
            if (n.Contains("door")) return DoorPaint;
            if (n.Contains("valve") || n.Contains("lever")) return RedPaint;
            if (n.Contains("bottle")) return Ceramic;
            if (n.Contains("trolley") || n.Contains("tray") || n.Contains("socket") || n.Contains("shelf") || n.Contains("dish")) return Metal;
            return Plastic;
        }

        // ------------------------------------------------------------------ materials

        public static Material LitMaterial(string name)
        {
            Shader shader = Shader.Find(LitShader);
            if (!shader) throw new InvalidOperationException("Shader '" + LitShader + "' not found: URP must be the active pipeline before compose.");
            ArtTextures.CreateFolders();
            string path = ArtTextures.Folder + "/M_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader) { name = "M_" + name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            return material;
        }

        private static Material Textured(string name, ArtTextures.Pair pair, Color tint, float metallic, float smoothness, float bump, float metresV = 0)
        {
            Material m = LitMaterial(name);
            m.SetTexture("_BaseMap", pair.Albedo);
            m.SetColor("_BaseColor", tint);
            m.SetTexture("_BumpMap", pair.Normal);
            m.SetFloat("_BumpScale", bump);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness); // multiplies the smoothness stored in the albedo's alpha
            m.SetFloat("_SmoothnessTextureChannel", 1);
            m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            m.SetTextureScale("_BaseMap", new Vector2(1f / pair.Metres, 1f / (metresV > 0 ? metresV : pair.Metres)));
            m.SetTextureOffset("_BaseMap", Vector2.zero);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1);

        // ------------------------------------------------------------------ meshes

        private static bool IsBuiltinCube(Mesh mesh) =>
            mesh && mesh.name == "Cube" && AssetDatabase.GetAssetPath(mesh) == "Library/unity default resources";

        private static readonly Vector3[] FaceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        /// <summary>
        /// The unit cube with UVs in world metres, projected per face along the face's dominant world axis: floors and
        /// ceilings get (x, z), walls get (along, height). Saved inside the scene with the renderer that uses it.
        /// </summary>
        public static Mesh WorldUvBox(Transform t) => UvBox(t, Vector3.zero);

        /// <summary>
        /// E-21: as <see cref="WorldUvBox"/>, but in metres from <paramref name="origin"/>, so two copies of a piece of building
        /// (the stairwell lobbies) carry exactly the same texture although they stand in different places.
        /// </summary>
        public static Mesh UvBox(Transform t, Vector3 origin)
        {
            var vertices = new Vector3[24];
            var normals = new Vector3[24];
            var uvs = new Vector2[24];
            var triangles = new int[36];
            var corners = new Vector3[4];
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = FaceNormals[f];
                Vector3 a = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 b = Vector3.Cross(n, a);
                corners[0] = (n - a - b) * .5f;
                corners[1] = (n + a - b) * .5f;
                corners[2] = (n + a + b) * .5f;
                corners[3] = (n - a + b) * .5f;
                Vector3 worldNormal = t.TransformDirection(n);
                for (int k = 0; k < 4; k++)
                {
                    vertices[f * 4 + k] = corners[k];
                    normals[f * 4 + k] = n;
                    uvs[f * 4 + k] = Project(t.TransformPoint(corners[k]) - origin, worldNormal);
                }

                // Unity's front faces wind clockwise: Cross(p1 - p0, p2 - p0) points out of the face.
                bool flip = Vector3.Dot(Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]), n) < 0;
                int v = f * 4, i = f * 6;
                triangles[i] = v;
                triangles[i + 1] = flip ? v + 2 : v + 1;
                triangles[i + 2] = flip ? v + 1 : v + 2;
                triangles[i + 3] = v;
                triangles[i + 4] = flip ? v + 3 : v + 2;
                triangles[i + 5] = flip ? v + 2 : v + 3;
            }

            var mesh = new Mesh { name = "World-UV box " + t.name };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2 Project(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
            return ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
        }
    }
}
