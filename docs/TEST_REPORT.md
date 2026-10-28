# Test Report

Last updated 2026-10-28 by Claude · Build under test: **none exists** · Repo commit: see `git log -1` on `claude/technical-director`.

## Status vocabulary

| Status | Meaning |
|---|---|
| **PASS** | Tested, met the criterion, with evidence recorded below |
| **FAIL** | Tested and did not meet the criterion |
| **BLOCKED** | Can't be tested yet; the blocker is named |
| **NOT TESTED** | Could be tested but hasn't been |

**Device rule:** a headset check is PASS only with all of these: the headset model and OS version, the build hash, the date, an `adb` log or OVR Metrics capture, and a video or screenshot from the headset, or the owner's written observation. Claude does not mark a device check PASS without that evidence.

## Test layers

| Layer | Tool | Runs without Unity | Status today |
|---|---|---|---|
| 1. Contract and logic tests | `python tools/logic_tests/run_logic_tests.py` | yes | runs: 43/43 PASS (Core only, no seat code yet) |
| 2. Static rules | `python tools/review/check_rules.py` (+ `--self-test`) | yes | runs: 0 findings; self-test PASS |
| 3. Unity EditMode tests (same files as layer 1, plus Unity-only ones) | Unity Test Runner, `-runTests -testPlatform EditMode` | no | BLOCKED: no Unity Editor |
| 4. Scene build and validation | `PatientZero/Build Scenes` (batchmode) | no | BLOCKED: no Unity Editor; composer not written yet |
| 5. PlayMode tests: autoplay full run, restart ×10 leak test, pause during load | Unity Test Runner, `-testPlatform PlayMode` | no | BLOCKED: no Unity Editor; tests not written yet |
| 6. Android development build | `PatientZero/Build Quest APK` (batchmode) | no | BLOCKED: no Unity Editor or Android module |
| 7. On-device checks | headset + `adb` + OVR Metrics Tool | no | BLOCKED: no headset connected, no `adb` |

## Evidence log

| Date | Layer | Commit | Command | Result |
|---|---|---|---|---|
| 2026-10-28 | 1 | contract v1 | `python tools/logic_tests/run_logic_tests.py` | 43 passed, 0 failed, 0 compiler warnings (Roslyn csc from VS 18 Build Tools, `/langversion:9.0`, against `tools/logic_tests/stubs`) |
| 2026-10-28 | 1 | contract v1 | mutation check: dropped one channel from `GameEvents.ResetAll`; made `EventChannel` remove handlers immediately during a raise | both caught (`ResetAll_ClearsEveryChannel`, `Unsubscribe_EarlierHandlerDuringRaise_LaterHandlersStillRun`) |
| 2026-10-28 | 2 | contract v1 | `python tools/review/check_rules.py --self-test` | every rule fired on its planted violation; 0 false positives from comments or strings |
| 2026-10-28 | 2 | contract v1 | `python tools/review/check_rules.py` | 0 errors, 0 warnings |

## Feature acceptance (Mission 6)

| # | Feature | Criterion | Status | Evidence / blocker |
|---|---|---|---|---|
| F-01 | Complete 120-second playable loop | GD-01: a first-time tester completes Awakening → Complete without being caught in 100–150 s; the PlayMode autoplay bot reaches Complete | BLOCKED | No Unity Editor; seat code not written |
| F-02 | Working hand interactions | GD-02: planks, valve, cable, lever and teleport completed with hands only, standing and seated | BLOCKED | No headset; seat A code not written |
| F-03 | Controller fallback | GD-02: the same with controllers only; switching modality mid-act keeps progress | BLOCKED | No headset; seat A code not written |
| F-04 | Sound-reactive zombies | GD-04: noise inside the radius triggers Suspicious → Investigating; outside it does nothing; the Act 2 investigation happens every run | NOT TESTED | Seat B `ZombieBrain` not written. Testable in the logic harness once it exists |
| F-05 | Spatial audio | GD-05: blind beacon localisation within 30° (3 of 3 testers); cue-table attenuation tests pass | BLOCKED | No headset; seat B cue table not written |
| F-06 | Functional ending | GD-08: the reveal plays in both modalities; Complete offers Play again | BLOCKED | No Unity Editor |
| F-07 | Pause and restart | GD-09: pause via button, gesture and headset removal freezes everything; restart act and full restart work; memory and subscribers back to baseline after 10 restarts | BLOCKED | No Unity Editor. Contract part PASS: `GameStateRules` transitions tested (`GameStateRulesTests`) |
| F-08 | Quest performance profiling | `PERFORMANCE_REPORT.md` budgets met on device | BLOCKED | No headset, no build |
| F-09 | No critical gameplay blockers | 0 open BLOCKER findings; F-01 to F-07 PASS | BLOCKED | Depends on all of the above |

## Supporting checks

| # | Check | Status | Evidence / blocker |
|---|---|---|---|
| S-01 | Every required puzzle is reachable from a teleport anchor and inside the seated-safe height band | PASS (static layout only) | `WardLayoutTests` (layer 1). Placement in real scenes: BLOCKED (composer validation) |
| S-02 | The final patient's reach can't touch the player's path or the lever | PASS (static layout only) | `WardLayoutTests.FinalPatient_CannotReachAnyAct4AnchorOrTheWalkBetweenThem` |
| S-03 | Event system: no allocation on raise, safe unsubscribe during a raise, exceptions isolated | PASS (logic) | `EventChannelTests` (10 tests). Allocation measured on device: BLOCKED |
| S-04 | Static state reset before play (domain reload off) | PASS (logic) | `GameEventsTests.ResetAll_ClearsEveryChannel`, `ServicesTests.ResetStatics_ClearsRegisteredServices` |
| S-05 | Settings are comfort-first by default and corrupt values are clamped | PASS (logic) | `ExperienceSettingsTests` |
| S-06 | Exactly 2 hard scares in Standard, 0 in Reduced (GD-06) | NOT TESTED | Needs seat B `DirectorCore` |
| S-07 | No luminance change above 3 Hz (GD-07) | NOT TESTED | Flicker generator test (seat B logic); then a device capture (BLOCKED) |
| S-08 | Hint ladder timing (GD-10) | NOT TESTED | Needs seat B `DirectorCore` |
| S-09 | Act restore reproduces the act-start state (GD-03) | BLOCKED | Needs Unity PlayMode |
| S-10 | Hand tracking loss drops held objects safely | BLOCKED | Needs the Editor hand simulator, then a headset |
| S-11 | Hand-tracking permission and feature present in the APK manifest | BLOCKED | No build |

## Release candidate verdict

**NOT APPROVED.** No playable build exists, and every critical check (F-01 to F-09) is BLOCKED or NOT TESTED. See `RELEASE_CHECKLIST.md`.
