# Release Checklist

**Current verdict (2026-10-28): RELEASE CANDIDATE NOT APPROVED.** No build exists. The gates below are unchecked because none of them has been met, not because they were skipped.

A release candidate is approved only when every **critical** box is checked and each check links to evidence in `TEST_REPORT.md` or `PERFORMANCE_REPORT.md`. A box can't be ticked because "it worked in the Editor" when the item says Device.

## Gate 0: Project exists (owner actions, `ARCHITECTURE.md` section 11)

- [ ] Unity 6.3 LTS with Android Build Support installed; owner signed in
- [ ] `Game/` opened as a project; packages pinned; `Packages/` and `ProjectSettings/` committed by Claude
- [ ] Headset in developer mode; `adb devices` lists it; model recorded
- [ ] Meta XR Audio licence accepted, or the built-in fallback chosen
- [x] Contract v1 approved and `main` created (owner approval in chat, 2026-10-28)

## Gate 1: Branches reviewed and merged (Mission 4–5)

- [ ] Seat B: `READY FOR REVIEW` posted; logic harness and `check_rules.py --seat B` clean; 0 open BLOCKER/MAJOR findings
- [ ] Seat A: same for `--seat A`
- [ ] Merged in order: contracts → B → A → integration commit
- [ ] Unity compile clean (0 errors; warnings reviewed)
- [ ] Unity EditMode tests: all pass
- [ ] Scene composer: `Bootstrap.unity` and `Ward.unity` generated; validation passes (every layout id placed once, ≤ 5 cm; one implementer per service; no missing references)
- [ ] PlayMode: the autoplay bot reaches Complete
- [ ] PlayMode: 10 restarts; memory within ±5 % and every channel's subscriber count back to baseline
- [ ] PlayMode: pause during scene load can't strand the game

## Gate 2: Android development build

- [ ] Batchmode build succeeds: arm64, IL2CPP, Vulkan, multiview, OpenXR with Meta Quest feature
- [ ] The APK manifest contains the hand-tracking permission and feature, and no unwanted permissions (internet off unless needed)
- [ ] Installs and launches on the headset (`adb install`, app reaches Calibration). Evidence: log + screenshot

## Gate 3: Feature acceptance on device (critical)

- [ ] **F-01** Complete 120-second loop: 3 testers, first-run times recorded, the target is 100–150 s
- [ ] **F-02** Hand interactions: every objective with hands only, standing and seated
- [ ] **F-03** Controller fallback: every objective with controllers only; modality switch mid-act
- [ ] **F-04** Sound-reactive zombies: inside/outside radius behaviour observed; the Act 2 investigation every run
- [ ] **F-05** Spatial audio: blind beacon localisation within 30° (3 of 3 testers)
- [ ] **F-06** Functional ending: the reveal in both modalities; Play again works
- [ ] **F-07** Pause and restart: button, gesture and headset removal; restart act; full restart
- [ ] **F-08** Performance: every budget in `PERFORMANCE_REPORT.md` met, with captures attached
- [ ] **F-09** No critical blockers: 0 open BLOCKER findings; no soft-locks found in 10 runs

## Gate 4: Safety, comfort and accessibility (critical)

- [ ] No camera motion without player input; no camera shake (code review + device run)
- [ ] No luminance change above 3 Hz (flicker generator test + device capture review); Reduce flicker removes all flicker and spikes
- [ ] Reduced scares: 0 hard scares, no lunge, sight off (`DirectorCore` test + device run)
- [ ] Every caption marked ● appears; Sound cue visuals makes every objective possible without audio (one run with audio muted)
- [ ] Seated and one-handed full runs completed
- [ ] Brightness calibration makes Act 3 navigable on the target headset

## Gate 5: Legal and assets

- [ ] Every imported asset has a complete row in `ASSET_MANIFEST.md`, with a verified licence and date
- [ ] `docs/CREDITS.md` lists every attribution required; credits reachable in game
- [ ] No paid, NC, or unverified asset in the build
- [ ] No owner-private data (paths, accounts, keys) in the build or repository

## Sign-off

| Role | Name | Date | Decision |
|---|---|---|---|
| Technical Director | Claude | — | not approved |
| Owner | — | — | — |
