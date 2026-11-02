// Claude integration E-21 (horror pass, 2026-11-02). Owner request: "those 8 can hunt, by using their senses, and remaining
// zombies can follow them, and don't stop by 20, if you can add more".
using System;
using System.Collections.Generic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI.Logic
{
    /// <summary>
    /// The horde (contract v1.4). Eight zombies hunt with their own senses: the five story hunters (Orderly, Patroller,
    /// Researcher, Guard, Porter) and three new ones. Every other new zombie is a follower: it keeps its place in one hunter's
    /// pack, and when that hunter investigates or gives chase, the pack goes with it. Followers are deaf and nearly blind; they
    /// catch the player only by reaching them. A follower is in play exactly while its hunter is; a new hunter in its own act.
    /// Seven story zombies + 21 here = 28 in all.
    /// </summary>
    public static class HordeLayout
    {
        public readonly struct Member
        {
            public readonly int Id;
            /// <summary>The hunter this zombie follows; 0 for a hunter.</summary>
            public readonly int Leader;
            /// <summary>A new hunter's act; None for a follower (it is in play while its hunter is).</summary>
            public readonly ActId Act;
            public readonly Vector3 Spawn;
            /// <summary>Which way it faces when the act starts (a dormant hunter faces into its corner, "feeding").</summary>
            public readonly float Yaw;
            /// <summary>A hunter that walks between PatrolA and PatrolB; otherwise it waits and listens.</summary>
            public readonly bool Patrols;
            public readonly Vector3 PatrolA, PatrolB;
            /// <summary>A follower's place in its pack, in the hunter's frame (x right, z forward).</summary>
            public readonly Vector3 Offset;

            public Member(int id, int leader, ActId act, Vector3 spawn, float yaw, bool patrols, Vector3 patrolA, Vector3 patrolB, Vector3 offset)
            {
                Id = id; Leader = leader; Act = act; Spawn = spawn; Yaw = yaw; Patrols = patrols; PatrolA = patrolA; PatrolB = patrolB; Offset = offset;
            }

            public bool Hunter => Leader == 0;
        }

        /// <summary>The story zombies that hunt by their own senses (the Walker is scripted, the Final Patient chained).</summary>
        public static readonly int[] StoryHunters = { ZombieIds.Orderly, ZombieIds.Patroller, ZombieIds.Researcher, ZombieIds.Guard, ZombieIds.Porter };

        private static readonly Member[] MemberArray =
        {
            // Floor 3. The Walker's shadows pass the window of B-7 with it (it is scripted and cannot catch; they follow it).
            F(101, ZombieIds.Walker, -1.65f, 2.6f, 90, -.6f, -.9f),
            F(102, ZombieIds.Walker, -1.65f, 4.15f, 90, .6f, -1.5f),
            // The Orderly's two, asleep with it in the supply room.
            F(103, ZombieIds.Orderly, 5.6f, 0f, 0, -.7f, -.9f),
            F(104, ZombieIds.Orderly, 7.3f, -.4f, 0, .7f, -.9f),
            // The Patroller's two, waiting in the side room at the north end of its beat.
            F(105, ZombieIds.Patroller, 10f, 6.6f, 180, -.6f, -1f),
            F(106, ZombieIds.Patroller, 11f, 6.6f, 180, .6f, -1.6f),
            // Act 4: a pack walks the corridor behind the player toward the vestibule.
            H(107, ActId.Exit, 3f, 3.25f, 90, new Vector3(3f, 0, 3.25f), new Vector3(9.5f, 0, 3.25f)),
            F(108, 107, 2.2f, 2.7f, 90, -.7f, -1f),
            F(109, 107, 2.2f, 3.8f, 90, .7f, -1f),
            F(110, 107, 1.2f, 3.25f, 90, 0f, -2f),

            // Floor 2: Lab B is a nest. The Researcher's two, and a hunter with two more in the doorway corner.
            F(111, ZombieIds.Researcher, 35.6f, -.9f, 270, -.6f, -.9f),
            F(112, ZombieIds.Researcher, 33.8f, .6f, 270, .6f, -.9f),
            H(113, ActId.Laboratory, 36.4f, 1.3f, 90),
            F(114, 113, 36.3f, .3f, 90, -.7f, -.9f),
            F(115, 113, 35.6f, 1.4f, 90, .7f, -.9f),

            // Floor 1: the Guard's two on its beat, one by the Porter behind the desk, and a hunter with two more in the
            // lobby's north-east corner, near the main doors.
            F(116, ZombieIds.Guard, 69.6f, 6.6f, 180, -.6f, -.9f),
            F(117, ZombieIds.Guard, 70.8f, 6.6f, 180, .6f, -.9f),
            F(118, ZombieIds.Porter, 63.2f, -1.5f, 0, -.7f, -.9f),
            H(119, ActId.Lobby, 72.8f, 6.4f, 0),
            F(120, 119, 72f, 5.9f, 0, -.7f, -.9f),
            F(121, 119, 73.5f, 5.8f, 0, .7f, -.9f),
        };

        public static readonly IReadOnlyList<Member> Members = Array.AsReadOnly(MemberArray);

        public static bool TryGet(int id, out Member member)
        {
            int i = id - ZombieIds.HordeFirst;
            if (i >= 0 && i < MemberArray.Length && MemberArray[i].Id == id) { member = MemberArray[i]; return true; }
            member = default;
            return false;
        }

        /// <summary>Where a follower should stand: its offset in the hunter's frame.</summary>
        public static Vector3 Slot(in Member follower, Vector3 hunter, Vector3 hunterForward)
        {
            hunterForward.y = 0;
            if (hunterForward.sqrMagnitude < 1e-6f) hunterForward = Vector3.forward;
            hunterForward = hunterForward.normalized;
            Vector3 right = new Vector3(hunterForward.z, 0, -hunterForward.x);
            return hunter + right * follower.Offset.x + hunterForward * follower.Offset.z;
        }

        private static Member H(int id, ActId act, float x, float z, float yaw) =>
            new Member(id, 0, act, new Vector3(x, 0, z), yaw, false, default, default, default);

        private static Member H(int id, ActId act, float x, float z, float yaw, Vector3 patrolA, Vector3 patrolB) =>
            new Member(id, 0, act, new Vector3(x, 0, z), yaw, true, patrolA, patrolB, default);

        private static Member F(int id, int leader, float x, float z, float yaw, float offsetRight, float offsetForward) =>
            new Member(id, leader, ActId.None, new Vector3(x, 0, z), yaw, false, default, default, new Vector3(offsetRight, 0, offsetForward));
    }
}
