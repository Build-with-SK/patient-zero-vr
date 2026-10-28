# Brief for Codex A: Player, Interaction, Flow, UI

Paste everything below the line into Codex A.

---

You are **Codex A** on PATIENT ZERO, a 120-second hand-tracked VR horror game for Meta Quest built in Unity 6.3 LTS with OpenXR and XR Interaction Toolkit 3. Claude is the Technical Director and integrator. Codex B builds AI, audio, atmosphere and environment in parallel.

**Repository:** `Documents/patient-zero` in the owner's Windows user folder. Work in your own worktree. From the repo root:

```bash
git worktree add ../patient-zero-worktrees/codex-a -b codex/a-player-interaction claude/technical-director
```

(Use `main` instead of `claude/technical-director` once `main` exists.)

**Read first, in this order:** `docs/collab/WORK_PLAN.md`, `docs/ARCHITECTURE.md`, `docs/GAME_DESIGN.md` (sections 2, 4, 8, 9), `docs/REVIEW_CHECKLIST.md`, and everything in `Game/Assets/PatientZero/Core/Contracts/`.

**You own only:** `Game/Assets/PatientZero/{Player,Interaction,Flow,UI}/**` and `docs/collab/CODEX_A_LOG.md`. Touch nothing else. If you need something in Core, write a `CONTRACT REQUEST` in your log.

**Important constraint:** Unity is not installed on this machine yet. Until it is:
* Write only `.cs`, `.asmdef`, `.md` and `.json` files. **No `.unity`, `.prefab`, `.asset`, `.mat` or `.meta` files.**
* Put every rule in `Logic/` (engine-free, `PatientZero.<Area>.Logic` asmdef referencing only `PatientZero.Core`) and test it in `Tests/EditMode/`. Run `python tools/logic_tests/run_logic_tests.py`. It compiles your Logic and tests against a UnityEngine stand-in. Logic may not use `Time`, `UnityEngine.Random`, `Physics` or `GameObject`; pass them in.
* MonoBehaviour shells and Editor build steps can't be compiled yet. Write them carefully against XRI 3.x APIs, keep them thin, and mark them `// UNVERIFIED IN EDITOR` at the top until integration compiles them.

## Milestone A1: Logic (do this first; fully testable now)

| Class (in `Logic/`) | Must do | Tests must cover |
|---|---|---|
| `Flow/Logic/GameFlowCore` | State machine over `GameStateRules`. Inputs: state requests, `PlayerCaught`, pause input, focus lost/gained, "ward loaded", "fade done". Outputs: a command list (set time scale, pause audio, fade to, load/unload Ward, `StartRun(act, isRestore)`, raise `StateChanged`). Tracks the current act for restores | every legal and illegal transition; pause only from Playing/Ending; resume returns to the paused-from state; Caught → reload → `StartRun(currentAct, true)` → Playing; a second `PlayerCaught` during Caught is ignored; restart from Complete and from Paused; focus loss pauses but focus regain does **not** resume |
| `Interaction/Logic/ValveMath` | accumulates signed rotation from successive grab-point vectors around the valve axis; re-grip continues without a jump (ratchet); either direction counts (accessibility); required 540°, assist 270°; turn speed → squeal intensity 0..1 (0 below 30°/s, 1 at ≥ 180°/s) | full turns, re-grip, wrong axis input, assist switch mid-turn, wrap-around at ±180°, intensity curve |
| `Interaction/Logic/PlankLogic` | pull distance along the plank normal → creak intensity; pops at 8 cm (assist 4 cm); springs back if released before popping | pop threshold, release before pop, assist |
| `Interaction/Logic/SocketLogic` | snap when ≤ 6 cm and ≤ 35° (assist 12 cm / 60°) | inside/outside thresholds, assist |
| `Interaction/Logic/LeverLogic` | hold progress to 2 s (assist 1 s); decays at 1 per second when released; emits a progress update only on ≥ 5 % change | completion, decay, partial re-hold, event throttling |
| `Interaction/Logic/ObjectiveTracker` | emits each `ObjectiveCompleted` once per attempt; `Act2_PowerRestored` when 200 and 201 are both complete, in either order; resets on `ActStarted` | duplicates, order independence, reset |
| `Player/Logic/SpeedEstimator` | smoothed head and hand speeds for `IPlayerQuery.HeadSpeed` / `FastestHandSpeed` (exponential smoothing, ignores teleport jumps > 1 m in one frame) | smoothing, teleport rejection, untracked hand = 0 |
| `Flow/Logic/SettingsCodec` | `ExperienceSettings` ↔ string for PlayerPrefs; unknown keys ignored; always `Sanitized()` | round trip, corrupt input, missing keys → defaults |
| `UI/Logic/CaptionQueue` | at most 2 visible lines; the same text within 1 s is merged; direction arrow (ahead/left/right/behind) from head position, forward and the sound position | limits, de-duplication, all four directions |

## Milestone A2: Runtime shells and build steps (write now, verified at integration)

* **Player** (Bootstrap): XR Origin from the XRI Starter Assets pattern; XR Input Modality Manager for hands ↔ controllers; teleport to `TeleportationAnchor`s only; snap turn 45° (settings); comfort vignette; seated height offset; `IPlayerQuery` and `IScreenFader` implementations (fader uses unscaled time); haptics listening to `HapticRequested` (controllers only); hand-loss handling (`ARCHITECTURE.md` section 6); hand visuals reading `InfectionReveal`.
* **Interaction** (Ward): planks, valve, cable plug + socket, lever, door latch, tray props, bottle-cart physics. Each placed at its `WardLayout` id with a `LayoutTag`; sounds only through `Cues.Play(..., NoiseOrigin.Player, intensity)`; objectives through `ObjectiveTracker`; obey `AssistChanged`; required items return home after 5 s if dropped out of reach.
* **Flow** (Bootstrap): `GameFlow` shell around `GameFlowCore`; additive Ward load/unload; focus and pause handling; the settings provider and persistence.
* **UI**: calibration (incl. the brightness screen), pause menu, captions (`CaptionRequested` + `CaptionQueue`), hint text from `AssistChanged`, title, Complete screen, credits.
* **Build steps:** `[SceneBuildStep(BuildScene.Ward, 300..399)]` for interactables and teleport anchors; `[SceneBuildStep(BuildScene.Bootstrap, 400..499)]` for rig, flow and UI. Greybox props from primitives; no imported assets.

## Milestone A3: PlayMode tests (written now, run at integration)

`Flow/Tests/PlayMode`: an autoplay bot that completes every objective through component APIs and reaches Complete; restart ×10 with `SubscriberCount` back to baseline on every `GameEvents` channel; pause during Ward load.

## Rules you will be reviewed against

`docs/REVIEW_CHECKLIST.md` and `docs/ARCHITECTURE.md` section 8. Particularly: no `AudioSource`, `Light`, `FindObjectOfType` or LINQ; no allocation in `Update`; no raising B's channels (you write `StateChanged`, `ObjectiveProgress`, `ObjectiveCompleted`, `SettingsChanged`, `TrackingChanged`); no reference to any B assembly.

## Definition of ready

Post `READY FOR REVIEW <commit>` in `docs/collab/CODEX_A_LOG.md`, with clean output from both commands in `WORK_PLAN.md`. Post A1 as its own review before starting A2, so contract problems surface early.
