using System;
using PatientZero.Core;

namespace PatientZero.Audio.Logic
{
    /// <summary>Original deterministic synthesis, generated once at load. No runtime DSP callback or external recordings.</summary>
    public static class ProceduralSound
    {
        public const int SampleRate = 48000;
        public const float PeakLimit = .16f;
        public static float Duration(CueDefinition row) => row.Id == CueId.None ? 0 :
            row.Id == CueId.HeartbeatWake || row.Id == CueId.RevealDrone ? 6 : row.Id == CueId.CaughtHeartbeat ? 2.5f :
            row.Bus == MixerBus.Voice ? 4 : row.Bus == MixerBus.UI ? .15f : row.Loop ? 4 :
            row.Id == CueId.ZombieGroan || row.Id == CueId.DistantInfected ? 2.4f : .8f;

        public static float[] Render(CueDefinition row)
        {
            float duration = Duration(row);
            var samples = new float[(int)(duration * SampleRate)];
            uint random = (uint)((int)row.Id + 207);
            double low = 0, slower = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)SampleRate;
                random ^= random << 13; random ^= random >> 17; random ^= random << 5;
                double noise = random / (double)uint.MaxValue * 2 - 1;
                low += .04 * (noise - low); slower += .005 * (noise - slower);
                double signal = Signal(row, t, noise, low, slower);
                // Taper both ends, including loops, to remove wrap clicks. Not a streaming allocation.
                double edge = Math.Min(1, Math.Min(t / .025, (samples.Length - 1 - i) / (SampleRate * .04)));
                if (row.Id == CueId.HeartbeatWake) edge *= Math.Max(0, 1 - t / 6);
                samples[i] = (float)(Math.Tanh(signal) * PeakLimit * edge);
            }
            return samples;
        }
        private static double Sin(double hz, double time) => Math.Sin(2 * Math.PI * hz * time);
        private static double Pulse(double time, double period, double decay, double offset = 0)
        {
            if (time < offset) return 0;
            return Math.Exp(-((time - offset) % period) * decay);
        }
        private static double Signal(CueDefinition r, double t, double noise, double low, double slower)
        {
            switch (r.Id)
            {
                case CueId.ZombieBreath: case CueId.WindowBreath:
                    return low * (1.2 + .8 * Sin(.5, t)) + .08 * Sin(53, t) * Math.Max(0, Sin(.5, t));
                case CueId.ZombieShuffle:
                    return (low * 2 + noise * .12) * Pulse(t, .5, 15);
                case CueId.PlayerFootstep: case CueId.TeleportLand:
                    return (Sin(65, t) * .7 + low * 2) * Math.Exp(-t * 14);
                case CueId.ZombieGroan: case CueId.DistantInfected: case CueId.ZombieSnarl: case CueId.ZombieLunge:
                    return (Sin(58 + 7 * Sin(.8, t), t) * .55 + Sin(116, t) * .2 + low * 1.8) *
                        (r.Id == CueId.ZombieSnarl || r.Id == CueId.ZombieLunge ? Math.Exp(-t * 2) : Math.Pow(Math.Sin(Math.PI * t / Duration(r)), 2));
                case CueId.ZombieSniff:
                    return (noise - low) * .7 * (Pulse(t, .4, 18) + .3 * low);
                case CueId.Ventilation: case CueId.ExteriorWind:
                    return slower * 4 * (1 + .2 * Sin(.5, t));
                case CueId.RoomTone: case CueId.BlackoutTone:
                    return Sin(r.Id == CueId.BlackoutTone ? 43 : 50, t) * .08 + slower * .8;
                case CueId.FluorescentBuzz:
                    return (Sin(100, t) * .2 + Sin(200, t) * .05 + noise * .012) * (1 + .15 * Sin(2, t));
                case CueId.AlarmKlaxon:
                    return (Sin(440 + 110 * Sin(.5, t), t) * .65 + Sin(880, t) * .12) * (.35 + .65 * Math.Max(0, Sin(.5, t)));
                case CueId.BeaconChirp: case CueId.MonitorBeep:
                    return (Sin(r.Id == CueId.BeaconChirp ? 880 : 1100, t) * .65 + noise * .15) * Pulse(t, r.Loop ? 1 : 10, 12);
                case CueId.HeartbeatWake: case CueId.CaughtHeartbeat:
                    return Sin(52, t) * (Pulse(t, .9, 28) + .65 * Pulse(t, .9, 32, .18));
                case CueId.TensionDrone: case CueId.RevealDrone:
                    return (Sin(43, t) + Sin(43.5, t) + Sin(65, t) * .2) * .15;
                case CueId.WaterDrip:
                    return Sin(1200 - 300 * Math.Exp(-(t % 1) * 20), t) * Pulse(t, 1, 35) * .4;
                case CueId.ValveVentHiss: case CueId.PlugSpark: case CueId.BreakerOverload:
                    return (noise - slower) * .65 * Math.Exp(-t * (r.Id == CueId.PlugSpark ? 18 : 3));
                case CueId.PlankCreak: case CueId.ValveSqueal: case CueId.ReleaseBarGrind: case CueId.RestraintStrain: case CueId.CableDrag:
                    return Sin(170 + 25 * Sin(1, t), t) * .2 + low * .8 + noise * Pulse(t, .25, 60) * .1;
                case CueId.PaGarbledBreach: case CueId.PaCarrierWarning: case CueId.PaPatientZero:
                    // Original radio-static bursts; the approved caption carries the words, not counterfeit speech.
                    return (low - slower) * 2 * (.25 + .75 * Math.Max(0, Sin(3, t)));
                case CueId.UiSelect: case CueId.UiBack: case CueId.UiPause:
                    return Sin(r.Id == CueId.UiBack ? 440 : 660, t) * Math.Exp(-t * 25) * .5;
                case CueId.PowerThrum:
                    return Sin(50, t) * .4 * Math.Sin(Math.PI * t / Duration(r)) + slower;
                case CueId.ImpactLight: case CueId.ImpactHeavy: case CueId.DistantCrashLure:
                case CueId.WindowSlam: case CueId.DoorUnlatch: case CueId.ExitDoorOpen:
                case CueId.ZombieGlassTap: case CueId.RestraintSnap: case CueId.PlankPop:
                    double weight = r.Id == CueId.ImpactLight || r.Id == CueId.ZombieGlassTap || r.Id == CueId.DoorUnlatch ? 1.6 : 1;
                    return (Sin(73 * weight, t) * .7 + Sin(187 * weight, t) * .3 + noise * .8) * Math.Exp(-t * 9 * weight);
                default: return 0;
            }
        }
    }
}
