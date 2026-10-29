# Seat B integration handoff

Approved base: `origin/main` at `44ea7b7` (PR #4, contract v1.1; merged into this branch); historical sources remain at frozen `72be6d8`
and its implementation parent `87bb494482a0c00ed40d52eebb2871009c730091`.
No old package, duplicate Core, SDK imports, handwritten Unity assets or foreign-seat files are introduced.

## What is built

The approved layout is isolation B-7 â†’ corridor â†’ exit vestibule, with supply/side/linen rooms.
Round 2 dresses the existing SupplyRoom as an operating theatre, as requested by the owner, without
moving any room, interaction, spawn or doorway. It adds a static operating table, unlit surgical lamp,
sterile cabinet and instrument shelf/tray, plus patient-room bed/mattress dressing, with tested clearance around Z2's doorway route and layout
points. There are no external assets with verified file licences in the project, so these use the approved
procedural fallbacks. The geometry contains six floors/ceilings, deduplicated
walls, six aligned door openings/headers, a monitor, narrative text, exit sign, guidance strip, cart landmark (A supplies the physical trolley),
gurney, readable 1.3 m chain and PA/vent dressing. Functional door leaves and cart interactions belong
to seat A. Text currently uses Unity's built-in legacy font as a generated greybox placeholder.

| Scene / order | Builder | Output |
| --- | --- | --- |
| Ward 100 | `Environment.Editor.HospitalBuildSteps.Build` | RoomGeometry primitives, colliders, non-puzzle markers, dressing and NavMeshSurface |
| Ward 150 | `HospitalBuildSteps.BakeNavigation` | Unity-generated NavMesh from Environment child colliders, saved to `Environment/Generated/WardNavMesh.asset` (GUID retained on rebuild) |
| Ward 200 | `Atmosphere.Editor.AtmosphereBuildStep.Build` | Six shadowless short-range fixtures, emission/fog controller and director |
| Ward 500 | `AI.Editor.ZombieBuildStep.Build` | Four generated seven-bone, one-material mannequin prefabs and director bindings; one active actor per act |
| Bootstrap 600 | `Audio.Editor.AudioBuildSteps.Build` | Persistent cue-player service, native-generated Master + World/Voice/Ambience/Music/UI mixer |
| Ward 650 | `AudioBuildSteps.Emitters` | Act-specific loops and director tension binding |

These are `[SceneBuildStep]` sources for Claude's composer. **2026-10-29 update:** B's
assemblies compile in Unity 6000.3.27f1; native preview tests execute the hospital geometry
builder, with 149/149 EditMode and 9/9 selected PlayMode tests passing. Full saved-scene
composition and end-to-end gameplay have separate integration dependencies (see DESKTOP_PLAYTEST.md).
They generate native assets under each area's Generated directory when the editor is available;
no `.meta`, `.unity`, `.prefab`, `.asset`, `.mat`, `.mixer` or audio binary is handwritten or committed.
The Audio mixer factory is isolated behind Editor-only reflection; method names were checked against
UnityCsReference 6000.3, but its native behavior still requires editor validation. A missing native API
fails scene generation explicitly rather than manufacturing a mixer asset.

## Required integration bindings

- Claude pins Unity **6000.3.x**, URP, AI Navigation, XRI â‰¥3.5, XR Hands â‰¥1.8 and the test framework
  in the approved Game project. B has no XRI/Hands/Meta Interaction SDK assembly references.
- Seat A registers **IPlayerQuery**, **ISettingsProvider** and **IScreenFader** in Bootstrap and calls
  **IExperienceDirector.StartRun** after Ward load. B registers IAudioCuePlayer and IExperienceDirector.
- Seat A alone creates rig and interaction prefabs. The old tracking-settle/cancel/velocity-suppression
  sources and tests at `72be6d8` are references for its XRI port. B's new player-facing adapter is
  the contract-only perception snapshot; it neither reads hand joints nor owns grabbed objects.
- Puzzle audio calls go through **Cues**. Impact intensity is speed/3 clamped to [0,1]; smooth steps
  and teleport landings use their dedicated cue ids. B raises Noise only after an accepted audible cue.
  Intensity zero emits no noise; user volume preferences do not reduce gameplay hearing radii.
- Supply player/held-prop collision layers separately from geometry/doors. **ZombieNavigation.Obstacles**
  and **SightBlockers / AudioCuePlayer.Occluders** default to layer 0 for the greybox; they include walls
  and closed doors. **HearingOccluders defaults to Nothing** and must be assigned to closed door leaves
  only: walls and the impact source never halve hearing. The shared door/player/held-prop layer mapping
  remains TD integration action INT-B-2; it was not included in approved v1.1. Geometry/door masks must
  exclude player/zombie colliders. Zombie roots use Ignore Raycast. Seat A's moving
  doors require appropriate NavMesh carving or a final bake by Claude, plus the collision-only path
  guard. Do not mark a door traversal/catch test passed before this is verified.
- Environment creates static tags for all zombie markers and A2.Monitor/A3.Beacon/A3.Cart/A3.Drip.
  Seat A supplies anchor, puzzle and functional-door tags. Do not duplicate LayoutTag ids on dynamic
  actors or secondary audio/visual children. Validate every required tag/coordinate in the composer.
- **Act 1 gate, approved Core v1.1:** B's ExperienceDirector is the only `GateReleased` writer. It raises
  `GateReleased(GateId.Act1Door)` exactly once per Act 1 start after unbarring, the window beat, the lure
  and Z1's four-metre clearance. `IExperienceDirector.IsGateReleased(GateId.Act1Door)` stays true while
  Act 1 remains active; it is false for None, other acts and restores/new runs. The latch closes before
  `ActStarted` is published. Seat A's door subscribes on enable AND queries the service for late load;
  it fails closed if the service is missing. Door opening still reports `Act1_DoorOpened`, which advances
  the director to Machinery. An early/stale completion while the gate is closed is ignored; the door must
  report it after release. Do not have a second component publish GateReleased.
- Inactive zombies disable their NavMeshAgents as well as their visuals; act starts reset the animation
  pose and AI clock. The native navigation test unloads/reloads Ward and checks the saved NavMesh asset
  and complete routes. Agent type 0's bake radius remains TD action INT-B-1 (target 0.25 m).
- The director owns story beats, hints, tension, adaptive attempt memory and InfectionReveal.
  Seat A owns ending screen fades/title and player hand rendering. Ward reload is the reset mechanism.

## Audio and comfort

All contract cues have exactly one row, including a silent None sentinel. World clips are generated
mono 48 kHz placeholders; PA clips are noise bursts with actual approved speech captions, not recorded
speech or TTS. Native 3D panning is the default. Optional HRTF is disabled until licensing is confirmed
and a spatializer is selected; missing spatializer support falls back to panning. No headphone test
has been performed, and front/back localization is not established by a stereo-panning test.

The warmed pool has 24 voices, at most 12 HRTF-spatialized. Overflow steals the least important,
oldest eligible voice, reports the stolen cue, and never steals UI or Voice. A pool consisting only
of protected voices explicitly rejects new cues and counts rejections. Master/effects/voice controls,
speech ducking (-6 dB), post-scare ambience ducking (-9 dB), occlusion gain/filter and scaled fades
are applied in the audio service, so fallback routing remains functional.

Standard mode has two hard-scare budgets; Reduced has none and no final lunge. Blackout disables sight.
The window light pulse replaces a scheduled flicker edge, lasts approximately 250 ms (â‰¤300 ms), and
holds other flicker stable to stay under three discrete luminance changes in any second. Reduced
flicker removes that pulse and holds the fluorescent/red variations constant. No camera motion is added.

## Verification

Run from repository root:

```sh
python tools/logic_tests/run_logic_tests.py
python tools/review/check_rules.py --seat B --base origin/main --head integration/xr-systems
```

These compile/test Core and Logic using the approved stand-ins. They do **not** compile Unity shells.
Round 2 evidence and actual blocked tool output: [ROUND_2_VALIDATION.md](ROUND_2_VALIDATION.md).
Unity-facing sources are marked `UNVERIFIED IN EDITOR`. The canonical composer is now on this branch;
Unity 6000.3.27f1 is still absent. Once Claude provisions the editor:

1. Compile Game in Unity 6.3 and fix all API/type/assembly errors before generating assets.
2. Generate Bootstrap/Ward, validate tags/services/references, and review native mixer groups and mesh budgets.
3. Run B's PlayMode tests: real cue-to-hearing boundary, missing/zero-intensity assets, mono/custom rolloff,
   director lifecycle subscribers, and complete NavMesh paths along approved routes. A missing generated
   Ward fails the route test; it is not skipped or counted as a passing test.
4. Run the full integrated autoplay, door blocking, restore Ã—10, tracking-loss and pause suites with seat A.
5. Perform headphones/localization checks and Quest captures required by TEST_REPORT/PERFORMANCE_REPORT.
   Target remains 72 Hz; no device performance or Unity compilation is claimed by this branch.

No external art/audio has been imported. Licensed upgrades remain subject to the existing asset manifest.


## First desktop test (2026-10-29)

Use `DESKTOP_PLAYTEST.md` for the exact assembly-conflict resolution, proposed compile/
Video-module patches, Windows menu sequence, simulator controls and spawn/objectives.
No A-owned file or TD project setting is modified on this branch. The owner's existing
Unity-generated files are preserved; native validation runs in separate checkouts.
B's layer binding consumes a proposed `PZ_ClosedDoor` user layer if configured and otherwise
keeps door-hearing disabled with an explicit validator warning. The bake refuses an
incorrect radius; Claude's composer must set agent type 0 to 0.25 m before step 150.
