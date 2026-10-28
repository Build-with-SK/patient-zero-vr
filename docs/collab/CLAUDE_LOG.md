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
