# PATIENT ZERO 1.0.0 — install, controls and play

Release files are in `Game/Builds/Release/` (not in git; rebuild them with the commands at the end).
`SHA256SUMS.txt` and `manifest.json` in the same folder list each file's checksum and the commit it was built from.
What was tested, and how: `docs/TEST_REPORT.md`. What is not done: `docs/RELEASE_NOTES.md`, "Known limitations".

| Platform | File | Status |
|---|---|---|
| Windows 10/11 x64 | `PatientZero-1.0.0-windows-x64.zip` | release candidate |
| Android phone (Android 10+, arm64) | `PatientZero-1.0.0-android-phone.apk` | built; see the test report for what ran on a device |
| Meta Quest 3 / 3S | `PatientZero-1.0.0-quest-dev.apk` | development build; never run on a headset |
| iPad | none | BLOCKED: see "iPad" below |

## The game

You wake strapped to a hospital bed with two minutes on the clock. Get out of the room (Act 1), restore the power
(Act 2), cross the ward in the dark by sound (Act 3) and pull the emergency release (Act 4). Zombies hunt by
sound: footsteps, thrown objects and slammed doors draw them; a thrown object is also how you lure them away.
If one catches you, the act restarts. The 02:00 countdown sets the mood; nothing fails when it reaches zero.

## Windows

1. Unzip `PatientZero-1.0.0-windows-x64.zip` anywhere (for example `Documents\Games`). Keep the folder together:
   `PatientZero.exe` needs `PatientZero_Data`, `UnityPlayer.dll` and the other files next to it.
2. Double-click `PatientZero.exe`. Windows SmartScreen may say "Windows protected your PC" because the file is not
   code-signed: **More info → Run anyway**.
3. The game opens full screen at your desktop resolution on the main menu. Click **New game** (or press Enter).
4. **Alt+Enter** switches between full screen and a window. **Quit** is on the main menu, the pause menu and the
   results screen.

Settings are saved per Windows user (`HKEY_CURRENT_USER\Software\Build with SK\PATIENT ZERO`); the log of the last
run is `%USERPROFILE%\AppData\LocalLow\Build with SK\PATIENT ZERO\Player.log`. To uninstall, delete the folder.

### Keyboard and mouse

| Input | Action |
|---|---|
| W A S D | move |
| move the mouse | look around (the pointer is hidden while you play; Esc brings it back) |
| left mouse | grab what is under the centre dot (+), from about one step away; hold and drag the mouse to work it: drag any way to pull a plank, drag sideways to turn the valve, drag down and keep holding to pull the release |
| left mouse release | let go |
| Q | throw what you hold (makes a noise where it lands: use it to lure zombies) |
| F | flashlight on/off (it dies when the power surges in Act 3, and works again once the lights are back) |
| T | jump to the next teleport point of the current act |
| Esc | pause / resume |
| Enter | New game on the main menu |

Menus are clicked with the mouse. Keys are fixed in 1.0.0 (no rebinding screen).

## Android phone

1. Copy `PatientZero-1.0.0-android-phone.apk` to the phone (USB, cloud drive or `adb`).
2. Open it on the phone. Android asks to allow installs from that source the first time: **Settings → Allow from this
   source**, then **Install**. Play Protect may warn about an unknown developer: **Install anyway**.
   With a USB cable and USB debugging on: `adb install -r PatientZero-1.0.0-android-phone.apk`.
3. Open **PATIENT ZERO**. The game runs in landscape.

It is signed with the Android debug key, which is fine for sideloading but not for Google Play (that needs the
owner's own upload key; see Known limitations). The phone app (`com.buildwithsk.patientzero.mobile`) installs next to
the Quest app without replacing it.

### Touch

| Touch | Action |
|---|---|
| left 40% of the screen, drag | move (a stick appears under your thumb) |
| right side, drag | look around |
| right side, quick tap | grab what is under the centre dot; tap again to let go |
| drag on the right while holding | work the held puzzle part (pull, turn) |
| GRAB (hold) | grab while the button is held |
| THROW | throw what you hold |
| LIGHT | flashlight on/off |
| JUMP | jump to the next teleport point |
| II | pause |

Menus are tapped. The controls stay inside the screen's safe area (notches and rounded corners).

## iPad

BLOCKED in this release. The iPad uses the same touch controls as the phone, and the project has the build command
(`python tools/unity/unity_batch.py build-ipad`, which writes an Xcode project), but:

1. Unity's **iOS Build Support** module is not installed on the build PC, so no Xcode project was generated tonight;
2. turning an Xcode project into an app on an iPad needs a **Mac with Xcode** and the owner's **Apple developer team**
   for signing. Neither exists here, and none was invented.

With the module installed: run `build-ipad`, copy `Game/Builds/iOS/PatientZero-Xcode` to a Mac, open
`Unity-iPhone.xcodeproj`, choose your team under **Signing & Capabilities**, connect the iPad and press **Run**.

## Meta Quest 3 / 3S (development build)

The VR game is unchanged: hands or controllers, teleport, the hand menu. The APK is a development build signed with
the debug key, and it has **not been run on a headset**.

One-time headset setup (owner; needs your Meta account):

1. Meta Horizon phone app: **Devices → your headset → Headset settings → Developer mode → On**.
2. Restart the headset, connect it with a USB-C data cable, accept **Allow USB debugging**.
3. `adb devices` must list the headset as `device`.

`adb` ships with Unity: `C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.

```
adb install -r PatientZero-1.0.0-quest-dev.apk
adb shell monkey -p com.buildwithsk.patientzero 1
```

Or in the headset: **Library → Unknown Sources → PATIENT ZERO**. Logs: `adb logcat -s Unity`.

## Rebuilding (developers)

Unity 6000.3.27f1 with Android Build Support; keep Unity Hub open and the editor closed.

```
python tools/unity/unity_batch.py release
python tools/release/run_probe.py --rounds 3
python tools/release/package_release.py
```

`release` runs: open, sample import, desktop setup, scene build, EditMode and PlayMode tests, then the Windows, phone and
Quest builds and the Quest validation. `run_probe.py` plays the built Windows game unattended (`-pz-autoplay`).

Playing inside the Unity Editor: **PatientZero → Set Up And Play** the first time, then **PatientZero → Play**. Other
modes are under **PatientZero → Desktop Simulation** (keyboard and mouse, touch layout, XR Interaction Simulator, headset
over Link).
