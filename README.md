# PATIENT ZERO

A 120-second hand-tracked VR horror experience for Meta Quest. You wake in a sealed isolation ward where the infected hunt by sound, and you have two minutes to get out. At the exit you learn why they were always so close: you are Patient Zero.

Built by three agents: Claude (Technical Director and integrator) and two Codex agents (A: player, interaction, flow, UI · B: AI, audio, atmosphere, environment).

## Status (2026-11-18)

**Playable on Windows; the newest work is on branch `claude/two-minute`** (`main` is older). The project compiles and builds in Unity 6000.3.27f1, and a release probe plays it through on Windows. The game is now the ward (floor 3) then the ground-floor lobby, with a revolver, the player's arms on flat screens, and zombies that hunt by sound and run to gunshots. What changed lately and what was tested: [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) sections 11-16 and the end of [`docs/collab/CLAUDE_LOG.md`](docs/collab/CLAUDE_LOG.md). **Nothing has run on a headset**; the Quest build is a development build. The rest of this README dates from 2026-10-28.

## Start here

| If you are | Read |
|---|---|
| The owner | this file, then `docs/ARCHITECTURE.md` §11 (what you need to do), then `docs/GAME_DESIGN.md` |
| Any coding agent | `AGENTS.md` (identities and git rules), then `docs/collab/WORK_PLAN.md` |
| Codex 1 (seat A) | `docs/collab/CODEX_A_BRIEF.md` |
| Codex 2 (seat B) | `docs/collab/CODEX_B_BRIEF.md`, then `docs/collab/REVIEW_XR_SYSTEMS_72be6d8.md` |
| Reviewing | `docs/REVIEW_CHECKLIST.md`, `docs/TEST_REPORT.md` |
| Integrating or building | `docs/collab/INTEGRATION_CHECKLIST.md` (pins, gates, first Unity run) |

## Documents

| File | What it is |
|---|---|
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Platform choices, ownership, contracts, runtime design, merge plan, blockers |
| [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) | The 120-second spec: beats, sound cues, lighting, zombies, failure, accessibility |
| [`docs/ASSET_MANIFEST.md`](docs/ASSET_MANIFEST.md) | Every asset with its source, licence, budget, status and fallback |
| [`docs/REVIEW_CHECKLIST.md`](docs/REVIEW_CHECKLIST.md) | How branches are reviewed |
| [`docs/TEST_REPORT.md`](docs/TEST_REPORT.md) | PASS / FAIL / BLOCKED / NOT TESTED for every feature, with evidence |
| [`docs/PERFORMANCE_REPORT.md`](docs/PERFORMANCE_REPORT.md) | Quest budgets and measurements |
| [`docs/RELEASE_CHECKLIST.md`](docs/RELEASE_CHECKLIST.md) | Release gates |
| [`docs/collab/WORK_PLAN.md`](docs/collab/WORK_PLAN.md) | Seats, branches, protocol |
| [`docs/collab/INTEGRATION_CHECKLIST.md`](docs/collab/INTEGRATION_CHECKLIST.md) | Pinned toolchain with sources, PR flow, per-PR gate, first Unity run, task board |
| [`docs/collab/DESIGN_RECONCILIATION.md`](docs/collab/DESIGN_RECONCILIATION.md) | Production-prompt beats vs the approved design; amendments awaiting the owner |

## Commands (no Unity needed)

```bash
python tools/logic_tests/run_logic_tests.py     # compile + run contract and logic tests (C# 9)
python tools/review/check_rules.py              # static review rules on the working tree
python tools/review/check_rules.py --self-test  # prove every rule fires
python tools/unity/validate_packages.py         # package pins, forbidden SDKs, editor pin (--online asks the registry)
python -m unittest discover -s tools/unity -p "test_*.py"   # tests for the Unity tooling
```

With Unity 6000.3.27f1 installed: `python tools/unity/unity_batch.py <validate|configure|compose|desktop-sim|editmode-tests|build-apk>`. Without it, the runner prints BLOCKED and exits 2.

The logic harness needs a Roslyn C# compiler: Visual Studio / Build Tools (found automatically), `PZ_CSC=<path to csc.exe>`, or a .NET SDK.

## Layout

```
Game/                     Unity project root (open this folder in Unity Hub)
  Assets/PatientZero/
    Core/                 shared contracts (Claude)
    Player/ Interaction/ Flow/ UI/          Codex A
    AI/ Audio/ Atmosphere/ Environment/     Codex B
    Integration/          build plan + checks (Logic/), scene composer, Quest build, desktop simulation (Editor/) (Claude)
  Packages/manifest.json  pinned packages · ProjectSettings/ProjectVersion.txt  pinned editor
docs/                     design, architecture, reports
tools/                    logic-test harness, review checker, Unity tooling (package policy, batchmode runner)
.github/workflows/        offline CI
```
