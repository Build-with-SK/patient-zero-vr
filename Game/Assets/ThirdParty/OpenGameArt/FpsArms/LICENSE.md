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
