using System;
using System.Collections.Generic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Environment.Logic
{
    public readonly struct WallSpan
    {
        public readonly Vector3 A, B;
        public WallSpan(Vector3 a, Vector3 b) { A = a; B = b; }
        public float Length => Vector3.Distance(A, B);
        // Butt joints overlap at corners; longitudinal padding would intrude into door cuts.
        public Vector3 ColliderSize => A.x != B.x ? new Vector3(Length, 2.8f, .1f) : new Vector3(.1f, 2.8f, Length);
    }
    public readonly struct DoorOpening
    {
        public readonly string RoomA, RoomB;
        public readonly Vector3 Centre;
        public readonly bool AlongX;
        public readonly float Width;
        public DoorOpening(string a, string b, Vector3 centre, bool alongX, float width)
        { RoomA = a; RoomB = b; Centre = centre; AlongX = alongX; Width = width; }
    }
    /// <summary>Derived union boundary geometry; the Ward build step creates meshes/colliders from these spans.</summary>
    public sealed class RoomGeometry
    {
        public readonly RoomRect[] Floors;
        public readonly WallSpan[] Walls;
        public readonly DoorOpening[] Doors;
        private const float Epsilon = .001f;
        public RoomGeometry()
        {
            Floors = new RoomRect[WardLayout.Rooms.Count];
            for (int i = 0; i < Floors.Length; i++) Floors[i] = WardLayout.Rooms[i];
            var doors = new List<DoorOpening>();
            for (int i = 0; i < Floors.Length; i++)
                for (int j = i + 1; j < Floors.Length; j++)
                {
                    var a = Floors[i]; var b = Floors[j];
                    float x0 = Math.Max(a.MinX, b.MinX), x1 = Math.Min(a.MaxX, b.MaxX);
                    float z0 = Math.Max(a.MinZ, b.MinZ), z1 = Math.Min(a.MaxZ, b.MaxZ);
                    if (x1 - x0 > Epsilon && (Near(a.MaxZ, b.MinZ) || Near(b.MaxZ, a.MinZ)))
                    {
                        float z = Near(a.MaxZ, b.MinZ) ? a.MaxZ : a.MinZ;
                        float x = CentreFor(a.Id, b.Id, true, (x0 + x1) / 2);
                        doors.Add(new DoorOpening(a.Id, b.Id, new Vector3(x, 0, z), true, 1.2f));
                    }
                    if (z1 - z0 > Epsilon && (Near(a.MaxX, b.MinX) || Near(b.MaxX, a.MinX)))
                    {
                        float x = Near(a.MaxX, b.MinX) ? a.MaxX : a.MinX;
                        float z = CentreFor(a.Id, b.Id, false, (z0 + z1) / 2);
                        doors.Add(new DoorOpening(a.Id, b.Id, new Vector3(x, 0, z), false, 1.6f));
                    }
                }
            doors.Add(new DoorOpening("Vestibule", "Outside", WardLayout.A4_ExitDoor.Position, false, 1.5f));
            Doors = doors.ToArray();
            var walls = new List<WallSpan>();
            for (int r = 0; r < Floors.Length; r++)
            {
                var room = Floors[r];
                Edge(r, true, room.MinZ, room.MinX, room.MaxX, walls);
                Edge(r, true, room.MaxZ, room.MinX, room.MaxX, walls);
                Edge(r, false, room.MinX, room.MinZ, room.MaxZ, walls);
                Edge(r, false, room.MaxX, room.MinZ, room.MaxZ, walls);
            }
            Walls = walls.ToArray();
        }
        private void Edge(int roomIndex, bool alongX, float fixedAxis, float min, float max, List<WallSpan> walls)
        {
            var cuts = new List<float> { min, max };
            for (int i = 0; i < Floors.Length; i++)
            {
                if (i == roomIndex) continue; var other = Floors[i];
                if (alongX ? Near(fixedAxis, other.MinZ) || Near(fixedAxis, other.MaxZ) : Near(fixedAxis, other.MinX) || Near(fixedAxis, other.MaxX))
                {
                    AddCut(cuts, alongX ? other.MinX : other.MinZ, min, max);
                    AddCut(cuts, alongX ? other.MaxX : other.MaxZ, min, max);
                }
            }
            foreach (var door in Doors)
            {
                if (door.AlongX != alongX || !Near(alongX ? door.Centre.z : door.Centre.x, fixedAxis)) continue;
                float centre = alongX ? door.Centre.x : door.Centre.z;
                AddCut(cuts, centre - door.Width / 2, min, max); AddCut(cuts, centre + door.Width / 2, min, max);
            }
            cuts.Sort();
            for (int i = 0; i < cuts.Count - 1; i++)
            {
                float lo = cuts[i], hi = cuts[i + 1], mid = (lo + hi) / 2;
                if (hi - lo < Epsilon) continue;
                bool duplicate = false;
                for (int n = 0; n < roomIndex; n++)
                {
                    var other = Floors[n];
                    if (alongX ? (Near(fixedAxis, other.MinZ) || Near(fixedAxis, other.MaxZ)) && mid > other.MinX && mid < other.MaxX :
                        (Near(fixedAxis, other.MinX) || Near(fixedAxis, other.MaxX)) && mid > other.MinZ && mid < other.MaxZ) duplicate = true;
                }
                bool gap = false;
                foreach (var door in Doors)
                {
                    if (door.AlongX != alongX || !Near(alongX ? door.Centre.z : door.Centre.x, fixedAxis)) continue;
                    float centre = alongX ? door.Centre.x : door.Centre.z;
                    if (mid > centre - door.Width / 2 && mid < centre + door.Width / 2) gap = true;
                }
                if (!duplicate && !gap) walls.Add(alongX ? new WallSpan(new Vector3(lo, 0, fixedAxis), new Vector3(hi, 0, fixedAxis)) :
                    new WallSpan(new Vector3(fixedAxis, 0, lo), new Vector3(fixedAxis, 0, hi)));
            }
        }
        private static void AddCut(List<float> cuts, float value, float min, float max) { if (value > min && value < max) cuts.Add(value); }
        private static bool Near(float a, float b) => Math.Abs(a - b) < Epsilon;
        private static float CentreFor(string a, string b, bool alongX, float fallback)
        {
            if (a == "IsolationB7" || b == "IsolationB7") return WardLayout.A1_DoorWindow.Position.x;
            if (a == "SupplyRoom" || b == "SupplyRoom") return WardLayout.Z2_Doorway.Position.x;
            if (a == "SideRoom" || b == "SideRoom" || a == "LinenRoom" || b == "LinenRoom") return WardLayout.Z3_PatrolNorth.Position.x;
            if (!alongX && (a == "Vestibule" || b == "Vestibule")) return WardLayout.A3_Beacon.Position.z;
            return fallback;
        }
        public bool HasFloor(Vector3 position)
        { foreach (var floor in Floors) if (floor.Contains(position, 0)) return true; return false; }
    }
}
