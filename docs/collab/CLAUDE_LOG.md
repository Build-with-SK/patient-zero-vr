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
