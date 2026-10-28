# Integration guide

## Import without changing scenes

1. Use Codex 1's Unity 6 project and add the `Packages/com.patientzero.xr` directory as an embedded
   package, or add it as a local package with Unity Package Manager. Adopt the pinned baseline from
   `XR_SYSTEMS.md`; review the existing manifest before adding dependencies.
2. In the isolated test fixture, Unity should resolve Meta Interaction SDK 207.0.0 and its OVR/core
   dependencies. The runtime Core assembly does not require Meta. The optional Meta adapter assembly
   is enabled only for Interaction SDK 207.x; other versions require an explicit compatibility review.
3. Android settings: IL2CPP, ARM64, OpenXR loader under Android XR Plug-in Management; enable the Meta
   Quest support feature, controller interaction profile, Hand Tracking Subsystem and Meta Quest hand
   features provided by the installed packages. Resolve OpenXR and Meta Project Setup Tool validation
   findings. Do not run a second Oculus XR loader alongside OpenXR. Configure hand tracking as hands
   and controllers in the SDK rig. Set floor-level tracking origin and request/test 72 Hz on the target
   headset through the SDK's supported display settings.
4. Use a Quest-compatible HRTF spatializer for meaningful front/back headphone localization (for example
   the supported Meta XR Audio SDK configuration). Select it in Project Settings → Audio, then enable
   `SpatialAudioSystem.UseSpatializer`. Without it the system uses Unity's 3D stereo panning, which
   does **not** establish convincing front/back localization. The mixer and diagnostic bank still work.

## Generate the XR and interaction prefabs

The repository does not invent serialized SDK GUIDs or ship third-party SDK assets. Its builder creates
the following outputs **after** valid source prefabs are assigned in Unity:

| Output | Source configuration required |
| --- | --- |
| `PZ_XRRig.prefab` | Meta comprehensive rig with center-eye camera/audio listener, SDK hand and controller interactors, SDK input switching |
| `PZ_Throwable.prefab` | SDK hand/controller grabbable with SDK throw velocity, a single Rigidbody and collider |
| `PZ_TwoHanded.prefab` | SDK one/two-hand transformers, both hand and controller interaction, two simultaneous grab points; constrain scale where needed |
| `PZ_Valve.prefab` | SDK constrained rotation transformer around an authored pivot; grab and pinch configurations on the moving part |
| `PZ_Lever.prefab` | SDK constrained rotation transformer with lever limits, or SDK's large two-hand lever sample |

Import Meta's example samples using Package Manager; use its Comprehensive Rig, Hand Grab, Throwing and
Sliders and Handles examples as source configurations. SDK samples remain governed by Meta's license.
Create prefab assets for any scene-only examples in a separate scratch scene, preserving their SDK
references. Do not open or edit Codex 1's production scenes to prepare sources.

Create **Patient Zero → SDK Prefab Sources** with the asset creation menu. Assign the five prefab roots,
each side's SDK hand/controller `IInteractorView` components (including aggregate groups), and the valve/
lever moving transforms and axes/limits. Every rig interactor must be assigned exactly once; the builder
rejects missing or duplicate gating. Select this asset and run **Patient Zero → Build SDK Prefabs From
Selected Sources**. It writes to `Assets/PatientZeroGenerated` and refuses to overwrite an existing
directory. Delete nothing automatically; use `SdkPrefabBuilder.Build(sources, newOutputPath)` for another
revision. The builder saves prefabs and never saves a scene.

The builder adds OpenXR tracking validity input, two tracking guards, held-object safety, a shared
cancellation hub, optional physical footstep sampling and prop noise/feedback. SDK grasp and throw
parameters stay unchanged. Confirm each SDK interactor group's active-state configuration respects
the gate: if that group manages child enablement, combine `SdkTrackingActiveState` with its existing
active state using SDK's AND active-state group. The component is supplied for this integration; do
not replace SDK controller/hand switching rules. All input switching and cancellation remain subject
to the headset checklist before treating the generated rig as ready.

Valve/lever feedback is a normalized readout of constrained SDK rotation. Match `MinDegrees`,
`MaxDegrees` and `LocalAxis` to the source transformer. The provided reader covers a constrained arc
within ±180°; a continuously revolving multi-turn valve needs a separate accumulation contract.

## Bind services and player

Place one `PZ_NoiseManager` and one `PZ_SpatialAudio` in Codex 1's integration scene. Bind the center-eye
transform to Audio.Listener; choose Occluders excluding player, zombies and props that contain sources.
Map UI sliders to `SetMasterVolume`/`SetComfortVolume`. The voice pool drops new sounds when full; it
never creates runtime voice objects after Awake. Audio uses unscaled cooldowns. Pause policy belongs
to Codex 1: call `StopAll()` on pause if desired, and cancel active encounters.

Bind every prop/door `NoiseEmitter.Manager`, rig `PlayerFootstepNoise.Manager`, zombie sensor manager
and distraction trigger manager. Bindings are explicit; there is no hidden singleton or scene scan.
For instances created at runtime, instantiate an inactive root, assign bindings, then activate it, or
use `ZombieHearingSensor.Bind`/`NoiseEncounterTrigger.Bind` after initialization.

- **Bottles/collisions:** set category Bottle or Collision; set mass/colliders/physics material in SDK
  source prefab. Impact speed threshold defaults to 0.8 m/s, emitter cooldown 0.3 s. Settled contacts
  below threshold do not publish noise.
- **Doors:** set category Door and `EmitOnCollision=false`; call `Emit()` from the door motion/open
  event. MechanismFeedback already publishes throttled changes for generated valves/levers.
- **Footsteps:** the generated rig's optional distance sampler emits at the head's floor-projected
  position every 0.65 m, at most once per 0.35 s. Set FloorReference to the actual floor origin and
  Grounded from the locomotion system. Call `ResetAfterTeleport()` on every teleport (including short
  teleports). If Codex 1 already has footstep events, disable this sampler and publish those events
  through a Footstep NoiseEmitter instead, to avoid duplicates.
- **Zombie sounds:** use left/right foot and mouth animation events through ZombieAnimationController;
  these positional audio cues do not emit hearing events, preventing zombie feedback loops.

## Zombie prefab

`PZ_ZombieAgent` contains navigation, FSM, hearing and animation adapters, with no art or Animator assigned.
Add the zombie model/Animator as a child. Disable root motion. Assign its feet and mouth, the shared
audio service and manager. Create the `State`/`Speed` Animator parameters described in the contracts.
The FSM owns attack timing; animation events must not independently deal damage.

Bake a NavMesh matching the agent type, radius and height (defaults 0.3 m / 1.8 m). Spawn on that NavMesh.
Set PlayerMask to physical player colliders and AttackBlockers to walls/glass/closed doors; implement
IDamageReceiver on the collider ancestry. Give the player's colliders a suitable height so they overlap
the zombie's attack query at one metre above the agent root. With empty masks the prefab cannot deal
damage. Set attack range/damage only after validating player scale and collision layers.

The manager budget is fixed at Awake. Configure it before activation. Unadmitted zombies stay stopped;
the scene's population controller should pool their roots and retry admission only when capacity frees.

## Author the five encounters

Package encounter prefabs contain two timed audio beats and callback slots, with no scene-specific actor
references. Replace diagnostic audio before shipping. Assign Listener/Audio, position safe anchors,
then bind these callbacks:

| Template | Scene integration |
| --- | --- |
| BehindGlass | Put anchor/zombie behind physical glass. First beat scratching; second growl and `SetActive(true)` on an authored silhouette. Include glass in AttackBlockers. No glass break toward the player. |
| ThrownDistraction | Bind NoiseEncounterTrigger to manager; Bottle sounds above threshold start it. The zombie's hearing independently investigates the actual bottle point. Use callbacks for an optional attention animation. |
| OperatingRoom | Author a doorway Environment noise emitter. First beat's OnBeat calls that emitter, causing hearing-based navigation into the doorway/room. Second beat growls; gate the sequence using the objective trigger. |
| DarkCorridor | First beat directional scratching at a corridor anchor; second breathing. For listener-relative breathing, set `PlayAudio=false` on the second beat and call `ListenerRelativeAudioCue.PlayBehind` with a 3 m distance. |
| InfectedReveal | `AllowIntenseEncounters` is false by default. Opt in from comfort settings; callbacks reveal the authored patient model with a lighting/audio lead-in at ≥2.5 m. No forced approach or camera rotation. |

Delays are relative to the previous beat. `Cancel()` stops future beats; previously invoked callbacks are
not reversed and already playing audio is not stopped globally. Bind explicit reset/cleanup callbacks
for reversible doors/models, and do not use `ResetSequence()` from inside a beat callback. Recheck
distance on every beat: unsafe player movement cancels the sequence. Pool encounter instances by
disabling them, then explicitly reset for replay. Cancellation does not assume ownership of unrelated audio.

Run the validation and performance procedures before marking these integrations complete.
