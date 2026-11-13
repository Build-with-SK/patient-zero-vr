# Claude log (Technical Director)

Append-only. Newest last. Format: `docs/collab/WORK_PLAN.md`.

## 2026-10-28 · Project bootstrap and contract v1
Kind: PROGRESS
What:
- Looked for an existing PATIENT ZERO Unity project (Documents, Desktop, Downloads, OneDrive, home `src`, GitHub account Build-with-SK) and found none. Also: no Unity Editor or Hub, no Android SDK, no `adb`, no headset. No Codex branches exist.
- Created this standalone repo at `Documents/patient-zero`, branch `claude/technical-director`, separate from the trading-system repo.
- Checked current platform facts (web, 2026-10-28): Unity 6.3 LTS is the Quest LTS line; Meta recommends the Unity OpenXR plugin (Oculus XR plugin deprecated, slated for removal from 6.5); XRI ≥ 3.5 + XR Hands ≥ 1.8 needed for Editor hand simulation.
- Wrote the Core contracts: ids, event channels, payloads, services, settings, game-state rules, ward layout, scene build step, layout tag, cue helper.
- Wrote the logic harness (UnityEngine/NUnit stand-ins plus a runner) and the review checker (`check_rules.py`, with a self-test).
- Wrote ARCHITECTURE, GAME_DESIGN, ASSET_MANIFEST, REVIEW_CHECKLIST, TEST_REPORT, PERFORMANCE_REPORT, RELEASE_CHECKLIST, WORK_PLAN and both Codex briefs.
Evidence:
- `python tools/logic_tests/run_logic_tests.py` → `Logic tests: 43 passed, 0 failed.` (0 compiler warnings)
- Mutation check: removed a channel from `ResetAll`, and made `EventChannel` remove handlers immediately during a raise → both caught by tests (the second needed a new test, now added)
- `python tools/review/check_rules.py --self-test` → `SELF-TEST PASSED`; `python tools/review/check_rules.py` → `0 error(s), 0 warning(s).`
Not verified: anything inside Unity or on a headset.
Next: owner actions (ARCHITECTURE §11). Codex A and B can start on their Logic milestones now. Once the Editor exists, Claude writes the scene composer and Android build script and pins the packages.

## 2026-10-28 · Contract v1 approved; `main` created
Kind: PROGRESS
What: The owner approved contract v1 in chat ("Approve, create main"). Updated the briefs, the work plan, the architecture and the release checklist so both seats branch from and diff against `main`. Created `main` at this commit; `main` and `claude/technical-director` now point at the same commit.
Evidence: on the commit that became `main`: `Logic tests: 43 passed, 0 failed.` · `check_rules.py` → `0 error(s), 0 warning(s).` · `--self-test` → `SELF-TEST PASSED`.
Next: the owner gives the briefs to Codex A and B, installs Unity 6.3 LTS with Android support, connects the headset and decides the Meta XR Audio licence.

## 2026-10-28 · GitHub `main` published; Codex 2's first delivery reviewed
Kind: PROGRESS
What:
- Checked before publishing: the tree was clean, `main` = `b3bab8b`, the required docs and contracts were present, and no secrets, local paths or trading-system material are tracked (the whole history was searched). The commit author email was already public in the owner's other public repository.
- Inspected the remote first. It is public, with no `main` and no branch protection. `codex/xr-systems` = `72be6d8` (parent `87bb494`, the commit Codex 2 reported) and `Lavanya` = `87bb494`, both on a history unrelated to `main`. Collaborators: `Build-with-SK` (admin) and `lavanyahd` (write).
- Pushed `main` and `claude/technical-director` as new branches, without force. Codex 2's refs are unchanged.
- Reviewed `codex/xr-systems` at `72be6d8`: `docs/collab/REVIEW_XR_SYSTEMS_72be6d8.md` (FIX-B-1…12, the parts worth keeping, and a migration proposal). **Not merged.**
- Added `AGENTS.md` (identities, git rules). Pointed the briefs, work plan and architecture at the GitHub repo, `codex/gameplay` (Codex 1, seat A) and `integration/xr-systems` (Codex 2, seat B, pending approval). Asked both agents for a contract review first.
Evidence:
- `git ls-remote origin` after the push: `main` and `claude/technical-director` at `b3bab8b`; `codex/xr-systems` still `72be6d8`, `Lavanya` still `87bb494`.
- Codex 2's suite: `26 passed; 0 failed; 0 skipped` (csc, .NET 8.0.8). Mutations of the noise cooldown and the radius cut-off were caught (2 and 1 failures). `validate_assets.py` → `PASS 67 unique asset GUIDs; all references resolve…`.
- Our suite on `main`: `Logic tests: 43 passed, 0 failed.`; `check_rules.py` → `0 error(s), 0 warning(s).`
Not verified: anything in Unity or on a headset, for either branch.
Next: the owner approves the migration, after which Claude creates `integration/xr-systems` from `main`. Codex 1 and Codex 2 post their contract reviews.

## 2026-10-28 · GitHub setup applied (owner: "Apply all three")
Kind: PROGRESS
What:
- Default branch changed from `codex/xr-systems` to `main`.
- Ruleset "Protect main" (id 26052045): the `deletion` and `non_fast_forward` rules on `refs/heads/main`, enforcement active, no bypass actors, so it applies to admins too.
- `integration/xr-systems` cut from `main` (after this commit) for Codex 2, seat B. `codex/xr-systems` and `Lavanya` were left untouched.
Evidence: `gh api repos/Build-with-SK/patient-zero-vr` → `default_branch=main`; `gh api .../rules/branches/main` → `deletion`, `non_fast_forward` (ruleset 26052045). The ref check after the push is in the next entry, or in `git ls-remote origin`.
Next: Codex 1 and Codex 2 post their contract reviews. Codex 2 then ports milestone B1 on `integration/xr-systems`.

## 2026-10-28 · Full Production Director: integration tooling, pins, CI
Kind: PROGRESS
What:
- Verified the baseline before starting: `main`, `claude/technical-director` and `integration/xr-systems` at `73a3b67`; `codex/xr-systems` `72be6d8` and `Lavanya` `87bb494` untouched; ruleset "Protect main" active.
- Ownership under the production prompt maps onto contract v1 with no path moved (`INTEGRATION_CHECKLIST.md` section 1). No amendment needed.
- Integration logic (`Integration/Logic/`, engine-free, 27 tests): `BuildPlan` (order bands, target scene, signature, duplicate orders), `PlacementCheck` (every required `WardLayout` id once, within 5 cm), `ServiceCensus` (one implementer per Core service in its home scene), `QuestBuildConfig` (editor pin, API 32/34, OpenXR features, forbidden SDKs).
- Editor tooling (`Integration/Editor/`, all marked UNVERIFIED IN EDITOR): `SceneComposer` (menu and batchmode; refuses to touch scenes if the plan is invalid), `ProjectValidator`, `QuestBuild` (configure, validate, dev APK), `DesktopSimulation` (XRI Interaction Simulator; Standalone XR off in desktop mode, Android untouched). XR and URP code sits in its own assembly, so the composer depends on Core only.
- Pins: `ProjectVersion.txt` 6000.3.27f1 (`672d562a42a7`); `Packages/manifest.json`; single source `tools/unity/package_policy.json`; `validate_packages.py` (offline and `--online`); `unity_batch.py` (BLOCKED exit 2 without the editor).
- CI: `.github/workflows/checks.yml` runs checker self-test, whole-tree checker, logic harness, package policy and tooling tests on every push and PR, plus the seat diff check on PRs.
- Checker ownership table aligned with this plan (`.github/*`, `AGENTS.md`, `Game/Assets/XR/*` are Claude's). No Codex path changed.
- Design: `DESIGN_RECONCILIATION.md`. The prompt's beats match the approved acts in 7 of 11 points; flashlight, trolley, player-made lure and operating room are amendments D-1…D-4 for the owner. GAME_DESIGN.md is unchanged.
- Assets: site-level licence check (Poly Haven, ambientCG, Kenney CC0; Quaternius CC0 per pack; Freesound per sound; Sonniss forbids redistribution; Mixamo page 403, unverified). New rule 7: the repository is public, so Mixamo, Asset Store, Sonniss and Soundly files stay out. New rows E-09…E-11 (lights, decals, affordances), all generated.
Evidence:
- `run_logic_tests.py` → `Logic tests: 70 passed, 0 failed.` Mutations (Flow building Ward; missing-point check removed) each caught by one test.
- `check_rules.py --self-test` → `SELF-TEST PASSED`; `check_rules.py` → `0 error(s), 0 warning(s).`
- `validate_packages.py` → `0 error(s)`; tooling unittest `Ran 24 tests … OK`; mutation (forbidden checks off) → 2 failures.
- `validate_packages.py --online` → 7 non-core pins published, all need Unity 6000.0 or 2022.3; `com.unity.xr.management` `latest` tag reads 4.6.1, while OpenXR: Meta 2.7.0 requires 4.7.0.
- Roslyn parse of the editor scripts: 0 syntax errors (64 expected missing-type errors). Not a compile check.
- `unity_batch.py validate` → `BLOCKED: Unity 6000.3.27f1 not found`, exit 2.
Not verified: every Unity API call in `Integration/Editor/`, scene composition, desktop simulation, the APK, anything on a headset.
Next: review Codex 1's draft PR #1 (`codex/gameplay`), then the PR for this work. Codex 2 has pushed nothing since `73a3b67`.

## 2026-10-28 · Review of Codex 1 draft PR #1 (`codex/gameplay` `83b57d3`)
Kind: REVIEW
What: Not ready to merge. FIX-A-1 BLOCKER (`DesktopBuildStep` is an Interaction step building Bootstrap at 420, which breaks the composer's plan), FIX-A-2 MAJOR (`Unity.ugui` asmdef references; uGUI's assembly is `UnityEngine.UI`, to confirm at the first compile), FIX-A-3 MAJOR (mouse desktop mode collides with the XRI simulator; proposal: opt-in define `PZ_DESKTOP_MOUSE`), FIX-A-4 MINOR (camera lookup). Contract request "Act 1 door release": ACCEPTED IN PRINCIPLE as `GateId`/`GateReleased` + `IExperienceDirector.IsGateReleased` (contract v1.1). Awaits the owner and Codex 2's ACK; not merged. Full review: `docs/collab/REVIEW_GAMEPLAY_83b57d3.md`.
Evidence: branch `Logic tests: 80 passed, 0 failed.`; `check_rules.py --seat A` 0 findings; scratch merge onto `05d16be` → `Logic tests: 107 passed, 0 failed.`, checker, packages and tooling clean; temporary probe through the real `BuildPlan` → `PROBE errors=2` (band, scene), both on DesktopBuildStep. The probe was deleted, never committed.
Not verified: any of the branch in Unity or on a headset.

## 2026-10-28 · PR #2 merged to `main`
Kind: MERGE
What: The owner approved "Merge when green". PR #2 (`claude/technical-director` `a220834` → `main`) merged as merge commit `8b5e5cb`, with `--match-head-commit a220834` so nothing unreviewed could slip in. No force push; `73a3b67` stays an ancestor of `main`. Branch `claude/technical-director` kept and fast-forwarded to `8b5e5cb`.
Evidence:
- Before merging: `gh pr view 2` → `mergeable=MERGEABLE`, `mergeStateStatus=CLEAN`; both `checks / offline` runs (push and pull_request) `SUCCESS` on `a220834`.
- After: tree of `main` `5a781a4` equals the tree of `a220834`. Scratch worktree of `main`: `Logic tests: 70 passed, 0 failed.`; `SELF-TEST PASSED`; checker `0 error(s), 0 warning(s).`; `validate_packages.py` `0 error(s)`; tooling unittest `Ran 24 tests … OK`. Worktree removed.
Not verified: anything in Unity or on a headset (unchanged: no editor, no device).
Seen at the same time: Codex 2 pushed `integration/xr-systems` to `6bc4c42` (5 commits on `73a3b67`, 51 files, +3 103 lines; B1 port, B2 ward/audio/AI runtime, B3 PlayMode sources). `CODEX_B_LOG.md` reports 100/100 logic tests and a clean seat B check, ACKs contract v1.1 (`GateId`/`GateReleased`), and records that `gh pr create` failed with `Forbidden` from its environment, so it has no PR. `businessteen` is still not a collaborator. Not reviewed yet.
Next: review `integration/xr-systems` `6bc4c42` (Codex 2 first, per the production prompt), then Codex 1's fixes on PR #1. Both branches need `main` merged in.

## 2026-10-28 · Review of Codex 2 delivery (`integration/xr-systems` `6bc4c42`), draft PR #3
Kind: REVIEW
What: Not ready to merge: 0 BLOCKER, 3 MAJOR, 3 MINOR. All 12 findings of the first review (FIX-B-1…12) are resolved. New: FIX-B-13 MAJOR (assist always-on and adaptive assist switch off at the first progress), FIX-B-14 MAJOR (one mask for sight and hearing, so walls halve hearing; design: only closed doors), FIX-B-15 MAJOR (script-baked NavMesh not saved as an asset; verify at the first run), FIX-B-16 MINOR (audio occlusion mask empty), FIX-B-17 MINOR (exit sign loses its green in Blackout), FIX-B-18 MINOR (walls overhang door openings by 5 cm). Integration actions for Claude: INT-B-1 NavMesh agent radius (default 0.5 m vs 1.1 m openings), INT-B-2 shared physics layers proposed for contract v1.1, INT-B-3 first-run checks. Codex 2's ACK of v1.1 recorded; v1.1 now waits only for the owner. Opened draft PR #3 for the branch because Codex 2's `gh pr create` returned `Forbidden`. Full review: `docs/collab/REVIEW_XR_SYSTEMS_6bc4c42.md`.
Evidence:
- Branch: `Logic tests: 100 passed, 0 failed.`; `check_rules.py --seat B --base 73a3b67 --head 6bc4c42` and whole tree `0 error(s), 0 warning(s).`
- Scratch merge onto `main` `8b5e5cb`: no conflicts, `Logic tests: 127 passed, 0 failed.`, checker, `validate_packages.py` and tooling tests clean. Adding `codex/gameplay` `83b57d3`: no conflicts, `164 passed, 0 failed.`
- Temporary `BuildPlan` probe (not committed): seat B steps `PROBE errors=0`; with seat A, only FIX-A-1's 2 errors.
- Temporary director probe (not committed): `PROBE alwaysOn: start=Assist afterProgress=None | adaptive: start=Assist afterProgress=None`.
- Old-finding patterns in the diff: 0; Unity asset files committed: 0. Frozen refs `72be6d8` / `87bb494` unchanged.
Not verified: any of the branch in Unity (compile, generated scenes, mixer, the 6 PlayMode tests) or on a headset.
Next: Codex 2 merges `main` and fixes FIX-B-13…18; Codex 1 fixes FIX-A-1…4; owner decides contract v1.1.

## 2026-10-28 · Contract v1.1: Act 1 gate
Kind: CONTRACT CHANGE
What: The owner approved v1.1 in chat ("Approve v1.1", with the instruction to add it to Core and merge to `main` once CI is green). Added, additive only: `GateId { None = 0, Act1Door = 1 }` (`Ids.cs`), `GateReleased` payload (`Payloads.cs`), `GameEvents.GateReleased` (writer Atmosphere; at most once per gate per act start; gates close again at their act's `ActStarted`) and in `ResetAll`, `IExperienceDirector.IsGateReleased(GateId)` (`Interfaces.cs`). Checker writer table: `GateReleased: {Atmosphere}`. Docs: `AGENTS.md`, `ARCHITECTURE.md` section 5, both seat briefs, `WORK_PLAN.md` (what each seat does next), release checklist, test report, task board. Requested by Codex 1 (`CODEX_A_LOG.md`), answered in `REVIEW_GAMEPLAY_83b57d3.md`, ACKed by Codex 2 (`CODEX_B_LOG.md` at `6bc4c42`).
Evidence:
- `run_logic_tests.py` → `Logic tests: 72 passed, 0 failed.` (Core 45 + Integration 27).
- Mutations: `GateReleased.Clear()` removed → `GateReleased is missing from GameEvents.ResetAll`; Interaction file raising `GateReleased` → `ERROR event-writer … Interaction may not raise GameEvents.GateReleased (writers: Atmosphere).` Both restored.
- `check_rules.py --self-test` → `SELF-TEST PASSED`; `check_rules.py` → `0 error(s), 0 warning(s).`
Impact: nothing on `main` implements `IExperienceDirector`, so `main` is unaffected. Seat B's `ExperienceDirector` must implement `IsGateReleased` when it merges `main`; seat A's door can now wait on the gate. Shared physics layer names (INT-B-2) are not part of v1.1.
Not verified: Unity compile of the Core change (no editor).

## 2026-10-28 · Integration round 2: v1.1 merged, both seats integrated, first Unity run attempted
Kind: INTEGRATION
What: Owner prompt "CLAUDE CODE — PATIENT ZERO ROUND 2". PR #4 re-verified and merged to `main` as `44ea7b7`. `integration/round2` cut from it; Codex 2 `6bc4c42` and Codex 1 `83b57d3` merged unchanged (no conflicts). Integration exceptions E-1…E-5 (`4911c3c`): the director implements and raises the v1.1 gate, the Act 1 door uses Codex 1's own release hook, FIX-A-1 (step moved to `Player/Editor`), FIX-A-2 (`UnityEngine.UI`), FIX-A-5 (explicit test asmdefs). Claude tooling (`f0d670b`): required-sample import, INT-B-1 radius before the bake, `unity_batch.py first-run`, `tools/review/integration_scan.py` in CI. New findings: FIX-A-6, -7, FIX-B-19 (Act 3 cart built twice), FIX-B-20 (`HapticRequested` never raised), FIX-B-21 (operating theatre, needs D-4). Full record: `INTEGRATION_ROUND2.md`.
Evidence:
- PR #4 head `5202249`: CI SUCCESS ×2; `Logic tests: 72 passed, 0 failed.`; 0 removed lines in `Core/Contracts`; 104 enum values unchanged.
- `integration/round2`: `Logic tests: 166 passed, 0 failed.`; checker `0 error(s), 0 warning(s).`; integration scan `0 error(s), 1 warning(s).` (21 errors on the plain merge); `BuildPlan` probe `errors=0` over 11 steps; packages 0; tooling 30/30.
- `--seat claude --base 9d3e864 --head 4911c3c`: 13 ownership warnings, exactly the exception files; tooling commit 0.
- `unity_batch.py first-run` → `BLOCKED: Unity 6000.3.27f1 not found`, exit 2. No Hub, editor, Android SDK, JDK or `adb` on this machine (290 GB free).
Not verified: Unity compile, package resolution, generated scenes, EditMode/PlayMode in Unity, desktop simulation, APK, headset. `packages-lock.json` and generated `ProjectSettings` were not hand-written.
Next: owner installs Unity 6000.3.27f1 + Android (or Codex 1 runs `first-run`); Claude runs `first-run`, commits what Unity generates and fixes what it reports; the seats take section 7 of `INTEGRATION_ROUND2.md`.

## 2026-10-30 · First Unity run: project compiles, scenes build, the game plays in the Editor
Kind: INTEGRATION
What: Owner installed Unity 6000.3.27f1 (no Android module) and asked "open and run the game". `unity_batch.py first-run` on `integration/round2`, four integration exceptions in seat A for real compiler errors (E-6…E-9), the built-in Video module for the XRI hands demo, and tooling fixes: licence detection, URP set by desktop simulation, composer requires a render pipeline, `first-run` order, `PlayPipeline` (`PatientZero/Play`, `Set Up And Play`). Full record: `INTEGRATION_ROUND2.md` section 11.
Evidence:
- `open`: PASS, exit 0. `import-samples`: PASS. `compose`: PASS, 0 errors (recomposed under URP in the Editor: PASS). `desktop-sim`: PASS.
- `editmode-tests`: 165/166 (FIX-B-22). `playmode-tests`: 6/10 (FIX-A-9, FIX-B-23). `build-apk`, `validate`: BLOCKED, no Android module.
- Editor play via `PatientZero/Play`: calibration → Ward, countdown 02:00 → 01:54, PA captions, 0 console errors (screenshots in the session). FIX-A-8 found there: a pause during the opening fade leaves the screen black.
- Tooling unittest: all pass; package policy 0 errors.
Not verified: batch `first-run` in its new order (the owner's editor held the project), a full 120-second run, APK, headset.
Next: seats take FIX-A-8, -9 and FIX-B-22, -23 with the earlier fixes; owner adds Android Build Support (+ OpenJDK, SDK & NDK) to 6000.3.27f1 for the APK; Claude re-runs `first-run` and the PR gate.

## 2026-10-31 · Three floors: contract v1.3 and integration exception E-19
Kind: CONTRACT CHANGE + INTEGRATION
What: The owner, after the Windows playtest: "i asked for 3 floors". Offered keeping one floor or adding two, they chose "Add 2 new floors of content". The ward becomes Floor 3; Floor 2 (laboratories: fuse, fuse box, stair door override, a researcher lured by the spark) and Floor 1 (lobby: generator valve, lockdown release, a patrolling guard and a sleeping porter, the ending at the main doors) follow by stairs (fade, teleport, `StairArrival`). Design: `GAME_DESIGN.md` section 11. Contract v1.3 is additive (`ARCHITECTURE.md` section 5); every seat file touched is listed under E-19 in `INTEGRATION_ROUND2.md`. Also fixed: a carried plug could never snap on flat screens (`SocketLogic.CanSnapFlat`, 25 cm).
Evidence:
- `run_logic_tests.py` → `Logic tests: 218 passed, 0 failed.` (200 before; new: `FloorDressingTests` ×5, six `WardLayoutTests`, four director tests, `LowerFloorRestoresMarkEveryEarlierObjective`, `FlatScreenSocketSnapIsGenerousButBounded`, the fixture-policy floor case). `check_rules.py` → `0 error(s), 7 warning(s)` (the same seven `ReleaseProbe` Debug.Log warnings as before).
- Unity 6000.3.27f1 `compose`: first FAIL, two compiler errors the offline suite cannot see (it does not compile Runtime or Editor code): `PhysicalPuzzle.Act` property vs its `Act(ActStarted)` handler (renamed `BoundAct`), a duplicate `sign` local in `HospitalBuildSteps`. Then PASS, 0 errors, 59 LayoutTags.
- `HospitalValidation.ValidateGenerated` in batch mode: `Hospital validation passed: saved navigation, routes, actors, lighting and audio bindings.` (new routes: Lab B to the Floor 2 landing, the lobby patrol, reception to the lobby; 7 zombie actors).
- `editmode-tests`: first 221/222 (`HospitalGenerationTests` found four `ExitSign` objects; the floors' signs are now `FloorExitSign`, still lit by the lighting controller), then **222/222**. `playmode-tests`: first 22/23 (the autoplay gripped the Floor 2 lever under the stairs fade, which drops held items 0.35 s in), then **23/23**; the autoplay logs Awakening 30.7 s → Machinery 41.3 → Darkness 43.9 → Exit 43.9 → Laboratory 47.4 → Lobby 53.0 → Complete.
- `build-windows`: PASS, 0 errors, 121 MB. `run_probe.py --rounds 2 --shots`: PASS, 2/2 rounds complete (52.4 s, 50.7 s), 0 errors or exceptions. The screenshots showed the floor name and the new hints cut off at two lines (the ward's Act 1 and Act 4 hints were already cut); `WorldUi` now shrinks hint and clock text to fit and puts the clock on the title row. Rebuilt and re-probed: PASS 2/2, every hint whole.
Not verified: a person playing Floors 2 and 1 (catch rates of the guard and the researcher are untuned); VR on a headset; the Android phone build with the floors.

## 2026-11-02 · Art pass: animated zombie, textured hospital, flat-screen post-processing (E-20)
Kind: INTEGRATION
What: Side session of the integration session, branch `claude/art-pass` (worktree `patient-zero-art`), from `a1af6fb` with `integration/round2` `47bc610` (three floors) merged in. Compose-time Integration step `ArtPassStep` (Ward 980, Bootstrap 980): every `ZombieAnimationController` wears the CC0 Quaternius zombie (glTFast 6.20.0, Mecanim; looping Idle/Walk/Run_Arms blended on `Speed`), its `Animator` and `Visual` wired and the box mannequin hidden; every box built with a seat material or the pipeline default gets a generated URP/Lit material (procedural albedo + normal maps: terrazzo floor, two-tone blockwork with chair rail, ceiling tiles, scratched metal, fabric, wood, grime) and a cube mesh with world-metre UVs; a global post-processing volume (bloom, ACES, grading, vignette, grain) runs only while no headset is active. `QuestBuild.ConfigureUrp` assigns the URP post-processing data; `ReleaseBuild.BuildWindows` turns URP HDR on and removes the Standalone OpenXR loader for the Windows build and puts both back. Zombie `.meta` GUIDs taken from `47bc610`. Seat A exception: one credits sentence in `UiBuildStep.cs`. Record: E-20 in `INTEGRATION_ROUND2.md`.
Evidence:
- `build-windows`: PASS, 0 errors, 0 warnings, 124 MB (floors build: 121 MB). Compose: `art pass: 7 zombies dressed, 285 renderers re-materialled, 278 boxes given world-metre UVs`. HDR and the Standalone loader restored (`xr-loader: OpenXR only for Standalone`). Player.log no longer shows `XR_ERROR_RUNTIME_UNAVAILABLE`.
- First compose FAIL: `Zombie model height 0.01194441 m is implausible` (`BakeMesh` ignores the armature's x100 parent scale); now skinned on the CPU from bone matrices and bind poses.
- `run_probe.py --rounds 2 --runs 2 --shots` on the final exe: PASS 2/2 runs, rounds 52.6/51.1 s and 52.3/51.0 s, 0 errors. On the previous exe (same game logic, older grading): one PASS 2/2 (`focus pause; resuming` in round 2), one run whose round 2 never ended after a focus loss, and one stuck 240 s in Exit (`act Exit did not advance in 12 s` x20). See follow-ups.
- Grading measured on the probe shots (luminance below the HUD band): a 0.015 lift put blacks at 33/255 (a grey veil); removed, blacks are 0-2/255 and the alarm lights carry the image. Floors build for comparison: 24-25/255.
- `check_rules.py`: 0 errors, 7 warnings (the existing `ReleaseProbe` ones). `run_logic_tests.py`: 218 passed, 0 failed. `validate_packages.py`: 0. Tooling tests: OK. `integration_scan.py`: 1 error that `47bc610` already has (`InteractionBuildStep` is `partial`; the scan calls it a duplicate type), 1 existing warning.
Not verified: EditMode/PlayMode tests in Unity; Quest or any headset (the volume is off when `XRSettings.isDeviceActive`, never observed on a device); the Android phone build (post-processing runs there with HDR off: ACES on LDR, look and cost unmeasured); Editor play; a person playing.
Follow-ups: `ReleaseProbe` waits with scaled `WaitForSeconds` inside a round, so a focus pause during a wait stalls the run until the process timeout, and the Exit stall needs a cause (both in a file this pass did not edit); `integration_scan.py` should accept `partial` types; the zombie model is over the C-01 budget (2 skinned meshes, 50 joints).

## 2026-11-02 · Horror pass: news broadcast, a horde of 28, walkable stairs, blood and voices, exit scare (E-21)
Kind: INTEGRATION
What: Owner request made directly in the art-pass side session, on `claude/art-pass` after `7145101`: real stairs instead of the floor teleport, about 20 more zombies (eight hunting by their senses, the rest following them), a hospital with white tiled walls and blood, people crying, a jump scare at the final exit, and a ceiling news screen that holds the player until the bulletin ends. Genre and mood only, no content from any existing game. Contract v1.4 (`Ids.cs`): six recorded-voice cues 900-905 and the horde ids 101-121. Integration steps `HorrorPassStep` (Ward 985: the TV in B-7 with headlines, ticker and a failing picture; soundscape; exit scare) and `StairwellStep` (Ward 986: two walkable stairwells, each two flights of ten risers with a fire-door lobby where the two built halves meet; doorways cut into the stair rooms). The art pass adds white tiles, blood on walls, floors and ceilings with two messages (`ArtBlood`), and three skin tints and sizes for the horde. Seat files touched: E-21 in `INTEGRATION_ROUND2.md`. Sounds: the four OpenGameArt groups the owner approved (A-07: CC0, except one CC-BY 3.0 crying voice, credited in the game and the release notes). Design: GAME_DESIGN.md section 12.
Evidence:
- Compose PASS, 0 errors, 0 warnings: `zombies: 7 story, 21 horde (3 hunters, 18 followers)`; `art pass: 28 zombies dressed, 285 renderers re-materialled, 278 boxes given world-metre UVs; blood: 31 on walls, 31 on floors, 4 on ceilings, 2 messages`; `horror pass: news bulletin 64.0 s, 9 headlines`; `stairwells: 2 walkable (Floor 3 to 2, Floor 2 to 1), 184 pieces, 20 risers each`.
- Compose failures on the way: CS0119 (a method named like the `Stairwell` class); then the doorway check refused the new opening on floor 3, where the painted `Stair3_FlightDown` panel and the "DOWN TO FLOOR 2" sign stood. The step now removes the painted flight and lifts a sign in the opening onto the door header; anything else in the way still stops the build.
- `build-windows`: PASS, 0 errors, 0 warnings. `run_probe.py --rounds 2 --shots`: PASS 2/2, rounds 126.9 s and 125.8 s, 0 errors or exceptions. The probe logged that the news held the player "for another" 58.0 s and 58.5 s (64 s bulletin). The probe walked the capsule down each stairwell with no teleport: floor 2 in 8.5 s, floor 1 in 9.5 s, both rounds. Shots viewed: the TV with headlines and captions, both stairwells top and bottom, a horde member in the Lab B doorway, the Act 4 pack beside the player in the vestibule, the exit scare at the main doors. A level view from the top doorway passes over the whole flight, so the probe now tilts the view 40 degrees down for that shot (rebuilt: PASS 0/0).
- Probe on the rebuilt exe, first run: FAIL. Round 1 completed (142.1 s, one `act Exit did not advance in 12 s` retry), but its floor-1 walk took 4.5 s, its top-of-stairs shot shows a flat floor, and the game then quit cleanly during the 2 s results wait without the probe's `RESULT` line. The probe quits only after that line, so something outside it closed the game; the most likely cause is mouse and keyboard input reaching the probe's window on the owner's desktop. Re-run on the same exe: PASS 2/2, rounds 126.8 s and 125.7 s, walks 8.6/9.5 s and 8.5/9.5 s, 0 errors. Both top-of-stairs shots show the flights and their glow strips.
- `Failed to create agent because there is no valid NavMesh`: 56 lines as the scene loads (28 zombies x 2). The art-pass build logged 14 (7 x 2), so the pattern is not new; the zombies move in the shots.
- `integration_scan.py` read the two private nested `Kit` classes (`ZombieArt`, `StairwellStep`) as one duplicate type; the stairwell's is now `StairKit` (`unity_batch.py open`: PASS). The scan is back to the one existing `partial` error.
- `.gitignore` now ignores `Game/Assets/ThirdParty/Mixamo/`, so an owner download there cannot be committed.
- Before the horde and the stairs: build and probe PASS, news 57.9 s, round 110.4 s. A black exit-scare shot (camera inside the model) led to the 1.05 m stop distance; the TV labels stayed lit after the bulletin and now switch off with it.
- `run_logic_tests.py`: 226 passed, 0 failed (8 new `HordeTests`). `check_rules.py`: 0 errors, 9 warnings (the `ReleaseProbe` log lines). `validate_packages.py`: 0.
- A leftover probe of the previous exe, from a chain started before a context reset, held `D3D12Core.dll` and failed one build; it was stopped and the build re-run.
Not verified: any headset (VR keeps the fade between floors and never enters a stairwell; the news lock turns the body once and holds movement, never seen on a device); EditMode and PlayMode tests in Unity (`HordeTests` ran only offline); `HospitalValidation` NavMesh routes with the horde (menu item only); the phone and iPad builds; a person playing (catch rates with packs are untuned, and the stairwell is dark unless the torch is on). The news captions come from a machine transcript nobody has checked. The scare has no controller rumble: only AI and Atmosphere may raise haptics.
Follow-ups: the owner wants to choose a better zombie model from Mixamo before it goes in. They download it with their own Adobe account into the ignored `ThirdParty/Mixamo` folder; the game must fall back to the CC0 Quaternius zombie when it is absent (CI, a fresh clone). `integration_scan.py` still reports the existing `partial` duplicate-type error. The probe window takes focus on the desktop it runs on, so outside input can spoil a run.

## 2026-11-11 · Two-minute version, step 1: blind zombies, freeze or die (E-22)
Kind: INTEGRATION
What: The owner approved the 2-minute plan on 2026-11-10 ("Approved, start"; Claude Doc "PATIENT ZERO Game Plan"), to be built in its 7-step order with each step checked on PC before the next. This is step 1, on `claude/two-minute` from `c97177c`. Every zombie now hunts by sound alone: a grab needs a player noise less than 1 s old, so a player who stands still is never grabbed. The three planks are gone. The room door has one push bar and swings open onto the door zombie, which screams in the player's face (on Standard scares the first time in a run also brings the stinger, the silence and the light pulse). It loses a player who keeps still and shuffles off down the corridor, and the act ends once it has gone; if it is held up it is taken away after 12 s, never as a fail. The first grab of a life is a struggle: 3 s to make 5 presses (Space or left click, a tap on a touch screen, a hard shove of either hand in a headset). Losing it, or a second grab, restarts from the bed. New signs: PUSH THE BAR and THEY HEAR YOU. The seat files changed are recorded as integration exception E-22 in `INTEGRATION_ROUND2.md`. There is no Core contract change.
Evidence:
- `run_logic_tests.py`: 241 passed, 0 failed.
- `compose`: PASS, 0 errors, 0 warnings. "59 LayoutTag(s), every required WardLayout id placed once": the old plank points stay, as the door's lock and sign.
- `build-windows`: PASS, 0 errors, 0 warnings, 130 MB.
- `run_probe.py --rounds 3 --shots`: PASS 3/3, 0 errors or exceptions, twice on the final source. Rounds took 130.3 s, 140.0 s and 139.5 s; the news held the player 58 s each time.
  - Round 1 stood still at the open door: no grab, and the act ended 11.6 s after the door opened.
  - Round 2 kept stepping. It was grabbed 2.3 s after the door opened, won the struggle, and the act ended 22.5 s after the door opened.
  - Round 3 lost the struggle and woke in the bed 6.5 s later. It then stood still and the act ended.
  - Stairs: 8.5 s and 9.5 s, as before.
- Shots viewed: the door swung open with the zombie in the doorway; the struggle panel reading "IT HAS YOU 0 / 5 / Press SPACE or click fast to break free!"; the room from the bed with PUSH THE BAR above B-7.
- Defects found in the shots and fixed:
  - The panel showed the last grab's "YOU DIED" for the first frame of every grab.
  - The push-the-bar hint stayed on screen while the zombie screamed.
  - PUSH THE BAR overlapped the B-7 room number.
  - THEY HEAR YOU, at eye height beside the door, filled the view of a player standing there as a giant "THE".
  - The struggle count wrapped onto a second line that the panel cuts off.
- Probe failures on the way:
  - First run: "the door zombie grabbed a player who stood still" in round 1. Four later runs did not reproduce it. The probe logged no noises then, so the cause is unknown; it now logs every player noise and every grab of the door scene.
  - The probe waited on scaled time, which stops during a grab. It once sat through the whole 3 s struggle and lost it. A grab now ends the wait.
  - In three runs a lever (Act 4, once the lobby) never moved, so the act retried until the round timed out. The diagnostic showed "Lever at 0.00" after holds of up to 10 s at 144 fps.
    - The rig drops every held grip each frame while the game window has no focus (`TrackingRecovery`, by design: an alt-tab lets go). The valve, plank and bar finish within one grip sample, so only the lever, which must be held for 2 s, was hit.
    - The probe now tells the rig it has focus before it holds a lever (logged once per run), and holds until the lever is down.
    - In both passing runs the window had focus, so that path did not run. Focus is the only cause the code leaves, but it was not seen directly.
- `check_rules.py`: 0 errors, 19 warnings (the probe's log lines). `integration_scan.py`: only the existing `partial` duplicate-type error and the `HapticRequested` warning.
- Unity tests, first run for this work: PlayMode 23/23 passed (the rewritten `FirstEncounterNavigationTests` door-zombie test and the autoplay included). EditMode 244/245: `HospitalGenerationTests.OwnedBuildStepsAreRegisteredOnceWithPublicComposerSignatures` failed on the recorded-cue catalogue step (Bootstrap 610). That step came with E-21 (`c97177c`), whose Unity tests were never run, so the test's list was never updated. The list now includes it, and EditMode passes 245/245.
Not verified:
- Any headset: the shove, headset footsteps, and standing still in VR. This is the owner's check.
- A person playing: whether 3 s and 5 presses is fair, and how far footsteps carry.
- Reduced scares, and the phone and iPad builds.
Follow-ups:
- The owner checks step 1 on PC before step 2 (opening: bed wake-up, street of cars, 15 s news, crying).
- `IntegrationPlayTests` still holds a lever for a fixed 2.1 s, as the probe did (Seat A test file, left as it was).
- The unexplained first-run grab.
- The probe's main-loop waits still freeze during a focus pause, as before E-22.

## 2026-11-11 · Owner's step 1 feedback: no instructions, dead bodies, the look-away body (E-23)
Kind: INTEGRATION
What: The owner played and reported: no struggle (death at once and a restart on the same floor), no guns, no zombie that stared or screamed, no jump scare. They asked for no instructions on the walls or the screen, dead bodies on the floor, and a body that hangs where there was nothing once they look away. Their report matches the old 2026-10-31 build in the main folder (checkpoint restart, instant death, no door scene), not step 1, which they may not have opened: the game's own `Player.log` was last written on 2026-11-02, and the step-1 build writes it on every launch outside the probe. Guns are plan step 4 and the leap at the exit is step 6.
- Instructions: the PUSH THE BAR, MOVE QUIETLY / LISTEN and THEY HEAR YOU signs are gone. On Floor 2, "LAB B - DO NOT WAKE" is now "LAB B" and "GENERATOR / PRIME BEFORE RELEASE" is now "GENERATOR"; FUSE MISSING / SPARE FUSES: LAB A and STAIR DOOR OVERRIDE / NEEDS POWER are gone. On screen there is no title during play and no objective, assist or completion hints; the controls show only in calibration and pause; sound captions need Sound direction visuals (speech still follows Subtitles); the struggle panel no longer shows while the player fights; death reads only "YOU DIED".
- Struggle without instructions: on a flat screen any key except Esc and any mouse button counts as a press, and each press jolts the view for 0.14 s.
- Dead bodies: new Integration step `DeadBodiesStep` (Ward 987). Four CC0 zombie models posed as corpses (the Death clip's last frame, a blood pool under each) lie in the corridor, the vestibule, behind the lobby desk and in the lobby. Two hang from the ceiling on a rope, feet 0.35 m over the floor: in the corner of B-7 and at the corridor's dead end.
- The look-away body (`LookAwayRule`, `LookAwayScare`): the spot must first be seen empty (0.4 s within 40°), then be out of view for 1.5 s (beyond 65°, or behind a wall, within 12 m). Then the body is there, the rope creaks 0.7 s later, and when the player sees it a stinger plays (not with reduced scares). It sways. Every new life (the bed) empties the spot again. Ambient sounds only, so zombies do not hear it.
- Seat files changed are recorded as integration exception E-23 in `INTEGRATION_ROUND2.md`. No Core contract change.
Evidence:
- `run_logic_tests.py`: 248 passed, 0 failed (7 new `LookAwayRuleTests`).
- `compose`: PASS, 0 errors, 0 warnings. "dead bodies: 4 on the floor, 2 hanged (look-away)"; both hanged bodies "feet 0.35 m over the floor, 1.66 m tall, ceiling 2.78 m, eyes on 1.44 m".
- `build-windows`: PASS, 0 errors, 0 warnings, 130 MB.
- Unity tests on the final source: EditMode 252/252, PlayMode 23/23.
- `run_probe.py --rounds 3 --shots`: PASS 3/3, 0 errors or exceptions, on the final source. Rounds took 139.3 s, 140.1 s and 140.6 s.
  - Round 1 looked at the B-7 corner, turned its back, turned round: "the body is there now", then "spotted". In the corridor it looked at the dead end, turned to the stairs, turned back: the same.
  - Round 2 won the struggle; round 3 lost it and woke in the bed 6.6 s later. In round 3 the B-7 body appeared and was spotted with no look-back from the probe, during the door scene.
- Shots viewed: the hanged bodies in the B-7 corner and at the corridor's dead end (head bowed, arms hanging); a dead body lying in the corridor; the door zombie screaming in the doorway; the struggle with no text on screen; the room and corridor with no instruction signs; no title on screen during play, only the clock.
- Defects found and fixed on the way:
  - `check_rules.py` flagged five errors in the new `LookAwayScare`: a scene search for the game state, and log strings built in `Update`. It now arms on the run's first `ActStarted` and builds its log lines once.
  - The first hanged bodies hung about 0.35 m too high (a fixed 0.78 m rope). The rope is now sized from the posed body, and the eyes' target is the body's chest as it hangs.
  - "PATIENT ZERO" stayed at the top of the screen during play. It now shows only in pause, calibration and the results.
  - The probe looked at the B-7 corner without ever looking away, so the body (correctly) never came; it now turns its back first.
  - One run stopped in round 3 with the player held for 300 s. The game window had lost focus, and an unfocused window holds the struggle's clock (E-22), so the lost struggle never ended. The probe now tells the struggle it has focus, as it already did for a held lever. In the passing run the window had focus, so that path did not run.
- `check_rules.py`: 1 error, 25 warnings; the error is the probe's `SendMessage` from before this change. `integration_scan.py`: only the existing `partial` duplicate-type error and the `HapticRequested` warning.
- Privacy scan of the change: no paths, names or keys.
- The 112 "Failed to create agent because there is no valid NavMesh" lines in the probe log were already in the 2026-11-02 probe log (14) and the 2026-10-31 player log (28).
Not verified:
- Any headset: whether the hanging bodies read in VR and whether 65° fits a headset's view; the hands warning still shows there.
- A person playing: whether the reveal is noticed and scary, and whether struggling without instructions is found.
- Reduced scares, and the phone and iPad builds.
Follow-ups:
- The owner plays this build, at the path given in the chat, before step 2.
- The countdown can read 00:00 while act 1 is still going (seen in a shot). It predates this change and is left as it was.
- Kept on purpose, for the owner to decide: EMERGENCY RELEASE, LOCKDOWN RELEASE, RESTRAINED - KEEP CLEAR, UPPER FLOORS SEALED, EMERGENCY EXIT, the room and place names, the B-7 note, the blood writing, speech subtitles, the crosshair, the countdown and the VR hands warning.

## 2026-11-11 · Owner's second playtest: the unseen room body, the news lock, zombies through walls (E-24)
Kind: INTEGRATION
What: The owner played the step 1 + E-23 build and reported: they never saw the body hanging in the room; the news still held the view on the TV for about a minute; zombies standing by a wall pushed their hands and part of the head through it into the next room. They also asked where the gun is (not built: plan step 4), for a scarier zombie model and for much better graphics; those are answered in the chat and wait for the owner's choices.
- The room body was revealed while the news held the view, then sat at the edge of a dark view, and the player could leave the room before turning back. Now `LookAwayScare` reveals only while the player is in the body's room, the rope creaks again every 4.5 s (up to 5 times) until the body is seen, and both hanged bodies get a faint, untextured cold lift (material emission, not a light: lights belong to Atmosphere and Environment).
- The news: `NewsBroadcast` no longer turns or holds the view, or holds movement. The recording is cut to 17.4 s (three sentences: the outbreak, the two hospitals, the violence) in a new file `ZombieNews_Zisongbr_Short.ogg` made from the CC0 original; the screen shows three headlines. Its `LICENSE.md` notes the cut.
- Walls: the NavMesh agent keeps 0.25 m from walls, but the model's arms reach 0.6 m ahead. New `WallClearance` on every zombie model eases the model (never the agent) back from walls in reach of its arms and head, ahead, at the front corners and at the sides, and lets it return when the wall is gone. Players, loose props and other zombies never push it. The NavMesh was not widened, since that would block doorways.
- The four floor bodies are kept inside their room (their arms are spread, about 2.5 m from hand to hand). The corridor and vestibule bodies now lie across the floor instead of with a hand in the wall.
- Seat B's `RecordedCueCatalog` change is recorded as integration exception E-24 in `INTEGRATION_ROUND2.md`. No Core contract change.
Evidence:
- `run_logic_tests.py`: 248 passed, 0 failed.
- `build-windows` (it composes first): PASS, 0 errors, 0 warnings, 130 MB. Compose: "news bulletin 17.4 s, 3 headlines"; both hanged bodies "feet 0.35 m over the floor, 1.66 m tall, ceiling 2.78 m, eyes on 1.44 m"; the floor bodies moved 0.76, 0.77, 0.57 and 0.48 m off the walls.
- Unity tests on the final source: EditMode 252/252, PlayMode 28/28 (5 new `WallClearanceTests`: a wall ahead pushes the model back 0.1 m, a wall beside and ahead push both ways and it returns when they go, a player or a loose prop in front never pushes it, the push turns with the zombie).
- `run_probe.py --rounds 3 --shots`: PASS 3/3, 0 errors or exceptions, on the final source. Rounds took 91.1 s, 92.5 s and 91.9 s (139 to 141 s before: the news no longer holds the player).
  - Every round: "the news bulletin played for another 11.3 to 11.9 s; the view was free".
  - Round 1: "B-7 corner: the body is there now", then "spotted"; the same at the corridor's dead end.
  - Wall clearance: 5 of 28 zombie models eased off a wall at each act (4 in the lobby), at most 0.15 m.
- Shots viewed: the news on the TV with the second headline and the subtitle; both hanged bodies (pale grey, visible in the dark corner); the corridor body lying across the floor; the door zombie; the lobby.
- Defect found and fixed on the way: the first glow lit the skin texture itself, so both hanged bodies glowed bright green (seen in the shots). The emission is now a faint grey with no texture.
- `check_rules.py`: 1 error, 26 warnings; the error is the probe's `SendMessage` from before; the one new warning is the probe's wall-clearance log line. `integration_scan.py`: only the existing `partial` duplicate-type error and the `HapticRequested` warning.
- Privacy scan of the change, scenes and new assets: no paths, names or keys.
- The 112 "Failed to create agent because there is no valid NavMesh" lines are the same count as in the E-23 probe log.
Not verified:
- A person playing: whether the room body is now noticed, and whether the shorter news reads well.
- No person has listened to the cut recording to check the joins (the cut points were chosen from the quiet gaps in its loudness).
- Any headset, reduced scares, and the phone and iPad builds.
Follow-ups:
- The owner plays this build before step 2.
- The owner's other asks wait for their choices: the gun (plan step 4), a scarier zombie model (their Mixamo download, a free model found for them, or a darker, bloodier version of the current one), and graphics (a PC "Ultra" quality level in URP, or HDRP for PC only).

## 2026-11-13 · The revolver (owner: "Build the gun next"; plan step 4; E-25)
Kind: INTEGRATION
What: The revolver of the owner's two-minute plan, as agreed in the plan doc: found on a dead police officer halfway through floor 3; six rounds, never reloaded, none to find; a headshot kills, a body shot staggers; every shot draws every zombie within about 20 m; on a flat screen the right mouse button aims and the left fires (the left still grabs when not aiming); in a headset it is drawn from a hip holster.
- Rules (`RevolverRules`, engine-free, 11 EditMode tests): six rounds, a 0.45 s hammer cycle, a seventh pull clicks, a new life resets it; hits are a head ellipsoid and a hips-to-neck capsule tested against the ray up to the first wall, the nearer part winning and a tie going to the head (`ShotGeometry`).
- `Revolver` (runtime): flat screen, look at the gun within 2.4 m and click to take it; holding the right button raises it to the eye; the left button fires along the view. Headset: XRI grab, the trigger fires along the muzzle, letting go stows it in `HolsterSocket` at the right hip. The shot is the `RevolverShot` cue (noise 20 m), played before the bullet lands; blood or dust specks; a self-lit muzzle-flash quad, not a light. Nothing on screen counts the rounds; the rounds in the cylinder go one by one.
- Zombies (seat B, exception E-25): `ZombieAgentShell.Shot(head)` kills or staggers 1.2 s; a dead zombie keeps its Death pose until the next life; new `HitReact` and `Death` states in the zombie controller (`ZombieArt`).
- `ZombieHitbox` on every dressed zombie, sized at compose time from the model's own skinned mesh: the head is an ellipsoid in the head bone's space spanning the 2nd to 98th percentile of its vertices on each axis (the model's cartoon head, ears, jaw and tongue included); the body capsule's radius is the 70th percentile of the torso vertices' distance from the hips-to-neck line, kept within 0.09-0.4 m. Arms and legs do not stop a bullet.
- `RevolverStep` (Ward 988): the officer (the zombie model in its Death pose, dark navy tint, cap beside him, pool of blood) across the floor-3 corridor at x 6.8, the gun by his right hand, the holster, the hit areas.
- Contract v1.5: `RevolverShot` 906, `RevolverDryFire` 907, `RevolverHandle` 908; CueTable rows, fallback recipes in `ProceduralSound`, and three synthesized recordings (`tools/audio/make_revolver_sounds.py`; nobody has listened to them).
- Seat A's `FlatInput` (`GunInHand`, `Aim`, `Fire`) and seat B's files are recorded as integration exception E-25 in `INTEGRATION_ROUND2.md`; `GAME_DESIGN.md` section 13 and the cue table; `ASSET_MANIFEST.md` A-08, E-12, C-05.
- The probe: round 1 takes the gun at the start of Act 3, waits for a zombie that can hear (the director holds the patroller for its first 10 s), body-shoots then head-shoots a zombie with no other zombie in the line (a deaf pack follower first, so the hearing ones are left to answer), checks the shot drew the zombies in hearing, empties the gun and pulls once more; rounds 2 and 3 check the gun is back, full, and no zombie stayed dead.
Evidence:
- Windows build (`unity_batch.py build-windows`, which recomposes first): PASS, 0 errors, 0 warnings. Compose measured 28 zombies' hit areas: on average the head 0.82 x 0.59 x 0.55 m along the head bone's axes, its centre 1.24 m up; the body capsule 0.289 m round.
- Release probe, 3 rounds with screenshots: RESULT PASS, 3/3 complete, 0 errors or exceptions logged. Round 1: the gun taken with 6 rounds; one zombie that can hear came into range after 7.5 s (the patroller); a body shot on pack follower 105 at 3.6 m hit Body and staggered it; the shot drew 1 of 1 zombie in hearing; a headshot on 105 hit Head and killed it; the drawn patroller (3) came to the player and a headshot killed it; then the shots into the floor, the seventh pull `Empty`, 0 left. Rounds 2 and 3: the gun back on the officer, 6 rounds, at the start of Act 3.
- One thing the probe showed: the second pack follower (106) walked into the player while the gun was being emptied at the floor, and that shot counted as a point-blank headshot (the shot started inside its head shape).
- First probe run (before the probe fix): the body shot hit the patroller standing in the line of fire instead of the chosen zombie, and no zombie could hear (the director holds the patroller deaf for the first 10 s of Act 3). The probe now waits for a zombie that can hear and shoots only with no other zombie in the line.
- Shots viewed: the officer lying across the corridor with the gun by his hand (brightened to check: the model's death pose has his hips 6 cm and torso 9 cm off the floor; the big cartoon head stands 0.9 m); the gun at the bottom right in hand; three zombies in the side room at the headshot; the patroller reaching for the player after the shot.
- Unity tests: EditMode 263/263 passed (the 11 revolver tests included); PlayMode 28/28 passed. Engine-free logic tests 259/259.
- `check_rules.py`: 1 error, 41 warnings, all `debug-log`; the error is the probe's scene search from before. `integration_scan.py`: only the existing `partial` duplicate-type error and the `HapticRequested` warning.
- Privacy scan of the change, scenes and new assets: no paths, names or keys.
- The 112 "Failed to create agent because there is no valid NavMesh" lines are the same count as in the E-23 and E-24 probe logs.
Not verified:
- The mouse path (right button aims, left fires) was not driven by the probe: the probe calls `Revolver.Pull` with its own aim. A person must try it.
- Any headset: the grab, the trigger, the hip holster and the stow-on-release are untested (VR checks are the owner's).
- Nobody has listened to the three synthesized sounds.
- Phone and iPad builds: there is no fire button on touch yet.
Follow-ups:
- The owner plays it: the feel of aiming, the kick, the flash, the sound, and whether 20 m draws too many zombies.
- Owner decisions still open from the plan: whether zombies hear the empty click; whether shooting is possible while held.
- A pack whose hunter is shot stays in play, standing where it is.
- A zombie standing on the player takes any shot as a headshot (the shot starts inside its head shape); fair at point blank, but the owner may want it otherwise.
- The woman's lunge and the dead doctor (later plan steps) are not built, so the officer lies alone.
- The scarier zombie model and the graphics level still wait for the owner's choices.

## 2026-11-13 · Floor 2 out, torch on, real doors (owner playtest feedback; E-26)
Kind: INTEGRATION
What: The owner: "for now concentrate the 2 min gameplay. can you just takeout the floor 2 from it", "keep the flashlilght on default all the time, and turning it off it's the players wish", and the doors should "open like an actual door instead when i open the door the middle stuck to the ceiling and spins in the middle". They also asked whether visible hands would look good (answered in chat; nothing built).
- Floor 2 out: `DirectorCore` follows the ward's stairs with the lobby (act 6), not the laboratories (act 5). `StairwellStep` builds one stairwell, floor 3 to the ground floor: the top half and the fire-door lobby at floor 3's stair room as before; the lobby again and the bottom half 3.6 m higher and 30 m along, ending at the lobby's stair room (`Stairwell.Shift` replaces `Rise`). Floor 2's rooms, puzzles and Z5 stay in the scene, sealed, for the bigger game (its stair rooms are no longer opened). Floor 3's two stair signs name the ground floor. No Core contract change.
- Torch: `DesktopInteraction` starts with it on; F (LIGHT on touch) turns it off and on. `HandheldLight` no longer dies in the power cut. Headsets still have no torch.
- Doors: the floor-3 emergency door and the lobby's main entrance (and floor 2's sealed stair door) are double doors, two leaves filling the 1.6 x 2.4 m doorway, each on a hinge at its own jamb, with a push bar on the approach side; they swing 90 degrees away from the player. `ObjectiveDoor.pair` turns the second leaf the other way. They were one leaf turning about its middle, which also left the doorway half blocked once open. The B-7 room door was already hinged.
- Tests: `AtmosphereLogicTests` now expect the lobby after the ward's stairs, and the golden run's beats without floor 2's three cues. The probe photographs the emergency door open (round 1).
- Docs: `GAME_DESIGN.md` section 14 (and a note in section 11; section 13's head is an ellipsoid, not a sphere); `INTEGRATION_ROUND2.md` E-26 (and E-25's test count, 11).
Evidence:
- Windows build (`unity_batch.py build-windows`, which recomposes first): PASS, 0 errors, 0 warnings; compose logged "stairwell: 1 walkable (Floor 3 to the ground floor; floor 2 sealed), 92 pieces, 20 risers". Built twice: the second time with the probe's door photo.
- Release probe, 3 rounds with screenshots: RESULT PASS, 3/3 complete, 0 errors or exceptions logged. No act ran on floor 2: each round walked from floor 3 down the one stairwell to floor 1 (8.6, 8.5 and 8.5 s). Rounds took 88.9, 77.2 and 76.7 s (they were 92-104 s with floor 2). Round 1: "emergency door open after 1.5 s; leaves turned 90 and 90 degrees on their hinges"; the revolver checks as in E-25 (body shot staggered 105, the shot drew 1 of 1, headshot killed it, the click).
- Shots viewed: the torch shining from the first moment in B-7, in the Act 3 blackout and in Act 4's red light; the emergency door open from the vestibule (a leaf swung back against the stair room's side, the doorway clear, the stair sign reading DOWN TO GROUND FLOOR / MAIN LOBBY); the stairwell top with the GROUND FLOOR title; the stairwell bottom opening on the lobby and the shut main entrance.
- Unity tests: EditMode 263/263 passed; PlayMode 28/28 passed. Engine-free logic tests 259/259 (the golden run and two floor tests changed for the skipped floor).
- `check_rules.py`: 1 error, 42 warnings, all `debug-log` (one new: the probe's door line); the error is the probe's scene search from before. `integration_scan.py`: only the existing `partial` duplicate-type error and the `HapticRequested` warning.
- Privacy scan of the change: no paths, names or keys.
- The 112 "Failed to create agent because there is no valid NavMesh" lines are the same count as before.
Not verified:
- A person playing: the feel of the doors, the torch from the start, the walk down one stairwell.
- Any headset: a headset still fades from the ward to the lobby (no stairwell), and has no torch.
- Phone and iPad builds.
Follow-ups:
- The owner plays this build.
- Visible hands on PC (asked by the owner): waits for their choice of a model; nothing built.
- The plan's chained lab door with banging on the way down (a hint at floor 2) is not built.
