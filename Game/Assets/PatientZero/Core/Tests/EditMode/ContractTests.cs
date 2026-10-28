using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PatientZero.Core.Tests
{
    public class GameEventsTests
    {
        [TearDown]
        public void TearDown()
        {
            GameEvents.ResetAll();
        }

        [Test]
        public void ResetAll_ClearsEveryChannel()
        {
            var channels = AllChannels();
            Assert.Greater(channels.Count, 0);

            foreach (var field in channels)
            {
                object channel = field.GetValue(null);
                SubscribeDummy(channel);
                Assert.AreEqual(1, SubscriberCount(channel), field.Name + " did not accept a subscriber");
            }

            GameEvents.ResetAll();

            foreach (var field in channels)
            {
                Assert.AreEqual(0, SubscriberCount(field.GetValue(null)), field.Name + " is missing from GameEvents.ResetAll");
            }
        }

        [Test]
        public void EveryChannel_IsNamedAfterItsField()
        {
            foreach (var field in AllChannels())
            {
                object channel = field.GetValue(null);
                string name = (string)channel.GetType().GetProperty("Name").GetValue(channel, null);
                Assert.AreEqual(field.Name, name);
            }
        }

        private static List<FieldInfo> AllChannels()
        {
            var result = new List<FieldInfo>();
            foreach (var field in typeof(GameEvents).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                Type t = field.FieldType;
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EventChannel<>)) result.Add(field);
            }
            return result;
        }

        private static void SubscribeDummy(object channel)
        {
            Type payload = channel.GetType().GetGenericArguments()[0];
            Type actionType = typeof(Action<>).MakeGenericType(payload);
            MethodInfo noop = typeof(GameEventsTests).GetMethod("Noop", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(payload);
            Delegate handler = Delegate.CreateDelegate(actionType, noop);
            channel.GetType().GetMethod("Subscribe").Invoke(channel, new object[] { handler });
        }

        private static int SubscriberCount(object channel)
        {
            return (int)channel.GetType().GetProperty("SubscriberCount").GetValue(channel, null);
        }

        private static void Noop<T>(T payload)
        {
        }
    }

    public class ServicesTests
    {
        private sealed class PlainFake : IFake
        {
        }

        [SetUp]
        [TearDown]
        public void Clean()
        {
            CoreStaticReset.ResetStatics();
        }

        [Test]
        public void Register_ThenTryGet_ReturnsService()
        {
            var fake = new PlainFake();
            Assert.IsTrue(Services<IFake>.Register(fake));

            IFake found;
            Assert.IsTrue(Services<IFake>.TryGet(out found));
            Assert.AreSame(fake, found);
        }

        [Test]
        public void Register_SecondLiveInstance_IsRefusedAndLogged()
        {
            var first = new PlainFake();
            Services<IFake>.Register(first);

            LogAssert.Expect(LogType.Error, new Regex("Second IFake refused"));
            Assert.IsFalse(Services<IFake>.Register(new PlainFake()));
            Assert.AreSame(first, Services<IFake>.Instance);
        }

        [Test]
        public void Unregister_ByOtherInstance_IsIgnored()
        {
            var first = new PlainFake();
            Services<IFake>.Register(first);
            Services<IFake>.Unregister(new PlainFake());
            Assert.AreSame(first, Services<IFake>.Instance);
        }

        [Test]
        public void DestroyedUnityObject_IsTreatedAsMissing_AndCanBeReplaced()
        {
            var dead = ScriptableObject.CreateInstance<UnityFake>();
            Services<IFake>.Register(dead);
            UnityEngine.Object.DestroyImmediate(dead);

            IFake found;
            Assert.IsFalse(Services<IFake>.TryGet(out found));
            Assert.IsNull(found);

            var replacement = new PlainFake();
            Assert.IsTrue(Services<IFake>.Register(replacement));
            Assert.AreSame(replacement, Services<IFake>.Instance);
        }

        [Test]
        public void ResetStatics_ClearsRegisteredServices()
        {
            Services<IFake>.Register(new PlainFake());
            CoreStaticReset.ResetStatics();
            Assert.IsNull(Services<IFake>.Instance);
        }
    }

    public class GameStateRulesTests
    {
        private static readonly GameState[] All =
        {
            GameState.Boot, GameState.Calibration, GameState.Playing, GameState.Paused,
            GameState.Caught, GameState.Ending, GameState.Complete,
        };

        [Test]
        public void TransitionTable_MatchesArchitectureDoc()
        {
            var allowed = new HashSet<string>
            {
                "Boot>Calibration", "Boot>Playing",
                "Calibration>Playing",
                "Playing>Paused", "Playing>Caught", "Playing>Ending",
                "Paused>Playing", "Paused>Ending",
                "Caught>Playing",
                "Ending>Paused", "Ending>Complete",
                "Complete>Playing",
            };

            foreach (var from in All)
            {
                foreach (var to in All)
                {
                    bool expected = allowed.Contains(from + ">" + to);
                    Assert.AreEqual(expected, GameStateRules.CanTransition(from, to), from + " -> " + to);
                }
            }
        }

        [Test]
        public void Pause_OnlyFromPlayingOrEnding()
        {
            foreach (var state in All)
            {
                bool expected = state == GameState.Playing || state == GameState.Ending;
                Assert.AreEqual(expected, GameStateRules.CanPauseFrom(state), state.ToString());
                if (expected) Assert.IsTrue(GameStateRules.CanTransition(state, GameState.Paused));
            }
        }

        [Test]
        public void EveryState_CanEventuallyReachPlaying()
        {
            foreach (var start in All)
            {
                Assert.IsTrue(Reaches(start, GameState.Playing), start + " cannot reach Playing");
            }
        }

        private static bool Reaches(GameState from, GameState target)
        {
            var seen = new HashSet<GameState>(); // start unvisited so Playing must find a real loop back
            var frontier = new Queue<GameState>();
            frontier.Enqueue(from);
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var next in All)
                {
                    if (!GameStateRules.CanTransition(current, next) || !seen.Add(next)) continue;
                    if (next == target) return true;
                    frontier.Enqueue(next);
                }
            }
            return false;
        }
    }

    public class ExperienceSettingsTests
    {
        [Test]
        public void Defaults_AreComfortFirst()
        {
            var s = ExperienceSettings.Defaults;
            Assert.AreEqual(LocomotionMode.TeleportOnly, s.Locomotion);
            Assert.AreEqual(TurnMode.Snap, s.Turn);
            Assert.IsTrue(s.ComfortVignette);
            Assert.IsTrue(s.Subtitles);
            Assert.AreEqual(45, s.SnapTurnDegrees);
        }

        [Test]
        public void Defaults_AreAlreadySanitized()
        {
            var s = ExperienceSettings.Defaults;
            var clean = s.Sanitized();
            Assert.AreEqual(s.SnapTurnDegrees, clean.SnapTurnDegrees);
            Assert.AreEqual(s.HeightOffsetMeters, clean.HeightOffsetMeters, 0f);
            Assert.AreEqual(s.MasterVolume01, clean.MasterVolume01, 0f);
        }

        [Test]
        public void Sanitized_ClampsCorruptValues()
        {
            var s = ExperienceSettings.Defaults;
            s.SnapTurnDegrees = 50;
            s.HeightOffsetMeters = 9f;
            s.MasterVolume01 = float.NaN;
            s.SubtitleScale = -3f;
            s.Locomotion = (LocomotionMode)99;
            s.Scares = (ScareIntensity)(-1);

            var clean = s.Sanitized();

            Assert.AreEqual(45, clean.SnapTurnDegrees);
            Assert.AreEqual(0.6f, clean.HeightOffsetMeters, 1e-6f);
            Assert.AreEqual(1f, clean.MasterVolume01, 0f);
            Assert.AreEqual(0.75f, clean.SubtitleScale, 1e-6f);
            Assert.AreEqual(LocomotionMode.TeleportOnly, clean.Locomotion);
            Assert.AreEqual(ScareIntensity.Standard, clean.Scares);
        }

        [Test]
        public void SnapTurnOptions_Are30_45_60()
        {
            Assert.AreEqual(3, ExperienceSettings.SnapTurnOptionCount);
            Assert.AreEqual(30, ExperienceSettings.SnapTurnOption(0));
            Assert.AreEqual(45, ExperienceSettings.SnapTurnOption(1));
            Assert.AreEqual(60, ExperienceSettings.SnapTurnOption(2));
        }
    }

    public class CuesTests
    {
        private sealed class RecordingPlayer : IAudioCuePlayer
        {
            public CueId LastCue;
            public NoiseOrigin LastOrigin;
            public float LastIntensity;
            public int Stops;

            public CueHandle Play(CueId cue, Vector3 position, float intensity01, NoiseOrigin origin)
            {
                LastCue = cue;
                LastOrigin = origin;
                LastIntensity = intensity01;
                return new CueHandle(7);
            }

            public CueHandle PlayAttached(CueId cue, Transform follow, float intensity01, NoiseOrigin origin)
            {
                return Play(cue, Vector3.zero, intensity01, origin);
            }

            public void SetIntensity(CueHandle handle, float intensity01)
            {
                LastIntensity = intensity01;
            }

            public void Stop(CueHandle handle, float fadeSeconds)
            {
                Stops++;
            }
        }

        [SetUp]
        [TearDown]
        public void Clean()
        {
            CoreStaticReset.ResetStatics();
        }

        [Test]
        public void Play_RoutesToRegisteredPlayer()
        {
            var player = new RecordingPlayer();
            Services<IAudioCuePlayer>.Register(player);

            CueHandle handle = Cues.Play(CueId.PlankPop, new Vector3(1f, 2f, 3f), NoiseOrigin.Player, 0.5f);

            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(CueId.PlankPop, player.LastCue);
            Assert.AreEqual(NoiseOrigin.Player, player.LastOrigin);
            Assert.AreEqual(0.5f, player.LastIntensity, 0f);
        }

        [Test]
        public void Stop_InvalidHandle_DoesNotReachPlayer()
        {
            var player = new RecordingPlayer();
            Services<IAudioCuePlayer>.Register(player);
            Cues.Stop(default(CueHandle));
            Assert.AreEqual(0, player.Stops);
        }

        [Test]
        public void Play_WithoutPlayer_LogsOnceAndReturnsInvalidHandle()
        {
            LogAssert.Expect(LogType.Error, new Regex("No IAudioCuePlayer registered"));
            Assert.IsFalse(Cues.Play(CueId.PlankPop, Vector3.zero, NoiseOrigin.Player).IsValid);
            Assert.IsFalse(Cues.Play(CueId.PlankCreak, Vector3.zero, NoiseOrigin.Player).IsValid); // no second log
        }
    }

    public class IdsTests
    {
        [Test]
        public void CueIds_AreUnique()
        {
            AssertUniqueValues(typeof(CueId));
        }

        [Test]
        public void GateIds_AreUniqueAndPinned()
        {
            // Contract v1.1. Pinned numbers: Unity serialises enums as integers.
            AssertUniqueValues(typeof(GateId));
            Assert.AreEqual(0, (int)GateId.None);
            Assert.AreEqual(1, (int)GateId.Act1Door);
        }

        [Test]
        public void GateReleased_CarriesItsGate()
        {
            Assert.AreEqual(GateId.Act1Door, new GateReleased(GateId.Act1Door).Gate);
            Assert.AreEqual(GateId.None, default(GateReleased).Gate);
        }

        [Test]
        public void ObjectiveIds_AreUnique()
        {
            AssertUniqueValues(typeof(ObjectiveId));
        }

        [Test]
        public void Payloads_ClampTheirInputs()
        {
            Assert.AreEqual(1f, new ObjectiveProgress(ObjectiveId.Act2_ValveVented, 3f).Progress01, 0f);
            Assert.AreEqual(0f, new InfectionReveal(-1f).Amount01, 0f);
            Assert.AreEqual(0f, new NoiseEvent(Vector3.zero, -5f, CueId.None, NoiseOrigin.Player, 0f).RadiusMeters, 0f);
            Assert.AreEqual(0f, new HapticRequested(HandSide.Left, 2f, -1f).DurationSeconds, 0f);
            Assert.IsFalse(default(CueHandle).IsValid);
        }

        private static void AssertUniqueValues(Type enumType)
        {
            var seen = new HashSet<int>();
            foreach (var value in Enum.GetValues(enumType))
            {
                Assert.IsTrue(seen.Add((int)value), enumType.Name + " repeats value " + (int)value);
            }
        }
    }
}
