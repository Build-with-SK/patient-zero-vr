# Codex 2 (seat B) brief: scarier zombies, the AI and audio side

Written by Claude (Technical Director), 2026-11-13. Read `docs/collab/WORK_PLAN.md` first: its seats, invariants and
protocol apply here unchanged.

## Why

The owner played the two-minute build and asked to "make the zombie scarier". They found realistic zombie models on
Meshy (CC0). Claude is bringing one of them into the game: a realistic, bloodied zombie, rigged as a **Unity Humanoid**
(Mixamo bone names), with walk and run clips. Your part is how the zombies move and sound once that model is in, and it
can be built and tested now, before the model lands.

## Where to work

* Base: `origin/claude/two-minute` at `f948115` or later (the current game: E-22…E-28). **Not** `main`, which is far behind.
* Your branch: `codex/zombie-fear`, cut from that base. Push it to `origin`. Never push to `claude/two-minute`,
  `integration/round2` or `main`; never force-push. Claude reviews and merges.
* Edit only seat B paths: `Game/Assets/PatientZero/AI/`, `Audio/`, `Atmosphere/`, `Environment/`. Claude has edited
  some of your files on `claude/two-minute` as recorded integration exceptions (`INTEGRATION_ROUND2.md`, E-22…E-28):
  keep their behaviour. `Core/` and `Integration/` are Claude's; if you need a new id or event, write a CONTRACT REQUEST
  in your log (file, exact addition, why) and do not code against it until it is merged.

## Rules that stay

* Zombies are **blind and hunt by sound** (owner). Nothing here may let a zombie see the player or know where a silent
  player is. In particular, a zombie must not *look at* the player unless it is chasing them: it looks at what it heard.
* Freeze or die, the one struggle, the gunshot rush (E-28: `ZombieBrain.Rushing`, `GunshotRushSeconds`) and every
  number in `ZombieBrain` keep their behaviour. This task changes how zombies look and sound, not what they do.
* Never move or shake the player's camera (`GAME_DESIGN.md` sections 7 and 9). No text or hints on screen (owner rule).
* The Quest build must not get post-processing or per-frame allocation cost from this.
* The current Quaternius zombie (a **Generic** rig, glTFast) must keep working exactly as now: everything new runs
  only when `Animator != null && Animator.isHuman`.

## What Claude provides (do not build these)

The model import, its Animator controller and the compose step are Claude's (`Integration/Editor/ZombieArt.cs`). The
controller keeps today's parameters: float `Speed` (m/s), int `AlertLevel`, triggers `Hit` and `Die`.
`ZombieAnimationController.Animator` and `.Visual` are set at compose time as now. Find bones with
`Animator.GetBoneTransform(HumanBodyBones.X)`; never by name.

## Tasks

**T1. A creeping body (Humanoid only).** In `AI/Runtime/ZombieAnimationController.cs`, after the Animator has posed the
body (`LateUpdate`), add small procedural motion on top of the clips:

* *Listening:* while `Suspicious`, the zombie stands stock still, head cocked to one side (about 20 degrees) and turned
  toward the sound, for the whole telegraph.
* *Looking:* while `Investigating` or `Searching`, the head turns toward the zombie's current target (where it heard the
  noise); while `Chasing`, toward the player's head. Clamp the turn (about 70 degrees left or right, 30 up or down) and
  ease it, never snap. Today the shell passes the player's head to `Apply` in every state; pass the brain's `Target`
  instead outside a chase (`ZombieAgentShell`, seat B).
* *Twitches:* when unaware or searching and not moving fast, now and then a short jerk of the head or one shoulder
  (about 0.1 s out, eased back), a few seconds apart, different for each zombie but the same from run to run (seed from
  the zombie id). None while chasing, staggered (`Hit`), dead, kneeling or restrained.
* *Running:* when moving faster than its walk (rushing to a gunshot or chasing), the upper body leans forward (about 15
  degrees, eased in and out).

Put every rule that can be tested without the engine (which state allows which motion, the twitch schedule from a
seed, the clamp and easing of the head turn) in a new engine-free type under `AI/Logic/` and test it in
`AI/Tests/EditMode/` so the logic harness runs it.

**T2. Running footsteps.** A zombie moving faster than its walk should sound like it: footsteps faster and heavier than
the slow shuffle (today `ZombieShuffle` is one attached loop whatever the speed; see `ZombieAgentShell`). Use what the
audio API already offers (rate, volume, variant) if it can do this; if it needs a new cue id, write a CONTRACT REQUEST
instead. Its noise to other zombies is unchanged (zombie-origin sounds are not heard by zombies). You cannot listen to
audio in your environment: describe exactly what changed so the owner can judge it by ear.

## Done means

* `python tools/logic_tests/run_logic_tests.py`: all pass (today 261; yours added).
* `python tools/review/check_rules.py --seat B --base origin/claude/two-minute --head codex/zombie-fear`: no new error
  or warning from your files.
* One `READY FOR REVIEW <commit>` entry in `docs/collab/CODEX_B_LOG.md` (format in `WORK_PLAN.md`), listing every file
  changed and, separately, what you could not run. You have no Unity: write "NOT RUN in Unity" for anything that needs
  the editor, and never call it working. Claude builds, runs the release probe and the Unity tests on Windows.
