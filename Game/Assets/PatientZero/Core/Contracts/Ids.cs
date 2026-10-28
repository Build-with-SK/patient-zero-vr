// Shared identifiers for every PATIENT ZERO assembly.
//
// APPEND-ONLY. Never renumber, reorder or delete a value: Unity serialises enums as
// integers, so changing a number silently rewires every asset that stores it.
// New values are added by the Technical Director (Claude) on request from either
// Codex agent; see docs/ARCHITECTURE.md section 5.

namespace PatientZero.Core
{
    /// <summary>Top-level session state. Owned by GameFlow (Codex A).</summary>
    public enum GameState
    {
        Boot = 0,
        Calibration = 1,
        Playing = 2,
        Paused = 3,
        Caught = 4,
        Ending = 5,
        Complete = 6,
    }

    /// <summary>The four 30-second acts. Owned by ExperienceDirector (Codex B).</summary>
    public enum ActId
    {
        None = 0,
        Awakening = 1,  // 00:00-00:30
        Machinery = 2,  // 00:30-01:00
        Darkness = 3,   // 01:00-01:30
        Exit = 4,       // 01:30-02:00
    }

    /// <summary>Player objectives. Completed by puzzle components (Codex A).</summary>
    public enum ObjectiveId
    {
        None = 0,
        Act1_DoorUnbarred = 100,   // all three planks removed (progress = planks / 3)
        Act1_DoorOpened = 101,
        Act2_ValveVented = 200,    // progress = accumulated rotation / required rotation
        Act2_CablePlugged = 201,
        Act2_PowerRestored = 202,  // raised when 200 and 201 are both complete
        Act3_BeaconReached = 300,
        Act4_ReleasePulled = 400,  // progress = hold time / required hold time
        Act4_ExitOpened = 401,
    }

    /// <summary>One zombie per act. Used as ZombieAlertChanged.ZombieId and PlayerCaught.ZombieId.</summary>
    public static class ZombieIds
    {
        public const int Walker = 1;        // Z1, Act 1, scripted only: cannot catch
        public const int Orderly = 2;       // Z2, Act 2, investigates noise
        public const int Patroller = 3;     // Z3, Act 3, blind in the dark, hunts by ear
        public const int FinalPatient = 4;  // Z4, Act 4, restrained by a 1.3 m chain
    }

    public enum FailReason
    {
        None = 0,
        CaughtByZombie = 1,
    }

    /// <summary>How aware a zombie is of the player. Owned by zombie AI (Codex B).</summary>
    public enum AlertLevel
    {
        Unaware = 0,
        Suspicious = 1,
        Investigating = 2,
        Searching = 3,
        Chasing = 4,
    }

    /// <summary>Who made a noise. Zombies ignore their own kind.</summary>
    public enum NoiseOrigin
    {
        Player = 0,
        Zombie = 1,
        Scripted = 2,  // director lures, e.g. the distant crash in Act 1
        Ambient = 3,
    }

    /// <summary>Lighting presets. Owned by Atmosphere (Codex B).</summary>
    public enum LightingState
    {
        None = 0,
        Awakening = 1,
        WardDim = 2,
        Surge = 3,
        Blackout = 4,
        EmergencyRed = 5,
        ExitDaylight = 6,
    }

    public enum InputModality
    {
        None = 0,
        Hands = 1,
        Controllers = 2,
    }

    public enum HandSide
    {
        Left = 0,
        Right = 1,
    }

    /// <summary>Escalating help for a stalled objective. Director (B) raises, puzzles (A) obey.</summary>
    public enum AssistLevel
    {
        None = 0,
        Hint1 = 1,   // ambient pointer: light or sound draws the eye
        Hint2 = 2,   // explicit: PA line or caption names the object
        Assist = 3,  // mechanical: wider snap radius, less rotation needed
    }

    public enum CaptionKind
    {
        Sound = 0,
        Speech = 1,
    }

    public enum ScareIntensity
    {
        Standard = 0,
        Reduced = 1,
    }

    public enum LocomotionMode
    {
        TeleportOnly = 0,
        TeleportAndSmooth = 1,
    }

    public enum TurnMode
    {
        Snap = 0,
        Smooth = 1,
    }

    /// <summary>
    /// Every gameplay sound. The audio cue bank (Codex B) maps each id to clips,
    /// attenuation, mixer group, noise radius and caption. Ranges:
    /// 100 Act 1, 200 Act 2, 300 Act 3, 400 Act 4, 500 zombie, 600 player and physics,
    /// 700 ambience and music, 800 UI. Full table: docs/GAME_DESIGN.md section 6.
    /// </summary>
    public enum CueId
    {
        None = 0,

        // Act 1 - Awakening
        HeartbeatWake = 100,
        FluorescentBuzz = 101,
        PaGarbledBreach = 102,
        PlankCreak = 103,
        PlankPop = 104,
        WindowSlam = 105,
        WindowBreath = 106,      // Reduced-intensity replacement for WindowSlam
        DistantCrashLure = 107,
        DoorUnlatch = 108,

        // Act 2 - Machinery
        ValveSqueal = 200,       // loop; intensity follows turn speed
        ValveVentHiss = 201,
        CableDrag = 202,         // loop
        PlugSpark = 203,
        PowerThrum = 204,
        MonitorBeep = 205,
        BreakerOverload = 206,

        // Act 3 - Darkness
        BeaconChirp = 300,
        PaCarrierWarning = 301,
        WaterDrip = 302,

        // Act 4 - Exit
        AlarmKlaxon = 400,
        RestraintStrain = 401,
        RestraintSnap = 402,
        ReleaseBarGrind = 403,   // loop
        ExitDoorOpen = 404,
        PaPatientZero = 405,
        RevealDrone = 406,

        // Zombies
        ZombieBreath = 500,      // loop
        ZombieGroan = 501,
        ZombieSnarl = 502,
        ZombieShuffle = 503,
        ZombieSniff = 504,
        ZombieLunge = 505,
        ZombieGlassTap = 506,

        // Player and physics
        ImpactLight = 600,
        ImpactHeavy = 601,
        PlayerFootstep = 602,
        TeleportLand = 603,
        CaughtHeartbeat = 604,

        // Ambience and music
        RoomTone = 700,
        Ventilation = 701,
        DistantInfected = 702,
        TensionDrone = 703,
        BlackoutTone = 704,
        ExteriorWind = 705,

        // UI (2D, ignores listener pause)
        UiSelect = 800,
        UiBack = 801,
        UiPause = 802,
    }
}
