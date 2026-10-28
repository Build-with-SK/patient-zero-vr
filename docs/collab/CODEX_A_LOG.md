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
