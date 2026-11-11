// Claude integration E-22 (two-minute version, owner-approved plan 2026-11-10).
using PatientZero.Core;
using PatientZero.Flow;
using PatientZero.Player.Logic;
using UnityEngine;
using UnityEngine.XR;
namespace PatientZero.Player
{
    /// <summary>
    /// E-22: what the player's body does that the blind zombies care about. In a headset, walking makes footsteps they hear, as
    /// the keys do on a flat screen (DesktopLocomotion); a head turn or a sway makes none. In every build it reads the struggle
    /// out of a zombie's grab: Space or the left mouse button on Windows, a tap on a touch screen, a hard shove of either hand in
    /// a headset. Added to the rig by PlayerBuildStep. E-23 (owner, 2026-11-11): nothing on screen explains the struggle, so on a
    /// flat screen any key or mouse button counts (FlatInput) and each press jolts the view, which shows the press is a fight.
    /// </summary>
    public sealed class PlayerBody : MonoBehaviour
    {
        public PlayerRig rig; public GameFlow flow;
        private readonly FootstepCounter steps = new FootstepCounter();
        private readonly ShoveDetector leftShove = new ShoveDetector(), rightShove = new ShoveDetector();
        private Vector3 lastFloor, lastLeft, lastRight;
        /// <summary>E-23: how long and how far (metres) one struggle press jolts a flat-screen view.</summary>
        public const float JoltSeconds = .14f, JoltMeters = .035f;
        private float joltUntil = -1;
        private Transform jolted; private Vector3 joltBase;
        private bool hasFloor, hasLeft, hasRight;

        private void Update()
        {
            if (!rig || !flow) return;
            bool headset = XRSettings.isDeviceActive;
            if (flow.Core.Struggling)
            {
                hasFloor = false;
                if (headset) { if (Shove(HandSide.Left, leftShove, ref lastLeft, ref hasLeft) | Shove(HandSide.Right, rightShove, ref lastRight, ref hasRight)) flow.Struggle(); }
                else { FlatInput.Poll(); if (FlatInput.Struggle) { flow.Struggle(); joltUntil = Time.unscaledTime + JoltSeconds; } }
                return;
            }

            hasLeft = hasRight = false; leftShove.Reset(); rightShove.Reset();
            if (!headset || flow.Core.State != GameState.Playing || flow.Core.IsTransitioning) { hasFloor = false; steps.Reset(); return; }
            Vector3 floor = rig.FloorPosition;
            if (hasFloor)
            {
                StepKind step = steps.Sample(floor - lastFloor, Time.deltaTime);
                if (step == StepKind.Footstep) Cues.Play(CueId.PlayerFootstep, floor, NoiseOrigin.Player);
                else if (step == StepKind.Landing) Cues.Play(CueId.TeleportLand, floor, NoiseOrigin.Player);
            }

            lastFloor = floor; hasFloor = true;
        }

        private void LateUpdate()
        {
            // The world is held (time scale 0) during a struggle, so the jolt runs on the unscaled clock. Flat screens only: the
            // head is the player's own in a headset.
            bool on = Time.unscaledTime < joltUntil && !XRSettings.isDeviceActive && rig && rig.origin && rig.origin.Camera;
            if (!on)
            {
                if (jolted) jolted.localPosition = joltBase;
                jolted = null;
                return;
            }

            if (!jolted) { jolted = rig.origin.Camera.transform; joltBase = jolted.localPosition; }
            float k = (joltUntil - Time.unscaledTime) / JoltSeconds;
            jolted.localPosition = joltBase + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * (JoltMeters * k);
        }

        /// <summary>The world is held during a struggle, so hand speed is measured on the unscaled clock.</summary>
        private bool Shove(HandSide side, ShoveDetector detector, ref Vector3 last, ref bool has)
        {
            if (!rig.TryGetHandPosition(side, out Vector3 hand)) { has = false; return false; }
            float seconds = Time.unscaledDeltaTime;
            bool shoved = has && seconds > 0 && detector.Sample(Vector3.Distance(hand, last) / seconds);
            last = hand; has = true;
            return shoved;
        }
    }
}
