using System;

namespace PatientZero.Core
{
    public enum AudioCue { FootLeft, FootRight, Breathing, Scratching, Alarm, Growl }
    public static class AudioPolicy
    {
        public static bool CanPlay(bool hasAsset, float masterVolume, float comfortVolume) =>
            hasAsset && WorldPoint.Finite(masterVolume) && WorldPoint.Finite(comfortVolume) &&
            masterVolume > 0 && comfortVolume > 0;
        public static bool SafeEncounter(WorldPoint listener, WorldPoint source, float minDistance) =>
            listener.IsFinite && source.IsFinite && WorldPoint.Finite(minDistance) && minDistance >= 0 &&
            WorldPoint.Distance(listener, source) >= minDistance;
        public static float Decibels(float linear) => 20 * (float)Math.Log10(Math.Max(.0001f, Math.Min(1, linear)));
    }
}
