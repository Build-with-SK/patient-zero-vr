# Brief for Codex B: AI, Audio, Atmosphere, Environment

Paste everything below the line into Codex B.

---

You are **Codex B** on PATIENT ZERO, a 120-second hand-tracked VR horror game for Meta Quest built in Unity 6.3 LTS with OpenXR and XR Interaction Toolkit 3. Claude is the Technical Director and integrator. Codex A builds the player rig, interactions, game flow and UI in parallel.

**Repository:** `Documents/patient-zero` in the owner's Windows user folder. Work in your own worktree. From the repo root:

```bash
git worktree add ../patient-zero-worktrees/codex-b -b codex/b-ai-atmosphere claude/technical-director
```

(Use `main` instead of `claude/technical-director` once `main` exists.)

**Read first, in this order:** `docs/collab/WORK_PLAN.md`, `docs/ARCHITECTURE.md`, `docs/GAME_DESIGN.md` (all of it: you implement the timeline), `docs/ASSET_MANIFEST.md`, `docs/REVIEW_CHECKLIST.md`, and everything in `Game/Assets/PatientZero/Core/Contracts/`.

**You own only:** `Game/Assets/PatientZero/{AI,Audio,Atmosphere,Environment}/**` and `docs/collab/CODEX_B_LOG.md`. Touch nothing else. If you need something in Core, write a `CONTRACT REQUEST` in your log.

**Important constraint:** Unity is not installed on this machine yet. Until it is:
* Write only `.cs`, `.asmdef`, `.md` and `.json` files. **No `.unity`, `.prefab`, `.asset`, `.mat`, `.mixer` or `.meta` files.**
* Put every rule in `Logic/` (engine-free, `PatientZero.<Area>.Logic` asmdef referencing only `PatientZero.Core`) and test it in `Tests/EditMode/`. Run `python tools/logic_tests/run_logic_tests.py`. Logic may not use `Time`, `UnityEngine.Random`, `Physics` or `GameObject`; pass time and a seeded `System.Random` in.
* MonoBehaviour shells and Editor build steps can't be compiled yet. Write them carefully, keep them thin, and mark them `// UNVERIFIED IN EDITOR` until integration compiles them.
* **No external assets.** Every sound starts as a code-generated placeholder; every mesh is a primitive.

## Milestone B1: Logic (do this first; fully testable now)

| Class (in `Logic/`) | Must do | Tests must cover |
|---|---|---|
| `Audio/Logic/CueTable` | one entry per `CueId` with every column of `GAME_DESIGN.md` section 6 (space, min/max, loop, noise radius, mixer group, caption text and policy, priority) | **every `CueId` has exactly one entry**; min < max; world cues spatial, Doppler 0, rolloff that reaches 0 at max; noise radius ≤ max; **every zombie vocal max ≥ 9 m**; loops flagged as specified; every ● cue and every Speech cue has caption text |
| `Audio/Logic/VoiceBudget` | 24 voices, ≤ 12 spatialized; steals the lowest-priority, oldest voice; never steals UI or Voice | stealing order, caps, nothing lost silently (stolen cue reported) |
| `AI/Logic/Hearing` | `heard = distance ≤ radius × multiplier × occlusion`; ignores `Zombie`/`Ambient` origins | boundary cases, occlusion, origin filter |
| `AI/Logic/ZombieBrain` | the state machine and parameters in `GAME_DESIGN.md` 2.2: telegraph, investigate, search, retarget hysteresis, chase, lose, catch, frozen when not live, Restrained/chain, Passive, Reduced mode, movement-based sight (0 in Blackout) | **every transition**; no dead state; retarget jitter suppressed; chase always preceded by a snarl command; catch needs 0.8 m for 0.25 s; Z4 never moves beyond 1.3 m of the gurney; a frozen player is never seen |
| `Atmosphere/Logic/DirectorCore` | act state machine and every beat in `GAME_DESIGN.md` section 4: gates, triggers, hint ladder (+10/+20/+30 s of no progress), scare budget, tension, silence windows, adaptive assist after a second catch, the ending sequence; restores to act-start state. Emits commands (play cue, set lighting, zombie command, raise `ActStarted`/`AssistChanged`/`InfectionReveal`, request state) | **golden run:** a scripted event trace produces the expected beat order and times; exactly 2 hard scares in Standard, 0 in Reduced; no ambient one-shot within 6 s of a hard scare; hints reset on progress; the Act 2 investigation fires even if the player is silent; Act 1 door gate waits for the Z1 beat |
| `Atmosphere/Logic/LightingPresets` | values for each `LightingState` (section 7); the flicker and pulse generator | **never more than 3 luminance changes in any 1 s window** (sample 10 minutes at 1 ms); Reduce flicker gives constant output; Surge is monotonic (a ramp, not a flash) |
| `Environment/Logic/RoomGeometry` | walls, door openings and floor rectangles derived from `WardLayout.Rooms` | rooms close; doors line up with the layout's door positions; every anchor is on a floor |

## Milestone B2: Runtime shells and build steps (write now, verified at integration)

* **Audio** (Bootstrap): the `IAudioCuePlayer` implementation, with pooled voices, custom rolloff curves from `CueTable`, mixer routing, raising `GameEvents.Noise` (radius × intensity) and `GameEvents.CaptionRequested`, UI cues ignoring listener pause, and the Meta XR Audio spatializer with a fallback to built-in panning. `PlaceholderClipFactory` generates every clip in code at load.
* **AI** (Ward): a zombie shell driving a NavMeshAgent from `ZombieBrain` and reading `IPlayerQuery`. Z1–Z4 as `GAME_DESIGN.md` describes; the generated mannequin with procedural shamble (`ASSET_MANIFEST.md` C-03).
* **Atmosphere** (Ward): the `IExperienceDirector` implementation wrapping `DirectorCore`; the lighting controller (the only code touching `Light`/`RenderSettings`); emissives through `MaterialPropertyBlock`.
* **Environment** (Ward): greybox geometry, colliders and the NavMesh bake from `RoomGeometry`; dressing props (cart, gurney and chain, speakers, vents).
* **Build steps:** `[SceneBuildStep(BuildScene.Ward, 100..299)]` for environment and atmosphere, `500..599` for AI; `[SceneBuildStep(BuildScene.Bootstrap, 600..699)]` for the cue player and `Ward 600..699` for static emitters. Place everything by `WardLayout` id with a `LayoutTag`.

## Milestone B3: PlayMode tests (written now, run at integration)

A noise inside or outside a zombie's radius produces or skips an investigation in a real scene; a NavMesh path exists between every zombie marker and its targets; sampled AudioSource volume at Max distance is 0 for every world cue.

## Rules you will be reviewed against

`docs/REVIEW_CHECKLIST.md` and `docs/ARCHITECTURE.md` section 8. Particularly: no XRI or XR Hands types anywhere in your code (use `IPlayerQuery`); no reference to any A assembly; no allocation in `Update`; you write `ActStarted`, `AssistChanged`, `Noise`, `ZombieAlert`, `PlayerCaught`, `LightingChanged`, `InfectionReveal`, `CaptionRequested`, `HapticRequested` and nothing else.

## Definition of ready

Post `READY FOR REVIEW <commit>` in `docs/collab/CODEX_B_LOG.md`, with clean output from both commands in `WORK_PLAN.md`. Post B1 as its own review before starting B2.
