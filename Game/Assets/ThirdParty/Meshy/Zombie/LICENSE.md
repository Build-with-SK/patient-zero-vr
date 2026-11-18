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
