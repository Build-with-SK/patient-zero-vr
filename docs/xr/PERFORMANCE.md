# Performance budget and profiling record

Target: **at least 72 FPS on standalone Quest**, corresponding to **13.89 ms per frame**.
This target has not been verified on hardware. No real-headset result exists in this handoff.

## Implemented controls

- Default cap: 12 active zombie sensors/FSMs; population beyond the cap is suspended. Pool visual
  actor roots in the scene population controller and retry admission on capacity changes.
- AI: 10 Hz default, configurable 2–20 Hz, phased initial ticks; navigation destinations at most every
  0.35 s when needed. Animator rendering remains per frame. Hear callbacks update target memory
  immediately; movement/state progression uses scheduled ticks.
- Bus: fixed listener/source arrays; per-emitter and per-source cooldowns; no unbounded event queues.
- Audio: 12 preallocated voices, at most one occlusion ray per playing voice per 0.15 s by default;
  no runtime instantiate/destroy for repeated sounds. Pool exhaustion drops new cues.
- Attacks: 8-slot nonalloc collider overlap at strike time, not per frame; configure PlayerMask so this
  bound covers the player's collider set. Bodies behind configured wall/glass blockers receive no damage.
- Tracking: fixed prop registry, cached input lists and selection components. SDK transform/throw
  work remains SDK-owned. Allocation during initial setup and profile export is outside gameplay loops.
- Profiler markers: `PatientZero.AI.Tick`, `PatientZero.Audio.Occlusion`.

## Cloud measurement

Portable Release run on this Linux host: **1,200,000 idle core ticks in 31.06 ms** on the recorded run.
This measures only a pure C# idle FSM, excluding NavMesh, rendering, animation, physics, SDK tracking,
audio and Android runtime. It establishes neither a Quest CPU budget nor a frame rate. Shared tests
also measured zero steady-state allocation in dispatch and active hearing/FSM iterations.

## Required device run

1. Use the integrated Android IL2CPP ARM64 build with the intended graphics API, stereo rendering,
   HRTF/audio assets and quality settings. Record git hash, Unity/Meta/OpenXR versions, Quest model,
   OS, refresh rate, render scale, dynamic resolution and CPU/GPU levels. Verify requested refresh
   is actually 72 Hz. Do not derive this from `Application.targetFrameRate` alone.
2. Attach Unity Profiler to a Development Build without Deep Profile. Also use Meta's on-device
   performance tooling for GPU time, dropped/stale frames and thermal behavior. Profile a representative
   release build afterwards, since development instrumentation can affect timing.
3. Warm up two minutes. Capture five minutes for 1, 6 and 12 active zombies: idle, simultaneous hearing,
   chase/navigation, repeated bottle collisions, two-hand interaction, max audio voices and all five
   encounters. Include occlusion-heavy corridors, blocked paths, tracking loss and pause/resume.
4. Add `QuestPerformanceRecorder`, bind NoiseManager and call `BeginCapture`/`EndCapture` from a
   debug control. It keeps up to 30,000 samples and writes CSV to `Application.persistentDataPath`.
   The export records wall-frame time, previous main-thread counter, GC bytes and active population;
   unavailable counters are `-1`. Export happens after capture and allocates then. Retrieve via adb
   using the app's package/persistent-data path. Use on-device tools separately for GPU metrics.
5. Report p50/p95/p99 wall-frame, main-thread and GPU times; frame misses; steady GC; AI and occlusion
   marker cost; memory/voice count; thermals at start/end. Tune population/TickHz/repath/occlusion and
   the application rendering budget using those measurements. Do not claim 72 FPS from average alone.

| Build / device | Population | Capture duration | CPU p95 | GPU p95 | Frame p95 | GC/frame | 72 Hz sustained |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Not run — no Quest attached | — | — | — | — | — | — | **Unverified** |

Keep real CSV/profiler exports with the build hash outside source control or in an explicitly chosen
artifact store. Fill this table only with measured device data, including any failed target.
