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
        [Test] public void BuiltWallCollidersPreserveTheEntireDoorWidth()
        {
            var geometry = new RoomGeometry();
            foreach (var door in geometry.Doors)
                foreach (var wall in geometry.Walls)
                {
                    var size = wall.ColliderSize; var centre = (wall.A + wall.B) * .5f;
                    // Test the actual box extents, not just the ideal wall segment.
                    float normal = door.AlongX ? System.Math.Abs(centre.z - door.Centre.z) : System.Math.Abs(centre.x - door.Centre.x);
                    float normalHalf = (door.AlongX ? size.z : size.x) / 2;
                    if (normal > normalHalf + .001f) continue;
                    float distance = door.AlongX ? System.Math.Abs(centre.x - door.Centre.x) : System.Math.Abs(centre.z - door.Centre.z);
                    float half = (door.AlongX ? size.x : size.z) / 2;
                    Assert.GreaterOrEqual(distance - half, door.Width / 2 - .001f, door.RoomA + " / " + door.RoomB);
                }
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
        [Test] public void TheatreDressingStaysOnFloorAndClearOfZombieAndPuzzleRoutes()
        {
            var geometry = new RoomGeometry();
            foreach (var prop in HospitalDressing.Create())
            {
                foreach (float x in new[] { -.5f, .5f }) foreach (float z in new[] { -.5f, .5f })
                    Assert.IsTrue(geometry.HasFloor(prop.Position + new Vector3(prop.Size.x * x, 0, prop.Size.z * z)), prop.Name);
                if (!prop.Collider) continue;
                foreach (var point in WardLayout.Points)
                {
                    float dx = System.Math.Max(0, System.Math.Abs(prop.Position.x - point.Position.x) - prop.Size.x / 2);
                    float dz = System.Math.Max(0, System.Math.Abs(prop.Position.z - point.Position.z) - prop.Size.z / 2);
                    Assert.GreaterOrEqual(dx * dx + dz * dz, .3f * .3f, prop.Name + " / " + point.Id);
                }
                for (int i = 0; i <= 20; i++)
                {
                    var point = WardLayout.Z2_Dormant.Position + (WardLayout.Z2_Doorway.Position - WardLayout.Z2_Dormant.Position) * (i / 20f);
                    float dx = System.Math.Max(0, System.Math.Abs(prop.Position.x - point.x) - prop.Size.x / 2);
                    float dz = System.Math.Max(0, System.Math.Abs(prop.Position.z - point.z) - prop.Size.z / 2);
                    Assert.GreaterOrEqual(dx * dx + dz * dz, .3f * .3f, prop.Name + " blocks Z2");
                }
            }
        }
        [Test] public void NewDressingLeavesEveryCriticalPathSegmentAndDoorClear()
        {
            var boxes = HospitalDressing.Create(); var geometry = new RoomGeometry();
            foreach (var box in boxes)
            {
                Assert.Greater(box.Size.x, 0); Assert.Greater(box.Size.y, 0); Assert.Greater(box.Size.z, 0);
                if (!box.Collider) continue;
                for (int n = 1; n < WardLayout.CriticalPath.Count; n++)
                {
                    var a = WardLayout.CriticalPath[n - 1]; var b = WardLayout.CriticalPath[n];
                    for (int i = 0; i <= 100; i++)
                    {
                        var p = a.Position + (b.Position - a.Position) * (i / 100f);
                        float dx = System.Math.Max(0, System.Math.Abs(box.Position.x - p.x) - box.Size.x / 2);
                        float dz = System.Math.Max(0, System.Math.Abs(box.Position.z - p.z) - box.Size.z / 2);
                        Assert.GreaterOrEqual(dx * dx + dz * dz, .3f * .3f, box.Name + " blocks " + a.Id + " -> " + b.Id);
                    }
                }
                foreach (var door in geometry.Doors)
                {
                    float dx = System.Math.Max(0, System.Math.Abs(box.Position.x - door.Centre.x) - box.Size.x / 2);
                    float dz = System.Math.Max(0, System.Math.Abs(box.Position.z - door.Centre.z) - box.Size.z / 2);
                    Assert.GreaterOrEqual(dx * dx + dz * dz, .3f * .3f, box.Name + " / " + door.RoomA);
                }
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
        [Test] public void ColliderLongitudinalExtentNeverShrinksDoorOpenings()
        {
            var geometry = new RoomGeometry();
            foreach (var span in geometry.Walls)
            {
                var size = span.ColliderSize;
                Assert.AreEqual(span.Length, System.Math.Max(size.x, size.z), .001);
                foreach (var door in geometry.Doors)
                {
                    bool horizontal = System.Math.Abs(span.B.x - span.A.x) > .001;
                    if (horizontal != door.AlongX) continue;
                    float axis = horizontal ? span.A.z : span.A.x;
                    if (System.Math.Abs(axis - (horizontal ? door.Centre.z : door.Centre.x)) > .001) continue;
                    float centre = horizontal ? door.Centre.x : door.Centre.z;
                    float min = System.Math.Min(horizontal ? span.A.x : span.A.z, horizontal ? span.B.x : span.B.z);
                    float max = System.Math.Max(horizontal ? span.A.x : span.A.z, horizontal ? span.B.x : span.B.z);
                    Assert.IsTrue(max <= centre - door.Width / 2 + .001 || min >= centre + door.Width / 2 - .001);
                }
            }
        }
    }
}
