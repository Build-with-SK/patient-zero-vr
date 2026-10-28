# Review Checklist (Mission 4)

Claude applies this to every `READY FOR REVIEW` commit. **Auto** = a tool decides; **Read** = a human reads the diff; **Unity** = needs the Editor; **Device** = needs the headset.
Findings are written into the agent's log as `FIX-<seat>-<n>`. Each one has a file and line, the defect, the exact fix, and a severity:

* **BLOCKER**: breaks the build, the 120-second loop, a contract or comfort safety. The branch is not merged.
* **MAJOR**: wrong behaviour or a budget breach. Must be fixed before the release candidate.
* **MINOR**: style, clarity, small waste.

## 0. Always run first

```bash
python tools/logic_tests/run_logic_tests.py
python tools/review/check_rules.py --seat A --base origin/main --head origin/codex/gameplay
python tools/review/check_rules.py --seat B --base origin/main --head origin/integration/xr-systems
```

Any ERROR from either tool is a BLOCKER.

## 1. Compilation errors

| Check | How |
|---|---|
| Logic and Core compile as C# 9 with zero warnings | Auto: harness |
| Runtime and Editor code compile | **Unity**: open the project, check the Console is clean, then `-batchmode -quit` with the log scanned for `error CS` |
| No API that doesn't exist in the pinned package versions (XRI 3.x renamed many 2.x types, e.g. `XRDirectInteractor` → `NearFarInteractor` patterns) | Read, then Unity |
| No `#if UNITY_EDITOR` code in runtime paths that the Android build needs | Read |

## 2. Broken Unity references

| Check | How |
|---|---|
| No hand-written `.unity`/`.prefab`/`.asset`/`.mat`/`.meta` | Auto: `hand-yaml` |
| Asmdefs reference by name, with no cross-seat references | Auto: `asmdef-*` |
| Every required `WardLayout` id is placed exactly once | Unity: composer validation |
| No missing scripts or null serialized references in generated scenes and prefabs | Unity: composer validation |
| `[SceneBuildStep]` order inside the seat's band, signature `static void Build(Transform)` | Read |

## 3. SDK compatibility

| Check | How |
|---|---|
| No `OVR*`, `Oculus.*`, `Meta.XR.*` (Meta XR Audio allowed in Audio) | Auto: `wrong-sdk` |
| Seat B has no XRI/XR Hands dependency | Auto: `b-no-xri`, `asmdef-b-xri` |
| Hands and controllers both drive every interaction through XRI interactables. No device-specific input code paths | Read |
| OpenXR features enabled: Meta Quest Support, Hand Tracking Subsystem, Meta Hand Tracking Aim, Touch controller profiles, Hand Interaction Profile | Unity: Project Settings → XR Plug-in Management → OpenXR |
| The merged `AndroidManifest.xml` in the APK declares hand tracking (permission and feature) | **Device build**: `aapt dump xmltree` or the Unity build report |

## 4. State-machine bugs

| Check | How |
|---|---|
| GameFlow refuses every transition `GameStateRules` refuses, and logs it | Auto: GameFlowCore tests |
| Every state has a way out. No dead ends (e.g. Caught never returning) | Auto: tests that walk the table |
| Zombie FSM: every state has an exit. Retargeting has hysteresis. Chasing ends when the player is lost. Restrained never moves past its chain | Auto: ZombieBrain tests |
| Director: each act advances only on its objectives. Hint ladder resets on progress. At most 2 hard scares. Restoring an act yields exactly the act-start state | Auto: DirectorCore tests, golden-run trace |
| Pausing during a transition (fade, scene load) can't strand the game | Read, then the PlayMode test "pause during load" |
| Events raised during `OnDisable`/scene unload don't reach dead listeners | Read, then the PlayMode restart ×10 test |

## 5. Performance risks

| Check | How |
|---|---|
| No per-frame allocation, lookup, LINQ or string building in hot methods | Auto: `hot-*` |
| No scene searches or LINQ at runtime | Auto |
| Lights ≤ 6 in Ward, no realtime shadows, ≤ 3 per object | Unity: composer validation |
| ≤ 30 active rigidbodies. No joint chains | Read |
| Pools warmed at load | Read |
| Frame timing, draw calls, triangles | **Device**: `PERFORMANCE_REPORT.md` |

## 6. Unreachable puzzles

| Check | How |
|---|---|
| All required interactables reachable from an anchor, at a seated-safe height | Auto: `WardLayoutTests` |
| Placed objects match layout positions (≤ 5 cm) | Unity: composer validation |
| Required items that can be dropped (cable plug) return to their hook within 5 s if they fall out of reach or below 0.3 m | Read, then PlayMode test |
| Every objective is completable one-handed, seated, with hands only, and with controllers only | PlayMode autoplay bot (logic level), then **Device** |
| The assist ladder actually lowers the requirement (valve 540° → 270°, socket snap 6 → 12 cm, lever 2 → 1 s) | Auto: A's logic tests |

## 7. Tracking failures

| Check | How |
|---|---|
| A held object drops with zero velocity when its hand is lost (no flinging) | Read, then the Editor hand simulator, then **Device** |
| Modality switch mid-grab (put the controller down) doesn't duplicate or freeze the grab | **Device** |
| Valve and lever keep their progress across re-grips | Auto: logic tests |
| A "Hands not visible" prompt appears after 1.5 s of loss in hands mode | Read, then **Device** |

## 8. Memory allocation spikes

| Check | How |
|---|---|
| 0 B GC per frame in Playing | **Device**: Profiler over ADB, 60 s capture |
| Ward reload stays inside the fade. Memory returns to baseline after 10 restarts (±5 %) | **Device**: Memory Profiler snapshots before and after |
| No `Instantiate` during Playing outside warmed pools | Read |

## 9. Audio attenuation

| Check | How |
|---|---|
| Every `CueId` has a cue-table entry | Auto: B's CueTable tests |
| World cues: spatial blend 1, Doppler 0, a custom rolloff that **reaches 0 at Max Distance** (Unity's logarithmic default stops attenuating there instead) | Auto: CueTable tests |
| Every zombie vocal is audible at ≥ 9 m (1.5 × the largest player noise radius), so the player hears a zombie before it hears them | Auto: CueTable tests |
| Noise radius ≤ the cue's Max Distance. Silence equals no noise | Auto: CueTable tests |
| One attenuation model per source (Meta XR Audio distance attenuation off when Unity curves are used) | Read, then Unity |
| Spatialized clips are mono | Unity: import settings check in composer validation |
| Positional accuracy: the beacon direction can be located blind | **Device**: three testers, eyes closed, pointing within 30° |

## 10. Scene integration

| Check | How |
|---|---|
| Exactly one implementer per service per scene | Unity: composer validation |
| Bootstrap persists and Ward reloads cleanly ×10. `SubscriberCount` back to baseline | PlayMode test |
| The full 120-second autoplay run reaches Complete | PlayMode autoplay bot |
| No seat raises another seat's events or touches another seat's components | Auto |
