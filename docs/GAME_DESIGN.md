# PATIENT ZERO — 120-Second Experience Specification

Version 1 · 2026-10-28 · Owner: Claude (Technical Director / Horror Experience Designer)
Implementation owners: **A** = Codex A (player, interaction, flow, UI) · **B** = Codex B (AI, audio, atmosphere, environment).
Coordinates are in `Game/Assets/PatientZero/Core/Contracts/WardLayout.cs`, which overrides any number in this document.

---

## 1. Pitch and pillars

You wake in isolation room B-7 of a sealed hospital ward. The infected patients hunt by sound. You have about two minutes to get out without being heard. At the exit you learn why they always seemed to be following you: **you are Patient Zero.**

1. **Sound is both the threat and the map.** Everything you do makes noise. Everything they do tells you where they are.
2. **Your hands are the interface, and they deliver the reveal.** Every puzzle is physical, and the ending happens on your own hands.
3. **Dread over shock.** Exactly **two** hard scares in 120 seconds. The rest is anticipation, sound and near-misses.
4. **Comfort is never traded for fear.** No forced camera motion, no strobing, teleport by default, every scare can be reduced.

Narrative seeds, so the reveal is earned: in Act 1 the player's wristband reads `B-7 · P-0` (readable if they look). In Act 2 the corridor monitor shows `ISOLATION B-7 — SUBJECT P-0 — ASYMPTOMATIC`. In Act 3 the PA says "...do not approach the carrier...". Act 4 reveals it.

---

## 2. Core mechanics

### 2.1 Noise (zombies hear radius, not volume)

| Player action | Cue | Noise radius |
|---|---|---|
| Pulling a plank (creak, scales with pull) | `PlankCreak` | 0–2 m |
| Plank pops free | `PlankPop` | 5 m |
| Object hits the floor | `ImpactLight` / `ImpactHeavy` | 2 m / 6 m × intensity (impact speed ÷ 3 m/s, clamped 0–1) |
| Valve turned slowly, < 90°/s | `ValveSqueal` (low) | ≤ 1 m |
| Valve turned fast, ≥ 180°/s | `ValveSqueal` (high) | up to 6 m |
| Cable dragged | `CableDrag` | ≤ 1 m |
| Plug inserted | `PlugSpark` | 6 m |
| Door unlatches | `DoorUnlatch` | 3 m |
| Teleport landing | `TeleportLand` | 1.5 m |
| Smooth-locomotion footstep (optional mode) | `PlayerFootstep` | 2.5 m |
| Bumping the bottle cart (Act 3) | `ImpactHeavy` | up to 6 m |
| PA, ventilation, drip, beacon, alarm, monitor | — | 0 (zombies ignore) |

A noise is heard when `distance ≤ radius × hearingMultiplier × occlusion`. Occlusion is 0.5 through a closed door and 1.0 otherwise. Noises with `NoiseOrigin.Zombie` or `Ambient` are ignored.

### 2.2 Zombie senses and behaviour (B · `ZombieBrain`)

**Hearing** is the main sense. **Sight sees movement, not presence.** A zombie sees the player only when the player is inside its 60° view cone, in range, and moving: head speed > 0.6 m/s or a hand > 1.2 m/s. Sight range is 6 m in `WardDim`, 3 m in `EmergencyRed`, and **0 in `Blackout`**. If you freeze, they can't see you. Act 1 teaches this.

| From | Trigger | To | Notes |
|---|---|---|---|
| Patrol / Idle / Dormant | hears a noise | **Suspicious** | 0.6 s telegraph: head snaps toward the sound, `ZombieSniff`. Reduced: 1.2 s |
| Suspicious | telegraph ends | **Investigating** | walks (0.9 m/s) to the noise position. Uses the NavMesh, snapped to the nearest reachable point |
| Investigating / Searching | hears a new noise | retarget | only if louder, or ≥ 1.5 s since the last retarget. Prevents jitter |
| Investigating | arrives | **Searching** | 5 s of sniffing and looking around |
| Searching | timer ends | Patrol / back to spawn | `ZombieGroan` on giving up |
| Investigating / Searching | sees the player, or hears a player noise within 2 m | **Chasing** | always preceded by `ZombieSnarl` and its caption: the player gets a warning |
| Chasing | player lost for 3 s (unseen, unheard) | Searching | teleporting away breaks a chase |
| Chasing | player within 0.8 m for 0.25 s | **Caught** → `PlayerCaught` | |
| any | GameState not live, or surge stun | frozen | |
| Restrained (Z4) | — | — | thrashes. Can reach only 1.3 m from the gurney (chain) |
| Passive | director command at the exit | kneel | no senses |

| Parameter | Z2 Orderly | Z3 Patroller | Reduced-scare mode |
|---|---|---|---|
| Walk / chase speed | 0.9 / 1.6 m/s | 0.8 / 1.5 m/s | chase × 0.8 |
| Hearing multiplier | 1.0 | 1.2 | same |
| Sight | as above | none (blind in Blackout) | sight off |
| Catch | 0.8 m for 0.25 s | same | 0.6 m for 0.5 s |

### 2.3 Hands and controllers (A)

| Interaction | Hands | Controllers | One-handed |
|---|---|---|---|
| Grab | pinch or whole-hand grab | grip | yes |
| Pull plank | grab and pull 8 cm out (assist 4 cm) | same | yes |
| Turn valve | grab the rim and turn. Re-gripping keeps progress (ratchet). 540° in either direction (assist 270°) | grip and twist | yes |
| Plug cable | grab the plug, carry it 0.8 m, push into the socket. Snaps within 6 cm and 35° (assist 12 cm and 60°) | same | yes |
| Release lever | grab, pull down, hold 2 s (assist 1 s). Letting go decays progress at 1 per second; it doesn't reset to zero | same | yes |
| Teleport | point and pinch at an anchor (hand ray) | thumbstick forward, release | yes |
| Turn | physical. Snap turn on the thumbstick (controllers) | snap 45° by default | — |
| Pause | left hand palm-up pinch (system menu gesture) | menu button | — |

A required item that falls below 0.3 m or out of reach returns to its hook or start position after 5 s. The player can never strand a puzzle.

---

## 3. The ward (schematic, not to scale; `WardLayout.cs` is authoritative)

```
 z 7                                         +--------+
                                             |  SIDE  |  Z3 patrol north (10.5, 6.5)
 z 4.5 +-------------------------------------+--|  |--+----+-------------------+
       | monitor VALVE HOOK SOCKET        drip            |                   |
       |  (1.0)  (2.0) (3.0) (3.8)        (7.0)           |   Z4 gurney        |
       | A2.Start A2.Valve A2.Machinery  A3.Start Mid1 Mid2   A3.End|   (15, 4.4)  EXIT |
       | (0,3.25) (2,3.9)  (3.4,3.9)     (5.5)  (7.5) (9.6)   (12.4)| A4.Start     door |
       |                CORRIDOR          cart (8.5, 2.3)    BEACON | (13.6,3.0)  x=17 |
 z 2   +--|door|------------+--|door|---+   +--|  |--+  (13, 2.1) | A4.Release LEVER |
       | B-7  planks (0,1.9)|  SUPPLY    |   | LINEN  |            | (16.3,2.5) (16.9)|
       |      A1.Door (0,1.4)| Z2 (6.5,0.5)  | Z3 S   |   z 1.5 -> +-------------------+
       |      A1.Start (0,-0.5)            |   |(10.5,0.5)              VESTIBULE
 z -2  +--------------------+-------------+   +--------+
     x -2        0        2  4            8  9.5   11.5  13                    17
```

The critical path runs through the anchors, each step ≤ 3 m: `A1.Start → A1.Door → A2.Start → A2.Valve → A2.Machinery → A3.Start → A3.Mid1 → A3.Mid2 → A3.End → A4.Start → A4.Release`.

---

## 4. The 120 seconds

The times are a **target for a first-time player who is never caught.** Acts advance on objectives, not clocks (section 5).

### Master timeline

| Time | Act | Beat | Key sound | Light | Zombie | Player objective |
|---|---|---|---|---|---|---|
| 0:00 | 1 | Wake in the dark | `HeartbeatWake` | fade in from black, 4 s | — | look around |
| 0:03 | 1 | Garbled PA | `PaGarbledBreach` | one tube flickers | — | — |
| 0:06 | 1 | Physics teach: tray items clatter | `ImpactLight/Heavy` | — | — | optional: grab things |
| 0:10 | 1 | Barricaded door, scratched note "THEY HEAR YOU" | `FluorescentBuzz` | — | — | go to the door |
| ~0:16 | 1 | **First zombie** passes the door window | `ZombieShuffle`, `ZombieBreath` | silhouette against corridor light | Z1 scripted walk | freeze |
| ~0:24 | 1 | **HARD SCARE 1**: third plank pops, Z1 slams the window | `WindowSlam` | ≤ 0.3 s flicker spike | Z1 at window | — |
| +2 s | 1 | Distant crash; Z1 turns and follows the sound | `DistantCrashLure` | — | Z1 walks away | *learn: they follow sound* |
| ~0:30 | 1→2 | Door unlatches | `DoorUnlatch` | — | — | step into the corridor |
| 0:32 | 2 | Corridor, monitor beeping (`P-0` text) | `MonitorBeep`, `Ventilation` | `WardDim` | Z2 dormant, breathing behind the supply door | go to the valve |
| 0:35 | 2 | Vent the pressure valve (slowly = quietly) | `ValveSqueal` | — | — | turn 540° |
| ~0:42 | 2 | **Investigation**: Z2 emerges and walks past toward a noise | `ZombieSniff`, `ZombieShuffle` | — | Z2 investigates and searches | freeze; let it pass |
| ~0:52 | 2 | Plug the cable (spark is loud: wait for Z2) | `CableDrag`, `PlugSpark` | — | Z2 returns | plug in |
| ~0:58 | 2→3 | Power surges and blows the lights | `PowerThrum`, `BreakerOverload` | `Surge` → `Blackout` | all frozen 2.5 s | — |
| 1:01 | 3 | Silence. PA: "...do not approach the carrier..." | `BlackoutTone`, `PaCarrierWarning` | `Blackout` | Z2 gone | — |
| 1:04 | 3 | Beacon chirp far ahead: navigate by ear | `BeaconChirp` every 3 s | exit sign is the only light | — | teleport toward the chirp |
| ~1:10 | 3 | Z3 crosses the corridor 0.9 m from you, blind | `ZombieBreath`, `ZombieShuffle` | — | Z3 patrol crossing | don't make a sound |
| ~1:28 | 3→4 | Reach the beacon; fire door swings open | — | red light spills in | — | — |
| 1:30 | 4 | Red vestibule; final infected patient thrashing on a gurney | `AlarmKlaxon`, `RestraintStrain` | `EmergencyRed` | Z4 restrained | go to the lever |
| 1:36 | 4 | Pull and hold the emergency release | `ReleaseBarGrind` | — | — | hold 2 s |
| ~1:38 | 4 | **HARD SCARE 2**: strap snaps, Z4 lunges and stops short on its chain | `RestraintSnap`, `ZombieLunge` | — | Z4 released, chained | keep holding |
| ~1:42 | 4 | Exit bursts open into white daylight | `ExitDoorOpen`, `ExteriorWind` | `ExitDaylight` (2 s ramp) | Z4 goes passive | — |
| 1:44 | End | **REVEAL**: Z4 kneels; the ward falls silent; PA names you; veins spread over your hands | `PaPatientZero`, `RevealDrone` | — | passive | look at your hands |
| ~1:58 | End | Fade white → black, title **PATIENT ZERO**, Play again / Quit | — | — | — | — |

### Act 1 — Awakening (00:00–00:30) · Isolation room B-7

**Act-start state (restore):** lights `Awakening`. Three planks nailed. Door latched. Tray items on the tray. Z1 at `Z1.WalkFrom`, scripted. Player at `A1.Start` facing +Z.
**Objectives:** `Act1_DoorUnbarred` (progress = planks ÷ 3), then `Act1_DoorOpened`.

| Beat | Trigger | Detail | Owner |
|---|---|---|---|
| Wake | act start | 4 s fade from black (the fader, not the lights). `HeartbeatWake` 2D, fading over 6 s | B, A (fade) |
| PA | +3 s | garbled line from the corner speaker. Speech caption | B |
| Physics teach | always available | cup, kidney dish and clipboard on the tray (`A1.Tray`). Dropping them clatters. Noise is real, but nothing is near enough to hear | A |
| Door | +8 s | three planks across the door (`A1.PlankLow/Mid/High`). Note scratched in the wood: THEY HEAR YOU | A, B (text decal) |
| **Z1 pass** | first plank pops, or +16 s, whichever comes first | Z1 walks from `Z1.WalkFrom` to the glass of the door window (`A1.DoorWindow`), lit from behind, and lingers there tapping (`ZombieGlassTap`) | B |
| **Hard scare 1** | the third plank pops, or Z1 reaches the glass if that happens later | `WindowSlam` + ≤ 0.3 s light spike. Reduced: `WindowBreath`, face slowly emerging, no spike | B |
| Lure | 2 s after the scare | `DistantCrashLure` (Scripted noise, 20 m) far east. Z1 turns and shambles toward it, on to `Z1.WalkTo`, then is removed. **This teaches that they follow sound** | B |
| Unlatch | all planks off **and** Z1 ≥ 4 m from the window | `DoorUnlatch`, door swings 30° → `Act1_DoorOpened` | A (door), B (gate signal via director) |

**Gates:** the door can't open until the Z1 beat has played. Fast players get a short pause filled by Z1's slam and retreat.
**Hint ladder** (no plank progress): +10 s: the tube above the door flickers (Hint1) · +20 s: caption "The planks are loose." (Hint2) · +30 s: planks pop at 4 cm (Assist).
**Failure:** none. Z1 is scripted and can't catch. Act 1 is safe to learn in.

### Act 2 — Machinery (00:30–01:00) · Corridor

**Act-start state:** lights `WardDim`. Valve at 0°. Cable plug on `A2.CableHook`. Socket empty. Supply door half open. Z2 dormant at `Z2.Dormant`. Z1 removed. Player at `A2.Start` facing +X.
**Objectives:** `Act2_ValveVented` (progress = degrees ÷ 540) and `Act2_CablePlugged`, in either order, then `Act2_PowerRestored`.

| Beat | Trigger | Detail | Owner |
|---|---|---|---|
| Corridor | act start | `MonitorBeep` draws the eye to the monitor (`P-0` text). `ZombieBreath` is faintly audible from the supply doorway, audible from about 8 m | B |
| Valve | player | the turn speed sets the squeal and its noise radius (section 2.1). Fast turning wakes Z2 | A, B (cue) |
| **Investigation** (guaranteed) | Z2 hears a player noise **or** act time +12 s | if the player hasn't drawn Z2, the director plays `DistantCrashLure` at the corridor's west end (−1.5, 1.0, 3.3). Z2 emerges at `Z2.Doorway`, walks west past the player (about 1.3 m away), searches 5 s, then walks back | B |
| Cable | player | `CableDrag` while carrying. `PlugSpark` (6 m) on insert. If Z2 is within 6 m, it comes to investigate the socket. Freezing beside it is survivable; moving is not | A, B |
| Surge | `Act2_PowerRestored` | `PowerThrum` → lights ramp to 130 % over 0.6 s → `BreakerOverload` → 1.2 s fade to `Blackout`. **All zombies are stunned for 2.5 s.** Then `ActStarted(Darkness)` and Z2 is removed | B |

**Gates:** Act 3 can't start before the investigation beat has started. A player who completes both puzzles first still sees Z2 at the far end of the corridor as the lights die.
**Hint ladder** (per objective, no progress): +10 s: steam puff at the valve or a spark at the socket (Hint1) · +20 s: caption "Vent the valve." / "Plug in the cable." (Hint2) · +30 s: valve 270°, socket snap 12 cm (Assist).
**Failure:** caught by Z2 → restore Act 2.

### Act 3 — Darkness (01:00–01:30) · East corridor

**Act-start state:** lights `Blackout` (ambient ≈ 0.015 plus the brightness calibration offset, dense fog). `BeaconChirp` every 3.0 s at `A3.Beacon`. Valve and cable done. Z3 paused at `Z3.PatrolNorth`. Player at `A3.Start` facing +X.
**Objective:** `Act3_BeaconReached`: the player's floor position comes within 0.75 m of `A3.End`.

| Beat | Trigger | Detail | Owner |
|---|---|---|---|
| Silence | act start | ventilation stops. Only `BlackoutTone`, then the PA line "...do not approach the carrier..." | B |
| Beacon | +3 s | chirp from 7 m ahead. Teleport anchors show as faint markers **only while the teleport ray is up** (UI, not lighting). With **Sound cue visuals**, a floor strip pulses with each chirp and the caption shows a direction arrow | B (chirp), A (anchors, captions) |
| Landmarks | always | `WaterDrip` at `A3.Drip` (north wall) gives a second reference point. The bottle cart (`A3.Cart`) sits 1.1 m south of `A3.Mid1`: reach out carelessly and it rattles (up to 6 m noise) | B, A (cart physics) |
| **Z3 crossing** | the player arrives at `A3.Mid1`, or act time +10 s | Z3 walks north to south across the corridor at x = 10.5 (`Z3.PatrolNorth → PatrolSouth`), pauses 3 s, returns, pauses 4 s, and loops. It is blind; hearing ×1.2. From `A3.Mid2` it passes 0.9 m away. **A teleport landing (1.5 m) near it gives you away; waiting doesn't** | B |
| Arrival | `Act3_BeaconReached` | the fire door swings into the vestibule (no noise). Red light spills through | A (trigger), B (light) |

**Hint ladder** (no anchor progress): +12 s: chirp +3 dB and the strip pulse brighter (Hint1) · +22 s: caption "Follow the chirp." (Hint2) · +32 s: Z3 holds in the side room until the player reaches `A3.End` (Assist).
**Failure:** caught by Z3 → restore Act 3.

### Act 4 — Exit (01:30–02:00) · Vestibule

**Act-start state:** lights `EmergencyRed` (wall lamps pulse at 0.5 Hz, 50–100 %; ReduceFlicker holds them steady at 75 %). `AlarmKlaxon` looping at the exit, ducked −6 dB under PA lines. Z4 restrained on its gurney at `Z4.Gurney` (`RestraintStrain` loop, head tracking the player). Lever up. Exit door shut. Player at `A4.Start` facing +X.
**Objectives:** `Act4_ReleasePulled` (progress = hold ÷ 2 s), then `Act4_ExitOpened`.

| Beat | Trigger | Detail | Owner |
|---|---|---|---|
| Vestibule | act start | the chain from Z4's ankle and the scrape marks on the floor show its 1.3 m reach. **Fairness is readable at a glance** | B (art), A |
| Lever | player | `ReleaseBarGrind` while held. The door's lock bolts retract one by one, which shows progress in the world | A |
| **Hard scare 2** | lever progress ≥ 60 %, or act time +15 s | `RestraintSnap`: the wrist strap breaks, `ZombieLunge`, and Z4 lunges to the end of its chain. Its catch reach ends 1.3 m from the gurney, about 1 m short of a player standing at `A4.Release` (the layout test guarantees ≥ 0.3 m everywhere on the path). Reduced: the strap breaks, the strain continues, no lunge, stinger −9 dB | B |
| Open | `Act4_ReleasePulled` | `ExitDoorOpen`, the door swings outward, `ExitDaylight` ramps over 2 s (a ramp, not a flash), `ExteriorWind`. `Act4_ExitOpened` when it is fully open. The director requests `Ending` | A (door), B |

**Hint ladder** (lever untouched): +10 s: the lever's indicator blinks at 0.5 Hz (Hint1) · +20 s: caption "Pull the emergency release." (Hint2) · +30 s: hold 1 s (Assist).
**Failure:** only by walking into the chain's reach. Z4 snarls when the player comes within 2.0 m → restore Act 4.

### Ending — the Patient Zero reveal (≈ 12 s, no failure possible)

| t | Beat | Owner |
|---|---|---|
| 0 s | Daylight floods in. Every zombie goes **Passive**, and all zombie loops fade out over 2 s. **The ward goes silent for the first time** | B |
| 1 s | Z4 stops thrashing, turns, and kneels toward the player | B |
| 2 s | `PaPatientZero`: "Containment failure. Patient Zero has left isolation." (Speech caption) | B |
| 3–9 s | `InfectionReveal` 0 → 1: dark veins spread over the player's own hands (tracked hands or controller hands). The wristband `P-0` glows faintly | B (drives), A (renders) |
| 6 s | `RevealDrone`, low and slow. A sound, not a jump-scare sting | B |
| 10 s | fade to white over 1 s, then to black over 0.5 s. Title **PATIENT ZERO** | A |
| 12 s | `Complete`: **Play again** / **Quit**. No auto-advance. Credits are reachable from here | A |

---

## 5. Pacing model (dynamic timing)

* **Objectives drive acts, not clocks.** Target: 30 s per act and 120 s per run. Expected first-run range: 100–150 s.
* **Gates** keep the key beats from being skipped: Act 1 waits for the Z1 window beat, and Act 3 can't start before the Act 2 investigation has started. Acts 3 and 4 have no time gates, so a skilled player can be faster.
* **Hint ladder** (per act, measured from the last objective progress): Hint1 at +10 s (ambient), Hint2 at +20 s (caption), Assist at +30 s (mechanical). Any progress resets it. **No timer ever fails the player.**
* **Scare budget:** exactly **2 hard scares** (Act 1 window slam, Act 4 strap snap). Soft scares are the Z1 pass, the Z2 investigation, the Z3 crossing, and `DistantInfected` one-shots. One-shots happen at most once per 12 s, never within 6 s of a hard scare or a zombie state change, and only when tension < 0.4.
* **Tension** (director, 0..1): +0.3 when a zombie within 6 m reaches Investigating or higher; +0.1 per player noise ≥ 4 m; −0.05 per second. `TensionDrone` volume follows tension.
* **Silence:** for 3 s after each hard scare, ambience drops −9 dB. Quiet after a peak makes the next sound land.
* **Adaptive assist:** after a second catch in the same act, that act runs with Assist active, Reduced zombie speeds, and a pause-menu note saying so.

---

## 6. Sound cue table (B · `CueTable`, every `CueId`)

All world cues use **spatial blend 1, Doppler 0, a custom rolloff curve that is full volume to Min and falls to 0 at Max**. Never use Unity's logarithmic default: it stops attenuating at Max Distance instead of reaching silence, so a 15 m zombie would be audible across the whole ward. Spatialized clips are mono, 48 kHz.
**Caption policy:** Speech is captioned whenever subtitles are on. Sound captions marked ● are always shown; ○ only with **Sound cue visuals** on. Positional captions show a direction arrow (ahead/left/right/behind).

| CueId | What | Space | Min–Max m | Loop | Noise m | Mixer | Caption |
|---|---|---|---|---|---|---|---|
| HeartbeatWake | waking heartbeat | 2D | — | no | 0 | Music | ○ [heartbeat] |
| FluorescentBuzz | B-7 tube | 3D | 0.5–6 | yes | 0 | Ambience | — |
| PaGarbledBreach | PA, Act 1 | 3D | 2–20 | no | 0 | Voice | Speech: "...containment breach... Ward B is sealed..." |
| PlankCreak | pulling a plank | 3D | 0.5–8 | yes | 2 × i | World | ○ [wood creaks] |
| PlankPop | plank comes free | 3D | 1–12 | no | 5 | World | ○ [plank cracks] |
| WindowSlam | hard scare 1 | 3D | 1–15 | no | 0 | World | ● [slam on the glass] |
| WindowBreath | Reduced-mode replacement | 3D | 0.5–6 | no | 0 | World | ● [breathing behind the glass] |
| DistantCrashLure | scripted lure | 3D | 3–25 | no | 20 (Scripted) | World | ● [distant crash] |
| DoorUnlatch | B-7 door | 3D | 0.5–10 | no | 3 | World | ○ [latch clicks] |
| ValveSqueal | turning the valve | 3D | 0.5–12 | yes | 6 × i | World | ○ [valve squeals] |
| ValveVentHiss | vent complete / hint | 3D | 1–12 | no | 4 | World | ○ [steam hiss] |
| CableDrag | carrying the plug | 3D | 0.3–4 | yes | 1 × i | World | — |
| PlugSpark | plug inserted | 3D | 1–14 | no | 6 | World | ● [sparks] |
| PowerThrum | power restored | 3D | 2–20 | no | 0 | World | ● [power hums] |
| MonitorBeep | corridor monitor | 3D | 0.5–8 | yes | 0 | Ambience | — |
| BreakerOverload | surge | 3D | 2–25 | no | 0 | World | ● [breaker bursts] |
| BeaconChirp | exit beacon, every 3 s | 3D | 2–25 | no | 0 | World | ● [chirp] + arrow |
| PaCarrierWarning | PA, Act 3 | 3D | 2–20 | no | 0 | Voice | Speech: "...do not approach the carrier..." |
| WaterDrip | landmark | 3D | 0.3–7 | yes | 0 | Ambience | ○ [dripping] |
| AlarmKlaxon | exit alarm | 3D | 3–20 | yes | 0 | World | ● [alarm] |
| RestraintStrain | Z4 thrashing | 3D | 0.5–10 | yes | 0 | World | ● [straps straining] |
| RestraintSnap | hard scare 2 | 3D | 1–15 | no | 0 | World | ● [strap snaps] |
| ReleaseBarGrind | lever held | 3D | 0.5–8 | yes | 0 | World | ○ [metal grinding] |
| ExitDoorOpen | exit opens | 3D | 2–20 | no | 0 | World | ● [door bursts open] |
| PaPatientZero | reveal line | 3D | 2–20 | no | 0 | Voice | Speech: "Containment failure. Patient Zero has left isolation." |
| RevealDrone | ending | 2D | — | no | 0 | Music | — |
| ZombieBreath | zombie idle | 3D (head) | 0.5–9 | yes | 0 | World | ● [breathing] + arrow |
| ZombieGroan | zombie gives up | 3D | 1–15 | no | 0 | World | ● [groan] + arrow |
| ZombieSnarl | chase telegraph | 3D | 1–15 | no | 0 | World | ● [snarl: it heard you] + arrow |
| ZombieShuffle | zombie footsteps | 3D (feet) | 0.5–10 | yes | 0 | World | ● [shuffling] + arrow |
| ZombieSniff | Suspicious / Searching | 3D | 0.5–9 | no | 0 | World | ● [sniffing] + arrow |
| ZombieLunge | lunge / catch | 3D | 0.5–8 | no | 0 | World | ● [lunge] |
| ZombieGlassTap | Z1 at the window | 3D | 0.5–8 | no | 0 | World | ● [tapping on glass] |
| ImpactLight | small object lands | 3D | 0.5–10 | no | 2 × i | World | ○ [clatter] |
| ImpactHeavy | heavy object / cart | 3D | 1–14 | no | 6 × i | World | ○ [crash] |
| PlayerFootstep | smooth locomotion only | 3D (feet) | 0.3–5 | no | 2.5 | World | — |
| TeleportLand | teleport arrival | 3D | 0.3–4 | no | 1.5 | World | — |
| CaughtHeartbeat | caught sequence | 2D | — | no | 0 | Music | — |
| RoomTone | base room tone | 2D | — | yes | 0 | Ambience | — |
| Ventilation | vents (Acts 1–2) | 3D | 1–8 | yes | 0 | Ambience | — |
| DistantInfected | director one-shots, 8–15 m behind walls | 3D | 3–25 | no | 0 | Ambience | ● [distant groan] + arrow |
| TensionDrone | follows tension | 2D | — | yes | 0 | Music | — |
| BlackoutTone | Act 3 bed | 2D | — | yes | 0 | Ambience | — |
| ExteriorWind | daylight beyond the exit | 3D | 2–30 | yes | 0 | Ambience | — |
| UiSelect / UiBack / UiPause | menus | 2D, ignores pause | — | no | 0 | UI | — |
| VictimCrying (v1.4) | someone sobbing behind a door on the player's floor, in bouts | 3D | 1–14 | no | 0 | Ambience | ● [someone sobbing behind a door] |
| DistantScream (v1.4) | a scream 9–30 m away on the player's floor, every 22–50 s (not during the news) | 3D | 4–40 | no | 0 | Ambience | ● [a scream, far off] |
| CrowdPanic (v1.4) | the crowd outside the main doors, ground floor | 3D | 3–30 | yes | 0 | Ambience | ● [a panicking crowd outside] |
| NewsBroadcast (v1.4) | the ceiling TV in B-7 at the start of a run | 3D | 1.5–14 | no | 0 | Voice | the anchor's words, timed from the recording's transcript |
| JumpScareStinger (v1.4) | the exit jump scare | 2D | — | no | 0 | Music | — |
| JumpScareScream (v1.4) | the exit zombie, in the player's face | 3D | .5–12 | no | 0 | World | ● [a scream in your face] |
| RevolverShot (v1.5) | the revolver fires; the noise draws every zombie within 20 m | 3D | 2–60 | no | 20 | World | ○ [gunshot] |
| RevolverDryFire (v1.5) | the trigger pulled with all six spent | 3D | .3–6 | no | 0 | World | ○ [click] |
| RevolverHandle (v1.5) | the revolver is picked up | 3D | .3–4 | no | 0 | World | — |

`i` = the intensity argument (0..1) passed to `Cues.Play`.
**Fairness rule (tested):** every zombie vocal's Max is ≥ 9 m (1.5 × the largest common player noise radius of 6 m), so **you always hear a zombie before it can hear you.** Noise radius ≤ Max for every cue.
**Mixer:** Master → World, Voice, Ambience, Music, UI. Voice ducks Ambience and World by −6 dB. Settings volumes map to Master, World + Ambience (effects) and Voice.
**Voices:** 24 real voices, ≤ 12 spatialized. Priority (lower wins): UI 0 · Voice 32 · zombie 64 · player actions 96 · ambience 160.

---

## 7. Lighting states (B · Atmosphere)

| State | Ambient | Fog | Lights | Enter | Variation | Reduce flicker |
|---|---|---|---|---|---|---|
| Awakening | 0.10, cold blue-grey | linear 2–12 m | 2 tubes in B-7, light spilling through the door window | 4 s fade from black (fader) | one tube flickers irregularly, ≤ 2 changes/s, 60–100 % (never off) | steady 80 % |
| WardDim | 0.08 | linear 3–15 m | 3 corridor fixtures at 40 %, monitor emissive | 1.0 s | none | — |
| Surge | — | — | all fixtures ramp to 130 % | 0.6 s ramp | — | — |
| Blackout | 0.015 + brightness offset (0–0.02) | exponential 0.12 | exit-sign emissive at `A3.Beacon` only | 1.2 s fade down | — | — |
| EmergencyRed | 0.03 red | linear 2–10 m | 2 red wall lamps | 1.5 s | 0.5 Hz pulse, 50–100 % | steady 75 % |
| ExitDaylight | ramps to 1.0 white | fog → white | directional light through the exit, emissive door frame | 2 s ramp | — | — |

**Photosensitivity:** no luminance change more than 3 times in any second, anywhere (WCAG 2.3.1). No full-field flashes: the surge is a ramp, and the hard-scare spike is ≤ 0.3 s, local and non-repeating (removed when Reduce flicker is on). Teleport and failure fades use the fader, not the lights.

---

## 8. Failure and restart

* **The only failure is being caught:** a chasing zombie stays within 0.8 m for 0.25 s, or the player is inside Z4's chain reach after the snap.
* **Caught sequence:** `PlayerCaught` → `Caught` → 0.4 s fade to black (no gore, no camera motion), `CaughtHeartbeat`, caption "It heard you." → after 2.5 s total, the Ward reloads and the act is **restored to its act-start state** → 1 s fade in → Playing.
* **Pause menu:** Resume · Restart act · Restart from the beginning · Comfort & accessibility · Quit. Restart from the beginning = Awakening, fresh.
* **Complete screen:** Play again (full restart) · Quit · Credits.
* **Adaptive:** a second catch in the same act turns on that act's assist (section 5).

---

## 9. Accessibility and comfort

First launch runs **Calibration** (state `Calibration`), with every option reachable later from the pause menu:

| Option | Default | Effect |
|---|---|---|
| Standing / Seated | Standing | seated raises the virtual floor so every interactable stays in the 0.75–1.5 m band |
| Height offset | 0 | ±0.6 m fine-tune |
| Dominant hand | Right | which hand the menus and teleport favour |
| One-handed mode | off | already true of every interaction. This also moves menus to the dominant hand |
| Locomotion | Teleport only | optional smooth locomotion (controllers) with the comfort vignette on |
| Turning | Snap 45° | 30 / 45 / 60°, or smooth |
| Comfort vignette | on | during smooth locomotion and turning |
| Darkness brightness | 0.5 | calibration screen: three hospital signs at 2 %, 5 % and 10 % luminance. Adjust until the 2 % sign is barely visible |
| Scare intensity | Standard | Reduced: no stingers, no lunge, slower chases, zombie sight off, longer telegraphs |
| Reduce flicker | off | flicker and pulses become steady. No light spikes |
| Subtitles | on | Speech and ● captions. Scale 0.75–2× |
| Sound cue visuals | off | ○ captions, direction arrows, beacon floor pulse, steam and spark hints. **No objective requires hearing when this is on** |
| Puzzle assist always on | off | Assist values from the start |
| Volumes | 100 % | master, effects, voice |
| Haptics | 100 % | controllers only |

**Comfort rules (non-negotiable):** never move or rotate the camera without the player's input. No camera shake. Fades ≥ 0.15 s. Teleport uses an optional 0.15 s blink. Text ≥ 1.5° of visual angle. Every menu is reachable with either hand or either controller. Pause works at any moment in Playing or Ending.

---

## 10. Acceptance criteria (copied into `TEST_REPORT.md`)

| ID | Criterion |
|---|---|
| GD-01 | A first-time tester completes the run without being caught in 100–150 s, and a skilled run takes ≥ 80 s |
| GD-02 | Every objective can be completed with hands only, with controllers only, seated, and one-handed |
| GD-03 | Every act's restore reproduces its act-start state exactly (positions, puzzle state, lights, zombie state) |
| GD-04 | Each zombie reacts to an audible player noise inside its radius and ignores one outside it. The investigation beat in Act 2 happens on every run |
| GD-05 | Testers locate the beacon with eyes closed to within 30° (3 of 3 testers) |
| GD-06 | Exactly 2 hard scares per run in Standard mode, 0 in Reduced |
| GD-07 | No luminance change above 3 Hz anywhere in a full-run capture |
| GD-08 | The reveal plays fully in both hands mode and controllers mode, and Complete offers Play again |
| GD-09 | Pause from the menu button, the hand gesture and headset removal all freeze zombies, audio and timers. Resume continues exactly |
| GD-10 | No hint or assist fires while the player is making progress. Each ladder step fires at +10/+20/+30 s of no progress |

---

## 11. Three floors (owner request, 2026-10-31; contract v1.3)

The owner asked for three floors. The ward above (acts 1-4) is **Floor 3, the Isolation Ward**. Two floors follow it, each
reached by a stairwell. All three floors live in the same Ward scene, side by side in X, so `RoomGeometry` builds their
walls, doors and floors from `WardLayout.Rooms` like the ward's. The floors never touch, so no doorway joins them.

| Floor | Act | Rooms (x range) | Objectives | Zombies | Lighting |
|---|---|---|---|---|---|
| 3 · Isolation Ward | 1-4 | B-7 … Vestibule, **Stair3** (17-19.5) | as before, then `Act4_StairsReached` (402) | Z1-Z4 | as before; the release no longer brings daylight |
| 2 · Laboratories | 5 `Laboratory` | Stair2, LabCorridor, LabA (north), LabB (south), Stair2Down (30-48.5) | `Act5_FuseInserted` (500), `Act5_OverridePulled` (501), `Act5_StairDoorOpened` (502), `Act5_StairsReached` (503) | Z5 Researcher: dormant in Lab B, hears like Z2 | `WardDim` |
| 1 · Main Lobby | 6 `Lobby` | Stair1, Lobby, Security (north), Entrance (60-77) | `Act6_GeneratorPrimed` (600), `Act6_LockdownReleased` (601), `Act6_MainDoorsOpened` (602) | Z6 Guard: patrols across the lobby (x 70.2); Z7: dormant behind the reception desk | `EmergencyRed`, then `ExitDaylight` |

**Two-minute version (E-26, owner 2026-11-13).** The run skips floor 2: the ward's stairs lead straight down to the
lobby (section 14). Floor 2 below stays built and sealed for the bigger game.

**Floor change.** When the stair door of a floor is open and the player reaches the landing anchor (`A4.Stairs`,
`A5.Stairs`), the stairs objective completes and the director enters the next act. The player rig fades to black, moves
to the next floor's start anchor, and fades in while the floor's name is shown. A restore after a catch starts the
current floor again, never an earlier one.

**Floor 2.** Lab B's door is open and Z5 sleeps inside. The fuse lies on a bench in Lab A. Carry it to the fuse box on the
corridor's south wall (the Act 2 socket mechanic). The spark is loud, so the director answers it with a crash at the
west landing that draws Z5 out of Lab B and away from the player. The door override lever (Act 4 mechanic, 2 s hold)
works only once the fuse is in; it opens the stairwell door at x 46.

**Floor 1.** The generator valve (Act 2 valve mechanic) is in the Security office north of the lobby. The lockdown
lever by the main doors works only once the generator is primed. Z6 walks the lobby north to south and back on a line
at least 0.8 m from every anchor, pausing 3-4 s at each end; red light limits its sight to 3 m. Pulling the lever
floods the lobby with daylight; when the main doors are fully open the ending (section 4) plays in the lobby, with Z6
and Z7 kneeling.

Each new mechanic is an existing one placed again: puzzles carry their act and objective as serialized fields whose
defaults are the original Act 1-4 bindings, so the ward's puzzles are unchanged. A lever can require another objective
first (`requires`).

## 12. Horror pass (owner request, 2026-11-02; contract v1.4, integration exception E-21)

The owner asked for "real" stairs, a horde, a Resident Evil-like hospital (white tiles, blood, people crying), a jump
scare at the final exit and an opening news broadcast that holds the player. Genre and mood only: no content from any
existing game.

* **News broadcast.** A television hangs from the ceiling of B-7. When a new run starts it plays a 64 s emergency
  bulletin (a CC0 recording) with its own headlines, ticker and a failing picture. The player cannot walk away until it
  ends: on a flat screen the view turns to the set and stays there; in a headset the body is turned to it once and
  movement is held, the head stays free. The PA ducks it. A restored act does not replay it.
* **Hospital.** White tiled walls; blood on walls, floors and ceilings, and two messages written in blood.
* **Soundscape.** People sob behind doors on the player's floor, screams come from elsewhere in the building, and a
  crowd panics outside the main doors. None of it is a noise a zombie can hear. Reduced scares: fewer, quieter screams.
* **The horde (v1.4).** 28 zombies. Eight hunt with their own senses: the five story hunters and three new ones (a pack
  walking the floor-3 corridor in Act 4, a nest in Lab B, a pack by the main doors). The other 18 follow a hunter: they
  keep their place in its pack, go where it goes, and when it gives chase the pack chases too. Followers are deaf, see
  only at arm's length (1.8 m) and are slower (1.3 m/s chasing against 1.6); the Walker's two cannot see or catch. A
  follower is in play exactly while its hunter is. Ids 101–121, `HordeLayout` (AI) is authoritative.
* **Stairs.** On a flat screen the player walks down a real stairwell between floors: twenty risers (0.18 m, going 0.28 m)
  with a fire-door lobby halfway. Stepping on the stair landing still finishes the act; the top door then opens. In a
  headset the fade between floors stays (movement there is by teleport anchors only).
* **Exit jump scare.** As the main doors open, a zombie throws itself at the player's face from the doorway, with a
  stinger and a scream; the picture cuts to black for 0.75 s and the ending plays. Skipped with reduced scares. No
  controller rumble (only AI and Atmosphere may raise haptics).

## 13. The revolver (owner request, 2026-11-12; contract v1.5, integration exception E-25)

Step 4 of the owner's two-minute plan. Agreed: found on a dead police officer halfway through floor 3; six bullets fill
it once, it never reloads and there are none to find; a headshot kills, a body shot staggers for a moment; every shot
draws every zombie within about 20 m; on a flat screen the right mouse button aims and the left fires (the left still
grabs when not aiming); in a headset it is drawn from a hip holster.

* **Where.** The officer lies across the floor-3 corridor at x 6.8 (between the supply-room door and the cart), his head
  to the north wall, in a pool of blood, his cap beside him. The gun lies on the floor by his right hand. Until the
  woman's lunge and the dead doctor are built (later plan steps), he lies there alone.
* **Taking it.** Flat screen: look at it from within 2.4 m and click. Headset: grab it; let go anywhere and it goes to
  the hip holster, where it is drawn again. Taking it makes a quiet handling sound no zombie can hear.
* **Firing.** Flat screen: hold the right button (the gun comes up to the eye in 0.12 s), click the left. The bullet
  goes where the view points. Headset: the trigger; the bullet leaves the muzzle. One shot per 0.45 s at most. A
  shot is not possible while paused or in a zombie's grip (the game is not running then).
* **What a bullet does.** It stops at the first wall or object, reaches 40 m at most, and meets a zombie's head (an
  ellipsoid round the model's big head, jaw and tongue) or body (hips to neck); arms and legs do not stop it. A headshot kills: the zombie falls and lies where it
  fell until the player's next life. A body shot staggers it for 1.2 s and it snarls; it then comes on. Blood specks
  fly from a hit, dust from a wall.
* **Noise.** Every shot is the `RevolverShot` cue, and its noise (20 m) draws every zombie in hearing to where it was
  fired. The sound goes out before the bullet lands, so the zombie that is hit hears it too unless it is staggered.
  E-28 (owner, after playing: "none of the zombie is following the gun sound"): a zombie that hears a shot snarls,
  then runs to where it was fired at its chase speed and searches there; it keeps going for up to 25 s, and a second
  shot sends it on to the new spot. Any other sound is still walked to and given up after 10 s. The horde's followers,
  deaf to everything else, hear a shot too and leave their pack for it. A zombie already chasing the player keeps on.
* **Six rounds.** Nothing on screen counts them (owner rule: no instructions or counters on screen); the rounds can be
  seen in the cylinder, one fewer per shot. The seventh pull only clicks (`RevolverDryFire`, which no zombie hears).
* **A new life.** Every death sends the player back to bed (Act 1), and the gun is back by the officer, full; every
  zombie shot in that life is back too.
* **Not decided (owner):** whether the empty click should be heard by zombies, and whether shooting should be possible
  while held. Touch builds (phone, iPad) have no fire button yet. A pack whose hunter is shot stays where it stands.

## 14. Floor 2 out, torch on, real doors (owner request, 2026-11-13; integration exception E-26)

The owner: "for now concentrate the 2 min gameplay. can you just takeout the floor 2 from it", "keep the flashlilght on
default all the time, and turning it off it's the players wish", and "make the doors open like an actual door".

* **Floor 2 out.** After the ward (acts 1-4) the run goes straight to the lobby (act 6). One stairwell leads from floor 3's
  stair room down to the lobby's: a landing, nine steps, a fire-door lobby, nine steps, a landing, as before. The
  laboratories (act 5, rooms, puzzles, Z5) stay in the scene, sealed, for the bigger game; their stair rooms are no
  longer opened. Floor 3's two stair signs now read STAIRS - GROUND FLOOR and DOWN TO GROUND FLOOR / MAIN LOBBY.
* **Torch.** On from the start of play on flat screens; F (or LIGHT on touch) turns it off and on again. The power
  cut no longer kills it. Headsets still have no torch (none was ever
  built for them).
* **Doors.** The floor-3 emergency door and the lobby's main entrance are double doors: two leaves filling the 1.6 m by
  2.4 m doorway, each on a hinge at its own jamb, with a push bar on the side the player comes from. When their lever
  is pulled they swing away from the player into the room beyond (90 degrees in 1.5 s). They were one leaf turning
  about its middle, which left the doorway half blocked. The room door (B-7) was already hinged and is unchanged; the
  stairwell's fire doors slide.

## 15. The player's arms (owner request, 2026-11-13; integration exception E-27)

The owner asked whether visible hands would look good, then "Yes, find a model". On a flat screen (PC, phone, iPad)
the player now sees their own arms: CC0 arms by para (OpenGameArt, made with MakeHuman), bare forearms and hands.

* **Left hand: the torch.** A black metal torch held low on the left in a fist, the thumb on its tail cap and the lens
  coming out of the little finger's side of the fist, pointing forward (E-28: the first hold, knuckles up and the lens out
  of the index finger's side, turned the forearm 153 degrees and the owner saw the wrist twisted). Its beam
  starts at the lens (it started at a fixed point low on the right before). The lens glows while the torch is on and is
  dark glass when F (or LIGHT) turns it off. The torch rises and falls slightly with the player's breathing.
* **Right hand: the revolver.** Until the revolver is taken the right arm hangs out of sight. Taking it raises the
  arm onto the grip in 0.2 s, the hand wrapped round it, the forefinger in the trigger guard and the thumb on the far
  side (E-28: the grip now slopes back like a real revolver's; upright, it put the forefinger over the frame); aiming brings
  the gun to the middle of the view, and the torch moves a little toward the middle with it.
* **Walls.** Near a wall the torch and the gun draw back toward the player instead of going through it.
* **Not in a headset.** The controllers are the hands there (and the Quest build has no torch). The hands do not move
  on levers, planks or valves: the player still uses them with the mouse or a touch as before.
