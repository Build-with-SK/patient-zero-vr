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
        /// <summary>v1.3: how close to a stair landing anchor the player must be for the stairs objective.</summary>
        public const float StairTriggerRadius = 0.75f;
        /// <summary>v1.3: rooms of different floors stay at least this far apart, so no wall or doorway joins them.</summary>
        public const float FloorSeparation = 5.0f;

        private static readonly RoomRect[] RoomArray =
        {
            new RoomRect("IsolationB7", -2f, 2f, -2f, 2f),
            new RoomRect("Corridor", -2f, 13f, 2f, 4.5f),
            new RoomRect("SupplyRoom", 4f, 8f, -1f, 2f),
            new RoomRect("SideRoom", 9.5f, 11.5f, 4.5f, 7f),
            new RoomRect("LinenRoom", 9.5f, 11.5f, -0.5f, 2f),
            new RoomRect("Vestibule", 13f, 17f, 1.5f, 5f),
            // v1.3: Floor 3 stairwell, behind the emergency door.
            new RoomRect("Stair3", 17f, 19.5f, 2f, 4.5f),
            // v1.3: Floor 2, Laboratories.
            new RoomRect("Stair2", 30f, 32.5f, 2f, 4.5f),
            new RoomRect("LabCorridor", 32.5f, 46f, 2f, 4.5f),
            new RoomRect("LabA", 36f, 40f, 4.5f, 7f),
            new RoomRect("LabB", 33f, 37f, -1.5f, 2f),
            new RoomRect("Stair2Down", 46f, 48.5f, 2f, 4.5f),
            // v1.3: Floor 1, Main Lobby.
            new RoomRect("Stair1", 60f, 62.5f, 2f, 4.5f),
            new RoomRect("Lobby", 62.5f, 74f, -2f, 7f),
            new RoomRect("Security", 65f, 69f, 7f, 10f),
            new RoomRect("Entrance", 74f, 77f, 2f, 4.5f),
        };

        /// <summary>v1.3: which floor each room is on (3 = the ward, 1 = the ground floor), parallel to the room list.</summary>
        private static readonly int[] RoomFloorArray = { 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 1, 1, 1, 1 };

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
        // v1.3: the landing behind the emergency door; reaching it after the door opens takes the stairs to Floor 2.
        public static readonly LayoutPoint A4_Stairs = Anchor("A4.Stairs", ActId.Exit, 18.6f, 3.25f, 90f);

        // Act 5 - Laboratory (Floor 2, v1.3)
        public static readonly LayoutPoint A5_Start = Anchor("A5.Start", ActId.Laboratory, 31.6f, 3.25f, 90f);
        public static readonly LayoutPoint A5_Hall1 = Anchor("A5.Hall1", ActId.Laboratory, 34.2f, 3.25f, 90f);
        public static readonly LayoutPoint A5_Hall2 = Anchor("A5.Hall2", ActId.Laboratory, 36.9f, 3.4f, 45f);
        public static readonly LayoutPoint A5_Bench = Anchor("A5.Bench", ActId.Laboratory, 38.4f, 5.6f, 0f);
        public static readonly LayoutPoint A5_Hall3 = Anchor("A5.Hall3", ActId.Laboratory, 39f, 3.2f, 90f);
        public static readonly LayoutPoint A5_FuseBox = Anchor("A5.FuseBox", ActId.Laboratory, 41.8f, 2.7f, 180f);
        public static readonly LayoutPoint A5_Override = Anchor("A5.Override", ActId.Laboratory, 44.4f, 2.7f, 180f);
        public static readonly LayoutPoint A5_Stairs = Anchor("A5.Stairs", ActId.Laboratory, 47.2f, 3.25f, 90f);
        public static readonly LayoutPoint A5_Fuse = Item("A5.Fuse", ActId.Laboratory, 38.8f, 1.0f, 6.1f, true);
        public static readonly LayoutPoint A5_FuseSocket = Item("A5.FuseSocket", ActId.Laboratory, 41.8f, 1.2f, 2.1f, true);
        public static readonly LayoutPoint A5_OverrideLever = Item("A5.OverrideLever", ActId.Laboratory, 44.4f, 1.15f, 2.1f, true);
        public static readonly LayoutPoint A5_StairDoor = Mark("A5.StairDoor", ActId.Laboratory, LayoutKind.Landmark, 46f, 0f, 3.25f);
        public static readonly LayoutPoint Z5_Dormant = Mark("Z5.Dormant", ActId.Laboratory, LayoutKind.ZombieMarker, 34.6f, 0f, -0.6f);

        // Act 6 - Lobby (Floor 1, v1.3)
        public static readonly LayoutPoint A6_Start = Anchor("A6.Start", ActId.Lobby, 61.6f, 3.25f, 90f);
        public static readonly LayoutPoint A6_Hall1 = Anchor("A6.Hall1", ActId.Lobby, 64.2f, 4.4f, 45f);
        public static readonly LayoutPoint A6_SecurityDoor = Anchor("A6.SecurityDoor", ActId.Lobby, 66.4f, 6.2f, 0f);
        public static readonly LayoutPoint A6_Generator = Anchor("A6.Generator", ActId.Lobby, 66.8f, 8.9f, 0f);
        public static readonly LayoutPoint A6_Hall2 = Anchor("A6.Hall2", ActId.Lobby, 67.6f, 6.3f, 135f);
        public static readonly LayoutPoint A6_Hall3 = Anchor("A6.Hall3", ActId.Lobby, 69.2f, 4.2f, 90f);
        public static readonly LayoutPoint A6_Hall4 = Anchor("A6.Hall4", ActId.Lobby, 71.4f, 3f, 90f);
        public static readonly LayoutPoint A6_Lockdown = Anchor("A6.Lockdown", ActId.Lobby, 73.2f, 1.9f, 90f);
        public static readonly LayoutPoint A6_Exit = Anchor("A6.Exit", ActId.Lobby, 75.6f, 3.25f, 90f);
        public static readonly LayoutPoint A6_ValveWheel = Item("A6.ValveWheel", ActId.Lobby, 66.8f, 1.1f, 9.55f, true);
        public static readonly LayoutPoint A6_LockdownLever = Item("A6.LockdownLever", ActId.Lobby, 73.8f, 1.15f, 1.7f, true);
        public static readonly LayoutPoint A6_MainDoors = Mark("A6.MainDoors", ActId.Lobby, LayoutKind.Landmark, 74f, 0f, 3.25f);
        public static readonly LayoutPoint A6_Desk = Mark("A6.Desk", ActId.Lobby, LayoutKind.Landmark, 64.6f, 0f, 0.2f);
        public static readonly LayoutPoint Z6_PatrolNorth = Mark("Z6.PatrolNorth", ActId.Lobby, LayoutKind.ZombieMarker, 70.2f, 0f, 6.4f);
        public static readonly LayoutPoint Z6_PatrolSouth = Mark("Z6.PatrolSouth", ActId.Lobby, LayoutKind.ZombieMarker, 70.2f, 0f, -1.4f);
        public static readonly LayoutPoint Z7_Dormant = Mark("Z7.Dormant", ActId.Lobby, LayoutKind.ZombieMarker, 64f, 0f, -1.2f);

        private static readonly LayoutPoint[] PointArray =
        {
            A1_Start, A1_Door, A1_Tray, A1_PlankLow, A1_PlankMid, A1_PlankHigh, A1_DoorWindow, Z1_WalkFrom, Z1_WalkTo,
            A2_Start, A2_Valve, A2_Machinery, A2_ValveWheel, A2_CableHook, A2_Socket, A2_Monitor, Z2_Dormant, Z2_Doorway,
            A3_Start, A3_Mid1, A3_Mid2, A3_End, A3_Beacon, A3_Cart, A3_Drip, Z3_PatrolNorth, Z3_PatrolSouth,
            A4_Start, A4_Release, A4_ReleaseLever, A4_ExitDoor, Z4_Gurney, A4_Stairs,
            A5_Start, A5_Hall1, A5_Hall2, A5_Bench, A5_Hall3, A5_FuseBox, A5_Override, A5_Stairs,
            A5_Fuse, A5_FuseSocket, A5_OverrideLever, A5_StairDoor, Z5_Dormant,
            A6_Start, A6_Hall1, A6_SecurityDoor, A6_Generator, A6_Hall2, A6_Hall3, A6_Hall4, A6_Lockdown, A6_Exit,
            A6_ValveWheel, A6_LockdownLever, A6_MainDoors, A6_Desk, Z6_PatrolNorth, Z6_PatrolSouth, Z7_Dormant,
        };

        /// <summary>
        /// The teleport anchors a player passes through, in order, on a normal run. Within a floor each step is within
        /// teleport range; between floors the stairs move the player (v1.3).
        /// </summary>
        private static readonly LayoutPoint[] CriticalPathArray =
        {
            A1_Start, A1_Door, A2_Start, A2_Valve, A2_Machinery, A3_Start, A3_Mid1, A3_Mid2, A3_End, A4_Start, A4_Release, A4_Stairs,
            A5_Start, A5_Hall1, A5_Hall2, A5_Bench, A5_Hall3, A5_FuseBox, A5_Override, A5_Stairs,
            A6_Start, A6_Hall1, A6_SecurityDoor, A6_Generator, A6_Hall2, A6_Hall3, A6_Hall4, A6_Lockdown, A6_Exit,
        };

        public static readonly IReadOnlyList<RoomRect> Rooms = Array.AsReadOnly(RoomArray);
        public static readonly IReadOnlyList<int> RoomFloors = Array.AsReadOnly(RoomFloorArray);
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
                case ActId.Laboratory: return A5_Start;
                case ActId.Lobby: return A6_Start;
                default: throw new ArgumentOutOfRangeException(nameof(act), act, "No start anchor");
            }
        }

        /// <summary>v1.3: the floor an act is played on (3 = the ward, 2 = laboratories, 1 = lobby); 0 for None.</summary>
        public static int FloorOf(ActId act)
        {
            switch (act)
            {
                case ActId.Awakening: case ActId.Machinery: case ActId.Darkness: case ActId.Exit: return 3;
                case ActId.Laboratory: return 2;
                case ActId.Lobby: return 1;
                default: return 0;
            }
        }

        /// <summary>v1.3: true for the first act on each floor, where the player arrives by the stairs (or wakes up).</summary>
        public static bool StartsFloor(ActId act) => act == ActId.Awakening || act == ActId.Laboratory || act == ActId.Lobby;

        /// <summary>v1.3: the landing whose arrival ends the act and takes the stairs down; false on the last floor.</summary>
        public static bool TryStairLanding(ActId act, out LayoutPoint landing)
        {
            landing = act == ActId.Exit ? A4_Stairs : act == ActId.Laboratory ? A5_Stairs : default(LayoutPoint);
            return act == ActId.Exit || act == ActId.Laboratory;
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
