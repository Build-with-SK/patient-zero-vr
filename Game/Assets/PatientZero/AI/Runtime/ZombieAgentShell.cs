// UNVERIFIED IN EDITOR
using PatientZero.AI.Logic;
using PatientZero.Core;
using Unity.Profiling;
using UnityEngine;

namespace PatientZero.AI
{
    /// <summary>Ward zombie shell: 10 Hz phased AI, contract-only player queries, cue-before-chase ordering.</summary>
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
        private ZombieBrain brain;
        private ZombieNavigation navigation;
        private GameState state = GameState.Boot;
        private LightingState lighting = LightingState.Awakening;
        private ExperienceSettings settings = ExperienceSettings.Defaults;
        private float nextTick, lastTick;
        private bool active;
        private CueHandle breathing, shuffling;
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("PatientZero.AI.Tick");
        public ZombieBrain Brain => brain;
        private void Awake()
        {
            navigation = GetComponent<ZombieNavigation>();
            Vector3 spawn = Spawn();
            brain = new ZombieBrain(ZombieId, spawn, new System.Random(1200 + ZombieId),
                WardLayout.Z3_PatrolNorth.Position, WardLayout.Z3_PatrolSouth.Position);
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
        private Vector3 Spawn() => ZombieId == 1 ? WardLayout.Z1_WalkFrom.Position : ZombieId == 2 ? WardLayout.Z2_Dormant.Position :
            ZombieId == 3 ? WardLayout.Z3_PatrolNorth.Position : WardLayout.Z4_Gurney.Position;
        private void ActStarted(ActStarted e)
        {
            active = (int)e.Act == ZombieId; StopLoops(); lastTick = Time.time; nextTick = Time.time + ZombieId * .017f;
            brain.Reset(ZombieId == 4 ? ZombieMode.Restrained : ZombieMode.Dormant);
            if (navigation) { navigation.SetSimulationActive(active); if (active) navigation.Warp(Spawn()); }
            if (Animation) Animation.ResetPose();
            if (Animation) Animation.SetVisible(active && ZombieId != 1);
            if (active && ZombieId != 1) StartBreathing();
        }
        private void StateChanged(GameStateChanged e)
        { state = e.To; lastTick = Time.time; if (!GameStateRules.IsSimulationLive(state)) navigation.Stop(); }
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
