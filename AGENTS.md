# PATIENT ZERO: rules for every coding agent

Canonical repository: `https://github.com/Build-with-SK/patient-zero-vr.git`. **`main` is the single source of truth.** It holds approved contract v1.

**Before any task, read `docs/collab/WORK_PLAN.md`. It is the authority on seats, branches and process.** Then read your brief.

## Who is who

| Agent | GitHub login | Seat | Your branch | Brief | Your log |
|---|---|---|---|---|---|
| Claude | `Build-with-SK` | Technical Director | `claude/technical-director` | — | `docs/collab/CLAUDE_LOG.md` |
| Codex 1 | `lavanyahd` | A: Player, Interaction, Flow, UI | `codex/gameplay` | `docs/collab/CODEX_A_BRIEF.md` | `docs/collab/CODEX_A_LOG.md` |
| Codex 2 | `businessteen` | B: AI, Audio, Atmosphere, Environment | `integration/xr-systems` (see `WORK_PLAN.md`: created when the owner approves the migration) | `docs/collab/CODEX_B_BRIEF.md` | `docs/collab/CODEX_B_LOG.md` |

Commit with your own identity, set per command. Never change global git config.

```bash
git -c user.name=<login> -c user.email=<id>+<login>@users.noreply.github.com commit -m "..."
```

| Login | Commit email |
|---|---|
| `Build-with-SK` | `261377178+Build-with-SK@users.noreply.github.com` |
| `lavanyahd` | `116944917+lavanyahd@users.noreply.github.com` |
| `businessteen` | `192131711+businessteen@users.noreply.github.com` |

The GitHub account `lavanya` is a different person. Don't use it.

## Git rules

* **Never force-push. Never delete, reset or rebase a branch that is on GitHub.**
* Push only to your own branch. Only Claude merges into `main`.
* `codex/xr-systems` (`72be6d8`) and `Lavanya` are frozen records: read them, never push to them.
* Your branch starts from `origin/main`. Review diffs are taken against `origin/main`.

## Project rules (full list in `WORK_PLAN.md`)

* `Game/Assets/PatientZero/Core/Contracts/` is **approved contract v1**. Change it only through a `CONTRACT REQUEST` in your log. Claude decides and the owner approves. Never define a second `PatientZero.Core`.
* Edit only your seat's folders. Seats talk only through `PatientZero.Core`.
* No hand-written Unity YAML, `.meta`, `.prefab`, `.asset`, `.mixer`, `.unity` or binary assets. Scenes and prefabs come from `[SceneBuildStep]` code.
* Never claim that something works in Unity or on a headset without the evidence `docs/TEST_REPORT.md` requires.

## Before you say "ready"

```bash
python tools/logic_tests/run_logic_tests.py
python tools/review/check_rules.py --seat <A|B> --base origin/main --head <your branch>
```

Both must be clean. Paste the output into your log entry.
