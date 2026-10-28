# Integration Checklist

Owner: Claude (Technical Director) · Created 2026-10-28 for the Full Production Director prompt · Read together with `WORK_PLAN.md`.

This file covers how work gets from a seat branch onto `main`, which tools gate it, and what is verified versus blocked.
`WORK_PLAN.md` still defines seats, ownership and invariants. Nothing here overrides it.

## 1. Ownership under the production prompt (no amendment needed)

The prompt's ownership list maps onto contract v1 seats without moving any path:

| Prompt says | Seat | Contract v1 area(s) |
|---|---|---|
| Player, interaction and physics gameplay | A (Codex 1, `lavanyahd`) | `Player/`, `Interaction/` |
| Puzzles | A | `Interaction/` (planks, valve, cable, socket, lever) |
| GameDirector | A | `Flow/` (`GameFlow`: state, pause, restart, scene loading) |
| UI and ending | A | `UI/`, plus A renders the hand reveal; B drives `InfectionReveal` |
| Zombie AI | B (Codex 2, `businessteen`) | `AI/` |
| Spatial audio | B | `Audio/` |
| Level construction | B | `Environment/` |
| Lighting and horror pacing, encounter sequences | B | `Atmosphere/` (`DirectorCore` / `IExperienceDirector`: beats, scares, hint ladder) |
| Contracts, packages and versions, scene assembly, build automation, testing and QA, performance, release review | Claude | `Core/`, `Integration/`, `Packages/`, `ProjectSettings/`, `Assets/XR/`, `tools/`, `docs/`, `.github/`, root files |

The prompt's "GameDirector" (A, game state) and B's "experience director" (pacing) are different classes that already
exist in the contract. A changes state only through `GameEvents.StateRequested`; B listens to `StateChanged`.

## 2. Pinned toolchain (checked 2026-10-28)

The single source is `tools/unity/package_policy.json`. `validate_packages.py` keeps `Packages/manifest.json`,
`ProjectSettings/ProjectVersion.txt` and `QuestBuildConfig.cs` in line with it.

| Item | Pin | How it was checked |
|---|---|---|
| Editor | Unity **6000.3.27f1** (revision `672d562a42a7`), 6.3 LTS | Unity release API; Android module listed for this release |
| URP | 17.3.0 | core package, fixed by the 6000.3 editor (6.3 manual, URP changelog title) |
| Test Framework | 1.6.0 | core package, fixed by the 6000.3 editor |
| OpenXR | 1.19.0 | packages.unity.com; needs Unity 6000.0 (`validate_packages.py --online`) |
| OpenXR: Meta | 2.7.0 | packages.unity.com; needs 6000.0; depends on OpenXR ≥ 1.18.0, XR Management 4.7.0, AR Foundation 6.6.0 |
| XR Management | 4.7.0 | packages.unity.com. The registry's `latest` tag still says 4.6.1, but OpenXR: Meta 2.7.0 requires 4.7.0 |
| XR Interaction Toolkit | 3.7.0 | packages.unity.com; needs 6000.0 (architecture asks ≥ 3.5) |
| XR Hands | 1.10.0 | packages.unity.com; needs 6000.0 (architecture asks ≥ 1.8) |
| Input System | 1.20.1 | packages.unity.com; needs 6000.0 |
| AI Navigation | 2.0.16 | packages.unity.com; needs 6000.0 |
| Android | min API 32, target API 34, arm64, IL2CPP, Vulkan, multiview, Linear | [Meta: Android 14 required from 1 March](https://developers.meta.com/vr/blog/meta-quest-apps-android-14-march-1/), [Meta: mobile manifest](https://developers.meta.com/vr/documentation/native/android/mobile-native-manifest/) |
| Forbidden | `com.unity.xr.oculus`, `com.meta.xr.sdk.{core,interaction,all,platform}` | ARCHITECTURE.md section 1, "Not used"; enforced on direct **and** transitive packages |
| Held back | `com.meta.xr.sdk.audio` 85.0.0 (Meta registry) | owner licence decision (ARCHITECTURE.md section 11) |

## 3. Tools

| Tool | Needs Unity | What it proves | Today |
|---|---|---|---|
| `python tools/logic_tests/run_logic_tests.py` | no | Core contracts, every seat's `Logic/`, Integration logic (build plan, placement, service census, Quest config) | 70/70 PASS |
| `python tools/review/check_rules.py` (+ `--self-test`, `--seat`) | no | ownership, cross-seat namespaces, SDK ban, light/audio owners, hot-path allocation, asmdef graph | 0 findings; self-test PASS |
| `python tools/unity/validate_packages.py` (+ `--online`) | no | manifest pins, forbidden SDKs, git/file sources, scoped registries, editor-pin drift | PASS; online: 7 pins published, 2 core |
| `python -m unittest discover -s tools/unity -p "test_*.py"` | no | the two tools above, with fixtures and a fake registry | 24/24 PASS |
| GitHub Actions `checks` (`.github/workflows/checks.yml`) | no | all of the above on every push and PR, plus the seat diff check on PRs | see TEST_REPORT.md |
| `python tools/unity/unity_batch.py <compose\|validate\|configure\|desktop-sim\|build-apk\|editmode-tests>` | **yes** | Unity compile, scene assembly, Quest config, EditMode tests, APK | BLOCKED (exit 2: no 6000.3.27f1 on this machine) |

Editor menus (same code): `PatientZero/Build Scenes`, `Validate Project (core)`, `Validate Project (Quest)`, `Configure Quest`,
`Build Dev APK`, `Desktop Simulation/Use Desktop Simulation`, `Desktop Simulation/Use Headset (Link)`.

**Every editor script carries `// UNVERIFIED IN EDITOR`.** A Roslyn parse found 0 syntax errors. That does not prove they
compile against Unity. APIs most likely to need a fix on first compile: `XRPackageMetadataStore.AssignLoader/RemoveLoader`,
`XRGeneralSettingsPerBuildTarget.SetSettingsForBuildTarget`, `OpenXRSettings.GetSettingsForBuildTargetGroup`,
`UniversalRenderPipelineAsset.Create`, and the `Sample` import API. OpenXR project validation, feature refresh and the
XRI simulator settings are reached by reflection, so a mismatch there is a reported warning, not a compile error.

## 4. How a seat's work reaches `main`

1. The seat pushes its own branch (A: `codex/gameplay`; B: `integration/xr-systems`) and posts `READY FOR REVIEW <commit>` in its log, with the logic harness and `check_rules.py --seat` output pasted in.
2. Claude reviews **B first, then A, independently** (prompt Mission 7). Findings come back as `FIX-<seat>-<n>` in `CLAUDE_LOG.md`.
3. Claude opens a PR from the seat branch into `main`. If it conflicts with the other seat's merged work, Claude cuts `integration/<seat>-<milestone>` from `main`, merges the seat branch into it, resolves only what is needed (never another seat's files), and opens the PR from there.
4. The PR merges only when every **applicable** gate in section 5 is green and the contract compatibility check passes. Merge commits only: no squash, rebase or force-push. The seat branch is never rewritten.
5. After the merge, the other seat rebases nothing. It merges `main` into its own branch when it next starts work.

## 5. Per-PR gate

| Gate | Applies | Evidence attached to the PR |
|---|---|---|
| CI `checks` green (rules, logic, packages, tooling, seat diff) | always | Actions run link |
| Contract compatibility: no edit to `Core/` outside an approved CONTRACT REQUEST; enum values append-only; no renumbered ids | always | `check_rules.py` enum check + reviewer note |
| Review findings: 0 open BLOCKER/MAJOR for this seat | always | `CLAUDE_LOG.md` entry |
| Unity compile: 0 errors | once an editor exists | `unity_batch.py validate` log |
| EditMode tests in Unity | once an editor exists | `unity_batch.py editmode-tests` summary |
| Scene composition: plan valid, every required layout id placed once within 5 cm, one implementer per service, no missing scripts or references | once an editor exists **and** both seats have build steps | `Logs/PatientZero/compose.txt` |
| Desktop simulation run of the affected act | once an editor exists | screen capture |
| Device checks | only with a headset | TEST_REPORT.md device rule |

A gate that cannot run is written as BLOCKED with its reason, never as PASS.

## 6. First run with a Unity Editor (owner installs, Claude runs)

1. The owner installs **6000.3.27f1** through Unity Hub with **Android Build Support** (OpenJDK, Android SDK and NDK tools).
2. `python tools/unity/unity_batch.py validate`. The first open resolves the packages and writes `packages-lock.json`. Expect FAIL for settings not configured yet, but **no compile errors and no forbidden packages**.
3. `python tools/unity/unity_batch.py configure`, then `validate` again. Expect PASS apart from OpenXR project-validation items, each of which is triaged.
4. `python tools/unity/unity_batch.py editmode-tests`. Expect the same 70 tests, now inside Unity.
5. `python tools/unity/unity_batch.py compose`. **Until the seats deliver build steps, this must FAIL** with `layout-missing` for every required id and `service-count` for all 5 services. That failure proves the gate works.
6. `python tools/unity/unity_batch.py desktop-sim`, then Play mode in `Bootstrap.unity` once A's rig exists.
7. Claude commits the generated `ProjectSettings/`, `Packages/packages-lock.json`, `Assets/XR/`, `Integration/Settings/` and their `.meta` files (Claude-owned; the checker's hand-YAML rule exempts Claude).
8. `python tools/unity/unity_batch.py build-apk` once step 5 passes. It configures, composes, validates and builds `Game/Builds/Android/PatientZero-dev.apk`.

## 7. Task board (2026-10-28)

| # | Task | Owner | Status |
|---|---|---|---|
| 1 | Contract review v1 (ACK or CONTRACT REQUESTs) | Codex 2, then Codex 1 | DONE: Codex 1 raised the Act 1 gate request; Claude answered with v1.1 (`GateId`/`GateReleased`); Codex 2 ACKed it (`CODEX_B_LOG.md`, `6bc4c42`) |
| 2 | B1 port onto `integration/xr-systems` (`REVIEW_XR_SYSTEMS_72be6d8.md` section 5) | Codex 2 | DELIVERED `6bc4c42` (B1–B3), draft PR #3. Review `REVIEW_XR_SYSTEMS_6bc4c42.md`: FIX-B-13…18 open (3 MAJOR, 3 MINOR) |
| 3 | A1 on `codex/gameplay` (`CODEX_A_BRIEF.md`) | Codex 1 | DELIVERED `83b57d3`, draft PR #1. Review `REVIEW_GAMEPLAY_83b57d3.md`: FIX-A-1…4 open |
| 4 | Integration logic, editor tooling, package pins, CI | Claude | DONE offline, on `main` since `8b5e5cb` (PR #2); Unity-side UNVERIFIED |
| 5 | First Unity run (section 6) | Claude, after owner step 1 | BLOCKED: no editor |
| 6 | Design amendments D-1…D-4 (`DESIGN_RECONCILIATION.md`) | owner decides | WAITING |
| 8 | Contract v1.1: `GateId`/`GateReleased` + `IExperienceDirector.IsGateReleased` (both seats ACK) | owner, then Claude | APPROVED 2026-10-28; added to Core by Claude. Shared physics layer names (INT-B-2) will be a separate proposal |
| 9 | INT-B-1 NavMesh agent radius 0.25 m in `ProjectSettings`; INT-B-2 layers in `TagManager`; seat B PlayMode tests | Claude, at the first Unity run | BLOCKED: no editor |
| 7 | Install Unity 6000.3.27f1 + Android; connect the headset and name its model; Meta XR Audio licence; invite `businessteen`; confirm Codex 1 is `lavanyahd`; repository public or private (asset rule 7) | owner | WAITING |
