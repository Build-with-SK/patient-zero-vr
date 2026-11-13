// Claude integration E-21 (horror pass, 2026-11-02): the v1.4 horde.
using System.Collections.Generic;
using NUnit.Framework;
using PatientZero.AI.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI.Tests
{
    public sealed class HordeTests
    {
        private static PerceptionFrame Frame(Vector3 zombie, Vector3 player, float speed = 1) =>
            new PerceptionFrame(zombie, Vector3.right, player, speed, 0, true, false, LightingState.WardDim);

        private static bool InRoom(Vector3 p)
        {
            foreach (RoomRect room in WardLayout.Rooms)
            {
                if (room.Contains(p, .3f)) return true;
            }

            return false;
        }

        private static ZombieBrain Follower(int id, Vector3 spawn)
        {
            var brain = new ZombieBrain(id, spawn, new System.Random(7));
            brain.SetMode(ZombieMode.Follow);
            return brain;
        }

        [Test]
        public void LayoutHasEightHuntersAndEveryFollowerHasOne()
        {
            Assert.AreEqual(ZombieIds.HordeCount, HordeLayout.Members.Count);
            var ids = new HashSet<int>();
            int hunters = HordeLayout.StoryHunters.Length;
            for (int i = 0; i < HordeLayout.Members.Count; i++)
            {
                HordeLayout.Member member = HordeLayout.Members[i];
                Assert.AreEqual(ZombieIds.HordeFirst + i, member.Id, "ids are dense and in order (TryGet indexes them)");
                Assert.IsTrue(ZombieIds.IsHorde(member.Id) && ZombieIds.IsValid(member.Id));
                Assert.IsTrue(ids.Add(member.Id));
                Assert.IsTrue(HordeLayout.TryGet(member.Id, out HordeLayout.Member found) && found.Id == member.Id);
                Assert.IsTrue(InRoom(member.Spawn), member.Id + " spawns less than 0.3 m from a wall, or outside the ward");
                if (member.Patrols) Assert.IsTrue(InRoom(member.PatrolA) && InRoom(member.PatrolB), member.Id + " patrols through a wall");
                if (member.Hunter)
                {
                    hunters++;
                    Assert.AreNotEqual(ActId.None, member.Act);
                    continue;
                }

                Assert.AreEqual(ActId.None, member.Act);
                bool storyLeader = member.Leader >= ZombieIds.Walker && member.Leader <= ZombieIds.Count;
                bool hordeLeader = HordeLayout.TryGet(member.Leader, out HordeLayout.Member leader) && leader.Hunter;
                Assert.IsTrue(storyLeader || hordeLeader, member.Id + " follows " + member.Leader);
                Assert.AreNotEqual(ZombieIds.FinalPatient, member.Leader, "the chained patient leads no pack");
            }

            Assert.AreEqual(8, hunters, "the owner asked for 8 hunters");
            Assert.IsFalse(HordeLayout.TryGet(ZombieIds.HordeFirst + ZombieIds.HordeCount, out _));
            Assert.IsFalse(ZombieIds.IsValid(0) || ZombieIds.IsValid(ZombieIds.Count + 1) || ZombieIds.IsValid(ZombieIds.HordeFirst - 1));
        }

        [Test]
        public void SlotIsInTheHuntersFrame()
        {
            Assert.IsTrue(HordeLayout.TryGet(108, out HordeLayout.Member follower));
            Vector3 slot = HordeLayout.Slot(in follower, new Vector3(10, 0, 0), Vector3.forward);
            Assert.AreEqual(10 + follower.Offset.x, slot.x, 1e-4f);
            Assert.AreEqual(follower.Offset.z, slot.z, 1e-4f);
            Vector3 turned = HordeLayout.Slot(in follower, Vector3.zero, Vector3.right);
            Assert.AreEqual(follower.Offset.z, turned.x, 1e-4f, "forward is +x after a quarter turn");
            Assert.AreEqual(-follower.Offset.x, turned.z, 1e-4f);
        }

        [Test]
        public void FollowerIsDeafAndKeepsToItsSlot()
        {
            var brain = Follower(103, new Vector3(5.6f, 0, 0));
            var noise = new NoiseEvent(new Vector3(6, 0, 0), 8, CueId.ImpactHeavy, NoiseOrigin.Player, 0);
            Assert.IsFalse(brain.Hear(in noise, new Vector3(5.6f, 0, 0), 1, 0, true));

            Vector3 slot = new Vector3(8, 0, 0);
            brain.Follow(false, Vector3.zero, slot, 0, -100);
            brain.Tick(.1f, .1f, true, Frame(new Vector3(5.6f, 0, 0), new Vector3(20, 0, 20)));
            Assert.IsTrue(brain.HasMoveTarget);
            Assert.AreEqual(slot, brain.Target);
            brain.Tick(.2f, .1f, true, Frame(new Vector3(7.8f, 0, 0), new Vector3(20, 0, 20)));
            Assert.IsFalse(brain.HasMoveTarget, "it stops once it is in its place");
            Assert.AreEqual(AlertLevel.Unaware, brain.Level);
        }

        [Test]
        public void FollowerRunsToAGunshot()
        {
            // E-28 (owner: "none of the zombie is following the gun sound"): deaf to everything else, a follower hears the revolver.
            var brain = Follower(103, new Vector3(5.6f, 0, 0));
            Vector3 shot = new Vector3(15, 0, 0), slot = new Vector3(8, 0, 0), player = new Vector3(20, 0, 20);
            var noise = new NoiseEvent(shot, 20, CueId.RevolverShot, NoiseOrigin.Player, 0);
            Assert.IsTrue(brain.Hear(in noise, new Vector3(5.6f, 0, 0), 1, 0, true));
            Assert.AreEqual(AlertLevel.Suspicious, brain.Level);
            brain.Follow(false, Vector3.zero, slot, .1f, -100);
            ZombieSignals signals = brain.Tick(.1f, .1f, true, Frame(new Vector3(5.6f, 0, 0), player));
            Assert.AreNotEqual(ZombieSignals.None, signals & ZombieSignals.Snarl, "a gunshot is answered with a snarl");
            brain.Follow(false, Vector3.zero, slot, .7f, -100);
            brain.Tick(.7f, .6f, true, Frame(new Vector3(5.6f, 0, 0), player));
            Assert.AreEqual(AlertLevel.Investigating, brain.Level);
            Assert.IsTrue(brain.Rushing, "it runs, not walks");
            Assert.IsTrue(brain.HasMoveTarget);
            Assert.AreEqual(shot, brain.Target, "to the shot, not to its place in the pack");
            brain.Follow(false, Vector3.zero, slot, 13, -100);
            brain.Tick(13, 12.3f, true, Frame(new Vector3(9, 0, 0), player));
            Assert.AreEqual(AlertLevel.Investigating, brain.Level, "still going after the ten seconds an ordinary sound gets");
            brain.Follow(false, Vector3.zero, slot, 26, -100);
            brain.Tick(26, 13, true, Frame(new Vector3(12, 0, 0), player));
            Assert.AreEqual(AlertLevel.Searching, brain.Level, "it gives up after GunshotRushSeconds");
            Assert.IsFalse(brain.Rushing);
        }

        [Test]
        public void FollowerChasesWithItsHunterAndCatches()
        {
            var brain = Follower(103, Vector3.zero);
            Vector3 player = new Vector3(.5f, 0, 0);
            brain.Follow(true, player, Vector3.zero, 0, 0);
            ZombieSignals signals = brain.Tick(.1f, .1f, true, Frame(Vector3.zero, player, 0));
            Assert.AreEqual(AlertLevel.Chasing, brain.Level);
            Assert.AreNotEqual(ZombieSignals.None, signals & ZombieSignals.Snarl, "a chase is always telegraphed");
            Assert.AreEqual(player, brain.Target);
            brain.Follow(true, player, Vector3.zero, .2f, .2f);
            signals = brain.Tick(.4f, .3f, true, Frame(Vector3.zero, player, 0));
            Assert.AreNotEqual(ZombieSignals.None, signals & ZombieSignals.Caught);
            Assert.Less(brain.ChaseSpeed, new ZombieBrain(ZombieIds.Orderly, Vector3.zero, new System.Random(1)).ChaseSpeed);
        }

        [Test]
        public void FollowerGivesUpWhenItsHunterStopsAndReturnsToThePack()
        {
            var brain = Follower(104, Vector3.zero);
            Vector3 far = new Vector3(15, 0, 0);
            brain.Follow(true, far, Vector3.zero, 0, 0);
            brain.Tick(.1f, .1f, true, Frame(Vector3.zero, new Vector3(30, 0, 30), 0));
            Assert.AreEqual(AlertLevel.Chasing, brain.Level);
            float t = .1f;
            for (int i = 0; i < 120; i++)
            {
                t += .1f;
                brain.Follow(false, far, new Vector3(2, 0, 0), t, 0);
                brain.Tick(t, .1f, true, Frame(new Vector3(9, 0, 0), new Vector3(30, 0, 30), 0));
            }

            Assert.AreEqual(AlertLevel.Unaware, brain.Level);
            Assert.AreEqual(new Vector3(2, 0, 0), brain.Target, "back to its place in the pack, not to its spawn");
        }

        [Test]
        public void TheWalkersShadowsCannotSeeChaseOrCatch()
        {
            var brain = Follower(101, Vector3.zero);
            brain.Follow(true, new Vector3(.3f, 0, 0), Vector3.zero, 0, 0);
            for (int i = 1; i <= 20; i++)
            {
                ZombieSignals signals = brain.Tick(i * .1f, .1f, true, Frame(Vector3.zero, new Vector3(.3f, 0, 0), 2));
                Assert.AreEqual(ZombieSignals.None, signals & ZombieSignals.Caught);
            }

            Assert.AreEqual(AlertLevel.Unaware, brain.Level);
        }

        /// <summary>E-22: a pack that has run up to a player who then freezes cannot take them; its hunter heard nothing fresh.</summary>
        [Test]
        public void APackCannotGrabAFrozenPlayer()
        {
            var brain = Follower(103, Vector3.zero);
            Vector3 player = new Vector3(.5f, 0, 0);
            brain.Follow(true, player, Vector3.zero, 0, 0);
            brain.Tick(.1f, .1f, true, Frame(Vector3.zero, player, 0));
            Assert.AreEqual(AlertLevel.Chasing, brain.Level);
            for (int i = 2; i <= 25; i++)
            {
                float t = i * .1f;
                brain.Follow(true, player, Vector3.zero, t, 0); // the hunter is still chasing; its last sound of the player was at 0
                ZombieSignals signals = brain.Tick(t, .1f, true, Frame(Vector3.zero, player, 0));
                if (t > ZombieBrain.FreshNoiseSeconds) Assert.AreEqual(ZombieSignals.None, signals & ZombieSignals.Caught, "grabbed at " + t);
            }
        }

        /// <summary>E-22: every zombie in the two-minute version is blind, followers included.</summary>
        [Test]
        public void ABlindFollowerNeverNoticesAMovingPlayer()
        {
            var brain = Follower(105, Vector3.zero);
            for (int i = 1; i <= 20; i++) brain.Tick(i * .1f, .1f, true, Frame(Vector3.zero, new Vector3(1, 0, 0), 2));
            Assert.AreEqual(AlertLevel.Unaware, brain.Level);
        }

        [Test]
        public void FollowerSeesOnlyAtArmsLength()
        {
            var brain = Follower(105, Vector3.zero);
            brain.Sighted = true; // the later lab strain
            brain.Tick(.1f, .1f, true, Frame(Vector3.zero, new Vector3(3, 0, 0), 2));
            Assert.AreEqual(AlertLevel.Unaware, brain.Level, "a story hunter would see a moving player at 3 m; a follower does not");
            brain.Tick(.2f, .1f, true, Frame(Vector3.zero, new Vector3(1.5f, 0, 0), 2));
            Assert.AreEqual(AlertLevel.Suspicious, brain.Level);
        }

        [Test]
        public void HordeHunterHearsAndPatrolsLikeAStoryHunter()
        {
            Assert.IsTrue(HordeLayout.TryGet(107, out HordeLayout.Member hunter) && hunter.Hunter && hunter.Patrols);
            var brain = new ZombieBrain(107, hunter.Spawn, new System.Random(3), hunter.PatrolA, hunter.PatrolB);
            Assert.IsFalse(brain.IsFollower);
            var noise = new NoiseEvent(hunter.Spawn + Vector3.right * 2, 6, CueId.ImpactHeavy, NoiseOrigin.Player, 0);
            Assert.IsTrue(brain.Hear(in noise, hunter.Spawn, 1, 0, true));
            Assert.AreEqual(AlertLevel.Suspicious, brain.Level);
            Assert.Throws<System.ArgumentException>(() => new ZombieBrain(ZombieIds.HordeFirst + ZombieIds.HordeCount, Vector3.zero, new System.Random(1)));
        }
    }
}
