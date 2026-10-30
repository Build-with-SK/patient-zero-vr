# Review: `integration/xr-systems` at `6bc4c42` (Codex 2, seat B)

2026-10-28 · Reviewer: Claude (Technical Director) · Base: `main` `8b5e5cb` · Earlier review of this seat: `REVIEW_XR_SYSTEMS_72be6d8.md`

## Verdict

**Not ready to merge yet: 0 BLOCKER, 3 MAJOR, 3 MINOR.** This is a clean port onto contract v1. Every file is inside
seat B's paths (plus `CODEX_B_LOG.md`), it builds on `main`, it fixes all 12 findings from the first review, and it merges
with `main` and with Codex 1's branch without a conflict. The director follows the approved 120-second timeline beat by
beat, the zombie state machine matches `GAME_DESIGN.md` 2.2, and the cue table matches section 6 row by row. The three
MAJOR findings are one proven logic bug (assist), one design deviation (hearing through walls), and one editor risk to
close before the first Unity run (NavMesh persistence). Nothing here has run in Unity or on a headset, and the branch
says so.

## What was checked

| Check | Result |
|---|---|
| Branch | 5 commits by `businessteen` on `main` `73a3b67`; 51 files, +3 103 lines, all under `AI/`, `Atmosphere/`, `Audio/`, `Environment/` and `CODEX_B_LOG.md` |
| Frozen refs | `codex/xr-systems` `72be6d8` and `Lavanya` `87bb494` unchanged |
| `run_logic_tests.py` on the branch | `Logic tests: 100 passed, 0 failed.` (reproduces the log) |
| `check_rules.py --seat B --base 73a3b67 --head 6bc4c42`; whole tree | `0 error(s), 0 warning(s).` both |
| Trial merge onto `main` `8b5e5cb` (scratch, not pushed) | no conflicts. `Logic tests: 127 passed, 0 failed.`; checker, `validate_packages.py` and tooling tests clean |
| Trial merge of `main` + this branch + `codex/gameplay` `83b57d3` | no conflicts. `Logic tests: 164 passed, 0 failed.`; checker and package policy clean |
| All 6 seat B `[SceneBuildStep]`s through the real `BuildPlan` (temporary probe, not committed) | **0 errors.** Ward 100, 150, 200, 500, 650; Bootstrap 600. With seat A added, the only errors are FIX-A-1's two |
| Core services | one implementer each: `ExperienceDirector` (Ward), `AudioCuePlayer` (Bootstrap); seat A has the other three |
| Event writers | every `Raise` matches the writer named in `GameEvents.cs` |
| Assembly references | seat B and Core only, plus `Unity.AI.Navigation`; no Meta, Oculus or XRI reference |
| Old findings (health damage, Meta SDK, logarithmic rolloff, encounter list, handwritten YAML/`.meta`/`.wav`) | 0 matches in the diff; 0 Unity asset files committed |
| Director vs `GAME_DESIGN.md` sections 4–5 | matches: PA +3 s, Z1 pass (first plank or +16 s), slam when the third plank is off and Z1 is at the glass, lure +2 s, door gate (planks off and Z1 ≥ 4 m), Act 2 investigation (+12 s lure), surge (130 % over 0.6 s, blackout, 2.5 s stun), Act 3 chirp every 3 s and crossing (Mid1 or +10 s), Act 4 snap (60 % or +15 s; Reduced −9 dB, no lunge), ending at 1/2/3–9/6/12 s, hint ladders at 10/20/30 s (Act 3 at 12/22/32 s), scare budget kept across restores, tension and one-shot rules |
| Zombie brain vs section 2.2 | matches: movement-based sight in a 60° cone (6 m WardDim, 3 m EmergencyRed, 0 in Blackout, none for Z3 and in Reduced), 0.6/1.2 s sniff telegraph, retarget rule, 5 s search, snarl before every chase, 3 s chase loss, catch 0.8 m/0.25 s (Reduced 0.6 m/0.5 s), Z4 chain reach, speeds |
| Cue table vs section 6 | every row matches on range, loop, noise radius, bus and caption, except ZombieLunge and ZombieGlassTap Max 9 m instead of 8 m: see note N-1 |
| Lure reaches Z2 | Z2 at `Z2.Dormant` is 8.6 m from the lure; the 20 m radius stays above that even at the 0.5 occlusion factor |
| Unity compile, scene generation, the 6 PlayMode tests, device | BLOCKED: no Unity 6000.3.27f1 on any seat |

## Findings from the first review (`72be6d8`)

FIX-B-1 … FIX-B-12 are all **resolved** on this branch: the history is cut from `main` (1), there is no second Core (2),
no Meta SDK and no package changes (3), failure is the catch rule with `PlayerCaught` (4), the state machine follows 2.2
with both telegraphs (5), world cues use a custom curve that reaches 0 at Max (6), noise is raised only by the cue player
(7), beats live in `DirectorCore` and follow section 4 (8), `CueTable` covers every `CueId` and `VoiceBudget` steals by
priority and age (9), assets come from builders and `PlaceholderClipFactory` (10), only seat B paths are touched (11), and
`AGENTS.md` is not copied (12).

## New findings

### FIX-B-13 · MAJOR · `Atmosphere/Logic/DirectorCore.cs:89–90` — assist switches off at the first touch

`Progress` resets the objective's hint to 0 and emits `AssistLevel.None`. That is right for the hint ladder, but it also
cancels assist when assist is supposed to stay on: with **Puzzle assist always on** (`GAME_DESIGN.md` section 9: "Assist
values from the start") and with **adaptive assist** after a second catch in the act (section 5). A temporary probe on
this branch (not committed) printed:

```
PROBE alwaysOn: start=Assist afterProgress=None | adaptive: start=Assist afterProgress=None
```

The existing test `SecondCatchEnablesAdaptiveAssistOnRestore` checks that assist turns on, not that it stays on.
**Fix:** keep a per-act assist floor (`Assist` when `AdaptiveAssist || Settings.PuzzleAssistAlwaysOn`, else `None`);
progress resets the ladder to the floor, never below it. Turning the setting on from the pause menu mid-act applies the
floor at once. Add tests for both cases, and for the setting changing mid-act.

### FIX-B-14 · MAJOR · `AI/Runtime/ZombieAgentShell.cs:66,81` — one mask serves sight and hearing

The shell uses the same `Occluders` mask (default: layer 0, which holds every wall) for the sight line and for hearing
occlusion. Walls must block sight. Hearing is different: `GAME_DESIGN.md` 2.1 says occlusion is "0.5 through a closed
door and 1.0 otherwise", so sound carries past walls at full radius. As written, every wall halves a zombie's hearing.
The line also ends 0.1 m above the noise, so it can hit the collider of the object that made the noise (a plank, a
bottle) and count it as a door.
**Fix:** two masks: `SightBlockers` (walls and closed doors) and `HearingOccluders` (closed door leaves only). Until the
door layer exists (INT-B-2), `HearingOccluders` defaults to nothing, which is exactly the design for a ward with no
closed doors yet. Add a logic test that a wall between zombie and noise does not reduce hearing.

### FIX-B-15 · MAJOR (verify at the first Unity run) · `Environment/Editor/HospitalBuildSteps.cs:71` — NavMesh not saved as an asset

`BakeNavigation` calls `NavMeshSurface.BuildNavMesh()`, which leaves the `NavMeshData` in memory. Unity's own editor
bake (the Bake button, `NavMeshAssetManager`) saves it with `AssetDatabase.CreateAsset` beside the scene. Whether a
script-built `NavMeshData` survives the composer's scene save has not been confirmed, and if it doesn't, the Ward loads
with no NavMesh and every zombie reports `PathFailed`. **Fix:** after the bake, save it as
`Assets/PatientZero/Environment/Generated/WardNavMesh.asset` (replace the old one if present), as the editor does.
`NavigationSceneTests` then proves it at the first run.

### FIX-B-16 · MINOR · `Audio/Runtime/AudioCuePlayer.cs:16` — playback occlusion never applies

`public LayerMask Occluders;` defaults to 0 (Nothing) and `AudioBuildSteps` never sets it, so the 0.5 gain and
1.6 kHz low-pass behind walls never happen. `INTEGRATION.md` says all occlusion masks default to layer 0.
**Fix:** default to the same blockers as sight (walls and closed doors), set in `AudioBuildSteps.Build`.

### FIX-B-17 · MINOR · `Atmosphere/Runtime/LightingController.cs:68` — the exit sign loses its green

The controller writes `_EmissionColor = colour × gain` to the exit sign, where `colour` is the ambient tint. In
`Blackout` that is 0.2 × (0.65, 0.8, 1): a dim blue-grey. Act 3 is navigated by that sign (section 4: "exit sign is the
only light"; section 7, Blackout: "exit-sign emissive at `A3.Beacon` only"). **Fix:** keep the sign's own green and drive only its brightness, with a fixed visible level in
`Blackout`.

### FIX-B-18 · MINOR · `Environment/Editor/HospitalBuildSteps.cs:28` — walls overhang the door openings

Each wall box is 0.1 m longer than its span, so it overhangs 5 cm into every door opening: the 1.2 m and 1.6 m doors
are built 1.1 m and 1.5 m wide, and seat A's door leaves would overlap the wall. **Fix:** extend walls only where they
meet another wall (corners), not at door cuts. Extend `RoomGeometryTests` to check the built opening width.

## Notes (no change required)

| # | Note |
|---|---|
| N-1 | ZombieLunge and ZombieGlassTap use Max 9 m. `GAME_DESIGN.md`'s cue table says 8 m, but its fairness rule says every zombie vocal is ≥ 9 m. The branch follows the tested rule. **Accepted**; the design table needs correcting, which is the owner's call because the design is approved |
| N-2 | The Act 2 lure plays at `Z1.WalkFrom` + 1 m up, (−1.5, 1.0, 3.6); the design says (−1.5, 1.0, 3.3). 0.3 m, no effect on the beat |
| N-3 | Both B-7 tubes flicker (phase-locked, so still ≤ 2 changes per second); the design says one tube flickers. `WardDim` lights all six fixtures at 40 %; the design says the three corridor fixtures. Greybox-level differences, worth aligning at the lighting pass |
| N-4 | `ActStarted` warps every zombie to the nearest NavMesh point, which may slide Z4 off the gurney visually. Its catch reach is computed from the gurney marker, so the rule is unaffected |
| N-5 | `AudioCuePlayer` releases every non-UI voice on `ActStarted`, so a PA line still playing at an act change is cut. No current beat overlaps |

## Integration actions for Claude

| # | Action |
|---|---|
| INT-B-1 | **NavMesh agent radius.** The bake uses agent type 0 (Humanoid), whose default radius is 0.5 m. A 1.1 m opening (FIX-B-18) leaves about 0.1 m of walkable width, so Z2 may not path through its doorway. `NavMeshAgent.radius = 0.22` doesn't change the bake. Claude sets the agent type's radius to 0.25 m in `ProjectSettings` (Claude's area) at the first Unity run; `NavigationSceneTests` covers Z2's doorway route |
| INT-B-2 | **Physics layers.** Sight, hearing and audio occlusion need shared layer names: ward walls, closed door leaves (seat A's doors), player, held props. Proposed as an addition to contract v1.1 (a `PhysicsLayers` constant class in Core), alongside `GateId`/`GateReleased`. Owner approval needed; the `TagManager` entries are made at the first Unity run |
| INT-B-3 | Seat B's 6 PlayMode tests and the composer's checks run at the first Unity run (`INTEGRATION_CHECKLIST.md` section 6). The mixer factory uses an internal editor API through reflection and fails loudly if it is missing; check it there |

## Contract

Codex 2's ACK of the proposed v1.1 (`GateId`, `GateReleased`, `IExperienceDirector.IsGateReleased`) is recorded. Both seats
have now agreed it; it waits only for the owner. Seat B keeps `CanOpenAct1Door` internal until it lands, as it does now.

**Round 2 (2026-10-28):** integrated on `integration/round2`; exceptions and new findings are in `INTEGRATION_ROUND2.md`.

**Update 2026-10-28:** the owner approved contract v1.1 and Claude added it to Core. Build against it once it is on `main`; `WORK_PLAN.md` (current state) says what each seat does.

## Next for Codex 2

1. Merge `main` (`8b5e5cb`) into `integration/xr-systems`. Do not rebase.
2. Fix FIX-B-13 … 18 and reply `FIX DONE FIX-B-<n> <commit>` in `CODEX_B_LOG.md`, with the harness and seat B checker output.
3. Draft PR https://github.com/Build-with-SK/patient-zero-vr/pull/3 is open for this branch (opened by Claude, since `gh pr create` returned `Forbidden` from your environment). Push to the same branch and the PR updates.
