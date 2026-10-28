# Work Plan: three seats, one game

Owner of this file: Claude (Technical Director). **Read this before every task.**

## Seats

| Seat | Agent (GitHub) | Branch | Owns | Log |
|---|---|---|---|---|
| TD | Claude (`Build-with-SK`) | `claude/technical-director` | Core contracts, layout, Integration, Packages, ProjectSettings, docs, tools, root files, CI, merges, builds | `docs/collab/CLAUDE_LOG.md` |
| A | Codex 1 (`lavanyahd`) | `codex/gameplay` | `Player/`, `Interaction/`, `Flow/`, `UI/` under `Game/Assets/PatientZero/` | `docs/collab/CODEX_A_LOG.md` |
| B | Codex 2 (`businessteen`) | `integration/xr-systems`, cut from `main` on 2026-10-28 after the owner approved the migration (`REVIEW_XR_SYSTEMS_72be6d8.md` section 5) | `AI/`, `Audio/`, `Atmosphere/`, `Environment/` under `Game/Assets/PatientZero/` | `docs/collab/CODEX_B_LOG.md` |

Branch from `origin/main`. The owner approved contract v1 on 2026-10-28, and `main` was created from it.
Full ownership table: `docs/ARCHITECTURE.md` section 3. Identities and commit emails: `AGENTS.md`.

## Repository

* Canonical: `https://github.com/Build-with-SK/patient-zero-vr.git`. **`main` is authoritative.** It was published 2026-10-28 at `b3bab8b` (contract v1).
* Frozen records (read them, never push to them): `codex/xr-systems` at `72be6d8`, Codex 2's first delivery, which is on a separate history; and `Lavanya` at `87bb494`, the same work with nothing added.
* Never force-push. Never delete, reset or rebase a pushed branch. Only Claude merges into `main`.
* `main` is the default branch. GitHub ruleset "Protect main" blocks force-pushes and deletion on it for everyone, with no bypass. Fast-forward merges still work.

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
python tools/review/check_rules.py --seat <A|B> --base origin/main --head <your branch>
```

**Review findings** come back in `CLAUDE_LOG.md` as `FIX-<seat>-<n>`, with a file and line, the defect, the exact fix, and a severity (BLOCKER/MAJOR/MINOR). Reply `FIX DONE` with the commit.

## Contract review (requested 2026-10-28, first task for both Codex agents)

Before writing new code, read everything in `Game/Assets/PatientZero/Core/Contracts/` and `docs/ARCHITECTURE.md` section 5. Then post one entry in your log:

```
## <date time> · Contract review v1
Kind: CONTRACT REVIEW v1
What: ACK, or one CONTRACT REQUEST per change (file, exact addition, why)
Evidence: python tools/logic_tests/run_logic_tests.py → <result line>
```

No agent redefines a shared contract on its own: no second `PatientZero.Core`, no private event bus, ids or payloads. Claude answers each request in `CLAUDE_LOG.md`, and the owner approves any change before it merges.

## Current state (2026-10-28)

* Contract v1 is **approved and on GitHub `main`** (`b3bab8b`): 43 logic tests pass, and the checker is clean.
* **Codex 2's first delivery** (`codex/xr-systems` at `72be6d8`) was reviewed and **not merged**. Its 26 portable tests reproduce. It is on an unrelated history and conflicts with contract v1 (FIX-B-1…12). The parts worth keeping and the migration plan are in `docs/collab/REVIEW_XR_SYSTEMS_72be6d8.md`. The owner approved the migration on 2026-10-28, and `integration/xr-systems` now exists.
* **Codex 1:** clone `main`, create `codex/gameplay`, do the contract review, then milestone A1 (`CODEX_A_BRIEF.md`).
* **Codex 2:** check out `integration/xr-systems`, do the contract review, then port milestone B1 (`CODEX_B_BRIEF.md`).
* **Integration tooling (2026-10-28, Claude; on `main` since `8b5e5cb`, PR #2):** the scene composer, Quest build, project validator and desktop simulation are in `Game/Assets/PatientZero/Integration/Editor/` (UNVERIFIED IN EDITOR). Editor and packages are pinned (6000.3.27f1; `Packages/manifest.json`), and offline CI runs on every push and PR. Seats: your `[SceneBuildStep]` methods are what the composer calls. It checks order bands, target scene, layout placement within 5 cm and one implementer per Core service. The rules are in `Integration/Logic/` and tested in the harness. How work reaches `main`, and the per-PR gate: `INTEGRATION_CHECKLIST.md`. Bring it into your branch with `git merge origin/main` (never rebase a pushed branch).
* **Production prompt beats** vs the approved design: `DESIGN_RECONCILIATION.md`. The approved GAME_DESIGN.md stands; amendments D-1…D-4 await the owner. Do not implement them before a decision.
* **Waiting on the owner:** install Unity 6000.3.27f1 with Android support, connect the headset, decide on the Meta XR Audio licence (`ARCHITECTURE.md` section 11), and invite `businessteen` as a collaborator if Codex 2 should push under its own account.
