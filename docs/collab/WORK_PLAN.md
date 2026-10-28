# Work Plan: three seats, one game

Owner of this file: Claude (Technical Director). **Read this before every task.**

## Seats

| Seat | Agent | Branch | Owns | Log |
|---|---|---|---|---|
| TD | Claude | `claude/technical-director` | Core contracts, layout, Integration, Packages, ProjectSettings, docs, tools, merges, builds | `docs/collab/CLAUDE_LOG.md` |
| A | Codex A | `codex/a-player-interaction` | `Player/`, `Interaction/`, `Flow/`, `UI/` under `Game/Assets/PatientZero/` | `docs/collab/CODEX_A_LOG.md` |
| B | Codex B | `codex/b-ai-atmosphere` | `AI/`, `Audio/`, `Atmosphere/`, `Environment/` under `Game/Assets/PatientZero/` | `docs/collab/CODEX_B_LOG.md` |

Branch from `main` once the owner approves contract v1. Until then, branch from `claude/technical-director`.
Full ownership table: `docs/ARCHITECTURE.md` section 3.

## Invariants no seat may weaken

1. Edit only your own paths. Report defects elsewhere in your log, addressed to the owner.
2. Talk across seats only through `PatientZero.Core`: events, services, ids, layout, settings.
3. Raise only the `GameEvents` channels your seat writes (`ARCHITECTURE.md` 5.1).
4. Never renumber or remove an id. Never write a literal coordinate: use `WardLayout`.
5. No hand-written Unity YAML or `.meta` files. Scenes come from `[SceneBuildStep]` methods.
6. Rules worth testing live in `Logic/` and are tested in the logic harness.
7. Comfort and photosensitivity rules in `GAME_DESIGN.md` section 9 and section 7 are not tunable away.
8. Never claim something works in Unity or on a headset without the evidence `TEST_REPORT.md` requires.

## Protocol

**Log entries** (append-only, newest last, in your own log):

```
## 2026-10-29 14:05 · <short title>
Kind: PROGRESS | CONTRACT REQUEST | QUESTION | READY FOR REVIEW <commit> | FIX DONE FIX-A-3 <commit>
What: ...
Evidence: (command + result lines, or "none")
```

**Contract requests:** name the file (`Ids.cs`, `Interfaces.cs`, `WardLayout.cs`…), the exact addition, and why. Claude answers in `CLAUDE_LOG.md` with ACCEPTED (and the commit), CHANGED, or DECLINED. Don't code against a contract change until it is merged.

**Ready for review:** both of these clean, pasted into the entry:

```bash
python tools/logic_tests/run_logic_tests.py
python tools/review/check_rules.py --seat <A|B> --base <your base> --head <your branch>
```

**Review findings** come back in `CLAUDE_LOG.md` as `FIX-<seat>-<n>`, with a file and line, the defect, the exact fix, and a severity (BLOCKER/MAJOR/MINOR). Reply `FIX DONE` with the commit.

## Current state (2026-10-28)

* Contract v1 is on `claude/technical-director`: 43 logic tests pass, the checker is clean, the docs are written.
* **Waiting on the owner:** install Unity 6.3 LTS with Android support, connect the headset, approve contract v1 (`ARCHITECTURE.md` section 11).
* **Seat A and seat B can start now** on their `Logic/` milestones. These need no Unity. See your brief.
