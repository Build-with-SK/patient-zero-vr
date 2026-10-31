# First Windows desktop playtest â€” integration handoff

2026-10-29 Â· Codex 2 Â· Unity **6000.3.27f1** Â· PR #3

Use this with Claude's `integration/round2`, **not the B branch alone**: Player, Flow,
Interaction and UI are supplied by Codex 1. Do not update the owner's checkout over
untracked Unity output. The existing backup is left untouched. Perform the trial in a
new checkout; let Claude review and integrate the proposed A changes.

## Assembly conflict and minimal proposed resolution

`da316a4` adds dependencies after `PatientZero.UI` in Flow's PlayMode asmdef. Claude's
integration exception E-5 already changed the same array tail to explicit TestRunner
references and replaced `optionalUnityReferences` with explicit NUnit/test settings.
This is a textual conflict, not incompatible assemblies.

Keep **all** these Flow PlayMode references:

```json
[
  "PatientZero.Core", "PatientZero.Flow", "PatientZero.Interaction",
  "PatientZero.Player", "PatientZero.UI", "UnityEngine.TestRunner",
  "UnityEditor.TestRunner", "PatientZero.Flow.Logic", "Unity.XR.CoreUtils"
]
```

Also preserve `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]`,
`autoReferenced: false` and `defineConstraints: ["UNITY_INCLUDE_TESTS"]` from E-5.
Add `PatientZero.Flow.Logic` to Player's runtime asmdef, retaining its existing
`Unity.XR.CoreUtils` and all other dependencies. Do not replace the whole Flow asmdef
with `da316a4`'s version: that would lose E-5's settings.

`Validation/assembly-resolution.patch` makes only those reference additions against
`integration/round2` **7c20447**. Its application was checked in a temporary Git index;
the resolved graph has unique names, no duplicate references, no missing project/package
assemblies and no cycles. CoreUtils is present in the resolved Unity package cache.

An actual combined Unity compile then exposed **three more A-owned errors**:

| File | Proposed fix in `Validation/unity-compile-followups.patch` |
|---|---|
| `Interaction/Runtime/PhysicalPuzzle.cs` | Cast `interactable` to `IXRSelectInteractable` in `CancelInteractableSelection` to select XRI 3.7's supported overload |
| `Interaction/Runtime/PhysicsProp.cs` | Cast `grab` to `IXRSelectInteractable` in the same call |
| `UI/Editor/UiBuildStep.cs` | Qualify `FindFirstObjectByType<PatientZero.Flow.SettingsProvider>()`; `UnityEditor.SettingsProvider` is otherwise ambiguous |

After importing Hands Interaction Demo, Unity also reports CS1069 for `UnityEngine.Video.VideoPlayer`.
`Validation/sample-module-resolution.patch` proposes adding **com.unity.modules.video 1.0.0**
to the TD-owned package manifest; the demo contains video tutorial scripts even though the game
itself does not require video. Enable the built-in Video module before composing.

The integrated PlayMode run also demonstrated that scene-load completion can precede
`GameFlow.Start`: the test fixture saw Boot, skipped calibration and ran assertions before
Ward existed. `Validation/playmode-calibration-resolution.patch` proposes waiting for Boot
initialization before confirming calibration. This is a test-fixture change, not a calibration bypass.

These are **proposals for Claude/Codex 1**. No A file is changed by the commits on PR #3.
They were applied only in a disposable combined validation checkout.

## Safe trial integration (Claude, separate checkout)

From the repository root, after committing/saving any personal work there:

```powershell
git fetch origin
git worktree add -b integration/desktop-playtest ../patient-zero-desktop-playtest origin/integration/round2
Set-Location ../patient-zero-desktop-playtest
git merge --no-commit --no-ff origin/integration/xr-systems
```

At the reviewed tips the merge has one conflict, in **B's** `ExperienceDirector.cs`:
Claude's E-1 gate adapter overlaps the complete v1.1 implementation on PR #3.
In this new trial checkout, retain B's implementation (which implements the same Core
service and owns the one-shot gate event), then stage it:

```powershell
git restore --source=origin/integration/xr-systems -- Game/Assets/PatientZero/Atmosphere/Runtime/ExperienceDirector.cs
git add Game/Assets/PatientZero/Atmosphere/Runtime/ExperienceDirector.cs
```

Review the four proposed patches before applying. Windows Git may check patch files out
with CRLF while asmdefs use LF; normalize **the patch files only**, then check/apply:

```powershell
python -c "from pathlib import Path; ps=Path('Game/Assets/PatientZero/Environment/Validation').glob('*.patch'); [p.write_bytes(p.read_bytes().replace(b'\r\n', b'\n')) for p in ps]"
git apply --check Game/Assets/PatientZero/Environment/Validation/assembly-resolution.patch
git apply Game/Assets/PatientZero/Environment/Validation/assembly-resolution.patch
git apply --check Game/Assets/PatientZero/Environment/Validation/unity-compile-followups.patch
git apply Game/Assets/PatientZero/Environment/Validation/unity-compile-followups.patch
git apply --check Game/Assets/PatientZero/Environment/Validation/sample-module-resolution.patch
git apply Game/Assets/PatientZero/Environment/Validation/sample-module-resolution.patch
git apply --check Game/Assets/PatientZero/Environment/Validation/playmode-calibration-resolution.patch
git apply Game/Assets/PatientZero/Environment/Validation/playmode-calibration-resolution.patch
```

If `--check` fails on a newer tip, inspect the current JSON/code; do not force the patch.
Claude can commit the reviewed resolution locally with the six explicit A paths, the manifest addition and
the B conflict staged. Do not use `git add -A` over Unity-generated files, `git clean`,
reset or a force-push. A new worktree leaves the owner's old `.meta`, XR, settings,
package lock and backup files in place. PR #3 remains draft and is not merged here.

## Generate scenes in Unity

1. Add the new checkout's **Game** directory to Unity Hub, open with 6000.3.27f1 and let
   packages resolve. Use **Windows** as the active Build Profile. Accept the Input System
   activation/restart prompt if shown. First require **zero Console compile errors**.
2. Run **PatientZero â†’ Import Required Samples**. Wait for compilation after import.
   Starter Assets, Hands Interaction Demo and XR Hands HandVisualizer are needed by the
   rig builder. A missing Palm or modality manager is a rig/sample blocker; do not build
   a substitute rig or edit an already-generated scene to conceal it.
3. Run **PatientZero â†’ Desktop Simulation â†’ Use Desktop Simulation** and wait for its
   simulator sample scripts to compile. Keep **PZ_DESKTOP_MOUSE absent** for this mode.
   Confirm the report actually enables auto-instantiation of the XRI simulator and leaves
   Standalone XR initialization off. If it warns that simulator settings were not found,
   enable **Use XR Interaction Simulator in scenes** in XRI project settings manually.
4. If no render pipeline is assigned: **Assets â†’ Create â†’ Rendering â†’ URP Asset (with
   Universal Renderer)**, then assign it in **Project Settings â†’ Graphics**, plus the
   active Quality level if it has a pipeline override. Use Forward rendering, HDR off,
   shadows off, MSAA 4x. This desktop setup does not require Android Build Support;
   `Configure Quest` exits early when Android support is absent.
5. Run **PatientZero â†’ Build Scenes**. Claude's composer configures the type-0 bake
   radius to **0.25 m** before Ward step 150. B's bake now refuses a different radius,
   saves `Environment/Generated/WardNavMesh.asset`, and preserves its GUID on rebuild.
   Read `Game/Logs/PatientZero/compose.txt`; do not accept a failed report or partial scene.
6. Run **PatientZero â†’ Hospital â†’ Validate Generated Hospital**. This loads the saved
   scenes for read-only B checks and restores the previous scene setup. It checks saved
   navigation, three key paths, four bound zombie actors, six shadowless lights, director
   emitter bindings, the mixer and collision masks. Missing door-layer setup is reported
   as a warning, not disguised as a successful hearing-occlusion check.
7. Open **Assets/PatientZero/Integration/Generated/Bootstrap.unity** by itself and press
   Play. Bootstrap loads **Ward** additively. Starting Ward alone omits player/settings/
   audio/fader services and is not a valid game launch.

The full composer and B validator are distinct checks: the composer also checks every
required layout tag, all five Core services and missing references. B's geometry alone
cannot provide A's puzzle/anchor tags or player services.

## Door layer and navigation handoff

`WardCollisionBinding` recognizes the **proposed** name `PZ_ClosedDoor` when Claude
supplies it on a free **user layer** (8â€“31). This is a B-local binding, not an amendment
to Core or an automatic TagManager edit. Claude/Codex 1 must approve and assign only
the physical door-leaf colliders to it; opening leaves move the colliders out of the ray.
Do not assign player, zombie, puzzle, bottle or wall objects to the door layer.

Rebuild after assigning it. Hearing uses only the door mask (radius x0.5 through a
closed door); sight, audio and navigation use Default geometry **plus** that mask.
Without it, hearing-door attenuation is still unconfigured. The dynamic collider guard
already stops movement through Default-layer door leaves. Static walls route through
the NavMesh; a dynamic door waits briefly, then reports bounded path failure instead
of walking through it. Carving obstacles on A's doors/props remain A/Claude's decision.

B now keeps only the `A3.Cart` landmark. A's trolley is the sole physical cart; there is
no static B cart collider underneath it.

## Controls and first run

**Default mode: XRI Interaction Simulator.** Click the Game view. Use its on-screen
input/controls panel for the installed 3.7 sample to move the simulated HMD and select
left/right controllers, grip and UI select. Aiming at anchors uses the normal XRI
teleport binding. Confirm calibration using the UI. This follows the real XRI interaction
path; it is not headset or hand-tracking validation.

Default bindings verified from the installed XRI 3.7 package documentation:
Tab cycles FPS/device mode; H selects the HMD; `[` / `]` select left/right devices;
RMB+mouse rotates the active device; I/J/K/L drive its stick axes; G is controller grip;
T is controller trigger; LMB selects; Shift routes device buttons to the left device;
M is the controller menu button. In hand mode M toggles pinch and K toggles grab.
Use the simulator panel to confirm the active mode before gripping or pressing buttons.


**Optional mouse adapter (separate mode, A-owned):** only after Claude/Codex 1 configures
it and disables the XRI simulator, set Standalone scripting define `PZ_DESKTOP_MOUSE`.
The source implements RMB+mouse look, T next anchor in the current act, LMB grip/drag,
Q throw, Enter calibration, Esc pause; WASD works only with smooth locomotion enabled.
Do not activate both desktop input modes. The old UI text mentioning these keys alone
does not mean that the define is active or that every puzzle is mouse-playable.

- Spawn: `WardLayout.A1.Start`, floor **(0, 0, -0.5)**, facing +Z, with calibrated eye height.
- First objective: remove the three barricade planks. The tray props are optional.
  Wait for Z1's window encounter and retreat; the v1.1 gate then permits the door to open.
- Act 2: ratcheted valve, then plug/socket puzzle; Act 3: beacon-guided dark corridor;
  Act 4: release lever, emergency exit and hand infection reveal.
- Restart: pause UI â†’ **Restart act** or **Restart run**; completion UI â†’ **Play again**.
  Stop/re-enter Play for a fresh session. Do not manually reload Ward over a running
  Bootstrap. Focus loss may pause; explicitly select Resume after returning to Game view.

## Windows test sequence

After composition, **Window â†’ General â†’ Test Runner**:

1. Run all EditMode tests. Native B validation includes `HospitalGenerationTests`, which
   actually builds procedural room geometry and checks wall colliders and the cart marker.
2. Run PlayMode fixtures `NavigationObstacleTests`, `HearingSceneTests`, `AudioSceneTests`
   and `DirectorLifecycleTests` (these can run without generated game scenes).
3. Run `NavigationSceneTests` against the generated Ward; it reloads the saved scene twice
   and checks persisted navigation, Z1 window reach and complete routes. Then run A's
   `IntegrationPlayTests` and the complete PlayMode suite.
4. Manually complete a desktop run, pause, restart each act and confirm the ending. Listen
   for beacon/alarm direction, zombie footsteps and occlusion; check the green blackout
   exit sign and Reduced settings. Record errors and timing instead of assuming a pass.

See `Validation/native-results.json` and CODEX_B_LOG for measured results. Native B tests
are not a complete game playtest. Procedural art/audio are placeholders; physical Quest,
real hand tracking, subjective audio direction and 72 FPS remain unverified.


## Native findings fixed on B's branch

- The exact two-second lure deadline was evaluated differently by Mono than the portable
  harness. Store the float deadline before comparing: all five failing EditMode gate cases pass.
- Project ear-height cue/window targets onto the actor's floor before querying the NavMesh.
  Otherwise the 1.65 m window landmark can select the walkable ceiling and stall Act 1.
  Saved floor-projected window/theatre/corridor routes have been checked in the native engine.
- Initialize the late-loaded director on StartRun and zombies on ActStarted. Bootstrap can
  broadcast Playing before Ward subscribes; waiting for a second state event freezes the first run.
  Dedicated native regressions cover that missed-event ordering.
- A blank batch-mode scene setup may have no active saved scene to restore. The validator
  now returns to an empty scene in that case instead of masking route errors with a restore exception.

The combined trial initially failed compilation until the proposed A/reference/Video patches
were applied. Successful combined results therefore apply to that explicitly patched trial,
not unchanged integration/round2 or protected main. No trial integration commit was published.


## Measured handoff result (2026-10-29)

- B portable suite: **147/147**, tooling **24/24**, static/package checks clean.
- Unity EditMode: **149/149**, including actual procedural geometry generation.
- Patched combined trial: **15/15 PlayMode**, including autoplay to Complete, pause/load,
  plug recovery, ten restarts, saved NavMesh reloads and the new late-loaded director test.
- Additional isolated first-encounter fixture: **1/1**; the walker traverses the saved floor
  navigation to the window, leaves and releases the gate.
- Composer: **PASS**, Bootstrap + Ward saved, 30 layout tags, all five services validated;
  saved hospital validator **PASS** with the door-layer warning noted above.
- Desktop simulator setup: **PASS**, four settings changed, Standalone XR startup off.

These are headless native tests in separate checkouts. The four proposed patches remain
review items for Claude/Codex 1; the six A files and TD package manifest are not changed
on PR #3. A normal human keyboard/mouse run and visual/headphone checks remain outstanding.
