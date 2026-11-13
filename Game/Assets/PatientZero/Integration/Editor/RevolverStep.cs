// Claude-owned integration (owner, 2026-11-12: "Build the gun next"; plan step 4; E-25).
using System;
using System.Collections.Generic;
using PatientZero.AI;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Object = UnityEngine.Object;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// The revolver of the owner's two-minute plan (step 4), built at compose time after the dead bodies (Ward 988):
    ///   a dead police officer lying across the floor-3 corridor, halfway along, in a pool of blood (the zombie model frozen in
    ///   its Death pose, in a dark uniform tint, his cap beside him);
    ///   the revolver on the floor by his right hand: a model made of plain shapes (no download), six rounds in the cylinder,
    ///   a grip, a muzzle and a muzzle-flash quad; one box collider and a kinematic body for the headset's hands (XRI);
    ///   the hip holster for the headset; blood and dust specks for the hits;
    ///   a <see cref="ZombieHitbox"/> on every dressed zombie, its head ellipsoid and body capsule measured from the model's mesh.
    /// No light, AI or navigation change; nothing here can block a path (the officer has no collider, the gun is 25 cm long).
    /// </summary>
    public static class RevolverStep
    {
        /// <summary>Where the officer lies (floor x, z): halfway along the floor-3 corridor, under the drip, head to the north wall.</summary>
        public static readonly Vector3 Officer = new Vector3(6.8f, 0, 3.7f);

        [SceneBuildStep(BuildScene.Ward, 988)]
        public static void Build(Transform root)
        {
            ZombieHitbox[] targets = Hitboxes(root);
            ZombieArt.Kit kit = ZombieArt.Prepare();
            var holder = new GameObject("Revolver").transform;
            holder.SetParent(root, false);

            Transform officer = PlaceOfficer(kit, holder, out Transform hand);
            Vector3 floor = DeadBodiesStep.Floor(new Vector3(hand.position.x, 0, Mathf.Clamp(hand.position.z - .22f, 2.35f, 4.15f)));
            Transform model = Gun(holder, out Transform muzzle, out Renderer flash, out GameObject[] rounds, out BoxCollider pickup);
            // Lying on its side by his hand, the barrel pointing back along the corridor.
            model.rotation = Quaternion.Euler(0, -100, 90);
            model.position = floor;
            Bounds b = RendererBounds(model);
            model.position += new Vector3(floor.x - b.center.x, floor.y + .002f - b.min.y, floor.z - b.center.z);
            DeadBodiesStep.RequireRoom(model.position, "revolver");

            var body = model.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            // No interpolation: on a flat screen the revolver places the carried gun itself every frame.
            body.interpolation = RigidbodyInterpolation.None;
            var grab = model.gameObject.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.attachTransform = model.Find("Grip");

            var holster = new GameObject("Hip holster");
            holster.transform.SetParent(holder, false);
            var ring = holster.AddComponent<SphereCollider>();
            ring.isTrigger = true;
            ring.radius = .14f;
            var socket = holster.AddComponent<HolsterSocket>();
            socket.accepts = grab;
            socket.showInteractableHoverMeshes = false;
            holster.SetActive(false); // the revolver switches it on, only in a headset and only once the gun is taken

            var revolver = holder.gameObject.AddComponent<Revolver>();
            revolver.model = model;
            revolver.muzzle = muzzle;
            revolver.flash = flash;
            revolver.rounds = rounds;
            revolver.pickup = pickup;
            revolver.grab = grab;
            revolver.holster = socket;
            revolver.targets = targets;
            revolver.blood = Specks(holder, "Blood specks", new Color(.32f, .02f, .02f, 1), .012f, .035f, 2.4f);
            revolver.dust = Specks(holder, "Dust specks", new Color(.42f, .40f, .36f, 1), .006f, .02f, 1.6f);
            Debug.Log("[PZ] revolver by the officer at " + Round(model.position) + " (officer " + Round(officer.position) + "); " +
                      targets.Length + " zombies can be shot");
        }

        // ------------------------------------------------------------------ the officer

        private static Transform PlaceOfficer(ZombieArt.Kit kit, Transform parent, out Transform hand)
        {
            Material pool = AssetDatabase.LoadAssetAtPath<Material>(ArtTextures.Folder + "/M_Blood_Pool.mat");
            if (!pool) throw new InvalidOperationException("The blood pool material is missing: the art pass (Ward 980) must come first.");
            // The zombie model in a dark navy tint: a uniform, not a patient's gown.
            Material uniform = DeadBodiesStep.Skin(kit, "Officer_Uniform", new Color(.30f, .34f, .46f, 1));
            Vector3 floor = DeadBodiesStep.Floor(Officer);
            DeadBodiesStep.RequireRoom(floor, "police officer");
            Transform body = ZombieArt.Posed(kit, parent, "Dead police officer", "Death", 99f, uniform, out Vector3 size);
            float yaw = size.x >= size.z ? 0 : 90;
            body.SetPositionAndRotation(floor, Quaternion.Euler(0, yaw, 0));
            // Head to the north wall, so he lies across the corridor and the gun by his hand is in the open.
            Transform head = Bone(body, "Head");
            if (head.position.z < floor.z) body.rotation = Quaternion.Euler(0, yaw + 180, 0);
            Vector3 shift = DeadBodiesStep.IntoRoom(ZombieArt.WorldBounds(body), floor);
            body.position += shift;
            DeadBodiesStep.Pool(body, pool, body.position, 1.4f);
            hand = Bone(body, "Middle1.R");
            Cap(parent, head.position, body.position);
            Debug.Log("[PZ] dead police officer: " + size.x.ToString("0.00") + " x " + size.z.ToString("0.00") + " m, moved " +
                      shift.magnitude.ToString("0.00") + " m off the walls; right hand at " + Round(hand.position));
            return body;
        }

        /// <summary>His cap, fallen off beside his head.</summary>
        private static void Cap(Transform parent, Vector3 head, Vector3 body)
        {
            Material navy = Paint("Officer_Cap", new Color(.07f, .08f, .13f, 1), 0, .3f);
            Material badge = Paint("Officer_Badge", new Color(.75f, .62f, .25f, 1), 1, .7f);
            Vector3 floor = DeadBodiesStep.Floor(new Vector3(head.x + .35f, 0, head.z));
            var cap = new GameObject("Police cap").transform;
            cap.SetParent(parent, false);
            cap.SetPositionAndRotation(floor, Quaternion.Euler(0, 35, 0));
            Part(cap, "Crown", PrimitiveType.Cylinder, navy, new Vector3(0, .045f, 0), Vector3.zero, new Vector3(.22f, .04f, .24f));
            Part(cap, "Band", PrimitiveType.Cylinder, navy, new Vector3(0, .02f, 0), Vector3.zero, new Vector3(.19f, .02f, .21f));
            Part(cap, "Peak", PrimitiveType.Cube, navy, new Vector3(0, .006f, .13f), new Vector3(-8, 0, 0), new Vector3(.17f, .008f, .08f));
            Part(cap, "Badge", PrimitiveType.Cube, badge, new Vector3(0, .05f, .105f), Vector3.zero, new Vector3(.03f, .035f, .006f));
            DeadBodiesStep.RequireRoom(floor, "police cap");
        }

        // ------------------------------------------------------------------ the gun

        /// <summary>A revolver about 25 cm long made of plain shapes: the grip at its origin, the barrel along +z.</summary>
        internal static Transform Gun(Transform parent, out Transform muzzle, out Renderer flash, out GameObject[] rounds, out BoxCollider pickup)
        {
            Material steel = Paint("Revolver_Steel", new Color(.07f, .07f, .075f, 1), .9f, .55f);
            Material wood = Paint("Revolver_Grip", new Color(.17f, .09f, .05f, 1), 0, .35f);
            Material brass = Paint("Revolver_Brass", new Color(.78f, .58f, .22f, 1), 1, .65f);
            var gun = new GameObject("Revolver model").transform;
            gun.SetParent(parent, false);

            // E-28: the grip's bottom sweeps back, as a revolver's does (it leaned forward, which pointed the hand's fingers up
            // and laid the forefinger over the frame).
            Part(gun, "Grip", PrimitiveType.Cube, wood, new Vector3(0, -.05f, -.035f), new Vector3(18, 0, 0), new Vector3(.03f, .1f, .042f));
            Part(gun, "Frame", PrimitiveType.Cube, steel, new Vector3(0, .008f, .035f), Vector3.zero, new Vector3(.028f, .042f, .085f));
            Part(gun, "Cylinder", PrimitiveType.Cylinder, steel, new Vector3(0, .014f, .047f), new Vector3(90, 0, 0), new Vector3(.044f, .021f, .044f));
            Part(gun, "Barrel", PrimitiveType.Cylinder, steel, new Vector3(0, .024f, .13f), new Vector3(90, 0, 0), new Vector3(.017f, .06f, .017f));
            Part(gun, "Rib", PrimitiveType.Cube, steel, new Vector3(0, .035f, .12f), Vector3.zero, new Vector3(.01f, .008f, .15f));
            Part(gun, "Front sight", PrimitiveType.Cube, steel, new Vector3(0, .042f, .186f), Vector3.zero, new Vector3(.004f, .01f, .008f));
            Part(gun, "Hammer", PrimitiveType.Cube, steel, new Vector3(0, .036f, -.004f), new Vector3(-25, 0, 0), new Vector3(.008f, .022f, .012f));
            Part(gun, "Trigger guard", PrimitiveType.Cube, steel, new Vector3(0, -.03f, .03f), Vector3.zero, new Vector3(.006f, .006f, .05f));
            Part(gun, "Trigger", PrimitiveType.Cube, steel, new Vector3(0, -.016f, .018f), new Vector3(15, 0, 0), new Vector3(.005f, .024f, .006f));

            // The six rounds, seen at the back of the cylinder.
            rounds = new GameObject[6];
            for (int i = 0; i < rounds.Length; i++)
            {
                float a = i * Mathf.PI * 2 / rounds.Length;
                var at = new Vector3(Mathf.Cos(a) * .013f, .014f + Mathf.Sin(a) * .013f, .0255f);
                rounds[i] = Part(gun, "Round " + (i + 1), PrimitiveType.Cylinder, brass, at, new Vector3(90, 0, 0), new Vector3(.0085f, .0012f, .0085f)).gameObject;
            }

            var grip = new GameObject("Grip").transform;
            grip.SetParent(gun, false);
            grip.localPosition = new Vector3(0, -.04f, -.02f);
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gun, false);
            muzzle.localPosition = new Vector3(0, .024f, .195f);
            var quad = Part(gun, "Muzzle flash", PrimitiveType.Quad, Flash(), new Vector3(0, .024f, .25f), Vector3.zero, new Vector3(.16f, .16f, 1));
            flash = quad.GetComponent<Renderer>();
            flash.shadowCastingMode = ShadowCastingMode.Off;
            flash.enabled = false;

            pickup = gun.gameObject.AddComponent<BoxCollider>();
            pickup.center = new Vector3(0, -.005f, .08f);
            pickup.size = new Vector3(.04f, .13f, .23f);
            return gun;
        }

        internal static Transform Part(Transform parent, string name, PrimitiveType shape, Material material, Vector3 at, Vector3 euler, Vector3 scale)
        {
            GameObject o = GameObject.CreatePrimitive(shape);
            o.name = name;
            Object.DestroyImmediate(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false);
            o.transform.localPosition = at;
            o.transform.localRotation = Quaternion.Euler(euler);
            o.transform.localScale = scale;
            o.GetComponent<Renderer>().sharedMaterial = material;
            return o.transform;
        }

        internal static Material Paint(string name, Color color, float metallic, float smoothness)
        {
            Material m = ArtSurfaces.LitMaterial(name);
            m.SetTexture("_BaseMap", null);
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A hot, self-lit star with a cut-out edge: seen for one frame or two (Revolver.FlashSeconds).</summary>
        private static Material Flash()
        {
            ArtTextures.Pair star = ArtTextures.Ensure("MuzzleFlash", 128, 128, 1f, 0f, PaintFlash, cutout: true);
            Material m = ArtSurfaces.LitMaterial("Revolver_Flash");
            m.SetTexture("_BaseMap", star.Albedo);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
            m.SetFloat("_Metallic", 0);
            m.SetFloat("_Smoothness", 0);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", star.Albedo);
            m.SetColor("_EmissionColor", new Color(6f, 4.2f, 1.8f, 1));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetFloat("_Surface", 0);
            m.SetFloat("_AlphaClip", 1);
            m.SetFloat("_Cutoff", .4f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void PaintFlash(ArtTextures.Canvas c)
        {
            for (int y = 0; y < c.H; y++)
            for (int x = 0; x < c.W; x++)
            {
                float u = (x + .5f) / c.W * 2 - 1, v = (y + .5f) / c.H * 2 - 1;
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                // Five uneven spikes over a round core.
                float spikes = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f + .4f)), 6) * (.75f + .25f * Mathf.Sin(a * 7));
                float reach = .34f + .62f * spikes;
                float alpha = Mathf.Clamp01((reach - r) / .12f);
                float heat = Mathf.Clamp01(1 - r / reach);
                c.Albedo[y * c.W + x] = new Color(1, .55f + .45f * heat, .2f + .7f * heat * heat, alpha);
            }
        }

        private static ParticleSystem Specks(Transform parent, string name, Color color, float smallest, float largest, float speed)
        {
            var o = new GameObject(name);
            o.transform.SetParent(parent, false);
            var system = o.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.25f, .6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(smallest, largest);
            main.startColor = color;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false; // Revolver emits a burst per hit
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28;
            shape.radius = .01f;
            var renderer = o.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = SpeckMaterial(name.Replace(" ", "_"), color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static Material SpeckMaterial(string name, Color color)
        {
            const string shaderName = "Universal Render Pipeline/Particles/Unlit";
            Shader shader = Shader.Find(shaderName);
            if (!shader) throw new InvalidOperationException("Shader '" + shaderName + "' not found: URP must be the active pipeline before compose.");
            ArtTextures.CreateFolders();
            string path = ArtTextures.Folder + "/M_" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(shader) { name = "M_" + name };
                AssetDatabase.CreateAsset(m, path);
            }

            m.shader = shader;
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ where a zombie can be hit

        /// <summary>A <see cref="ZombieHitbox"/> on every zombie that wears the model, sized from its own skinned mesh.</summary>
        private static ZombieHitbox[] Hitboxes(Transform root)
        {
            var all = new List<ZombieHitbox>();
            Vector3 headSize = Vector3.zero;
            float bodyRadius = 0, headUp = 0;
            var zombies = new List<ZombieAgentShell>();
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects()) zombies.AddRange(sceneRoot.GetComponentsInChildren<ZombieAgentShell>(true));
            foreach (ZombieAgentShell zombie in zombies)
            {
                SkinnedMeshRenderer mesh = null;
                foreach (SkinnedMeshRenderer s in zombie.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (s.name == "Zombie" && s.transform.parent && HasAncestorNamed(s.transform, ZombieArt.ModelName)) { mesh = s; break; }
                if (!mesh) throw new InvalidOperationException(zombie.name + " does not wear the zombie model: the art pass (Ward 980) must come first.");
                Transform model = Ancestor(mesh.transform, ZombieArt.ModelName);
                var box = zombie.gameObject.AddComponent<ZombieHitbox>();
                box.zombie = zombie;
                box.head = Bone(model, "Head");
                box.hips = Bone(model, "Hips");
                box.neck = Bone(model, "Neck");
                Measure(mesh, box);
                headSize += new Vector3(box.head.TransformVector(Vector3.right * box.headRadii.x).magnitude,
                                        box.head.TransformVector(Vector3.up * box.headRadii.y).magnitude,
                                        box.head.TransformVector(Vector3.forward * box.headRadii.z).magnitude) * 2;
                bodyRadius += box.bodyRadius;
                headUp += box.HeadCentre.y - zombie.transform.position.y;
                all.Add(box);
            }

            if (all.Count == 0) throw new InvalidOperationException("Ward has no zombie to shoot (the AI step 500 and the art pass 980 come first).");
            headSize /= all.Count;
            Debug.Log("[PZ] zombie hit areas: " + all.Count + " zombies; on average the head " + headSize.x.ToString("0.00") + " x " + headSize.y.ToString("0.00") +
                      " x " + headSize.z.ToString("0.00") + " m (along the head bone's axes), its centre " + (headUp / all.Count).ToString("0.00") +
                      " m up; the body capsule " + (bodyRadius / all.Count).ToString("0.000") + " m round");
            return all.ToArray();
        }

        /// <summary>
        /// Skins the mesh on the CPU in its current pose and takes the vertices each bone moves most: the head's (with its tongue
        /// and eyelids) give the head ellipsoid, in the head bone's space, spanning the 2nd to the 98th percentile of them along
        /// each of its axes (the model's head is a big cartoon one: a sphere fitted round it would reach far past the skull or fall
        /// short of its top); the hips', abdomen's, torso's and neck's give the body capsule, whose radius covers 70% of them, so
        /// a fold of cloth or the top of a thigh does not make a zombie easier to hit than it looks.
        /// </summary>
        private static void Measure(SkinnedMeshRenderer renderer, ZombieHitbox box)
        {
            Mesh mesh = renderer.sharedMesh;
            Transform[] bones = renderer.bones;
            Matrix4x4[] bindposes = mesh.bindposes;
            BoneWeight[] weights = mesh.boneWeights;
            Vector3[] vertices = mesh.vertices;
            var skin = new Matrix4x4[bones.Length];
            var part = new int[bones.Length]; // 0 none, 1 head, 2 body
            for (int i = 0; i < bones.Length; i++)
            {
                if (!bones[i]) throw new InvalidOperationException(ZombieArt.ModelPath + ": bone " + i + " is missing.");
                skin[i] = bones[i].localToWorldMatrix * bindposes[i];
                string n = bones[i].name;
                part[i] = bones[i] == box.head || bones[i].IsChildOf(box.head) ? 1 : n == "Hips" || n == "Abdomen" || n == "Torso" || n == "Neck" ? 2 : 0;
            }

            var head = new List<Vector3>();
            var body = new List<float>();
            Vector3 a = box.hips.position, b = box.neck.position;
            for (int v = 0; v < vertices.Length && v < weights.Length; v++)
            {
                BoneWeight w = weights[v];
                float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                if (sum <= 0) continue;
                int dominant = w.boneIndex0;
                float most = w.weight0;
                if (w.weight1 > most) { most = w.weight1; dominant = w.boneIndex1; }
                if (w.weight2 > most) { most = w.weight2; dominant = w.boneIndex2; }
                if (w.weight3 > most) dominant = w.boneIndex3;
                if (part[dominant] == 0) continue;
                Vector3 p = vertices[v];
                Vector3 world = (skin[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 + skin[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 +
                                 skin[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 + skin[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3) / sum;
                if (part[dominant] == 1) head.Add(world);
                else body.Add(DistanceToSegment(world, a, b));
            }

            if (head.Count < 20 || body.Count < 20)
                throw new InvalidOperationException(ZombieArt.ModelPath + ": only " + head.Count + " head and " + body.Count + " body vertices to measure.");
            var xs = new List<float>(head.Count);
            var ys = new List<float>(head.Count);
            var zs = new List<float>(head.Count);
            foreach (Vector3 p in head)
            {
                Vector3 local = box.head.InverseTransformPoint(p);
                xs.Add(local.x);
                ys.Add(local.y);
                zs.Add(local.z);
            }

            var low = new Vector3(Percentile(xs, .02f), Percentile(ys, .02f), Percentile(zs, .02f));
            var high = new Vector3(Percentile(xs, .98f), Percentile(ys, .98f), Percentile(zs, .98f));
            box.headCentre = (low + high) * .5f;
            box.headRadii = (high - low) * .5f;
            if (box.headRadii.x <= 0 || box.headRadii.y <= 0 || box.headRadii.z <= 0)
                throw new InvalidOperationException(ZombieArt.ModelPath + ": the head measured flat (" + box.headRadii + ").");
            box.bodyRadius = Mathf.Clamp(Percentile(body, .7f), .09f, .4f);
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude < 1e-8f ? 0 : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector3.Distance(p, a + ab * t);
        }

        private static float Percentile(List<float> values, float q)
        {
            values.Sort();
            return values[Mathf.Clamp(Mathf.RoundToInt(q * (values.Count - 1)), 0, values.Count - 1)];
        }

        // ------------------------------------------------------------------ helpers

        private static Transform Bone(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            throw new InvalidOperationException(ZombieArt.ModelPath + " has no bone '" + name + "' under " + root.name + ".");
        }

        private static bool HasAncestorNamed(Transform t, string name) => Ancestor(t, name);

        private static Transform Ancestor(Transform t, string name)
        {
            for (Transform p = t; p; p = p.parent)
                if (p.name == name) return p;
            return null;
        }

        private static Bounds RendererBounds(Transform root)
        {
            bool any = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Muzzle flash") continue;
                if (!any) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                any = true;
            }

            return bounds;
        }

        private static string Round(Vector3 p) =>
            "(" + p.x.ToString("0.00") + ", " + p.y.ToString("0.00") + ", " + p.z.ToString("0.00") + ")";
    }
}
