# Review: `codex/gameplay` at `83b57d3` (Codex 1, seat A) — draft PR #1

2026-10-28 · Reviewer: Claude (Technical Director) · PR: https://github.com/Build-with-SK/patient-zero-vr/pull/1 (draft, base `main`)

## Verdict

**Not ready to merge: 1 BLOCKER, 2 MAJOR, 1 MINOR.** The work stays inside seat A's paths, changes no contract, and is
honestly labelled UNVERIFIED IN EDITOR. The portable suite and the review checker pass. One build step breaks the
contract's build bands, so the composer would refuse to build **any** scene until it is fixed. No Unity, scene or device
result exists for this branch, and none is claimed.

## What was checked

| Check | Result |
|---|---|
| Branch | 4 commits by `lavanyahd` (116944917 noreply), based on `main` `73a3b67`; 61 files, +1 465 lines, all under `Player/`, `Interaction/`, `Flow/`, `UI/` and `CODEX_A_LOG.md` |
| `run_logic_tests.py` on the branch | `Logic tests: 80 passed, 0 failed.` (43 Core + 37 seat A) |
| `check_rules.py --seat A --base origin/main --head HEAD` | `0 error(s), 0 warning(s).` |
| Merge onto `claude/technical-director` `05d16be` (scratch worktree, not pushed) | no conflicts. `Logic tests: 107 passed, 0 failed.`; checker 0 findings; `validate_packages.py` 0 errors; tooling tests OK |
| The branch's 5 `[SceneBuildStep]`s through the real `BuildPlan` (temporary probe test in the scratch merge, not committed) | **2 errors**, both on `DesktopBuildStep` (FIX-A-1). The other 4 steps are valid |
| Unity compile, scene composition, PlayMode, device | BLOCKED: no Unity 6000.3.27f1 on any seat |

## Findings

### FIX-A-1 · BLOCKER · `Interaction/Editor/DesktopBuildStep.cs:9`

`[SceneBuildStep(BuildScene.Bootstrap, 420)]` lives in assembly `PatientZero.Interaction.Editor`. In contract v1
(`SceneBuildStep.cs`), Interaction owns orders 300–399 and builds into Ward only. The composer validates the whole
plan before touching any scene and stops on:

```
band: PatientZero.Interaction.Editor.DesktopBuildStep.Build uses order 420; Interaction owns 300-399.
scene: Interaction may not build into Bootstrap (PatientZero.Interaction.Editor.DesktopBuildStep.Build). ...
```

**Fix:** move the build step (only the step; `DesktopInteraction` can stay in `PatientZero.Interaction`) into
`Player/Editor/`. Keep order 420, which is inside Player's 400–499 Bootstrap band, and add `PatientZero.Interaction`
to `PatientZero.Player.Editor`'s references (a seat A-internal reference, which is allowed).

### FIX-A-2 · MAJOR (verify at the first compile) · 5 asmdefs reference `Unity.ugui`

`Flow/Runtime`, `Player/Runtime`, `Player/Editor`, `UI/Runtime` and `UI/Editor` reference an assembly named
`Unity.ugui`, and 4 files use `UnityEngine.UI`. The uGUI package's runtime assembly is named **`UnityEngine.UI`**
(the package is `com.unity.ugui`; the assembly name differs). An unresolved asmdef reference leaves every
`UnityEngine.UI` type missing, so these assemblies would not compile. Confidence is high but not proven: no Unity
Editor exists yet. **Fix:** reference `UnityEngine.UI` (and `Unity.TextMeshPro` if TextMeshPro types are used).
Claude confirms the name at the first Unity run.

### FIX-A-3 · MAJOR · two desktop modes collide

`Player/Runtime/DesktopLocomotion.cs` and `Interaction/Runtime/DesktopInteraction.cs` run whenever
`XRSettings.isDeviceActive` is false. That is also the case when the XR Interaction Simulator drives the rig (Claude's
`PatientZero/Desktop Simulation`, which the production prompt asked for). Both then read WASD, RMB and the mouse in the same
frame. **Proposal:** the XRI simulator is the default desktop mode, because it exercises the real interactors and the
simulated hands. Seat A's mouse mode stays as a quick flow-test mode and is **opt-in**: its components return early
unless the scripting define `PZ_DESKTOP_MOUSE` is set. Claude adds a menu item `Desktop Simulation/Use Mouse Mode` that
sets the define and turns the simulator off. Codex 1 may counter-propose in `CODEX_A_LOG.md`.

### FIX-A-4 · MINOR · `Interaction/Editor/DesktopBuildStep.cs:10`

`FindFirstObjectByType<Camera>()` returns whichever camera Unity finds first. If `ScreenFader` or the UI uses its own
camera (INTEGRATION.md mentions a screen-space camera fader), the desktop adapter can bind to the wrong one. **Fix:**
take the camera from the rig component the Player step created, for example `PlayerRig`'s camera reference.

## Contract request: Act 1 door release (CODEX_A_LOG, "Integration requirements")

**Answer: ACCEPTED IN PRINCIPLE, CHANGED shape. Not merged.** It needs the owner's approval and Codex 2's ACK, as
WORK_PLAN.md requires.

The gap is real. GAME_DESIGN Act 1 "Unlatch" needs all planks off **and** Z1 ≥ 4 m from the window: "A (door), B (gate
signal via director)". Contract v1 has no B → A signal for it. Proposed contract v1.1 (append-only, no renumbering):

```csharp
// Ids.cs
public enum GateId { None = 0, Act1Door = 1 }            // append-only like every other id enum
// Payloads.cs
public readonly struct GateReleased { public readonly GateId Gate; public GateReleased(GateId gate) { Gate = gate; } }
// GameEvents.cs: writers Atmosphere (director); readers Interaction
public static readonly EventChannel<GateReleased> GateReleased = ...;
// Interfaces.cs: IExperienceDirector gains a query, so a door enabled after the raise still opens
bool IsGateReleased(GateId gate);
```

This is a general `GateId` rather than an Act 1-specific channel, so a later gate needs only an id. Until it merges,
keep the door failing closed, as Codex 1 already does. Do not code against the proposed API.

**Update 2026-10-28:** the owner approved contract v1.1 and Claude added it to Core. Build against it once it is on `main`; `WORK_PLAN.md` (current state) says what each seat does.

## What seat A has in hand from Claude

| Codex 1's blocker | State |
|---|---|
| Exact Unity patch | Pinned: 6000.3.27f1 (`672d562a42a7`). Codex 1 reports 6000.3.26f1 (Windows only) installed locally; 27f1 with Android Build Support is needed |
| Package pins | `Game/Packages/manifest.json`, single source `tools/unity/package_policy.json` |
| Scene composer and build settings | `Integration/Editor/SceneComposer.cs` (UNVERIFIED IN EDITOR); its rules are tested in `Integration/Logic` |
| Act 1 release gate | answered above; awaits the owner and Codex 2 |
| XRI rig wiring, palm orientation, vignette | needs a Unity Editor; BLOCKED |

These are on PR `claude/technical-director` → `main`, not yet on `main`.

## Next for Codex 1

1. Merge `main` into `codex/gameplay` once the Claude PR lands. Do not rebase a pushed branch.
2. Fix FIX-A-1…4, then reply `FIX DONE FIX-A-<n> <commit>` in `CODEX_A_LOG.md`.
3. Keep PR #1 as a draft until FIX-A-1 is done and CI `checks` is green on it.
