# Review: `codex/xr-systems` at `72be6d8` (Codex 2)

Reviewer: Claude (Technical Director) · 2026-10-28 · Mission 4 review, **not merged**.

**Verdict: well-engineered, but not mergeable into `main`.** The branch has no history in common with `main`. It also defines its own `PatientZero.Core` assembly, a separate SDK stack and a different game: blind zombies, a damage system, and five encounters that aren't in the approved timeline. It was written before contract v1 was published, so none of this is a judgement on quality. The logic is careful, allocation-free and well tested, and the parts listed in section 4 should be carried into contract v1 rather than lost. Section 5 proposes the migration path. Nothing is force-pushed or deleted.

## 1. Branch graph (GitHub, 2026-10-28)

```
main, claude/technical-director   b3bab8b ─ 2073c40          (contract v1, root 2073c40)
codex/xr-systems (default branch) 72be6d8 ─ 87bb494          (root 87bb494, no common ancestor with main)
Lavanya                           87bb494                    (same commit; no work of its own)
```

* `87bb494` (the commit Codex 2 reported) **is on `codex/xr-systems`**, as the parent of the tip `72be6d8` ("docs: record assigned contributor identities").
* `git merge-base main origin/codex/xr-systems` returns nothing: the histories are unrelated.
* GitHub attributes both commits to `businessteen`.

## 2. Evidence

| Check | Result |
|---|---|
| Portable tests, as `Tests/Portable/Portable.csproj` specifies (net8.0, C# 9, warnings as errors). Sources: `Runtime/Core/*.cs`, `Tests/Shared/CoreScenarios.cs`, `Program.cs`. Built with Roslyn csc against the .NET 8.0.8 runtime and run with `dotnet` (no .NET SDK on this machine) | **26 passed; 0 failed; 0 skipped**, 0 compiler warnings. **Codex 2's reported 26 passing tests reproduce.** |
| Mutation: removed the per-source cooldown rejection (`NoiseBus.cs`) | caught: 2 tests fail |
| Mutation: `LevelAt` hears up to twice the radius (`NoiseEvent.cs`) | caught: 1 test fails |
| `python -I tools/validate_assets.py` in a scratch copy | PASS: 67 unique GUIDs, every reference resolves, 8 prefabs, 6 mono WAVs, regeneration byte-identical |
| Unity import, compile, EditMode (29 required) and PlayMode (7 required) suites, SDK prefab generation, Android build, headset | **NOT RUN.** No Unity Editor or headset exists anywhere in this project yet. Codex 2's own `XR_SYSTEMS.md` states the same |

The portable suite covers only `Runtime/Core`. Nothing under `Runtime/Unity`, `Runtime/Meta`, `Editor`, the prefabs or the mixer has compiled or run.

## 3. Findings

Severity: **BLOCKER** stops a merge; **MAJOR** must be fixed before review passes; **MINOR** is a note. Paths are relative to `Packages/com.patientzero.xr/` unless they start at the repository root.

| Id | Sev | Where (at `72be6d8`) | Defect | Required fix |
|---|---|---|---|---|
| FIX-B-1 | BLOCKER | whole branch | No merge base with `main` | Migrate through a new branch cut from `main` (section 5). Never force-push either history |
| FIX-B-2 | BLOCKER | `Runtime/Core/PatientZero.Core.asmdef:2`; `Runtime/Core/NoiseEvent.cs:3,27` | A second assembly named `PatientZero.Core`. Unity refuses two assemblies with one name, so the project would not compile. Its types also sit in namespace `PatientZero.Core`, and `NoiseEvent` collides with the contract's `PatientZero.Core.NoiseEvent` | Seat code never defines Core. Put the logic in `PatientZero.AI.Logic` / `PatientZero.Audio.Logic` (asmdefs referencing only the contract `PatientZero.Core`), and use the contract types (`NoiseEvent`, `AlertLevel`, `CueId`) |
| FIX-B-3 | BLOCKER | `Tests/UnityProject/Packages/manifest.json:4`; `Runtime/Meta/PatientZero.Meta.asmdef:4`; `Tests/UnityProject/ProjectSettings/ProjectVersion.txt:1`; `package.json:6,10` | Built on a stack the approved platform excludes: the Meta Interaction SDK (`Oculus.Interaction`), Unity 6000.0.66f2 instead of 6.3 LTS, and XR Hands 1.7.2 instead of ≥ 1.8. `ARCHITECTURE.md` section 1: "Not used: the Meta XR Core SDK or Interaction SDK" | Drop `Runtime/Meta/*` and `Editor/SdkPrefabBuilder.cs`. If Codex 2 believes the Interaction SDK is needed, raise a `CONTRACT REQUEST` with the reason. The owner decides; until then contract v1 stands |
| FIX-B-4 | BLOCKER | `Runtime/Unity/ZombieStateMachine.cs:15,61`; `Runtime/Core/ZombieBrain.cs:13` | Failure is modelled as health damage (`IDamageReceiver.TryApplyDamage`, `Damage = 10`, a strike at 1.3 m after a 0.7 s windup). The approved design has **no health**: the only failure is being caught, defined as within 0.8 m for 0.25 s → `PlayerCaught` → act restore (`GAME_DESIGN.md` 2.2 and section 8) | Implement the catch rule in `AI/Logic/ZombieBrain` and raise `GameEvents.PlayerCaught`. Drop `IDamageReceiver` |
| FIX-B-5 | BLOCKER | `Runtime/Core/ZombieBrain.cs:5,61` | Different perception model and states. Zombies here are fully blind, use six states (`Idle…Recover`), chase only on footstep or voice noise, and skip the 0.6 s sniff telegraph and the snarl before a chase. The approved design uses movement-based sight (blind only in Blackout) and the `AlertLevel` states, and requires both telegraphs (`GAME_DESIGN.md` 2.2, lines 49–66; cue table `ZombieSniff`, `ZombieSnarl`) | Rebuild the state machine to `GAME_DESIGN.md` 2.2 as brief milestone B1 specifies. Keep this branch's robustness rules (section 4) |
| FIX-B-6 | MAJOR | `Runtime/Unity/SpatialAudioSystem.cs:43` | `AudioRolloffMode.Logarithmic` with Max Distance 25 m (60 m for the alarm). Unity's logarithmic curve **stops falling at Max Distance instead of reaching silence**, so cues stay audible across the whole ward and a player hears what zombies can't. `GAME_DESIGN.md` section 6: "a custom rolloff curve that is full volume to Min and falls to 0 at Max" | Use a custom curve generated from `CueTable` that reaches 0 at Max. The B3 PlayMode test samples the volume at Max = 0 |
| FIX-B-7 | MAJOR | `Runtime/Unity/NoiseManager.cs:41`; `Runtime/Unity/PlayerFootstepNoise.cs:32`; `SpatialAudioSystem.Play` | Noise and sound are separate: noise can be published with no audible cue, and `Play` emits no noise. The contract rule is that **audible means hearable**: `Cues.Play` → B's cue player → `GameEvents.Noise` (radius × intensity) | Publish noise only from the `IAudioCuePlayer` implementation. Footsteps become the `PlayerFootstep` cue, played by seat A |
| FIX-B-8 | MAJOR | `Runtime/Unity/EncounterSequence.cs:8,23,34` | Five encounter kinds that aren't in the 120-second timeline (for example `OperatingRoom`; the ward has no such room). A hard 2.5 m floor **cancels any beat closer than 2.5 m**: the approved final patient (`WardLayout.Z4_Gurney`) is **2.30 m** from the release-lever anchor (`A4_Release`), so the Act 4 scare would never fire. `InfectedReveal` is opt-out by default, but the reveal is the approved ending | Beats belong to `Atmosphere/Logic/DirectorCore` and follow `GAME_DESIGN.md` section 4. Comfort comes from `ScareIntensity.Reduced` and the `WardLayout` fairness tests. If Codex 2 wants a minimum distance, raise a `CONTRACT REQUEST` against `WardLayout` |
| FIX-B-9 | MAJOR | `Runtime/Core/AudioPolicy.cs:5`; `Runtime/Unity/SpatialAudioSystem.cs:64–74` | Six private cues instead of the 47 contract `CueId`s. When the pool is full, the new sound is dropped silently | `Audio/Logic/CueTable` (every `CueId`) and `VoiceBudget`: steal the lowest-priority, oldest voice; never steal UI or Voice; report what was stolen (brief B1) |
| FIX-B-10 | MAJOR | `Prefabs/*.prefab`, `Audio/*.asset`, `Audio/PatientZero.mixer`, 67 `.meta`, 6 `.wav` | Unity YAML and metadata written by a script, never imported by Unity (`WORK_PLAN.md` invariant 5). The generator and validator are careful, but a mixer or prefab is only proven by a Unity import. The `.wav` files are committed outside LFS (`.gitattributes:29` on `main` sends `*.wav` to LFS) | Replace them with `[SceneBuildStep]` builders, plus a `PlaceholderClipFactory` that synthesises the clips in code at load; the formulas in `tools/generate_assets.py` can carry over. No generated YAML, `.meta` or `.wav` in seat commits |
| FIX-B-11 | MAJOR | `Packages/`, `AGENTS.md`, `.gitignore`, `.github/workflows/xr-core.yml`, `tools/`; `Runtime/Core/TrackingRecovery.cs`, `Runtime/Unity/{TrackingLossGuard,OpenXRTrackingInput,HeldObjectSafety,TrackingSafetyHub}.cs` | Outside seat B. `Packages/`, root files, CI and `tools/` belong to Claude. Tracking, input switching and held-object safety are seat A (Player, Interaction) | Seat B ports only AI, Audio, Atmosphere and Environment work. The tracking pieces go to Codex 1 as reference (section 4). CI on `main` is Claude's job |
| FIX-B-12 | MINOR | `AGENTS.md:9` (repository root) | Lists Codex 1 as GitHub `lavanya` (`lavanya@users.noreply.github.com`). That login belongs to an **unrelated GitHub account**. The repo's collaborator is `lavanyahd` | Superseded by `AGENTS.md` on `main`. Don't copy the old table |
| NOTE-1 | MINOR | `Tests/Shared/CoreScenarios.cs` (the two allocation tests) | Zero-allocation is proven on .NET 8 via `GC.GetAllocatedBytesForCurrentThread`. Unity's runtime counts allocations differently | In Unity, also assert 0 B with a `ProfilerRecorder` on `GC.Alloc` in PlayMode (`PERFORMANCE_REPORT.md` budget: 0 B/frame) |

## 4. What to keep, and where it goes under contract v1

These are good and should not be lost. Each port commit names its source: `Port of <path>@72be6d8`.

| From `72be6d8` | Why it's worth keeping | Destination | Seat |
|---|---|---|---|
| `NoiseBus`: fixed capacity, per-source cooldown, stale and future rejection, no nested publish | Flood control that `EventChannel` doesn't do | Inside B's cue player, before it raises `GameEvents.Noise` (`Audio/Logic`) | B |
| `NoiseEvent.LevelAt` + threshold | Clear, tested hearing maths | `AI/Logic/Hearing` (adapted to `distance ≤ radius × multiplier × occlusion`) | B |
| `ZombieBrain` guards: monotonic clock, ground-distance arrival, path failure → search, memory expiry, distraction retarget | Robust edge-case handling with tests | `AI/Logic/ZombieBrain` (states per `GAME_DESIGN.md` 2.2) | B |
| `CoreScenarios` (26) | Most still apply once retargeted | NUnit tests in `AI/Tests/EditMode`, `Audio/Tests/EditMode` (they then run in our harness and in Unity) | B |
| `ZombieStateMachine`: 10 Hz staggered tick, `ProfilerMarker` | Keeps AI cost flat | `AI/Runtime` shell | B |
| `SpatialAudioSystem`: pooled voices, rate-limited occlusion low-pass | Good fit for the Quest budget | `Audio/Runtime` cue player (with the custom rolloff) | B |
| `generate_audio()` formulas | Original, licence-free placeholder sounds | `Audio/Runtime/PlaceholderClipFactory` | B |
| `TrackingRecovery` (0.25 s settle, cancel on switch or focus loss), `HeldObjectSafety` (release-velocity suppression) | Sound comfort and safety logic | `Player/Logic`, `Interaction/Logic` (reference for Codex 1; XRI instead of the Interaction SDK) | A |
| `QuestPerformanceRecorder` | Useful capture helper | Integration perf tooling (reference for Claude) | TD |

## 5. Migration proposal (awaits the owner)

**Recommended: a new integration branch from `main`.**

1. Freeze `codex/xr-systems` at `72be6d8`: nobody pushes to it, nothing is deleted, and it remains the record of Codex 2's first delivery. `Lavanya` (`87bb494`) is left as it is.
2. Claude creates `integration/xr-systems` from `main` and pushes it.
3. Codex 2 does seat-B milestone B1 on `integration/xr-systems`, porting the section 4 rows marked B into `Game/Assets/PatientZero/{AI,Audio,Atmosphere,Environment}/`, with tests that pass in `tools/logic_tests`.
4. Review follows `WORK_PLAN.md`. Claude merges `integration/xr-systems` into `main`, without force.
5. Codex 1 may port the rows marked A on `codex/gameplay`, crediting `72be6d8`.

The result is one history on `main`, Codex 2's original history kept untouched, and no force-push anywhere.

**Alternative (not recommended):** Codex 2 merges `main` into `codex/xr-systems` with `--allow-unrelated-histories`; this is a normal push, not a force-push. It keeps the branch name, but `main` would inherit a second root commit, and the branch would contain two `PatientZero.Core` assemblies until the port finished.

## 6. Contract review requested from Codex 2

Before writing new code, post a `CONTRACT REVIEW v1` entry in `docs/collab/CODEX_B_LOG.md` (format in `WORK_PLAN.md`). Either acknowledge contract v1, or raise a `CONTRACT REQUEST` for each part of your design you believe is better than it: the Interaction SDK, Unity 6.0, health damage, blind zombies, the 2.5 m floor, or per-source noise cooldowns in the contract. Requests are decided by Claude and approved by the owner. **Don't ship a parallel Core.**
