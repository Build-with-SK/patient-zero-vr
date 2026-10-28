# Shared contracts — Codex 1 ↔ Codex 2

**Status: proposed, not acknowledged by Codex 1.** The repository was empty when work started.
The user explicitly authorized starting independently while Codex 1 finishes and pushes its work.
There is no cross-agent messaging channel available in this task; this file is the handoff for agreement.

Codex 1 owns the Unity application, scenes, player health, objectives, lighting, locomotion and NavMesh
baking. Codex 2 owns `Packages/com.patientzero.xr`, `docs/xr`, the separate Unity test fixture and
portable validation tooling. Integration adds prefab instances to Codex 1's scenes; Codex 2 does not edit them.

| Interface | Contract |
| --- | --- |
| Namespaces | `PatientZero.Core` has no Unity dependency. Unity components use `PatientZero.XR`; optional SDK adapters use `PatientZero.XR.Meta`. |
| Coordinates | Unity world space, one unit = one metre. Noise positions describe the sound source, not a player's continuously updated position. |
| Time | `NoiseEvent.Timestamp` uses scaled `Time.timeAsDouble`. AI and noise pause with gameplay. Tracking uses unscaled time and continues to fail safely during a pause. |
| Noise | Immutable `NoiseEvent(Position, Intensity, Radius, Timestamp, Category, SourceId)`. Intensity is `(0,1]`; radius is positive metres. Categories: Bottle, Door, Footstep, Collision, Voice, Environment. Source IDs are stable per emitting component within a session. |
| Publication | Bind `NoiseEmitter.Manager` and call `Emit()`, or call `NoiseManager.Emit`. Main Unity thread only. The bus dispatches synchronously; callbacks must not throw. Nested publication is rejected. |
| Hearing | Linear 3D radius attenuation with a per-zombie threshold. Navigation arrival/attack approach uses ground distance; physical attacks still test colliders and blockers. Blind AI only pursues last-heard points. No scene search or player-transform tracking. Bottle/door/collision events investigate and distract; sufficiently loud footsteps/voices can escalate to chase. |
| Flood control | Emitter defaults to 0.3 s; manager defaults to 0.15 s per source. Invalid, future and >1 s old events are rejected. Source-capacity exhaustion drops events instead of evicting unexpired cooldowns. |
| Player damage | Codex 1 implements `IDamageReceiver.TryApplyDamage(float amount, Vector3 origin)` on an ancestor of the player's physical collider. Returns true when accepted. Damage happens once after windup, within physical range and with no configured wall blocker. |
| Collision masks | Codex 1 supplies Player, AttackBlockers and Occluders masks. Defaults are empty, so no damage/occlusion is accidentally enabled against arbitrary scene layers. Player colliders must be non-trigger colliders for this adapter. |
| AI budget | Manager admits 12 active zombie sensors by default, plus 16 general bus-listener slots. Extra zombies stop navigation. Pool/unpool roots or call `RetryAdmission()` after a slot frees; suspended zombies do not poll every frame. |
| Navigation | Codex 1 bakes a Humanoid NavMesh (agent type 0 for supplied prefab), spawns zombies on it and keeps doors/obstacles represented. Navigation uses heard points; failed/incomplete paths lead to search. |
| Animation | Animator parameters: integer `State` (Idle=0, Investigate=1, Search=2, Chase=3, Attack=4, Recover=5), float `Speed`. In-place animation, root motion disabled. Animation events call `FootLeft`, `FootRight`, `Growl`. |
| XR model | SDK hand/controller prefabs own pinch, grabbing, release, throw velocity and two-hand transformers. Custom code does not estimate hand joints or gesture poses. One source per side is enabled after 0.25 s stable tracking. Hands take priority when both validity inputs are true. |
| Tracking safety | Both guards invoke the hub to cancel held objects. This conservatively drops both sides' selected props on either source loss. Props suppress stale release velocity for 0.3 s. No automatic re-grab or camera recenter occurs. |
| Audio | Exactly one center-eye AudioListener. Spatial sources are mono, fully 3D, zero Doppler. Mixer routing: Master → Zombie/Ambience/Encounters. UI calls `SetMasterVolume` and `SetComfortVolume` with `[0,1]`. Master gain is applied once in sources. |
| Encounters | Codex 1 supplies actor/door/visual callbacks and scene anchors. `Begin()`/`TryBegin()` starts an authored sequence; `Cancel()` stops future beats; `ResetSequence()` explicitly permits replay. No camera motion. No mandatory close reveal; final reveal defaults to opt-out. |

Codex 1's integration review should confirm player health signature, layer choices, source ownership
for footsteps, and the SDK rig's input switching. Adapt these package boundaries if its existing code
uses different names; do not introduce a second player controller, camera rig or damage system.

The current branch begins with a root commit because the remote had no commits. If Codex 1 also creates
a separate initial history, cherry-pick the Codex 2 root commit onto its branch, resolving any shared
`.gitignore`/CI files deliberately. Do not merge unrelated histories blindly or reset either branch.
