# PATIENT ZERO — Codex 2 XR systems

Isolated Unity package on `codex/xr-systems`. No gameplay scenes are included or modified.
Codex 1 can import `Packages/com.patientzero.xr` into its Unity project without adopting the test fixture.

**Status:** the portable AI/safety tests pass. Unity import, SDK prefab generation, headphone listening,
Android builds and Quest profiling have **not** run in this cloud machine. This is an implementation
and integration handoff, not a headset-validated release. The SDK rig and four interaction prefabs
are generated in Unity from SDK source prefabs; those five generated artifacts are still pending.

| Deliverable | Location / behavior |
| --- | --- |
| Shared contracts | [docs/xr/CONTRACTS.md](docs/xr/CONTRACTS.md), proposed pending Codex 1 review |
| Blind zombie AI | `Runtime/Core`, `Runtime/Unity/Zombie*.cs` inside the package |
| Noise propagation | Fixed-capacity bus, distance attenuation, per-source cooldown, stale-event rejection |
| XR safety | OpenXR hand/controller input, selection cancellation, settling delay, focus-loss and stalled-provider recovery |
| SDK interactions | `Editor/SdkPrefabBuilder.cs` wraps existing SDK rig, throwable, two-hand, valve and lever templates |
| Spatial audio | Fixed voice pool, positional sources, optional project HRTF spatializer, rate-limited occlusion |
| Audio assets | Four-group mixer; six original **diagnostic** mono clips, not production sound design |
| Prefabs | Eight serialized service/zombie/encounter templates in `Prefabs/` |
| Encounters | Five timed, cancellable sequences with visual/gameplay callbacks and a hard 2.5 m comfort floor |
| Tests | 26 shared scenarios; Unity adapter and import tests; portable CI |
| Integration | [docs/xr/INTEGRATION.md](docs/xr/INTEGRATION.md) |
| Hardware validation | [docs/xr/VALIDATION.md](docs/xr/VALIDATION.md) and [docs/xr/PERFORMANCE.md](docs/xr/PERFORMANCE.md) |

## Portable development

With .NET 8 installed, run from the repository root:

```sh
dotnet run --project Tests/Portable -c Release
python3 tools/validate_assets.py
```

On this Linux x64 cloud machine, `bash tools/setup_portable.sh` reproduces the verified .NET SDK
installation and runs both checks. It uses the official Microsoft SHA-512, retains TLS verification,
and installs outside the checkout in `/workspace/tools`. No NuGet dependencies or credentials are required.
The .NET runtime needs ordinary access to its named-mutex storage under the user's profile;
the cloud sandbox required a runtime-access escalation for the first test run.

This task already has an isolated checkout. Future tasks should use their existing checkout;
do not create a worktree unless explicitly requested.

## Unity baseline

The integration fixture is `Tests/UnityProject`, separate from Codex 1's project:

- Unity **6000.0.66f2**.
- Meta XR Interaction SDK OVR integration **207.0.0**, including core/interaction **207.0.0**.
- OpenXR **1.16.1**, XR Hands **1.7.2**, XR Management **4.5.3**, AI Navigation **2.0.9**.
- Unity Test Framework **1.6.0**.

These are the versions declared by Meta's official sample project at commit
`1ed3e2cbfac357da4cb14bdad7a9e9c001ea444d`, inspected during implementation:
[manifest](https://github.com/oculus-samples/Unity-InteractionSDK-Samples/blob/1ed3e2cbfac357da4cb14bdad7a9e9c001ea444d/Packages/manifest.json),
[lockfile](https://github.com/oculus-samples/Unity-InteractionSDK-Samples/blob/1ed3e2cbfac357da4cb14bdad7a9e9c001ea444d/Packages/packages-lock.json),
[editor](https://github.com/oculus-samples/Unity-InteractionSDK-Samples/blob/1ed3e2cbfac357da4cb14bdad7a9e9c001ea444d/ProjectSettings/ProjectVersion.txt).
This is a documented compatibility baseline, not evidence of an Android build here.
The fixture's transitive lockfile must be produced and reviewed after a successful Unity package
resolution; no unresolved lockfile has been fabricated.

Use the licensed Unity editor with Android Build Support, SDK/NDK and OpenJDK. Install packages
through Unity Package Manager's Unity registry. The current cloud network denies `packages.unity.com`;
that destination must be allowed in environment settings before package resolution here. Any redirected
artifact domains should be added only when the registry identifies them. No token is needed for the
portable workflow, and no license or secret should be committed.

See the integration guide for Android/OpenXR settings and SDK-prefab authoring. Once the editor is available:

```sh
UNITY_EDITOR=/path/to/Unity bash tools/run_unity_tests.sh
```

The runner requires at least 29 passing EditMode and 7 passing PlayMode tests, checks fresh XML results,
and rejects failed, skipped or zero-test runs. Logs and reports go to ignored `artifacts/`.
