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

        /// <summary>A swing door in an X-facing wall, like the ward's emergency door, that opens on its own lever.</summary>
        private static void FloorDoor(Transform root, ObjectivePublisher publisher, LayoutPoint point, string name, ActId act, ObjectiveId opensOn, ObjectiveId completes)
        {
            var o = new GameObject(name); o.transform.SetParent(root, false); o.transform.position = point.Position; o.transform.rotation = Quaternion.Euler(0, 90, 0);
            o.AddComponent<LayoutTag>().LayoutId = point.Id;
            var leaf = Shape(o.transform, "Door leaf", PrimitiveType.Cube, new Vector3(1.5f, 2.2f, .08f)); leaf.transform.localPosition = Vector3.up * 1.1f;
            var door = o.AddComponent<ObjectiveDoor>();
            door.objectives = publisher; door.leaf = leaf.transform; door.isolationDoor = false;
            door.doorAct = act; door.opensOn = opensOn; door.completes = completes;
        }
    }
}
