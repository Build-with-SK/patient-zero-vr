using System;
using UnityEngine;

namespace PatientZero.Core
{
    /// <summary>
    /// Comfort and accessibility settings shared by every team. Codex A stores and edits
    /// them; everyone reads them through ISettingsProvider and GameEvents.SettingsChanged.
    /// Defaults are the most comfortable choice, not the most "immersive" one.
    /// </summary>
    [Serializable]
    public struct ExperienceSettings
    {
        private static readonly int[] AllowedSnapTurnDegrees = { 30, 45, 60 };

        public static int SnapTurnOptionCount => AllowedSnapTurnDegrees.Length;

        public static int SnapTurnOption(int index)
        {
            return AllowedSnapTurnDegrees[index];
        }

        // Comfort
        public LocomotionMode Locomotion;
        public TurnMode Turn;
        public int SnapTurnDegrees;
        public bool ComfortVignette;
        public bool SeatedMode;
        public float HeightOffsetMeters;

        // Accessibility
        public HandSide DominantHand;
        public bool OneHandedMode;
        public bool PuzzleAssistAlwaysOn;
        public ScareIntensity Scares;
        public bool ReduceFlicker;
        public bool SoundCueVisuals;
        public bool Subtitles;
        public float SubtitleScale;
        public float DarknessBrightness01;

        // Audio and haptics
        public float MasterVolume01;
        public float EffectsVolume01;
        public float VoiceVolume01;
        public float HapticsStrength01;

        public static ExperienceSettings Defaults
        {
            get
            {
                return new ExperienceSettings
                {
                    Locomotion = LocomotionMode.TeleportOnly,
                    Turn = TurnMode.Snap,
                    SnapTurnDegrees = 45,
                    ComfortVignette = true,
                    SeatedMode = false,
                    HeightOffsetMeters = 0f,
                    DominantHand = HandSide.Right,
                    OneHandedMode = false,
                    PuzzleAssistAlwaysOn = false,
                    Scares = ScareIntensity.Standard,
                    ReduceFlicker = false,
                    SoundCueVisuals = false,
                    Subtitles = true,
                    SubtitleScale = 1f,
                    DarknessBrightness01 = 0.5f,
                    MasterVolume01 = 1f,
                    EffectsVolume01 = 1f,
                    VoiceVolume01 = 1f,
                    HapticsStrength01 = 1f,
                };
            }
        }

        /// <summary>
        /// Returns a copy with every value inside its legal range. Call on load and before
        /// broadcasting, so a corrupted PlayerPrefs entry cannot break comfort settings.
        /// </summary>
        public ExperienceSettings Sanitized()
        {
            ExperienceSettings s = this;
            if (!Enum.IsDefined(typeof(LocomotionMode), s.Locomotion)) s.Locomotion = LocomotionMode.TeleportOnly;
            if (!Enum.IsDefined(typeof(TurnMode), s.Turn)) s.Turn = TurnMode.Snap;
            if (!Enum.IsDefined(typeof(HandSide), s.DominantHand)) s.DominantHand = HandSide.Right;
            if (!Enum.IsDefined(typeof(ScareIntensity), s.Scares)) s.Scares = ScareIntensity.Standard;
            s.SnapTurnDegrees = NearestAllowedSnap(s.SnapTurnDegrees);
            s.HeightOffsetMeters = Mathf.Clamp(Finite(s.HeightOffsetMeters, 0f), -0.6f, 0.6f);
            s.SubtitleScale = Mathf.Clamp(Finite(s.SubtitleScale, 1f), 0.75f, 2f);
            s.DarknessBrightness01 = Mathf.Clamp01(Finite(s.DarknessBrightness01, 0.5f));
            s.MasterVolume01 = Mathf.Clamp01(Finite(s.MasterVolume01, 1f));
            s.EffectsVolume01 = Mathf.Clamp01(Finite(s.EffectsVolume01, 1f));
            s.VoiceVolume01 = Mathf.Clamp01(Finite(s.VoiceVolume01, 1f));
            s.HapticsStrength01 = Mathf.Clamp01(Finite(s.HapticsStrength01, 1f));
            return s;
        }

        private static int NearestAllowedSnap(int degrees)
        {
            int best = AllowedSnapTurnDegrees[0];
            for (int i = 1; i < AllowedSnapTurnDegrees.Length; i++)
            {
                int candidate = AllowedSnapTurnDegrees[i];
                if (Math.Abs(candidate - degrees) < Math.Abs(best - degrees)) best = candidate;
            }
            return best;
        }

        private static float Finite(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }
    }
}
