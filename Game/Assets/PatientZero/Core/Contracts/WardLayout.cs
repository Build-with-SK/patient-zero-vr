using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatientZero.Core
{
    public enum LayoutKind
    {
        TeleportAnchor = 0,
        Interactable = 1,
        ZombieMarker = 2,
        Landmark = 3,
    }

    /// <summary>A named point in the ward. Metres, Y up, origin = floor centre of Isolation Room B-7.</summary>
    public readonly struct LayoutPoint
    {
        public readonly string Id;
        public readonly ActId Act;
        public readonly LayoutKind Kind;
        public readonly Vector3 Position;
        /// <summary>Facing, degrees clockwise from +Z. For anchors: where the player looks on arrival.</summary>
        public readonly float YawDegrees;
        /// <summary>Required interactables must be reachable; optional ones are flavour.</summary>
        public readonly bool Required;

        public LayoutPoint(string id, ActId act, LayoutKind kind, Vector3 position, float yawDegrees, bool required)
        {
            Id = id;
            Act = act;
            Kind = kind;
            Position = position;
            YawDegrees = yawDegrees;
            Required = required;
        }
    }

    /// <summary>Walkable floor area, axis-aligned, in metres.</summary>
    public readonly struct RoomRect
    {
        public readonly string Id;
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinZ;
        public readonly float MaxZ;

        public RoomRect(string id, float minX, float maxX, float minZ, float maxZ)
        {
            Id = id;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        public bool Contains(Vector3 p, float clearance)
        {
            return p.x >= MinX + clearance && p.x <= MaxX - clearance && p.z >= MinZ + clearance && p.z <= MaxZ - clearance;
        }
    }

    /// <summary>
    /// The single source of truth for where things are. Both teams' scene builders place
    /// objects at these ids; nobody hard-codes a coordinate elsewhere. Changes go through the
    /// Technical Director because WardLayoutTests prove reachability and fairness from them.
    /// Map: docs/GAME_DESIGN.md section 3.
    /// </summary>
    public static class WardLayout
    {
        // Rules the layout is tested against.
        public const float TeleportMaxDistance = 3.0f;
        public const float MaxReachHorizontal = 0.7f;
        public const float MinInteractHeight = 0.75f;
        public const float MaxInteractHeight = 1.5f;
        public const float WallClearance = 0.4f;
        public const float FinalPatientReach = 1.3f;
        public const float FairnessMargin = 0.3f;
        public const float PatrolClearance = 0.8f;
        public const float BeaconTriggerRadius = 0.75f;

        private static readonly RoomRect[] RoomArray =
        {
            new RoomRect("IsolationB7", -2f, 2f, -2f, 2f),
            new RoomRect("Corridor", -2f, 13f, 2f, 4.5f),
            new RoomRect("SupplyRoom", 4f, 8f, -1f, 2f),
            new RoomRect("SideRoom", 9.5f, 11.5f, 4.5f, 7f),
            new RoomRect("LinenRoom", 9.5f, 11.5f, -0.5f, 2f),
            new RoomRect("Vestibule", 13f, 17f, 1.5f, 5f),
        };

        // Act 1 - Awakening (Isolation Room B-7)
        public static readonly LayoutPoint A1_Start = Anchor("A1.Start", ActId.Awakening, 0f, -0.5f, 0f);
        public static readonly LayoutPoint A1_Door = Anchor("A1.Door", ActId.Awakening, 0f, 1.4f, 0f);
        public static readonly LayoutPoint A1_Tray = Item("A1.Tray", ActId.Awakening, 0.45f, 0.8f, -0.1f, false);
        public static readonly LayoutPoint A1_PlankLow = Item("A1.PlankLow", ActId.Awakening, 0f, 0.95f, 1.9f, true);
        public static readonly LayoutPoint A1_PlankMid = Item("A1.PlankMid", ActId.Awakening, 0f, 1.2f, 1.9f, true);
        public static readonly LayoutPoint A1_PlankHigh = Item("A1.PlankHigh", ActId.Awakening, 0f, 1.45f, 1.9f, true);
        public static readonly LayoutPoint A1_DoorWindow = Mark("A1.DoorWindow", ActId.Awakening, LayoutKind.Landmark, 0f, 1.65f, 2f);
        public static readonly LayoutPoint Z1_WalkFrom = Mark("Z1.WalkFrom", ActId.Awakening, LayoutKind.ZombieMarker, -1.5f, 0f, 3.6f);
        public static readonly LayoutPoint Z1_WalkTo = Mark("Z1.WalkTo", ActId.Awakening, LayoutKind.ZombieMarker, 6f, 0f, 3.6f);

        // Act 2 - Machinery (corridor north wall)
        public static readonly LayoutPoint A2_Start = Anchor("A2.Start", ActId.Machinery, 0f, 3.25f, 90f);
        public static readonly LayoutPoint A2_Valve = Anchor("A2.Valve", ActId.Machinery, 2f, 3.9f, 0f);
        public static readonly LayoutPoint A2_Machinery = Anchor("A2.Machinery", ActId.Machinery, 3.4f, 3.9f, 0f);
        public static readonly LayoutPoint A2_ValveWheel = Item("A2.ValveWheel", ActId.Machinery, 2f, 1.1f, 4.4f, true);
        public static readonly LayoutPoint A2_CableHook = Item("A2.CableHook", ActId.Machinery, 3f, 1.05f, 4.4f, true);
        public static readonly LayoutPoint A2_Socket = Item("A2.Socket", ActId.Machinery, 3.8f, 1.2f, 4.4f, true);
        public static readonly LayoutPoint A2_Monitor = Mark("A2.Monitor", ActId.Machinery, LayoutKind.Landmark, 1f, 1.6f, 4.45f);
        public static readonly LayoutPoint Z2_Dormant = Mark("Z2.Dormant", ActId.Machinery, LayoutKind.ZombieMarker, 6.5f, 0f, 0.5f);
        public static readonly LayoutPoint Z2_Doorway = Mark("Z2.Doorway", ActId.Machinery, LayoutKind.ZombieMarker, 6f, 0f, 2f);

        // Act 3 - Darkness (corridor east)
        public static readonly LayoutPoint A3_Start = Anchor("A3.Start", ActId.Darkness, 5.5f, 3.25f, 90f);
        public static readonly LayoutPoint A3_Mid1 = Anchor("A3.Mid1", ActId.Darkness, 7.5f, 2.8f, 90f);
        public static readonly LayoutPoint A3_Mid2 = Anchor("A3.Mid2", ActId.Darkness, 9.6f, 3.7f, 90f);
        public static readonly LayoutPoint A3_End = Anchor("A3.End", ActId.Darkness, 12.4f, 3.25f, 90f);
        public static readonly LayoutPoint A3_Beacon = Mark("A3.Beacon", ActId.Darkness, LayoutKind.Landmark, 13f, 2.1f, 3.25f);
        public static readonly LayoutPoint A3_Cart = Mark("A3.Cart", ActId.Darkness, LayoutKind.Landmark, 8.5f, 0.9f, 2.3f);
        public static readonly LayoutPoint A3_Drip = Mark("A3.Drip", ActId.Darkness, LayoutKind.Landmark, 7f, 2.4f, 4.3f);
        public static readonly LayoutPoint Z3_PatrolNorth = Mark("Z3.PatrolNorth", ActId.Darkness, LayoutKind.ZombieMarker, 10.5f, 0f, 6.5f);
        public static readonly LayoutPoint Z3_PatrolSouth = Mark("Z3.PatrolSouth", ActId.Darkness, LayoutKind.ZombieMarker, 10.5f, 0f, 0.5f);

        // Act 4 - Exit (vestibule)
        public static readonly LayoutPoint A4_Start = Anchor("A4.Start", ActId.Exit, 13.6f, 3f, 90f);
        public static readonly LayoutPoint A4_Release = Anchor("A4.Release", ActId.Exit, 16.3f, 2.5f, 90f);
        public static readonly LayoutPoint A4_ReleaseLever = Item("A4.ReleaseLever", ActId.Exit, 16.9f, 1.15f, 2.4f, true);
        public static readonly LayoutPoint A4_ExitDoor = Mark("A4.ExitDoor", ActId.Exit, LayoutKind.Landmark, 17f, 0f, 3.25f);
        public static readonly LayoutPoint Z4_Gurney = Mark("Z4.Gurney", ActId.Exit, LayoutKind.ZombieMarker, 15f, 0f, 4.4f);

        private static readonly LayoutPoint[] PointArray =
        {
            A1_Start, A1_Door, A1_Tray, A1_PlankLow, A1_PlankMid, A1_PlankHigh, A1_DoorWindow, Z1_WalkFrom, Z1_WalkTo,
            A2_Start, A2_Valve, A2_Machinery, A2_ValveWheel, A2_CableHook, A2_Socket, A2_Monitor, Z2_Dormant, Z2_Doorway,
            A3_Start, A3_Mid1, A3_Mid2, A3_End, A3_Beacon, A3_Cart, A3_Drip, Z3_PatrolNorth, Z3_PatrolSouth,
            A4_Start, A4_Release, A4_ReleaseLever, A4_ExitDoor, Z4_Gurney,
        };

        /// <summary>The teleport anchors a player passes through, in order, on a normal run.</summary>
        private static readonly LayoutPoint[] CriticalPathArray =
        {
            A1_Start, A1_Door, A2_Start, A2_Valve, A2_Machinery, A3_Start, A3_Mid1, A3_Mid2, A3_End, A4_Start, A4_Release,
        };

        public static readonly IReadOnlyList<RoomRect> Rooms = Array.AsReadOnly(RoomArray);
        public static readonly IReadOnlyList<LayoutPoint> Points = Array.AsReadOnly(PointArray);
        public static readonly IReadOnlyList<LayoutPoint> CriticalPath = Array.AsReadOnly(CriticalPathArray);

        /// <summary>Where the player is placed when an act starts or restarts.</summary>
        public static LayoutPoint ActStart(ActId act)
        {
            switch (act)
            {
                case ActId.Awakening: return A1_Start;
                case ActId.Machinery: return A2_Start;
                case ActId.Darkness: return A3_Start;
                case ActId.Exit: return A4_Start;
                default: throw new ArgumentOutOfRangeException(nameof(act), act, "No start anchor");
            }
        }

        public static bool TryGet(string id, out LayoutPoint point)
        {
            for (int i = 0; i < PointArray.Length; i++)
            {
                if (PointArray[i].Id == id)
                {
                    point = PointArray[i];
                    return true;
                }
            }
            point = default(LayoutPoint);
            return false;
        }

        /// <summary>Horizontal (XZ) distance from p to segment a-b.</summary>
        public static float HorizontalDistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            float abx = b.x - a.x, abz = b.z - a.z;
            float lengthSq = abx * abx + abz * abz;
            float t = lengthSq < 1e-8f ? 0f : Mathf.Clamp01(((p.x - a.x) * abx + (p.z - a.z) * abz) / lengthSq);
            float dx = p.x - (a.x + abx * t), dz = p.z - (a.z + abz * t);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static LayoutPoint Anchor(string id, ActId act, float x, float z, float yaw)
        {
            return new LayoutPoint(id, act, LayoutKind.TeleportAnchor, new Vector3(x, 0f, z), yaw, true);
        }

        private static LayoutPoint Item(string id, ActId act, float x, float y, float z, bool required)
        {
            return new LayoutPoint(id, act, LayoutKind.Interactable, new Vector3(x, y, z), 0f, required);
        }

        private static LayoutPoint Mark(string id, ActId act, LayoutKind kind, float x, float y, float z)
        {
            return new LayoutPoint(id, act, kind, new Vector3(x, y, z), 0f, false);
        }
    }
}
