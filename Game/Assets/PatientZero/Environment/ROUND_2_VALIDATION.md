# Round 2 validation — 2026-10-28 23:03 GMT

Branch: `integration/xr-systems`; main `44ea7b7` (PR #4 merged, Core v1.1).

## Portable logic

Command: `PATH=/workspace/tools/dotnet:$PATH DOTNET_CLI_HOME=/workspace/tools/dotnet-home python tools/logic_tests/run_logic_tests.py`

Actual output:

```text
Compiling 37 files ...
PASS ZombieLogicTests.APlayerThrownImpactRedirectsTheLastHeardPoint
PASS ZombieLogicTests.AQuietPlayerAtCatchDistanceStillDoesNotTriggerDetection
PASS ZombieLogicTests.ActiveBrainTicksAllocateZeroBytesAfterWarmup
PASS ZombieLogicTests.BackwardClockAndInvalidPositionsCannotRetarget
PASS ZombieLogicTests.ChaseExpiresWhenSilentPlayerTeleportsAway
PASS ZombieLogicTests.ChasePathFailureCannotCatchThroughAnObstacle
PASS ZombieLogicTests.FrozenPlayerAndBlackoutAreNeverSeen
PASS ZombieLogicTests.HarmlessOriginsInvalidAndStaleNoiseAreIgnored
PASS ZombieLogicTests.HeadHeightSoundUsesGroundDistanceForArrival
PASS ZombieLogicTests.HearingBoundaryMultiplierAndDoorOcclusion
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
PASS AtmosphereLogicTests.ActOneDoorGateNeedsScareLureAndFourMetreClearance
PASS AtmosphereLogicTests.ActTwoInvestigationIsGuaranteedBeforeSurge
PASS AtmosphereLogicTests.AdaptiveAssistSurvivesProgressAfterRestore
PASS AtmosphereLogicTests.AlwaysOnAssistSurvivesProgressAndHints
PASS AtmosphereLogicTests.AmbientRequiresDistanceWallAndScareSilenceWindow
PASS AtmosphereLogicTests.EnablingPuzzleAssistMidActEmitsFloorImmediately
PASS AtmosphereLogicTests.EndingOrdersKneelPaRevealDroneThenComplete
PASS AtmosphereLogicTests.FlickerAndPulseStayBelowThreeDiscreteChangesPerSecondForTenMinutes
PASS AtmosphereLogicTests.GateSignalIsOneShotQueryPersistsAndResetClosesBeforeActStart
PASS AtmosphereLogicTests.GateWaitsForPlanksLureAndFullClearanceInReducedModeToo
PASS AtmosphereLogicTests.GoldenRunFollowsApprovedBeatOrderAndTwoHardScares
PASS AtmosphereLogicTests.HintsFireAtTenTwentyThirtyAndResetOnActualProgress
PASS AtmosphereLogicTests.PausingFreezesEveryDirectorTimer
PASS AtmosphereLogicTests.ReducedFlickerIsConstantAndSurgeIsMonotonic
PASS AtmosphereLogicTests.ReducedRunHasZeroHardScaresAndNoLunge
PASS AtmosphereLogicTests.RestoreDoesNotSpendHardScareBudgetTwice
PASS AtmosphereLogicTests.SecondCatchEnablesAdaptiveAssistOnRestore
PASS AtmosphereLogicTests.TrackingLossDefersHintsWithoutAutoPausingStory
PASS AtmosphereLogicTests.WindowPulseAndCombinedFixturesNeverExceedThreeEdgesInAnySecond
PASS AudioLogicTests.ApprovedLoopsAndZeroNoiseLandmarksArePreserved
PASS AudioLogicTests.EveryContractIdHasExactlyOneRow
PASS AudioLogicTests.GuidanceAndLureAttenuationCoversActualWardDimensions
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
PASS RoomGeometryTests.DoorOpeningsMatchApprovedMarkersAndAreClearOfWalls
PASS RoomGeometryTests.EachRoomBoundaryIsCoveredByAWallOrDoor
PASS RoomGeometryTests.EveryLayoutAnchorHasFloor
PASS RoomGeometryTests.TheatreDressingStaysOnFloorAndClearOfZombieAndPuzzleRoutes
PASS RoomGeometryTests.WallSpansAreNonzeroAxisAlignedAndNeverDuplicated
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

Logic tests: 137 passed, 0 failed.
```


## Whole-tree static rules

```text

0 error(s), 0 warning(s).
```


## Pinned package policy

```text
PASS  package: com.unity.render-pipelines.universal 17.3.0
PASS  package: com.unity.test-framework 1.6.0
PASS  package: com.unity.inputsystem 1.20.1
PASS  package: com.unity.xr.management 4.7.0
PASS  package: com.unity.xr.openxr 1.19.0
PASS  package: com.unity.xr.meta-openxr 2.7.0
PASS  package: com.unity.xr.interaction.toolkit 3.7.0
PASS  package: com.unity.xr.hands 1.10.0
PASS  package: com.unity.ai.navigation 2.0.16
INFO  lock: no packages-lock.json yet (written when Unity first opens Game/); transitive packages not checked
PASS  editor-version: 6000.3.27f1 (672d562a42a7)
PASS  config: QuestBuildConfig matches the policy
0 error(s), 0 warning(s).
```


## TD-owned Python tooling tests

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
Ran 24 tests in 0.021s

FAILED (failures=1)
```


The tooling failure is pre-existing on approved main: `candidate_paths` builds a Windows path with the host
`PosixPath` implementation, but `test_env_override_is_tried_first` expects a backslash-only string.
23 of 24 tooling tests pass. This seat does not modify TD-owned tools. The package validator reports
no lock file until Unity resolves dependencies; package policy success does not prove resolution.

## Actual Unity attempts

Commands: `python tools/unity/unity_batch.py validate`, `editmode-tests`, `compose`, `desktop-sim`.
Each returned exit 2 before launching an editor. PlayMode discovery also found no pinned editor.


### validate

```text
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.
```

### editmode-tests

```text
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.
```

### compose

```text
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.
```

### desktop-sim

```text
BLOCKED: Unity 6000.3.27f1 not found. Looked in:
  /home/agent/Unity/Hub/Editor/6000.3.27f1/Editor/Unity
Install it with Unity Hub (add Android Build Support) or set PZ_UNITY to its executable.
```

### playmode

```text
BLOCKED: PlayMode, navigation and emitter validation cannot run: Unity 6000.3.27f1 not found.
```


No Unity compilation, EditMode/PlayMode execution, native scene generation, navigation validation,
audio-emitter validation or desktop simulation succeeded here. The generated-Ward test now requires
two load/unload passes, a persisted WardNavMesh asset and complete paths. The hearing fixture checks
wall/full hearing versus door/half hearing and disables off-act agents. The director fixture tests
real Core gate event publication, a late service query and a reset before ActStarted.

Once Unity is available, run the four commands above and PlayMode tests with:

```sh
"$PZ_UNITY" -batchmode -nographics -projectPath Game -runTests -testPlatform PlayMode -testResults /tmp/pz-playmode.xml -logFile /tmp/pz-playmode.log
```

Before compose can pass service/layout validation, the TD must integrate seat A's service, rig, puzzle
and door builders. No seat A source or generated scene was edited or merged from its unapproved branch.
Theatre dressing uses existing WardLayout coordinates, adds no real-time lights and leaves encounter
scare counts/timing unchanged. Static dressing opts into editor batching; no draw-call or frame-rate
claim is made until actual profiling.

## Remaining native checks

- Unity API/type compilation and native mixer factory, material, prefab and NavMesh serialization.
- TD agent-type bake radius and shared physics masks (INT-B-1/2), followed by A's physical door/path tests.
  The native route test now checks Z1 can actually reach the 0.5 m window-arrival threshold; a default
  0.5 m bake radius plus wall thickness may miss it and stall Act 1. Set bake radius to 0.25 m before
  validating the generated scene; changing only NavMeshAgent.radius does not change the bake.
- All services and LayoutTags with seat A merged by the TD; restart/restore/tracking tests in the full game.
- Headphone front/back/left/right listening, actual occlusion and perceived attenuation. Portable attenuation
  tests only verify the configured gains at the real beacon, alarm and lure distances.
- Quest CPU/GPU, allocations, draw calls, memory and 72 Hz. No physical device was present.
- Licensed art and recordings; current cues are original generated mono placeholders and captions.
  No separate scratching CueId/extra timed scare was invented outside the approved design.


## Concurrent-delivery reconciliation

The first push was rejected (non-fast-forward): another session published `6ad78f8` on the same
branch while this work was in progress. Both histories are preserved by merging that commit;
no force push, reset or rebase. The merged implementation keeps its explicit hearing policy,
wall regression, settings-toggle handling, native NavMesh GUID preservation and additional
patient-room dressing, while retaining the live agent/animation fixes, tested theatre dressing,
window-reach assertion and fail-closed early door completion. Duplicate theatre geometry was
consolidated into the tested dressing plan. Both sessions' log entries and tests are retained.
Gate release is evaluated on a live tick (not a position report during pause), preserving the
concurrent pause regression. All native validation remains blocked as above.

Actual merged portable output (supersedes the earlier 137-test checkpoint):

```text
Compiling 37 files ...
PASS ZombieLogicTests.APlayerThrownImpactRedirectsTheLastHeardPoint
PASS ZombieLogicTests.AQuietPlayerAtCatchDistanceStillDoesNotTriggerDetection
PASS ZombieLogicTests.ActiveBrainTicksAllocateZeroBytesAfterWarmup
PASS ZombieLogicTests.BackwardClockAndInvalidPositionsCannotRetarget
PASS ZombieLogicTests.ChaseExpiresWhenSilentPlayerTeleportsAway
PASS ZombieLogicTests.ChasePathFailureCannotCatchThroughAnObstacle
PASS ZombieLogicTests.FrozenPlayerAndBlackoutAreNeverSeen
PASS ZombieLogicTests.HarmlessOriginsInvalidAndStaleNoiseAreIgnored
PASS ZombieLogicTests.HeadHeightSoundUsesGroundDistanceForArrival
PASS ZombieLogicTests.HearingBoundaryMultiplierAndDoorOcclusion
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
PASS RoomGeometryTests.TheatreDressingStaysOnFloorAndClearOfZombieAndPuzzleRoutes
PASS RoomGeometryTests.WallSpansAreNonzeroAxisAlignedAndNeverDuplicated
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

Logic tests: 145 passed, 0 failed.
```
