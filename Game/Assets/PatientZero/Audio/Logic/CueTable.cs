using System;
using PatientZero.Core;

namespace PatientZero.Audio.Logic
{
    public enum MixerBus { World, Voice, Ambience, Music, UI }
    public enum CaptionPolicy { None, Always, Visuals, Speech }

    /// <summary>One immutable row in the approved sound table; consumed by Audio runtime.</summary>
    public readonly struct CueDefinition
    {
        public readonly CueId Id;
        public readonly bool World, Loop;
        public readonly float Min, Max, NoiseRadius;
        public readonly MixerBus Bus;
        public readonly CaptionPolicy CaptionPolicy;
        public readonly string Caption;
        public readonly int Priority;
        public float Doppler => 0;
        public CueDefinition(CueId id, bool world, float min, float max, bool loop, float noise,
            MixerBus bus, CaptionPolicy policy = CaptionPolicy.None, string caption = "")
        {
            Id = id; World = world; Min = min; Max = max; Loop = loop; NoiseRadius = noise;
            Bus = bus; CaptionPolicy = policy; Caption = caption;
            Priority = bus == MixerBus.UI ? 0 : bus == MixerBus.Voice ? 32 :
                (int)id >= 500 && (int)id < 600 ? 64 : bus == MixerBus.Ambience || bus == MixerBus.Music ? 160 : 96;
        }
        /// <summary>Unity's custom distance curve uses this linear-to-silence rule.</summary>
        public float GainAt(float distance)
        {
            if (!World) return 1;
            if (float.IsNaN(distance) || distance >= Max) return 0;
            return distance <= Min ? 1 : 1 - (distance - Min) / (Max - Min);
        }
        public bool ShouldCaption(ExperienceSettings settings) => CaptionPolicy == CaptionPolicy.Always ||
            CaptionPolicy == CaptionPolicy.Speech && settings.Subtitles || CaptionPolicy == CaptionPolicy.Visuals && settings.SoundCueVisuals;
    }

    /// <summary>Exactly one row per contract CueId. None is a silent sentinel, never playable.</summary>
    public static class CueTable
    {
        private static CueDefinition W(CueId id, float min, float max, bool loop = false, float noise = 0,
            MixerBus bus = MixerBus.World, CaptionPolicy policy = CaptionPolicy.None, string caption = "") =>
            new CueDefinition(id, true, min, max, loop, noise, bus, policy, caption);
        private static CueDefinition F(CueId id, MixerBus bus, bool loop = false, CaptionPolicy policy = CaptionPolicy.None, string caption = "") =>
            new CueDefinition(id, false, 0, 1, loop, 0, bus, policy, caption);
        private static readonly CueDefinition[] Rows =
        {
            F(CueId.None, MixerBus.World),
            F(CueId.HeartbeatWake, MixerBus.Music, false, CaptionPolicy.Visuals, "[heartbeat]"),
            W(CueId.FluorescentBuzz, .5f, 6, true, 0, MixerBus.Ambience),
            W(CueId.PaGarbledBreach, 2, 20, false, 0, MixerBus.Voice, CaptionPolicy.Speech, "...containment breach... Ward B is sealed..."),
            W(CueId.PlankCreak, .5f, 8, true, 2, MixerBus.World, CaptionPolicy.Visuals, "[wood creaks]"),
            W(CueId.PlankPop, 1, 12, false, 5, MixerBus.World, CaptionPolicy.Visuals, "[plank cracks]"),
            W(CueId.WindowSlam, 1, 15, false, 0, MixerBus.World, CaptionPolicy.Always, "[slam on the glass]"),
            W(CueId.WindowBreath, .5f, 6, false, 0, MixerBus.World, CaptionPolicy.Always, "[breathing behind the glass]"),
            W(CueId.DistantCrashLure, 3, 25, false, 20, MixerBus.World, CaptionPolicy.Always, "[distant crash]"),
            W(CueId.DoorUnlatch, .5f, 10, false, 3, MixerBus.World, CaptionPolicy.Visuals, "[latch clicks]"),
            W(CueId.ValveSqueal, .5f, 12, true, 6, MixerBus.World, CaptionPolicy.Visuals, "[valve squeals]"),
            W(CueId.ValveVentHiss, 1, 12, false, 4, MixerBus.World, CaptionPolicy.Visuals, "[steam hiss]"),
            W(CueId.CableDrag, .3f, 4, true, 1),
            W(CueId.PlugSpark, 1, 14, false, 6, MixerBus.World, CaptionPolicy.Always, "[sparks]"),
            W(CueId.PowerThrum, 2, 20, false, 0, MixerBus.World, CaptionPolicy.Always, "[power hums]"),
            W(CueId.MonitorBeep, .5f, 8, true, 0, MixerBus.Ambience),
            W(CueId.BreakerOverload, 2, 25, false, 0, MixerBus.World, CaptionPolicy.Always, "[breaker bursts]"),
            W(CueId.BeaconChirp, 2, 25, false, 0, MixerBus.World, CaptionPolicy.Always, "[chirp]"),
            W(CueId.PaCarrierWarning, 2, 20, false, 0, MixerBus.Voice, CaptionPolicy.Speech, "...do not approach the carrier..."),
            W(CueId.WaterDrip, .3f, 7, true, 0, MixerBus.Ambience, CaptionPolicy.Visuals, "[dripping]"),
            W(CueId.AlarmKlaxon, 3, 20, true, 0, MixerBus.World, CaptionPolicy.Always, "[alarm]"),
            W(CueId.RestraintStrain, .5f, 10, true, 0, MixerBus.World, CaptionPolicy.Always, "[straps straining]"),
            W(CueId.RestraintSnap, 1, 15, false, 0, MixerBus.World, CaptionPolicy.Always, "[strap snaps]"),
            W(CueId.ReleaseBarGrind, .5f, 8, true, 0, MixerBus.World, CaptionPolicy.Visuals, "[metal grinding]"),
            W(CueId.ExitDoorOpen, 2, 20, false, 0, MixerBus.World, CaptionPolicy.Always, "[door bursts open]"),
            W(CueId.PaPatientZero, 2, 20, false, 0, MixerBus.Voice, CaptionPolicy.Speech, "Containment failure. Patient Zero has left isolation."),
            F(CueId.RevealDrone, MixerBus.Music),
            W(CueId.ZombieBreath, .5f, 9, true, 0, MixerBus.World, CaptionPolicy.Always, "[breathing]"),
            W(CueId.ZombieGroan, 1, 15, false, 0, MixerBus.World, CaptionPolicy.Always, "[groan]"),
            W(CueId.ZombieSnarl, 1, 15, false, 0, MixerBus.World, CaptionPolicy.Always, "[snarl: it heard you]"),
            W(CueId.ZombieShuffle, .5f, 10, true, 0, MixerBus.World, CaptionPolicy.Always, "[shuffling]"),
            W(CueId.ZombieSniff, .5f, 9, false, 0, MixerBus.World, CaptionPolicy.Always, "[sniffing]"),
            W(CueId.ZombieLunge, .5f, 9, false, 0, MixerBus.World, CaptionPolicy.Always, "[lunge]"),
            W(CueId.ZombieGlassTap, .5f, 9, false, 0, MixerBus.World, CaptionPolicy.Always, "[tapping on glass]"),
            W(CueId.ImpactLight, .5f, 10, false, 2, MixerBus.World, CaptionPolicy.Visuals, "[clatter]"),
            W(CueId.ImpactHeavy, 1, 14, false, 6, MixerBus.World, CaptionPolicy.Visuals, "[crash]"),
            W(CueId.PlayerFootstep, .3f, 5, false, 2.5f),
            W(CueId.TeleportLand, .3f, 4, false, 1.5f),
            F(CueId.CaughtHeartbeat, MixerBus.Music),
            F(CueId.RoomTone, MixerBus.Ambience, true),
            W(CueId.Ventilation, 1, 8, true, 0, MixerBus.Ambience),
            W(CueId.DistantInfected, 3, 25, false, 0, MixerBus.Ambience, CaptionPolicy.Always, "[distant groan]"),
            F(CueId.TensionDrone, MixerBus.Music, true),
            F(CueId.BlackoutTone, MixerBus.Ambience, true),
            W(CueId.ExteriorWind, 2, 30, true, 0, MixerBus.Ambience),
            F(CueId.UiSelect, MixerBus.UI), F(CueId.UiBack, MixerBus.UI), F(CueId.UiPause, MixerBus.UI)
        };
        public static int Count => Rows.Length;
        public static CueDefinition At(int index) => Rows[index];
        public static bool TryGet(CueId id, out CueDefinition row)
        {
            for (int i = 0; i < Rows.Length; i++) if (Rows[i].Id == id) { row = Rows[i]; return true; }
            row = default; return false;
        }
    }
}
