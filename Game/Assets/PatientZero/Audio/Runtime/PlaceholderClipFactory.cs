// UNVERIFIED IN EDITOR
using PatientZero.Audio.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Audio
{
    /// <summary>Unity clip adapter for original, tested mono synthesis. All allocations occur at load.</summary>
    public static class PlaceholderClipFactory
    {
        public static AudioClip Create(CueDefinition row)
        {
            if (row.Id == CueId.None) return null;
            var samples = ProceduralSound.Render(row);
            var clip = AudioClip.Create("Placeholder_" + row.Id, samples.Length, 1, ProceduralSound.SampleRate, false);
            clip.SetData(samples, 0); return clip;
        }
    }
}
