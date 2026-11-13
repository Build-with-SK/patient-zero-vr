using System;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI.Logic
{
    // Follow (v1.4): a horde follower keeping its place in its hunter's pack.
    public enum ZombieMode { Dormant, Idle, Patrol, Scripted, Restrained, Passive, Follow }
    [Flags]
    public enum ZombieSignals { None = 0, Sniff = 1, Snarl = 2, Groan = 4, Caught = 8, AlertChanged = 16 }

    /// <summary>Snapshot supplied by the AI shell through IPlayerQuery; no live player transform is retained.</summary>
    public readonly struct PerceptionFrame
    {
        public readonly Vector3 Zombie, Forward, Player;
        public readonly float HeadSpeed, HandSpeed;
        public readonly bool LineClear, PathFailed;
        public readonly LightingState Lighting;
        public PerceptionFrame(Vector3 zombie, Vector3 forward, Vector3 player, float headSpeed = 0,
            float handSpeed = 0, bool lineClear = true, bool pathFailed = false, LightingState lighting = LightingState.WardDim)
        { Zombie = zombie; Forward = forward; Player = player; HeadSpeed = headSpeed; HandSpeed = handSpeed; LineClear = lineClear; PathFailed = pathFailed; Lighting = lighting; }
    }

    /// <summary>
    /// Approved alert-state FSM. Monotonic time/path/distraction guards port ZombieBrain@72be6d8.
    /// Two-minute version (owner-approved plan 2026-11-10; Claude integration E-22): every zombie is blind and hunts by sound
    /// alone (sight stays behind <see cref="Sighted"/> for the later lab strain), and a grab needs a player noise in the last
    /// <see cref="FreshNoiseSeconds"/>, so a player who freezes is safe even face to face ("freeze or die").
    /// </summary>
    public sealed class ZombieBrain
    {
        private readonly System.Random random;
        private readonly int id;
        private readonly Vector3 spawn, patrolNorth, patrolSouth;
        /// <summary>E-22: how recent a player noise must be for a zombie in reach to grab. Footsteps come every 0.6 s while moving.</summary>
        public const float FreshNoiseSeconds = 1f;
        /// <summary>E-22: how long the door zombie ignores every sound while it screams in the player's face.</summary>
        public const float ConfrontDeafSeconds = .8f;
        /// <summary>
        /// E-28 (owner playtest 2026-11-13: "none of the zombie is following the gun sound"): a gunshot is heard by every zombie
        /// in reach, a pack's followers too, and each runs (chase speed) to where it was fired, for up to this long, then
        /// searches there. Other sounds are still walked to.
        /// </summary>
        public const float GunshotRushSeconds = 25;
        // v1.4 horde: followers are deaf and go where their hunter goes; the Walker's never chase or grab.
        private readonly bool horde, follower, harmless;
        private Vector3 followSlot;
        private float clock, stateAge, searchAge, lastRetarget = -100, lastPlayerStimulus = -100, chaseHeld = -100;
        private float salience = -1, catchAge, stunRemaining, patrolPause, nextSearchPoint, deafUntil = -100;
        private Vector3 searchCentre;
        private bool returning, southbound = true, released, caught, nearWarning, rushing;
        private ZombieSignals pending;
        public AlertLevel Level { get; private set; }
        public ZombieMode Mode { get; private set; }
        public Vector3 Target { get; private set; }
        public bool HasMoveTarget { get; private set; }
        public bool Reduced { get; set; }
        public bool AssistedSpeed { get; set; }
        /// <summary>E-22: false for every zombie in the two-minute version. True brings back the sight rules (the lab strain).</summary>
        public bool Sighted { get; set; }
        /// <summary>When this zombie last heard (or, if sighted, saw) the player: what its pack's grabs are measured against.</summary>
        public float LastPlayerStimulus => lastPlayerStimulus;
        public float HearingMultiplier => id == ZombieIds.Patroller ? 1.2f : 1;
        public float WalkSpeed => horde ? .85f : id == ZombieIds.Patroller ? .8f : .9f;
        /// <summary>The horde is slower than the story hunters, so a pack strings out behind the one leading it.</summary>
        public float ChaseSpeed => (follower ? 1.3f : horde ? 1.45f : id == ZombieIds.Patroller ? 1.5f : 1.6f) * (Reduced || AssistedSpeed ? .8f : 1);
        public bool IsFollower => follower;
        public bool IsStunned => stunRemaining > 0;
        /// <summary>E-28: running to a gunshot (moves at <see cref="ChaseSpeed"/> although it is only investigating).</summary>
        public bool Rushing => rushing && Level == AlertLevel.Investigating;

        public ZombieBrain(int id, Vector3 spawn, System.Random random, Vector3 patrolNorth = default, Vector3 patrolSouth = default)
        {
            if (!ZombieIds.IsValid(id) || !Hearing.Finite(spawn)) throw new ArgumentException("Invalid zombie setup");
            this.id = id; this.spawn = spawn; this.random = random ?? throw new ArgumentNullException(nameof(random));
            horde = HordeLayout.TryGet(id, out HordeLayout.Member member);
            if (ZombieIds.IsHorde(id) && !horde) throw new ArgumentException("Horde zombie " + id + " is not in HordeLayout");
            follower = horde && !member.Hunter;
            harmless = follower && member.Leader == ZombieIds.Walker;
            followSlot = spawn;
            this.patrolNorth = patrolNorth; this.patrolSouth = patrolSouth;
            Reset(id == ZombieIds.FinalPatient ? ZombieMode.Restrained : ZombieMode.Dormant);
        }
        /// <summary>Called at an act start/restore; clears every transient and catch latch.</summary>
        public void Reset(ZombieMode mode)
        {
            Mode = mode; Level = AlertLevel.Unaware; Target = spawn; HasMoveTarget = false;
            clock = stateAge = searchAge = catchAge = stunRemaining = patrolPause = 0;
            salience = -1; lastRetarget = lastPlayerStimulus = deafUntil = chaseHeld = -100; returning = released = caught = nearWarning = rushing = false;
            pending = ZombieSignals.None; southbound = true;
        }
        public void SetMode(ZombieMode mode)
        {
            Mode = mode; catchAge = 0;
            if (mode == ZombieMode.Passive || mode == ZombieMode.Dormant || mode == ZombieMode.Restrained)
            { HasMoveTarget = false; Change(AlertLevel.Unaware); }
            if (mode == ZombieMode.Patrol) { Target = patrolSouth; southbound = true; HasMoveTarget = true; }
        }
        public void ScriptedMove(Vector3 target)
        { if (!Hearing.Finite(target)) return; Mode = ZombieMode.Scripted; Target = target; HasMoveTarget = true; }
        public void ReleaseRestraint() { released = true; }

        /// <summary>
        /// E-22, the door zombie: it is suddenly right outside the player's door and sniffs about the spot it stands on. For
        /// <see cref="ConfrontDeafSeconds"/> it hears nothing (it is screaming); after that any sound the player makes is heard as
        /// usual, so moving means it attacks, while a player who stays still and quiet is lost when the search runs out.
        /// </summary>
        public void Confront(Vector3 at, float now)
        {
            if (!Hearing.Finite(at) || !Hearing.Finite(now) || now < clock || Mode == ZombieMode.Passive) return;
            Mode = ZombieMode.Idle; caught = returning = false; catchAge = 0;
            lastPlayerStimulus = lastRetarget = chaseHeld = -100; salience = -1;
            deafUntil = now + ConfrontDeafSeconds; Target = at;
            StartSearch();
        }

        /// <summary>
        /// E-22, the one struggle: the player fought this zombie off. It lets go, staggers back stunned, and has to find the
        /// player again by sound, from where the player was.
        /// </summary>
        public void BreakFree(float stunSeconds)
        {
            if (!caught) return;
            caught = false; catchAge = 0; lastPlayerStimulus = chaseHeld = -100;
            if (Mode == ZombieMode.Follow) { Change(AlertLevel.Unaware); HasMoveTarget = false; }
            else StartSearch();
            Stun(stunSeconds);
        }

        /// <summary>
        /// A follower's link to its pack (v1.4), given before each tick: where its place in the pack is now, and whether its
        /// hunter is chasing. A chasing hunter takes the pack with it, straight for what it is chasing.
        /// </summary>
        /// <remarks>E-22: <paramref name="leaderStimulus"/> is when the hunter last heard the player. A follower's grab needs that to
        /// be fresh, as the hunter's own does, so a pack that has run up to a frozen player cannot take them.</remarks>
        public void Follow(bool leaderChasing, Vector3 leaderTarget, Vector3 slot, float now, float leaderStimulus)
        {
            if (!follower || Mode != ZombieMode.Follow || caught || !Hearing.Finite(slot) || !Hearing.Finite(leaderTarget) ||
                !Hearing.Finite(now) || now < clock) return;
            followSlot = slot;
            if (!leaderChasing || harmless) return;
            Target = leaderTarget; searchCentre = Target; chaseHeld = now;
            if (Hearing.Finite(leaderStimulus) && leaderStimulus <= now) lastPlayerStimulus = Math.Max(lastPlayerStimulus, leaderStimulus);
            BeginChase();
        }
        public void Stun(float seconds) { if (Hearing.Finite(seconds)) stunRemaining = Math.Max(stunRemaining, seconds); }

        public bool Hear(in NoiseEvent noise, Vector3 position, float occlusion, float now, bool live)
        {
            bool gunshot = noise.Origin == NoiseOrigin.Player && noise.Source == CueId.RevolverShot;
            if ((follower && !gunshot) || !live || IsStunned || caught || now < deafUntil || Mode == ZombieMode.Passive || Mode == ZombieMode.Scripted ||
                Mode == ZombieMode.Restrained || !Hearing.Finite(now) || now < clock ||
                !Hearing.CanHear(in noise, position, HearingMultiplier, occlusion, now)) return false;
            float strength = Hearing.Salience(in noise, position, HearingMultiplier, occlusion);
            bool thrownImpact = noise.Origin == NoiseOrigin.Player &&
                (noise.Source == CueId.ImpactHeavy || noise.Source == CueId.ImpactLight);
            bool remoteImpact = thrownImpact && WardLayout.HorizontalDistance(position, noise.Position) > 2;
            // E-22: blind, a chasing zombie follows the footsteps of the player it is after, however faint, so a player who keeps
            // moving stays heard (and can be grabbed); jitter hysteresis still guards every other retarget.
            bool sameQuarry = Level == AlertLevel.Chasing && noise.Origin == NoiseOrigin.Player && !remoteImpact &&
                WardLayout.HorizontalDistance(noise.Position, Target) <= 3;
            if (!sameQuarry && Level >= AlertLevel.Investigating && strength <= salience && now - lastRetarget < 1.5f) return false;
            Target = noise.Position; searchCentre = Target; salience = strength; lastRetarget = now;
            // A thrown bottle identifies its landing, not the thrower's live position.
            if (remoteImpact) lastPlayerStimulus = -100;
            else if (noise.Origin == NoiseOrigin.Player) lastPlayerStimulus = now;
            // E-28: a gunshot. A snarl, then a run to where it was fired (a chasing zombie keeps chasing).
            if (gunshot && Level != AlertLevel.Chasing)
            {
                if (Level == AlertLevel.Unaware) { Change(AlertLevel.Suspicious); pending |= ZombieSignals.Snarl; }
                else if (Level == AlertLevel.Searching) Change(AlertLevel.Investigating);
                rushing = true;
                if (Level == AlertLevel.Investigating) { stateAge = 0; HasMoveTarget = true; }
            }
            else if (Level == AlertLevel.Unaware)
            { Change(AlertLevel.Suspicious); pending |= ZombieSignals.Sniff; }
            else if (Level == AlertLevel.Chasing && (noise.Origin == NoiseOrigin.Scripted || remoteImpact))
            { lastPlayerStimulus = -100; Change(AlertLevel.Investigating); }
            else if (Level == AlertLevel.Searching) Change(AlertLevel.Investigating);
            // Nearby player-origin noise escalates only after the initial sniff telegraph.
            if ((Level == AlertLevel.Investigating || Level == AlertLevel.Chasing) && noise.Origin == NoiseOrigin.Player &&
                WardLayout.HorizontalDistance(position, noise.Position) <= 2) BeginChase();
            return true;
        }

        /// <summary>Scheduled shell tick. Inactive states do not consume telegraph/search/catch timers.</summary>
        public ZombieSignals Tick(float now, float delta, bool live, in PerceptionFrame frame)
        {
            if (!Hearing.Finite(now) || !Hearing.Finite(delta) || now < clock || delta < 0 ||
                !Hearing.Finite(frame.Zombie) || !Hearing.Finite(frame.Player)) return ZombieSignals.None;
            clock = now;
            if (!live || caught || Mode == ZombieMode.Passive) return ZombieSignals.None;
            if (stunRemaining > 0) { stunRemaining = Math.Max(0, stunRemaining - delta); return ZombieSignals.None; }
            if (Mode == ZombieMode.Scripted) return Flush();
            if (Mode == ZombieMode.Restrained)
            {
                HasMoveTarget = false; Target = spawn; // The root never follows the player beyond the chain.
                float distance = WardLayout.HorizontalDistance(spawn, frame.Player);
                if (distance <= 2 && !nearWarning) { nearWarning = true; pending |= ZombieSignals.Snarl; }
                Catch(delta, released && distance <= WardLayout.FinalPatientReach && frame.LineClear && Math.Abs(spawn.y - frame.Player.y) <= 1.2f);
                return Flush();
            }
            bool sees = Sighted && !harmless && CanSee(in frame);
            if (sees && (Level == AlertLevel.Investigating || Level == AlertLevel.Searching || Level == AlertLevel.Chasing))
            { Target = frame.Player; lastPlayerStimulus = now; BeginChase(); }
            else if (sees && Level == AlertLevel.Unaware)
            { Target = frame.Player; searchCentre = Target; lastPlayerStimulus = now; Change(AlertLevel.Suspicious); pending |= ZombieSignals.Sniff; }
            stateAge += delta;
            switch (Level)
            {
                case AlertLevel.Unaware: TickPatrol(delta, in frame); break;
                case AlertLevel.Suspicious:
                    HasMoveTarget = false;
                    if (stateAge >= (Reduced ? 1.2f : .6f)) { Change(AlertLevel.Investigating); HasMoveTarget = true; }
                    break;
                case AlertLevel.Investigating:
                    HasMoveTarget = true;
                    if (frame.PathFailed || stateAge >= (rushing ? GunshotRushSeconds : 10)) StartSearch();
                    else if (WardLayout.HorizontalDistance(frame.Zombie, Target) <= .35f) StartSearch();
                    else if (now - lastPlayerStimulus <= 1 && WardLayout.HorizontalDistance(frame.Zombie, Target) <= 2) BeginChase();
                    break;
                case AlertLevel.Searching:
                    searchAge += delta;
                    if (searchAge >= 5)
                    { pending |= ZombieSignals.Groan; Change(AlertLevel.Unaware); Target = spawn; HasMoveTarget = true; returning = true; }
                    else if (searchAge >= nextSearchPoint)
                    {
                        nextSearchPoint += 1.5f;
                        double angle = random.NextDouble() * Math.PI * 2;
                        Target = searchCentre + new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle)) * .8f;
                        HasMoveTarget = true;
                    }
                    break;
                case AlertLevel.Chasing:
                    HasMoveTarget = true;
                    // A follower runs with its hunter (Follow keeps chaseHeld current), so a pack chase lasts as long as the hunter's.
                    if (frame.PathFailed || now - Math.Max(lastPlayerStimulus, chaseHeld) >= 3) { StartSearch(); catchAge = 0; }
                    else Catch(delta, !harmless && now - lastPlayerStimulus <= FreshNoiseSeconds && frame.LineClear &&
                        Math.Abs(frame.Zombie.y - frame.Player.y) <= 1.2f &&
                        WardLayout.HorizontalDistance(frame.Zombie, frame.Player) <= (Reduced ? .6f : .8f));
                    break;
            }
            return Flush();
        }
        private bool CanSee(in PerceptionFrame frame)
        {
            if (Reduced || id == ZombieIds.Patroller || frame.Lighting == LightingState.Blackout || !frame.LineClear ||
                !(frame.HeadSpeed > .6f || frame.HandSpeed > 1.2f)) return false;
            float range = frame.Lighting == LightingState.WardDim ? 6 : frame.Lighting == LightingState.EmergencyRed ? 3 : 0;
            // The horde's hunters see two thirds as far; a follower notices only what is right in front of it.
            if (follower) range = Math.Min(range, 1.8f);
            else if (horde) range *= .67f;
            Vector3 offset = frame.Player - frame.Zombie; offset.y = 0;
            Vector3 facing = frame.Forward; facing.y = 0;
            return range > 0 && offset.sqrMagnitude <= range * range &&
                (offset.sqrMagnitude < .0001f || Vector3.Angle(facing, offset) <= 30);
        }
        private void TickPatrol(float delta, in PerceptionFrame frame)
        {
            if (Mode == ZombieMode.Follow)
            {
                // Keep to the pack: walk to the slot once it is a step away, stop when there.
                returning = false;
                Target = followSlot;
                HasMoveTarget = WardLayout.HorizontalDistance(frame.Zombie, followSlot) > (HasMoveTarget ? .35f : .9f);
                return;
            }
            if (returning)
            {
                if (WardLayout.HorizontalDistance(frame.Zombie, spawn) > .35f) return;
                returning = false; HasMoveTarget = false;
            }
            if (Mode != ZombieMode.Patrol) { HasMoveTarget = returning; return; }
            if (patrolPause > 0)
            { patrolPause = Math.Max(0, patrolPause - delta); HasMoveTarget = false; return; }
            if (HasMoveTarget && WardLayout.HorizontalDistance(frame.Zombie, Target) <= .35f)
            { patrolPause = southbound ? 3 : 4; southbound = !southbound; HasMoveTarget = false; }
            else { Target = southbound ? patrolSouth : patrolNorth; HasMoveTarget = true; }
        }
        private void Catch(float delta, bool inRange)
        {
            catchAge = inRange ? catchAge + delta : 0;
            if (!caught && catchAge >= (Reduced ? .5f : .25f))
            { caught = true; HasMoveTarget = false; pending |= ZombieSignals.Caught; }
        }
        private void BeginChase()
        {
            if (Level == AlertLevel.Chasing) return;
            pending |= ZombieSignals.Snarl; Change(AlertLevel.Chasing); HasMoveTarget = true;
        }
        private void StartSearch()
        {
            searchCentre = Target; searchAge = 0; nextSearchPoint = 1.5f;
            Change(AlertLevel.Searching); pending |= ZombieSignals.Sniff; HasMoveTarget = false;
        }
        private void Change(AlertLevel level)
        {
            if (Level == level) return;
            Level = level; stateAge = catchAge = 0; pending |= ZombieSignals.AlertChanged;
            if (level != AlertLevel.Suspicious && level != AlertLevel.Investigating) rushing = false;
        }
        private ZombieSignals Flush() { ZombieSignals result = pending; pending = ZombieSignals.None; return result; }
    }
}
