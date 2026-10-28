using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Environment.Logic;
using UnityEngine;

namespace PatientZero.Environment.Tests
{
    public sealed class RoomGeometryTests
    {
        [Test] public void EveryLayoutAnchorHasFloor()
        {
            var g = new RoomGeometry(); Assert.AreEqual(WardLayout.Rooms.Count, g.Floors.Length);
            foreach (var p in WardLayout.Points) if (p.Kind == LayoutKind.TeleportAnchor) Assert.IsTrue(g.HasFloor(p.Position), p.Id);
        }
        [Test] public void DoorOpeningsMatchApprovedMarkersAndAreClearOfWalls()
        {
            var g = new RoomGeometry(); Assert.AreEqual(6, g.Doors.Length);
            foreach (var door in g.Doors)
                foreach (var wall in g.Walls)
                    Assert.GreaterOrEqual(WardLayout.HorizontalDistanceToSegment(door.Centre, wall.A, wall.B), door.Width / 2 - .001f);
            Assert.IsTrue(System.Array.Exists(g.Doors, d => d.Centre == new Vector3(WardLayout.Z2_Doorway.Position.x, 0, WardLayout.Z2_Doorway.Position.z)));
        }
        [Test] public void WallSpansAreNonzeroAxisAlignedAndNeverDuplicated()
        {
            var g = new RoomGeometry();
            for (int i = 0; i < g.Walls.Length; i++)
            {
                var w = g.Walls[i]; Assert.Greater(w.Length, 0); Assert.IsTrue(w.A.x == w.B.x || w.A.z == w.B.z);
                for (int j = i + 1; j < g.Walls.Length; j++) Assert.IsFalse(w.A == g.Walls[j].A && w.B == g.Walls[j].B);
            }
        }
        [Test] public void EachRoomBoundaryIsCoveredByAWallOrDoor()
        {
            var g = new RoomGeometry();
            foreach (var room in g.Floors)
                for (int edge = 0; edge < 4; edge++)
                    for (int step = 1; step < 100; step++)
                    {
                        float t = step / 100f;
                        var p = edge < 2 ? new Vector3(room.MinX + (room.MaxX - room.MinX) * t, 0, edge == 0 ? room.MinZ : room.MaxZ) :
                            new Vector3(edge == 2 ? room.MinX : room.MaxX, 0, room.MinZ + (room.MaxZ - room.MinZ) * t);
                        bool covered = false;
                        foreach (var w in g.Walls) if (WardLayout.HorizontalDistanceToSegment(p, w.A, w.B) < .001f) covered = true;
                        foreach (var d in g.Doors) if ((d.AlongX ? System.Math.Abs(p.z - d.Centre.z) < .001f && System.Math.Abs(p.x - d.Centre.x) <= d.Width / 2 :
                            System.Math.Abs(p.x - d.Centre.x) < .001f && System.Math.Abs(p.z - d.Centre.z) <= d.Width / 2)) covered = true;
                        Assert.IsTrue(covered, room.Id + " edge " + edge + " t=" + t);
                    }
        }
    }
}
