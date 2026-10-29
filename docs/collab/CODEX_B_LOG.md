# Codex B log — AI, Audio, Atmosphere, Environment

Append-only; newest last. Identity: `businessteen` (`192131711+businessteen@users.noreply.github.com`).

## 2026-10-28 15:26 · Contract review v1 and recovery
Kind: CONTRACT REVIEW v1
What: ACK approved contract v1. No second Core, health system, Meta Interaction SDK, private cross-seat event bus, old encounter taxonomy or generated YAML will be migrated. Unity 6.3/XRI player ownership remains seat A. The approved ward layout (isolation, corridor, vestibule and ancillary rooms) takes precedence over the generic operating-theatre request; a theatre would require a layout/design change by Claude.
Evidence:
- Fetched all remote heads. `main`, `integration/xr-systems`, `claude/technical-director` = `73a3b67`; frozen `codex/xr-systems` = `72be6d8`, `Lavanya` = `87bb494`. `git merge-base origin/main origin/codex/xr-systems` returns no ancestor.
- Recovered original files to `/tmp/patientzero-recovery-72be6d8` from the preserved commit. Original portable suite: `26 passed; 0 failed; 0 skipped` (.NET SDK 8.0.427). Tracking-loss scenarios remain reference evidence for seat A, not a claim that current XRI tracking is implemented.
- `python tools/logic_tests/run_logic_tests.py` → `Logic tests: 43 passed, 0 failed.` No compiler warnings.
- Whole-commit cherry-picks are incompatible (FIX-B-1…12). Port reviewed algorithms at file level, crediting their source in each implementation commit. All frozen refs and files remain recoverable.
- GitHub API read returned HTTP 403; native Git read succeeds. An additive `api.github.com` network draft is being prepared for the explicitly requested PR; no credential values are inspected or requested.

## 2026-10-28 15:26 · Act 1 physical door gate
Kind: CONTRACT REQUEST
What: `Core/Contracts/Interfaces.cs`: add read-only `bool CanOpenAct1Door { get; }` to `IExperienceDirector`, or an equivalent approved director-to-seat-A gate event. Act 1 requires all planks removed, the window beat played and Z1 at least 4 m from the window before the door physically opens. Current Core exposes only CurrentAct/ExperienceTime/StartRun, so seat A cannot query that gate. B will gate its act advance internally but will not code against an unapproved interface addition.
Evidence: GAME_DESIGN §4 Act 1 gate versus current Interfaces.cs. No Core edits.

## 2026-10-28 15:26 · Cue fairness interpretation and ending ownership
Kind: PROGRESS
What: The cue-table rows list ZombieLunge and ZombieGlassTap at 8 m, while the mandatory tested fairness rule says every zombie vocal max is at least 9 m. B will conservatively use 9 m for these two cues and record this for Claude's review; ids and contracts are unchanged. The ending's white/black fade and title stay with seat A; B drives reveal amounts and state requests only through existing Core channels. Built-in Unity panning is the default until optional Meta XR Audio licensing is confirmed.
Evidence: GAME_DESIGN §§4,6; ARCHITECTURE ownership table. No runtime/editor/device claims.

## 2026-10-28 15:49 · B1 logic review checkpoint
Kind: READY FOR REVIEW 22d40025f0e680471396feb48971710f2fde8c8d
What: B1 is a separate reviewable implementation commit before B2 shells. Ported approved flood-control/hearing/AI guards, replaced the incompatible six-state/health model, implemented the full cue table, protected 24/12 voice budget, four-act director, lighting rules and derived room boundaries. Core and frozen branches are untouched. The original 26 tests were rerun independently; current contract tests are not represented as the same suite.
Evidence:
```
python tools/logic_tests/run_logic_tests.py
Logic tests: 91 passed, 0 failed.
python tools/review/check_rules.py --seat B --base origin/main --head integration/xr-systems
0 error(s), 0 warning(s).
```
No compiler warnings. Includes the Standard/Reduced golden run, catch/LOS/range/timeout cases, exactly-once cue coverage, HRTF voice cap, tracking-loss hint handling, and 10 minutes of flicker/pulse sampled at 1 ms. Not verified in Unity or on a headset. Proceeding with separately committed B2/B3 source under the user's full implementation request; review does not imply merge approval.

## 2026-10-28 16:27 · B2/B3 implementation and integration limits
Kind: PROGRESS
What:
- Continued after B1, porting the 10 Hz phased AI shell/ProfilerMarker, warmed voice pool, rate-limited occlusion and original synthesis formulas from `72be6d8` into owned folders. Current AI uses movement-based vision, radius/occlusion hearing, sniff/snarl telegraphs, path-failure/search recovery, continuous catch timers, vertical/LOS guards, restrained reach and Passive ending behavior. Player input is read only through IPlayerQuery.
- Added composer build steps for the approved six-room ward geometry, colliders, six doorway headers, medical dressing, chain, sign/monitor/text, sound-guidance strip, six shadowless fixtures, NavMeshSurface bake, four single-material/seven-bone mannequin prefabs, native mixer groups and cue-player/static-emitter bindings. No generated or binary asset files are committed. No operating theatre or unapproved coordinates are added.
- Added PlayMode sources for real cue-to-hearing boundary/telegraph, mono/rolloff configuration, missing and silent cues, generated-Ward routes and ten director lifetimes. All Unity-facing sources remain `UNVERIFIED IN EDITOR`.
- Added a tested local window pulse: a regression caught floating-point scheduling producing four edges within a second; sharing the flicker phase corrected it. The ordinary variation and combined two-fixture pulse tests each sample ten minutes at 1 ms. Host AI steady-state allocation test measures before assertions so NUnit's boxing is not counted as gameplay allocation.
- Integration guide: `Game/Assets/PatientZero/Environment/INTEGRATION.md`. It documents build orders, service/mask bindings, marker ownership, fallback audio, editor-only native mixer factory, tests and blockers.
- Physical Act 1 door gate still needs the Core response requested above. Scene integration must exclude player colliders from environment/door masks and include closed door leaves in collision/occlusion/carving. Seat A owns XR rig, XRI interaction prefabs and the tracking-safety port; frozen tracking sources/tests remain intact as its reference. Seat A also owns ending fades/title; B drives the infection reveal and state requests.
Evidence:
- Activated .NET SDK 8.0.427; `python tools/logic_tests/run_logic_tests.py` → `Logic tests: 100 passed, 0 failed.` No compiler warnings. Separate original suite remains 26/26 passing from recovery.
- Working-tree static review → `0 error(s), 0 warning(s).` Full branch diff review follows the implementation commit.
- C# 9 syntax parsing and internal asmdef reference/cycle checks pass. These are **not Unity API/type compilation**.
Not verified: Unity compile, scene/prefab/mixer generation, the six PlayMode tests, Android build, headphone localization, headset behavior or 72 Hz/device metrics. No Unity Editor/Hub/adb is available and Claude has not reported an integration compile. External asset upgrades and optional Meta XR Audio remain unimported/unlicensed per the manifest.
Preservation: frozen branches were not pushed, rewritten or removed. Legacy untracked `Tests/Portable/bin` and `obj` outputs are left intact and are not staged. Main/Core/seat-A files are unchanged.

## 2026-10-28 16:39 · B2/B3 source review checkpoint
Kind: READY FOR REVIEW 4e1fda2dd19d12f850c5f9b42db5ffbb5bddfef1
What: B2 runtime/build sources and B3 test sources are committed separately from B1. Ready for source review, not a Unity/device readiness claim. Post-review hardening makes generated folders through AssetDatabase, saves regenerated mesh/material data natively, and ramps the daylight fog/ambient colour instead of switching fog to white instantly.
Evidence:
```
python tools/logic_tests/run_logic_tests.py
Logic tests: 100 passed, 0 failed.
python tools/review/check_rules.py --seat B --base origin/main --head integration/xr-systems
0 error(s), 0 warning(s).
```
No compiler warnings. All project asmdefs have unique names, internal references resolve, and the reference graph has no cycles. C# 9 syntax scan: 0 syntax errors; **not Unity API/type compilation**. Six PlayMode tests are written, none executed. Main still `73a3b67`; frozen refs still `72be6d8` / `87bb494`.

## 2026-10-28 16:39 · ACK proposed gate contract v1.1
Kind: CONTRACT REVIEW v1.1
What: ACK Claude's proposed `GateId` / `GateReleased` / `IExperienceDirector.IsGateReleased` approach reported on `claude/technical-director` at `a220834`. It addresses the same Act 1 gate request from B and seat A. It is accepted in principle by B; B will not implement against that interface until the owner approves it and it lands on main. Current approved Core is unchanged. Once merged, map B's tested CanOpenAct1Door predicate to the approved gate id and publish exactly once; no A namespace/component dependency is needed.
Evidence: read the TD log at `a220834` and kept source based on approved main `73a3b67`. Operating-room amendment D-4 likewise remains unapproved and unimplemented.

## 2026-10-28 16:22 · Delivery and PR blocker
Kind: PROGRESS
What: Pushed `integration/xr-systems` through `37e88357541802f6305ef9b7ae7ba3bdbc19721e`; no merge and no frozen/main push. Attempted a draft PR targeting main with `gh pr create --repo Build-with-SK/patient-zero-vr --base main --head integration/xr-systems --draft --title "Seat B: recover XR work and implement approved ward systems" --body-file /tmp/patientzero-xr-pr.md`.
Evidence: `Post "https://api.github.com/graphql": Forbidden` (exit 1). Native Git still succeeds. `git ls-remote origin 'refs/pull/*/head' 'refs/pull/*/merge'` lists only PR #1 (seat A) and #2 (TD); neither is this branch. **No B PR was created and there is no B PR URL to report.**
Action needed: review/save the already-persisted additive `api.github.com` environment-network draft, then publish the environment as required by the configuration tool. This saved draft is not a runtime update. Retry the actual PR operation after the access change; existing injected authentication must be tried before requesting any new credential. No secret values belong in chat or source.
PR review summary: recover `87bb494`/`72be6d8`; B1 `22d4002`, B2 `4e1fda2`, native-generation/transition hardening `37e8835`; original suite 26 passed; current harness 100 passed, 0 failed; full seat-B branch checker 0 errors/0 warnings. Native Unity compile/generation, six PlayMode tests and device/headphone/performance checks remain unverified. The v1.1 physical-door gate is acknowledged but not coded against before approval/main. Full integration notes are in Environment/INTEGRATION.md.

Timestamp correction (append-only log): earlier manually entered checkpoint headings `15:49`, `16:27` and `16:39` were inaccurate. Authoritative Git UTC times are B1 implementation `15:38:27`, B1 checkpoint `15:38:39`, B2 `16:14:46`, hardening `16:18:48`. The clock was read at `16:19 UTC` before the push/PR attempt. Use those commit times for chronology; no test or commit result is changed by this correction.


## 2026-10-28 23:06 GMT · Round 2 synchronisation and contract v1.1
Kind: CONTRACT REVIEW v1.1
What: ACK. Fetched all branch heads and PR #4's head (`5202249`), inspected the approved work plan, B brief, review and exact Core diff. Main `44ea7b742fd4305c01cadd2cd026d417792db69b` contains the PR #4 merge. Merged main into this branch as `3130bbc`, preserving every earlier B commit. No rebase, force push or frozen-branch changes. Canonical composer/package pins are now included. Implemented only the approved GateId/GateReleased/IsGateReleased additions; shared physics layers remain an integration request, not part of v1.1.
Evidence: `git log origin/main` shows `44ea7b7 Merge PR #4`; `git ls-remote origin refs/pull/3/head` confirms existing PR #3 points at this branch's original delivery. GitHub GraphQL still returns HTTP 403, so CLI PR inspection/editing is unavailable; the merged Git graph and main's review identify the PRs. Existing PR #3: https://github.com/Build-with-SK/patient-zero-vr/pull/3.

## 2026-10-28 23:06 GMT · Gate and hospital integration
Kind: READY FOR REVIEW 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Contract v1.1 gate release is emitted once after the planks/window/lure/4 m retreat conditions, remains queryable for late doors while Act 1 is active, and closes before ActStarted on a restore/new run. None/other-act queries are false. Early/stale door completion is ignored until release; authorised DoorOpened completes Act 1 and enters Machinery. ExperienceDirector alone raises the channel. To Codex 1: subscribe to GateReleased and query IExperienceDirector on door enable; fail closed without a service, then report ObjectiveCompleted(Act1_DoorOpened) after opening. No A assembly, rig, scene or file was edited.
What: Per the owner's Round 2 operating-theatre request, dressed the existing supply footprint with static procedural table, unlit surgical lamp, cabinet, shelf and tray. No room/marker/puzzle/doorway or encounter timing moved. Furniture clearance is tested against layout points and Z2's route. The patient-room/corridor/vestibule greybox, AI, audio, props and timed director were retained. Fixture housings and static batching flags add no dynamic lights. Inactive zombie agents are disabled to avoid blocking puzzles; act starts reset pose and the AI tick clock.
Evidence:
```
PATH=/workspace/tools/dotnet:$PATH DOTNET_CLI_HOME=/workspace/tools/dotnet-home python tools/logic_tests/run_logic_tests.py
Logic tests: 137 passed, 0 failed.
python tools/review/check_rules.py --seat B --base origin/main --head integration/xr-systems
0 error(s), 0 warning(s).
python tools/review/check_rules.py
0 error(s), 0 warning(s).
python tools/unity/validate_packages.py
0 error(s), 0 warning(s).
```
Also: review checker SELF-TEST PASSED; assembly graph has unique names, present internal references and no cycles; supplementary Roslyn C# 9 parse: 56 files, 0 syntax errors (not Unity API/type compilation). Actual portable and native-attempt output is committed in `Environment/ROUND_2_VALIDATION.md`.

## 2026-10-28 23:06 GMT · Review fixes
Kind: FIX DONE FIX-B-13 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Progress resets hints to the per-act assist floor. Always-on/adaptive assist survives progress, and enabling always-on mid-act immediately publishes the floor. Three new portable regression tests pass.
Evidence: 137/137 portable tests; seat B checker 0 errors/0 warnings (commands above).

Kind: FIX DONE FIX-B-14 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Split SightBlockers (walls/closed doors, default layer 0) from HearingOccluders (closed door leaves only, default Nothing until TD/A assign the shared layer). Hearing rays end at the noise source without the old upward offset. Expanded the native hearing test to prove a wall retains full radius, while a closed-door mask halves it. Native fixture written, NOT RUN.
Evidence: Existing portable hearing boundary/origin/occlusion tests pass; Unity unavailable.

Kind: FIX DONE FIX-B-15 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Build step explicitly saves native NavMeshData to `Assets/PatientZero/Environment/Generated/WardNavMesh.asset`, replacing old data. Navigation fixture reloads Ward twice and asserts the saved asset and complete routes. It also checks Z1 can reach the window-arrival threshold, exposing an oversized bake radius before an Act 1 stall. Native persistence and geometry NOT VERIFIED without Unity.
Evidence: Source/static checks pass; compose attempt returns BLOCKED exit 2 (Unity 6000.3.27f1 absent).

Kind: FIX DONE FIX-B-16 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Audio occlusion defaults to layer 0 and is assigned explicitly by the Bootstrap builder; built-in 3D remains the fallback. Added portable attenuation checks at real Ward beacon/alarm/lure distances. Audible gain/low-pass remain native/headphone validation items.
Evidence: 137/137 portable tests, including configured-distance attenuation; no headphone claim.

Kind: FIX DONE FIX-B-17 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Exit-sign emission retains its authored green, with fixed visible emission during Blackout instead of the ambient blue tint. Still written only by LightingController.
Evidence: Ownership/static checks clean. Actual rendered brightness/green must be inspected in Unity.

Kind: FIX DONE FIX-B-18 8b7f7de8135c8470a4474aba7dd3c6c1c0304de7
What: Built wall boxes use exact span lengths and only transverse thickness. New portable tests check actual collider extents preserve every complete door-opening width, rather than checking only mathematical spans.
Evidence: Portable clearance test passes; no native collision claim.

## 2026-10-28 23:06 GMT · Native attempts and TD handoff
Kind: QUESTION
What: Actual `unity_batch.py validate`, `editmode-tests`, `compose`, `desktop-sim` attempts each returned exit 2: `BLOCKED: Unity 6000.3.27f1 not found`. The PlayMode resolver also found no editor, so navigation/emitter validation and native test execution are blocked. No scene/prefab/mixer/NavMesh assets were generated. No headset/headphone tests or 72 FPS claim.
What: TD-owned tooling suite on this Linux host: 23 pass, 1 fails (`test_env_override_is_tried_first`, backslash-only expected Windows path versus PosixPath joins). No B change touches tools; TD please fix the platform-independent test expectation. Package policy passes but dependencies have not been resolved by Unity.
What: TD must configure bake agent radius 0.25 m (INT-B-1), approve/configure shared geometry/door/player/prop masks (INT-B-2), integrate A's services/tags/doors, and execute the canonical composer plus native test suites (INT-B-3). Default bake radius can prevent the Z1 window beat. Optional licensed assets and Meta XR Audio remain unverified; all audio/art stays procedural. Full commands, actual outputs and remaining checks: `Game/Assets/PatientZero/Environment/ROUND_2_VALIDATION.md`; updated integration contract: `Environment/INTEGRATION.md`.
Evidence: No editor binary found; all four attempted tool commands and PlayMode discovery blocked. Frozen refs remain read-only. Existing PR #3 updates through the same branch; no second PR or main merge is needed from B.

## 2026-10-28 · Round 2: approved v1.1 integration and hospital fixes
Kind: CONTRACT REVIEW v1.1 / PROGRESS
What:
- Verified PR #4 is MERGED (head 5202249, main 44ea7b7). Merged origin/main into integration/xr-systems without rebasing. Existing delivery 6bc4c4274aaabd2a265d1625dd543f0d43c33028 remains an ancestor; frozen refs untouched.
- ACK the exact approved GateId/GateReleased/IExperienceDirector.IsGateReleased contract; no Core files changed. DirectorCore latches Act1Door once after the approved planks/window/lure/four-metre clearance. ExperienceDirector is the sole runtime event writer. The query is true before publication, supports late-enabled doors, closes before ActStarted on restores and clears outside Act 1. Act1_DoorOpened advances to Machinery through the normal director state machine.
- FIX DONE FIX-B-13: assist floor persists on progress, adaptive restores and mid-act settings changes; progress still resets ordinary hint escalation.
- FIX DONE FIX-B-14: independent SightBlockers and HearingOccluders; only closed doors attenuate gameplay hearing. HearingOccluders defaults to Nothing until Claude supplies the approved door-only layer. Sight/audio retain geometry blockers. Wall/door radius regression passes.
- FIX IMPLEMENTED FIX-B-15 (Unity validation pending): bake persists WardNavMesh.asset through AssetDatabase, preserves its GUID on rebuild and rebinds the surface. Navigation PlayMode test now checks the data survives scene loading, as well as complete routes. INT-B-1 bake radius configuration remains Claude-owned and outstanding.
- FIX DONE FIX-B-16: built-in audio occlusion defaults and builder configuration use geometry mask 1; no optional Meta audio SDK was added. Curves cover the actual darkness anchors and final release/exit distance in portable tests, reaching silence at Max; listening/direction remain NOT TESTED.
- FIX DONE FIX-B-17: exit emissive retains green with a visible fixed Blackout level.
- FIX DONE FIX-B-18: wall collider thickness is perpendicular only; longitudinal spans no longer overlap doorway cuts. Regression checks every opening.
- Hospital: patient bed, operating table/instrument tray dressing in the existing supply-room footprint, emergency corridor sign, existing colliders/door openings, props, markers, saved navigation, six shadowless lights, audio emitters and director encounter commands. No room boundary, puzzle position or approved timing was changed. Functional door leaves and puzzle interactions remain seat A-owned; operating-theatre relocation/design D-4 remains a separate amendment.
- Kept the approved two-hard-scare timeline, Reduced mode, movement-based sight, catch/recovery rules, bounded searching, sound distraction, obstacle/path failure guards and reveal signal. All visual/audio content remains procedural fallback; no third-party asset import/licence assumption.
Evidence:
- python tools/logic_tests/run_logic_tests.py -> Logic tests: 137 passed, 0 failed (8 new regression tests beyond the merged baseline 129).
- python tools/review/check_rules.py -> 0 error(s), 0 warning(s).
- python tools/review/check_rules.py --self-test -> SELF-TEST PASSED.
- python tools/unity/validate_packages.py --quiet -> 0 error(s), 0 warning(s).
- unity_batch.py validate, editmode-tests, compose, desktop-sim each exit 2: BLOCKED, Unity 6000.3.27f1 executable absent. Local command logs under Game/Logs/PatientZero/Round2 (ignored, not published).
- PlayMode launch blocked by the same absent editor; no XML results generated. Gate lifecycle/late-door publication test and navigation reload test are source only, NOT TESTED.
Remaining dependencies:
- Claude: first pinned Unity import/compile; INT-B-1 bake agent radius (0.25 m); INT-B-2 approved door-only collision mask/layer; generated asset persistence verification and native mixer factory validation.
- Codex 1: consume GateReleased and query IsGateReleased on enable, keep failing closed without the director, emit Act1_DoorOpened only after the actual door opens. Uses Core only; no dependency on seat-B classes. A-owned code was not edited.
- Actual full-scene assembly, navigation, emitter direction/occlusion and desktop play are NOT TESTED. A physical Quest profile is still required for the 72 FPS gate.
Identity: commits use assigned businessteen noreply identity. This machine's existing authenticated transport is Build-with-SK; no authentication or repository security was changed.
Handoff: existing draft PR #3 tracks integration/xr-systems and will update on push. Do not merge until applicable Unity checks and shared integration review pass.


## 2026-10-28 23:10 GMT · Concurrent branch delivery preserved
Kind: PROGRESS
What: Push of local checkpoint `047e42c` was safely rejected because another session published `6ad78f8e9e678c6ff0e231fe43a631966db91e4b`. Fetched and reviewed that commit, then merged both histories without force, reset or rebase. All original commits and both append-only logs are retained. Consolidated equivalent fixes rather than generating duplicate theatre furniture. Kept the concurrent explicit hearing policy, wall regression, setting toggles, GUID-preserving NavMesh rebuild and patient-room bed/mattress dressing; retained our tested surgical lamp/cabinet plan, inactive-agent/pose fixes, real window reach assertion, saved-navigation reload and early door completion guard. Gate release now occurs during a live tick, so paused actor reports cannot release it. Both sessions' regression tests are retained.
Evidence: merged portable harness -> `Logic tests: 145 passed, 0 failed.` Full new output appended to Environment/ROUND_2_VALIDATION.md. Native status remains BLOCKED without Unity, and the earlier 137-test entries are historical checkpoints, not the final count. Existing PR #3 receives this merge on the same branch.

## 2026-10-29 · Overnight Windows desktop preparation and native validation
Kind: PROGRESS / CROSS-TEAM REVIEW
What:
- Fetched current refs. B advanced safely from 6ad78f8 to 7b23e03, preserving both sessions' Round 2 work. main remains 44ea7b7; round2 7c20447; gameplay cefbd3d includes da316a4. Frozen branches unchanged.
- The owner checkout on integration/round2 and its untracked Unity files/backup were not modified. Native work ran in isolated xr-round2 and disposable overnight-integration checkouts. No generated metadata/assets/settings/lock files are included in these commits.
- Assembly conflict: da316a4 extends the same reference-array tail Claude rewrote in E-5. Keep explicit TestRunner/NUnit settings and union in Flow.Logic and CoreUtils; also add Flow.Logic to Player runtime. Proposal: Environment/Validation/assembly-resolution.patch. Temporary-index application check passes; combined graph has unique names, present references and no cycles. Native packages contain CoreUtils and both TestRunner assemblies.
- Native combined compilation found two obsolete CancelInteractableSelection overload calls and ambiguous SettingsProvider. Proposal: unity-compile-followups.patch (casts to IXRSelectInteractable; qualifies PatientZero.Flow.SettingsProvider). Sample import then revealed the missing built-in Video module; proposal: sample-module-resolution.patch adds com.unity.modules.video 1.0.0. Native player-flow setup also raced GameFlow.Start; proposal: playmode-calibration-resolution.patch waits out Boot before confirming calibration. All are supplied as B-owned patch documents, not edits to A or TD files. Applied only in the disposable validation checkout, with one B ExperienceDirector merge conflict resolved using this branch's complete v1.1 implementation.
- FIX DONE FIX-B-19: retain A3.Cart marker and remove B's overlapping static cart/bottle geometry. A owns the physical trolley.
- WardCollisionBinding consumes proposed PZ_ClosedDoor if present; masks are tested and assigned to B components only. No layer creation, TagManager edit, A-door modification or Core amendment. Hearing uses only that user layer; movement/sight/audio include Default geometry too.
- B bake now refuses radius other than 0.25 m. Claude's existing NavigationSettings sets it successfully before baking in the trial. Runtime path handling validates a complete cached path, steers around walls and waits briefly at dynamic doors before bounded failure; native obstacle tests pass.
- Fixed native-only Act 1 timer-boundary failures by storing the lure deadline as a rounded float. Fixed the window/ear-height target selecting ceiling navigation by projecting movement to the actor's floor. Fixed first-load director/zombie freezes when Playing was broadcast before Ward subscribed. Added a late-load regression and saved-floor first-encounter integration test.
- Added HospitalValidation menus for saved scene bindings/routes and the saved navigation asset. It reports absent door-layer configuration and handles an empty batch scene setup without hiding errors.
Evidence:
- Portable logic: 147 passed, 0 failed. Tooling unittest: 24 passed. Whole-tree checker: 0 errors/0 warnings. Rule self-test PASS. Package validator PASS.
- Unity 6000.3.27f1 is now installed and licensed on Windows; isolated native compilation succeeded. EditMode 149 passed / 0 failed; geometry builder executed in preview tests.
- Combined trial + proposed patches: composer PASS, Bootstrap and Ward saved, 30 layout tags within 0.05 m, all five Core services, bake radius 0.5 -> 0.25 m. This is a trial result, not a merge to main or round2.
- Integrated native PlayMode: 15 passed / 0 failed, including autoplay to Complete, pause during load, plug recovery, ten restarts, saved routes/reload and cold-start director fixture. An earlier run had 10 pass/4 fail; those findings drove the fixes/proposals. One intermediate rerun was stopped after confirming the missed-state-event freeze; it is not counted as a pass.
- Additional saved-floor first encounter: 1 passed / 0 failed.
- Saved hospital validator PASS, with warning that PZ_ClosedDoor is unassigned. Standalone saved navigation routes PASS. Desktop simulator setup PASS (four fields changed, Standalone OpenXR init off).
- Sanitized evidence: Environment/Validation/native-results.json. Raw XML/logs retained under Game/Logs/PatientZero/Overnight in the isolated checkouts, excluded from git. Windows procedure: Environment/DESKTOP_PLAYTEST.md.
Remaining:
- Claude/Codex 1 review the four proposed patches; rebuild in their safe integration checkout. No A-owned code, protected main or published round2 branch was changed by B.
- INT-B-2 CONTRACT REQUEST: approve/configure PZ_ClosedDoor on an unused user layer, assign only physical door leaves through A/TD generation, rebuild. B consumes the name without reserving a numeric layer. Until then closed-door hearing attenuation remains unverified; movement still checks Default door geometry.
- Human keyboard/mouse full run, rendered lighting/readability and subjective headphone direction are NOT TESTED. No Quest build, real tracking or 72 FPS claim. Native headless autoplay is not a human playtest.
Delivery: same integration/xr-systems branch and draft PR #3; do not merge it automatically.

Final native follow-up: a fresh Unity session after simulator import compiled successfully and exited 0. Native first-encounter test passed 1/1; saved hospital validator passed with the documented door-layer warning.
