# Codex A log

## 2026-10-28 · Contract review v1
Kind: CONTRACT REVIEW v1
What: ACK approved contract v1 at origin/main 73a3b67a8699724c7dc8b76c9b2b7a562eef1738. Read AGENTS, WORK_PLAN, ARCHITECTURE, GAME_DESIGN, CODEX_A_BRIEF, REVIEW_CHECKLIST, TEST_REPORT and every Core/Contracts file. Own only Player, Interaction, Flow, UI and this log. No competing architecture or reuse of the prior prototype contracts. GitHub API /user verifies lavanyahd. Commit identity will be 116944917+lavanyahd@users.noreply.github.com per command.
Evidence: python tools/logic_tests/run_logic_tests.py → Logic tests: 43 passed, 0 failed. No compiler warnings.

## 2026-10-28 · Integration requirements
Kind: CONTRACT REQUEST
What: Interfaces.cs / Payloads.cs / GameEvents.cs: request an explicit director-owned Act1DoorReleaseRequested channel (empty readonly payload or act id), emitted only once all planks are off and the Z1 scare/retreat gate has finished. No current service or event communicates this gate to the A-owned door. Cannot infer it from elapsed time or ActStarted without skipping/reordering the scare. Implementation will fail closed until approved; no code will reference this proposed API.
Evidence: GAME_DESIGN §4 Act1 requires Z1 >=4m from window; IExperienceDirector exposes only CurrentAct, ExperienceTime and StartRun. ZombieAlert carries no guaranteed Walker retreat signal.

## 2026-10-28 · Tooling inspection and plan
Kind: PROGRESS
What: Installed editors 6000.3.26f1 and 6000.6.6f1. Canonical Game/ contains Assets only: no ProjectVersion.txt, Packages manifest/lock, Integration composer or approved Unity compile evidence. 6000.3.26f1 has Windows support only; Android Build Support/SDK/NDK/OpenJDK absent from that editor. Request Claude pin exact Unity 6.3 patch and packages and provide integration project. Do not modify Packages/ProjectSettings or copy the unrelated prototype. A1: tested engine-independent flow, mechanics, tracking/reach, settings and captions; publish separate review. A2: thin shells/build steps explicitly unverified pending the pinned project; A3: playmode integration tests, no claim of runtime/device pass until run.
Evidence: local editor installation inventory, absence of canonical ProjectVersion.txt and manifest; portable Core baseline 43/43.

## 2026-10-28 · A1 review
Kind: READY FOR REVIEW 3fb432c26089e757c0036f339b3e1a1e7290e4e6
What: Engine-independent A1 milestone complete; 31 new tests, 74 total. No runtime/Editor claims. Adapted tracking settling and cancellation from 72be6d8 with credit in commit. Act1 gate request remains open.
Evidence:
```text
python tools/logic_tests/run_logic_tests.py
Compiling 36 files ...
Logic tests: 74 passed, 0 failed.
(no compiler warnings)
python tools/review/check_rules.py --seat A --base origin/main --head codex/gameplay
0 error(s), 0 warning(s).
```

## 2026-10-28 · A2/A3 source implementation and integration blockers
Kind: PROGRESS
What: A1 published separately in draft PR #1 before A2 began. Added thin runtime shells, XRI-backed mechanic/prop adapters, tracking safety and controller fallback, palm-menu debounce, desktop simulator, additive scene/fader executor, settings/UI/captions/reveal and scene-build steps. Added PlayMode source tests for autoplay, reload x10 per-channel subscriber counts, pause during load and required plug recovery. Rechecked origin/main: still 73a3b67; no project pin, package manifest or composer. Thus runtime/Editor/PlayMode sources remain UNVERIFIED IN EDITOR; no canonical Unity scene/build or headset success is claimed. This does not inherit the earlier prototype's validation.
Evidence:
```text
python tools/logic_tests/run_logic_tests.py
Logic tests: 80 passed, 0 failed.
(no compiler warnings)
python tools/review/check_rules.py
0 error(s), 0 warning(s).
python tools/review/check_rules.py --self-test
missing rules: none
comment false positives: 0  string false positives: 0
SELF-TEST PASSED
```
Integration: Player/INTEGRATION.md documents every build step and dependency, desktop controls, services/events and tests. Only A-owned paths are included. No generated binary assets, hand-authored metadata, packages/settings, Core changes or seat B code. Missing assets: approved imported XRI hand/controller rig and generated Bootstrap/Ward; greybox primitive builders and hand vein placeholders supplied, no external production assets claimed. No standalone Android toolchain was configured; the installed 6.3 editor has no Android Build Support/SDK/NDK/OpenJDK.
Open blockers addressed to Claude: exact Unity patch + package pins + scene composer; Act1 release-gate contract request above; wire and verify XRI rig pose/action/teleport provider/vignette references and palm orientation in Unity. Prior broad flashlight/barricade requests are not a reason to invent v1 cue/layout/objective IDs: approved GAME_DESIGN uses three planks and B-owned lighting. Runtime request review remains draft until dependencies and actual Unity tests pass.

## 2026-10-28 · Source review handoff
Kind: READY FOR REVIEW b9ceded9b030babea0840bc9fa28a07269deb952
What: Ready for source/portable review only, NOT Unity integration acceptance. Draft PR #1 targets main; no merge performed. Runtime/Editor/PlayMode verification remains blocked as documented above. No contract modifications made.
Evidence:
```text
python tools/logic_tests/run_logic_tests.py
Logic tests: 80 passed, 0 failed.
python tools/review/check_rules.py --seat A --base origin/main --head codex/gameplay
0 error(s), 0 warning(s).
python tools/review/check_rules.py --self-test
SELF-TEST PASSED
```

## 2026-10-28 · Round 2: approved v1.1 synchronisation
Kind: PROGRESS
What: GitHub API as lavanyahd verified PR #4 closed/merged, head 5202249f2c68232d905ff41654ffb98e35a22382, merge 44ea7b742fd4305c01cadd2cd026d417792db69b on main. Merged origin/main normally into codex/gameplay, no conflicts or rewritten commits. Only A-owned source changes after that merge. No Core/seat B code changed; no PR or main merge performed by A.
Evidence: /pulls/4 merged=true, merged_at=2026-10-28T22:57:38Z; main 44ea7b7. Existing 83b57d3 preserved in ancestry.

## 2026-10-28 · Round 2 review fixes
Kind: FIX DONE FIX-A-1 f1ed57b
What: Moved DesktopBuildStep to Player/Editor (Bootstrap 420) and added the same-seat Interaction runtime reference. FIX-A-2: replaced Unity.ugui references with UnityEngine.UI (no TMP types used). FIX-A-3: mouse locomotion and interaction hot loops compile only with PZ_DESKTOP_MOUSE; default desktop uses XRI simulator. Tracking bridge no longer skips the simulator merely because physical XR is inactive. FIX-A-4: desktop camera comes explicitly from PlayerRig.origin.Camera. Source fixes in f1ed57b; actual package/Unity confirmation still blocked.
Evidence: portable check of all 11 actual combined scene-build declarations through Integration.BuildPlan passed (disposable probe); whole-tree and seat A static checks have 0 errors/0 warnings. No project define was changed.

## 2026-10-28 · Round 2 door-gate implementation
Kind: PROGRESS
What: ObjectiveDoor subscribes/unsubscribes GateReleased, queries IExperienceDirector.IsGateReleased after subscription and on act restart, and retries if director service arrives late. DoorGateLatch consumes release once per act start. Director query is authoritative; no manual bypass and no A-raised gate events. Duplicate notifications/re-enable do not replay unlatch; act reset closes the door. Added five portable gate tests and four authored PlayMode regressions (live+restart, late creation+reenable, false/wrong gate, late director service). PlayMode tests have NOT run.
Evidence: branch portable suite 114 passed/0 failed, no compiler warnings. Disposable combination with origin/integration/xr-systems 6ad78f8: 179 existing tests passed. Two additional disposable probes passed (181 total): real B DirectorCore -> A DoorGateLatch live/late/duplicate/reset/new-attempt path, and all 11 actual SceneBuildStep declarations through the real BuildPlan. No probe, B source or scratch merge committed to A. These are portable checks, not scene/Unity/device checks.

## 2026-10-28 · Round 2 Unity validation attempts and integration status
Kind: PROGRESS
What: Canonical project now pins 6000.3.27f1 (672d562a42a7). Installed editors are 6000.3.26f1 and 6000.6.6f1; neither substituted. Ran the canonical launcher commands below: every one is BLOCKED before launching an editor. Consequently there is no new Unity Editor log or NUnit XML to inspect. Runtime/Editor headers remain UNVERIFIED IN EDITOR. Android toolchain verification/build also blocked (pinned editor and its Android module absent; 26f1 has Windows support only). No generated scenes or playable canonical build claimed.
Evidence:
```text
python tools/unity/unity_batch.py validate
python tools/unity/unity_batch.py editmode-tests
python tools/unity/unity_batch.py compose
python tools/unity/unity_batch.py desktop-sim
Each: BLOCKED: Unity 6000.3.27f1 not found.
Expected location: C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe
Launcher exit 2 (environment blocker, not compile/test failure).
python tools/logic_tests/run_logic_tests.py
Logic tests: 114 passed, 0 failed.
python tools/review/check_rules.py
0 error(s), 0 warning(s).
python tools/unity/validate_packages.py
0 error(s), 0 warning(s). (lock absent until actual package restore)
python -m unittest discover -s tools/unity -p "test_*.py"
Ran 24 tests ... OK
```
Remaining: install exact pinned editor and Android support; compose with B integration; verify XRI 3.7 rig, poses, actions, teleport provider, hand-menu orientation, stereo fader and reach in Unity/device. Run authored full-loop/restart/pause/recovery/door PlayMode suites and desktop simulation. Hands tracking loss follows approved design: zero-velocity release after 1.5s + warning, no automatic pause; headset focus loss pauses and explicit input resumes. Flashlight/Act1 trolley amendment remains outside approved Core/layout (DESIGN_RECONCILIATION D-1/D-2); no competing lighting/event SDK introduced. Player/INTEGRATION.md updated for v1.1 and pins. Claude should combine reviewed A/B branches for actual integration; scratch merge was diagnostic only.

## 2026-10-29 · Urgent CS0012 dependency fix
Kind: FIX DONE da316a4b593323a5da3d4db9376a95c3c62b21de
What: GameFlow.Core returns GameFlowCore, defined by PatientZero.Flow.Logic. Player referenced Flow but not Flow.Logic; added the direct reference. Also added Flow.Logic and Unity.XR.CoreUtils to Flow.PlayModeTests, which accesses flow.Core and PlayerRig.origin (XROrigin). Only these two asmdefs changed in the isolated fix commit so an integration branch can cherry-pick it without pulling gameplay history. No classes duplicated, no Core/B/project settings changed.
Evidence: inspected all 24 local asmdefs; names unique, graph acyclic. Portable tests 114 passed/0 failed; tooling 24 passed; whole-tree checker and package policy 0 errors/0 warnings. Actual Unity launcher validate remains BLOCKED locally (6000.3.27f1 not installed on this host), despite owner reporting their Unity session is open. No Unity compile success claimed.
Apply on gameplay: git fetch origin, git pull --ff-only origin codex/gameplay. Apply on existing integration branch: git fetch origin, git cherry-pick da316a4b593323a5da3d4db9376a95c3c62b21de. Preserve local edits before pulling/cherry-picking; do not reset or change branches blindly. Unity reimports the changed asmdefs and recompiles.
