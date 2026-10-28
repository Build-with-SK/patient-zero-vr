# Codex B log — AI, Audio, Atmosphere, Environment

Append-only; newest last. Identity: `businessteen` (`192131711+businessteen@users.noreply.github.com`).

## 2026-10-28 15:26 · Contract review v1 and recovery
Kind: CONTRACT REVIEW v1
What: ACK approved contract v1. No second Core, health system, Meta Interaction SDK, private cross-seat event bus, old encounter taxonomy or generated YAML will be migrated. Unity 6.3/XRI player ownership remains seat A. The approved ward layout (isolation, corridor, vestibule and ancillary rooms) takes precedence over the generic operating-theatre request; a theatre would require a layout/design change by Claude.
Evidence:
- Fetched all remote heads. `main`, `integration/xr-systems`, `claude/technical-director` = `73a3b67`; frozen `codex/xr-systems` = `72be6d8`, `Lavanya` = `87bb494`. `git merge-base origin/main origin/codex/xr-systems` returns no ancestor.
- Recovered original files to `/tmp/patientzero-recovery-72be6d8` from the preserved commit. Original portable suite: `26 passed; 0 failed; 0 skipped` (.NET SDK 8.0.427). Tracking-loss scenarios remain reference evidence for seat A, not a claim that current XRI tracking is implemented.
- `python tools/logic_tests/run_logic_tests.py` → `Logic tests: 43 passed, 0 failed.` No compiler warnings.
- Whole-commit cherry-picks are incompatible (FIX-B-1…12). Port reviewed algorithms at file level, crediting their source in each implementation commit. All frozen refs and files remain recoverable.
- GitHub API read returned HTTP 403; native Git read succeeds. An additive `api.github.com` network draft is being prepared for the explicitly requested PR; no credential values are inspected or requested.

## 2026-10-28 15:26 · Act 1 physical door gate
Kind: CONTRACT REQUEST
What: `Core/Contracts/Interfaces.cs`: add read-only `bool CanOpenAct1Door { get; }` to `IExperienceDirector`, or an equivalent approved director-to-seat-A gate event. Act 1 requires all planks removed, the window beat played and Z1 at least 4 m from the window before the door physically opens. Current Core exposes only CurrentAct/ExperienceTime/StartRun, so seat A cannot query that gate. B will gate its act advance internally but will not code against an unapproved interface addition.
Evidence: GAME_DESIGN §4 Act 1 gate versus current Interfaces.cs. No Core edits.

## 2026-10-28 15:26 · Cue fairness interpretation and ending ownership
Kind: PROGRESS
What: The cue-table rows list ZombieLunge and ZombieGlassTap at 8 m, while the mandatory tested fairness rule says every zombie vocal max is at least 9 m. B will conservatively use 9 m for these two cues and record this for Claude's review; ids and contracts are unchanged. The ending's white/black fade and title stay with seat A; B drives reveal amounts and state requests only through existing Core channels. Built-in Unity panning is the default until optional Meta XR Audio licensing is confirmed.
Evidence: GAME_DESIGN §§4,6; ARCHITECTURE ownership table. No runtime/editor/device claims.
