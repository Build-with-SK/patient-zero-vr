# Design reconciliation: Full Production Director prompt vs approved GAME_DESIGN.md

2026-10-28 · Claude (Technical Director) · **Status: proposal. `docs/GAME_DESIGN.md` is unchanged.**

The Full Production Director prompt lists four 30-second horror beats and also says *"Preserve the detailed approved
GAME_DESIGN.md timing and interaction specifications."* So the approved design wins wherever the two differ. Each
difference below is written up as an amendment the owner can accept or decline. Nothing changes in code, layout
or contracts until the owner decides.

## Beat-by-beat

| Prompt window | Prompt beat | Approved design (GAME_DESIGN.md section 4) | Verdict |
|---|---|---|---|
| 00:00–00:30 | Wake | 0:00 wake in the dark, 4 s fade from black, `HeartbeatWake` | MATCH |
| | Flashlight | No flashlight. Act 1 is lit by one flickering tube and corridor light through the door window | NOT IN DESIGN → D-1 |
| | Trolley | Bedside tray `A1.Tray` with cup, kidney dish and clipboard (physics teach). The only cart is the Act 3 bottle cart `A3.Cart`, a noise hazard | DIFFERENT → D-2 |
| | Distract zombie | Scripted `DistantCrashLure` 2 s after hard scare 1 pulls Z1 away and *teaches* that zombies follow sound. Player-made noise already works everywhere (section 2.1) | PARTLY → D-3 |
| 00:30–01:00 | Valve and cable puzzle | Act 2: valve 540° (slow is quiet), carry and plug the cable (the spark is loud) | MATCH |
| | Zombie enters the operating room | Z2 emerges from the **supply doorway** `Z2.Doorway` into the corridor, investigates, passes about 1.3 m away | LOCATION DIFFERS → D-4 |
| 01:00–01:30 | Lights fail | ~0:58 surge → blackout, all zombies stunned 2.5 s | MATCH |
| | Navigate by spatial audio | Beacon chirp every 3 s from 7 m ahead, `WaterDrip` landmark, blind Z3 crossing 0.9 m away | MATCH |
| 01:30–02:00 | Final infected encounter | Z4 restrained on a gurney; hard scare 2 lunge stops 1 m short on its chain | MATCH |
| | Emergency exit | Hold the release lever 2 s, bolts retract, exit door opens to daylight | MATCH |
| | Reveal on the hands | Ending: veins spread over the player's own hands (3–9 s), PA names Patient Zero | MATCH |

Seven of the eleven beats match. All four differences sit in the first minute, and none of them changes the master timeline.

## Proposed amendments (owner decides)

### D-1 Flashlight — recommend **decline for v1**; fallback option D-1b

- **Conflicts with the approved design.** Pillar 1 ("sound is both the threat and the map") and Act 3 ("exit sign is the only light"). Act 3 navigation by ear is a beat the prompt itself keeps; a working flashlight in Act 3 removes it.
- **Ownership.** The held object is seat A (Interaction); every light is seat B (Atmosphere). It needs a new Core contract (a light-request channel plus a layout id) and a contract amendment. Neither seat may add it alone.
- **Performance.** A held spot light is the costliest dynamic light on Quest. Budget: one extra per-pixel light, no shadows.
- **D-1b, if the owner wants a flashlight.** A dying penlight on the bedside tray. It works in Acts 1–2, flickers as Z2 investigates and dies in the surge, which turns "lights fail" into a personal loss. The timeline is unchanged. Cost: Core v1.1 (new `WardLayout` id `A1.Penlight`, a `HandheldLightChanged` channel written by Interaction and read by Atmosphere), about 0.5 day per seat, plus one dynamic light in Acts 1–2.

### D-2 Trolley — recommend **accept** (prop change, no contract change)

Replace the bedside tray with a wheeled instrument trolley at the same layout id `A1.Tray`, carrying the same items.
Pushing it rattles (`ImpactHeavy`, already in the noise table). Act 1 already says nothing is near enough to hear, so the
teach is unchanged. Owner: A (prop and physics). Budget: one prop of ≤ 1.5 k triangles.

### D-3 Distract the zombie — recommend **keep the scripted lure**, no change

The scripted lure guarantees the lesson that zombies follow sound, at a fixed time, for every player. Player-made
distraction is already a system, not a script: any thrown object makes noise (section 2.1), and Z2 (Act 2) and Z3 (Act 3)
investigate it. A player-triggered early lure in Act 1 is optional: if a heavy impact reaches the door before the scripted
lure fires, the director fires the lure early. That would be a B director change with a new test. Decline unless playtests
show players want it.

### D-4 Operating room — recommend **accept as set dressing**

Dress the Act 2 supply room as an operating theatre: double doors with porthole windows, an unlit surgical lamp, an
instrument table. `Z2.Doorway` and every coordinate stay as they are, so the timing is unchanged. Owner: B (environment
art). Budget: within the prop and draw-call budgets in ASSET_MANIFEST.md.

## Decisions needed

| # | Question | Default if no answer |
|---|---|---|
| D-1 | Flashlight: decline, or the D-1b penlight (needs Core v1.1)? | decline |
| D-2 | Wheeled trolley instead of the bedside tray? | keep the tray |
| D-3 | Player-triggered early lure in Act 1? | no |
| D-4 | Dress the Act 2 supply room as an operating theatre? | keep the supply room |

An accepted amendment is recorded in GAME_DESIGN.md with a version bump. D-1b also needs a contract amendment entry in
ARCHITECTURE.md and review by both seats, as WORK_PLAN.md requires.
