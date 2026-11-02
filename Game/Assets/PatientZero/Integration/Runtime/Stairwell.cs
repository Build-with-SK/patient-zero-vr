// Claude-owned integration (horror pass, 2026-11-02; E-21). Owner request: "there should be step I don't want to teleport to
// different floor".
using System;
using PatientZero.Core;
using PatientZero.Player;
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// A stairwell the player walks down, from a floor's stair room to the stair room of the floor below. The ward's floors
    /// stand side by side at the same height, so the stairwell is built twice: the top half (stairs down from the upper floor
    /// to a fire-door lobby) and the bottom half (the same lobby, then stairs down to the lower floor), the second exactly
    /// <see cref="Rise"/> metres above the first. Each lobby has two automatic doors, and only one of them works in each copy.
    /// While the player crosses a lobby with both doors shut, the two copies look the same to the metre (same pieces, same
    /// texture, no room light reaches either), and the player is moved from one copy to the other, which they cannot see.
    /// The doors then open on the rest of the way down (or up). The top door opens once the act has moved to the lower floor.
    /// A headset keeps the fade between floors (PlayerRig): it moves by teleport anchors only and never enters a stairwell.
    /// </summary>
    public sealed class Stairwell : MonoBehaviour
    {
        public enum Opens { Always, OnceDown, Never }

        [Serializable]
        public struct Door
        {
            public Transform Left, Right;
            [Tooltip("Middle of the doorway at floor level.")]
            public Vector3 Centre;
            [Tooltip("The way the panels slide apart (the right panel along it, the left against it).")]
            public Vector3 Slide;
            public Opens Opens;
            [HideInInspector] public Vector3 LeftClosed, RightClosed;
        }

        [Serializable]
        public struct Lobby
        {
            [Tooltip("The floor of the lobby where the player can stand: its lowest corner, and its far corner.")]
            public Vector3 Min, Max;
            [Tooltip("The door that leads somewhere from this copy, and the one that never opens here.")]
            public int Live, Dead;
        }

        public Door[] doors = new Door[0];
        public Lobby upper, lower;
        [Tooltip("Floor number below the stairwell (WardLayout.FloorOf); the top door opens once the act is on it.")]
        public int lowerFloor;
        [Tooltip("How far below the lower copy the upper copy stands (the drop of each half, twice).")]
        public float Rise = 3.6f;
        public float openRadius = .95f, slideSeconds = .3f, panelTravel = .6f;

        private float[] open = new float[0];
        private int floor;
        private bool crossed, told;

        private void Awake()
        {
            open = new float[doors.Length];
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i].Left) doors[i].LeftClosed = doors[i].Left.localPosition;
                if (doors[i].Right) doors[i].RightClosed = doors[i].Right.localPosition;
            }
        }

        private void OnEnable() => GameEvents.ActStarted.Subscribe(OnAct);

        private void OnDisable()
        {
            GameEvents.ActStarted.Unsubscribe(OnAct);
            if (told && Services<IPlayerQuery>.TryGet(out IPlayerQuery player) && player is PlayerRig rig) rig.walkStairs = false;
            told = false;
        }

        private void OnAct(ActStarted e)
        {
            floor = WardLayout.FloorOf(e.Act);
            crossed = false;
            // A restore or a new act places the player anew: every door starts shut.
            for (int i = 0; i < open.Length; i++) open[i] = 0;
            Slide();
        }

        private void Update()
        {
            if (!Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) return;
            var rig = player as PlayerRig;
            if (!told && rig)
            {
                rig.walkStairs = true; // flat screens walk down this stairwell instead of fading between floors
                told = true;
            }

            Vector3 feet = player.FloorPosition;
            float step = Time.deltaTime / Mathf.Max(.01f, slideSeconds);
            for (int i = 0; i < doors.Length; i++)
            {
                Opens rule = doors[i].Opens;
                bool allowed = rule == Opens.Always || (rule == Opens.OnceDown && floor != 0 && floor <= lowerFloor);
                bool near = Mathf.Abs(feet.y - doors[i].Centre.y) < 1.2f && WardLayout.HorizontalDistance(feet, doors[i].Centre) <= openRadius;
                open[i] = Mathf.MoveTowards(open[i], allowed && near ? 1 : 0, step);
            }

            Slide();
            if (rig && rig.origin) Cross(rig, feet);
        }

        private void Slide()
        {
            for (int i = 0; i < doors.Length; i++)
            {
                float k = Mathf.SmoothStep(0, 1, i < open.Length ? open[i] : 0) * panelTravel;
                Vector3 slide = doors[i].Slide;
                if (doors[i].Left) doors[i].Left.localPosition = doors[i].LeftClosed - slide * k;
                if (doors[i].Right) doors[i].Right.localPosition = doors[i].RightClosed + slide * k;
            }
        }

        /// <summary>Moves the player to the other copy of the lobby once both of its doors are shut behind them.</summary>
        private void Cross(PlayerRig rig, Vector3 feet)
        {
            bool inUpper = Inside(upper, feet), inLower = !inUpper && Inside(lower, feet);
            if (!inUpper && !inLower)
            {
                crossed = false;
                return;
            }

            if (crossed) return;
            Lobby here = inUpper ? upper : lower;
            if (open[here.Live] > 0 || open[here.Dead] > 0) return;
            // Only on the way through: closer to the door that leads on in the other copy than to the one they came in by.
            if (WardLayout.HorizontalDistance(feet, doors[here.Dead].Centre) >= WardLayout.HorizontalDistance(feet, doors[here.Live].Centre)) return;
            rig.origin.transform.position += (inUpper ? 1 : -1) * Rise * Vector3.up;
            Physics.SyncTransforms();
            crossed = true;
        }

        private static bool Inside(in Lobby lobby, Vector3 feet) =>
            feet.x > lobby.Min.x && feet.x < lobby.Max.x && feet.z > lobby.Min.z && feet.z < lobby.Max.z && Mathf.Abs(feet.y - lobby.Min.y) < 1f;
    }
}
