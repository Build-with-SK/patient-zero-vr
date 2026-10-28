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
| 1. Contract and logic tests | `python tools/logic_tests/run_logic_tests.py` | yes | runs: 72/72 PASS (Core 45 + Integration 27; no seat code on `main` yet) |
| 2. Static rules | `python tools/review/check_rules.py` (+ `--self-test`) | yes | runs: 0 findings; self-test PASS |
| 2b. Package policy and tooling tests | `python tools/unity/validate_packages.py` (+ `--online`); `python -m unittest discover -s tools/unity -p "test_*.py"` | yes | runs: 0 errors; 24/24 PASS |
| 2c. CI | GitHub Actions `checks` runs layers 1, 2 and 2b on every push and PR | yes | see the evidence log |
| 3. Unity EditMode tests (same files as layer 1, plus Unity-only ones) | `python tools/unity/unity_batch.py editmode-tests` | no | BLOCKED: no Unity Editor (the runner exits 2) |
| 4. Scene build and validation | `python tools/unity/unity_batch.py compose` / menu `PatientZero/Build Scenes` | no | BLOCKED: no Unity Editor. Composer written, UNVERIFIED IN EDITOR; its rules are tested in layer 1 |
| 5. PlayMode tests: autoplay full run, restart ×10 leak test, pause during load | Unity Test Runner, `-testPlatform PlayMode` | no | BLOCKED: no Unity Editor; tests not written yet |
| 6. Android development build | `python tools/unity/unity_batch.py build-apk` / menu `PatientZero/Build Dev APK` | no | BLOCKED: no Unity Editor or Android module. Build script written, UNVERIFIED IN EDITOR |
| 7. On-device checks | headset + `adb` + OVR Metrics Tool | no | BLOCKED: no headset connected, no `adb` |

## Evidence log

| Date | Layer | Commit | Command | Result |
|---|---|---|---|---|
| 2026-10-28 | 1 | contract v1 | `python tools/logic_tests/run_logic_tests.py` | 43 passed, 0 failed, 0 compiler warnings (Roslyn csc from VS 18 Build Tools, `/langversion:9.0`, against `tools/logic_tests/stubs`) |
| 2026-10-28 | 1 | contract v1 | mutation check: dropped one channel from `GameEvents.ResetAll`; made `EventChannel` remove handlers immediately during a raise | both caught (`ResetAll_ClearsEveryChannel`, `Unsubscribe_EarlierHandlerDuringRaise_LaterHandlersStillRun`) |
| 2026-10-28 | 2 | contract v1 | `python tools/review/check_rules.py --self-test` | every rule fired on its planted violation; 0 false positives from comments or strings |
| 2026-10-28 | 2 | contract v1 | `python tools/review/check_rules.py` | 0 errors, 0 warnings |
| 2026-10-28 | 1 | `claude/technical-director` (Integration logic) | `python tools/logic_tests/run_logic_tests.py` | 70 passed, 0 failed |
| 2026-10-28 | 1 | same | mutation check: let Flow build into Ward; removed the missing-required-point check | both caught (`SeatBuildingIntoTheWrongScene_IsAnError`, `MissingRequiredPoint_IsAnError`); 70/70 after restore |
| 2026-10-28 | 2 | same | `check_rules.py --self-test`, then `check_rules.py` | SELF-TEST PASSED; 0 errors, 0 warnings (Integration editor scripts included) |
| 2026-10-28 | 2b | same | `validate_packages.py`; tooling unittest | 0 errors; 24/24 PASS. Mutation: forbidden-package checks disabled → 2 tests fail; restored → OK |
| 2026-10-28 | 2b | same | `validate_packages.py --online` (packages.unity.com) | 7 non-core pins published and support Unity 6000.x; 2 core pins fixed by the editor |
| 2026-10-28 | — | same | Roslyn parse of the 5 Integration editor scripts with no Unity references | 0 syntax errors; 64 expected missing-type errors (CS0246/CS0234). **Not a compile check** |
| 2026-10-28 | 3–6 | same | `python tools/unity/unity_batch.py validate` | BLOCKED, exit 2: `C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe` not found |
| 2026-10-28 | 1 | contract v1.1 | `python tools/logic_tests/run_logic_tests.py` | 72 passed, 0 failed (new: `GateIds_AreUniqueAndPinned`, `GateReleased_CarriesItsGate`; `ResetAll_ClearsEveryChannel` now covers `GateReleased`) |
| 2026-10-28 | 1–2 | contract v1.1 | mutations: dropped `GateReleased.Clear()` from `ResetAll`; planted an Interaction file raising `GateReleased` | both caught: `GateReleased is missing from GameEvents.ResetAll`; `event-writer … Interaction may not raise GameEvents.GateReleased (writers: Atmosphere)` |
| 2026-10-28 | 1 | `integration/round2` (v1.1 + both seats + E-1…E-5) | `python tools/logic_tests/run_logic_tests.py` | 166 passed, 0 failed (Core 45, Integration 27, seat A and B Logic suites) |
| 2026-10-28 | 2 | same | `check_rules.py --self-test`; whole tree; `--seat claude --base 9d3e864 --head 4911c3c` | SELF-TEST PASSED; 0/0; 13 ownership warnings = exactly the 13 files of exceptions E-1…E-5 |
| 2026-10-28 | 2 | same | `python tools/review/integration_scan.py` (new, self-test passes) | 0 errors, 1 warning (`HapticRequested` has no writer, FIX-B-20). The plain merge `9d3e864` before the exceptions: 21 errors |
| 2026-10-28 | 1 | same | temporary `BuildPlan` probe over all 11 `[SceneBuildStep]`s (not committed) | 0 errors (2 before E-3) |
| 2026-10-28 | — | same | Roslyn syntax check of the 7 changed runtime/editor C# files | 0 syntax errors; a planted missing `;` is caught. **Not a compile check** |
| 2026-10-28 | 2b | same | tooling unittest | 30/30 PASS (new: first-run order, Android module paths, licence detection) |
| 2026-10-28 | 3–6 | same | `python tools/unity/unity_batch.py first-run` | BLOCKED, exit 2: `C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe` not found. No Android SDK, JDK or `adb` either |
| 2026-10-28 | review | `codex/xr-systems` `72be6d8` (not on `main`) | Codex 2's portable suite: Roslyn csc against the .NET 8.0.8 runtime (C# 9, warnings as errors), run with `dotnet` | 26 passed, 0 failed, 0 skipped; 2 mutations caught; `validate_assets.py` PASS. **Not counted toward acceptance:** not on `main`, and Unity import was not run. Details: `docs/collab/REVIEW_XR_SYSTEMS_72be6d8.md` |

## Quality gates (Full Production Director prompt, Mission 6)

| # | Gate | Status | Evidence / blocker |
|---|---|---|---|
| Q-01 | Contract tests | PASS | layer 1: 72/72 with contract v1.1; mutations caught |
| Q-02 | Compile (outside Unity) | PASS for Core and every `Logic/` assembly | Roslyn csc, C# 9, 0 warnings. Editor scripts: parse only, NOT TESTED as a compile. `integration/round2` (both seats): 166/166, integration scan 0 errors |
| Q-03 | Unity compile | BLOCKED | no Unity 6000.3.27f1 on any seat. `unity_batch.py first-run` attempted 2026-10-28: exit 2 |
| Q-04 | Scene assembly | BLOCKED | composer written (UNVERIFIED IN EDITOR); no seat build steps exist yet, so it is expected to fail with `layout-missing` and `service-count` |
| Q-05 | Hand tracking | BLOCKED | no headset; seat A code not written |
| Q-06 | Controller fallback | BLOCKED | no headset; seat A code not written |
| Q-07 | Zombies respond to noise | NOT TESTED | seat B `ZombieBrain` not on `main`; testable in layer 1 once ported |
| Q-08 | Spatial audio direction | BLOCKED | needs a headset and 3 testers (F-05) |
| Q-09 | 120-second loop | BLOCKED | no Unity Editor; seat code not written |
| Q-10 | Ending | BLOCKED | no Unity Editor |
| Q-11 | No critical errors | NOT TESTED | no build to run |
| Q-12 | Quest build (APK) | BLOCKED | no Unity Editor or Android module; build script UNVERIFIED IN EDITOR |
| Q-13 | 72 FPS on device | BLOCKED | no headset, no build (`PERFORMANCE_REPORT.md`) |

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
