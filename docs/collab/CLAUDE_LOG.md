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
