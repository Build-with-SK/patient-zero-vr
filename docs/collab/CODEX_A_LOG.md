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
