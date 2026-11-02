# PATIENT ZERO 1.0.0 — release notes

Date: 2026-10-30 · Branch: `integration/round2` (draft PR #5) · Unity 6000.3.27f1 · Built and tested by Claude Code
(release authority under the owner's final-release mission). Exact commit, file sizes and SHA-256 checksums:
`Game/Builds/Release/manifest.json` and `SHA256SUMS.txt`. Install and controls: `docs/PLAY_AND_INSTALL.md`.
Evidence: `docs/TEST_REPORT.md`.

## What the game is

A horror escape down three floors of a hospital: two minutes in the ward, then the laboratories and the lobby.
Six acts with one rising threat:

| Act | Time | Goal | Danger |
|---|---|---|---|
| 1 Awakening | 00:00–00:30 | pull the planks off the door | a zombie at the door window (scripted; it cannot catch you) |
| 2 Machinery | 00:30–01:00 | vent the pressure valve, plug the cable in | a zombie investigates noise |
| 3 Darkness | 01:00–01:30 | the surge kills every light (and your flashlight); follow the chirp to the exit | zombies hunt by sound |
| 4 Exit | 01:30–02:00 | pull and hold the emergency release, then take the stairs down | emergency red |
| 5 Laboratory (Floor 2) | no clock | carry the spare fuse from Lab A to the fuse box, pull the stair door override | the fuse sparks and wakes a researcher in Lab B |
| 6 Lobby (ground floor) | no clock | prime the generator in Security, pull the lockdown release by the main doors | a guard patrols the lobby; a porter sleeps behind reception; the reveal |

Win: the main doors open, and the ending reveals that you were Patient Zero ("YOU WERE THE CARRIER"), then Results
(Play again, Credits, Quit). Loss: a zombie catches you ("YOU WERE CAUGHT"), and the act restarts. Throwing objects
makes noise where they land, which lures zombies away. The countdown is atmosphere: nothing fails at 00:00.

## Update 2026-10-31: art pass

* **Animated zombies**: every zombie is now the CC0 "Animated Zombie" by Quaternius, idling, walking and running at the
  speed the AI moves it, instead of the box mannequin.
* **Textured hospital**: terrazzo floor tiles, two-tone painted blockwork with a chair rail, ceiling tiles, scratched
  metal, curtains and wooden planks, all generated in the project with normal maps and sized in real metres on every
  surface.
* **Post-processing on flat screens** (Windows, phone): bloom, ACES tone mapping, colour grading, a vignette and film
  grain, with a little detail kept in the darkest areas. It switches off whenever a headset is active, so VR renders
  without it, as before.

## Update 2026-10-31: three floors

* **Two new floors** after the ward, reached by the stairwell behind the emergency door (the screen fades as you go
  down): Floor 2, the laboratories, and the ground floor, the main lobby. New puzzles (a fuse you carry, a stair door
  override, a generator valve, the lockdown release), three new zombies, new signs, furniture, lights and sound.
  The ending now plays at the lobby's main doors.
* The top-left clock counts down in the ward and shows the floor below it ("FLOOR 2 · LABS", "GROUND FLOOR").
* **Fix (Windows):** a carried plug or fuse now snaps into its socket when it is held near it; before, it had to be
  within 6 cm at an exact angle, which the mouse could not do.
* **Fix:** long objective hints were cut off after two lines; they now shrink to fit. The clock moved up beside the title
  so it no longer covers the hint.

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
* Art is original geometry with textures generated in the project, procedural audio, and one third-party model: the
  CC0 Quaternius animated zombie. No paid assets. The zombie model has never been seen in a headset (NOT TESTED in VR).
* **Floors 2 and 1 (2026-10-31)** have been played through only by the automated tests and the release probe, never by a
  person yet: how often the lobby guard or the lab researcher catches a real player is not tuned. In VR they are NOT TESTED.
