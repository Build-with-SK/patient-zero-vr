using System;
using PatientZero.Core;

namespace PatientZero.Tests
{
    public static class CoreScenarios
    {
        public readonly struct Scenario
        {
            public readonly string Name;
            public readonly Action Run;
            public Scenario(string name, Action run) { Name = name; Run = run; }
            public override string ToString() => Name;
        }
        private sealed class Listener : INoiseListener
        {
            public int Count;
            public NoiseEvent Last;
            public void Hear(in NoiseEvent noise) { Count++; Last = noise; }
        }
        private static readonly WorldPoint Origin = new WorldPoint(0, 0, 0);
        private static NoiseEvent Sound(double time = 0, float intensity = .9f, float radius = 10,
            NoiseCategory category = NoiseCategory.Bottle, int source = 1, float x = 2) =>
            new NoiseEvent(new WorldPoint(x, 0, 0), intensity, radius, time, category, source);
        private static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }
        private static void Equal(ZombieState expected, ZombieBrain brain) => Check(brain.State == expected, $"Expected {expected}, got {brain.State}");
        public static readonly Scenario[] All =
        {
            new Scenario("noise attenuates with distance and stops at radius", Propagation),
            new Scenario("invalid positions intensity radius category and timestamps rejected", InvalidNoise),
            new Scenario("stale and future noise rejected", StaleNoise),
            new Scenario("source cooldown prevents collision flooding", Cooldown),
            new Scenario("fixed source capacity cannot evict unexpired cooldowns", Capacity),
            new Scenario("listener registration capacity and removal", Listeners),
            new Scenario("hearing threshold prevents distant reactions", Threshold),
            new Scenario("investigation timeout enters search then idle", InvestigationTimeout),
            new Scenario("arrival at bottle starts search", Arrival),
            new Scenario("loud player noise follows all six states", FullStatePath),
            new Scenario("no noise means no chase", BlindIdle),
            new Scenario("chase follows last heard point then expires", MemoryExpires),
            new Scenario("thrown bottle redirects chase without becoming player target", Distraction),
            new Scenario("unreachable path enters search", PathFailure),
            new Scenario("attack windup cannot deal repeated damage", SingleStrike),
            new Scenario("retreat during windup avoids damage", Retreat),
            new Scenario("head-height voice can be approached without treating height as ground distance", ElevatedVoice),
            new Scenario("backward clocks cannot prolong or rewind state", Monotonic),
            new Scenario("tracking loss cancels selection immediately", TrackingLoss),
            new Scenario("controller fallback waits for stable tracking", ControllerFallback),
            new Scenario("tracking flicker cannot restore selection early", TrackingFlicker),
            new Scenario("focus loss cancels and restoration debounces", FocusLoss),
            new Scenario("missing audio assets and mute safely skip playback", MissingAudio),
            new Scenario("comfort distance rejects close or invalid encounters", ComfortDistance),
            new Scenario("noise dispatch allocates zero bytes after warmup", DispatchAllocations),
            new Scenario("AI ticks allocate zero bytes after warmup", TickAllocations)
        };
        private static void Propagation()
        {
            var n = Sound(intensity: 1, x: 0);
            Check(Math.Abs(n.LevelAt(new WorldPoint(5, 0, 0)) - .5f) < .00001f, "Wrong attenuation");
            Check(n.LevelAt(new WorldPoint(10, 0, 0)) == 0 && n.LevelAt(new WorldPoint(11, 0, 0)) == 0, "Outside radius heard");
        }
        private static void InvalidNoise()
        {
            var bus = new NoiseBus();
            Check(!bus.Publish(Sound(intensity: float.NaN), 0), "NaN intensity accepted");
            Check(!bus.Publish(Sound(intensity: 2), 0), "Out of range intensity accepted");
            Check(!bus.Publish(Sound(radius: 0), 0), "Zero radius accepted");
            Check(!bus.Publish(Sound(x: float.NaN), 0), "NaN point accepted");
            Check(!bus.Publish(Sound(category: (NoiseCategory)99), 0), "Invalid category accepted");
            Check(!bus.Publish(Sound(time: double.NaN), 0), "NaN time accepted");
            Check(!bus.Publish(Sound(), double.NaN), "NaN clock accepted");
        }
        private static void StaleNoise()
        {
            var bus = new NoiseBus();
            Check(!bus.Publish(Sound(time: 2), 1), "Future event accepted");
            Check(!bus.Publish(Sound(), 2), "Old event accepted");
        }
        private static void Cooldown()
        {
            var bus = new NoiseBus(); var listener = new Listener(); bus.Register(listener);
            Check(bus.Publish(Sound(), 0), "First failed");
            Check(!bus.Publish(Sound(time: .1), .1), "Flood accepted");
            Check(bus.Publish(Sound(time: .2), .2), "Cooldown never clears");
            Check(listener.Count == 2, "Unexpected deliveries");
        }
        private static void Capacity()
        {
            var bus = new NoiseBus(sourceCapacity: 1);
            Check(bus.Publish(Sound(), 0), "First failed");
            Check(!bus.Publish(Sound(source: 2, time: .01), .01), "Unexpired slot evicted");
            Check(!bus.Publish(Sound(time: .02), .02), "Cooldown bypassed");
            Check(bus.Publish(Sound(source: 2, time: .2), .2), "Expired slot not reusable");
        }
        private static void Listeners()
        {
            var bus = new NoiseBus(listenerCapacity: 1); var a = new Listener(); var b = new Listener();
            Check(bus.Register(a) && bus.Register(a) && !bus.Register(b), "Bad capacity");
            bus.Unregister(a); Check(bus.Register(b), "Slot not released"); bus.Publish(Sound(), 0);
            Check(a.Count == 0 && b.Count == 1, "Unregister failed");
        }
        private static void Threshold()
        {
            var b = new ZombieBrain(new ZombieSettings());
            Check(!b.Hear(Sound(intensity: .1f), Origin, 0), "Quiet noise heard"); Equal(ZombieState.Idle, b);
        }
        private static void InvestigationTimeout()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(), Origin, 0);
            Equal(ZombieState.Investigate, b); b.Tick(7, Origin); Equal(ZombieState.Search, b);
            b.Tick(11, Origin); Equal(ZombieState.Idle, b);
        }
        private static void Arrival()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(), Origin, 0);
            b.Tick(.1, new WorldPoint(2, 0, 0)); Equal(ZombieState.Search, b);
        }
        private static ZombieBrain Attacking()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(category: NoiseCategory.Footstep, x: 1), Origin, 0);
            b.Tick(.1, Origin); Equal(ZombieState.Chase, b);
            b.Tick(.2, Origin); Equal(ZombieState.Attack, b); return b;
        }
        private static void FullStatePath()
        {
            var b = Attacking(); Check(b.Tick(1, Origin), "No strike"); Equal(ZombieState.Recover, b);
            b.Tick(3, Origin); Equal(ZombieState.Search, b); b.Tick(7, Origin); Equal(ZombieState.Idle, b);
            Check(b.Transitions == 6, "Wrong transition count");
        }
        private static void BlindIdle()
        {
            var b = new ZombieBrain(new ZombieSettings()); for (int i = 0; i < 100; i++) b.Tick(i, Origin);
            Equal(ZombieState.Idle, b);
        }
        private static void MemoryExpires()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(category: NoiseCategory.Voice), Origin, 0);
            b.Tick(.1, Origin); Equal(ZombieState.Chase, b); b.Tick(3, Origin); Equal(ZombieState.Investigate, b);
            Check(b.Target.X == 2, "Target moved without noise");
        }
        private static void Distraction()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(category: NoiseCategory.Footstep), Origin, 0);
            b.Tick(.1, Origin); Equal(ZombieState.Chase, b);
            b.Hear(Sound(time: .2, source: 42, x: -3), Origin, .2);
            Equal(ZombieState.Investigate, b); Check(b.TargetSource == 42 && b.Target.X == -3, "Distraction not applied");
            b.Tick(.3, new WorldPoint(-3, 0, 0)); Equal(ZombieState.Search, b);
        }
        private static void PathFailure()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(), Origin, 0);
            b.Tick(.1, Origin, true); Equal(ZombieState.Search, b);
        }
        private static void SingleStrike()
        {
            var b = Attacking(); Check(b.Tick(1, Origin), "No strike");
            Check(!b.Tick(1.1, Origin) && !b.Tick(1.2, Origin), "Repeated strike");
        }
        private static void Retreat()
        {
            var b = Attacking(); Check(!b.Tick(1, new WorldPoint(-5, 0, 0)), "Out of range strike"); Equal(ZombieState.Recover, b);
        }
        private static void ElevatedVoice()
        {
            var b = new ZombieBrain(new ZombieSettings());
            var voice = new NoiseEvent(new WorldPoint(1, 1.7f, 0), 1, 10, 0, NoiseCategory.Voice, 1);
            Check(b.Hear(in voice, Origin, 0), "Head-height voice not heard");
            b.Tick(.1, Origin); b.Tick(.2, Origin); Equal(ZombieState.Attack, b);
        }
        private static void Monotonic()
        {
            var b = new ZombieBrain(new ZombieSettings()); b.Hear(Sound(time: 1), Origin, 1); b.Tick(3, Origin);
            Check(!b.Hear(Sound(time: 2, x: 8), Origin, 2), "Time reversed");
            Check(b.Target.X == 2, "Target changed on backward time");
        }
        private static void TrackingLoss()
        {
            var g = new TrackingRecovery(); g.Step(true, false, 0); Check(g.Step(true, false, .3).Source == InteractionSource.Hand, "Hand never enabled");
            var lost = g.Step(false, false, .31); Check(lost.CancelSelection && lost.Source == InteractionSource.None, "Loss didn't cancel");
        }
        private static void ControllerFallback()
        {
            var g = new TrackingRecovery(); g.Step(true, false, 0); g.Step(true, false, .3);
            var swap = g.Step(false, true, .4); Check(swap.CancelSelection && swap.Source == InteractionSource.None, "Unsafe instant switch");
            Check(g.Step(false, true, .7).Source == InteractionSource.Controller, "Fallback failed");
        }
        private static void TrackingFlicker()
        {
            var g = new TrackingRecovery(); g.Step(true, false, 0); g.Step(false, false, .1); g.Step(true, false, .2);
            Check(g.Step(true, false, .3).Source == InteractionSource.None, "Flicker restored too early");
            Check(g.Step(true, false, .5).Source == InteractionSource.Hand, "Stable hand never restored");
        }
        private static void FocusLoss()
        {
            var g = new TrackingRecovery(); g.Step(true, true, 0); g.Step(true, true, .3);
            Check(g.Step(true, true, .4, false).CancelSelection, "Focus loss failed");
            Check(g.Step(true, true, .5).Source == InteractionSource.None, "Resume not debounced");
        }
        private static void MissingAudio()
        {
            Check(!AudioPolicy.CanPlay(false, 1, 1), "Missing asset played");
            Check(!AudioPolicy.CanPlay(true, 0, 1) && !AudioPolicy.CanPlay(true, 1, 0), "Mute ignored");
            Check(!AudioPolicy.CanPlay(true, float.NaN, 1), "NaN volume accepted");
            Check(AudioPolicy.CanPlay(true, .5f, .5f), "Valid audio rejected");
        }
        private static void ComfortDistance()
        {
            Check(!AudioPolicy.SafeEncounter(Origin, new WorldPoint(1, 0, 0), 2.5f), "Close encounter allowed");
            Check(AudioPolicy.SafeEncounter(Origin, new WorldPoint(0, 0, -3), 2.5f), "Safe behind cue rejected");
            Check(!AudioPolicy.SafeEncounter(Origin, new WorldPoint(float.NaN, 0, 0), 2.5f), "Invalid anchor accepted");
        }
        private static void DispatchAllocations()
        {
            var bus = new NoiseBus(cooldown: 0); var listener = new Listener(); bus.Register(listener); var n = Sound();
            for (int i = 0; i < 100; i++) bus.Publish(in n, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) bus.Publish(in n, 0);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Dispatch allocated");
        }
        private static void TickAllocations()
        {
            var b = new ZombieBrain(new ZombieSettings());
            for (int i = 0; i < 100; i++)
            {
                var n = Sound(time: i * .1, category: NoiseCategory.Voice, x: 1);
                b.Hear(in n, Origin, i * .1); b.Tick(i * .1, Origin);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 100; i < 10100; i++)
            {
                var n = Sound(time: i * .1, category: NoiseCategory.Voice, x: 1);
                b.Hear(in n, Origin, i * .1); b.Tick(i * .1, Origin);
            }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "AI tick allocated");
        }
    }
}
