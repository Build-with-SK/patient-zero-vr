# Integration round 2: contract v1.1, both seats, first Unity build (2026-10-28)

Owner prompt: "CLAUDE CODE — PATIENT ZERO ROUND 2". Branch: `integration/round2`, cut from `main` at `44ea7b7`.
Integration PR: draft, to `main`. It must not merge until the gate in section 9 passes.

## 1. Verdict

| Item | Result |
|---|---|
| Contract v1.1 (PR #4) | **MERGED** to `main` as `44ea7b7` after re-verification (section 2) |
| Both seats integrated | **YES, offline**: Codex 2 `6bc4c42` and Codex 1 `83b57d3` merged unchanged, plus 5 documented integration exceptions (section 4) |
| Offline gates | **PASS**: 166/166 logic tests, checker 0/0, integration scan 0 errors, BuildPlan 0 errors over all 11 build steps, packages 0, tooling 30/30 |
| Unity compile | **BLOCKED**: no Unity 6000.3.27f1 on this machine. `unity_batch.py first-run` exits 2. Portable C# results are **not** a Unity compile PASS |
| Scenes generated | **BLOCKED**: same. `Bootstrap.unity` and `Ward.unity` do not exist yet |
| `packages-lock.json`, generated `ProjectSettings/` | **NOT GENERATED**: only Unity may write them. Hand-writing them would invent dependency versions |
| Android build, headset | **BLOCKED**: no Android Build Support, SDK, JDK, `adb` or headset |
| Game complete | **NO**: there is no playable build |

## 2. Refs verified

| Ref | SHA | Check |
|---|---|---|
| PR #4 head (`claude/technical-director`) | `5202249` | CI `checks / offline` SUCCESS ×2; MERGEABLE/CLEAN; ruleset needs no review; re-run here: 72/72, self-test, whole-tree and `--seat claude` 0/0; 0 removed lines in `Core/Contracts`; all 104 existing enum values unchanged, 2 added (`GateId`). Merged with `--match-head-commit` |
| `main` after merge | `44ea7b7` | merge commit, no force push |
| Codex 1 `codex/gameplay` (PR #1) | `83b57d3` | matches the prompt; based on `73a3b67`; 4 commits |
| Codex 2 `integration/xr-systems` (PR #3) | `6bc4c42` | matches the prompt; based on `73a3b67`; 5 commits |
| Frozen `codex/xr-systems`, `Lavanya` | `72be6d8`, `87bb494` | unchanged, not used |

## 3. Integration sequence on `integration/round2`

| Commit | What |
|---|---|
| `553355f` | merge Codex 2 `6bc4c42` (no conflicts) |
| `9d3e864` | merge Codex 1 `83b57d3` (no conflicts; the two deliveries share no file) → 166/166 |
| `4911c3c` | integration exceptions E-1…E-5 (section 4); the checker flags exactly these 13 seat files and nothing else |
| `f0d670b` | Claude tooling: required samples, INT-B-1, first-run driver, integration scan (0 ownership warnings) |

## 4. Integration exceptions (edits to seat-owned code)

Each one is needed for the merged tree to compile, compose or get past Act 1. Each seat may replace it with its own version
when it merges `main`; it must keep the behaviour.

| # | Seat | File | Change | Why it could not wait |
|---|---|---|---|---|
| E-1 | B | `Atmosphere/Runtime/ExperienceDirector.cs` | implements `IsGateReleased`; raises `GateReleased(Act1Door)` once when `CanOpenAct1Door` turns true, after the commands of that pump are applied; the gate closes at every `ActStarted` | contract v1.1 is on `main`: without the method `PatientZero.Atmosphere` does not compile (CS0535) |
| E-2 | A | `Interaction/Runtime/ObjectiveDoor.cs` | the isolation door subscribes to `GateReleased`, checks `IsGateReleased(Act1Door)` on enable, and calls Codex 1's existing `ReleaseIsolationGate()` | nothing called that hook, so the Act 1 door never opened and the director never left Act 1: no run could pass the first minute |
| E-3 | A | `Interaction/Editor/DesktopBuildStep.cs` → `Player/Editor/` | FIX-A-1, exactly as the review asked; `PatientZero.Player.Editor` references `PatientZero.Interaction` | BLOCKER: the composer refuses the whole plan (2 errors) |
| E-4 | A | 5 asmdefs | FIX-A-2: `Unity.ugui` → `UnityEngine.UI` | 5 `using UnityEngine.UI` / `UnityEngine.EventSystems` lines (Player.Editor, UI, UI.Editor) had no assembly to come from, so those assemblies would not compile |
| E-5 | A | 5 test asmdefs | FIX-A-5 (new): explicit test form (`UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll`, `UNITY_INCLUDE_TESTS`, `autoReferenced: false`), as Core and seat B use | 6 `using NUnit.Framework` / `UnityEngine.TestTools` lines got their assembly only through the deprecated `optionalUnityReferences: TestAssemblies` field, and whether 6000.3 still honours it is unverified. The explicit form works either way, and one test assembly that fails to compile stops every test run |

Not changed, although found: everything in section 7 (fixes for the owning seat).

## 5. Integration checks (the prompt's list)

| Check | Result | Evidence |
|---|---|---|
| Duplicate assemblies, namespace or type conflicts | PASS (offline) | `integration_scan.py`: 42 assemblies, 99 scripts, 186 types, 0 duplicates |
| asmdef references | PASS (offline) after E-3…E-5 | the same scan on the plain merge `9d3e864`: **21 errors**; after: 0 |
| Missing Unity scripts | PASS (offline) | every MonoBehaviour/ScriptableObject lives in a file of its own name; no script outside an asmdef |
| Editor code in player assemblies | PASS (offline) | 0 `UnityEditor` uses outside `#if UNITY_EDITOR` in player assemblies |
| XR package compatibility | PASS (offline, names only) | seat A uses the XRI 3.x namespaces (`Interactables`, `Locomotion.Movement/Turning/Teleportation`, `Inputs`, `UI`); no Meta/Oculus SDK. Unity compile will confirm |
| Director event wiring | PASS (offline) with 1 gap | 16 channels: every channel heard has a writer except `HapticRequested` (FIX-B-20). `GateReleased`: Atmosphere → Interaction |
| Services | PASS (offline) | one registrar each: `IExperienceDirector`←ExperienceDirector (Ward 200), `IAudioCuePlayer`←AudioCuePlayer (Bootstrap 600), `ISettingsProvider`←SettingsProvider (Bootstrap 400), `IPlayerQuery`/`IScreenFader`←PlayerRig/ScreenFader (Bootstrap 410). The composer counts them per scene at the first run |
| Zombie AI ↔ player interaction | PASS (offline) | B reads the player only through `IPlayerQuery`; A raises `ObjectiveProgress/Completed`, B reacts; no cross-seat assembly reference (checker) |
| Audio triggers | PASS (offline) | only Audio raises `Noise`/`CaptionRequested`; every scripted cue goes through `Cues.Play`; placeholder clips are generated at runtime (no audio asset needed) |
| Scene construction ownership | **1 conflict** | the Act 3 cart is built twice at `A3.Cart`: Environment's static cart (with colliders) and seat A's 18 kg pushable trolley overlap → FIX-B-19. No layout id is tagged twice (`PlacementCheck` would refuse it) |
| Pause and restart | PASS (offline, rules only) | only `GameFlow` writes `Time.timeScale`/`AudioListener.pause` (checker); the director runs on scaled time; the gate closes on every `ActStarted` (restore included). Runtime behaviour: NOT TESTED |
| Missing assets and materials | PASS (offline) with 1 dependency | materials, meshes, prefabs and the mixer are saved as assets by the build steps; the XR rig comes from XRI's **Starter Assets** sample, which the composer now requires (section 6) |
| Build order | PASS (offline) | real `BuildPlan` over all 11 steps: 0 errors. Bootstrap 400 Flow, 410 Player, 420 Desktop, 430 UI, 600 Audio; Ward 100 Environment, 150 NavMesh bake, 200 Atmosphere, 310 Interaction, 500 Zombies, 650 Audio emitters |

## 6. Claude tooling added this round

* `PackageSamples` (Integration/Editor): imports XRI **Starter Assets** (required), **Hands Interaction Demo** and XR Hands **HandVisualizer** (optional; a missing name is reported with the names the installed version has). Menu `PatientZero/Import Required Samples`. Runs in its own editor session because the samples contain scripts.
* `SceneComposer` preconditions, before any scene is touched: the required sample is imported, and **INT-B-1**: agent type 0's radius is set to 0.25 m (`NavigationSettings`, read back through `NavMesh.GetSettingsByID`) before Environment bakes at Ward 150.
* `unity_batch.py`: `open` (resolve packages, write `packages-lock.json`, compile), `import-samples`, `playmode-tests`, and **`first-run`**: `open → import-samples → compose → editmode-tests → playmode-tests → desktop-sim → build-apk → validate`, each in its own editor session, summary in `Game/Logs/PatientZero/first-run.json`. Android steps report BLOCKED when Android Build Support is missing; an unactivated licence reports BLOCKED, not FAIL.
* `tools/review/integration_scan.py` with `--self-test`, now in CI.

All C# here is **UNVERIFIED IN EDITOR**: syntax-checked with Roslyn (0 syntax errors; a planted missing `;` is caught), not compiled.

## 7. Fix assignments

**Codex 1 (seat A).** Merge `main` once this PR lands; E-2…E-5 are then in your files. Keep them or replace them with equivalent code.

| Fix | Severity | What |
|---|---|---|
| FIX-A-1, -2 | done as E-3, E-4 | confirm with `FIX DONE` after merging `main` |
| FIX-A-3 | MAJOR | desktop mouse mode and the XRI simulator both run when no headset is active (`REVIEW_GAMEPLAY_83b57d3.md`) |
| FIX-A-4 | MINOR | `DesktopBuildStep` takes the first camera it finds; take it from the rig (the file now lives in `Player/Editor/`) |
| FIX-A-5 | done as E-5 | explicit test asmdefs |
| FIX-A-6 | MINOR (new) | the Act 1 door leaf turns 90° about its own centre. `GAME_DESIGN.md` Act 1 "Unlatch": the door swings **30°**. Hinge the leaf at its edge and set `openDegrees` to 30 for the isolation door |
| FIX-A-7 | MINOR (new) | the Act 3 trolley is yours (`GAME_DESIGN.md`: "B, A (cart physics)"). Once FIX-B-19 removes Environment's cart, check that the trolley alone carries the bottles and the `ImpactHeavy` rattle |

**Codex 2 (seat B).** Merge `main` once this PR lands; E-1 is then in your `ExperienceDirector`. Keep it or replace it; `ActOneDoorGateReady` and its comment are now stale.

| Fix | Severity | What |
|---|---|---|
| FIX-B-13…18 | open | `REVIEW_XR_SYSTEMS_6bc4c42.md` (3 MAJOR, 3 MINOR) |
| FIX-B-19 | MAJOR (new) | `HospitalBuildSteps.Cart` builds a colliding cart and bottle dressing exactly where seat A spawns its physics trolley. Keep the `A3.Cart` marker; drop the cart geometry (A owns the cart) |
| FIX-B-20 | MINOR (new) | nothing raises `HapticRequested`; ARCHITECTURE section 5.1 gives it to the AI/director for zombie proximity and scares (controllers only) |
| FIX-B-21 | after owner confirmation | **operating theatre**: the round 2 prompt asks for one. `DESIGN_RECONCILIATION.md` D-4 (recommended) dresses the Act 2 supply room as an operating theatre without moving any coordinate, owner B. Build it only once the owner confirms D-4 |

## 8. First-run checks (Claude, on the first machine with the editor)

1. `python tools/unity/unity_batch.py first-run` and attach `first-run.json` to the PR.
2. The sample names exist in XRI 3.7.0 / XR Hands 1.10.0; Starter Assets' rig satisfies `PlayerBuildStep.FindRig`.
3. INT-B-1 radius write sticks (`navmesh:` line); Z2 paths through its 1.1 m doorway (`NavigationSceneTests`).
4. The NavMesh is baked at Ward 150, before seat A builds doors and props (310): closed doors and the cart are not NavMesh obstacles. Check whether any zombie walks through the B-7 door or the cart; if so, decide between carving obstacles (A) and a later bake.
5. INT-B-3: the reflection-based mixer factory and seat B's 6 PlayMode tests; seat A's `IntegrationPlayTests`.
6. Commit what Unity generated: `Packages/packages-lock.json`, `ProjectSettings/`, `Assets/XR/`, samples under `Assets/Samples/`, `.meta` files, and the two scenes.

## 9. Merge gate for the integration PR

* `first-run`: `open`, `import-samples`, `compose`, `editmode-tests`, `playmode-tests` PASS; `build-apk` PASS or BLOCKED only for the missing Android module (stated).
* CI `checks / offline` green; checker and integration scan clean.
* The owner says yes.

## 10. What the owner can do

* **Unblock Unity** (pick one): install Unity Hub, then Unity **6000.3.27f1** with Android Build Support, OpenJDK and Android SDK & NDK Tools on this computer and sign in to activate a licence (Claude cannot sign in or accept licence terms); **or** ask Codex 1 to install 6000.3.27f1 (they have 6000.3.26f1, Windows only) and run `first-run` on `integration/round2`.
* Confirm **D-4** (operating theatre as set dressing of the supply room) so FIX-B-21 can start.
* Still open from before: D-1…D-3, INT-B-2 physics layer names, headset model, Meta XR Audio licence, invite `businessteen`, repository public or private.
