# Handoff — 2026-10-30 ~20:10 UK (Claude, usage limit reached)

Branch `integration/round2` (draft PR #5). **Nothing from this session is committed yet.** Every change listed below is in
the working tree only.

## Done and verified on the built exe

* Mouse look fixed: the cursor is captured while Playing, so the mouse turns the view with no button held. It is freed
  in menus and pause, and a gripped puzzle part takes the mouse instead. The XR TrackedPoseDriver is disabled on flat
  screens. Head bob added. Files: `Player/Runtime/FlatInput.cs`, `DesktopLocomotion.cs`,
  `Interaction/Runtime/DesktopInteraction.cs`.
* Windows build PASS (0 errors, 0 warnings, 120 MB). `tools/release/run_probe.py --rounds 3 --shots` PASS: 3/3 rounds
  complete, 39.1 s each, exit 0. Screenshots are in `Game/Logs/PatientZero/shots/`.
* Flashlight via contract v1.2 `HandheldLightChanged` (E-17), release probe, `ReleaseBuild.cs`, `package_release.py`,
  and the docs listed in the previous log. All uncommitted.

## Not done (the owner asked for "final game graphics and animations")

1. **Text through walls (visible bug)**: `HospitalBuildSteps.Label` uses `font.material` (GUI/Text Shader, ZTest
   Always), so every wall sign draws on top of everything. Fix: a depth-tested unlit text shader, plus a runtime
   component that copies `font.material.mainTexture` and follows `Font.textureRebuilt`.
2. **Art pass**: planned as `Integration/Editor/ArtPassStep.cs` at `[SceneBuildStep(BuildScene.Ward, 970)]`, recorded
   as exception E-18. It would generate world-scale-UV cube meshes plus textures (floor tiles, two-tone wall, ceiling
   tiles, metal) and assign its own materials instead of editing seat B's. It must not mention `Light` or
   `RenderSettings` (check_rules light-owner).
3. **Post-processing**: `PZ_URP_Renderer.asset` has `postProcessData: {fileID: 0}`, so post-processing is off. Assign
   `Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset` in
   `QuestBuild.ConfigureUrp`. Add a global Volume (bloom, vignette, grain, colour adjustments) that is enabled only
   when `!XRSettings.isDeviceActive`, and turn HDR on only for the Windows build (restore it in `finally`).
4. **Zombie model**: the owner approved downloading the CC0 Quaternius "Animated Zombie". The GLB was downloaded to
   the session scratchpad (not in the repo). Source:
   `https://static.poly.pizza/c4002f69-6979-42e8-ad6e-2f4e14fc3a9d.glb`, 981,672 bytes, glTF 2.0.
   * Clips: Idle, Walk, Run, Run_Arms, Punch, Death, HitReact, Crawl.
   * Two skinned meshes, Zombie and Eyelid. The model is 1.36 m tall, so scale it to about 1.75 m.
   * Importer: `com.unity.cloud.gltfast` 6.20.0 from packages.unity.com (needs Unity 6000.0+; its dependencies are
     already met at burst 1.8.31, collections 2.7.1 and mathematics 1.3.3). Add it to `Packages/manifest.json` and
     `tools/unity/package_policy.json`.
   * Wiring: set `ZombieAnimationController.Animator`, with float `Speed` (blend Idle 0 / Walk .85 / Run 1.5) and int
     `AlertLevel`. Set `Visual` to the Zombie skinned mesh and mirror its enabled state onto Eyelid. Copy the clips to
     looping `.anim` assets.
5. Remove the Standalone OpenXR loader for the Windows build. It logs `XR_ERROR_RUNTIME_UNAVAILABLE` in Player.log
   (harmless, but noisy).
6. Then: rebuild, run the probe with `--shots`, and playtest by hand. Update `docs/PLAY_AND_INSTALL.md`, which still
   says to hold the right mouse button. After that: the phone APK, the Quest APK and its validation, EditMode/PlayMode
   tests, `package_release.py`, the test report, then commit and push to `integration/round2`.

## Next commands

```
python tools/unity/unity_batch.py build-windows
python tools/release/run_probe.py --rounds 3 --shots
```

Unity Hub must be running for batch mode. Commit as `Build-with-SK`. Run the privacy scan before committing.
