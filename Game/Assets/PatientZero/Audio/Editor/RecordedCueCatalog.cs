// Claude-owned integration (horror pass, 2026-11-02; E-21). Added to seat B's Audio area because only Audio may
// touch audio assets (check_rules "audio-owner"); AudioBuildSteps itself is unchanged.
using System;
using System.Collections.Generic;
using PatientZero.Core;
using UnityEditor;
using UnityEngine;

namespace PatientZero.Audio.Editor
{
    /// <summary>
    /// The recordings that replace placeholder cues (CC0 / CC-BY 3.0 from OpenGameArt, licence notes beside the files),
    /// handed to the AudioCuePlayer at compose time. A missing file fails the compose rather than shipping silence.
    /// </summary>
    public static class RecordedCueCatalog
    {
        public const string Root = "Assets/ThirdParty/OpenGameArt/";

        private static readonly Dictionary<CueId, string[]> Files = new Dictionary<CueId, string[]>
        {
            [CueId.ZombieGroan] = Moans(),
            [CueId.DistantInfected] = Moans(),
            [CueId.ZombieSnarl] = Numbered("ZombieVoices/Yell_ArcadeParty_", 1, 5),
            [CueId.ZombieLunge] = Numbered("ZombieVoices/Yell_ArcadeParty_", 6, 10),
            [CueId.JumpScareScream] = Numbered("ZombieVoices/Growl_ArcadeParty_", 1, 4),
            [CueId.VictimCrying] = new[] { "Crying/Crying_JeanMyna_VA.ogg" },
            [CueId.DistantScream] = new[]
            {
                "Screams/Scream_WuxiaScrub_1.ogg", "Screams/Scream_WuxiaScrub_2.ogg", "Screams/Scream_WuxiaScrub_3.ogg",
                "Screams/Scream_WuxiaScrub_4.ogg", "Screams/Scream_NocturnalVanguard.ogg", "Screams/Scream_Pauliuw.ogg",
                "Screams/Scream_Pauliuw_Reverb.ogg",
            },
            [CueId.CrowdPanic] = new[] { "Crowd/CrowdPanic_StarNinjas.ogg" },
            // E-24 (owner, 2026-11-11): the bulletin is cut to its three key lines, about 17 s (see the folder's LICENSE.md).
            [CueId.NewsBroadcast] = new[] { "News/ZombieNews_Zisongbr_Short.ogg" },
            [CueId.JumpScareStinger] = new[] { "Stingers/Stinger_Psychhead_VeryBassy2.ogg", "Stingers/Stinger_Psychhead_VeryBassy4.ogg" },
        };

        /// <summary>
        /// What the news anchor says, as timed speech captions. Transcribed by machine (faster-whisper "small", run locally) from
        /// the CC0 recording and not yet checked by a person. The third line names two counties the transcription could not make
        /// out, so its caption says "two county hospitals" instead.
        /// </summary>
        private static readonly Dictionary<CueId, AudioCuePlayer.TimedCaption[]> Transcripts = new Dictionary<CueId, AudioCuePlayer.TimedCaption[]>
        {
            // E-24: retimed for the short cut (the full recording's 18-29.7 s, then 37-42.6 s).
            [CueId.NewsBroadcast] = new[]
            {
                Say(.4f, 5.6f, "At 2:35 PM, reports from these counties have issued an outbreak of a highly contagious virus."),
                Say(6.1f, 5.5f, "Some reports claim the outbreak started in two county hospitals."),
                Say(12.1f, 5, "Reports have also claimed that those infected have acted out in extremely violent behavior."),
            },
        };

        private static AudioCuePlayer.TimedCaption Say(float at, float seconds, string text) =>
            new AudioCuePlayer.TimedCaption { At = at, Seconds = seconds, Text = text };

        /// <summary>Clips longer than this stay compressed in memory instead of being decoded on load.</summary>
        private const float LongClipSeconds = 8f;

        private static string[] Moans()
        {
            var moans = new List<string>(Numbered("ZombieVoices/Moan_Darsycho_", 1, 5));
            moans.AddRange(Numbered("ZombieVoices/Moan_EmoPreben_", 1, 3));
            return moans.ToArray();
        }

        private static string[] Numbered(string prefix, int from, int to)
        {
            var names = new string[to - from + 1];
            for (int i = from; i <= to; i++) names[i - from] = prefix + i + ".ogg";
            return names;
        }

        [SceneBuildStep(BuildScene.Bootstrap, 610)]
        public static void Build(Transform root)
        {
            // Every build step gets its own root, so the player AudioBuildSteps (600) added is found in the scene.
            AudioCuePlayer player = null;
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
            {
                AudioCuePlayer[] found = sceneRoot.GetComponentsInChildren<AudioCuePlayer>(true);
                if (found.Length > 0) { player = found[0]; break; }
            }

            if (!player) throw new InvalidOperationException("AudioBuildSteps (Bootstrap 600) must add the AudioCuePlayer before the recordings are wired.");
            var cues = new List<AudioCuePlayer.RecordedCue>();
            int count = 0;
            foreach (KeyValuePair<CueId, string[]> entry in Files)
            {
                var clips = new AudioClip[entry.Value.Length];
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i] = Load(Root + entry.Value[i]);
                    count++;
                }

                Transcripts.TryGetValue(entry.Key, out AudioCuePlayer.TimedCaption[] captions);
                if (captions != null && clips.Length != 1) throw new InvalidOperationException(entry.Key + ": a transcript needs exactly one recording.");
                cues.Add(new AudioCuePlayer.RecordedCue { Id = entry.Key, Clips = clips, Captions = captions });
            }

            player.Recorded = cues.ToArray();
            Debug.Log("[PZ] recorded cues: " + cues.Count + " cues, " + count + " clips");
        }

        private static AudioClip Load(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Recording missing or not imported as audio: " + path);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) throw new InvalidOperationException("No AudioClip at " + path);
            Configure(importer, clip.length);
            return clip;
        }

        private static void Configure(AudioImporter importer, float seconds)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            AudioClipLoadType load = seconds > LongClipSeconds ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            bool changed = !importer.forceToMono || importer.loadInBackground != (seconds > LongClipSeconds) ||
                           settings.loadType != load || settings.compressionFormat != AudioCompressionFormat.Vorbis ||
                           Mathf.Abs(settings.quality - .6f) > .001f;
            if (!changed) return;
            importer.forceToMono = true;
            importer.loadInBackground = seconds > LongClipSeconds;
            settings.loadType = load;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .6f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
