# Asset Manifest

Version 1.1 · 2026-10-28 · Owner: Claude · Seat B proposes environment, audio and zombie entries; seat A proposes rig, hand and prop entries.

## Rules

1. **v1 ships on generated greybox and code-made placeholders.** Every row has a fallback that needs no external file. External assets are upgrades, never dependencies.
2. **No paid assets.** None are assumed to be available.
3. **Nothing is imported until its row is complete.** That means a source URL, the licence as shown on the download page, an attribution requirement, the date verified, and who verified it. The licence names below are what each source is generally known to publish. **None has been verified for this project yet.** The *Verified* column stays empty until someone checks the actual download page.
4. **Rejected licences:** anything Non-Commercial (CC BY-NC), No-Derivatives, "editorial only", unknown, or "free for personal use". CC BY is accepted only with its attribution added to `docs/CREDITS.md` (created on the first CC BY import).
5. **Status vocabulary:** NOT IMPORTED · CANDIDATE (licence verified, not yet imported) · IMPORTED · REJECTED · GENERATED (made by our build steps or code).
6. Raw source files that a licence forbids redistributing (e.g. Mixamo) stay out of git. Commit only the converted, in-game asset, and only to a **private** repository.
7. **This repository is public** (`Build-with-SK/patient-zero-vr`, checked 2026-10-28). Rule 6 therefore excludes, for as long as it stays public: Mixamo, Unity Asset Store content, Sonniss bundle sounds and Soundly library sounds, in raw **or** converted form. Only sources whose licence allows redistribution (CC0; CC BY with credit) may be committed. Making the repository private would reopen rule 6; that is the owner's decision.
8. **Quest-safe visuals.** No URP Decal renderer feature, screen-space effects or real-time shadows from dynamic lights. Blood, grime and signage are mesh decals: alpha-tested quads with a shared atlas.

## Source licence check (site level, read 2026-10-28 by Claude)

This checks what each **site** publishes. Each file must still be checked on its own download page at import (rule 3).

| Source | Licence as stated | Commercial | Attribution | May the files be committed to this public repo? | Page read |
|---|---|---|---|---|---|
| Poly Haven | "Our assets are all licensed as CC0" | yes | no | yes ("You can redistribute them") | polyhaven.com/license |
| ambientCG | CC0 1.0 Universal | yes | no | yes ("include the raw files in your project") | docs.ambientcg.com/license |
| Kenney | "public domain licensed (CC0)" | yes | no | CC0 permits it. Read the licence file inside each download | kenney.nl/support, asset pages |
| Quaternius | "CC0" on the two pack pages read; no site-wide statement found | yes (pack pages) | not stated | only if the pack page says CC0. Check per pack | two pack pages |
| Freesound | set per sound: CC0, CC BY, or CC BY-NC | CC0, BY: yes; BY-NC: **no** | BY: yes | CC0 yes; BY with `CREDITS.md`; BY-NC rejected (rule 4) | freesound.org/help/faq |
| Sonniss GDC bundle | "#GAMEAUDIOGDC Bundle Licensing Agreement" v2.0 (27 Aug 2026) | yes | no | **no**: may not supply the sounds "as sound effects" to anyone; re-designed sounds stay licensed. AI training forbidden | sonniss.com/gdc-bundle-license |
| Mixamo | **not verified**: Adobe's FAQ returned HTTP 403 | — | — | **no** while the repo is public (rule 7) | helpx.adobe.com (failed) |

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
| P-01 | Universal RP 17.3.0 (core, fixed by 6000.3) | rendering | Unity registry | Unity Companion Licence | none | — | PINNED in `Packages/manifest.json`, not yet resolved by Unity | none (core) |
| P-02 | OpenXR 1.19.0 + OpenXR: Meta 2.7.0 + XR Management 4.7.0 | XR runtime | Unity registry | Unity Companion Licence | none | features enabled by `QuestBuild.Configure`. OpenXR: Meta 2.7.0 requires XR Management 4.7.0, although the registry's `latest` tag still reads 4.6.1 | PINNED, not yet resolved | none (core) |
| P-03 | XR Interaction Toolkit 3.7.0 (+ Starter Assets and XR Interaction Simulator samples) | interaction, rig, desktop simulation | Unity registry | Unity Companion Licence | none | 3.x API differs from 2.x tutorials. `PatientZero/Desktop Simulation` imports the samples | PINNED, not yet resolved | none (core) |
| P-04 | XR Hands 1.10.0 (+ HandVisualizer sample) | hand tracking, hand meshes | Unity registry | Unity Companion Licence | none | the hand mesh must accept the infection-reveal material | PINNED, not yet resolved | capsule-joint hands generated in code |
| P-05 | Input System 1.20.1 | input | Unity registry | Unity Companion Licence | none | — | PINNED, not yet resolved | none (core) |
| P-06 | AI Navigation 2.0.16 | NavMesh | Unity registry | Unity Companion Licence | none | — | PINNED, not yet resolved | none (core) |
| P-07 | Test Framework 1.6.0 (core, fixed by 6000.3) | EditMode/PlayMode tests | Unity registry | Unity Companion Licence | none | — | PINNED, not yet resolved | logic harness only |
| P-08 | ProBuilder (optional) | greybox editing | Unity registry | Unity Companion Licence | none | build steps can use primitives instead | NOT IMPORTED | primitives |
| P-09 | Meta XR Audio SDK (`com.meta.xr.sdk.audio` 85.0.0, held back in `tools/unity/package_policy.json`) | HRTF spatializer | Meta developer site / Unity Asset Store (free) | Meta Platform Technologies SDK licence | check the licence text | **the owner must accept the licence.** HRTF CPU cost per voice; cap 12 spatialized voices | NOT IMPORTED | Unity built-in 3D panning (no HRTF) |
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
| E-09 | Ceiling tubes, emergency red lamps, exit sign housing | flicker, surge, `EmergencyRed`, `ExitDaylight` | fixture meshes built in code (B, Environment); emissive materials and light behaviour (B, Atmosphere) | ours | none | within `PERFORMANCE_REPORT.md`: ≤ 6 realtime lights in Ward, 0 shadow-casting. Flicker drives emissive intensity and never strobes (pillar 4) | GENERATED (not built yet) | emissive-only fixtures |
| E-10 | Blood smears, drag marks, scratches, grime, chain scrape marks | dread; makes Z4's reach readable | procedural splat textures (noise + falloff) in one 1024² atlas on alpha-tested quads (B). Optional CC0 upgrade from Poly Haven / ambientCG | ours / CC0 | none | rule 8: mesh decals only, 2 mm offset against z-fighting. Gore stays non-graphic; reduced-scare mode lowers it | GENERATED (not built yet) | is the fallback |
| E-11 | Interaction affordances: grab rim on the valve, plug glow, lever stripes, hover highlight | readable interactables | materials and outline shells built in code (A) | ours | none | the highlight must not rely on colour alone (accessibility). Readable in `Blackout` without counting as lighting | GENERATED (not built yet) | is the fallback |

## C. Characters and animation (seat B)

| ID | Asset | Purpose | Source | Licence (to verify) | Attribution | Concerns | Status | Fallback |
|---|---|---|---|---|---|---|---|---|
| C-01 | Humanoid zombie model (one rig, two material variants) | Z1–Z4 | candidate 1: a CC0 humanoid (Quaternius or Kenney); **confirm a suitable model exists before planning around it** | CC0 as published | none required | likely stylized, which may undercut horror; mitigated by darkness and silhouette | NOT IMPORTED | C-03 |
| C-02 | Humanoid zombie model + animations | Z1–Z4 | candidate 2: Mixamo (Adobe) | Adobe's Mixamo terms (generally known: free use with an Adobe account; raw files may not be redistributed). **Not verified: Adobe's FAQ returned HTTP 403 on 2026-10-28** | unknown | needs the owner's Adobe account (I can't create accounts). 10–40 k triangles and about 65 bones, so decimate to ≤ 8 k triangles and ≤ 40 bones | REJECTED while the repository is public (rule 7) | C-03 |
| C-03 | Generated mannequin zombie | Z1–Z4 | capsule/primitive humanoid built in code, with a procedural shamble (sway, head tilt, dragging foot) | ours | none | relies on sound and silhouette, which suits the darkness | GENERATED (not built yet) | is the fallback |
| C-04 | Animations: shamble, idle sway, sniff/search, snarl, lunge, restrained thrash, kneel | zombie behaviour | from C-01 or C-02's source; otherwise procedural | per source | per source | the restrained thrash and kneel may need to be authored | NOT IMPORTED | procedural motion in AI runtime |

## D. Audio (seat B; every `CueId` in `GAME_DESIGN.md` section 6)

Order of preference for each cue: **(1)** a code-generated placeholder (always exists) → **(2)** own recording or synthesis → **(3)** Freesound, **CC0 only** (CC BY accepted with credit; CC BY-NC rejected) → **(4)** Sonniss GDC Game Audio Bundle (royalty-free, but its v2.0 licence forbids supplying the sounds to others, so **not in this public repository**; rule 7) → **(5)** the owner's own Soundly library (same restriction while the repository is public). Soundly is installed on this machine, but **its sounds are usable only under the owner's Soundly licence tier. The owner must confirm that before any import; it is not assumed.**

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
| T-03 | Unity tooling (`tools/unity`: package policy and validator, batchmode runner) and the Integration editor scripts | written for this project | project licence | GENERATED |

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
