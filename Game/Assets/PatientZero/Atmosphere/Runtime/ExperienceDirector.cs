// UNVERIFIED IN EDITOR
using PatientZero.AI;
using PatientZero.AI.Logic;
using PatientZero.Audio;
using PatientZero.Atmosphere.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Atmosphere
{
    /// <summary>Ward director implements Core's service and applies tested commands using only seat-B systems.</summary>
    public sealed class ExperienceDirector : MonoBehaviour, IExperienceDirector
    {
        public ZombieAgentShell[] Zombies = new ZombieAgentShell[0];
        public LightingController Lighting;
        public WardAudioEmitters Emitters;
        public LayerMask AmbientOccluders = 1;
        private static DirectorMemory retained = new DirectorMemory();
        private DirectorCore core;
        private GameState state = GameState.Boot;
        private float nextTick, lastTick, nextAmbient;
        private bool pumping;
        public ActId CurrentAct => core == null ? ActId.None : core.Act;
        public float ExperienceTime => core == null ? 0 : core.ExperienceTime;
        // Contract v1.1: late-loaded doors query the same latch that produces the one-shot signal.
        public bool IsGateReleased(GateId gate) => core != null && core.IsGateReleased(gate);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRetainedState() => retained = new DirectorMemory();
        private void Awake() { core = new DirectorCore(retained); lastTick = Time.time; }
        private void OnEnable()
        {
            if (!Services<IExperienceDirector>.Register(this)) { enabled = false; return; }
            GameEvents.ObjectiveProgress.Subscribe(Progress); GameEvents.ObjectiveCompleted.Subscribe(Complete);
            GameEvents.Noise.Subscribe(Noise); GameEvents.ZombieAlert.Subscribe(Alert); GameEvents.PlayerCaught.Subscribe(Caught);
            GameEvents.SettingsChanged.Subscribe(SettingsChanged); GameEvents.StateChanged.Subscribe(StateChanged);
            GameEvents.TrackingChanged.Subscribe(TrackingChanged);
            if (Services<ISettingsProvider>.TryGet(out var provider)) core.Settings = provider.Current;
        }
        private void OnDisable()
        {
            Services<IExperienceDirector>.Unregister(this);
            GameEvents.ObjectiveProgress.Unsubscribe(Progress); GameEvents.ObjectiveCompleted.Unsubscribe(Complete);
            GameEvents.Noise.Unsubscribe(Noise); GameEvents.ZombieAlert.Unsubscribe(Alert); GameEvents.PlayerCaught.Unsubscribe(Caught);
            GameEvents.SettingsChanged.Unsubscribe(SettingsChanged); GameEvents.StateChanged.Unsubscribe(StateChanged);
            GameEvents.TrackingChanged.Unsubscribe(TrackingChanged);
        }
        public void StartRun(ActId startAct, bool isRestore) { core.StartRun(startAct, isRestore); lastTick = Time.time; Pump(); }
        private void Progress(ObjectiveProgress e) { core.Progress(e.Objective, e.Progress01); Pump(); }
        private void Complete(ObjectiveCompleted e) { core.Complete(e.Objective); Pump(); }
        private void Noise(NoiseEvent e) => core.Noise(in e);
        private void Alert(ZombieAlertChanged e)
        { if (Services<IPlayerQuery>.TryGet(out var player)) core.Alert(e.ZombieId, e.Level, e.Position, player.FloorPosition); }
        private void Caught(PlayerCaught e) => core.Caught();
        private void SettingsChanged(SettingsChanged e) { core.Settings = e.Settings; Pump(); }
        private void StateChanged(GameStateChanged e) { state = e.To; lastTick = Time.time; }
        private void TrackingChanged(TrackingChanged e) => core.Tracking(e.Modality, e.LeftTracked, e.RightTracked);
        private void Update()
        {
            if (Time.time < nextTick || core.Act == ActId.None) return;
            nextTick = Time.time + .1f;
            float delta = Mathf.Max(0, Time.time - lastTick); lastTick = Time.time;
            if (!Services<IPlayerQuery>.TryGet(out var player)) return;
            for (int i = 0; i < Zombies.Length; i++) if (Zombies[i]) core.ReportZombiePosition(Zombies[i].ZombieId, Zombies[i].transform.position);
            bool live = GameStateRules.IsSimulationLive(state);
            core.Tick(delta, live, player.FloorPosition); Pump();
            if (live && Time.time >= nextAmbient)
            {
                nextAmbient = Time.time + 1;
                TryAmbient(WardLayout.Z1_WalkFrom.Position + Vector3.up, player);
                TryAmbient(WardLayout.Z2_Dormant.Position + Vector3.up, player);
                TryAmbient(WardLayout.Z3_PatrolNorth.Position + Vector3.up, player);
                TryAmbient(WardLayout.Z4_Gurney.Position + Vector3.up, player);
                Pump();
            }
        }
        private void TryAmbient(Vector3 position, IPlayerQuery player)
        {
            if (Vector3.Dot(position - player.HeadPosition, player.HeadForward) > 0) return;
            core.TryAmbient(position, player.HeadPosition, Physics.Linecast(player.HeadPosition, position, AmbientOccluders, QueryTriggerInteraction.Ignore));
        }
        private void Pump()
        {
            if (pumping) return; pumping = true;
            try
            {
                while (core.TryRead(out var c))
                {
                    switch (c.Kind)
                    {
                        case DirectorCommandKind.Cue:
                            Cues.Play(c.Cue, c.Position, c.Cue == CueId.DistantCrashLure ? NoiseOrigin.Scripted : NoiseOrigin.Ambient, c.Value);
                            if (c.Cue == CueId.BeaconChirp && Lighting) Lighting.BeaconPulse();
                            break;
                        case DirectorCommandKind.Lighting: if (Lighting) Lighting.SetState((LightingState)c.Id); break;
                        case DirectorCommandKind.LocalLightPulse: if (Lighting) Lighting.WindowPulse(); break;
                        case DirectorCommandKind.Gate: GameEvents.GateReleased.Raise(new GateReleased((GateId)c.Id)); break;
                        case DirectorCommandKind.Act: GameEvents.ActStarted.Raise(new ActStarted((ActId)c.Id, c.Detail != 0, (int)c.Value)); break;
                        case DirectorCommandKind.Assist: GameEvents.AssistChanged.Raise(new AssistChanged((ObjectiveId)c.Id, (AssistLevel)c.Detail)); break;
                        case DirectorCommandKind.Zombie: ZombieCommand(c); break;
                        case DirectorCommandKind.Reveal: GameEvents.InfectionReveal.Raise(new InfectionReveal(c.Value)); break;
                        case DirectorCommandKind.State: GameEvents.StateRequested.Raise(new GameStateRequest((GameState)c.Id, FailReason.None)); break;
                        case DirectorCommandKind.Silence:
                            if (Services<IAudioCuePlayer>.TryGet(out var service) && service is AudioCuePlayer audio) audio.DuckAmbience(c.Value);
                            break;
                        case DirectorCommandKind.Tension: if (Emitters) Emitters.SetTension(c.Value); break;
                        case DirectorCommandKind.StopZombieLoops:
                            if (Services<IAudioCuePlayer>.TryGet(out var s) && s is AudioCuePlayer a) a.FadeZombieLoops(c.Value);
                            break;
                    }
                }
            }
            finally { pumping = false; }
        }
        private void ZombieCommand(DirectorCommand c)
        {
            for (int i = 0; i < Zombies.Length; i++)
            {
                var z = Zombies[i]; if (!z || z.ZombieId != c.Id) continue;
                z.AssistSpeed(core.AdaptiveAssist);
                switch ((ZombieDirection)c.Detail)
                {
                    case ZombieDirection.Hide: z.SetActiveInAct(false); break;
                    case ZombieDirection.WindowApproach: z.SetActiveInAct(true); z.MoveScripted(c.Position + Vector3.forward * .45f); break;
                    case ZombieDirection.LeaveWindow: z.MoveScripted(c.Position); break;
                    case ZombieDirection.Dormant: z.SetActiveInAct(true); z.SetMode(ZombieMode.Dormant); break;
                    case ZombieDirection.Investigate: z.SetActiveInAct(true); break; // The scripted audible cue owns the actual hearing target.
                    case ZombieDirection.Patrol: z.SetActiveInAct(true); z.SetMode(ZombieMode.Patrol); break;
                    case ZombieDirection.Hold: z.SetActiveInAct(true); z.MoveScripted(WardLayout.Z3_PatrolNorth.Position); break;
                    case ZombieDirection.Stun: z.Stun(c.Value); break;
                    case ZombieDirection.Restraint: z.SetActiveInAct(true); z.SetMode(ZombieMode.Restrained); break;
                    case ZombieDirection.Snap: z.SnapRestraint(c.Value > 0); break;
                    case ZombieDirection.Passive: z.SetMode(ZombieMode.Passive); break;
                    case ZombieDirection.Kneel: z.Kneel(); break;
                }
            }
        }
    }
}
