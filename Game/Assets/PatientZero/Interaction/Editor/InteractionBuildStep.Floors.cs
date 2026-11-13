using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Interaction.Editor
{
    /// <summary>
    /// Contract v1.3 (Claude integration E-19, owner request 2026-10-31): the stairs and the puzzles of the two lower
    /// floors. Every mechanic is one the ward already has, placed again with its act and objective set
    /// (GAME_DESIGN.md section 11). Called from <see cref="Build"/> after the ward's own puzzles, so it shares its publisher.
    /// </summary>
    public static partial class InteractionBuildStep
    {
        private static void Floors(Transform root, ObjectivePublisher publisher)
        {
            // Floor 3: the emergency door now opens onto the stair landing.
            Stairs(root, publisher, ActId.Exit, ObjectiveId.Act4_StairsReached, ObjectiveId.Act4_ExitOpened, WardLayout.A4_Stairs);

            // Floor 2: carry the fuse from Lab A to the fuse box; then the override opens the stair door.
            var bench = At(root, WardLayout.A5_Fuse);
            var fuse = Prop(bench.transform, "Fuse", PrimitiveType.Cylinder, new Vector3(.05f, .07f, .05f), false);
            fuse.requiredItem = true;
            var box = At(root, WardLayout.A5_FuseSocket);
            Shape(box.transform, "Fuse box", PrimitiveType.Cube, new Vector3(.22f, .3f, .1f));
            var socket = box.AddComponent<CableSocket>();
            socket.plug = fuse; socket.objectives = publisher;
            socket.socketAct = ActId.Laboratory; socket.socketObjective = ObjectiveId.Act5_FuseInserted;
            var overrideLever = Constrained(root, publisher, WardLayout.A5_OverrideLever, PhysicalPuzzle.Mechanic.Lever, PrimitiveType.Cube, new Vector3(.08f, .35f, .08f));
            overrideLever.puzzleAct = ActId.Laboratory; overrideLever.puzzleObjective = ObjectiveId.Act5_OverridePulled;
            overrideLever.requires = ObjectiveId.Act5_FuseInserted;
            FloorDoor(root, publisher, WardLayout.A5_StairDoor, "Stairwell door (Floor 2)", ActId.Laboratory,
                ObjectiveId.Act5_OverridePulled, ObjectiveId.Act5_StairDoorOpened);
            Stairs(root, publisher, ActId.Laboratory, ObjectiveId.Act5_StairsReached, ObjectiveId.Act5_StairDoorOpened, WardLayout.A5_Stairs);

            // Floor 1: prime the generator in Security; then the lockdown lever opens the main doors.
            var generator = Constrained(root, publisher, WardLayout.A6_ValveWheel, PhysicalPuzzle.Mechanic.Valve, PrimitiveType.Cylinder, new Vector3(.38f, .04f, .38f));
            generator.puzzleAct = ActId.Lobby; generator.puzzleObjective = ObjectiveId.Act6_GeneratorPrimed;
            var lockdown = Constrained(root, publisher, WardLayout.A6_LockdownLever, PhysicalPuzzle.Mechanic.Lever, PrimitiveType.Cube, new Vector3(.08f, .35f, .08f));
            lockdown.puzzleAct = ActId.Lobby; lockdown.puzzleObjective = ObjectiveId.Act6_LockdownReleased;
            lockdown.requires = ObjectiveId.Act6_GeneratorPrimed;
            FloorDoor(root, publisher, WardLayout.A6_MainDoors, "Main entrance doors", ActId.Lobby,
                ObjectiveId.Act6_LockdownReleased, ObjectiveId.Act6_MainDoorsOpened);
        }

        private static void Stairs(Transform root, ObjectivePublisher publisher, ActId act, ObjectiveId objective, ObjectiveId requires, LayoutPoint landing)
        {
            var o = new GameObject("Stairs down — " + landing.Id); o.transform.SetParent(root, false); o.transform.position = landing.Position;
            var arrival = o.AddComponent<StairArrival>();
            arrival.objectives = publisher; arrival.act = act; arrival.objective = objective; arrival.requires = requires; arrival.landing = landing.Position;
        }

        /// <summary>A swing door in an X-facing wall, like the ward's emergency door, that opens on its own lever (E-26: a double door).</summary>
        private static void FloorDoor(Transform root, ObjectivePublisher publisher, LayoutPoint point, string name, ActId act, ObjectiveId opensOn, ObjectiveId completes)
        {
            var o = new GameObject(name); o.transform.SetParent(root, false); o.transform.position = point.Position; o.transform.rotation = Quaternion.Euler(0, 90, 0);
            o.AddComponent<LayoutTag>().LayoutId = point.Id;
            var door = o.AddComponent<ObjectiveDoor>();
            door.objectives = publisher; door.isolationDoor = false;
            door.doorAct = act; door.opensOn = opensOn; door.completes = completes;
            DoubleDoor(o.transform, door);
        }

        /// <summary>The doorway the ward's walls leave for a door in an X-facing wall (Environment's RoomGeometry and its header).</summary>
        private const float DoorwayWidth = 1.6f, DoorwayHeight = 2.4f;

        /// <summary>
        /// E-26 (owner, 2026-11-13: "make the doors open like an actual door"): two leaves filling the doorway, each on a hinge at
        /// its own jamb, with a push bar on the side the player comes from (local -z). Both swing away from the player, into the
        /// room beyond (local +z): <see cref="ObjectiveDoor"/> turns the right leaf (seen from the approach) one way and its pair the other.
        /// </summary>
        private static void DoubleDoor(Transform frame, ObjectiveDoor door)
        {
            float half = DoorwayWidth * .5f, width = half - .015f, height = DoorwayHeight - .02f;
            foreach (int side in new[] { 1, -1 })
            {
                var hinge = new GameObject(side > 0 ? "Door hinge right" : "Door hinge left").transform;
                hinge.SetParent(frame, false);
                hinge.localPosition = new Vector3(side * half, 0, 0);
                var leaf = Shape(hinge, "Door leaf", PrimitiveType.Cube, new Vector3(width, height, .06f));
                leaf.transform.localPosition = new Vector3(-side * (width * .5f + .005f), .01f + height * .5f, 0);
                var bar = Shape(hinge, "Push bar", PrimitiveType.Cube, new Vector3(width * .7f, .045f, .045f));
                bar.transform.localPosition = new Vector3(-side * (width * .55f), 1.02f, -.06f);
                Object.DestroyImmediate(bar.GetComponent<Collider>());
                if (side > 0) door.leaf = hinge; else door.pair = hinge;
            }
        }
    }
}
