# Asset Manifest

Version 1 · 2026-10-28 · Owner: Claude · Seat B proposes environment, audio and zombie entries; seat A proposes rig, hand and prop entries.

## Rules

1. **v1 ships on generated greybox and code-made placeholders.** Every row has a fallback that needs no external file. External assets are upgrades, never dependencies.
2. **No paid assets.** None are assumed to be available.
3. **Nothing is imported until its row is complete.** That means a source URL, the licence as shown on the download page, an attribution requirement, the date verified, and who verified it. The licence names below are what each source is generally known to publish. **None has been verified for this project yet.** The *Verified* column stays empty until someone checks the actual download page.
4. **Rejected licences:** anything Non-Commercial (CC BY-NC), No-Derivatives, "editorial only", unknown, or "free for personal use". CC BY is accepted only with its attribution added to `docs/CREDITS.md` (created on the first CC BY import).
5. **Status vocabulary:** NOT IMPORTED · CANDIDATE (licence verified, not yet imported) · IMPORTED · REJECTED · GENERATED (made by our build steps or code).
6. Raw source files that a licence forbids redistributing (e.g. Mixamo) stay out of git. Commit only the converted, in-game asset, and only to a **private** repository.

## Budgets that apply to every visual asset

| Item | Budget |
|---|---|
| Zombie (one per act, never two on screen) | ≤ 8 k triangles, 1 skinned mesh, 1 material, ≤ 40 bones, texture ≤ 1024² ASTC 6×6 |
| Prop | ≤ 1.5 k triangles, shares a material where possible, texture ≤ 512² |
| Whole Ward visible at once | ≤ 250 k triangles, ≤ 100 draw calls |
| Textures total in memory | ≤ 200 MB |
| Audio loaded | ≤ 48 MB. Loops stream as Vorbis. Short effects are ADPCM, decompressed on load. Spatialized clips are mono |

## A. Engine packages (Unity registry and Meta)

| ID | Asset | Purpose | Source | Licence (to verify) | Attribution | Concerns | Status | Fallback |
|---|---|---|---|---|---|---|---|---|
| P-01 | Universal RP | rendering | Unity registry | Unity Companion Licence | none | — | NOT IMPORTED | none (core) |
| P-02 | OpenXR plugin + OpenXR: Meta | XR runtime | Unity registry | Unity Companion Licence | none | Meta features must be enabled per section 3 of the review checklist | NOT IMPORTED | none (core) |
| P-03 | XR Interaction Toolkit ≥ 3.5 (+ Starter Assets sample) | interaction, rig, Editor simulator | Unity registry | Unity Companion Licence | none | 3.x API differs from 2.x tutorials | NOT IMPORTED | none (core) |
| P-04 | XR Hands ≥ 1.8 (+ HandVisualizer sample) | hand tracking, hand meshes | Unity registry | Unity Companion Licence | none | the hand mesh must accept the infection-reveal material | NOT IMPORTED | capsule-joint hands generated in code |
| P-05 | Input System | input | Unity registry | Unity Companion Licence | none | — | NOT IMPORTED | none (core) |
| P-06 | AI Navigation | NavMesh | Unity registry | Unity Companion Licence | none | — | NOT IMPORTED | none (core) |
| P-07 | Test Framework | EditMode/PlayMode tests | Unity registry | Unity Companion Licence | none | — | NOT IMPORTED | logic harness only |
| P-08 | ProBuilder (optional) | greybox editing | Unity registry | Unity Companion Licence | none | build steps can use primitives instead | NOT IMPORTED | primitives |
| P-09 | Meta XR Audio SDK | HRTF spatializer | Meta developer site / Unity Asset Store (free) | Meta Platform Technologies SDK licence | check the licence text | **the owner must accept the licence.** HRTF CPU cost per voice; cap 12 spatialized voices | NOT IMPORTED | Unity built-in 3D panning (no HRTF) |
| P-10 | TextMeshPro essentials (LiberationSans SDF) | all text | ships with Unity uGUI | Liberation fonts: SIL Open Font Licence 1.1 | include the licence text if shipped | — | NOT IMPORTED | — |

## B. Environment and props (seat B builds the environment, seat A the interactable props)

| ID | Asset | Purpose | Source | Licence (to verify) | Attribution | Concerns | Status | Fallback |
|---|---|---|---|---|---|---|---|---|
| E-01 | Ward greybox (rooms, walls, doors, corridor) | the level | our Environment build steps, from `WardLayout.Rooms` | ours | none | ≤ 30 k triangles | GENERATED (not built yet) | is the fallback |
| E-02 | Modular hospital interior kit (optional art pass) | nicer walls and fixtures | candidates: Kenney or Quaternius modular interiors | CC0 as published | none required | check that a hospital-like kit actually exists; style mismatch | NOT IMPORTED | E-01 |
| E-03 | Surface textures: tile, concrete, painted metal, grime | materials | Poly Haven / ambientCG | CC0 as published | none required | download 1–2 K and import at 512–1024 | NOT IMPORTED | flat colours + vertex-colour grime |
| E-04 | Exit sign, monitor screen, wristband text, scratched note | narrative text | TextMeshPro in world space | ours | none | — | GENERATED (not built yet) | — |
| E-05 | Hospital bed, tray, cup, kidney dish, clipboard | Act 1 physics teach | primitives built in code (A) | ours | none | colliders must match visuals | GENERATED (not built yet) | — |
| E-06 | Planks ×3, valve wheel, junction box, socket, cable plug, release lever, lock bolts, exit door | puzzles | primitives built in code (A) | ours | none | the valve rim needs grab-friendly thickness (≥ 3 cm) for hand tracking | GENERATED (not built yet) | — |
| E-07 | Bottle cart, gurney with chain, ceiling PA speakers, vents | Acts 2–4 dressing | primitives built in code (B) | ours | none | the chain must read clearly as 1.3 m reach | GENERATED (not built yet) | — |
| E-08 | Vein mask for the infection reveal | ending | generated procedurally by A's build step (noise texture 512²) | ours | none | 1 extra texture | GENERATED (not built yet) | colour tint only |

## C. Characters and animation (seat B)

| ID | Asset | Purpose | Source | Licence (to verify) | Attribution | Concerns | Status | Fallback |
|---|---|---|---|---|---|---|---|---|
| C-01 | Humanoid zombie model (one rig, two material variants) | Z1–Z4 | candidate 1: a CC0 humanoid (Quaternius or Kenney); **confirm a suitable model exists before planning around it** | CC0 as published | none required | likely stylized, which may undercut horror; mitigated by darkness and silhouette | NOT IMPORTED | C-03 |
| C-02 | Humanoid zombie model + animations | Z1–Z4 | candidate 2: Mixamo (Adobe) | Adobe's Mixamo terms: free use in projects with an Adobe account. **Raw files may not be redistributed** | none known; verify | needs the owner's Adobe account (I can't create accounts). 10–40 k triangles and about 65 bones, so decimate to ≤ 8 k triangles and ≤ 40 bones; private repo only | NOT IMPORTED | C-03 |
| C-03 | Generated mannequin zombie | Z1–Z4 | capsule/primitive humanoid built in code, with a procedural shamble (sway, head tilt, dragging foot) | ours | none | relies on sound and silhouette, which suits the darkness | GENERATED (not built yet) | is the fallback |
| C-04 | Animations: shamble, idle sway, sniff/search, snarl, lunge, restrained thrash, kneel | zombie behaviour | from C-01 or C-02's source; otherwise procedural | per source | per source | the restrained thrash and kneel may need to be authored | NOT IMPORTED | procedural motion in AI runtime |

## D. Audio (seat B; every `CueId` in `GAME_DESIGN.md` section 6)

Order of preference for each cue: **(1)** a code-generated placeholder (always exists) → **(2)** own recording or synthesis → **(3)** Freesound, **CC0 only** (CC BY accepted with credit; CC BY-NC rejected) → **(4)** Sonniss GDC Game Audio Bundle (royalty-free; check that year's bundle licence) → **(5)** the owner's own Soundly library. Soundly is installed on this machine, but **its sounds are usable only under the owner's Soundly licence tier. The owner must confirm that before any import; it is not assumed.**

| ID | Cues | Purpose | Source plan | Licence (to verify) | Attribution | Concerns | Status | Fallback |
|---|---|---|---|---|---|---|---|---|
| A-01 | all 47 `CueId`s | every sound in the game | `PlaceholderClipFactory` in Audio: tones, filtered noise, envelopes generated at load | ours | none | must exist before any real asset, so the game and tests have sound from day one | GENERATED (not built yet) | is the fallback |
| A-02 | BeaconChirp, UiSelect/Back/Pause, HeartbeatWake, CaughtHeartbeat, TensionDrone, RevealDrone | synthesizable cues | synthesized (A-01 tuned, or offline synthesis) | ours | none | the chirp needs a broadband attack so it localises well. Pure sine waves localise poorly | NOT IMPORTED | A-01 |
| A-03 | Zombie vocals: Breath, Groan, Snarl, Sniff, Lunge, GlassTap, Shuffle | creature voice | own recording (processed human voice) or Freesound CC0 | per file | per file | mono, 48 kHz; loops must be seamless | NOT IMPORTED | A-01 |
| A-04 | Foley: plank creak/pop, valve squeal, steam hiss, cable drag, plug spark, impacts, door, lever grind, strap snap | physical actions | Freesound CC0 / Sonniss / own recording | per file | per file | the squeal must stay continuous when pitch- or volume-modulated by intensity | NOT IMPORTED | A-01 |
| A-05 | Ambience: room tone, ventilation, fluorescent buzz, monitor beep, drip, alarm, exterior wind, blackout bed | atmosphere | Freesound CC0 / Sonniss / synthesis | per file | per file | streamed loops; check loop points | NOT IMPORTED | A-01 |
| A-06 | PA lines ×3 (`PaGarbledBreach`, `PaCarrierWarning`, `PaPatientZero`) | narrative | owner or volunteer recording, processed through a band-pass "tannoy" chain | ours (with the speaker's written OK) | credit the voice if they wish | **no text-to-speech voice unless its commercial licence is verified.** Captions carry the meaning anyway | NOT IMPORTED | captions + processed noise bursts |

## E. Code and tools

| ID | Asset | Source | Licence | Status |
|---|---|---|---|---|
| T-01 | Logic test harness and UnityEngine/NUnit stand-ins (`tools/logic_tests`) | written for this project | project licence (owner to choose) | GENERATED |
| T-02 | Review checker (`tools/review/check_rules.py`) | written for this project | project licence | GENERATED |

## Import checklist (copy into the import commit message)

```
Asset ID:            (e.g. A-03)
File(s):             Game/Assets/PatientZero/<Area>/Imported/<...>
Source URL:
Licence as shown:    (exact name, copied from the page)
Verified on / by:    YYYY-MM-DD / <seat>
Attribution added:   docs/CREDITS.md line <n> | not required
Budgets met:         tris / bones / texture / audio format
Fallback still works: yes (asset removed -> game still runs)
```
