// Claude-owned integration (owner, 2026-11-13: visible hands for the player, "Yes, find a model"; E-27).
using PatientZero.Core;
using PatientZero.Interaction;
using UnityEngine;
using UnityEngine.XR;

namespace PatientZero.Integration
{
    /// <summary>
    /// E-27: the player's own arms on a flat screen (PC, phone, iPad). The left hand holds the torch; the right hand holds the
    /// revolver once it is taken, and until then hangs out of sight. The arms are the CC0 "fps arms" by para (OpenGameArt,
    /// made with MakeHuman; Assets/ThirdParty/OpenGameArt/FpsArms). The IK of its .blend rig does not survive in FBX, so this
    /// poses them itself every frame, after the revolver has placed the gun: each hand is put on its grip (the torch's handle,
    /// the gun's grip) and a two-bone IK bends the arm to it, the elbow out and down, the forearm turned toward the hand.
    /// E-28 (owner: "the left hand is twisted"): the torch is held in a fist with the thumb on its tail cap and the lens out of
    /// the little finger's side, which keeps the forearm within about 16 degrees of its rest (the E-27 hold turned it 153, and
    /// the model's single forearm bone wrung the skin); the gun's grip leans back like a real one, so the forefinger reaches
    /// into the trigger guard instead of lying over the frame. How the hands hold both is <see cref="Hold"/>.
    /// The fingers were curled round the grips at compose time (FirstPersonArmsStep) and never move. Near a wall the torch
    /// and the gun draw back toward the player (<see cref="DrawBack"/>) instead of going through it. A headset never shows
    /// them: the controllers are the hands there, and the Quest build has no torch.
    /// The beam comes from the torch's lens: Interaction still raises HandheldLightChanged (its writer), from
    /// DesktopInteraction.TorchFromEyes, and this moves that point to the lens each frame (used on the next). Near a wall the
    /// beam starts on the player's side of it. Hidden, the point goes back to Interaction's own.
    /// </summary>
    [DefaultExecutionOrder(1000)] // after Revolver.LateUpdate has placed the gun
    public sealed class FirstPersonArms : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Arm
        {
            [Tooltip("The upper arm, the forearm, the forearm's end (the wrist) and the hand's IK handle (hand.X.control).")]
            public Transform upper, fore, wrist, hand;
            public bool right;
            [Tooltip("Rest pose: the upper arm's and the forearm's local rotations.")]
            public Quaternion upperRest, foreRest;
            [Tooltip("Rest pose: the hand handle's rotation in its grip frame (the fingers along +z, the palm's normal along +y).")]
            public Quaternion inGrip;
            [Tooltip("Rest pose: the hand handle's rotation in the forearm's frame.")]
            public Quaternion inFore;
            [Tooltip("Rest pose: the forearm's own axis (elbow to wrist), in its local space.")]
            public Vector3 foreAxis;
            [Tooltip("Rest pose: the elbow's hinge axis, in the upper arm's local space.")]
            public Vector3 hinge;
            [Tooltip("Rest pose: the forearm's end, from the hand handle, in the hand handle's frame.")]
            public Vector3 wristInHand;
        }

        [Tooltip("Moved to the eyes every frame; the arms model hangs under it with its shoulders below and behind the eyes.")]
        public Transform eyes;
        public Arm left, right;
        [Tooltip("The torch in the left hand: its origin is the middle of the handle, the lens along +z.")]
        public Transform torch;
        public Renderer lens;
        public Material lensOn, lensOff;
        public Revolver revolver;
        [Tooltip("The revolver's grip: its centre, and its +y up the grip toward the hammer.")]
        public Transform gunGrip;
        [Tooltip("Everything shown: the arms and the torch.")]
        public Renderer[] renderers;

        /// <summary>
        /// How the hands hold the torch and the gun. The rake turns a handle across the palm (a power grip runs from the base of
        /// the index finger to the heel of the hand); the reach is from the wrist along the fingers to the handle's centre, the
        /// depth from the wrist's middle to it across the palm. The finger and thumb angles are bent once, at compose time
        /// (FirstPersonArmsStep); the rest is used every frame.
        /// </summary>
        [System.Serializable]
        public sealed class Hold
        {
            [Tooltip("From the eyes: where the left palm faces, into the torch's handle.")]
            public Vector3 torchPalm = new Vector3(.75f, -.65f, 0);
            [Tooltip("The torch's lens comes out of the little finger's side of the fist (the thumb on the tail cap), not the index finger's.")]
            public bool torchReverse = true;
            public float torchRake = 25, torchReach = .1f, torchDepth = .03f;
            public float gunRake = 15, gunReach = .1f, gunDepth = .03f;
            [Tooltip("Metres up the gun's grip from its middle to the middle of the hand (negative: lower).")]
            public float gunSlide;
            [Tooltip("From the eyes: where the left elbow points.")]
            public Vector3 leftElbow = new Vector3(-.55f, -.8f, 0);
            [Tooltip("From the eyes: the torch's handle, low on the left, and where it goes while the revolver is aimed.")]
            public Vector3 torchAt = new Vector3(-.16f, -.14f, .34f), torchAimAt = new Vector3(-.12f, -.14f, .34f);
            [Tooltip("Degrees at the knuckle, the middle and the end joint of each finger; the thumb's first value turns it across the palm.")]
            public Vector3 torchFingers = new Vector3(70, 80, 45), torchThumb = new Vector3(30, 30, 25);
            public Vector3 gunIndex = new Vector3(30, 50, 25), gunFingers = new Vector3(75, 85, 50), gunThumb = new Vector3(25, 25, 20);
        }

        public Hold hold = new Hold();

        /// <summary>From the eyes: the right hand, down at the side and out of sight, until the revolver is taken.</summary>
        public static readonly Vector3 RightDown = new Vector3(.24f, -.62f, .02f);
        /// <summary>From the eyes: where the right elbow points (out and down).</summary>
        public static readonly Vector3 RightElbow = new Vector3(.55f, -.8f, 0);
        public const float RaiseSeconds = .2f, TwistShare = .85f, BreathMetres = .004f, BreathHz = .23f;
        /// <summary>How far ahead a held thing may reach before it meets a wall, and how much of that it can give back.</summary>
        public const float ClearMetres = .55f, MostBack = .3f;
        /// <summary>From the torch's handle: just ahead of the lens, where the beam starts.</summary>
        public const float LensAhead = .135f;

        private float raised;
        private bool torchOn, shown = true;
        private static int clearanceFrame = -1;
        private static Vector3 clearance;
        private static float clearAhead = float.MaxValue;

        public bool Shown => shown;
        public float Raised => raised;

        private void OnEnable() => GameEvents.HandheldLightChanged.Subscribe(OnTorch);

        private void OnDisable() => GameEvents.HandheldLightChanged.Unsubscribe(OnTorch);

        private void Awake()
        {
            if (lens && lensOff) lens.sharedMaterial = lensOff;
        }

        private void OnTorch(HandheldLightChanged change)
        {
            if (change.Enabled == torchOn) return;
            torchOn = change.Enabled;
            if (lens) lens.sharedMaterial = torchOn ? lensOn : lensOff;
        }

        /// <summary>A flat screen: not a headset, and not the Quest build (Android without the touch screen's define).</summary>
        public static bool FlatScreen()
        {
#if UNITY_ANDROID && !PZ_TOUCH
            return false;
#else
            return !XRSettings.isDeviceActive;
#endif
        }

        private void LateUpdate()
        {
            IPlayerQuery player = null;
            bool want = FlatScreen() && Services<IPlayerQuery>.TryGet(out player) && player.HeadForward.sqrMagnitude > 1e-6f;
            if (want != shown)
            {
                shown = want;
                if (renderers != null)
                    foreach (Renderer r in renderers)
                        if (r) r.enabled = want;
                if (!want) DesktopInteraction.TorchFromEyes = DesktopInteraction.TorchFromEyesDefault;
            }

            if (!want) return;
            bool held = revolver && revolver.Held;
            raised = Mathf.MoveTowards(raised, held ? 1 : 0, Time.deltaTime / RaiseSeconds);
            Vector3 eye = player.HeadPosition, forward = player.HeadForward.normalized;
            Quaternion look = Quaternion.LookRotation(forward, Vector3.up);
            Pose(eye, look, raised, revolver ? revolver.Aim : 0, Time.time);
            if (torch)
            {
                Vector3 beam = torch.TransformPoint(0, 0, LensAhead);
                float ahead = Vector3.Dot(beam - eye, forward), most = Mathf.Max(.05f, clearAhead - .06f);
                if (ahead > most) beam -= forward * (ahead - most);
                DesktopInteraction.TorchFromEyes = Quaternion.Inverse(look) * (beam - eye);
            }
        }

        private void OnDestroy() => DesktopInteraction.TorchFromEyes = DesktopInteraction.TorchFromEyesDefault;

        /// <summary>
        /// Poses everything for eyes at <paramref name="eye"/> turned to <paramref name="look"/>: the torch and the left arm; the
        /// right arm on the gun's grip by <paramref name="gunShare"/> (0: down out of sight, 1: on the grip). The gun must already
        /// be where it is drawn this frame. Public for the Editor's preview (ArmsPreview).
        /// </summary>
        public void Pose(Vector3 eye, Quaternion look, float gunShare, float aim, float time)
        {
            if (!eyes || left == null || right == null || !left.upper || !right.upper) return;
            eyes.SetPositionAndRotation(eye, look);

            Vector3 breath = Vector3.up * (BreathMetres * Mathf.Sin(time * BreathHz * Mathf.PI * 2));
            Vector3 handle = Vector3.Lerp(hold.torchAt, hold.torchAimAt, aim) + breath + DrawBack(eye, look * Vector3.forward);
            if (torch)
            {
                torch.SetPositionAndRotation(eye + look * handle, look);
                Grip(left, torch.position, hold.torchReverse ? -torch.forward : torch.forward, look * hold.torchPalm, hold.torchRake, hold.torchReach,
                     hold.torchDepth, out Vector3 at, out Quaternion turn);
                Solve(left, at, turn, eye + look * hold.leftElbow);
            }

            // Down at the side: the fingers down, the palm to the thigh.
            Vector3 handAt = eye + look * RightDown;
            Quaternion handTurn = Quaternion.LookRotation(look * Vector3.down, look * Vector3.left) * right.inGrip;
            if (gunGrip && gunShare > 0)
            {
                Transform gun = gunGrip.parent ? gunGrip.parent : gunGrip;
                Grip(right, gunGrip.position + gunGrip.up * hold.gunSlide, gunGrip.up, -gun.right, hold.gunRake, hold.gunReach, hold.gunDepth,
                     out Vector3 at, out Quaternion turn);
                float k = Mathf.SmoothStep(0, 1, gunShare);
                handAt = Vector3.Lerp(handAt, at, k);
                handTurn = Quaternion.Slerp(handTurn, turn, k);
            }

            Solve(right, handAt, handTurn, eye + look * RightElbow);
        }

        /// <summary>
        /// Where a hand goes to hold a handle centred at <paramref name="centre"/> whose <paramref name="axis"/> runs toward the
        /// thumb and index finger, with the palm's normal toward <paramref name="palm"/> (into the handle).
        /// </summary>
        private static void Grip(Arm arm, Vector3 centre, Vector3 axis, Vector3 palm, float rake, float reach, float depth, out Vector3 at, out Quaternion rotation)
        {
            axis.Normalize();
            Vector3 n = Vector3.ProjectOnPlane(palm, axis).normalized;
            Vector3 across = Vector3.Cross(n, axis);
            float c = Mathf.Cos(rake * Mathf.Deg2Rad), s = Mathf.Sin(rake * Mathf.Deg2Rad);
            // t runs from the little finger's knuckle to the index finger's, f from the wrist along the fingers. For a right hand
            // t = n x f, for a left hand t = f x n; the handle crosses the palm at the rake from t toward f.
            Vector3 t = arm.right ? axis * c + across * s : axis * c - across * s;
            Vector3 f = arm.right ? Vector3.Cross(t, n) : Vector3.Cross(n, t);
            at = centre - f * reach - n * depth;
            rotation = Quaternion.LookRotation(f, n) * arm.inGrip;
        }

        /// <summary>
        /// Two-bone IK: the upper arm and forearm from their rest pose, bent in the arm's own elbow hinge toward
        /// <paramref name="pole"/> so that the forearm ends where the hand puts the wrist; the forearm then turns about itself
        /// toward the hand (TwistShare), so the wrist bends without wringing.
        /// </summary>
        private static void Solve(Arm arm, Vector3 handAt, Quaternion handRotation, Vector3 pole)
        {
            arm.upper.localRotation = arm.upperRest;
            arm.fore.localRotation = arm.foreRest;
            Vector3 target = handAt + handRotation * arm.wristInHand;
            Vector3 s = arm.upper.position, e = arm.fore.position, w = arm.wrist.position;
            float a = (e - s).magnitude, b = (w - e).magnitude;
            if (a < 1e-4f || b < 1e-4f) return;
            Vector3 toTarget = target - s;
            float d = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(a - b) + 1e-3f, a + b - 1e-3f);
            Vector3 dir = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : (e - s).normalized;
            Vector3 bend = Vector3.ProjectOnPlane(pole - s, dir);
            if (bend.sqrMagnitude < 1e-8f) bend = Vector3.ProjectOnPlane(Vector3.down, dir);
            bend.Normalize();
            float cos = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1f, 1f);
            Vector3 elbow = s + dir * (a * cos) + bend * (a * Mathf.Sqrt(1 - cos * cos));
            Vector3 reach = s + dir * d;

            arm.upper.rotation = Quaternion.FromToRotation(e - s, elbow - s) * arm.upper.rotation;
            Vector3 along = (elbow - s).normalized;
            Vector3 hingeNow = Vector3.ProjectOnPlane(arm.upper.TransformDirection(arm.hinge), along);
            Vector3 hingeWant = Vector3.ProjectOnPlane(Vector3.Cross(elbow - s, reach - elbow), along);
            if (hingeNow.sqrMagnitude > 1e-8f && hingeWant.sqrMagnitude > 1e-8f)
                arm.upper.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(hingeNow, hingeWant, along), along) * arm.upper.rotation;

            e = arm.fore.position;
            w = arm.wrist.position;
            arm.fore.rotation = Quaternion.FromToRotation(w - e, reach - e) * arm.fore.rotation;
            Quaternion delta = Quaternion.Inverse(arm.fore.rotation) * (handRotation * Quaternion.Inverse(arm.inFore));
            Vector3 v = Vector3.Project(new Vector3(delta.x, delta.y, delta.z), arm.foreAxis);
            float m = Mathf.Sqrt(v.sqrMagnitude + delta.w * delta.w);
            if (m > 1e-6f)
            {
                var twist = new Quaternion(v.x / m, v.y / m, v.z / m, delta.w / m);
                arm.fore.rotation = arm.fore.rotation * Quaternion.Slerp(Quaternion.identity, twist, TwistShare);
            }

            arm.hand.SetPositionAndRotation(handAt, handRotation);
        }

        /// <summary>How far the forearm ends from where the hand puts the wrist (0 when the arm reaches).</summary>
        public static float WristGap(Arm arm) =>
            arm == null || !arm.wrist || !arm.hand ? float.MaxValue : Vector3.Distance(arm.wrist.position, arm.hand.position + arm.hand.rotation * arm.wristInHand);

        /// <summary>
        /// From the eyes: how far a held thing draws back (and down) so it does not reach into what the player faces. Measured
        /// once a frame with a thin sphere cast; the revolver uses it too.
        /// </summary>
        public static Vector3 DrawBack(Vector3 eye, Vector3 forward)
        {
            if (clearanceFrame == Time.frameCount) return clearance;
            clearanceFrame = Time.frameCount;
            clearance = Vector3.zero;
            clearAhead = float.MaxValue;
            if (forward.sqrMagnitude < 1e-6f) return clearance;
            if (Physics.SphereCast(eye, .06f, forward.normalized, out RaycastHit hit, ClearMetres, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                clearAhead = hit.distance;
                float back = Mathf.Min(MostBack, ClearMetres - hit.distance);
                clearance = new Vector3(0, -.5f * back, -.7f * back);
            }

            return clearance;
        }
    }
}
