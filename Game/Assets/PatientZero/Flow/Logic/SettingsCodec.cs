using System;
using System.Globalization;
using System.Text;
using PatientZero.Core;
namespace PatientZero.Flow.Logic
{
    /// <summary>Version-tolerant settings storage; used only on settings load/save, never per frame.</summary>
    public static class SettingsCodec
    {
        public static string Encode(ExperienceSettings settings)
        {
            var s=settings.Sanitized();var b=new StringBuilder();
            b.Append("Locomotion=").Append(s.Locomotion.ToString()).Append("; ");
            b.Append("Turn=").Append(s.Turn.ToString()).Append("; ");
            b.Append("SnapTurnDegrees=").Append(s.SnapTurnDegrees.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("ComfortVignette=").Append(s.ComfortVignette.ToString()).Append("; ");
            b.Append("SeatedMode=").Append(s.SeatedMode.ToString()).Append("; ");
            b.Append("HeightOffsetMeters=").Append(s.HeightOffsetMeters.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("DominantHand=").Append(s.DominantHand.ToString()).Append("; ");
            b.Append("OneHandedMode=").Append(s.OneHandedMode.ToString()).Append("; ");
            b.Append("PuzzleAssistAlwaysOn=").Append(s.PuzzleAssistAlwaysOn.ToString()).Append("; ");
            b.Append("Scares=").Append(s.Scares.ToString()).Append("; ");
            b.Append("ReduceFlicker=").Append(s.ReduceFlicker.ToString()).Append("; ");
            b.Append("SoundCueVisuals=").Append(s.SoundCueVisuals.ToString()).Append("; ");
            b.Append("Subtitles=").Append(s.Subtitles.ToString()).Append("; ");
            b.Append("SubtitleScale=").Append(s.SubtitleScale.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("DarknessBrightness01=").Append(s.DarknessBrightness01.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("MasterVolume01=").Append(s.MasterVolume01.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("EffectsVolume01=").Append(s.EffectsVolume01.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("VoiceVolume01=").Append(s.VoiceVolume01.ToString(CultureInfo.InvariantCulture)).Append("; ");
            b.Append("HapticsStrength01=").Append(s.HapticsStrength01.ToString(CultureInfo.InvariantCulture)).Append("; ");
            return b.ToString();
        }
        public static ExperienceSettings Decode(string text)
        {
            var s=ExperienceSettings.Defaults;if(string.IsNullOrEmpty(text))return s;
            foreach(var pair in text.Split(';'))
            {
                int split=pair.IndexOf('=');if(split<0)continue;string key=pair.Substring(0,split).Trim(),value=pair.Substring(split+1).Trim();
                switch(key)
                {
                    case "Locomotion": if(Enum.TryParse<LocomotionMode>(value,out var parsedLocomotion))s.Locomotion=parsedLocomotion; break;
                    case "Turn": if(Enum.TryParse<TurnMode>(value,out var parsedTurn))s.Turn=parsedTurn; break;
                    case "SnapTurnDegrees": if(int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var parsedSnapTurnDegrees))s.SnapTurnDegrees=parsedSnapTurnDegrees; break;
                    case "ComfortVignette": if(bool.TryParse(value,out var parsedComfortVignette))s.ComfortVignette=parsedComfortVignette; break;
                    case "SeatedMode": if(bool.TryParse(value,out var parsedSeatedMode))s.SeatedMode=parsedSeatedMode; break;
                    case "HeightOffsetMeters": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedHeightOffsetMeters))s.HeightOffsetMeters=parsedHeightOffsetMeters; break;
                    case "DominantHand": if(Enum.TryParse<HandSide>(value,out var parsedDominantHand))s.DominantHand=parsedDominantHand; break;
                    case "OneHandedMode": if(bool.TryParse(value,out var parsedOneHandedMode))s.OneHandedMode=parsedOneHandedMode; break;
                    case "PuzzleAssistAlwaysOn": if(bool.TryParse(value,out var parsedPuzzleAssistAlwaysOn))s.PuzzleAssistAlwaysOn=parsedPuzzleAssistAlwaysOn; break;
                    case "Scares": if(Enum.TryParse<ScareIntensity>(value,out var parsedScares))s.Scares=parsedScares; break;
                    case "ReduceFlicker": if(bool.TryParse(value,out var parsedReduceFlicker))s.ReduceFlicker=parsedReduceFlicker; break;
                    case "SoundCueVisuals": if(bool.TryParse(value,out var parsedSoundCueVisuals))s.SoundCueVisuals=parsedSoundCueVisuals; break;
                    case "Subtitles": if(bool.TryParse(value,out var parsedSubtitles))s.Subtitles=parsedSubtitles; break;
                    case "SubtitleScale": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedSubtitleScale))s.SubtitleScale=parsedSubtitleScale; break;
                    case "DarknessBrightness01": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedDarknessBrightness01))s.DarknessBrightness01=parsedDarknessBrightness01; break;
                    case "MasterVolume01": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedMasterVolume01))s.MasterVolume01=parsedMasterVolume01; break;
                    case "EffectsVolume01": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedEffectsVolume01))s.EffectsVolume01=parsedEffectsVolume01; break;
                    case "VoiceVolume01": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedVoiceVolume01))s.VoiceVolume01=parsedVoiceVolume01; break;
                    case "HapticsStrength01": if(float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsedHapticsStrength01))s.HapticsStrength01=parsedHapticsStrength01; break;
                }
            }
            return s.Sanitized();
        }
    }
}
