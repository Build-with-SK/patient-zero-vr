namespace PatientZero.Core
{
    /// <summary>
    /// Every cross-team event. Each channel has ONE writing team; tools/review/check_rules.py
    /// fails a branch that raises a channel it does not own. Reading is open to everyone.
    ///
    /// Codex A = player, interaction, flow, UI.   Codex B = AI, audio, atmosphere, director.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Writer: GameFlow (A). The only source of truth for GameState.</summary>
        public static readonly EventChannel<GameStateChanged> StateChanged =
            new EventChannel<GameStateChanged>(nameof(StateChanged));

        /// <summary>Writer: anyone. Reader: GameFlow (A) only, which validates against GameStateRules.</summary>
        public static readonly EventChannel<GameStateRequest> StateRequested =
            new EventChannel<GameStateRequest>(nameof(StateRequested));

        /// <summary>Writer: ExperienceDirector (B). Every act-dependent system resets itself on this.</summary>
        public static readonly EventChannel<ActStarted> ActStarted =
            new EventChannel<ActStarted>(nameof(ActStarted));

        /// <summary>Writer: puzzle components (A).</summary>
        public static readonly EventChannel<ObjectiveProgress> ObjectiveProgress =
            new EventChannel<ObjectiveProgress>(nameof(ObjectiveProgress));

        /// <summary>Writer: puzzle components (A). Reader: ExperienceDirector (B) advances acts on it.</summary>
        public static readonly EventChannel<ObjectiveCompleted> ObjectiveCompleted =
            new EventChannel<ObjectiveCompleted>(nameof(ObjectiveCompleted));

        /// <summary>
        /// Writer: ExperienceDirector (B). Reader: gated components (A), e.g. the Act 1 door.
        /// Raised at most once per gate per act start. Every gate of an act is closed again at
        /// its ActStarted, including a restore. A component enabled after the raise asks
        /// IExperienceDirector.IsGateReleased instead of waiting for the event (contract v1.1).
        /// </summary>
        public static readonly EventChannel<GateReleased> GateReleased =
            new EventChannel<GateReleased>(nameof(GateReleased));

        /// <summary>Writer: ExperienceDirector (B). Reader: puzzles and hint UI (A).</summary>
        public static readonly EventChannel<AssistChanged> AssistChanged =
            new EventChannel<AssistChanged>(nameof(AssistChanged));

        /// <summary>Writer: the audio cue player (B) only, for every cue with a noise radius above 0.</summary>
        public static readonly EventChannel<NoiseEvent> Noise =
            new EventChannel<NoiseEvent>(nameof(Noise));

        /// <summary>Writer: zombie AI (B).</summary>
        public static readonly EventChannel<ZombieAlertChanged> ZombieAlert =
            new EventChannel<ZombieAlertChanged>(nameof(ZombieAlert));

        /// <summary>Writer: zombie AI (B). Reader: GameFlow (A) moves to Caught.</summary>
        public static readonly EventChannel<PlayerCaught> PlayerCaught =
            new EventChannel<PlayerCaught>(nameof(PlayerCaught));

        /// <summary>Writer: Atmosphere (B).</summary>
        public static readonly EventChannel<LightingChanged> LightingChanged =
            new EventChannel<LightingChanged>(nameof(LightingChanged));

        /// <summary>Writer: ExperienceDirector (B). Reader: hand visuals (A).</summary>
        public static readonly EventChannel<InfectionReveal> InfectionReveal =
            new EventChannel<InfectionReveal>(nameof(InfectionReveal));

        /// <summary>Writer: the audio cue player (B). Reader: caption UI (A).</summary>
        public static readonly EventChannel<CaptionRequested> CaptionRequested =
            new EventChannel<CaptionRequested>(nameof(CaptionRequested));

        /// <summary>Writer: settings service (A).</summary>
        public static readonly EventChannel<SettingsChanged> SettingsChanged =
            new EventChannel<SettingsChanged>(nameof(SettingsChanged));

        /// <summary>Writer: player rig (A).</summary>
        public static readonly EventChannel<TrackingChanged> TrackingChanged =
            new EventChannel<TrackingChanged>(nameof(TrackingChanged));

        /// <summary>Writer: AI or director (B). Reader: haptics (A), which ignores it in hands mode.</summary>
        public static readonly EventChannel<HapticRequested> HapticRequested =
            new EventChannel<HapticRequested>(nameof(HapticRequested));

        /// <summary>
        /// Drops every subscriber on every channel. Runs automatically before the first scene
        /// loads (CoreStaticReset), so Enter Play Mode without a domain reload starts clean.
        /// A unit test checks that no channel is missing from this list.
        /// </summary>
        public static void ResetAll()
        {
            StateChanged.Clear();
            StateRequested.Clear();
            ActStarted.Clear();
            ObjectiveProgress.Clear();
            ObjectiveCompleted.Clear();
            GateReleased.Clear();
            AssistChanged.Clear();
            Noise.Clear();
            ZombieAlert.Clear();
            PlayerCaught.Clear();
            LightingChanged.Clear();
            InfectionReveal.Clear();
            CaptionRequested.Clear();
            SettingsChanged.Clear();
            TrackingChanged.Clear();
            HapticRequested.Clear();
        }
    }
}
