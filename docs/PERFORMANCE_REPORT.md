# Performance Report

Last updated 2026-10-28 by Claude · **No measurements exist yet**: there is no build and no headset connected. Every result below is NOT MEASURED until a device capture is recorded with the evidence the device rule in `TEST_REPORT.md` requires.

## Target

* Device: **Quest 3 / 3S** primary, Quest 2 best effort. The owner has not yet said which headset is available; the budgets below are sized for Quest 2 so they hold on all three.
* Refresh rate: **72 Hz** (13.9 ms per frame). 90 Hz is a stretch goal on Quest 3 only.

## Budgets

| Metric | Budget | Why |
|---|---|---|
| Frame rate in Playing | ≥ 71.5 fps average; < 1 % frames late over a 120-second run | the runtime reprojects missed frames, and repeated misses cause judder |
| CPU main thread | ≤ 8.0 ms | headroom for render thread and spikes |
| GPU | ≤ 11.0 ms | fixed foveation on, MSAA 4× |
| Draw calls / SetPass | ≤ 100 / ≤ 40 | one zombie per act keeps skinned meshes low |
| Visible triangles | ≤ 250 k | small ward, greybox first |
| Realtime lights | ≤ 6 in Ward, ≤ 3 per object, 0 shadow-casting | Forward path on mobile |
| GC allocation | **0 B per frame** in Playing; act transitions only under fade | GC spikes cause dropped frames |
| Ward reload (restart) | completes under the fade; ≤ 1.5 s to fade-in | restart must feel immediate |
| Resident memory | ≤ 1.0 GB | far below the app limit on every target |
| Texture memory | ≤ 200 MB | see the asset manifest |
| Audio | ≤ 24 real voices, ≤ 12 spatialized, ≤ 48 MB loaded; audio thread ≤ 1.5 ms | HRTF cost per voice |
| Physics | fixed step 1/72 s, ≤ 30 active rigidbodies, FixedUpdate ≤ 1.0 ms | — |
| Thermals | no throttle during 3 consecutive runs (≈ 6 min) | players replay |

## How it will be measured

| What | Tool | Procedure |
|---|---|---|
| Frame rate, CPU/GPU level, dropped frames | OVR Metrics Tool (on headset) + `adb logcat -s VrApi` | full run ×3 (normal, caught once, hands-only), screen-recorded overlay |
| Per-system CPU, GC allocation | Unity Profiler over ADB on a development build (deep profiling off) | 60 s capture per act. GC Alloc column must read 0 in Playing frames |
| Memory | Unity Memory Profiler snapshots | snapshot at Act 1 start, after 10 restarts, after Complete. Growth ≤ 5 % |
| Draw calls, triangles | Frame Debugger (Editor) + RenderDoc/Profiler on device | worst view of each act: B-7 looking at the door, corridor looking east, darkness, vestibule facing the exit |
| Thermals | OVR Metrics Tool | 3 back-to-back runs |

## Results

| Metric | Act 1 | Act 2 | Act 3 | Act 4 | Ending | Status |
|---|---|---|---|---|---|---|
| Average fps | — | — | — | — | — | NOT MEASURED (BLOCKED: no build or device) |
| Late frames % | — | — | — | — | — | NOT MEASURED |
| Main thread ms (p95) | — | — | — | — | — | NOT MEASURED |
| GPU ms (p95) | — | — | — | — | — | NOT MEASURED |
| GC bytes per frame | — | — | — | — | — | NOT MEASURED |
| Draw calls (worst view) | — | — | — | — | — | NOT MEASURED |
| Visible triangles (worst) | — | — | — | — | — | NOT MEASURED |
| Resident memory | — | — | — | — | — | NOT MEASURED |
| Ward reload time | — | — | — | — | — | NOT MEASURED |

## Build configuration enforced by tooling

`QuestBuild.Configure` sets the configuration and `QuestBuild.Validate` checks it. Both are UNVERIFIED IN EDITOR until a Unity 6.3 Editor runs them.

| Setting | Value | Set by |
|---|---|---|
| Scripting backend / architecture | IL2CPP / ARM64 | `QuestBuild.Configure` |
| Graphics API | Vulkan only | same |
| Stereo rendering | OpenXR single pass instanced (multiview) | same |
| Colour space | Linear | same |
| URP asset | MSAA 4×, HDR off, render scale 1.0, no depth or opaque texture, SRP Batcher on | same (`Integration/Settings/PZ_URP.asset`) |
| Fixed foveated rendering | **not configured yet**: set at runtime through the XR display subsystem. Proposed as a Claude-owned runtime component at integration | open |

Measurement note: `build-apk` produces a **development** build, which carries profiler overhead. The 72 fps gate (Q-13) is judged on a non-development build of the same commit, with the development build used only for per-system profiling.

## Known risks to these budgets

| Risk | Where | Planned response |
|---|---|---|
| HRTF cost with many loops (breath, shuffle, drip, alarm, beacon) | Acts 3–4 | voice priority; non-critical loops fall back to non-spatialized |
| Realtime lights without baking in the corridor | Act 2 | ≤ 3 lights per object; bake static lighting if the GPU is over budget |
| Fog plus transparency in Blackout | Act 3 | fog in the shaders only; no transparent fog volumes |
| Skinned zombie with too many bones | all | ≤ 40 bones, GPU skinning, one zombie active per act |
| Ward reload hitch | restarts | async load under the fade; pre-warm pools |
