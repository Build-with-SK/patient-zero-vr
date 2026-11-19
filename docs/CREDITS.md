# PATIENT ZERO — credits and asset licences

Gameplay: lavanyahd. Technical direction: Build-with-SK. AI, audio and environment: seat B. Unity XR Interaction Toolkit. Surface textures generated in-project.

## Meshy/Zombie

# Zombie — made with Meshy (CC BY 4.0)

* Made with Meshy (https://www.meshy.ai) on the owner's free account, 2026-11-18: image-to-3D with the Meshy 6 Lite model
  (remixed from a zombie the owner picked from Meshy's community page), textured, rigged as a humanoid and animated in
  Meshy's web app. Downloaded with the owner's approval as one glTF 2.0 binary (Mixamo skeleton, all animations).
* Licence: Creative Commons Attribution 4.0 International (https://creativecommons.org/licenses/by/4.0/), the licence
  Meshy states for models made on its free plan. Credit: **"Zombie" made with Meshy (meshy.ai), CC BY 4.0.**
* Changed from the download: Meshy's image-to-3D left a 1.3 m square display stand fused under the feet, and rigging bent
  it with the legs. Every triangle lying wholly below the stand's top (0.1076 m) was removed, with the vertices only
  those triangles used (100,804 to 87,340 triangles; 61,078 to 51,080 vertices). Nothing else was changed.
  The script that did it is `tools/art/glb_cut_stand.py`.
* Download: 10,567,720 bytes, SHA-256 7c4a4827a728e66794df3d9a295d37528d2751ed2a6d1da1187d5eb88af34b2b.
  This file: SHA-256 cb6e0740333039f4c531a1c0c5f2e869d624ee24987b1fe26cb9401542b692c9.
* Contents: skinned mesh `Mesh_0` on a 66-joint Mixamo-named skeleton (`mixamorig:Hips`, `mixamorig:Head`, ...), one
  material with 2048 x 2048 colour, normal and metal/roughness maps (JPEG), animations Idle, Walking, Unsteady_Walk,
  Running, Attack and Dead. The game uses Idle, Unsteady_Walk, Running and Dead (Integration/Editor/ZombieArt.cs).
* Meshy's free plan allows 10 downloads a month; this used 1.


## OpenGameArt/Crowd

# Crowd panic (CC0 1.0, public domain)

* `CrowdPanic_StarNinjas.ogg`: "Crowd Shouting/Speaking Ambience" (`crowd_shouting.ogg`) by StarNinjas,
  https://opengameart.org/content/crowd-shoutingspeaking-ambience (CC0; the author appreciates a credit, given in the game's credits)
* Downloaded: 2026-11-02 from OpenGameArt.org, with the owner's approval.
* Changes made here: trimmed, mixed to mono, peak-normalised to -1 dBFS, 10-30 ms fades, re-encoded as Ogg Vorbis 44.1 kHz (ffmpeg).


## OpenGameArt/Crying

# Crying (CC-BY 3.0: attribution required)

* `Crying_JeanMyna_VA.ogg`: "Crying female low tone Voice Over | Vocal sound for character" (`4_cry.ogg`) by **JeanMyna_VA**,
  https://opengameart.org/content/crying-female-low-tone-voice-over-vocal-sound-for-character
* Licence: Creative Commons Attribution 3.0 Unported, https://creativecommons.org/licenses/by/3.0/
* Downloaded: 2026-11-02 from OpenGameArt.org, with the owner's approval.
* Changes made here: trimmed, mixed to mono, peak-normalised to -1 dBFS, 10-30 ms fades, re-encoded as Ogg Vorbis 44.1 kHz (ffmpeg).
* Attribution as shipped (credits screen and docs/RELEASE_NOTES.md): "Crying voice by JeanMyna_VA (CC-BY 3.0), modified".
  Keep that credit wherever this file ships.


## OpenGameArt/FpsArms

# First-person arms (CC0 1.0, public domain)

* "fps arms (rigged only)" by para, https://opengameart.org/content/fps-arms-rigged-only (licence stated there: CC0).
  The author credits the MakeHuman team: the mesh and texture were made with MakeHuman, then cleaned up, re-rigged and
  re-baked by para. Both are credited in the game's credits.
* Downloaded: 2026-11-13 from OpenGameArt.org, with the owner's approval: `fps arms.7z`, 1,406,250 bytes, holding two
  .blend files, two .fbx files and `new_diff.png`.
* Kept here, renamed: `FPS ARMS RIG 1.fbx` (the T-pose; FBX 7.4 binary, 434,092 bytes) as `FpsArms.fbx`, and
  `new_diff.png` (1024 x 1024) as `FpsArms_Skin.png`. Not kept: the two .blend files and `FPS ARMS RIG 1 test.fbx`
  (the author's own "very crude" sample animation).
* Contents: one skinned mesh, `caucasian_male_1:Body` (4,103 vertices, 4,076 faces, ~8,000 triangles), with 50 skin
  bones: clavicle, deltoid, upper arm and forearm for each arm, and a hand on its own IK handle (`hand.L.control`,
  `hand.R.control`) with palm, finger and thumb bones. The IK and finger-curl constraints of the .blend file do not
  survive in FBX: the game poses the arms itself (`PatientZero/Integration/Runtime/FirstPersonArms.cs`).
* CC0: no attribution required; credited in the game's credits anyway.


## OpenGameArt/News

# Emergency news broadcast (CC0 1.0, public domain)

* `ZombieNews_Zisongbr.ogg`: "Zombie news in Radio" (`radio_news_zombies.mp3`), uploaded by zisongbr,
  https://opengameart.org/content/zombie-news-in-radio. The page's notice names OrbitStudios and Fernando Ferreira;
  both are credited in the game's credits.
* Downloaded: 2026-11-02 from OpenGameArt.org, with the owner's approval.
* Changes made here: trimmed to the first 64 s (the speech ends at about 61 s; the static fades out by 64 s), mixed to mono, peak-normalised to -1 dBFS, 10-30 ms fades, re-encoded as Ogg Vorbis 44.1 kHz (ffmpeg).
* `ZombieNews_Zisongbr_Short.ogg` (2026-11-11, the owner asked for a short bulletin): cut from `ZombieNews_Zisongbr.ogg` here,
  17.95-29.72 s then 37.0-42.6 s (three sentences, cut in the quietest gaps of a loudness scan), 30-60 ms fades and a 0.3 s
  fade at the end, re-encoded as Ogg Vorbis 44.1 kHz mono (ffmpeg). 17.4 s. No person has listened to check the cuts.
* The captions (the transcript in `PatientZero/Audio/Editor/RecordedCueCatalog.cs`) were transcribed from this recording on this machine
  (faster-whisper small, run locally). No person has listened to check them: NOT VERIFIED.


## OpenGameArt/Screams

# Screams (CC0 1.0, public domain)

| File(s) | Author | Page | Licence |
|---|---|---|---|
| `Scream_WuxiaScrub_1..4.ogg` (cut from `screams.ogg`) | WuxiaScrub | https://opengameart.org/node/131040 | CC0 |
| `Scream_NocturnalVanguard.ogg` (`female_scream_1.ogg`) | Nocturnal_Vanguard | https://opengameart.org/node/147410 | CC0 |
| `Scream_Pauliuw.ogg`, `Scream_Pauliuw_Reverb.ogg` (`high_pitch_scream.mp3`, `high_pitch_scream_gverb.mp3`) | pauliuw | https://opengameart.org/content/high-pitch-scream-sounds2 | CC0 |

* Downloaded: 2026-11-02 from OpenGameArt.org, with the owner's approval.
* Changes made here: trimmed, mixed to mono, peak-normalised to -1 dBFS, 10-30 ms fades, re-encoded as Ogg Vorbis 44.1 kHz (ffmpeg).
* CC0: no attribution required; credited in the game's credits anyway.


## OpenGameArt/Stingers

# Horror stingers (CC0 1.0, public domain)

* `Stinger_Psychhead_VeryBassy2.ogg`, `Stinger_Psychhead_VeryBassy4.ogg`, `Stinger_Psychhead_High3.ogg`
  (`Very Bassy 2.wav`, `Very Bassy 4.wav`, `High 3.wav` from `horror_hit_soundpack_1.zip`) by psychhead_,
  https://opengameart.org/content/horror-hit-soundpack-1 ("Public Domain - CC0")
* Downloaded: 2026-11-02 from OpenGameArt.org, with the owner's approval.
* Changes made here: trimmed, mixed to mono, peak-normalised to -1 dBFS, 10-30 ms fades, re-encoded as Ogg Vorbis 44.1 kHz (ffmpeg).


## OpenGameArt/ZombieVoices

# Zombie voices (CC0 1.0, public domain)

| File(s) | Author | Page | Licence |
|---|---|---|---|
| `Moan_Darsycho_1..5.ogg` (cut from `darsycho__zombie-moans.ogg`) | Darsycho | https://opengameart.org/content/zombie-moans | CC0 |
| `Moan_EmoPreben_1..3.ogg` (`ZombieMoan1..3.wav` from `zombiemoans_-_wav.zip`) | EmoPreben | https://opengameart.org/content/zombie-moans-01-by-emopreben | CC0 |
| `Yell_ArcadeParty_1..10.ogg`, `Growl_ArcadeParty_1..4.ogg` (`zombieYell1..10.wav`, `zombieDeath1..4.wav`) | ArcadeParty | https://opengameart.org/content/zombie-skeleton-monster-voice-effects | CC0 |

* Downloaded: 2026-11-02 from OpenGameArt.org, with the owner's approval.
* Changes made here: trimmed, mixed to mono, peak-normalised to -1 dBFS, 10-30 ms fades, re-encoded as Ogg Vorbis 44.1 kHz (ffmpeg).
* CC0: no attribution required; credited in the game's credits anyway.


## Quaternius/AnimatedZombie

# Animated Zombie — Quaternius (CC0 1.0, public domain)

* Author: Quaternius (https://quaternius.com)
* Pack page: https://quaternius.com/packs/animatedzombie.html (licence stated there: CC0)
* File source: poly.pizza, https://static.poly.pizza/c4002f69-6979-42e8-ad6e-2f4e14fc3a9d.glb (CC0, author Quaternius)
* Downloaded: 2026-10-30, with the owner's approval. 981,672 bytes, glTF 2.0 binary (generator FBX2glTF v0.9.7).
* Contents: skinned meshes `Zombie` and `Eyelid` (one 50-joint armature), texture `Zombie_Atlas.png`, animations
  Crawl, Death, HitReact, Idle, Idle_Attack, Jump, Jump_Idle, Jump_Land, No, Punch, Run, Run_Arms, Run_Attack, Walk,
  Wave, Yes (each also exported under `CharacterArmature|<name>`).
* CC0: no attribution required; credited in the game's credits and release notes anyway.

Unity needs a glTF importer to use this file (planned: com.unity.cloud.gltfast 6.20.0 from packages.unity.com).
Until then Unity keeps it as a plain file.
