# Horror experience pass — 2026-10-30

Baseline: `integration/xr-systems` `07bf6f3`, fast-forwarded from the existing work. Main remains
approved contract v1.1 at `44ea7b7`. The scene composer and WardLayout remain authoritative.

## What the player will encounter after regeneration

- A curtained bedside area with a visible bed frame, headboard, IV equipment and a clinical note
  connecting B-7 to containment. The opening sign says **REMOVE THE 3 PLANKS**.
- A dressed operating theatre in the existing supply footprint: sterile drape, privacy screen,
  oxygen silhouette, table, lamp and instrument shelf. No room or puzzle coordinate moved.
- Corridor colour bands stop at actual doorway cuts; corrected EXIT arrows face the approach.
  **MOVE QUIETLY / LISTEN** and **EMERGENCY RELEASE** clarify the route and final action.
  Corrupted arrow/dash text has been replaced with plain ASCII that the placeholder font supports.
- Selective dark surface traces and chain scrape marks; Reduced scares hides the optional traces.
  They use opaque, static primitive surfaces, with no particles, postprocessing or animated decals.
- Six existing shadowless fixtures with visible lenses that follow light intensity. Only the damaged
  window tube flickers; B-7 fill stays steady. WardDim lights the three corridor fixtures, Blackout
  disables them, and the exit sign remains green. No extra real-time lights were introduced.
- Distinct original mono synthesis for creature breathing/growls/sniffs/shuffling, ventilation,
  fluorescent hum, metal transients, alarms, drip and broadband beacon attacks. PA remains radio-static
  with approved captions, not recorded or synthesized speech. Existing CueIds, routing, custom distance
  curves, volume preferences, missing-asset behavior and voice limits remain in force.
- A sufficiently salient thrown impact beyond two metres redirects a chasing zombie to investigate
  the landing; it does not reveal the thrower's live position. Movement that the zombie can actually
  see can still reacquire the player with a snarl. Existing retarget hysteresis prevents rapid toggling.
  Interrupting a chase also clears its accumulated catch timer before any later attack.

## Pacing and fairness

The approved objective-driven acts continue to shape the approximate 0–30/30–60/60–90/90–120 second
curve. They keep their gates, near-misses, adaptive help, two hard scares in Standard, zero in Reduced,
and the 12-second infection ending. A clock never kills or advances the player automatically. No forced
camera movement or new unavoidable scare was added. The earlier no-progress investigation/search,
wall/path guards, spawn validation, pause/restore and restrained-patient reach remain intact.

Curtains/screens are real static colliders in perimeter dressing areas. Tests check their bounds against
all shared markers, the entire critical route, every doorway and Z2's doorway route. Native rebaking is
still required because source clearance is not a NavMesh result. No A-owned trolley, interaction or scene
was edited. Renderer property blocks share generated materials; fixture faces add no light sources.

## Actual validation

- Portable suite: **158 passed, 0 failed**, including four new AI distraction/catch regressions, three
  synthesis/routing tests, three fixture/comfort tests and a complete route-clearance test.
- Whole-tree checker: **0 errors, 0 warnings**; checker self-test passes; package policy passes.
- Actual editor method attributes/signatures parsed with Roslyn and passed through the production
  `Integration.Logic.BuildPlan`: **6 steps, 0 errors**. A native TypeCache regression test was added too.
- Supplementary C# 9 parse: **42 B source files, 0 syntax errors**. This is not Unity type compilation.
- Python tooling: **23 passed, 1 failed**. The unchanged TD-owned Windows-path expectation fails on Linux.
- Original generated PCM payload: **21,456,000 bytes (20.46 MiB)**, mono 48 kHz, each cue bounded below
  0.16 linear peak. Mixed-bus peaks, native allocation and perceived loudness still need measurement.
- Local Unity validation, EditMode, compose and desktop-sim attempts each exited **2 / BLOCKED**:
  this cloud host has no Unity 6000.3.27f1. No Windows editor/build was launched. Historical Windows
  results in `Validation/native-results.json` apply to the earlier baseline, not these changes.

Native editor tests added for the new decorations/readable signs and unique composer registrations are
**not run**. No current-pass Unity, human desktop, headphone, Quest or frame-rate pass is claimed.
Actual local command output is below; machine-readable results and the exported production geometry
are in `Validation/horror-pass-results.json` and `Validation/horror-pass-scene-plan.json`.

## Review evidence

A floor-plan image and original mono WAV preview were exported to the task workspace for review.
The plan is generated from the real `RoomGeometry`, `HospitalDressing` and `WardLayout` Logic data;
it is explicitly labelled **not a Unity screenshot**. The preview order is breath, shuffle, ventilation,
buzz, metal impact, alarm, chirp and groan. It does not establish VR direction or headphone localisation.
No binary asset is committed. The JSON geometry is kept in the repository so the diagram can be audited.

## Claude integration requirements

1. Regenerate Bootstrap/Ward in the TD's existing safe integration checkout after reviewing this PR.
   Do not overwrite the owner's working settings, metadata or untracked files. Existing four trial patch
   proposals and native-integration dependencies are documented in DESKTOP_PLAYTEST.md.
2. Run Unity compilation, EditMode (including the two new editor tests), PlayMode, saved-hospital
   validation and the full desktop run. Rebake with the approved 0.25 m agent radius and configure the
   proposed door-only layer before judging hearing through doors. Confirm new curtains do not obscure
   critical sightlines, and check labels/lighting at the calibrated darkness level.
3. Inspect the fixture lens bindings and reduced-scare surface suppression after restart/settings changes.
   Listen to audio with headphones at actual ward distances; tune perceived mix while retaining gameplay
   noise radii and the tested peak cap. Capture mixed output for clipping; the per-cue bound is not a limiter.
4. Keep the existing Player/Interaction/Flow ownership. The flashlight request needs the documented Core
   handoff below; a second input path was not invented here. Human movement/interaction/ending issues must
   be fixed by A/TD through their existing desktop/VR implementations.
5. Profile the physical Quest for 72 Hz, native memory and draw calls. Primitive surfaces and static batching
   are a performance choice, not evidence of a device result. Art/audio remains original procedural fallback.

## Pending flashlight interface

Main has no flashlight state/pose contract. Proposed D-1b: an A-owned penlight, useful only in Acts 1–2,
that dies at the surge so Act 3 retains the approved audio-navigation challenge. To Claude, request the
existing D-1b design decision plus Core `HandheldLightChanged` (Interaction writer, Atmosphere reader):
`bool Enabled; Vector3 Position; Vector3 Forward;`, a `GameEvents.HandheldLightChanged` channel cleared by
ResetAll, and `WardLayout.A1_Penlight` on the bedside tray. Agree an event rate/pose update contract and
whether it replaces an existing light to retain the six-light budget. B then implements the light shell;
A owns grabbing, battery/input and hand/controller pose. No unapproved Core change is included here.

## Local portable output

```text
Compiling 43 files ...
PASS ZombieLogicTests.APlayerThrownImpactRedirectsTheLastHeardPoint
PASS ZombieLogicTests.AQuietPlayerAtCatchDistanceStillDoesNotTriggerDetection
PASS ZombieLogicTests.ARemoteImpactDuringInvestigationClearsStalePlayerStimulus
PASS ZombieLogicTests.ActiveBrainTicksAllocateZeroBytesAfterWarmup
PASS ZombieLogicTests.BackwardClockAndInvalidPositionsCannotRetarget
PASS ZombieLogicTests.BottleDistractsChaseWithoutRevealingTheStationaryThrower
PASS ZombieLogicTests.ChaseExpiresWhenSilentPlayerTeleportsAway
PASS ZombieLogicTests.ChasePathFailureCannotCatchThroughAnObstacle
PASS ZombieLogicTests.DistractionDoesNotOverrideHysteresisOrFreshVisibleMovement
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
PASS FixturePolicyTests.OneDamagedTubeFlickersWhileFillStaysSteadyAndReducedRemovesVariation
PASS FixturePolicyTests.ReducedScaresSuppressOptionalSurfaceTraces
PASS AudioLogicTests.ApprovedLoopsAndZeroNoiseLandmarksArePreserved
PASS AudioLogicTests.EveryContractIdHasExactlyOneRow
PASS AudioLogicTests.GuidanceAndLureAttenuationCoversActualWardDimensions
PASS AudioLogicTests.GuidanceCurvesCoverApprovedWardAnchorsAndSilenceBeyondTheirRange
PASS AudioLogicTests.MissingCueHasNoDefinitionAndNoneIsSilent
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

Logic tests: 158 passed, 0 failed.
```

## Local native attempts

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
```

## Local tooling failure

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
Ran 24 tests in 0.019s

FAILED (failures=1)
```
