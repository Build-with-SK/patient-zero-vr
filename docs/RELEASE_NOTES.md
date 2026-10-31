# PATIENT ZERO 1.0.0 — release notes

Date: 2026-10-30 · Branch: `integration/round2` (draft PR #5) · Unity 6000.3.27f1 · Built and tested by Claude Code
(release authority under the owner's final-release mission). Exact commit, file sizes and SHA-256 checksums:
`Game/Builds/Release/manifest.json` and `SHA256SUMS.txt`. Install and controls: `docs/PLAY_AND_INSTALL.md`.
Evidence: `docs/TEST_REPORT.md`.

## What the game is

A two-minute horror escape in a hospital ward. Four acts with one rising threat:

| Act | Time | Goal | Danger |
|---|---|---|---|
| 1 Awakening | 00:00–00:30 | pull the planks off the door | a zombie at the door window (scripted; it cannot catch you) |
| 2 Machinery | 00:30–01:00 | vent the pressure valve, plug the cable in | a zombie investigates noise |
| 3 Darkness | 01:00–01:30 | the surge kills every light (and your flashlight); follow the chirp to the exit | zombies hunt by sound |
| 4 Exit | 01:30–02:00 | pull and hold the emergency release | emergency red; the reveal |

Win: the release opens, and the ending reveals that you were Patient Zero ("YOU WERE THE CARRIER"), then Results
(Play again, Credits, Quit). Loss: a zombie catches you ("YOU WERE CAUGHT"), and the act restarts. Throwing objects
makes noise where they land, which lures zombies away. The countdown is atmosphere: nothing fails at 00:00.

## New in 1.0.0 (since the round-2 integration build)

* **Standalone Windows build** (`PatientZero.exe`): no Unity Editor, no XR simulator, no debug menu. Opens full screen at
  the desktop resolution on the main menu; Alt+Enter for a window.
* **Main menu** on every flat-screen start: New game, Credits, Quit, with the comfort and brightness settings below.
  Pause menu: Resume, Restart act, Restart run, Comfort/settings, Quit.
* **Flashlight** (F on Windows, LIGHT on touch): useful in Acts 1 and 2, dies in the Act 3 surge, back with the lights.
  Shared contract v1.2 (`HandheldLightChanged`), the light itself owned by the atmosphere system.
* **Objectives on screen**: each act opens with its goal; the hint ladder still steps in if you are stuck.
* **"You were caught" screen** before the act restarts; a centre dot (+) shows what you will grab.
* **Touch controls** for phones and iPad: move stick, look, tap to grab, GRAB/THROW/LIGHT/JUMP/II buttons, safe-area aware.
* **Android phone build**: its own app id (`com.buildwithsk.patientzero.mobile`), landscape, no VR, Android 10+.
* **Editor fixes**: the XR Project Validation window no longer opens on every Play (XRI interaction layer 31 is named
  "Teleport"); after an Android build, PatientZero → Play switches the Editor back to Windows so keyboard and mouse work.
* **Release QA tool**: `PatientZero.exe -pz-autoplay N` plays N complete rounds unattended and exits 0/1.

## Known limitations

* **Quest 3/3S**: development APK only, never run on a headset (no device was connected); no frame-rate claim.
  No flashlight in VR yet (the hand-held penlight pickup is not built).
* **iPad**: BLOCKED. iOS Build Support is not installed on the build PC, and an installable app needs a Mac with Xcode
  and the owner's Apple developer team. The build command exists (`unity_batch.py build-ipad`).
* **Android phone**: signed with the debug key (sideloading only; Google Play needs the owner's upload key).
* **Windows**: the exe is not code-signed, so SmartScreen warns on first launch. Keys cannot be rebound in 1.0.0.
  Mouse look is free while playing (the cursor is captured); menus and pause release it.
* Art is original greybox geometry and procedural audio; no paid or third-party art assets.
