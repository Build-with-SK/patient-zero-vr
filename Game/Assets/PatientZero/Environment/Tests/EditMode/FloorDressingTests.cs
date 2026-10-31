using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Environment.Logic;
using UnityEngine;

namespace PatientZero.Environment.Tests
{
    /// <summary>v1.3 (Claude integration E-19): the stairwells' and lower floors' dressing stays on its floor and out of the way.</summary>
    public sealed class FloorDressingTests
    {
        private static float BoxDistance(DressingBox box, Vector3 p)
        {
            float dx = System.Math.Max(0, System.Math.Abs(box.Position.x - p.x) - box.Size.x / 2);
            float dz = System.Math.Max(0, System.Math.Abs(box.Position.z - p.z) - box.Size.z / 2);
            return (float)System.Math.Sqrt(dx * dx + dz * dz);
        }

        [Test] public void EveryBoxIsOnAFloorAndHasSize()
        {
            var geometry = new RoomGeometry();
            foreach (var box in FloorDressing.Create())
            {
                Assert.Greater(box.Size.x, 0, box.Name); Assert.Greater(box.Size.y, 0, box.Name); Assert.Greater(box.Size.z, 0, box.Name);
                foreach (float x in new[] { -.5f, .5f }) foreach (float z in new[] { -.5f, .5f })
                    Assert.IsTrue(geometry.HasFloor(box.Position + new Vector3(box.Size.x * x, 0, box.Size.z * z)), box.Name);
                Assert.LessOrEqual(box.Position.y + box.Size.y / 2, 2.8f, box.Name + " pokes through the ceiling");
            }
        }

        [Test] public void SolidBoxesKeepAnchorsItemsZombiesAndDoorsClear()
        {
            var geometry = new RoomGeometry();
            foreach (var box in FloorDressing.Create())
            {
                if (!box.Collider) continue;
                float top = box.Position.y + box.Size.y / 2;
                foreach (var point in WardLayout.Points)
                {
                    if (point.Kind == LayoutKind.Landmark) continue;
                    // A puzzle part may rest on a bench or sit in front of a housing; it is above the box, not in it.
                    if (point.Kind == LayoutKind.Interactable && point.Position.y >= top - .01f) continue;
                    Assert.GreaterOrEqual(BoxDistance(box, point.Position), .3f, box.Name + " / " + point.Id);
                }
                foreach (var door in geometry.Doors)
                    Assert.GreaterOrEqual(BoxDistance(box, door.Centre), .3f, box.Name + " / door " + door.RoomA + "-" + door.RoomB);
            }
        }

        [Test] public void SolidBoxesNeverBlockACriticalPathStepOrTheLobbyPatrol()
        {
            var path = WardLayout.CriticalPath;
            foreach (var box in FloorDressing.Create())
            {
                if (!box.Collider) continue;
                for (int n = 1; n < path.Count; n++)
                {
                    if (WardLayout.FloorOf(path[n - 1].Act) != WardLayout.FloorOf(path[n].Act)) continue; // the stairs, not a walk
                    for (int i = 0; i <= 100; i++)
                    {
                        var p = path[n - 1].Position + (path[n].Position - path[n - 1].Position) * (i / 100f);
                        Assert.GreaterOrEqual(BoxDistance(box, p), .3f, box.Name + " blocks " + path[n - 1].Id + " -> " + path[n].Id);
                    }
                }
                for (int i = 0; i <= 40; i++)
                {
                    var p = WardLayout.Z6_PatrolNorth.Position + (WardLayout.Z6_PatrolSouth.Position - WardLayout.Z6_PatrolNorth.Position) * (i / 40f);
                    Assert.GreaterOrEqual(BoxDistance(box, p), .3f, box.Name + " blocks the lobby patrol");
                }
            }
        }

        [Test] public void SignsSitInsideARoomBelowTheCeiling()
        {
            var geometry = new RoomGeometry();
            foreach (var sign in FloorDressing.Signs())
            {
                Assert.IsFalse(string.IsNullOrEmpty(sign.Text));
                Assert.IsTrue(geometry.HasFloor(sign.Position), sign.Text);
                Assert.GreaterOrEqual(sign.Position.y, .5f, sign.Text); Assert.LessOrEqual(sign.Position.y, 2.75f, sign.Text);
            }
            foreach (var exit in FloorDressing.ExitSigns) Assert.IsTrue(geometry.HasFloor(exit), "exit sign " + exit);
        }

        [Test] public void StairwellDoorsJoinOnlyTheirOwnFloor()
        {
            var geometry = new RoomGeometry();
            Assert.IsFalse(System.Array.Exists(geometry.Doors, d => d.RoomB == "Outside" || d.RoomA == "Outside"), "the emergency door now opens onto Stair3");
            Assert.IsTrue(System.Array.Exists(geometry.Doors, d => d.RoomA == "Vestibule" && d.RoomB == "Stair3" &&
                d.Centre == new Vector3(WardLayout.A4_ExitDoor.Position.x, 0, WardLayout.A4_ExitDoor.Position.z)));
            Assert.IsTrue(System.Array.Exists(geometry.Doors, d => d.RoomA == "LabCorridor" && d.RoomB == "Stair2Down" && d.Centre == WardLayout.A5_StairDoor.Position));
            Assert.IsTrue(System.Array.Exists(geometry.Doors, d => d.RoomA == "Lobby" && d.RoomB == "Entrance" && d.Centre == WardLayout.A6_MainDoors.Position));
        }
    }
}
