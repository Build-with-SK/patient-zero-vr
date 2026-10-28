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
