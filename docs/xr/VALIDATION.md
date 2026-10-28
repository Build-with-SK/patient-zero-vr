# Validation record and acceptance checks

## Executed in this cloud machine

On 2026-10-28, Linux x64, verified official .NET SDK 8.0.427:

- `dotnet run --project Tests/Portable -c Release`: **26 passed, 0 failed, 0 skipped**.
- The shared suite tests distance/radius propagation, invalid/stale events, cooldowns, capacity, hearing
  thresholds, six-state progression, timeout, arrival, blindness, memory expiry, bottle distraction,
  unreachable-path response, attack cadence/range, monotonic clocks, tracking loss/flicker/fallback,
  focus loss, missing audio, mute and comfort distance.
- Core noise dispatch and active hearing/FSM loops: **0 allocated bytes after warmup**, measured with
  `GC.GetAllocatedBytesForCurrentThread` over 10,000 iterations each. This does not cover Unity/SDK code.
- `python3 tools/validate_assets.py`: reference integrity, unique GUIDs, all eight prefab templates,
  all six nonempty mono WAVs, and repeatable asset generation pass. It does not emulate Unity importing.
- Shell scripts parse with `bash -n`; no game scene was created or modified.

## Not executed

| Required check | Status / prerequisite |
| --- | --- |
| Unity import and C# compilation of adapters/editor tooling | Not run: Unity editor absent |
| Unity EditMode / PlayMode | Not run: licensed editor and Unity registry access required; expected minimum 29 / 7 tests |
| SDK XR and prop prefab generation | Not run: requires SDK source prefab selection in Unity |
| Android IL2CPP build and installation | Not run: editor, Android modules and target device required |
| Hand/controller, pinch, grab, throw, two-hand, valve/lever | Not run on headset |
| Headphone positional/occlusion listening | Not run; no physical listening hardware in cloud |
| Quest 72 Hz CPU/GPU/thermal profiling | Not run; no headset attached |

The current proxy responds 403 to `packages.unity.com`. GitHub read access succeeds. No license/token
was requested, and no authentication was fabricated. Resolve the package destination in environment
settings before importing here, or use a developer workstation with Unity already provisioned.

## Unity acceptance

Run `UNITY_EDITOR=/path/to/Unity bash tools/run_unity_tests.sh`. Check the current run's XML and logs;
zero/skipped tests do not count as validation. The EditMode suite also loads every package prefab,
detects missing scripts, checks the diagnostic bank and checks all three child mixer groups.
PlayMode tests exercise the real noise manager, budget, guard, stale input watchdog, unsafe Rigidbody
release, close-encounter suppression and missing audio path. These tests do not substitute for SDK tests.

In a scratch scene, verify:

1. Exactly one active center-eye camera and AudioListener; proper standing/seated floor height. No
   duplicate camera rig is introduced into Codex 1's scene.
2. Hand-only pinch/grab/release; controller-only grip/trigger fallback; both sources present; each side
   switches without retaining a selection or selecting two objects. Test each enabled SDK interactor group.
3. Throw a bottle repeatedly at a wall/floor. Correct velocity comes from SDK; one meaningful impact
   triggers investigation; subsequent quiet contacts do not flood events.
4. Grab with two hands, release either side, then both. The configured object neither snaps nor changes
   scale unexpectedly. Turn the valve/pull the lever through both limits; output normalized values and
   noise correspond to the constrained motion.
5. Occlude either hand mid-grab, remove a controller battery, leave tracking volume, interrupt app focus,
   pause/resume, and stop the tracking provider. Unsafe release has zero stale throw velocity, SDK
   selection is cleared and the object remains physical. Tracking restoration waits 0.25 s and requires
   a fresh deliberate selection. Repeated losses do not accumulate Rigidbody kinematic locks.
6. Zombie sees a silent nearby player and remains idle; hears a distant noise and investigates its
   point; a thrown object redirects it; an unreachable point times out into search/idle. It cannot attack
   through glass/walls, attack every tick, chase a moving silent player forever or treat a bottle as a player.
7. Encounter callbacks run in sequence and once; close approach cancels future beats, focus/pause policy
   works, opt-out suppresses infected reveal, no encounter rotates the camera or traps the player close.

## Headphone listening log — pending

Use the actual Quest output and intended headphones, HRTF enabled. Keep gains comfortable; start low.
For each cue, place an anchor 3 m left, right, ahead and behind, then at 10/25 m. Test stationary and
head-turning conditions; front/back localization cannot be accepted from stereo panning alone.

| Test | Expected behavior | Actual result |
| --- | --- | --- |
| Left/right animated zombie foot anchors | Correct lateral movement and distinct alternating feet | Not tested |
| Breathing 3 m behind | Direction remains at the captured world point as head turns | Not tested |
| Scratching at door/wall | Correct side and distance | Not tested |
| Emergency alarm at 25 m | Distant, audible with appropriate attenuation | Not tested |
| Growls behind an occluder | Lower gain and low-pass filtering, restored after clearing wall | Not tested |
| Master at zero | All package cues muted, including an existing voice | Not tested |
| Comfort at zero | Comfort-sensitive horror cues muted; configured exceptions remain | Not tested |
| Missing clip / full pool | No exception, no unbounded allocation, cue skipped | Not tested on device |

Record headset/headphone model, spatializer/version, gains, anchor positions and listener observations
alongside the build hash. Replace the diagnostic synthesized clips with production assets after review.
