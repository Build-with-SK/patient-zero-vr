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
        private bool active;
        private CueHandle breathing, shuffling;
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("PatientZero.AI.Tick");
        public ZombieBrain Brain => brain;
        /// <summary>In play this act (its simulation runs).</summary>
        public bool Active => active;
        /// <summary>In play and in view (the Walker stays out of view until its window pass).</summary>
        public bool Shown { get; private set; }
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
            // v1.4: a follower is never in play at the start of an act; it joins when its leader shows (Update).
            active = horde ? member.Hunter && member.Act == e.Act : ZombieIds.ActOf(ZombieId) == e.Act;
            StopLoops(); // v1.3: two zombies share the lobby lastTick = Time.time; nextTick = Time.time + ZombieId * .017f;
            brain.Reset(ZombieId == 4 ? ZombieMode.Restrained : ZombieMode.Dormant);
            formed = false;
            if (navigation) { navigation.SetSimulationActive(active); if (active) navigation.Warp(Spawn()); }
            if (horde) transform.rotation = Quaternion.Euler(0, member.Yaw, 0);
            if (Animation) Animation.ResetPose();
            Shown = active && ZombieId != 1;
            if (Animation) Animation.SetVisible(Shown);
            if (Shown) StartBreathing();
            if (active && horde && member.Patrols) brain.SetMode(ZombieMode.Patrol);
        }
        /// <summary>A follower comes into play with its leader and leaves with it: a pack is never split across acts.</summary>
        private void TrackLeader()
        {
            bool join = Leader.active && Leader.Shown;
            if (join == active) return;
            active = join; formed = false;
            StopLoops(); brain.Reset(ZombieMode.Dormant);
            navigation.SetSimulationActive(join);
            if (join)
            {
                navigation.Warp(Spawn()); transform.rotation = Quaternion.Euler(0, member.Yaw, 0);
                brain.SetMode(ZombieMode.Follow); lastTick = Time.time; nextTick = Time.time + ZombieId % 10 * .01f;
            }
            if (Animation) { Animation.ResetPose(); Animation.SetVisible(join); }
            Shown = join; // followers do not breathe: a pack is heard by its shuffling, and the voice budget stays with the hunters
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
                brain.Follow(Leader.brain.Level == AlertLevel.Chasing, Leader.brain.Target, Slot(), now);
            var signals = brain.Tick(now, delta, true, in frame);
            // The audible/captioned warning is applied before alert publication or navigation.
            if ((signals & ZombieSignals.Snarl) != 0) Cues.Play(CueId.ZombieSnarl, eye, NoiseOrigin.Zombie);
            if ((signals & ZombieSignals.Sniff) != 0) Cues.Play(CueId.ZombieSniff, eye, NoiseOrigin.Zombie);
            if ((signals & ZombieSignals.Groan) != 0) Cues.Play(CueId.ZombieGroan, eye, NoiseOrigin.Zombie);
            if ((signals & ZombieSignals.AlertChanged) != 0) GameEvents.ZombieAlert.Raise(new ZombieAlertChanged(ZombieId, brain.Level, transform.position));
            if ((signals & ZombieSignals.Caught) != 0)
            {
                navigation.Stop(); Cues.Play(CueId.ZombieLunge, eye, NoiseOrigin.Zombie);
                GameEvents.PlayerCaught.Raise(new PlayerCaught(ZombieId, transform.position)); return;
            }
            navigation.Drive(brain.HasMoveTarget && !brain.IsStunned, brain.Target,
                brain.Level == AlertLevel.Chasing ? brain.ChaseSpeed : brain.WalkSpeed, now);
            if (navigation.Speed > .05f && !shuffling.IsValid && FootEmitter) shuffling = Cues.PlayAttached(CueId.ZombieShuffle, FootEmitter, NoiseOrigin.Zombie);
            else if (navigation.Speed <= .05f && shuffling.IsValid) { Cues.Stop(shuffling); shuffling = default; }
            if (Animation) Animation.Apply(brain.Level, brain.Mode, navigation.Speed, player.HeadPosition);
        }
        /// <summary>Director-only same-seat command; never used by player gameplay.</summary>
        public void SetActiveInAct(bool value)
        {
            active = value;
            navigation.SetSimulationActive(value);
            if (value) navigation.Warp(transform.position);
            if (Animation) Animation.SetVisible(value);
            Shown = value;
            if (!value) { navigation.Stop(); StopLoops(); } else StartBreathing();
        }
        public void SetMode(ZombieMode mode) { brain.SetMode(mode); if (mode == ZombieMode.Passive) { navigation.Stop(); StopLoops(2); } }
        public void MoveScripted(Vector3 target) => brain.ScriptedMove(target);
        public void Stun(float seconds) { brain.Stun(seconds); navigation.Stop(); }
        public void AssistSpeed(bool value) => brain.AssistedSpeed = value;
        public void SnapRestraint(bool lunge) { brain.ReleaseRestraint(); if (Animation && lunge) Animation.Lunge(); }
        public void Kneel() { if (Animation) Animation.Kneel(); }
        private void StartBreathing() { if (!breathing.IsValid && Head) breathing = Cues.PlayAttached(CueId.ZombieBreath, Head, NoiseOrigin.Zombie); }
        private void StopLoops(float fade = .1f) { Cues.Stop(breathing, fade); Cues.Stop(shuffling, fade); breathing = shuffling = default; }
    }
}
