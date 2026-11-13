// Claude-owned integration (owner, 2026-11-12: "Build the gun next"; plan step 4; E-25).
using PatientZero.Core;
using PatientZero.Integration.Logic;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PatientZero.Integration
{
    /// <summary>
    /// The revolver of the owner's two-minute plan (step 4, rules in <see cref="RevolverRules"/>): it lies by the dead police
    /// officer on floor 3. On a flat screen the player looks at it and clicks to take it; then holding the right mouse button
    /// raises it to the eye and the left button fires (FlatInput); with the right button up, the left one still grabs. In a
    /// headset it is grabbed like any prop, the trigger fires, and letting go puts it in the hip holster (<see cref="HolsterSocket"/>).
    /// Every shot is the RevolverShot cue, whose noise (20 m, CueTable) draws every zombie in reach to where it was fired; the
    /// sound goes out before the bullet lands. The bullet is a ray from the eye (flat screen) or the muzzle (headset) that stops
    /// at the first wall and is tested against every zombie's head and body (<see cref="ZombieHitbox"/>): a headshot kills, a
    /// body shot staggers. Six rounds, never reloaded, nothing on screen counts them; a seventh pull only clicks. A new life (the
    /// bed) puts the gun back by the officer, full. The muzzle flash is a self-lit quad, not a light (lights belong to Atmosphere
    /// and Environment).
    /// </summary>
    public sealed class Revolver : MonoBehaviour
    {
        [Tooltip("The gun itself: the grip at its origin, the barrel along +z.")]
        public Transform model;
        public Transform muzzle;
        public Renderer flash;
        [Tooltip("The six rounds seen in the cylinder; one goes with each shot.")]
        public GameObject[] rounds;
        [Tooltip("Every zombie that can be shot (RevolverStep fills it at compose time).")]
        public ZombieHitbox[] targets;
        public Collider pickup;
        public XRGrabInteractable grab;
        public HolsterSocket holster;
        public ParticleSystem blood, dust;

        /// <summary>Flat screen: how near and how straight the player must look at the gun to take it.</summary>
        public const float ReachMeters = 2.4f, ReachDegrees = 14;
        public const float AimSeconds = .12f, FlashSeconds = .05f, KickSeconds = .2f, KickDegrees = 10;
        /// <summary>
        /// From the eyes, turning with the view: low on the right, and raised to the eye while aiming. E-27: a little higher and
        /// further out than before (hip .17, -.24, .36; aim 0, -.075, .32), so the hand holding it is seen and its arm is not folded tight.
        /// </summary>
        public static readonly Vector3 HipPose = new Vector3(.16f, -.18f, .4f), AimPose = new Vector3(0, -.085f, .4f);
        /// <summary>Headset: the holster at the right hip, from the eyes, turning with the head's yaw only.</summary>
        private static readonly Vector3 HolsterOffset = new Vector3(.2f, -.62f, .05f);

        private readonly RevolverRules rules = new RevolverRules();
        private Vector3 restPosition;
        private Quaternion restRotation;
        // Armed by the run's first act as well as by the state (the Ward loads after the game is already Playing).
        private bool live, stow, headset;
        private float aim, kickUntil = -1, flashUntil = -1;
        private string takenNote, emptyNote, stowNote;

        public bool Held => rules.Held;
        /// <summary>Flat screen: 0 at the hip, 1 raised to the eye (E-27: the arms follow it).</summary>
        public float Aim => aim;
        public int RoundsLeft => rules.Rounds;
        public ShotResult LastShot { get; private set; }
        public HitPart LastPart { get; private set; }
        public ZombieHitbox LastTarget { get; private set; }

        private void Awake()
        {
            if (model)
            {
                restPosition = model.position;
                restRotation = model.rotation;
            }

            takenNote = "[PZ] revolver taken: " + RevolverRules.Capacity + " rounds";
            emptyNote = "[PZ] revolver: click, empty";
            stowNote = "[PZ] revolver: in the hip holster";
            if (flash) flash.enabled = false;
            if (holster) holster.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.ActStarted.Subscribe(OnAct);
            GameEvents.StateChanged.Subscribe(OnState);
            if (!grab) return;
            grab.selectEntered.AddListener(OnSelected);
            grab.selectExited.AddListener(OnReleased);
            grab.activated.AddListener(OnTrigger);
        }

        private void OnDisable()
        {
            GameEvents.ActStarted.Unsubscribe(OnAct);
            GameEvents.StateChanged.Unsubscribe(OnState);
            FlatInput.GunInHand = false;
            if (!grab) return;
            grab.selectEntered.RemoveListener(OnSelected);
            grab.selectExited.RemoveListener(OnReleased);
            grab.activated.RemoveListener(OnTrigger);
        }

        private void OnState(GameStateChanged e) => live = GameStateRules.IsSimulationLive(e.To);

        private void OnAct(ActStarted e)
        {
            // An act starts only while the game is Playing; every death sends the player back to bed (Awakening), a new life.
            live = true;
            if (e.Act == ActId.Awakening) ResetGun();
        }

        private void ResetGun()
        {
            rules.Reset();
            FlatInput.GunInHand = false;
            aim = 0;
            kickUntil = flashUntil = -1;
            stow = false;
            if (grab && grab.isSelected && grab.interactionManager) grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            if (holster) holster.gameObject.SetActive(false);
            if (model) model.SetPositionAndRotation(restPosition, restRotation);
            if (pickup) pickup.enabled = true;
            if (flash) flash.enabled = false;
            if (rounds != null)
                foreach (GameObject round in rounds)
                    if (round) round.SetActive(true);
            LastShot = ShotResult.None;
            LastPart = HitPart.None;
            LastTarget = null;
        }

        private void Update()
        {
            if (flashUntil >= 0 && Time.time >= flashUntil)
            {
                flashUntil = -1;
                if (flash) flash.enabled = false;
            }

            headset = XRSettings.isDeviceActive;
            if (headset)
            {
                if (stow && holster && holster.gameObject.activeSelf)
                {
                    stow = false;
                    holster.Stow();
                    if (holster.hasSelection) Debug.Log(stowNote);
                }

                return;
            }

#if PZ_DESKTOP_MOUSE || PZ_TOUCH
            if (!live || !model || !Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) return;
            FlatInput.Poll();
            if (!rules.Held)
            {
                if (FlatInput.GrabPressed && Reachable(player, true)) Take();
                return;
            }

            aim = Mathf.MoveTowards(aim, FlatInput.Aim ? 1 : 0, Time.deltaTime / AimSeconds);
            if (FlatInput.Fire) Pull(player.HeadPosition, player.HeadForward);
#endif
        }

        private void LateUpdate()
        {
            if (!rules.Held || !model || !Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) return;
            Vector3 forward = player.HeadForward;
            if (holster)
            {
                bool want = headset;
                if (holster.gameObject.activeSelf != want) holster.gameObject.SetActive(want);
                Vector3 level = new Vector3(forward.x, 0, forward.z);
                if (want && level.sqrMagnitude > 1e-6f)
                {
                    Quaternion yaw = Quaternion.LookRotation(level.normalized, Vector3.up);
                    // Barrel down and a little forward, the grip up: where a hand finds it.
                    holster.transform.SetPositionAndRotation(player.HeadPosition + yaw * HolsterOffset, yaw * Quaternion.Euler(75, 0, 0));
                }
            }

            if (headset || forward.sqrMagnitude < 1e-6f) return; // in a headset the hand or the holster holds it
            Quaternion look = Quaternion.LookRotation(forward, Vector3.up);
            float kick = kickUntil > Time.time ? (kickUntil - Time.time) / KickSeconds : 0;
            // E-27: drawn back from a wall the player faces, as the torch is, instead of reaching into it.
            Vector3 pose = Vector3.Lerp(HipPose, AimPose, aim) + Vector3.back * (.035f * kick) + FirstPersonArms.DrawBack(player.HeadPosition, forward);
            model.SetPositionAndRotation(player.HeadPosition + look * pose, look * Quaternion.Euler(-KickDegrees * kick, 0, 0));
        }

        /// <summary>Flat screen: near enough, nothing in between, and (when <paramref name="looking"/>) looked at.</summary>
        private bool Reachable(IPlayerQuery player, bool looking)
        {
            Vector3 eye = player.HeadPosition, at = model.position + Vector3.up * .05f, to = at - eye;
            float meters = to.magnitude;
            if (meters > ReachMeters) return false;
            if (looking && meters > .05f && Vector3.Angle(player.HeadForward, to) > ReachDegrees) return false;
            return !Physics.Linecast(eye, at, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                   (pickup && hit.collider == pickup);
        }

        private void Take()
        {
            rules.Take();
            FlatInput.GunInHand = !XRSettings.isDeviceActive;
            // On a flat screen the gun is carried, not held by a collider: nothing it passes through may stop a ray or a grab.
            if (pickup && !XRSettings.isDeviceActive) pickup.enabled = false;
            if (model) Cues.Play(CueId.RevolverHandle, model.position, NoiseOrigin.Player);
            Debug.Log(takenNote);
        }

        /// <summary>The test bot's way to take it (ReleaseProbe): standing within reach is enough, as with its other grips.</summary>
        public bool TakeWithinReach()
        {
            if (rules.Held || !live || !model || !Services<IPlayerQuery>.TryGet(out IPlayerQuery player) || !Reachable(player, false)) return false;
            Take();
            return true;
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            if (args.interactorObject is HolsterSocket || rules.Held) return;
            if (live) Take();
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            // Let go of anywhere but the holster, it goes to the holster (next frame, outside XRI's own callbacks).
            if (rules.Held && !(args.interactorObject is HolsterSocket) && !args.isCanceled) stow = true;
        }

        private void OnTrigger(ActivateEventArgs args)
        {
            if (muzzle) Pull(muzzle.position, muzzle.forward);
        }

        /// <summary>
        /// Pulls the trigger with the bullet leaving <paramref name="origin"/> along <paramref name="direction"/>. Only while the run
        /// is live (not paused, not in a zombie's grip). Returns what happened; <see cref="LastPart"/> and <see cref="LastTarget"/>
        /// say what the bullet hit.
        /// </summary>
        public ShotResult Pull(Vector3 origin, Vector3 direction)
        {
            if (!live || direction.sqrMagnitude < 1e-6f) return ShotResult.None;
            ShotResult result = rules.Pull(Time.time);
            if (result == ShotResult.None) return result;
            LastShot = result;
            LastPart = HitPart.None;
            LastTarget = null;
            Vector3 from = muzzle ? muzzle.position : origin;
            if (result == ShotResult.Empty)
            {
                Cues.Play(CueId.RevolverDryFire, from, NoiseOrigin.Player);
                Debug.Log(emptyNote);
                return result;
            }

            // The sound first: it is what draws every zombie within 20 m to where the shot was fired.
            Cues.Play(CueId.RevolverShot, from, NoiseOrigin.Player);
            kickUntil = Time.time + KickSeconds;
            if (flash)
            {
                flash.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                flash.enabled = true;
                flashUntil = Time.time + FlashSeconds;
            }

            int spent = RevolverRules.Capacity - rules.Rounds - 1;
            if (rounds != null && spent >= 0 && spent < rounds.Length && rounds[spent]) rounds[spent].SetActive(false);

            direction.Normalize();
            float wall = RevolverRules.RangeMeters;
            bool walled = Physics.Raycast(origin, direction, out RaycastHit hit, RevolverRules.RangeMeters, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (walled) wall = hit.distance;

            ZombieHitbox target = null;
            HitPart part = HitPart.None;
            float nearest = float.MaxValue;
            if (targets != null)
                foreach (ZombieHitbox box in targets)
                {
                    if (!box) continue;
                    HitPart p = box.Test(origin, direction, wall, out float d);
                    if (p == HitPart.None || d >= nearest) continue;
                    nearest = d;
                    part = p;
                    target = box;
                }

            if (target)
            {
                bool died = target.zombie.Shot(part == HitPart.Head);
                LastPart = part;
                LastTarget = target;
                Emit(blood, origin + direction * nearest, -direction, part == HitPart.Head ? 40 : 22);
                Debug.Log("[PZ] revolver: " + (part == HitPart.Head ? "headshot" : "body shot") + " on zombie " + target.zombie.ZombieId +
                          " at " + nearest.ToString("0.0") + " m" + (died ? ", dead" : ", staggered") + "; " + rules.Rounds + " left");
            }
            else
            {
                if (walled) Emit(dust, hit.point, hit.normal, 16);
                Debug.Log("[PZ] revolver: missed" + (walled ? ", into " + hit.collider.name + " at " + hit.distance.ToString("0.0") + " m" : "") + "; " + rules.Rounds + " left");
            }

            return result;
        }

        private static void Emit(ParticleSystem system, Vector3 at, Vector3 facing, int count)
        {
            if (!system) return;
            system.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing.sqrMagnitude > 1e-6f ? facing : Vector3.up));
            system.Emit(count);
        }
    }
}
