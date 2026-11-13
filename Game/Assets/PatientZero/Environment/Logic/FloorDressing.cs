using System.Collections.Generic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Environment.Logic
{
    /// <summary>A wall sign: text at a position, readable when looking along <see cref="Yaw"/> (degrees clockwise from +Z).</summary>
    public readonly struct FloorSign
    {
        public readonly string Text;
        public readonly Vector3 Position;
        public readonly float Size, Yaw;
        public FloorSign(string text, Vector3 position, float size, float yaw) { Text = text; Position = position; Size = size; Yaw = yaw; }
    }

    /// <summary>
    /// Contract v1.3 (Claude integration E-19, owner request 2026-10-31): static dressing and signs for the stairwells
    /// and the two lower floors (GAME_DESIGN.md section 11). Built by seat B's <c>HospitalBuildSteps</c> before the
    /// NavMesh bake, beside the ward's own <see cref="HospitalDressing"/>. Nothing here sits on a critical-path anchor,
    /// a puzzle or a zombie's route; <c>FloorDressingTests</c> proves it.
    /// </summary>
    public static class FloorDressing
    {
        /// <summary>Exit signs, drawn with the ward's green emissive like the Act 3 beacon sign.</summary>
        public static readonly Vector3[] ExitSigns =
        {
            new Vector3(19.44f, 2.45f, 3.25f), new Vector3(48.44f, 2.45f, 3.25f), new Vector3(76.94f, 2.45f, 3.25f),
        };

        public static DressingBox[] Create()
        {
            var boxes = new List<DressingBox>();

            // Floor 3 stairwell: the flight down is a dark opening in the east wall; the flight up is barricaded.
            StairLanding(boxes, "Stair3", 19.5f, 3.25f);

            // Floor 2, laboratories.
            StairLanding(boxes, "Stair2Down", 48.5f, 3.25f);
            Barricade(boxes, "Stair2", 31.25f, 4.35f);
            boxes.Add(new DressingBox("LabA_FuseBench", new Vector3(38.9f, .46f, 6.3f), new Vector3(1.0f, .92f, .6f)));
            boxes.Add(new DressingBox("LabA_Microscope", new Vector3(39.25f, 1.02f, 6.4f), new Vector3(.12f, .2f, .16f), false, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("LabA_Shelving", new Vector3(36.25f, 1f, 5.75f), new Vector3(.4f, 2f, 1.8f)));
            boxes.Add(new DressingBox("LabA_SpecimenTank", new Vector3(39.6f, .8f, 4.95f), new Vector3(.5f, 1.6f, .5f), true, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("LabA_SpecimenFluid", new Vector3(39.6f, .95f, 4.95f), new Vector3(.44f, 1.1f, .44f), false, DressingStyle.Wayfinding));
            boxes.Add(new DressingBox("LabB_Desk", new Vector3(34.6f, .4f, -1.2f), new Vector3(1.2f, .8f, .5f)));
            boxes.Add(new DressingBox("LabB_Monitor", new Vector3(34.6f, 1.0f, -1.35f), new Vector3(.45f, .3f, .04f), false));
            boxes.Add(new DressingBox("LabB_Cabinet", new Vector3(36.7f, 1f, -1.25f), new Vector3(.5f, 2f, .4f)));
            boxes.Add(new DressingBox("LabB_Gurney", new Vector3(36.2f, .55f, 1.0f), new Vector3(.6f, .12f, 1.2f)));
            boxes.Add(new DressingBox("LabB_Restraints", new Vector3(36.2f, .63f, 1.0f), new Vector3(.62f, .04f, .08f), false, DressingStyle.Trace));
            boxes.Add(new DressingBox("LabCorridor_Crates", new Vector3(43.5f, .4f, 4.2f), new Vector3(.8f, .8f, .5f), true, DressingStyle.Fabric));
            boxes.Add(new DressingBox("FuseBox_Cable", new Vector3(41.8f, 1.85f, 2.04f), new Vector3(.04f, 1.0f, .04f), false));

            // Floor 1, main lobby.
            Barricade(boxes, "Stair1", 61.25f, 4.35f);
            boxes.Add(new DressingBox("Lobby_ReceptionDesk", new Vector3(64.6f, .55f, .2f), new Vector3(2.4f, 1.1f, .6f), true, DressingStyle.Wayfinding));
            boxes.Add(new DressingBox("Lobby_ReceptionTop", new Vector3(64.6f, 1.12f, .2f), new Vector3(2.5f, .04f, .7f), false, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("Lobby_Pillar_West", new Vector3(66f, 1.4f, 1.5f), new Vector3(.4f, 2.8f, .4f), true, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("Lobby_Pillar_East", new Vector3(72.6f, 1.4f, 5.6f), new Vector3(.4f, 2.8f, .4f), true, DressingStyle.Ceramic));
            boxes.Add(new DressingBox("Lobby_Seating", new Vector3(68f, .25f, -.8f), new Vector3(2f, .5f, .5f), true, DressingStyle.Fabric));
            boxes.Add(new DressingBox("Lobby_SeatBacks", new Vector3(68f, .65f, -1.02f), new Vector3(2f, .5f, .06f), false, DressingStyle.Fabric));
            boxes.Add(new DressingBox("Lobby_OverturnedChair", new Vector3(71.6f, .2f, -.6f), new Vector3(.5f, .4f, .5f), true, DressingStyle.Damage));
            boxes.Add(new DressingBox("Lobby_Bloodtrail", new Vector3(70.2f, .005f, 2.4f), new Vector3(.25f, .01f, 6.5f), false, DressingStyle.Trace));
            boxes.Add(new DressingBox("Security_Desk", new Vector3(68f, .4f, 9.4f), new Vector3(1.2f, .8f, .6f)));
            boxes.Add(new DressingBox("Security_Monitors", new Vector3(68f, 1.0f, 9.62f), new Vector3(.9f, .35f, .05f), false));
            boxes.Add(new DressingBox("Security_Generator", new Vector3(66.8f, .4f, 9.75f), new Vector3(.9f, .8f, .4f), true, DressingStyle.Wayfinding));
            boxes.Add(new DressingBox("Security_Pipe", new Vector3(66.8f, 1.9f, 9.92f), new Vector3(.08f, 1.6f, .08f), false));
            boxes.Add(new DressingBox("Entrance_Daylight", new Vector3(76.94f, 1.15f, 3.25f), new Vector3(.04f, 2.2f, 2.3f), false, DressingStyle.Ceramic));
            return boxes.ToArray();
        }

        public static FloorSign[] Signs()
        {
            return new[]
            {
                // Floor 3. E-26 (owner, 2026-11-13): the two-minute version skips floor 2, so the ward's stairs lead to the lobby.
                new FloorSign("STAIRS - GROUND FLOOR", new Vector3(16.92f, 2.6f, 3.25f), .035f, 90),
                new FloorSign("DOWN TO GROUND FLOOR\nMAIN LOBBY", new Vector3(19.42f, 1.75f, 3.25f), .04f, 90),
                new FloorSign("UPPER FLOORS SEALED", new Vector3(18.25f, 1.6f, 4.42f), .03f, 0),
                // Floor 2
                new FloorSign("FLOOR 2 - LABORATORIES", new Vector3(31.25f, 2.0f, 4.42f), .045f, 0),
                new FloorSign("LAB A - SPECIMENS", new Vector3(38f, 2.6f, 4.42f), .035f, 0),
                // E-23 (owner, 2026-11-11): no instructions on the walls. "LAB B - DO NOT WAKE" is now "LAB B"; FUSE MISSING /
                // SPARE FUSES: LAB A and STAIR DOOR OVERRIDE / NEEDS POWER are gone.
                new FloorSign("LAB B", new Vector3(35f, 2.6f, 2.08f), .035f, 180),
                new FloorSign("STAIRS - GROUND FLOOR", new Vector3(45.92f, 2.6f, 3.25f), .035f, 90),
                new FloorSign("DOWN TO GROUND FLOOR\nMAIN LOBBY", new Vector3(48.42f, 1.75f, 3.25f), .04f, 90),
                // Floor 1
                new FloorSign("GROUND FLOOR - MAIN LOBBY", new Vector3(61.25f, 2.0f, 4.42f), .045f, 0),
                new FloorSign("RECEPTION", new Vector3(64.6f, .75f, .52f), .04f, 180),
                new FloorSign("SECURITY", new Vector3(67f, 2.6f, 6.92f), .035f, 0),
                new FloorSign("GENERATOR", new Vector3(66.8f, 1.6f, 9.93f), .025f, 0), // E-23: was GENERATOR / PRIME BEFORE RELEASE
                new FloorSign("LOCKDOWN RELEASE", new Vector3(73.92f, 1.65f, 1.7f), .025f, 90),
                new FloorSign("MAIN ENTRANCE", new Vector3(73.92f, 2.6f, 3.25f), .035f, 90),
            };
        }

        /// <summary>The landing's flight down: a dark opening and a rail in the given east wall.</summary>
        private static void StairLanding(List<DressingBox> boxes, string room, float eastWall, float z)
        {
            boxes.Add(new DressingBox(room + "_FlightDown", new Vector3(eastWall - .04f, 1.1f, z), new Vector3(.03f, 2.2f, 1.3f), false, DressingStyle.Damage));
            boxes.Add(new DressingBox(room + "_Handrail", new Vector3(eastWall - .35f, .95f, z - .75f), new Vector3(.6f, .04f, .04f), false));
            boxes.Add(new DressingBox(room + "_Handrail", new Vector3(eastWall - .35f, .95f, z + .75f), new Vector3(.6f, .04f, .04f), false));
            Barricade(boxes, room, eastWall - 1.25f, 4.35f);
        }

        /// <summary>The flight up, blocked: planks across the north wall of a landing.</summary>
        private static void Barricade(List<DressingBox> boxes, string room, float x, float z)
        {
            for (int i = 0; i < 3; i++)
                boxes.Add(new DressingBox(room + "_SealedUp_" + i, new Vector3(x, .5f + i * .45f, z), new Vector3(1.6f, .12f, .06f), false, DressingStyle.Damage));
        }
    }
}
