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

    /// <summary>
    /// The acts, in play order. Acts 1-4 are the ward (Floor 3); contract v1.3 adds one act per lower floor
    /// (GAME_DESIGN.md section 11). Owned by ExperienceDirector (Codex B).
    /// </summary>
    public enum ActId
    {
        None = 0,
        Awakening = 1,  // 00:00-00:30
        Machinery = 2,  // 00:30-01:00
        Darkness = 3,   // 01:00-01:30
        Exit = 4,       // 01:30-02:00
        Laboratory = 5, // v1.3: Floor 2
        Lobby = 6,      // v1.3: Floor 1, the last act
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
        Act4_StairsReached = 402,     // v1.3: player on the Floor 3 stair landing after the exit door opened
        Act5_FuseInserted = 500,      // v1.3: fuse carried to the fuse box (socket mechanic)
        Act5_OverridePulled = 501,    // v1.3: stair door override held (lever mechanic); needs the fuse
        Act5_StairDoorOpened = 502,   // v1.3
        Act5_StairsReached = 503,     // v1.3
        Act6_GeneratorPrimed = 600,   // v1.3: generator valve turned (valve mechanic)
        Act6_LockdownReleased = 601,  // v1.3: lockdown lever held (lever mechanic); needs the generator
        Act6_MainDoorsOpened = 602,   // v1.3: the main doors are fully open; the ending follows
    }

    /// <summary>Every objective in play order (contract v1.3). Index lookups and per-act sets read this list.</summary>
    public static class ObjectiveIds
    {
        private static readonly ObjectiveId[] AllArray =
        {
            ObjectiveId.Act1_DoorUnbarred, ObjectiveId.Act1_DoorOpened,
            ObjectiveId.Act2_ValveVented, ObjectiveId.Act2_CablePlugged, ObjectiveId.Act2_PowerRestored,
            ObjectiveId.Act3_BeaconReached,
            ObjectiveId.Act4_ReleasePulled, ObjectiveId.Act4_ExitOpened, ObjectiveId.Act4_StairsReached,
            ObjectiveId.Act5_FuseInserted, ObjectiveId.Act5_OverridePulled, ObjectiveId.Act5_StairDoorOpened, ObjectiveId.Act5_StairsReached,
            ObjectiveId.Act6_GeneratorPrimed, ObjectiveId.Act6_LockdownReleased, ObjectiveId.Act6_MainDoorsOpened,
        };

        public static readonly System.Collections.Generic.IReadOnlyList<ObjectiveId> All = System.Array.AsReadOnly(AllArray);
        public static int Count => AllArray.Length;

        /// <summary>Position in <see cref="All"/>, or -1 for None and unknown values. Allocation-free.</summary>
        public static int IndexOf(ObjectiveId id)
        {
            for (int i = 0; i < AllArray.Length; i++) if (AllArray[i] == id) return i;
            return -1;
        }

        /// <summary>The act an objective belongs to: its number divided by 100.</summary>
        public static ActId ActOf(ObjectiveId id) => (ActId)((int)id / 100);
    }

    /// <summary>One zombie per ward act, then the lower floors' (v1.3). Used as ZombieAlertChanged.ZombieId and PlayerCaught.ZombieId.</summary>
    public static class ZombieIds
    {
        public const int Walker = 1;        // Z1, Act 1, scripted only: cannot catch
        public const int Orderly = 2;       // Z2, Act 2, investigates noise
        public const int Patroller = 3;     // Z3, Act 3, blind in the dark, hunts by ear
        public const int FinalPatient = 4;  // Z4, Act 4, restrained by a 1.3 m chain
        public const int Researcher = 5;    // Z5, Act 5 (v1.3), asleep in Lab B, investigates noise
        public const int Guard = 6;         // Z6, Act 6 (v1.3), patrols across the lobby
        public const int Porter = 7;        // Z7, Act 6 (v1.3), asleep behind the reception desk, investigates noise
        public const int Count = 7;

        /// <summary>The act in which a zombie is in play; None for an unknown id.</summary>
        public static ActId ActOf(int id)
        {
            if (id >= Walker && id <= Researcher) return (ActId)id;
            return id == Guard || id == Porter ? ActId.Lobby : ActId.None;
        }
    }

    /// <summary>
    /// Scripted gates the director opens when a beat's conditions are met (contract v1.1).
    /// Written by the ExperienceDirector (Codex B); read by the gated components (Codex A).
    /// </summary>
    public enum GateId
    {
        None = 0,
        Act1Door = 1,  // all planks off and Z1 at least 4 m from the door window (GAME_DESIGN.md, Act 1 "Unlatch")
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
