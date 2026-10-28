# Codex A integration handoff

This is the canonical repository implementation against Core v1, not the earlier independent prototype. A1 is portable-tested. Runtime/Editor/PlayMode sources are explicitly UNVERIFIED IN EDITOR.

## Integration prerequisites (Claude-owned)
- Pin exact Unity 6000.3 patch in Game/ProjectSettings/ProjectVersion.txt, manifest and package lock. Installed locally: 6000.3.26f1 (Windows only); 6000.6.6f1 is not the specified LTS line. Do not substitute editors silently.
- Provide the scene composer and build settings for Bootstrap and Ward. No handwritten assets or metadata are provided by seat A.
- Restore approved URP/OpenXR/Meta OpenXR/XRI >=3.5/XR Hands >=1.8/Input System/UI dependencies. Import the Unity XRI Starter Assets/Hands Interaction Demo rig containing XRInputModalityManager with both hand and controller groups. PlayerBuildStep deliberately fails if this rig/palm transforms are missing; it does not pretend an incomplete rig works.
- Approve and merge the Act1 director-to-door release signal requested in CODEX_A_LOG.md. Door is deliberately locked pending that contract. ReleaseIsolationGate is a local component API, not a substitute cross-seat signal.

## Build steps
- Ward 310: InteractionBuildStep (layout-tagged puzzles/anchors, physics props/cart, cable, doors, objective publisher, beacon detector).
- Bootstrap 400: FlowBuildStep (settings, flow).
- Bootstrap 410: PlayerBuildStep (imported rig, query/tracking/modality bridge, fader, reveal overlays, desktop locomotion, comfort settings).
- Bootstrap 420: DesktopBuildStep (same mechanics via mouse).
- Bootstrap 430: UiBuildStep (world-space calibration, pause/results/credits, captions, settings).
- Unity-created materials are written by these builders into the area's Generated folder; only Claude commits generated assets/metadata.

## Runtime protocol
A writes only StateChanged, ObjectiveProgress, ObjectiveCompleted, SettingsChanged, TrackingChanged. B owns ActStarted, acts, beats, countdown source and ending state requests. Countdown is a 120s UI target: zero never fails the player. No A-owned timer advances acts. Noise goes through Cues; A never emits GameEvents.Noise or owns AudioSource/Light/RenderSettings.

SettingsProvider registers ISettingsProvider. PlayerRig registers IPlayerQuery. ScreenFader registers IScreenFader. Ward puzzles subscribe/unsubscribe per enable lifecycle. Flow reloads Ward additively under fade for catches/restarts; pause freezes scaled time and listener audio; focus regain never auto-resumes. Hand loss does NOT auto-pause (approved design); after 1.5s props drop with zero velocity and UI shows tracking loss. Mode switch/focus loss cancel immediately. Both modalities use the imported XRI interactors and the same interactable component APIs.

## Desktop controls (after actual scene generation)
Enter confirms calibration. T advances to the next available current-act anchor (3m maximum). RMB looks. Optional smooth locomotion must be enabled in comfort settings before WASD moves. LMB selects a physical prop or constrained puzzle; drag up to pull planks, drag horizontally to turn valve, drag down and hold the lever. Release LMB to drop; Q throws held props. Bring plug to socket in the correct orientation. Esc toggles pause. Buttons are shared XRI world-space UI and desktop mouse input via XRUIInputModule.

## Validation still required
Run portable harness/static rules first; then pin/restore project, compile Unity, generate scenes, validate layout/dependencies, run EditMode and Flow.PlayModeTests, and inspect UI/render output. PlayMode tests include autoplay without bypassing the director gate, restart x10 per-channel subscriber baseline, focus/pause during load and required-plug recovery. Missing generated scenes explicitly skip these tests; skips are NOT passes. No playable canonical scene, Unity compile, APK or headset result is claimed yet.

Review and wire XRI teleport provider references, input action assets, hand/controller tracked pose transforms, hand gesture palm orientation and comfort vignette, and prove reach/one-handed interaction on the headset. Verify screen-space camera fader in stereo. Primitive vein overlays and medical props are placeholders, not imported licensed production assets. Flashlight/bed barricade from the older broad request have no objectives/cue/layout IDs in approved v1; no new IDs or lighting ownership have been invented. Follow GAME_DESIGN plank-based isolation gate.

API references consulted: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.5/api/UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable.html and XRInputModalityManager documentation at the same version; Unity XR Hands 1.8 XRHandSubsystem API. Actual pinned package compilation remains pending.
