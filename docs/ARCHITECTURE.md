# PATIENT ZERO — Architecture and Integration Contract

Version 1 · 2026-10-28 · Owner: Claude (Technical Director) · Binding on both Codex seats.
Changes go through Claude: write a `CONTRACT REQUEST` entry in your log (see `docs/collab/WORK_PLAN.md`).

---

## 0. Where things actually stand

When this contract was written there was **no Unity project, no Unity Editor, no Android SDK, no `adb`, and no headset connected** on the development machine, and no Codex branches existed. This repository is the project's starting point.

What has been verified, and how:

| Item | Status | Evidence |
|---|---|---|
| Core contracts compile as C# 9 (Unity 6's language version) | PASS | `python tools/logic_tests/run_logic_tests.py`: 43 tests, 0 failures, 0 compiler warnings, using Roslyn `csc` against a UnityEngine stand-in (not Unity) |
| Ward layout puts every puzzle within reach and keeps the final zombie's reach clear of the player's path | PASS (static) | `WardLayoutTests`, same run |
| Review checker detects every rule it claims to | PASS | `python tools/review/check_rules.py --self-test` |
| Anything compiles inside Unity | NOT TESTED | No Unity Editor installed |
| Anything runs on a Quest | NOT TESTED | No headset or `adb` |

The owner actions that unblock the rest are in section 11.

---

## 1. Platform decisions

| Decision | Choice | Why | Revisit if |
|---|---|---|---|
| Engine | **Unity 6.3 LTS, pinned to 6000.3.27f1** (revision `672d562a42a7`) in `ProjectVersion.txt` and `tools/unity/package_policy.json` (checked 2026-10-28) | The current LTS for Quest 2/3/3S/Pro. XR hand simulation in the Editor recommends 6.3 or newer | A newer LTS ships before integration starts |
| XR runtime | **Unity OpenXR plugin** 1.19.0 + **OpenXR: Meta** (`com.unity.xr.meta-openxr`) 2.7.0 + XR Management 4.7.0 | Meta's recommended path. The Oculus XR plugin is deprecated and marked for removal from Unity 6.5 | — |
| Interaction | **XR Interaction Toolkit 3.x (≥ 3.5; pinned 3.7.0)** + **XR Hands (≥ 1.8; pinned 1.10.0)** + Input System 1.20.1 | One stack covers hands and controllers (XR Input Modality Manager switches between them). It includes Editor hand simulation for testing without a headset, and every package comes from the Unity registry | On-device hand testing of the valve or cable fails the acceptance bar. Then evaluate the Meta Interaction SDK **for those two interactables only** |
| Spatial audio | **Meta XR Audio SDK** spatializer (free; Meta SDK licence the owner must accept). Fallback: Unity built-in 3D panning | Gives HRTF on Quest. The fallback keeps the game playable if the package is rejected | Licence review fails |
| Rendering | URP, Forward, Vulkan, multiview, linear colour, MSAA 4×, HDR off, no post-processing, fixed foveated rendering | The standard Quest configuration | — |
| Lighting | **Realtime only, no shadows.** At most 6 lights in the Ward and 3 affecting any one object. Ambient and fog set through `RenderSettings` | Surge, blackout and emergency-red must switch at runtime. Switching baked lightmaps is out of scope for v1 | The performance budget is missed. Then bake static lighting and add lightmap scenarios |
| Navigation | AI Navigation package (`NavMeshSurface`), baked by a scene build step | — | — |
| Device | Quest 3/3S primary, Quest 2 best effort, 72 Hz | The owner's headset model is unknown | The owner names the device |
| Android | arm64, IL2CPP, **min API 32, target API 34** (`QuestBuildConfig`) | Meta: Android 14 (API 34) required for apps from 1 March 2026; API 32 is the floor in Meta's manifest guidance | Meta raises the target API |

Every pin, with its source, is in `docs/collab/INTEGRATION_CHECKLIST.md` section 2. `python tools/unity/validate_packages.py` fails CI if the manifest drifts from it.

**Not used:** the Meta XR Core SDK or Interaction SDK (`OVR*`, `Oculus.*`, `Meta.XR.*` outside audio), Asset Store packages, and paid assets. `tools/review/check_rules.py` rejects any SDK namespace from that list.

---

## 2. Repository layout

```
patient-zero/
  Game/                                Unity project root: open THIS folder in Unity Hub
    Assets/PatientZero/
      Core/                            Claude: ids, events, services, layout, settings (no dependencies)
      Integration/                     Claude: scene composer, build scripts, validation (Editor only)
      Player/ Interaction/ Flow/ UI/   Codex A
      AI/ Audio/ Atmosphere/ Environment/   Codex B
    Packages/  ProjectSettings/        Claude (integration). Agents request changes
  docs/                                Claude, except docs/collab/CODEX_A_LOG.md and CODEX_B_LOG.md
  tools/                               Claude
```

Inside every area folder:

| Subfolder | Assembly | May reference | Contents |
|---|---|---|---|
| `Logic/` | `PatientZero.<Area>.Logic` | `PatientZero.Core` only | Engine-free rules: state machines, maths, tables. Compiled and tested by the logic harness without Unity |
| `Runtime/` | `PatientZero.<Area>` | Core, own Logic, same-seat areas, Unity packages | Thin MonoBehaviour shells that feed time and scene data into Logic |
| `Editor/` | `PatientZero.<Area>.Editor` (Editor platform) | the above + UnityEditor | `[SceneBuildStep]` methods, generated prefabs and materials |
| `Tests/EditMode/` | `PatientZero.<Area>.Tests` | Logic, Core, NUnit | Must also pass in the logic harness |
| `Tests/PlayMode/` | `PatientZero.<Area>.PlayModeTests` | anything | Scene-level tests, run in Unity at integration |
| `Generated/` | — | — | Assets written by the area's build steps. Never hand-edited |

Reference asmdefs **by name**, never by `GUID:`. Branches without `.meta` files resolve names only.

---

## 3. Ownership

A seat edits only its own paths. That includes "one-line fixes" elsewhere: you report them to the owner in your log instead. `check_rules.py --seat` fails any branch that strays.

| Path | Owner | What the others may do |
|---|---|---|
| `Game/Assets/PatientZero/Core/**` | Claude | Read. Request changes |
| `Game/Assets/PatientZero/Integration/**`, `Game/Packages/**`, `Game/ProjectSettings/**`, all `.meta` files | Claude | Read. Request packages and settings |
| `Game/Assets/PatientZero/{Player,Interaction,Flow,UI}/**` | **Codex A** | Read |
| `Game/Assets/PatientZero/{AI,Audio,Atmosphere,Environment}/**` | **Codex B** | Read |
| `docs/**`, `tools/**`, `README.md`, git config files | Claude | Read |
| `docs/collab/CODEX_A_LOG.md` / `CODEX_B_LOG.md` | that seat | Read |

**What each seat builds** (so nothing gets built twice):

| Concern | Owner | Notes |
|---|---|---|
| XR rig, hands/controller switching, locomotion, turning, comfort vignette, seated offset, fader, haptics | A | Implements `IPlayerQuery`, `IScreenFader` |
| Planks, valve, cable and socket, release lever, door latch, teleport anchors, physics props, impact noise calls | A | Raises objective events. Plays sounds only through `Cues` |
| GameFlow, pause/restart, scene loading, settings storage, calibration | A | Implements `ISettingsProvider` |
| Menus, captions display, hint text display, title and credits | A | Captions come from `GameEvents.CaptionRequested` |
| Hand infection-reveal visuals | A | Driven by `GameEvents.InfectionReveal` |
| Greybox geometry, colliders, NavMesh, occluders | B | Built from `WardLayout.Rooms` |
| Lights, emissives, fog, ambient, lighting states | B | The only code that touches `Light` or `RenderSettings` |
| Experience director: acts, beats, hints, tension, scare budget | B | Implements `IExperienceDirector` |
| Zombies: perception, state machine, navigation, animation | B | Raises `ZombieAlert`, `PlayerCaught` |
| Audio: cue bank, cue player, mixer, spatializer, voice budget, placeholder clips, captions text | B | Implements `IAudioCuePlayer`. The only code that touches `AudioSource` |
| Contracts, layout, scene composition, packages, project settings, Android build, merges | Claude | — |

---

## 4. Assemblies and the dependency rule

```
                PatientZero.Core  (references nothing)
               /        |         \
   <Area>.Logic   <Area>.Logic ...   (Core only)
        |               |
  Seat A runtime   Seat B runtime        A <-/-> B : never reference each other
  Player Interaction Flow UI     AI Audio Atmosphere Environment
        \               /
     PatientZero.Integration.Editor  (Claude; may reference everything)
```

* Seat A and seat B never reference each other's assemblies. All traffic goes through Core events and services.
* Seat B never references `Unity.XR.Interaction.Toolkit` or `Unity.XR.Hands`. It learns about the player only through `IPlayerQuery`.
* Unity enforces these through asmdef references, and `check_rules.py` checks them before Unity ever runs.

---

## 5. Contracts (all in `Core/Contracts`)

**Versions.** v1: approved 2026-10-28 (`b3bab8b`). v1.1: approved by the owner 2026-10-28, additive only: `GateId`, the `GateReleased` channel and `IExperienceDirector.IsGateReleased`, so the Act 1 door can open on the director's gate (requested by Codex 1, acknowledged by Codex 2).

### 5.1 Events: `GameEvents`

Allocation-free typed channels (`EventChannel<T>`). Subscribe in `OnEnable` and unsubscribe in `OnDisable`, because the channels are static and outlive scene reloads. **Every channel has exactly one writer seat.**

| Channel | Payload | Writer | Main readers | Raised when |
|---|---|---|---|---|
| `StateChanged` | from, to, reason | A · GameFlow | everyone | after a transition passes `GameStateRules` |
| `StateRequested` | requested state, reason | anyone | A · GameFlow | the director wants Ending/Complete |
| `ActStarted` | act, isRestore, attempt | B · director | everyone with act state | at run start and on every act start or restore |
| `ObjectiveProgress` | objective, 0..1 | A · puzzles | B · director (hint timers) | progress changes by ≥ 5 % |
| `ObjectiveCompleted` | objective | A · puzzles | B · director | once per objective per attempt |
| `GateReleased` (v1.1) | gate id | B · director | A · gated components (Act 1 door) | a gate's conditions are met; at most once per gate per act start. Gates close again at their act's `ActStarted` |
| `AssistChanged` | objective, level | B · director | A · puzzles and hint UI | the hint ladder steps up |
| `Noise` | position, radius, cue, origin, time | B · cue player | B · zombies, director | every cue with noise radius > 0 |
| `ZombieAlert` | zombie id, level, position | B · AI | B · director, A · haptics/UI debug | the alert level changes |
| `PlayerCaught` | zombie id, position | B · AI | A · GameFlow | a catch condition is met |
| `LightingChanged` | state, seconds | B · atmosphere | anyone | a lighting state starts |
| `InfectionReveal` | 0..1 | B · director | A · hand visuals | during Ending |
| `CaptionRequested` | text, kind, position, seconds | B · cue player | A · caption UI | a cue with a caption plays |
| `SettingsChanged` | `ExperienceSettings` | A · settings | everyone | on load and on every change |
| `TrackingChanged` | modality, left/right tracked | A · rig | B · director (hints), A · UI | the modality or tracking state changes |
| `HapticRequested` | hand, amplitude, seconds | B · AI/director | A · haptics (controllers only) | zombie proximity, scares |

### 5.2 Services: `Services<T>`

Register in `OnEnable` and unregister in `OnDisable`. A second live registration is refused and logged. A destroyed object that forgot to unregister reads as missing. **No `FindObjectOfType`, and no static `Instance` singletons anywhere else.**

| Interface | Implemented by | Scene | Used by |
|---|---|---|---|
| `IAudioCuePlayer` (via `Cues.Play`) | B · Audio | Bootstrap | everyone who makes a sound |
| `IPlayerQuery` | A · Player | Bootstrap | B · AI, director |
| `IScreenFader` | A · Player | Bootstrap | A · GameFlow, B · director (ending) |
| `ISettingsProvider` | A · Flow | Bootstrap | everyone |
| `IExperienceDirector` | B · Atmosphere | Ward | A · GameFlow (`StartRun`), gated components (`IsGateReleased`, v1.1), UI debug |

### 5.3 Identifiers: `Ids.cs`

`GameState`, `ActId`, `ObjectiveId`, `GateId` (v1.1), `CueId`, `LightingState`, `AlertLevel`, `NoiseOrigin`, `AssistLevel`, settings enums and `ZombieIds`. **Append-only with explicit numbers.** `check_rules.py` fails any branch that renumbers or removes a value. Need a new cue or objective? Request it, then use it once it lands.

### 5.4 Layout: `WardLayout`

Every anchor, puzzle, zombie marker and room is defined here in metres (map: `GAME_DESIGN.md` section 3). Build steps place objects **by layout id** and add a `LayoutTag` holding that id. Nobody writes a literal coordinate. `WardLayoutTests` prove that:

* every required interactable sits between 0.75 and 1.5 m high, within 0.7 m horizontally of a teleport anchor in the same act
* consecutive critical-path anchors are ≤ 3 m apart (teleport range), and every anchor clears the walls by 0.4 m
* the final patient's 1.3 m reach plus a 0.3 m margin never covers an Act 4 anchor, the walk between them or the release lever
* the Act 3 patrol line stays ≥ 0.8 m from every anchor

A layout change that breaks one of these is a design change, and it comes to Claude.

### 5.5 Settings: `ExperienceSettings`

One struct shared by everyone. Defaults are the most comfortable choices. `Sanitized()` clamps corrupt values. Seat A owns storage and UI, and everyone else reacts to `SettingsChanged`.

### 5.6 Game state: `GameStateRules`

```
Boot -> Calibration | Playing        Paused  -> Playing | Ending
Calibration -> Playing               Caught  -> Playing   (act restore)
Playing -> Paused | Caught | Ending  Ending  -> Paused | Complete
                                     Complete -> Playing  (full restart)
```

GameFlow refuses and logs anything else. Pause is allowed only from Playing or Ending. Zombies simulate only while `IsSimulationLive` (Playing, Ending).

### 5.7 Scene build steps: `[SceneBuildStep(scene, order)]`

See section 7.

---

## 6. Runtime architecture

**Two scenes.** `Bootstrap` loads once and stays: XR rig, GameFlow, settings, fader, UI, audio cue player. `Ward` loads additively and is **reloaded on every restart**: geometry, lights, puzzles, zombies and the director. Reloading is the reset mechanism, so no system has to undo its own state.

**Start of a run**

1. `CoreStaticReset` clears every static channel and service before the first scene loads.
2. Bootstrap services register in `OnEnable`.
3. GameFlow: `Boot → Calibration` on first launch or unconfirmed settings, otherwise `Boot → Playing`.
4. Entering a run: fade to black → load `Ward` additively (async) → `IExperienceDirector.StartRun(act, isRestore)` → fade in.
5. The director raises `ActStarted`. Each system then sets the **act-start state** in `GAME_DESIGN.md` section 4. Every act start is idempotent and captures nothing.

**Caught:** `PlayerCaught` → `Playing → Caught` → 0.4 s fade → unload and reload `Ward` → `StartRun(currentAct, true)` → `Caught → Playing`.
**Full restart:** from Paused or Complete, the same path with `Awakening`.

**Pause:** GameFlow sets `Time.timeScale = 0` and `AudioListener.pause = true` (UI cues set `ignoreListenerPause`), then raises `StateChanged`. Everything else freezes because gameplay uses scaled time. Pause is triggered by the controller menu button, the left-hand menu gesture, `OnApplicationFocus(false)` / `OnApplicationPause(true)` (system menu, headset removed). **It resumes only on explicit player input.**

**Tracking loss:** while in hands mode, if a hand is lost for more than 1.5 s, held objects drop with zero velocity and the UI shows "Hands not visible". There is no auto-pause, because zombies react only to noise and movement and a still player is safe.

**Logic/shell split.** Every rule worth testing lives in a plain C# class in `Logic/`: `GameFlowCore`, `ValveMath`, `ZombieBrain`, `DirectorCore`, `CueTable`… The MonoBehaviour shell passes in `deltaTime`, positions, events and a seeded `System.Random`, then applies the outputs (NavMeshAgent targets, light values, `Cues.Play`). Logic must not read `Time`, `UnityEngine.Random`, `Physics` or the scene. The harness stub omits them on purpose, so violations don't compile.

**Noise pipeline.** A puzzle calls `Cues.Play(cue, pos, NoiseOrigin.Player, intensity)`. B's cue player plays the sound and, if the cue's noise radius is above 0, raises `Noise(pos, radius × intensity, …)`. Each zombie's hearing tests distance against that radius, with an occlusion factor through closed doors. **Anything audible that matters is also hearable.** Audible volume and gameplay radius are tuned together in one table, B's cue table.

---

## 7. How scenes are built (nobody hand-edits a scene)

* Scenes, prefabs and materials are **generated** by Editor code and committed only by Claude at integration. Until an agent can run the Unity Editor, it writes only `.cs`, `.asmdef`, `.json` and `.md` files. **No hand-written `.unity`, `.prefab`, `.asset`, `.mat` or `.meta` files.** `check_rules.py` enforces this, because made-up GUIDs collide and break references.
* Each seat contributes `[SceneBuildStep(BuildScene.X, order)] public static void Build(Transform root)` methods in its `Editor/` folder, within its order band (see `SceneBuildStep.cs`).
* Claude's composer (`PatientZero/Build Scenes` menu, or `-batchmode -executeMethod PatientZero.Integration.Editor.SceneComposer.BuildAll`):
  1. creates `Bootstrap.unity` and `Ward.unity` empty
  2. creates one root object per step and calls the steps in order
  3. bakes the NavMesh after the Environment steps
  4. validates: every required `WardLayout` id has exactly one `LayoutTag`, within 5 cm of its layout position; every service interface has exactly one implementer per scene; no missing scripts or references
  5. saves the scenes and sets build settings
* A failed validation fails the build. The composer is written by Claude once the Editor exists (owner action 2).

---

## 8. Coding rules

`tools/review/check_rules.py` enforces the rules marked ✓. Reviewers check the rest.

1. ✓ No `FindObjectOfType` / `Find*` / `GameObject.Find` / `SendMessage` at runtime. Use `Services<T>` or serialized references.
2. ✓ No LINQ in runtime code. ✓ Inside `Update`/`LateUpdate`/`FixedUpdate`/`On*Stay`: no collection or array allocation, no string building, no `GetComponent`, no `Camera.main`.
3. ✓ Only Audio touches `AudioSource`/`AudioMixer`. ✓ Only Atmosphere and Environment touch `Light`/`RenderSettings`. ✓ Only Flow writes `Time.timeScale`/`AudioListener.pause`.
4. ✓ Each `GameEvents` channel is raised only by its writer seat.
5. ✓ No `OVR*`/`Oculus.*`/`Meta.XR.*` (except Meta XR Audio inside Audio). ✓ Seat B uses no XRI or XR Hands types.
6. ✓ `Logic/` code uses no `MonoBehaviour`, `Time`, `UnityEngine.Random`, `Physics`, `GameObject` or `Resources`.
7. ✓ Every script sits under an asmdef. Nothing compiles into Assembly-CSharp.
8. Gameplay uses scaled time. Only the fader and menus use unscaled time.
9. Every subscription has a matching unsubscribe. The PlayMode restart test checks every channel's `SubscriberCount` returns to baseline after 10 restarts.
10. Physics: fixed timestep 1/72 s, ≤ 30 active rigidbodies, held objects interpolated, no `ConfigurableJoint` chains (the cable is a kinematic spline, not a joint chain).
11. Pool everything spawned more than once (audio voices, impact effects). Nothing is instantiated during Playing except by pools warmed at load.
12. No `async void`. Coroutines stop in `OnDisable`.
13. Public API is documented in one line: what it does and who calls it.

---

## 9. Branches, review and merging

Canonical repository: `https://github.com/Build-with-SK/patient-zero-vr.git` (public). Identities: `AGENTS.md`.

| Branch | Seat | Notes |
|---|---|---|
| `main` | — | created 2026-10-28 from approved contract v1 and published to GitHub at `b3bab8b`. Default branch. Ruleset "Protect main" blocks force-pushes and deletion (no bypass). Only Claude merges into it |
| `claude/technical-director` | Claude (`Build-with-SK`) | contracts, docs, tools, integration |
| `codex/gameplay` | Codex 1, seat A (`lavanyahd`) | cut from `origin/main` |
| `integration/xr-systems` | Codex 2, seat B (`businessteen`) | cut from `main` on 2026-10-28 after the owner approved the migration (`docs/collab/REVIEW_XR_SYSTEMS_72be6d8.md`) |
| `codex/xr-systems`, `Lavanya` | — | frozen records of Codex 2's first delivery (`72be6d8`, unrelated history). Never pushed to, rebased or deleted |

**Ready for review:** the agent appends to its log `READY FOR REVIEW <commit>` with its logic-harness output and `check_rules.py --seat <X> --base main --head <branch>` output, both clean.

**Review** (`docs/REVIEW_CHECKLIST.md`): Claude reruns both tools, then reads the diff against the checklist. Each finding goes into the agent's log as `FIX-<seat>-<n>` with a file and line, the defect, the exact change required, and severity (BLOCKER / MAJOR / MINOR). Claude does not edit seat-owned code. The one exception is a merge-conflict resolution, and that is recorded in the log.

**Merge order** (the seats share no files, so conflicts can only come from Core, Packages or ProjectSettings, which only Claude edits):

1. `claude/technical-director` → `main` (done for contract v1 on 2026-10-28; later contract changes follow the same path)
2. `integration/xr-systems` (seat B) → `main` (the world exists first)
3. `codex/gameplay` (seat A) → `main`
4. Integration commit by Claude: generated scenes, `.meta` files, package lock, settings

After every merge: the logic harness, `check_rules.py`, and, once Unity exists, an Editor compile, EditMode and PlayMode tests, the scene build and its validation. A merge that fails any of these is reverted, not patched in place.

**Unity YAML merges** (once the Editor is installed), for generated files only:

```
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'<Unity install>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

---

## 10. Performance budgets (detail in `PERFORMANCE_REPORT.md`)

72 Hz (13.9 ms). Main thread ≤ 8 ms, GPU ≤ 11 ms. ≤ 100 draw calls, ≤ 250 k visible triangles. 0 B GC allocation per frame in Playing. ≤ 1 GB resident memory. ≤ 24 real audio voices, ≤ 12 of them spatialized. Scene reloads happen only under a full fade.

---

## 11. Blockers and owner actions

| # | Action | Who | Unblocks |
|---|---|---|---|
| 1 | Install Unity Hub and **Unity 6.3 LTS** with **Android Build Support** (OpenJDK, Android SDK and NDK). Sign in to your Unity account (I can't create accounts or sign in for you) | Owner | All Editor compilation, scenes, builds |
| 2 | Open `Game/` as a project: Unity Hub → *Add project from disk* → `Documents/patient-zero/Game`. Hub reads 6000.3.27f1 from `ProjectVersion.txt`; packages are already pinned in `Packages/manifest.json` and the composer is written. Then tell Claude, who runs `docs/collab/INTEGRATION_CHECKLIST.md` section 6 and commits the generated settings | Owner, then Claude | Integration |
| 3 | Headset: enable developer mode (Meta Horizon app), connect over USB, allow USB debugging. `adb devices` must list it | Owner | Every device check |
| 4 | Say which headset model you have (Quest 2, 3 or 3S) | Owner | Performance targets |
| 5 | Read and accept the Meta XR Audio SDK licence, or choose the built-in panning fallback | Owner | Spatial audio |
| 6 | Give each Codex agent its brief (`docs/collab/CODEX_A_BRIEF.md`, `CODEX_B_BRIEF.md`) | Owner | Implementation |
| 7 | ~~Approve contract v1 so Claude creates `main`~~ **Done 2026-10-28** | Owner | Agents branch from `main` |
| 8 | If either Codex agent runs in the cloud: create a **private** GitHub repo and allow Claude to push. Git LFS is installed locally; run `git lfs install` once before the first binary asset | Owner | Remote collaboration |

---

## 12. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Agents can't run Unity, so MonoBehaviour and Editor code goes untested until integration | Compile errors pile up at the first Editor open | Logic/shell split, harness tests, static checker, a budgeted fix round at integration |
| Hand tracking drops when a hand rotates on the valve or occludes the plug | Puzzle blocked in hands mode | Ratchet valve (re-grip keeps progress), pinch or whole-hand grab, assist ladder, controller fallback always live |
| LCD panels (Quest 3/3S) show dark grey instead of black | Darkness act loses tension | Brightness calibration, fog, lighting never depends on pure black |
| Audio attenuation wrong (Unity's log rolloff stops attenuating at Max Distance instead of falling to silence) | Zombies audible through the whole ward. Unfair hearing | Custom curves reaching 0 at Max, plus a cue-table test that every zombie is audible farther than it can hear |
| Leaked static subscriptions across Ward reloads | Double-handled events, exceptions after a restart | Subscribe/unsubscribe rule, `SubscriberCount` restart test |
| Ward reload hitch on Quest | Visible stall | Reload only under full fade. Measure in `PERFORMANCE_REPORT` |
| Package version drift between agents | Code written against different APIs | Claude pins packages at project creation and commits `packages-lock.json` |
| Two agents interpret a beat differently | Duplicate or contradictory behaviour | One owner per concern (section 3). The director owns timing, puzzles own their mechanics |
