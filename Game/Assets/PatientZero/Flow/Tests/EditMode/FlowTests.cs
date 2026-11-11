using System;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Flow.Logic;
namespace PatientZero.Flow.Tests
{
    public class FlowTests
    {
        private static GameFlowCore Running()
        {
            var f=new GameFlowCore();f.Start(true);f.ClearCommands();f.FadeDone();f.ClearCommands();f.WardLoaded();f.ClearCommands();f.FadeDone();return f;
        }
        private static bool Has(GameFlowCore f,FlowCommandKind kind){for(int i=0;i<f.CommandCount;i++)if(f.CommandAt(i).Kind==kind)return true;return false;}
        [Test] public void FocusLossDuringCompleteRestartDefersNewRun(){var f=Running();f.Request(GameState.Ending);f.Request(GameState.Complete);f.Restart(false);f.FadeDone();f.WardUnloaded();f.Focus(false);f.ClearCommands();f.WardLoaded();Assert.AreEqual(GameState.Paused,f.State);Assert.IsFalse(Has(f,FlowCommandKind.StartRun));f.Resume();Assert.IsTrue(Has(f,FlowCommandKind.StartRun));}
        [Test] public void NoStartRunUntilWardAcknowledged(){var f=new GameFlowCore();f.Start(true);Assert.IsFalse(Has(f,FlowCommandKind.StartRun));f.FadeDone();Assert.IsFalse(Has(f,FlowCommandKind.StartRun));f.ClearCommands();f.WardLoaded();Assert.IsTrue(Has(f,FlowCommandKind.StartRun));}
        [Test] public void InvalidTimeAndOutOfOrderCallbacksAreSafe(){var f=Running();f.ClearCommands();f.WardLoaded();f.WardUnloaded();f.FadeDone();Assert.AreEqual(0,f.CommandCount);Assert.Throws<ArgumentOutOfRangeException>(()=>f.Tick(float.PositiveInfinity));Assert.Throws<ArgumentOutOfRangeException>(()=>f.Tick(-1));}
        [Test] public void StartupCalibrationLoadsOnlyAfterConfirmation(){var f=new GameFlowCore();f.Start(false);Assert.AreEqual(GameState.Calibration,f.State);Assert.IsTrue(Has(f,FlowCommandKind.Fade));Assert.IsFalse(Has(f,FlowCommandKind.LoadWard));f.ClearCommands();Assert.IsTrue(f.Request(GameState.Playing));f.FadeDone();Assert.IsTrue(Has(f,FlowCommandKind.LoadWard));}
        [Test] public void AllDisallowedEdgesAreRejectedWithoutChangingState(){foreach(GameState from in Enum.GetValues(typeof(GameState)))foreach(GameState to in Enum.GetValues(typeof(GameState))){if(GameStateRules.CanTransition(from,to))continue;var f=At(from);var before=f.State;f.ClearCommands();Assert.IsFalse(f.Request(to));Assert.AreEqual(before,f.State);Assert.IsTrue(Has(f,FlowCommandKind.Rejected));}}
        private static GameFlowCore At(GameState state){var f=new GameFlowCore();if(state==GameState.Boot)return f;if(state==GameState.Calibration){f.Start(false);return f;}f=Running();if(state==GameState.Playing)return f;if(state==GameState.Paused)f.Pause();if(state==GameState.Caught)f.Catch();if(state==GameState.Ending || state==GameState.Complete)f.Request(GameState.Ending);if(state==GameState.Complete)f.Request(GameState.Complete);return f;}
        [Test] public void PauseAndResumeReturnsToEnding(){var f=Running();f.Request(GameState.Ending);f.ClearCommands();f.Pause();Assert.AreEqual(GameState.Paused,f.State);Assert.AreEqual(FlowCommandKind.TimeScale,f.CommandAt(0).Kind);Assert.AreEqual(0,f.CommandAt(0).Value);Assert.IsFalse(f.Request(GameState.Playing));f.ClearCommands();f.Resume();Assert.AreEqual(GameState.Ending,f.State);Assert.AreEqual(1,f.CommandAt(0).Value);}
        [Test] public void PauseOnlyFromPlayingOrEnding(){foreach(GameState state in Enum.GetValues(typeof(GameState))){var f=At(state);f.Pause();Assert.AreEqual(GameStateRules.CanPauseFrom(state)?GameState.Paused:state,f.State);}}
        [Test] public void FocusRegainNeverResumes(){var f=Running();f.Focus(false);Assert.AreEqual(GameState.Paused,f.State);f.Focus(true);Assert.AreEqual(GameState.Paused,f.State);f.Resume();Assert.AreEqual(GameState.Playing,f.State);}
        // E-22 (two-minute version): the first grab of a life is a struggle; a lost struggle or a second grab sends the player back to bed.
        private static void Press(GameFlowCore f,int times){for(int i=0;i<times;i++)f.Struggle();}
        private static void AssertBackInBed(GameFlowCore f)
        {
            f.Tick(.4f);f.FadeDone();Assert.IsFalse(Has(f,FlowCommandKind.UnloadWard),"the death is held before the reload");
            f.Tick(2.1f);Assert.IsTrue(Has(f,FlowCommandKind.UnloadWard));f.ClearCommands();f.WardUnloaded();Assert.IsTrue(Has(f,FlowCommandKind.LoadWard));
            f.ClearCommands();f.WardLoaded();Assert.AreEqual(GameState.Playing,f.State);Assert.AreEqual(ActId.Awakening,f.CurrentAct);
            bool found=false;for(int i=0;i<f.CommandCount;i++){var c=f.CommandAt(i);if(c.Kind!=FlowCommandKind.StartRun)continue;found=true;Assert.AreEqual(ActId.Awakening,c.Act);Assert.IsTrue(c.Restore,"the bed, not a new run: the news does not replay");}
            Assert.IsTrue(found);Assert.IsFalse(f.StruggleUsed,"a new life has its struggle back");
        }
        [Test] public void FirstGrabIsAStruggleThatCanBeWon()
        {
            var f=Running();f.SetAct(ActId.Machinery);f.ClearCommands();
            Assert.IsTrue(f.Catch());Assert.IsFalse(f.Catch());Assert.AreEqual(GameState.Caught,f.State);Assert.IsTrue(f.Struggling);
            Assert.IsFalse(Has(f,FlowCommandKind.Fade));Assert.IsFalse(Has(f,FlowCommandKind.UnloadWard));
            Press(f,GameFlowCore.StrugglePresses-1);Assert.AreEqual(GameState.Caught,f.State);Assert.AreEqual(.8f,f.StruggleProgress,1e-4f);
            f.Tick(GameFlowCore.StruggleSeconds-.1f);Assert.IsTrue(f.Struggling);f.ClearCommands();
            Assert.IsTrue(f.Struggle());Assert.AreEqual(GameState.Playing,f.State);Assert.IsFalse(f.Struggling);Assert.IsTrue(f.StruggleUsed);
            bool freed=false;for(int i=0;i<f.CommandCount;i++){var c=f.CommandAt(i);if(c.Kind==FlowCommandKind.StateChanged && c.From==GameState.Caught && c.To==GameState.Playing)freed=true;Assert.AreNotEqual(FlowCommandKind.StartRun,c.Kind);Assert.AreNotEqual(FlowCommandKind.UnloadWard,c.Kind);}
            Assert.IsTrue(freed);Assert.AreEqual(ActId.Machinery,f.CurrentAct);Assert.IsFalse(f.Struggle(),"no struggle to press once free");
        }
        [Test] public void LostStruggleKillsAndRestartsFromTheBed()
        {
            var f=Running();f.SetAct(ActId.Darkness);f.Catch();Press(f,GameFlowCore.StrugglePresses-1);f.ClearCommands();
            f.Tick(GameFlowCore.StruggleSeconds);Assert.IsFalse(f.Struggling);Assert.AreEqual(GameState.Caught,f.State);Assert.IsTrue(Has(f,FlowCommandKind.Fade));
            Assert.IsFalse(f.Struggle(),"too late");AssertBackInBed(f);
        }
        [Test] public void SecondGrabKillsAndTheNextLifeHasItsStruggleBack()
        {
            var f=Running();f.SetAct(ActId.Exit);f.Catch();Press(f,GameFlowCore.StrugglePresses);Assert.AreEqual(GameState.Playing,f.State);
            f.ClearCommands();Assert.IsTrue(f.Catch());Assert.IsFalse(f.Struggling,"the struggle was spent");Assert.IsTrue(Has(f,FlowCommandKind.Fade));
            AssertBackInBed(f);f.FadeDone();Assert.IsTrue(f.Catch());Assert.IsTrue(f.Struggling);
        }
        [Test] public void LosingFocusHoldsTheStruggleClock()
        {
            var f=Running();f.Catch();f.Focus(false);Assert.AreEqual(GameState.Caught,f.State,"a struggle never pauses");
            f.Tick(10);Assert.IsTrue(f.Struggling);f.Focus(true);f.Tick(GameFlowCore.StruggleSeconds-.01f);Assert.IsTrue(f.Struggling);f.Tick(.02f);Assert.IsFalse(f.Struggling);
        }
        [Test] public void CaughtCannotSkipReloadWithStateRequest()
        {
            var f=Running();f.Catch();Assert.IsFalse(f.Request(GameState.Playing),"nor the struggle");Assert.AreEqual(GameState.Caught,f.State);Assert.IsTrue(f.Struggling);
            Press(f,GameFlowCore.StrugglePresses);f.Catch();Assert.IsFalse(f.Request(GameState.Playing));Assert.AreEqual(GameState.Caught,f.State);
        }
        [Test] public void PauseDuringInitialLoadDefersDirectorUntilExplicitResume(){var f=new GameFlowCore();f.Start(true);f.FadeDone();f.Pause();f.ClearCommands();f.WardLoaded();Assert.AreEqual(GameState.Paused,f.State);Assert.IsFalse(Has(f,FlowCommandKind.StartRun));f.Focus(true);Assert.IsFalse(Has(f,FlowCommandKind.StartRun));f.Resume();Assert.IsTrue(Has(f,FlowCommandKind.StartRun));f.ClearCommands();f.WardLoaded();Assert.AreEqual(0,f.CommandCount);}
        [Test] public void RestartFromPausedAndComplete(){foreach(bool complete in new[]{false,true}){var f=Running();f.SetAct(ActId.Exit);if(complete){f.Request(GameState.Ending);f.Request(GameState.Complete);}else f.Pause();f.ClearCommands();Assert.IsTrue(f.Restart(false));Assert.IsFalse(f.Restart(false));f.FadeDone();f.WardUnloaded();f.ClearCommands();f.WardLoaded();Assert.AreEqual(GameState.Playing,f.State);Assert.AreEqual(ActId.Awakening,f.CurrentAct);Assert.IsTrue(Has(f,FlowCommandKind.StartRun));}}
        [Test] public void FocusLostDuringRestartRemainsPaused(){var f=Running();f.Pause();f.Restart(true);f.FadeDone();f.WardUnloaded();f.Focus(false);f.ClearCommands();f.WardLoaded();Assert.AreEqual(GameState.Paused,f.State);Assert.IsFalse(Has(f,FlowCommandKind.StartRun));f.Resume();Assert.IsTrue(Has(f,FlowCommandKind.StartRun));}
        [Test] public void CountdownNeverBecomesFailure(){Assert.AreEqual(120,CountdownDisplay.Remaining(0));Assert.AreEqual(1,CountdownDisplay.Remaining(119.5f));Assert.AreEqual(0,CountdownDisplay.Remaining(125));var f=Running();f.Tick(1000);Assert.AreEqual(GameState.Playing,f.State);}
        [Test] public void CorruptAndUnknownSettingsAreSafe(){var s=SettingsCodec.Decode("SeatedMode=true;Unknown=1;HeightOffsetMeters=NaN;SnapTurnDegrees=999;MasterVolume01=-2;Subtitles=oops;Turn=999;broken");Assert.IsTrue(s.SeatedMode);Assert.AreEqual(0,s.HeightOffsetMeters);Assert.AreEqual(60,s.SnapTurnDegrees);Assert.AreEqual(0,s.MasterVolume01);Assert.IsTrue(s.Subtitles);Assert.AreEqual(TurnMode.Snap,s.Turn);Assert.AreEqual(ExperienceSettings.Defaults.SubtitleScale,SettingsCodec.Decode(null).SubtitleScale);}
        [Test] public void SettingsRoundTripEveryFieldCultureIndependent(){var s=ExperienceSettings.Defaults;s.SeatedMode=true;s.HeightOffsetMeters=.35f;s.Turn=TurnMode.Smooth;s.Locomotion=LocomotionMode.TeleportAndSmooth;s.SnapTurnDegrees=60;s.ComfortVignette=false;s.DominantHand=HandSide.Left;s.OneHandedMode=true;s.PuzzleAssistAlwaysOn=true;s.Scares=ScareIntensity.Reduced;s.ReduceFlicker=true;s.SoundCueVisuals=true;s.Subtitles=false;s.SubtitleScale=1.5f;s.DarknessBrightness01=.8f;s.MasterVolume01=.3f;s.EffectsVolume01=.4f;s.VoiceVolume01=.5f;s.HapticsStrength01=.6f;var old=System.Globalization.CultureInfo.CurrentCulture;try{System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");var restored=SettingsCodec.Decode(SettingsCodec.Encode(s));foreach(var field in typeof(ExperienceSettings).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance))Assert.AreEqual(field.GetValue(s),field.GetValue(restored),field.Name);}finally{System.Globalization.CultureInfo.CurrentCulture=old;}}
    }
}
