# Non-VR touch adapter: Android phone and iPad

Implemented in seat B ownership; Unity/device validation is pending. This adapter supplies standard
Input System keyboard/mouse state to the existing non-VR player and interaction adapters. It does not
implement another movement controller, puzzle system, game flow or shared Core interface.

## Profile contract for Claude

Keep phone/tablet, PC and Quest build profiles separate. No ProjectSettings, input asset, player source
or owner-local Unity file is changed by this delivery.

| Profile | Defines | Required configuration |
| --- | --- | --- |
| Android phone / iPad | `PZ_MOBILE_TOUCH;PZ_MOBILE_NON_XR;PZ_MOBILE_SIMULATOR_OFF;PZ_DESKTOP_MOUSE` | XR startup/loader off, XRI simulator auto-instantiation off, dynamic Input System updates, existing non-VR consumers enabled |
| PC | existing `PZ_DESKTOP_MOUSE`; optional `PZ_NON_VR_HEADLAMP` | existing keyboard/mouse setup; disable XR simulator when using this driver |
| Quest | none of the mobile/non-VR defines | retain approved OpenXR/controller/hand configuration |

The simulator-off define is an assertion that TD actually disabled simulator auto-instantiation;
it does not silently rewrite XRI settings. The adapter also detects standard `XRSimulated*` device
layouts and refuses to publish input while a simulator, XR loader, XR startup or headset is active.
If XR becomes active later it disables itself, releases keys and disposes only its own devices.
Input System must process dynamic updates. The mobile component is not installed without all four
phone defines; the PC headlamp has its own explicit opt-in. No mobile HUD/devices are added to a
normal PC/Quest build. Editor touch preview uses the same opt-in and requires the simulator off.

TD must integrate the current A and B deliveries into the canonical game before generating scenes.
The current main still has no A player/game-flow implementation. The audited consumers are
`codex/gameplay` `cefbd3d` and `integration/round2` `d4de1a1`. Do not merge main directly from B.

## Controls and the existing consumers

| Touch | Standard input | Behavior |
| --- | --- | --- |
| Left joystick | WASD, eight directions and dead zone | Existing smooth movement when enabled in settings. Default teleport mode instead maps a forward release to one T step |
| STEP | T | Next approved anchor, subject to A's existing act/distance gate |
| Right drag | Mouse delta + RMB | User-controlled look, with bounded relative deltas and no initial absolute-position jump |
| Tap right pad / HOLD | Latched LMB | Existing center-view grab/selection ray; RELEASE releases the same selection |
| Drag while holding | Mouse delta + LMB, without RMB | Existing constrained puzzle motion: up for plank, sideways for valve, down then hold for lever |
| AIM OBJECT | Toggle RMB while still holding LMB | Turn while holding a bottle or cable; toggle back to puzzle mode for constrained manipulation |
| THROW | Q with LMB retained for this frame; release next frame | Keeps DesktopInteraction from dropping the prop before its throw branch runs |
| LIGHT | F | B's existing-fixture headlamp in Acts 1–2; disabled at the surge/Blackout |
| PAUSE / RESUME | Escape | Existing GameFlow pause/resume, including an ending pause |
| START | Enter | Existing calibration confirmation |
| RESTART on Complete | Core StateRequested(Playing) | Existing full replay path |
| RESTART while paused | R, optional | Enabled only after TD reviews/applies the compatibility patch and defines `PZ_MOBILE_RESTART` |

The held-object API does not currently expose selection type through Core. Aim/puzzle mode is an
explicit player choice, not a claim that B can inspect A's private selection. HUD hints use approved
ActStarted information. The same existing A ray selects a prop or puzzle. Aiming at a socket while
holding a cable uses AIM OBJECT; no parallel socket or grab logic is introduced.

Default settings still decide teleport versus smooth movement. The joystick becomes a forward-step
control in TeleportOnly; the STEP button also works. TD may select smooth movement through the existing
settings provider for the phone profile; B does not forge SettingsChanged or alter saved preferences.

## Safety, lifecycle and UI

- Ten fixed pointer slots, independent movement/look fingers, captured button roles, tap-vs-drag/hold
  separation and no action on canceled/outside button releases.
- Landscape only, safe-area clipping on phone/iPad, runtime landscape autorotation on mobile, retained
  Canvas scaling and a moving joystick thumb. A transparent look graphic avoids a large blended panel.
- HUD graphics intercept their physical touch hits; the router reads the Touchscreen independently.
  Virtual pointer position stays at the center for the existing first-person interaction ray.
- Focus loss, pause, resize, act restore and screen fades release gameplay input. Paused resume remains
  available even under a fade. No touch is converted to forced camera motion.
- Neutral input and device disposal restore prior hardware keyboard/mouse. No XR action asset or simulator
  is enabled, and no global time scale, audio listener pause or game-state authority is duplicated.
- A state-neutral standard device writer is shared with a new native mapping/lifetime fixture. The native
  test is source-only until Unity runs it; portable tests alone do not verify Input System scheduling.

## Headlamp and performance

`NonVrHeadlampInput` reads F only in the configured mobile or PC profile. LightingController remains the
sole light writer. Fixture 0's emitter is a child separate from its ceiling housing, so reusing it as a
head-following spot does not drag ceiling geometry in front of the camera. Its lens stays with the ceiling.
The emitter restores its original point-light pose when the feature is disabled. No new light source is
created: six fixtures total. The lamp fades over at least 0.15 s, toggles are rate-limited, and it is off
for Surge, Blackout, EmergencyRed and ExitDaylight. VR held-object flashlight integration remains separate.

Phone scene generation selects a 16-active-voice / zero-HRTF profile; the default remains 24/12.
The serialized cue-player limits permit 8–24 active voices, retain protected UI/speech, and never remove
gameplay hearing metadata. The source pool remains fixed and bounded. Original synthesized PCM payload
is about 20.46 MiB; actual Unity/native memory and combined output peaks still need measurement.
No render-pipeline, texture, Android/iOS signing or global quality setting is rewritten by B.

## Shared-source patch for Claude review

`../Validation/mobile-desktop-compatibility.patch` is a proposal, not an edit to A's source. It:

1. Adds the guarded R shortcut to existing GameFlow.RestartRun while paused.
2. Marks desktop look orientation dirty on ActStarted, then synchronizes its cached yaw/pitch from the
   actual restored rig before the next look delta. Source audit found that a restored act can face 90°
   while DesktopLocomotion still caches an old yaw, causing the first drag to snap the camera.

The complete patch was checked with temporary indexes against **both** audited branches. Review/apply
in TD's safe integration checkout, then run A's restore/pause tests and the new native mapping fixture.
Keep `PZ_MOBILE_RESTART` off until the shortcut is integrated; the pause restart button is hidden otherwise.
Do not merely apply a stale patch if the target changed. The existing native compilation follow-up patches
in DESKTOP_PLAYTEST.md remain subject to TD review; this delivery does not overwrite their source files.

## Required validation and release work

1. Set the separate profile configuration above, resolve the pinned packages, regenerate Bootstrap/Ward.
   HospitalValidation now checks the headlamp binding and independent emitter/lens setup.
2. Compile with default, PC and mobile/restart defines; run EditMode, PlayMode and saved-scene validation.
   Exercise calibration, all physical puzzles, bottle throw, cable aiming, pause during fade, act restore,
   completion and full replay using actual touch input. Check mobile HUD/world-UI interaction.
3. On Android and iPad: test two simultaneous fingers, cancellations, home/app resume, rotation, notches,
   tablet scaling, sustained look/movement and sound direction. Editor syntax checks cannot prove these.
4. Claude owns Android/iOS export, signing and release builds; iPad needs the appropriate macOS/Xcode/device
   toolchain. No owner Windows build was started. Profile each actual target before claiming FPS, latency,
   mixed audio peaks, memory or thermal behavior. Quest remains a separate unchanged XR profile.

Current result: 180 portable tests pass (including mobile, AI, audio and fixture regressions), static/package
checks pass; one pre-existing TD-owned Windows-path test fails on Linux. Current cloud Unity is absent.
See `../FINAL_MULTIPLATFORM_HANDOFF.md` for actual logs, remaining dependencies and interaction audit.
