using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.XR;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;

namespace PatientZero.Tests
{
    public sealed class SystemsTests
    {
        public static IEnumerable Scenarios()
        {
            foreach (var scenario in CoreScenarios.All) yield return new TestCaseData(scenario).SetName(scenario.Name);
        }
        [TestCaseSource(nameof(Scenarios))]
        public void CoreScenario(CoreScenarios.Scenario scenario) => scenario.Run();

        [Test]
        public void MissingClipIsReportedByAssetBank()
        {
            var bank = ScriptableObject.CreateInstance<AudioCueBank>();
            try
            {
                Assert.That(bank.HasAllAssets(out var missing), Is.False);
                Assert.That(missing, Is.EqualTo(AudioCue.FootLeft));
            }
            finally { Object.DestroyImmediate(bank); }
        }
        [Test]
        public void CompleteBankContainsAllSixCues()
        {
            var bank = ScriptableObject.CreateInstance<AudioCueBank>();
            var clip = AudioClip.Create("Test", 16, 1, 8000, false);
            try
            {
                bank.Entries = new AudioCueBank.Entry[6];
                for (int i = 0; i < 6; i++) bank.Entries[i] = new AudioCueBank.Entry { Cue = (AudioCue)i, Clip = clip };
                Assert.That(bank.HasAllAssets(out _), Is.True);
            }
            finally { Object.DestroyImmediate(clip); Object.DestroyImmediate(bank); }
        }
        [Test]
        public void PackageAssetsImportWithNoMissingScriptsOrAudio()
        {
            const string root = "Packages/com.patientzero.xr/";
            string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { root + "Prefabs" });
            Assert.That(prefabs.Length, Is.EqualTo(8), "All eight package templates must import");
            foreach (string guid in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(prefab, Is.Not.Null);
                foreach (var script in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    Assert.That(script, Is.Not.Null, "Missing script in " + prefab.name);
            }
            var bank = AssetDatabase.LoadAssetAtPath<AudioCueBank>(root + "Audio/DiagnosticCueBank.asset");
            Assert.That(bank, Is.Not.Null); Assert.That(bank.HasAllAssets(out _), Is.True);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(root + "Audio/PatientZero.mixer");
            Assert.That(mixer, Is.Not.Null);
            foreach (string group in new[] { "Zombie", "Ambience", "Encounters" })
                Assert.That(mixer.FindMatchingGroups(group).Length, Is.EqualTo(1), "Missing mixer group " + group);
        }
    }
}
