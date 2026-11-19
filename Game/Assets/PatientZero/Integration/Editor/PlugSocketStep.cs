// Claude-owned integration (E-29, 2026-11-18). Owner's playtest: "make the plug looks like a plug and socket".
using System;
using PatientZero.Core;
using PatientZero.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PatientZero.Integration.Editor
{
    /// <summary>
    /// Dresses floor 3's cable puzzle (seat A's CableSocket, built at Ward 310 as two plain boxes) as a real mains plug and a
    /// wall socket. The socket: a surface-mounted box on the corridor's north wall that fills exactly the socket's collider
    /// in front of the wall, an off-white faceplate with the three slots of a UK socket and a red rocker switch (a hospital's
    /// essential-supply socket), and a steel conduit up the wall. The plug: black rubber with three brass pins, a cable gland
    /// and the cable, placed so that when the plug snaps into the socket (its transform onto the socket's) the body sits on the
    /// faceplate and the pins are in the slots. The cable now runs from a steel junction box on the wall above the shelf to
    /// the plug's gland. The plug's collider moves onto the body it now shows, the hand holds the body (the grab's attach point),
    /// and the plug starts where that body rests on the middle of its shelf, turned with its pins toward the corridor so they
    /// can be seen. Its fixed grip aligns the pins with the hand when grabbed; the shelf rotation is not a required extra
    /// turn in VR. The snap rule and the socket's collider stay seat A's.
    /// </summary>
    public static class PlugSocketStep
    {
        // The plug body (metres): a UK plug is about 50 wide, 55 tall and 45 deep, with 18 mm pins.
        private const float BodyW = .05f, BodyH = .055f, BodyD = .045f, Pin = .018f;

        [SceneBuildStep(BuildScene.Ward, 990)]
        public static void Build(Transform root)
        {
            CableSocket socket = null;
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
                foreach (CableSocket s in sceneRoot.GetComponentsInChildren<CableSocket>(true))
                    if (s.socketObjective == ObjectiveId.Act2_CablePlugged) socket = s;
            if (!socket || !socket.plug) throw new InvalidOperationException("Ward has no floor 3 cable socket with a plug (Interaction step 310).");

            Transform face = socket.transform;
            Renderer box = Find<Renderer>(face, "Socket");
            BoxCollider guard = box ? box.GetComponent<BoxCollider>() : null;
            if (!box || !guard) throw new InvalidOperationException("The cable socket has no 'Socket' box with a collider (Interaction step 310).");
            Renderer plugBox = Find<Renderer>(socket.plug.transform, "Cable plug geometry");
            if (!plugBox) throw new InvalidOperationException("The cable plug has no 'Cable plug geometry' (Interaction step 310).");

            // The socket's collider, in the socket's frame (+z into the wall): its front face and where the wall is.
            Vector3 half = Vector3.Scale(box.transform.localScale, guard.size) * .5f;
            float front = box.transform.localPosition.z - half.z;
            float wall = WallBehind(face, box.transform.localPosition.z + half.z, socket);

            Material plastic = RevolverStep.Paint("Socket_Plastic", new Color(.84f, .83f, .78f, 1), 0, .45f);
            Material slot = RevolverStep.Paint("Socket_Slot", new Color(.03f, .03f, .03f, 1), 0, .1f);
            Material rocker = RevolverStep.Paint("Socket_Switch_Red", new Color(.62f, .07f, .05f, 1), 0, .55f);
            Material rubber = RevolverStep.Paint("Plug_Rubber", new Color(.045f, .045f, .045f, 1), 0, .3f);
            Material brass = RevolverStep.Paint("Plug_Brass", new Color(.8f, .62f, .28f, 1), 1, .72f);
            Material steel = new ArtSurfaces().Metal;

            box.enabled = false;
            var socketArt = new GameObject("Socket art (E-29)").transform;
            socketArt.SetParent(face, false);
            float depth = Mathf.Max(.01f, wall - front);
            Vector3 size = new Vector3(half.x * 2, half.y * 2, depth);
            Part(socketArt, "Back box", PrimitiveType.Cube, plastic, new Vector3(0, 0, front + depth * .5f), Vector3.zero, size);
            float plateZ = front - .003f;
            Part(socketArt, "Faceplate", PrimitiveType.Cube, plastic, new Vector3(0, 0, plateZ), Vector3.zero, new Vector3(size.x - .01f, size.y - .01f, .006f));
            // UK pattern: the earth slot above, live and neutral below; the rocker switch above the socket.
            float slotZ = plateZ - .0031f;
            Part(socketArt, "Earth slot", PrimitiveType.Cube, slot, new Vector3(0, .004f, slotZ), Vector3.zero, new Vector3(.0045f, .0085f, .001f));
            Part(socketArt, "Live slot", PrimitiveType.Cube, slot, new Vector3(.011f, -.018f, slotZ), Vector3.zero, new Vector3(.007f, .0045f, .001f));
            Part(socketArt, "Neutral slot", PrimitiveType.Cube, slot, new Vector3(-.011f, -.018f, slotZ), Vector3.zero, new Vector3(.007f, .0045f, .001f));
            Part(socketArt, "Switch", PrimitiveType.Cube, rocker, new Vector3(0, size.y * .5f - .03f, plateZ - .006f), new Vector3(-8, 0, 0), new Vector3(.022f, .03f, .01f));
            float top = face.position.y + size.y * .5f, ceiling = Ceiling(face, wall);
            if (ceiling > top + .1f)
            {
                float run = ceiling - top;
                Part(socketArt, "Conduit", PrimitiveType.Cylinder, steel, new Vector3(0, size.y * .5f + run * .5f, wall - .014f), Vector3.zero, new Vector3(.025f, run * .5f, .025f));
            }

            // The plug: its frame is the socket's once it snaps, so the body goes on the faceplate and the pins into the slots.
            Transform plug = socket.plug.transform;
            plugBox.enabled = false;
            var plugArt = new GameObject("Plug art (E-29)").transform;
            plugArt.SetParent(plug, false);
            float bodyBack = plateZ - .003f - BodyD;      // the body's back, in the socket's frame
            float bodyMid = bodyBack + BodyD * .5f;
            // The old box was centred on the plug's origin; the body keeps its bottom (y) and stands where the box stood (z).
            float bodyY = -plugBox.transform.localScale.y * .5f + BodyH * .5f;
            Part(plugArt, "Body", PrimitiveType.Cube, rubber, new Vector3(0, bodyY, bodyMid), Vector3.zero, new Vector3(BodyW, BodyH, BodyD));
            Part(plugArt, "Grip ridge L", PrimitiveType.Cube, rubber, new Vector3(BodyW * .5f, bodyY, bodyMid), Vector3.zero, new Vector3(.004f, BodyH * .8f, BodyD * .7f));
            Part(plugArt, "Grip ridge R", PrimitiveType.Cube, rubber, new Vector3(-BodyW * .5f, bodyY, bodyMid), Vector3.zero, new Vector3(.004f, BodyH * .8f, BodyD * .7f));
            Transform geometry = plugBox.transform;
            geometry.localPosition = new Vector3(0, bodyY, bodyMid);
            geometry.localRotation = Quaternion.identity;
            geometry.localScale = new Vector3(BodyW, BodyH, BodyD);
            plug.position -= plug.TransformVector(new Vector3(0, 0, bodyMid));
            plug.RotateAround(geometry.position, Vector3.up, 180); // pins to the corridor, cable to the wall
            var grip = new GameObject("Grip").transform;
            grip.SetParent(plugArt, false);
            grip.localPosition = new Vector3(0, bodyY, bodyMid);
            var grab = plug.GetComponent<XRGrabInteractable>();
            if (!grab) throw new InvalidOperationException("The cable plug has no XRGrabInteractable (Interaction step 310).");
            grab.attachTransform = grip;
            float pinMid = plateZ - .003f + Pin * .5f;
            Part(plugArt, "Earth pin", PrimitiveType.Cube, brass, new Vector3(0, .004f, pinMid), Vector3.zero, new Vector3(.004f, .008f, Pin));
            Part(plugArt, "Live pin", PrimitiveType.Cube, brass, new Vector3(.011f, -.018f, pinMid), Vector3.zero, new Vector3(.0064f, .004f, Pin));
            Part(plugArt, "Neutral pin", PrimitiveType.Cube, brass, new Vector3(-.011f, -.018f, pinMid), Vector3.zero, new Vector3(.0064f, .004f, Pin));
            Transform gland = Part(plugArt, "Cable gland", PrimitiveType.Cylinder, rubber, new Vector3(0, bodyY - .016f, bodyBack - .012f), new Vector3(90, 0, 0), new Vector3(.016f, .012f, .016f));
            var cableEnd = new GameObject("Cable end").transform;
            cableEnd.SetParent(plugArt, false);
            cableEnd.localPosition = gland.localPosition + Vector3.back * .012f;

            // The cable's other end: a steel junction box on the wall above the hook shelf, where the plug starts.
            Vector3 hook = geometry.position;
            float hookWall = WallBehind(face, wall, socket, hook);
            var junction = new GameObject("Cable junction box (E-29)").transform;
            junction.SetParent(face, false);
            junction.position = new Vector3(hook.x, hook.y + .26f, face.position.z);
            Part(junction, "Box", PrimitiveType.Cube, steel, new Vector3(0, 0, hookWall - .03f), Vector3.zero, new Vector3(.1f, .1f, .06f));
            var cableStart = new GameObject("Cable start").transform;
            cableStart.SetParent(junction, false);
            cableStart.localPosition = new Vector3(0, -.05f, hookWall - .03f);
            socket.cableStart = cableStart;
            socket.cableEnd = cableEnd;
            if (socket.cable)
            {
                socket.cable.startWidth = socket.cable.endWidth = .011f;
                socket.cable.numCapVertices = 2;
                socket.cable.shadowCastingMode = ShadowCastingMode.Off;
            }

            EditorUtility.SetDirty(socket);
            AssetDatabase.SaveAssets();
            Debug.Log("[PZ] plug and socket: socket box " + size.x.ToString("0.000") + " x " + size.y.ToString("0.000") + " x " + depth.ToString("0.000") +
                      " m on the wall " + (wall - front).ToString("0.000") + " m behind its front; plug body " + BodyW + " x " + BodyH + " x " + BodyD +
                      " m with 3 pins; conduit " + (ceiling > top + .1f ? (ceiling - top).ToString("0.00") + " m" : "none") + "; cable from the junction box to the plug");
        }

        /// <summary>
        /// The wall behind the socket, as a z in the socket's frame: a ray from in front of <paramref name="from"/> (world; the
        /// socket itself by default) into the wall, ignoring the puzzle's own colliders. Falls back to <paramref name="fallback"/>.
        /// </summary>
        private static float WallBehind(Transform face, float fallback, CableSocket socket, Vector3? from = null)
        {
            Physics.SyncTransforms();
            Vector3 origin = (from ?? face.position) - face.forward * .5f;
            float best = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, face.forward, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(face) || hit.collider.transform.IsChildOf(socket.plug.transform)) continue;
                if (hit.distance < best) best = hit.distance;
            }

            if (best == float.MaxValue) return fallback;
            return face.InverseTransformPoint(origin + face.forward * best).z;
        }

        /// <summary>The ceiling above the socket (world y), from a ray up the wall; the wall's top at 2.7 m if none is found.</summary>
        private static float Ceiling(Transform face, float wall)
        {
            Vector3 origin = face.TransformPoint(new Vector3(0, 0, wall - .03f));
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.up, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(face) && hit.distance > .1f) return origin.y + hit.distance;
            return 2.7f;
        }

        private static T Find<T>(Transform root, string name) where T : Component
        {
            foreach (T c in root.GetComponentsInChildren<T>(true))
                if (c.name == name) return c;
            return null;
        }

        private static Transform Part(Transform parent, string name, PrimitiveType shape, Material material, Vector3 at, Vector3 euler, Vector3 scale)
        {
            Transform t = RevolverStep.Part(parent, name, shape, material, at, euler, scale);
            t.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; // centimetre parts: their shadows cost more than they show
            return t;
        }
    }
}
