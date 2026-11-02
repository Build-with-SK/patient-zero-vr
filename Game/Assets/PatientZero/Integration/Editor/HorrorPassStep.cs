// Claude-owned integration (horror pass, 2026-11-02; E-21).
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
    /// The horror pass, built at compose time after the art pass (Ward 985):
    ///   the ceiling television in B-7 that carries the outbreak bulletin and holds the player until it ends (NewsBroadcast);
    ///   who cries and screams where on each floor, and the crowd outside the main doors (HorrorSoundscape);
    ///   the zombie that leaps at the player as the main doors open (ExitJumpScare).
    /// The recordings themselves are wired by Audio (RecordedCueCatalog, Bootstrap 610).
    /// </summary>
    public static class HorrorPassStep
    {
        public const string NewsClipPath = "Assets/ThirdParty/OpenGameArt/News/ZombieNews_Zisongbr.ogg";

        // The set: a 43-inch screen in the corner of B-7 beside the door, tilted down at the bed.
        private static readonly Vector3 TvCentre = new Vector3(1.3f, 2.12f, 1.45f);
        private const float EyeHeight = 1.6f, PictureWidth = .96f, PictureHeight = .54f, Depth = .07f, Bezel = .03f;

        [SceneBuildStep(BuildScene.Ward, 985)]
        public static void Build(Transform root)
        {
            NewsBroadcast news = Television(root);
            Soundscape(root, news);
            JumpScare(root);
            Debug.Log("[PZ] horror pass: news bulletin " + news.duration.ToString("0.0") + " s, " + news.cards.Length +
                      " headlines; soundscape and exit scare built");
        }

        // ------------------------------------------------------------------ the television

        private static NewsBroadcast Television(Transform root)
        {
            Vector3 eye = new Vector3(WardLayout.A1_Start.Position.x, EyeHeight, WardLayout.A1_Start.Position.z);
            if (!WardLayout.Rooms[0].Contains(TvCentre, .5f)) throw new InvalidOperationException("The B-7 television no longer fits inside " + WardLayout.Rooms[0].Id + ".");

            Vector3 facing = TvCentre - eye;
            var mount = new GameObject("News television (B-7)").transform;
            mount.SetParent(root, false);
            mount.SetPositionAndRotation(TvCentre, Quaternion.LookRotation(new Vector3(facing.x, 0, facing.z), Vector3.up));

            // The set itself: forward points from the bed into the screen, so the picture and its text face -Z.
            var set = new GameObject("Set").transform;
            set.SetParent(mount, false);
            set.rotation = Quaternion.LookRotation(facing, Vector3.up);

            Material plastic = Plastic();
            Box(set, "Housing", new Vector3(0, 0, 0), new Vector3(PictureWidth + 2 * Bezel, PictureHeight + 2 * Bezel, Depth), plastic);
            Box(set, "Bracket", new Vector3(0, .06f, Depth * .5f + .03f), new Vector3(.16f, .16f, .06f), plastic);

            float ceiling = CeilingAbove(TvCentre);
            float poleBottom = TvCentre.y + .1f;
            float pole = Mathf.Max(.05f, ceiling - poleBottom);
            Transform rod = Box(mount, "Ceiling pole", Vector3.zero, new Vector3(.05f, pole, .05f), plastic);
            rod.position = new Vector3(TvCentre.x, poleBottom + pole * .5f, TvCentre.z) + mount.forward * (Depth * .5f + .05f);
            Transform plate = Box(mount, "Ceiling plate", Vector3.zero, new Vector3(.22f, .02f, .22f), plastic);
            plate.position = new Vector3(rod.position.x, ceiling - .01f, rod.position.z);

            var screen = new GameObject("Screen").transform;
            screen.SetParent(set, false);
            screen.localPosition = new Vector3(0, 0, -Depth * .5f - .001f);

            var picture = GameObject.CreatePrimitive(PrimitiveType.Quad);
            picture.name = "Picture";
            Object.DestroyImmediate(picture.GetComponent<Collider>());
            picture.transform.SetParent(screen, false);
            picture.transform.localScale = new Vector3(PictureWidth, PictureHeight, 1);
            var pictureRenderer = picture.GetComponent<MeshRenderer>();
            pictureRenderer.sharedMaterial = PictureMaterial();
            pictureRenderer.shadowCastingMode = ShadowCastingMode.Off;
            pictureRenderer.receiveShadows = false;

            var texts = new List<TextMesh>();
            TextMesh banner = Text(screen, texts, "Banner", "BREAKING NEWS", .05f, .865f, .05f, Color.white, TextAnchor.MiddleLeft, true);
            TextMesh live = Text(screen, texts, "Live", "LIVE", .915f, .865f, .034f, Color.white, TextAnchor.MiddleCenter, true);
            TextMesh headline = Text(screen, texts, "Headline", "", .045f, .30f, .040f, new Color(.96f, .96f, .96f), TextAnchor.MiddleLeft, true);
            TextMesh detail = Text(screen, texts, "Detail", "", .045f, .19f, .030f, Color.white, TextAnchor.MiddleLeft, false);
            TextMesh alert = Text(screen, texts, "Alert", "ALERT", .105f, .065f, .026f, Color.white, TextAnchor.MiddleCenter, true);
            TextMesh ticker = Text(screen, texts, "Ticker", "", .20f, .065f, .024f, new Color(.06f, .05f, .03f), TextAnchor.MiddleLeft, true);

            var depth = mount.gameObject.AddComponent<WorldTextDepth>();
            depth.shader = Shader.Find(WorldTextStep.ShaderName);
            if (!depth.shader) throw new InvalidOperationException("Shader " + WorldTextStep.ShaderName + " is missing.");
            depth.texts = texts.ToArray();
            depth.tint = Color.white;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(NewsClipPath);
            if (!clip) throw new InvalidOperationException("The news recording is missing: " + NewsClipPath);

            var news = mount.gameObject.AddComponent<NewsBroadcast>();
            news.screen = screen;
            news.picture = pictureRenderer;
            news.headline = headline;
            news.detail = detail;
            news.ticker = ticker;
            news.liveDot = live.GetComponent<MeshRenderer>();
            news.labels = new[] { banner.gameObject, alert.gameObject };
            news.duration = clip.length;
            news.cards = Cards();
            news.tickerLines = new[]
            {
                "HOSPITALS SEALED - DO NOT ENTER OR LEAVE",
                "NATIONAL GUARD SENT TO QUARANTINE ZONE",
                "PHONE NETWORKS DOWN - USE TEXT ONLY",
                "ATTACKS REPORTED AT COUNTY GENERAL",
                "ALL ROADS OUT OF THE CITY CLOSED",
                "DO NOT TAKE THE SICK TO A HOSPITAL",
            };
            return news;
        }

        private static NewsBroadcast.Card[] Cards() => new[]
        {
            C(0, "EMERGENCY BROADCAST", "A MESSAGE FROM THE STATE POLICE"),
            C(18, "VIRUS OUTBREAK CONFIRMED", "HIGHLY CONTAGIOUS - SPREADING FAST"),
            C(24, "IT STARTED IN THE HOSPITALS", "TWO COUNTY HOSPITALS NAMED"),
            C(29, "SYMPTOMS: CONFUSION, COLLAPSE", "LOSS OF COORDINATION - THEN DEATH"),
            C(37, "THE INFECTED ARE VIOLENT", "DO NOT APPROACH THE SICK"),
            C(42, "AVOID ANYONE WITH SYMPTOMS", "DO NOT TRY TO HELP THEM"),
            C(46, "COUNTIES UNDER QUARANTINE", "UNTIL FURTHER NOTICE"),
            C(51, "STAY IN YOUR HOMES", "LOCK YOUR DOORS - WAIT FOR NEWS"),
            C(58, "STAY TUNED", "FURTHER DETAILS TO FOLLOW"),
        };

        private static NewsBroadcast.Card C(float at, string headline, string detail) => new NewsBroadcast.Card { At = at, Headline = headline, Detail = detail };

        /// <summary>Text on the picture at (u, v), 0..1 from its bottom-left corner; height is the em size in metres.</summary>
        private static TextMesh Text(Transform screen, List<TextMesh> all, string name, string text, float u, float v, float height, Color colour,
                                     TextAnchor anchor, bool bold)
        {
            var go = new GameObject(name);
            go.transform.SetParent(screen, false);
            go.transform.localPosition = new Vector3((u - .5f) * PictureWidth, (v - .5f) * PictureHeight, -.002f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 48;
            mesh.characterSize = height * 10f / mesh.fontSize;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Left;
            mesh.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            mesh.color = colour;
            mesh.text = text;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            all.Add(mesh);
            return mesh;
        }

        private static Transform Box(Transform parent, string name, Vector3 local, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>()); // nothing to bump into up there, and sound passes it
            box.transform.SetParent(parent, false);
            box.transform.localPosition = local;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box.transform;
        }

        private static float CeilingAbove(Vector3 point)
        {
            Physics.SyncTransforms();
            return Physics.Raycast(new Vector3(point.x, 1.5f, point.z), Vector3.up, out RaycastHit hit, 3f) && hit.point.y > point.y + .2f
                ? hit.point.y
                : 2.775f;
        }

        private static Material Plastic()
        {
            Material m = ArtSurfaces.LitMaterial("TV_Housing");
            m.SetTexture("_BaseMap", null);
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
            m.SetColor("_BaseColor", new Color(.035f, .035f, .04f, 1));
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", .55f);
            m.SetFloat("_SmoothnessTextureChannel", 0);
            m.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The broadcast picture: unlit, so the set glows in the dark room; its brightness is driven at runtime.</summary>
        private static Material PictureMaterial()
        {
            const string unlit = "Universal Render Pipeline/Unlit";
            Shader shader = Shader.Find(unlit);
            if (!shader) throw new InvalidOperationException("Shader '" + unlit + "' not found: URP must be the active pipeline before compose.");
            string path = ArtTextures.Folder + "/M_TV_Picture.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(shader) { name = "M_TV_Picture" };
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetTexture("_BaseMap", ArtTextures.Ensure("NewsPicture", 512, 288, 1f, 0f, PaintPicture).Albedo);
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetColor("_BaseColor", new Color(.94f, .94f, .94f, 1));
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ the picture

        private static void PaintPicture(ArtTextures.Canvas c)
        {
            Color navyTop = ArtTextures.Rgb(16, 30, 66), navyBottom = ArtTextures.Rgb(4, 8, 22);
            Color red = ArtTextures.Rgb(178, 20, 22), redDark = ArtTextures.Rgb(110, 8, 10);
            Color amber = ArtTextures.Rgb(236, 176, 24), lowerThird = ArtTextures.Rgb(10, 22, 52);
            Color land = ArtTextures.Rgb(40, 58, 72), infected = ArtTextures.Rgb(170, 22, 20), border = ArtTextures.Rgb(110, 132, 146);

            // Counties for the map: jittered seeds on a 6 x 4 grid; the outbreak spreads from two hospitals.
            const int cols = 6, rows = 4;
            var seeds = new Vector2[cols * rows];
            for (int j = 0; j < rows; j++)
            for (int i = 0; i < cols; i++)
                seeds[j * cols + i] = new Vector2((i + .2f + .6f * ArtTextures.Hash(i, j, 71)) / cols, (j + .2f + .6f * ArtTextures.Hash(i, j, 72)) / rows);
            Vector2 hospitalA = new Vector2(.36f, .55f), hospitalB = new Vector2(.58f, .42f);

            for (int y = 0; y < c.H; y++)
            for (int x = 0; x < c.W; x++)
            {
                int i = y * c.W + x;
                float u = (x + .5f) / c.W, v = (y + .5f) / c.H;
                Color col = Color.Lerp(navyBottom, navyTop, v);
                float vignette = 1 - .45f * Mathf.Pow(Mathf.Abs(u - .5f) * 1.9f, 3) - .35f * Mathf.Pow(Mathf.Abs(v - .55f) * 2f, 3);
                col = ArtTextures.Mul(col, Mathf.Clamp01(vignette));

                // The outbreak map, right of centre.
                const float mu0 = .54f, mu1 = .965f, mv0 = .40f, mv1 = .76f;
                if (u > mu0 && u < mu1 && v > mv0 && v < mv1)
                {
                    float mu = (u - mu0) / (mu1 - mu0), mv = (v - mv0) / (mv1 - mv0);
                    float best = 9, second = 9;
                    int nearest = 0;
                    for (int s = 0; s < seeds.Length; s++)
                    {
                        float d = Vector2.Distance(new Vector2(mu, mv * .62f), new Vector2(seeds[s].x, seeds[s].y * .62f));
                        if (d < best) { second = best; best = d; nearest = s; }
                        else if (d < second) second = d;
                    }

                    float spread = Mathf.Min(Vector2.Distance(seeds[nearest], hospitalA), Vector2.Distance(seeds[nearest], hospitalB));
                    Color county = spread < .2f ? infected : spread < .3f ? Color.Lerp(infected, land, .55f) : land;
                    county = ArtTextures.Mul(county, .85f + .15f * ArtTextures.Fbm(mu * 4, mv * 4, 4, 4, 3, 73));
                    col = second - best < .006f ? border : county;
                    if (Mathf.Abs(u - mu0) < .003f || Mathf.Abs(u - mu1) < .003f || Mathf.Abs(v - mv0) < .005f || Mathf.Abs(v - mv1) < .005f)
                        col = border;
                    foreach (Vector2 h in new[] { hospitalA, hospitalB })
                    {
                        float hx = (mu - h.x) * (mu1 - mu0) * c.W, hy = (mv - h.y) * (mv1 - mv0) * c.H;
                        float r = Mathf.Sqrt(hx * hx + hy * hy);
                        if (Mathf.Abs(r - 9) < 1.2f || Mathf.Abs(r - 15) < .9f) col = ArtTextures.Rgb(255, 70, 60);
                        if ((Mathf.Abs(hx) < 1.6f && Mathf.Abs(hy) < 5.5f) || (Mathf.Abs(hy) < 1.6f && Mathf.Abs(hx) < 5.5f)) col = Color.white;
                    }
                }

                // A warning sign, left of centre.
                {
                    float h = .30f * c.H, cx = .27f * c.W, cy = .58f * c.H;
                    float px = x - cx, py = y - cy;
                    float half = (h * 2 / 3 - py) / Mathf.Sqrt(3);
                    if (py >= -h / 3 && Mathf.Abs(px) <= half)
                    {
                        float edge = Mathf.Min(py + h / 3, (half - Mathf.Abs(px)) * .86f);
                        col = edge < 4 ? ArtTextures.Rgb(20, 14, 6) : amber;
                        bool bar = Mathf.Abs(px) < 4.5f && py > -h * .02f && py < h * .40f;
                        bool dot = px * px + (py + h * .14f) * (py + h * .14f) < 25;
                        if (bar || dot) col = ArtTextures.Rgb(20, 14, 6);
                    }
                }

                // Bars: banner and LIVE tag at the top, the lower third, the ticker.
                if (v > .80f && v < .93f && u > .03f && u < .62f) col = v < .815f ? redDark : red;
                if (v > .82f && v < .91f && u > .865f && u < .965f) col = red;
                if (v > .245f && v < .355f && u > .03f && u < .97f) col = lowerThird;
                if (v > .14f && v < .245f && u > .03f && u < .97f) col = red;
                if (v > .02f && v < .11f && u > .03f && u < .97f) col = u < .18f ? redDark : amber;

                if (y % 3 == 0) col = ArtTextures.Mul(col, .86f); // scan lines
                c.Albedo[i] = col;
                c.Smooth[i] = 1;
                c.Height[i] = 0;
            }
        }

        // ------------------------------------------------------------------ the soundscape

        private static void Soundscape(Transform root, NewsBroadcast news)
        {
            var sound = root.gameObject.AddComponent<HorrorSoundscape>();
            sound.news = news;
            sound.cryingRooms = new[]
            {
                // Floor 3: behind the supply, linen and side room doors.
                new Vector3(6f, 1.1f, .4f), new Vector3(10.5f, 1f, .6f), new Vector3(10.5f, 1.1f, 6.2f),
                // Floor 2: the two laboratories.
                new Vector3(38f, 1f, 6.2f), new Vector3(35f, 1f, -.6f),
                // Floor 1: the security office, and someone hiding in the lobby's far corner.
                new Vector3(67.5f, 1f, 9.2f), new Vector3(63f, .6f, -1.6f),
            };
            sound.screamPoints = new[]
            {
                // Floor 3, then the floor above it and below it (heard through the ceiling and the floor).
                new Vector3(-1.5f, 1.6f, 3.5f), new Vector3(12.6f, 1.6f, 3f), new Vector3(16.5f, 1.6f, 4.6f), new Vector3(18.8f, 1.6f, 2.4f),
                new Vector3(10.5f, 1.6f, 6.8f), new Vector3(6f, 1.6f, -.8f), new Vector3(4f, 4.4f, 3.2f), new Vector3(14.5f, -1.4f, 2f),
                // Floor 2.
                new Vector3(30.6f, 1.6f, 4.2f), new Vector3(45.5f, 1.6f, 2.3f), new Vector3(38f, 1.6f, 6.8f), new Vector3(33.4f, 1.6f, -1.3f),
                new Vector3(48.2f, 1.6f, 4.2f), new Vector3(40f, 4.4f, 3f), new Vector3(36f, -1.4f, 3.5f),
                // Floor 1, and the street outside.
                new Vector3(60.5f, 1.6f, 2.3f), new Vector3(73.5f, 1.6f, -1.7f), new Vector3(68.5f, 1.6f, 9.7f), new Vector3(72f, 1.6f, 6.7f),
                new Vector3(62.8f, 1.6f, 6.5f), new Vector3(76.8f, 1.6f, 4.3f), new Vector3(66f, 4.4f, 2f), new Vector3(70f, 4.4f, 4f),
            };
            sound.crowd = new Vector3(77.6f, 1.5f, WardLayout.A6_MainDoors.Position.z);
            foreach (Vector3 p in sound.cryingRooms) RequireRoom(p, "crying");
            foreach (Vector3 p in sound.screamPoints) RequireRoom(p, "scream");
        }

        private static void RequireRoom(Vector3 p, string what)
        {
            foreach (RoomRect room in WardLayout.Rooms)
            {
                if (room.Contains(p, 0)) return;
            }

            throw new InvalidOperationException("The " + what + " point " + p + " is in no room of WardLayout (layout changed?).");
        }

        // ------------------------------------------------------------------ the exit scare

        private static void JumpScare(Transform root)
        {
            ZombieArt.Kit kit = ZombieArt.Prepare();
            Transform zombie = ZombieArt.Puppet(kit, root, "Run_Attack");
            var scare = root.gameObject.AddComponent<ExitJumpScare>();
            scare.zombie = zombie;
            // Just outside the main doors, in the entrance.
            scare.doorway = WardLayout.A6_MainDoors.Position + Vector3.right * 1.2f;
            zombie.position = scare.doorway;
            zombie.gameObject.SetActive(false);
        }
    }
}
