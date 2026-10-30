# Final gameplay and multiplatform handoff — 2026-10-30

Branch: `integration/xr-systems`. Starting delivery: `0f4a820`. Main is still `44ea7b7` (Core v1.1).
This is implemented and portable-tested B work for the canonical game. It is **not a validated PC,
Android or iPad release**: this cloud host has no Unity Editor, and final integration/builds belong to TD.
No owner Windows build, protected-main merge, force push, settings write or untracked-file cleanup occurred.

## Delivered

- Reusable, opt-in non-VR touch router, standard Input System device writer and retained mobile HUD.
  Independent movement/look fingers, safe areas, landscape layout, scaling, cancellation, hold/release,
  puzzle swipes, bottle/cable aiming, throw, flashlight, pause/resume, calibration and Complete replay.
  Default teleport settings remain useful through STEP or a forward joystick release. Smooth movement
  uses the existing A settings/locomotion choice. No movement, grab, socket or objective implementation
  was duplicated in B.
- Phone/tablet input refuses XR startup, active XR loaders/devices and standard XRSimulated device layouts.
  All mobile input is off by default. It releases gameplay input during fades, pause, focus loss, resize
  and restore; paused resume remains available under a fade. It restores only the hardware devices it
  temporarily replaced. Native callback ordering/device injection still needs Unity verification.
- A headlamp for the configured mobile profile, or optional PC F shortcut. Reuses existing fixture 0,
  separates its emitter from static housing/lens geometry, fades over 0.15 s, rate-limits toggles and dies
  at the surge. Blackout/ending lighting cannot be bypassed. No seventh light or VR hand-input contract.
- Configurable 8–24 active voices; phone generation selects 16 voices and built-in spatial panning.
  Speech/UI protection, cue/noise/caption routing and volume controls are preserved. Default Quest/PC
  budget stays 24 voices/12 optional spatializers. Actual mixed peaks/native memory still need profiling.
- Blocked patrols wait two seconds and try the other endpoint. Failed return-home paths stop the infinite
  home request while leaving hearing/reactivity available. Existing hearing, sight, chase, caught and
  distraction rules remain tested; no attack/health system or additional hard scare was invented.
- Broken ceiling panel/recess, abandoned bag/chart and damaged door jambs, taking the tested dressing plan
  from 33 to 49 pieces. Collider/route tests retain door, puzzle, marker and critical-path clearances.
  Materials/meshes stay procedural and shared; no external licence assumption or binary asset import.
- Native mapping/lifetime fixture for keyboard/mouse state and further editor-generation assertions.
  These are written but **not executed** in Unity. Saved-hospital validation now checks the headlamp
  link, all six lens bindings and the independent emitter needed to avoid moving ceiling meshes.

## Shared input coordination and remaining player bugs

Full profile/controls instructions are in [Mobile/README.md](Mobile/README.md). B did not edit Core,
Player, Interaction, Flow, UI, Packages or ProjectSettings. Claude must review
`Validation/mobile-desktop-compatibility.patch`, which adds two small changes to A's existing adapter:

1. Guarded R -> `GameFlow.RestartRun()` while paused. Until it is integrated and the mobile restart
   define is enabled, the pause restart button is deliberately unavailable. Complete replay already
   uses the approved Core state request and does not need this shortcut.
2. Sync cached desktop yaw/pitch after ActStarted, before the next look delta. Reproduction from source:
   restore Machinery (rig faces 90°), then drag look while the desktop driver still caches an earlier
   yaw. The existing code can snap the camera; B cannot correct that cache through input translation.
   The proposal defers sync until the next Update so all ActStarted subscribers have placed the rig.

The combined patch applies cleanly to `codex/gameplay` `cefbd3d` and `integration/round2` `d4de1a1`
in temporary indexes. It was **not** applied to their real worktrees/branches. Native restore/input tests
must verify it. Existing four native compilation follow-up proposals in DESKTOP_PLAYTEST.md still need
TD reconciliation against the chosen integration tip; do not blindly reapply old patches.

## Interaction audit

| Feature | Current implementation / mobile route | Integration status |
| --- | --- | --- |
| Flashlight | B headlamp F; touch LIGHT; Acts 1–2 only | Implemented; regenerate Ward and opt into the non-VR profile; native visuals pending |
| Planks / valve / lever | Existing A LMB grip + mouse-delta constrained puzzles; mobile HOLD and swipes | No A source changed; physical feel requires native play |
| Props / bottles | Existing A PhysicsProp; mobile HOLD, AIM OBJECT and THROW | Q preserves left hold for the throw frame to prevent early drop; native mapping test added |
| Cable / socket | Existing A carry/snap mechanics; aim while holding | No separate socket logic; native end-to-end touch test needed |
| Trolley | A owns the only physical trolley; B keeps its marker | Earlier duplicate geometry remains removed |
| Doors | A consumes Core v1.1 gate; B remains sole gate writer | Normal opening/objective paths retained; verify rebuilt doors and door-only layer |
| Movement | Existing A keyboard/anchor driver | Default teleport STEP works; joystick smooth movement uses the existing settings option |
| Pause / calibration | Existing A Escape / Enter handlers | Mobile translation implemented, fade/focus safety tested in Logic |
| Mid-act restart | Proposed A R shortcut | TD review required; hidden until enabled |
| Ending / replay | Existing director + A ending; Core Complete -> Playing replay | Golden-run/restart tests retained; native full run required |
| Zombie hit feedback | Approved game is caught-only, with snarl/lunge/caught heartbeat/fade | No unapproved health, damage or combat feature added |

## Actual checks

- **B branch portable: 180 passed, 0 failed**, up from 158 at this mission's start. Includes 18 mobile
  mapping/lifecycle/platform tests, two route-recovery tests, one voice-profile test and one headlamp policy test.
- **Scratch combined portable: 222 passed, 0 failed** using latest A `cefbd3d`, TD Core/Integration from
  `d4de1a1` and the current B source. This was a file-level logic trial in `/tmp`, not a native scene build,
  approved merge or complete game playtest. No owner checkout or published integration branch changed.
- Whole-tree static rules and self-test pass; package policy passes. Internal assembly names/references
  are unique, present and acyclic. Six actual editor step declarations pass production BuildPlan rules.
- C# 9 syntax: 50 B files, zero syntax errors under default, PC, mobile and mobile/restart defines.
  This checks source syntax, **not Unity API/type compatibility**.
- TD tooling tests: 23 passed / 1 failed. Reproduction is the unchanged Windows-path expectation under
  Linux in `test_env_override_is_tried_first`; failure text is below. No TD tool was edited by B.
- `unity_batch.py validate`, `editmode-tests`, `compose`, `desktop-sim`: each **BLOCKED, exit 2**, missing
  local Unity 6000.3.27f1. Native PlayMode/input mapping, human PC/touch runs and hardware performance
  were not executed. The earlier Windows results apply to the older baseline, not this delivery.

## TD continuation in order

1. Review this B PR plus current A/TD integration changes. Apply/reconcile the narrow shared-source proposal.
   Keep all owner-local backups/settings/untracked files; work in the existing safe integration checkout.
2. Establish separate PC, phone and Quest profiles. The phone adapter requires all four defines listed in
   Mobile/README.md, real XR/simulator startup disabled and dynamic input updates. Do not turn off XR in
   the Quest profile. Enable optional pause restart only after the R handler exists. Optional PC headlamp
   needs its explicit define. Configure Android/iOS modules and signing in TD's release environment.
3. Recompile and regenerate Bootstrap/Ward. Retain the 0.25 m bake-radius setup and door-only collision
   layer work. Validate new lens/headlamp references, saved navigation and all Core services/LayoutTags.
4. Run EditMode, PlayMode (including MobileMappingSceneTests), full puzzle/escape autoplay and a human
   PC/touch run. Test first look after restore, held cable aim, bottle throw, double-touch, pause during
   fade, rotation, safe-area changes, app suspension and Complete replay. Review label/world-UI overlap.
5. Inspect lighting/contrast and final reveal in the actual render. Profile Android/iPad/Quest separately:
   CPU/GPU, draw calls, audio DSP/mixed peaks, memory, input latency and thermal behavior. No FPS claim
   can be made from these portable checks. iPad delivery requires the macOS/Xcode/device build toolchain.

## Review artifacts

The task workspace contains an updated production-geometry floor plan and a mobile control-layout plan
exported from the tested Logic data. They are explicitly **not Unity/device screenshots**. The original
mono audio preview remains available. `Validation/mobile-touch-layout.json` contains the exported layout
and `Validation/final-multiplatform-results.json` the machine-readable results. No fabricated native render
or hardware result is supplied. Final presentation is procedural greybox art, not a completed licensed art pass.

GitHub GraphQL remains HTTP 403 in this cloud environment. A push to the existing branch updates PR #3;
no duplicate PR or protected-main merge is needed. Public source/package lookups were also blocked, so
pinned-package API behavior remains a native-compile check, not an inferred success.

## Portable output (current B source)

```text
Compiling 46 files ...
PASS ZombieLogicTests.APlayerThrownImpactRedirectsTheLastHeardPoint
PASS ZombieLogicTests.AQuietPlayerAtCatchDistanceStillDoesNotTriggerDetection
PASS ZombieLogicTests.ARemoteImpactDuringInvestigationClearsStalePlayerStimulus
PASS ZombieLogicTests.ActiveBrainTicksAllocateZeroBytesAfterWarmup
PASS ZombieLogicTests.BackwardClockAndInvalidPositionsCannotRetarget
PASS ZombieLogicTests.BlockedPatrolWaitsThenTriesTheOtherEndInsteadOfOscillating
PASS ZombieLogicTests.BottleDistractsChaseWithoutRevealingTheStationaryThrower
PASS ZombieLogicTests.ChaseExpiresWhenSilentPlayerTeleportsAway
PASS ZombieLogicTests.ChasePathFailureCannotCatchThroughAnObstacle
PASS ZombieLogicTests.DistractionDoesNotOverrideHysteresisOrFreshVisibleMovement
PASS ZombieLogicTests.FailedReturnRouteCanResumeListeningWithoutAnInfiniteHomeRequest
PASS ZombieLogicTests.FrozenPlayerAndBlackoutAreNeverSeen
PASS ZombieLogicTests.HarmlessOriginsInvalidAndStaleNoiseAreIgnored
PASS ZombieLogicTests.HeadHeightSoundUsesGroundDistanceForArrival
PASS ZombieLogicTests.HearingBoundaryMultiplierAndDoorOcclusion
PASS ZombieLogicTests.InterruptedCatchMustBuildACompleteNewCatchTimer
PASS ZombieLogicTests.InvestigationHasBoundedTimeoutEvenWithoutArrival
PASS ZombieLogicTests.MovingPlayerOutsideViewConeIsNotSeen
PASS ZombieLogicTests.MovingSightEscalationIncludesSnarl
PASS ZombieLogicTests.PatrolPausesAtBothEndsAndResumes
PASS ZombieLogicTests.PauseStunAndPassiveFreezeSensesAndTimers
PASS ZombieLogicTests.PlayerOnAnotherHeightCannotBeCaughtFromSameGroundPoint
PASS ZombieLogicTests.ReducedCatchNeedsHalfSecondAndSmallerRadius
PASS ZombieLogicTests.ReducedTelegraphIsLongerAndSightIsOff
PASS ZombieLogicTests.RestrainedPatientCannotCatchOnAnotherHeight
PASS ZombieLogicTests.RestrainedRootNeverMovesBeyondChainAndCatchRequiresRelease
PASS ZombieLogicTests.RetargetHysteresisSuppressesJitterButAcceptsLouderOrLater
PASS ZombieLogicTests.ScriptedObjectDistractionRedirectsChase
PASS ZombieLogicTests.SearchTimeoutReturnsHomeWithGroan
PASS ZombieLogicTests.SnarlPrecedesAnyChaseAndCatchIsOneShot
PASS ZombieLogicTests.SoundRequiresSniffTelegraphBeforeInvestigating
PASS ZombieLogicTests.WallOrRetreatBreaksContinuousCatchTimer
PASS ZombieLogicTests.WallsDoNotReduceGameplayHearingButClosedDoorsDo
PASS AtmosphereLogicTests.ActOneDoorGateNeedsScareLureAndFourMetreClearance
PASS AtmosphereLogicTests.ActTwoInvestigationIsGuaranteedBeforeSurge
PASS AtmosphereLogicTests.AdaptiveAssistSurvivesProgressAfterRestore
PASS AtmosphereLogicTests.AdaptiveAssistSurvivesProgressAndSettingChanges
PASS AtmosphereLogicTests.AlwaysOnAssistSurvivesProgressAndAppliesImmediatelyMidAct
PASS AtmosphereLogicTests.AlwaysOnAssistSurvivesProgressAndHints
PASS AtmosphereLogicTests.AmbientRequiresDistanceWallAndScareSilenceWindow
PASS AtmosphereLogicTests.EnablingPuzzleAssistMidActEmitsFloorImmediately
PASS AtmosphereLogicTests.EndingOrdersKneelPaRevealDroneThenComplete
PASS AtmosphereLogicTests.FlickerAndPulseStayBelowThreeDiscreteChangesPerSecondForTenMinutes
PASS AtmosphereLogicTests.GateCannotReleaseBeforeFourMetreClearanceOrWhilePaused
PASS AtmosphereLogicTests.GateEventOccursOnceAndLateDoorCanQueryUntilActChanges
PASS AtmosphereLogicTests.GateSignalIsOneShotQueryPersistsAndResetClosesBeforeActStart
PASS AtmosphereLogicTests.GateWaitsForPlanksLureAndFullClearanceInReducedModeToo
PASS AtmosphereLogicTests.GoldenRunFollowsApprovedBeatOrderAndTwoHardScares
PASS AtmosphereLogicTests.HintsFireAtTenTwentyThirtyAndResetOnActualProgress
PASS AtmosphereLogicTests.PausingFreezesEveryDirectorTimer
PASS AtmosphereLogicTests.ReducedFlickerIsConstantAndSurgeIsMonotonic
PASS AtmosphereLogicTests.ReducedRunHasZeroHardScaresAndNoLunge
PASS AtmosphereLogicTests.RestoreClosesGateAndAllowsExactlyOneFreshRelease
PASS AtmosphereLogicTests.RestoreDoesNotSpendHardScareBudgetTwice
PASS AtmosphereLogicTests.SecondCatchEnablesAdaptiveAssistOnRestore
PASS AtmosphereLogicTests.TrackingLossDefersHintsWithoutAutoPausingStory
PASS AtmosphereLogicTests.WindowPulseAndCombinedFixturesNeverExceedThreeEdgesInAnySecond
PASS FixturePolicyTests.EveryActLightsOnlyItsIntendedFixturesAndBlackoutHasNone
PASS FixturePolicyTests.HeadlampNeverOverridesBlackoutOrFinalRevealLighting
PASS FixturePolicyTests.OneDamagedTubeFlickersWhileFillStaysSteadyAndReducedRemovesVariation
PASS FixturePolicyTests.ReducedScaresSuppressOptionalSurfaceTraces
PASS AudioLogicTests.ApprovedLoopsAndZeroNoiseLandmarksArePreserved
PASS AudioLogicTests.EveryContractIdHasExactlyOneRow
PASS AudioLogicTests.GuidanceAndLureAttenuationCoversActualWardDimensions
PASS AudioLogicTests.GuidanceCurvesCoverApprovedWardAnchorsAndSilenceBeyondTheirRange
PASS AudioLogicTests.MissingCueHasNoDefinitionAndNoneIsSilent
PASS AudioLogicTests.MobileVoiceProfileCapsPlaybackAndRetainsProtectedSpeechAndUi
PASS AudioLogicTests.PortedNoiseCapacityDoesNotEvictUnexpiredSources
PASS AudioLogicTests.PortedNoiseCooldownAndNestedDispatchGuards
PASS AudioLogicTests.PortedNoiseRejectsStaleFutureZeroAndInvalidInputs
PASS AudioLogicTests.ReleasedHandleNeverAddressesAReplacement
PASS AudioLogicTests.RequiredCaptionsAndAllSpeechHaveText
PASS AudioLogicTests.StealingSelectsLowestPriorityThenOldestAndReportsCue
PASS AudioLogicTests.UiAndSpeechCannotBeStolenAndRejectionIsReported
PASS AudioLogicTests.VoiceCapsAndHrtfFallbackAreBounded
PASS AudioLogicTests.WorldCuesReachSilenceAndCoverTheirGameplayRadius
PASS AudioLogicTests.ZombieCuesAreAudibleBeforeCommonPlayerNoise
PASS ProceduralSoundTests.CreatureVentilationMetalAndElectricalSoundsAreDistinctAndRepeatable
PASS ProceduralSoundTests.EveryPlayableCueHasFiniteBoundedAudioWithClickFreeEndpoints
PASS ProceduralSoundTests.SynthesizedAssetsPreserveGameplayNoiseAndMixerRouting
PASS CuesTests.Play_RoutesToRegisteredPlayer
PASS CuesTests.Play_WithoutPlayer_LogsOnceAndReturnsInvalidHandle
PASS CuesTests.Stop_InvalidHandle_DoesNotReachPlayer
PASS EventChannelTests.Clear_DuringRaise_DoesNotThrowAndStopsLaterHandlers
PASS EventChannelTests.HandlerThrows_RemainingHandlersStillRun
PASS EventChannelTests.NestedRaise_DeliversInnerAndOuter
PASS EventChannelTests.Raise_DeliversPayloadToEverySubscriber
PASS EventChannelTests.Subscribe_DuringRaise_RunsFromNextRaise
PASS EventChannelTests.Subscribe_Null_Throws
PASS EventChannelTests.Subscribe_SameHandlerTwice_RunsOnce
PASS EventChannelTests.Unsubscribe_DuringRaise_SkipsRemovedHandler
PASS EventChannelTests.Unsubscribe_EarlierHandlerDuringRaise_LaterHandlersStillRun
PASS EventChannelTests.Unsubscribe_MethodGroup_RemovesEqualDelegate
PASS EventChannelTests.Unsubscribe_SelfDuringRaise_CompactsAfterwards
PASS ExperienceSettingsTests.Defaults_AreAlreadySanitized
PASS ExperienceSettingsTests.Defaults_AreComfortFirst
PASS ExperienceSettingsTests.Sanitized_ClampsCorruptValues
PASS ExperienceSettingsTests.SnapTurnOptions_Are30_45_60
PASS GameEventsTests.EveryChannel_IsNamedAfterItsField
PASS GameEventsTests.ResetAll_ClearsEveryChannel
PASS GameStateRulesTests.EveryState_CanEventuallyReachPlaying
PASS GameStateRulesTests.Pause_OnlyFromPlayingOrEnding
PASS GameStateRulesTests.TransitionTable_MatchesArchitectureDoc
PASS IdsTests.CueIds_AreUnique
PASS IdsTests.GateIds_AreUniqueAndPinned
PASS IdsTests.GateReleased_CarriesItsGate
PASS IdsTests.ObjectiveIds_AreUnique
PASS IdsTests.Payloads_ClampTheirInputs
PASS ServicesTests.DestroyedUnityObject_IsTreatedAsMissing_AndCanBeReplaced
PASS ServicesTests.Register_SecondLiveInstance_IsRefusedAndLogged
PASS ServicesTests.Register_ThenTryGet_ReturnsService
PASS ServicesTests.ResetStatics_ClearsRegisteredServices
PASS ServicesTests.Unregister_ByOtherInstance_IsIgnored
PASS WardLayoutTests.ActStart_RejectsNone
PASS WardLayoutTests.Beacon_TriggerIsTheLastDarknessAnchor
PASS WardLayoutTests.CriticalPath_IncludesEveryActStartInOrder
PASS WardLayoutTests.CriticalPath_StepsAreWithinTeleportRange
PASS WardLayoutTests.DarknessPatrol_NeverRunsThroughAnAnchor
PASS WardLayoutTests.EveryAnchor_IsInsideARoomWithWallClearance
PASS WardLayoutTests.EveryInteractable_IsWithinArmsReachOfAnAnchorInTheSameAct
PASS WardLayoutTests.FinalPatient_CannotReachAnyAct4AnchorOrTheWalkBetweenThem
PASS WardLayoutTests.HorizontalDistanceToSegment_HandlesEndsAndMiddle
PASS WardLayoutTests.Ids_AreUnique
PASS WardLayoutTests.RequiredInteractables_AreInTheComfortableHeightBand
PASS WardLayoutTests.TryGet_FindsEveryPoint
PASS MobileTouchTests.ButtonsCannotTurnIntoLookGesturesAndDuplicateIdsAreIgnored
PASS MobileTouchTests.EmptyOrInvalidSafeAreasAreRejected
PASS MobileTouchTests.FadeReleasesGameplayInputWhilePausedResumeRemainsUsable
PASS MobileTouchTests.FlashlightLocksAtSurgeAndBlackoutAndResetsForNewAct
PASS MobileTouchTests.GrippingRoutesDragToPuzzleWithoutRightMouseCameraRotation
PASS MobileTouchTests.HeldBottleOrCableCanBeAimedWithoutReleasingIt
PASS MobileTouchTests.JoystickAndLookUseIndependentPointersWithoutFirstTouchCameraJump
PASS MobileTouchTests.MobilePlatformRequiresAllOptInsAndRejectsXrOrSimulatorConfiguration
PASS MobileTouchTests.PauseFocusResetAndResizeClearGripMovementAndPendingActions
PASS MobileTouchTests.PhoneDefaultTeleportModeHasPredictableForwardJoystickStep
PASS MobileTouchTests.PortraitInvalidAndExcessPointersCannotMoveOrRetainInput
PASS MobileTouchTests.RepeatedHeadlampTapsCannotStrobeAndCancelledThrowKeepsHold
PASS MobileTouchTests.RestartAndCalibrationRemainDistinctAndUnavailableActionsFailClosed
PASS MobileTouchTests.SafeAreaContainsEveryButtonOnPhoneAndIpadLandscape
PASS MobileTouchTests.SimulatorDevicesAreDetectedWithoutImportingAnyXriTypes
PASS MobileTouchTests.TapInteractIsDistinctFromLongHoldOrDragAndCancelledButtonsDoNotFire
PASS MobileTouchTests.ThrowKeepsLeftHeldForQFrameThenReleasesOnNextFrame
PASS MobileTouchTests.WarmTouchTranslationAllocatesNoBytes
PASS RoomGeometryTests.BuiltWallCollidersPreserveTheEntireDoorWidth
PASS RoomGeometryTests.ColliderLongitudinalExtentNeverShrinksDoorOpenings
PASS RoomGeometryTests.DoorOpeningsMatchApprovedMarkersAndAreClearOfWalls
PASS RoomGeometryTests.EachRoomBoundaryIsCoveredByAWallOrDoor
PASS RoomGeometryTests.EveryLayoutAnchorHasFloor
PASS RoomGeometryTests.NewDressingLeavesEveryCriticalPathSegmentAndDoorClear
PASS RoomGeometryTests.TheatreDressingStaysOnFloorAndClearOfZombieAndPuzzleRoutes
PASS RoomGeometryTests.WallSpansAreNonzeroAxisAlignedAndNeverDuplicated
PASS WardCollisionPolicyTests.ExplicitDoorLayerBlocksMovementSightAndAudioButAloneAttenuatesHearing
PASS WardCollisionPolicyTests.MissingAndBuiltinLayersDoNotTurnWallsIntoHearingDoors
PASS BuildPlanTests.AreaOfAssembly_ReadsTheSecondSegment
PASS BuildPlanTests.AudioAndIntegration_MayBuildBothScenes
PASS BuildPlanTests.BadSignature_IsReported
PASS BuildPlanTests.DuplicateOrderInOneScene_IsAnError
PASS BuildPlanTests.OrderOutsideTheAreasBand_IsAnError
PASS BuildPlanTests.Order_IsDeterministicRegardlessOfInputOrder
PASS BuildPlanTests.SameOrderInDifferentScenes_IsAllowed
PASS BuildPlanTests.SeatBuildingIntoTheWrongScene_IsAnError
PASS BuildPlanTests.StepOutsideAnArea_IsAnError
PASS BuildPlanTests.UnknownSceneValue_IsRejectedNotOrdered
PASS BuildPlanTests.ValidSteps_AreOrderedByOrderPerScene
PASS PlacementCheckTests.DuplicatePoint_IsAnError
PASS PlacementCheckTests.EveryRequiredPointPlacedExactly_Passes
PASS PlacementCheckTests.MissingRequiredPoint_IsAnError
PASS PlacementCheckTests.OffsetJustInsideTolerance_Passes_JustOutside_Fails
PASS PlacementCheckTests.OptionalPointsMayBeAbsentOrPlaced
PASS PlacementCheckTests.UnknownOrEmptyId_IsAnError
PASS QuestBuildConfigTests.ApiLevels_MeetMetaRequirements
PASS QuestBuildConfigTests.Bootstrap_IsSceneZero
PASS QuestBuildConfigTests.EditorVersion_IsA63FinalRelease
PASS QuestBuildConfigTests.ForbiddenPackages_MatchByPrefixOnly
PASS QuestBuildConfigTests.RequiredFeatures_AreUnique
PASS ServiceCensusTests.EveryCoreServiceHasAHomeScene
PASS ServiceCensusTests.MissingService_IsAnError
PASS ServiceCensusTests.OneImplementerEachInItsHomeScene_Passes
PASS ServiceCensusTests.ServiceInTheWrongScene_IsAnError
PASS ServiceCensusTests.TwoImplementers_IsAnError

Logic tests: 180 passed, 0 failed.
```

## Native attempts

```text
validate (exit 2)
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.

editmode-tests (exit 2)
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.

compose (exit 2)
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.

desktop-sim (exit 2)
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.

PlayMode/native input mapping: BLOCKED by the same absent Unity editor; no results XML or hardware profile generated.
```

## Existing tooling failure

```text
F.......................
======================================================================
FAIL: test_env_override_is_tried_first (test_unity_batch.UnityBatchTests.test_env_override_is_tried_first)
----------------------------------------------------------------------
Traceback (most recent call last):
  File "/workspace/patient-zero-vr/tools/unity/test_unity_batch.py", line 30, in test_env_override_is_tried_first
    self.assertIn(Path(r"C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe"), paths)
AssertionError: PosixPath('C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.27f1\\Editor\\Unity.exe') not found in [PosixPath('D:\\U\\Unity.exe'), PosixPath('C:\\Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe')]

----------------------------------------------------------------------
Ran 24 tests in 0.022s

FAILED (failures=1)
```
