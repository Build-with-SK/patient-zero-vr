// UNVERIFIED IN EDITOR
using PatientZero.AI.Logic;
using PatientZero.Core;
using Unity.Profiling;
using UnityEngine;

namespace PatientZero.AI
{
    /// <summary>
    /// Ward zombie shell: 10 Hz phased AI, contract-only player queries, cue-before-chase ordering.
    /// v1.4 (E-21): a horde member takes its spawn, facing and beat from HordeLayout. A horde hunter is in play in its own act;
    /// a follower is in play exactly while its Leader is in play and in view, and keeps its place in the Leader's pack.
    /// Two-minute version (owner-approved plan 2026-11-10; Claude integration E-22): the Walker is the door zombie the director
    /// confronts the player with, and its followers stay out of play. A zombie that grabbed the player lets go, stunned and
    /// pushed back, when the player wins the struggle.
    /// E-25 (the revolver, owner's plan step 4): a headshot kills it (it falls and stays down until the player's next life); a
    /// body shot staggers it. The shot's own noise has already told it where the shot came from.
    /// E-29 (owner's playtest: zombies vanished and appeared after the plug went in): it never comes into play or leaves it in
    /// the player's sight (ZombiePresence). Told to leave while the player could see it, it stops, hears nothing and catches no
    /// one, and goes once they cannot see it. Told to appear where the player could see, it waits until they cannot. A new
    /// life or run (an act that starts in the bed or from a restore) still swaps every zombie at once.
    /// </summary>
    [RequireComponent(typeof(ZombieNavigation))]
    public sealed class ZombieAgentShell : MonoBehaviour
    {
        public int ZombieId = ZombieIds.Orderly;
        public LayerMask SightBlockers = 1;
        // Door leaves only. TD/A must assign the approved shared door layer at integration.
        // No door layer exists in v1.1, so walls and impact props do not attenuate hearing.
        public LayerMask HearingOccluders = 0;
        public Transform Head, FootEmitter;
        public ZombieAnimationController Animation;
        [Tooltip("v1.4 horde follower: the hunter whose pack it keeps to (set at compose). Empty for every other zombie.")]
        public ZombieAgentShell Leader;
        private ZombieBrain brain;
        private bool horde, formed;
        private HordeLayout.Member member;
        private ZombieNavigation navigation;
        private GameState state = GameState.Boot;
        private LightingState lighting = LightingState.Awakening;
        private ExperienceSettings settings = ExperienceSettings.Defaults;
        private float nextTick, lastTick;
        private bool active, holdingPlayer, dead;
        // E-29: leaving = in play, standing still, until the player cannot see it; entering = out of play until the player cannot
        // see enterAt, where it will appear. snapFrame is the frame of the last act start that swaps zombies at once.
        private bool leaving, entering;
        private Vector3 enterAt;
        private int snapFrame = -10;
        private float nextPresence;
        private static readonly RaycastHit[] SightHits = new RaycastHit[8];
        private CueHandle breathing, shuffling;
        /// <summary>E-22: how long a zombie the player fought off stays stunned, and how far it staggers back.</summary>
        public const float BreakFreeStunSeconds = 3, BreakFreePushMeters = 1.2f;
        /// <summary>E-25: how long a body shot staggers it.</summary>
        public const float ShotStaggerSeconds = 1.2f;
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("PatientZero.AI.Tick");
        public ZombieBrain Brain => brain;
        /// <summary>In play this act (its simulation runs).</summary>
        public bool Active => active;
        /// <summary>In play and in view (the Walker stays out of view until its window pass).</summary>
        public bool Shown { get; private set; }
        /// <summary>E-25: shot in the head this life. It lies where it fell, in view, and nothing commands it until the player's next life.</summary>
        public bool Dead => dead;
        /// <summary>E-29: told to leave play while the player could see it; it stands still until they cannot.</summary>
        public bool Leaving => leaving;
        /// <summary>E-29: told to come into play where the player could see; it appears once they cannot.</summary>
        public bool Entering => entering;
        private void Awake()
        {
            navigation = GetComponent<ZombieNavigation>();
            horde = HordeLayout.TryGet(ZombieId, out member);
            Vector3 spawn = Spawn();
            // v1.3 (E-19): the lobby guard walks its own line; every other patroller uses the ward's. v1.4: a horde hunter its own.
            bool guard = ZombieId == ZombieIds.Guard;
            brain = new ZombieBrain(ZombieId, spawn, new System.Random(1200 + ZombieId),
                horde ? member.PatrolA : guard ? WardLayout.Z6_PatrolNorth.Position : WardLayout.Z3_PatrolNorth.Position,
                horde ? member.PatrolB : guard ? WardLayout.Z6_PatrolSouth.Position : WardLayout.Z3_PatrolSouth.Position);
            lastTick = Time.time; nextTick = Time.time + ZombieId * .017f;
        }
        private void OnEnable()
        {
            GameEvents.Noise.Subscribe(Hear); GameEvents.ActStarted.Subscribe(ActStarted);
            GameEvents.StateChanged.Subscribe(StateChanged); GameEvents.LightingChanged.Subscribe(LightingChanged);
            GameEvents.SettingsChanged.Subscribe(SettingsChanged);
            if (Services<ISettingsProvider>.TryGet(out var provider)) settings = provider.Current;
        }
        private void OnDisable()
        {
            GameEvents.Noise.Unsubscribe(Hear); GameEvents.ActStarted.Unsubscribe(ActStarted);
            GameEvents.StateChanged.Unsubscribe(StateChanged); GameEvents.LightingChanged.Unsubscribe(LightingChanged);
            GameEvents.SettingsChanged.Unsubscribe(SettingsChanged); StopLoops(); if (navigation) navigation.Stop();
        }
        private Vector3 Spawn()
        {
            if (horde) return member.Spawn;
            switch (ZombieId)
            {
                case ZombieIds.Walker: return WardLayout.Z1_WalkFrom.Position;
                case ZombieIds.Orderly: return WardLayout.Z2_Dormant.Position;
                case ZombieIds.Patroller: return WardLayout.Z3_PatrolNorth.Position;
                case ZombieIds.Researcher: return WardLayout.Z5_Dormant.Position;
                case ZombieIds.Guard: return WardLayout.Z6_PatrolNorth.Position;
                case ZombieIds.Porter: return WardLayout.Z7_Dormant.Position;
                default: return WardLayout.Z4_Gurney.Position;
            }
        }
        private void ActStarted(ActStarted e)
        {
            state = GameState.Playing; // Ward can load after Bootstrap's Playing notification.
            // E-25: a zombie the player killed stays down for the rest of this life; a new life or a new run (both begin in the
            // bed, Awakening) brings it back.
            if (dead && !e.IsRestore && e.Act != ActId.Awakening) return;
            // E-29: a new life or run starts in the bed (or from a restore), where the swap is not seen: it happens at once.
            // Otherwise a zombie of the last act that the player can see stays where it is until they cannot.
            bool atOnce = e.IsRestore || e.Act == ActId.Awakening;
            if (atOnce) snapFrame = Time.frameCount;
            bool inPlay = horde ? member.Hunter && member.Act == e.Act : ZombieIds.ActOf(ZombieId) == e.Act;
            if (!atOnce && !inPlay && (leaving || (active && Shown && PlayerCouldSee(transform.position)))) { BeginLeave(); return; }
            dead = false; leaving = false;
            enterAt = Spawn();
            // v1.4: a follower is never in play at the start of an act; it joins when its leader shows (Update).
            // E-29: one whose spawn the player can see is held back until they cannot (Update).
            active = inPlay && (atOnce || !PlayerCouldSee(enterAt));
            entering = inPlay && !active;
            StopLoops(); holdingPlayer = false; // v1.3: two zombies share the lobby lastTick = Time.time; nextTick = Time.time + ZombieId * .017f;
            brain.Reset(ZombieId == 4 ? ZombieMode.Restrained : ZombieMode.Dormant);
            formed = false;
            if (navigation) { navigation.SetSimulationActive(active); if (active) navigation.Warp(Spawn()); }
            if (horde) transform.rotation = Quaternion.Euler(0, member.Yaw, 0);
            if (Animation) Animation.ResetPose();
            Shown = active && ZombieId != 1;
            if (Animation) Animation.SetVisible(Shown);
            if (Shown) StartBreathing();
            if (inPlay && horde && member.Patrols) brain.SetMode(ZombieMode.Patrol);
        }
        /// <summary>A follower comes into play with its leader and leaves with it: a pack is never split across acts.</summary>
        private void TrackLeader()
        {
            if (dead || leaving) return; // E-29: a follower slipping away goes when the player cannot see it (UpdatePresence)
            // E-22: the Walker is now the lone door zombie; its old window-pass pack stays out of play.
            // E-25: a pack whose hunter was shot stays in play where it stands.
            bool join = (Leader.active || Leader.dead) && Leader.Shown && Leader.ZombieId != ZombieIds.Walker;
            if (join == (active || entering)) return;
            if (join)
            {
                enterAt = Spawn();
                if (SwapsAtOnce || !PlayerCouldSee(enterAt)) JoinPack(); else entering = true;
                return;
            }
            if (entering) { entering = false; return; }
            if (!SwapsAtOnce && Shown && PlayerCouldSee(transform.position)) BeginLeave(); else LeavePack();
        }
        private void JoinPack()
        {
            active = true; entering = false; formed = false;
            StopLoops(); brain.Reset(ZombieMode.Dormant);
            navigation.SetSimulationActive(true);
            navigation.Warp(enterAt); transform.rotation = Quaternion.Euler(0, member.Yaw, 0);
            brain.SetMode(ZombieMode.Follow); lastTick = Time.time; nextTick = Time.time + ZombieId % 10 * .01f;
            if (Animation) { Animation.ResetPose(); Animation.SetVisible(true); }
            Shown = true; // followers do not breathe: a pack is heard by its shuffling, and the voice budget stays with the hunters
        }
        private void LeavePack()
        {
            active = false; leaving = entering = false; formed = false;
            StopLoops(); brain.Reset(ZombieMode.Dormant);
            navigation.SetSimulationActive(false);
            if (Animation) { Animation.ResetPose(); Animation.SetVisible(false); }
            Shown = false;
        }

        /// <summary>E-29: true in the frames an act started from the bed or a restore, when every swap happens at once.</summary>
        private bool SwapsAtOnce => Time.frameCount - snapFrame <= 2;

        /// <summary>
        /// E-29: whether the player could see a zombie standing at <paramref name="feet"/>: its chest or head inside the cone they
        /// face (ZombiePresence) with nothing on <see cref="SightBlockers"/> between. Hits within ZombiePresence.NearMeters of the
        /// head (the player's own hands, gun and props) and on this zombie's own body do not block. No player: nobody sees.
        /// </summary>
        private bool PlayerCouldSee(Vector3 feet)
        {
            if (!Services<IPlayerQuery>.TryGet(out var player)) return false;
            Vector3 head = player.HeadPosition, forward = player.HeadForward;
            return SightLine(head, forward, ZombiePresence.Chest(feet)) || SightLine(head, forward, ZombiePresence.Head(feet));
        }
        private bool SightLine(Vector3 head, Vector3 forward, Vector3 point)
        {
            if (!ZombiePresence.InSightCone(head, forward, point)) return false;
            Vector3 to = point - head;
            float distance = to.magnitude;
            if (distance <= ZombiePresence.NearMeters) return true;
            int hits = Physics.RaycastNonAlloc(head, to / distance, SightHits, distance, SightBlockers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                RaycastHit hit = SightHits[i];
                if (hit.distance <= ZombiePresence.NearMeters || hit.collider.transform.IsChildOf(transform)) continue;
                return false;
            }
            return true;
        }
        /// <summary>E-29: stand still and stop hunting; <see cref="UpdatePresence"/> takes it out of play once the player cannot see it.</summary>
        private void BeginLeave()
        {
            if (leaving) return;
            leaving = true; entering = false; holdingPlayer = false;
            brain.SetMode(ZombieMode.Dormant);             // stands down (its alert drops to Unaware, published on the next tick)
            brain.ScriptedMove(transform.position);        // scripted: it hears nothing and catches no one
            navigation.Stop();
            nextPresence = Time.time + ZombiePresence.CheckSeconds;
        }
        /// <summary>E-29: finish a deferred leave or entrance once the player cannot see the spot.</summary>
        private void UpdatePresence()
        {
            nextPresence = Time.time + ZombiePresence.CheckSeconds;
            if (entering)
            {
                if (PlayerCouldSee(enterAt)) return;
                if (Leader) JoinPack(); else Appear();
                return;
            }
            if (PlayerCouldSee(transform.position)) return;
            if (Leader) { LeavePack(); return; }
            Vanish();
            brain.Reset(ZombieId == ZombieIds.FinalPatient ? ZombieMode.Restrained : ZombieMode.Dormant);
            if (Animation) Animation.ResetPose();
        }
        /// <summary>E-29: come into play at <see cref="enterAt"/> (a held-back act start, or the director showing it).</summary>
        private void Appear()
        {
            active = true; entering = false; leaving = false;
            navigation.SetSimulationActive(true); navigation.Warp(enterAt);
            if (horde) transform.rotation = Quaternion.Euler(0, member.Yaw, 0);
            if (Animation) { Animation.ResetPose(); Animation.SetVisible(true); }
            Shown = true; StartBreathing();
            lastTick = Time.time; nextTick = Time.time + .1f;
        }
        /// <summary>Out of play and out of view where it stands (the director's Hide, or the end of a deferred leave).</summary>
        private void Vanish()
        {
            active = false; leaving = entering = false;
            navigation.SetSimulationActive(false);
            if (Animation) Animation.SetVisible(false);
            Shown = false;
            navigation.Stop(); StopLoops();
            enterAt = transform.position;
        }
        /// <summary>Its place in the pack: where it spawned until the leader first moves off, then behind the leader.</summary>
        private Vector3 Slot()
        {
            Vector3 lead = Leader.transform.position;
            if (!formed && WardLayout.HorizontalDistance(lead, Leader.Spawn()) > 1) formed = true;
            return formed ? HordeLayout.Slot(in member, lead, Leader.transform.forward) : Spawn();
        }
        private void StateChanged(GameStateChanged e)
        {
            state = e.To; lastTick = Time.time; if (!GameStateRules.IsSimulationLive(state)) navigation.Stop();
            // E-22: the player won the struggle (Caught straight back to Playing): every zombie holding them lets go.
            if (holdingPlayer && e.From == GameState.Caught && e.To == GameState.Playing) LetGo();
            else if (e.To != GameState.Caught) holdingPlayer = false;
            // The director stands the story zombies down for the ending; the horde (which it does not command) kneels with them.
            if (horde && active && e.To == GameState.Ending) { SetMode(ZombieMode.Passive); Kneel(); }
        }
        private void LightingChanged(LightingChanged e) => lighting = e.State;
        private void SettingsChanged(SettingsChanged e) { settings = e.Settings; brain.Reduced = settings.Scares == ScareIntensity.Reduced; }
        private void Hear(NoiseEvent noise)
        {
            if (!active || !GameStateRules.IsSimulationLive(state)) return;
            Vector3 ear = Head ? Head.position : transform.position + Vector3.up;
            float occlusion = Hearing.Occlusion(Physics.Linecast(ear, noise.Position, HearingOccluders, QueryTriggerInteraction.Ignore));
            brain.Hear(in noise, transform.position, occlusion, Time.time, true);
        }
        private void Update()
        {
            if ((leaving || entering) && !dead && Time.time >= nextPresence) UpdatePresence();
            if (Leader) TrackLeader();
            if (!active || Time.time < nextTick) return;
            float now = Time.time, delta = Mathf.Max(0, now - lastTick); lastTick = now; nextTick = now + .1f;
            bool live = GameStateRules.IsSimulationLive(state);
            if (!live) { navigation.Stop(); return; }
            if (!Services<IPlayerQuery>.TryGet(out var player)) { navigation.Stop(); return; }
            using (TickMarker.Auto()) TickPlayer(player, now, delta);
        }
        private void TickPlayer(IPlayerQuery player, float now, float delta)
        {
            Vector3 eye = Head ? Head.position : transform.position + Vector3.up;
            bool clear = !Physics.Linecast(eye, player.HeadPosition, SightBlockers, QueryTriggerInteraction.Ignore);
            var frame = new PerceptionFrame(transform.position, transform.forward, player.FloorPosition,
                player.HeadSpeed, player.FastestHandSpeed, clear, navigation.PathFailed, lighting);
            brain.Reduced = settings.Scares == ScareIntensity.Reduced;
            if (Leader && brain.Mode == ZombieMode.Follow)
                brain.Follow(Leader.brain.Level == AlertLevel.Chasing, Leader.brain.Target, Slot(), now, Leader.brain.LastPlayerStimulus);
            var signals = brain.Tick(now, delta, true, in frame);
            // The audible/captioned warning is applied before alert publication or navigation.
            if ((signals & ZombieSignals.Snarl) != 0) Cues.Play(CueId.ZombieSnarl, eye, NoiseOrigin.Zombie);
            if ((signals & ZombieSignals.Sniff) != 0) Cues.Play(CueId.ZombieSniff, eye, NoiseOrigin.Zombie);
            if ((signals & ZombieSignals.Groan) != 0) Cues.Play(CueId.ZombieGroan, eye, NoiseOrigin.Zombie);
            if ((signals & ZombieSignals.AlertChanged) != 0) GameEvents.ZombieAlert.Raise(new ZombieAlertChanged(ZombieId, brain.Level, transform.position));
            if ((signals & ZombieSignals.Caught) != 0)
            {
                navigation.Stop(); Cues.Play(CueId.ZombieLunge, eye, NoiseOrigin.Zombie);
                holdingPlayer = true; // before the event: the flow may answer it at once
                GameEvents.PlayerCaught.Raise(new PlayerCaught(ZombieId, transform.position)); return;
            }
            // E-28: a zombie running to a gunshot moves at its chase speed although it is only investigating.
            // E-29: one leaving play stands where it is.
            navigation.Drive(brain.HasMoveTarget && !brain.IsStunned && !leaving, brain.Target,
                brain.Level == AlertLevel.Chasing || brain.Rushing ? brain.ChaseSpeed : brain.WalkSpeed, now);
            if (navigation.Speed > .05f && !shuffling.IsValid && FootEmitter) shuffling = Cues.PlayAttached(CueId.ZombieShuffle, FootEmitter, NoiseOrigin.Zombie);
            else if (navigation.Speed <= .05f && shuffling.IsValid) { Cues.Stop(shuffling); shuffling = default; }
            if (Animation) Animation.Apply(brain.Level, brain.Mode, navigation.Speed, player.HeadPosition);
        }
        /// <summary>
        /// Director-only same-seat command; never used by player gameplay. E-29: it comes into play or leaves it only where the
        /// player cannot see (except in the frames of an act start from the bed or a restore). A zombie already in play, such as
        /// the Walker waiting out of view for its scare, is shown where it stands at once: that reveal is meant to be seen.
        /// </summary>
        public void SetActiveInAct(bool value)
        {
            if (dead) return;
            if (value)
            {
                if (entering) return; // already waiting for the player to look away from where it will appear
                if (leaving) { leaving = false; brain.SetMode(ZombieMode.Dormant); } // shown again before it slipped away
                if (active) Present();
                else if (SwapsAtOnce || !PlayerCouldSee(enterAt)) Appear();
                else { entering = true; nextPresence = Time.time + ZombiePresence.CheckSeconds; }
                return;
            }
            if (entering) { entering = false; return; }
            if (leaving) return;
            if (active && Shown && !SwapsAtOnce && PlayerCouldSee(transform.position)) BeginLeave();
            else Vanish();
        }
        /// <summary>In play and in view where it stands (the pre-E-29 SetActiveInAct(true)).</summary>
        private void Present()
        {
            active = true;
            navigation.SetSimulationActive(true);
            navigation.Warp(transform.position);
            if (Animation) Animation.SetVisible(true);
            Shown = true;
            StartBreathing();
        }
        public void SetMode(ZombieMode mode) { if (dead) return; brain.SetMode(mode); if (mode == ZombieMode.Passive) { navigation.Stop(); StopLoops(2); } }
        /// <summary>
        /// E-22, director-only: the door zombie is suddenly at <paramref name="at"/>, facing the player, screaming (the director
        /// plays the scream). It hears nothing for a moment, then hunts by sound around that spot.
        /// </summary>
        public void Confront(Vector3 at)
        {
            if (dead) return;
            holdingPlayer = false;
            SetActiveInAct(true);
            navigation.Warp(at);
            Vector3 face = Services<IPlayerQuery>.TryGet(out var player) ? player.FloorPosition - at : -transform.forward;
            face.y = 0;
            if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(face);
            brain.Confront(at, Time.time);
            lastTick = Time.time; nextTick = Time.time + .1f;
            if (Animation) Animation.Lunge();
        }
        /// <summary>E-22: the player shoved this zombie off. It staggers back, groans, and must find them again by sound.</summary>
        private void LetGo()
        {
            holdingPlayer = false;
            brain.BreakFree(BreakFreeStunSeconds);
            Vector3 eye = Head ? Head.position : transform.position + Vector3.up;
            if (Services<IPlayerQuery>.TryGet(out var player))
            {
                Vector3 away = transform.position - player.FloorPosition; away.y = 0;
                if (away.sqrMagnitude < 1e-4f) away = -transform.forward;
                navigation.Warp(transform.position + away.normalized * BreakFreePushMeters);
            }
            navigation.Stop(); lastTick = Time.time;
            Cues.Play(CueId.ZombieGroan, eye, NoiseOrigin.Zombie);
        }
        public void MoveScripted(Vector3 target) { if (!dead) brain.ScriptedMove(target); }
        public void Stun(float seconds) { if (dead) return; brain.Stun(seconds); navigation.Stop(); }
        public void AssistSpeed(bool value) => brain.AssistedSpeed = value;
        public void SnapRestraint(bool lunge) { if (dead) return; brain.ReleaseRestraint(); if (Animation && lunge) Animation.Lunge(); }
        public void Kneel() { if (Animation && !dead) Animation.Kneel(); }

        /// <summary>
        /// E-25, the revolver (Claude integration): a headshot kills; a body shot staggers it for <see cref="ShotStaggerSeconds"/>
        /// and it snarls. Only a zombie in play and in view can be shot. Returns whether it died.
        /// </summary>
        public bool Shot(bool head)
        {
            if (dead || !active || !Shown) return false;
            Vector3 eye = Head ? Head.position : transform.position + Vector3.up;
            if (!head)
            {
                brain.Stun(ShotStaggerSeconds); navigation.Stop();
                if (Animation) Animation.Hit();
                Cues.Play(CueId.ZombieSnarl, eye, NoiseOrigin.Zombie);
                return false;
            }

            dead = true; active = false; holdingPlayer = false; leaving = entering = false;
            brain.SetMode(ZombieMode.Passive);
            navigation.SetSimulationActive(false); StopLoops();
            if (Animation) Animation.Die();
            GameEvents.ZombieAlert.Raise(new ZombieAlertChanged(ZombieId, AlertLevel.Unaware, transform.position));
            return true;
        }
        private void StartBreathing() { if (!breathing.IsValid && Head) breathing = Cues.PlayAttached(CueId.ZombieBreath, Head, NoiseOrigin.Zombie); }
        private void StopLoops(float fade = .1f) { Cues.Stop(breathing, fade); Cues.Stop(shuffling, fade); breathing = shuffling = default; }
    }
}
