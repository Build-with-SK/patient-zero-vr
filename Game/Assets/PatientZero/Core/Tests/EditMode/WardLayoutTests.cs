using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PatientZero.Core.Tests
{
    /// <summary>
    /// Proves from the layout alone that every required puzzle is reachable, standing or
    /// seated, by teleport and arm's length, and that no forced position is inside a
    /// zombie's reach. These checks run before any scene exists.
    /// </summary>
    public class WardLayoutTests
    {
        [Test]
        public void Ids_AreUnique()
        {
            var seen = new HashSet<string>();
            foreach (var p in WardLayout.Points) Assert.IsTrue(seen.Add(p.Id), "Duplicate layout id " + p.Id);
        }

        [Test]
        public void TryGet_FindsEveryPoint()
        {
            foreach (var p in WardLayout.Points)
            {
                LayoutPoint found;
                Assert.IsTrue(WardLayout.TryGet(p.Id, out found), p.Id);
                Assert.IsTrue(found.Position == p.Position, p.Id);
            }
            LayoutPoint none;
            Assert.IsFalse(WardLayout.TryGet("No.Such.Point", out none));
        }

        [Test]
        public void RequiredInteractables_AreInTheComfortableHeightBand()
        {
            foreach (var p in WardLayout.Points)
            {
                if (p.Kind != LayoutKind.Interactable || !p.Required) continue;
                Assert.GreaterOrEqual(p.Position.y, WardLayout.MinInteractHeight, p.Id + " is too low for seated play");
                Assert.LessOrEqual(p.Position.y, WardLayout.MaxInteractHeight, p.Id + " is too high");
            }
        }

        [Test]
        public void EveryInteractable_IsWithinArmsReachOfAnAnchorInTheSameAct()
        {
            foreach (var item in WardLayout.Points)
            {
                if (item.Kind != LayoutKind.Interactable) continue;
                float best = float.MaxValue;
                foreach (var anchor in WardLayout.Points)
                {
                    if (anchor.Kind != LayoutKind.TeleportAnchor || anchor.Act != item.Act) continue;
                    best = Mathf.Min(best, WardLayout.HorizontalDistance(anchor.Position, item.Position));
                }
                Assert.LessOrEqual(best, WardLayout.MaxReachHorizontal, item.Id + " is " + best + " m from the nearest anchor");
            }
        }

        [Test]
        public void CriticalPath_StepsAreWithinTeleportRange()
        {
            var path = WardLayout.CriticalPath;
            Assert.AreEqual("A1.Start", path[0].Id);
            Assert.AreEqual("A6.Exit", path[path.Count - 1].Id); // v1.3: the run ends at the lobby's main doors
            for (int i = 1; i < path.Count; i++)
            {
                // v1.3: the stairs carry the player between floors; within a floor every step is a teleport.
                if (WardLayout.FloorOf(path[i - 1].Act) != WardLayout.FloorOf(path[i].Act)) continue;
                float step = WardLayout.HorizontalDistance(path[i - 1].Position, path[i].Position);
                Assert.LessOrEqual(step, WardLayout.TeleportMaxDistance, path[i - 1].Id + " -> " + path[i].Id + " is " + step + " m");
            }
        }

        [Test]
        public void CriticalPath_IncludesEveryActStartInOrder()
        {
            var acts = new[] { ActId.Awakening, ActId.Machinery, ActId.Darkness, ActId.Exit, ActId.Laboratory, ActId.Lobby };
            int cursor = 0;
            foreach (var act in acts)
            {
                string start = WardLayout.ActStart(act).Id;
                while (cursor < WardLayout.CriticalPath.Count && WardLayout.CriticalPath[cursor].Id != start) cursor++;
                Assert.Less(cursor, WardLayout.CriticalPath.Count, start + " missing from the critical path or out of order");
            }
        }

        [Test]
        public void EveryAnchor_IsInsideARoomWithWallClearance()
        {
            foreach (var p in WardLayout.Points)
            {
                if (p.Kind != LayoutKind.TeleportAnchor) continue;
                bool inside = false;
                foreach (var room in WardLayout.Rooms) inside |= room.Contains(p.Position, WardLayout.WallClearance);
                Assert.IsTrue(inside, p.Id + " is not inside any room with " + WardLayout.WallClearance + " m clearance");
            }
        }

        [Test]
        public void FinalPatient_CannotReachAnyAct4AnchorOrTheWalkBetweenThem()
        {
            float safe = WardLayout.FinalPatientReach + WardLayout.FairnessMargin;
            Vector3 gurney = WardLayout.Z4_Gurney.Position;
            var exitPath = new List<LayoutPoint>();
            foreach (var p in WardLayout.CriticalPath)
            {
                if (p.Act == ActId.Exit) exitPath.Add(p);
            }
            Assert.GreaterOrEqual(exitPath.Count, 2);

            foreach (var p in exitPath)
            {
                Assert.GreaterOrEqual(WardLayout.HorizontalDistance(p.Position, gurney), safe, p.Id + " is inside the final patient's reach");
            }
            for (int i = 1; i < exitPath.Count; i++)
            {
                float d = WardLayout.HorizontalDistanceToSegment(gurney, exitPath[i - 1].Position, exitPath[i].Position);
                Assert.GreaterOrEqual(d, safe, "Walking " + exitPath[i - 1].Id + " -> " + exitPath[i].Id + " passes within " + d + " m");
            }
            float lever = WardLayout.HorizontalDistance(WardLayout.A4_ReleaseLever.Position, gurney);
            Assert.GreaterOrEqual(lever, safe, "The release lever is inside the final patient's reach");
        }

        [Test]
        public void DarknessPatrol_NeverRunsThroughAnAnchor()
        {
            Vector3 a = WardLayout.Z3_PatrolNorth.Position, b = WardLayout.Z3_PatrolSouth.Position;
            foreach (var p in WardLayout.Points)
            {
                if (p.Kind != LayoutKind.TeleportAnchor) continue;
                float d = WardLayout.HorizontalDistanceToSegment(p.Position, a, b);
                Assert.GreaterOrEqual(d, WardLayout.PatrolClearance, p.Id + " sits " + d + " m from the Act 3 patrol line");
            }
        }

        [Test]
        public void Beacon_TriggerIsTheLastDarknessAnchor()
        {
            float d = WardLayout.HorizontalDistance(WardLayout.A3_End.Position, WardLayout.A3_Beacon.Position);
            Assert.LessOrEqual(d, 1.0f, "The beacon should sit just beyond the last dark anchor");
        }

        [Test]
        public void HorizontalDistanceToSegment_HandlesEndsAndMiddle()
        {
            var a = new Vector3(0f, 0f, 0f);
            var b = new Vector3(10f, 0f, 0f);
            Assert.AreEqual(2f, WardLayout.HorizontalDistanceToSegment(new Vector3(5f, 7f, 2f), a, b), 1e-5f);
            Assert.AreEqual(5f, WardLayout.HorizontalDistanceToSegment(new Vector3(-3f, 0f, 4f), a, b), 1e-5f);
            Assert.AreEqual(1f, WardLayout.HorizontalDistanceToSegment(new Vector3(1f, 0f, 0f), a, a), 1e-5f);
        }

        [Test]
        public void ActStart_RejectsNone()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WardLayout.ActStart(ActId.None));
        }

        // v1.3 (E-19): three floors.

        [Test]
        public void EveryRoomHasAFloor_AndFloorsNeverTouch()
        {
            Assert.AreEqual(WardLayout.Rooms.Count, WardLayout.RoomFloors.Count);
            for (int i = 0; i < WardLayout.Rooms.Count; i++)
            {
                Assert.GreaterOrEqual(WardLayout.RoomFloors[i], 1, WardLayout.Rooms[i].Id); Assert.LessOrEqual(WardLayout.RoomFloors[i], 3, WardLayout.Rooms[i].Id);
                for (int j = i + 1; j < WardLayout.Rooms.Count; j++)
                {
                    if (WardLayout.RoomFloors[i] == WardLayout.RoomFloors[j]) continue;
                    RoomRect a = WardLayout.Rooms[i], b = WardLayout.Rooms[j];
                    float gapX = Mathf.Max(a.MinX - b.MaxX, b.MinX - a.MaxX), gapZ = Mathf.Max(a.MinZ - b.MaxZ, b.MinZ - a.MaxZ);
                    Assert.GreaterOrEqual(Mathf.Max(gapX, gapZ), WardLayout.FloorSeparation, a.Id + " and " + b.Id + " are on different floors but too close");
                }
            }
        }

        [Test]
        public void EveryPoint_IsInARoomOfItsActsFloor()
        {
            foreach (var p in WardLayout.Points)
            {
                if (p.Kind == LayoutKind.Landmark) continue; // door landmarks sit on the wall line between two rooms
                bool inside = false;
                for (int r = 0; r < WardLayout.Rooms.Count; r++)
                    inside |= WardLayout.RoomFloors[r] == WardLayout.FloorOf(p.Act) && WardLayout.Rooms[r].Contains(p.Position, 0);
                Assert.IsTrue(inside, p.Id + " is not inside a room of floor " + WardLayout.FloorOf(p.Act));
            }
        }

        [Test]
        public void FloorStartsAndStairLandings_MatchThePlayOrder()
        {
            Assert.IsTrue(WardLayout.StartsFloor(ActId.Awakening));
            Assert.IsFalse(WardLayout.StartsFloor(ActId.Machinery));
            Assert.IsFalse(WardLayout.StartsFloor(ActId.Exit));
            Assert.IsTrue(WardLayout.StartsFloor(ActId.Laboratory));
            Assert.IsTrue(WardLayout.StartsFloor(ActId.Lobby));
            Assert.AreEqual(3, WardLayout.FloorOf(ActId.Exit));
            Assert.AreEqual(2, WardLayout.FloorOf(ActId.Laboratory));
            Assert.AreEqual(1, WardLayout.FloorOf(ActId.Lobby));
            Assert.AreEqual(0, WardLayout.FloorOf(ActId.None));
            Assert.IsTrue(WardLayout.TryStairLanding(ActId.Exit, out var ward));
            Assert.AreEqual("A4.Stairs", ward.Id);
            Assert.IsTrue(WardLayout.TryStairLanding(ActId.Laboratory, out var labs));
            Assert.AreEqual("A5.Stairs", labs.Id);
            Assert.IsFalse(WardLayout.TryStairLanding(ActId.Lobby, out _));
            Assert.IsFalse(WardLayout.TryStairLanding(ActId.Darkness, out _));
        }

        [Test]
        public void StairLanding_IsTheLastAnchorOfItsFloor()
        {
            var path = WardLayout.CriticalPath;
            foreach (var act in new[] { ActId.Exit, ActId.Laboratory })
            {
                Assert.IsTrue(WardLayout.TryStairLanding(act, out var landing));
                int at = -1;
                for (int i = 0; i < path.Count; i++) if (path[i].Id == landing.Id) at = i;
                Assert.GreaterOrEqual(at, 0, landing.Id + " missing from the critical path");
                Assert.Less(at + 1, path.Count);
                Assert.AreNotEqual(WardLayout.FloorOf(act), WardLayout.FloorOf(path[at + 1].Act), landing.Id + " is not the last anchor of its floor");
            }
        }

        [Test]
        public void LobbyPatrol_NeverRunsThroughAnAnchor()
        {
            Vector3 a = WardLayout.Z6_PatrolNorth.Position, b = WardLayout.Z6_PatrolSouth.Position;
            foreach (var p in WardLayout.Points)
            {
                if (p.Kind != LayoutKind.TeleportAnchor && !(p.Kind == LayoutKind.Interactable && p.Required)) continue;
                float d = WardLayout.HorizontalDistanceToSegment(p.Position, a, b);
                Assert.GreaterOrEqual(d, WardLayout.PatrolClearance, p.Id + " sits " + d + " m from the lobby patrol line");
            }
        }

        [Test]
        public void SleepingZombies_AreOutOfCatchReachOfEveryAnchor()
        {
            foreach (var zombie in new[] { WardLayout.Z5_Dormant, WardLayout.Z7_Dormant })
                foreach (var p in WardLayout.Points)
                {
                    if (p.Kind != LayoutKind.TeleportAnchor) continue;
                    float d = WardLayout.HorizontalDistance(p.Position, zombie.Position);
                    Assert.GreaterOrEqual(d, 2.0f, p.Id + " is " + d + " m from " + zombie.Id);
                }
        }
    }
}
