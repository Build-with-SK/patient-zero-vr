// UNVERIFIED IN EDITOR — run with pinned Unity 6000.3.27f1 after Bootstrap/Ward composition.
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Flow;
using PatientZero.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace PatientZero.Flow.Tests
{
    public sealed class IntegrationPlayTests
    {
        private GameFlow flow;private bool hadCalibration;private int savedCalibration;
        [UnitySetUp] public IEnumerator LoadBootstrap()
        {
            if(!Application.CanStreamedLevelBeLoaded("Bootstrap") || !Application.CanStreamedLevelBeLoaded("Ward"))Assert.Ignore("BLOCKED: Claude must generate approved scenes/build settings first.");
            // Integration E-14: the calibration flag lives in the owner's PlayerPrefs; tests must not change it.
            hadCalibration=PlayerPrefs.HasKey(SettingsProvider.CalibrationKey);savedCalibration=PlayerPrefs.GetInt(SettingsProvider.CalibrationKey,0);
            yield return LoadAndStart();
            if(flow.Core.State==GameState.Calibration)flow.ConfirmCalibration();
            yield return WaitReady();
        }
        [TearDown] public void RestoreCalibration(){Time.timeScale=1;if(hadCalibration)PlayerPrefs.SetInt(SettingsProvider.CalibrationKey,savedCalibration);else PlayerPrefs.DeleteKey(SettingsProvider.CalibrationKey);PlayerPrefs.Save();}
        private IEnumerator LoadAndStart()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);
            flow=UnityEngine.Object.FindFirstObjectByType<GameFlow>();Assert.IsNotNull(flow);
            // FIX-A-9 (Codex 2's playmode-calibration-resolution.patch): scene load completion can precede GameFlow.Start.
            float initializeDeadline=Time.realtimeSinceStartup+5;
            while(flow.Core.State==GameState.Boot && Time.realtimeSinceStartup<initializeDeadline)yield return null;
            Assert.AreNotEqual(GameState.Boot,flow.Core.State,"GameFlow.Start did not initialize");
        }
        private IEnumerator WaitReady(){float deadline=Time.realtimeSinceStartup+20;while(flow.Core.IsTransitioning && Time.realtimeSinceStartup<deadline)yield return null;Assert.IsFalse(flow.Core.IsTransitioning,flow.BlockingDependency);}
        private static T[] Components<T>() where T:Component{var roots=SceneManager.GetSceneByName("Ward").GetRootGameObjects();var all=new System.Collections.Generic.List<T>();foreach(var root in roots)all.AddRange(root.GetComponentsInChildren<T>(true));return all.ToArray();}
        private static int[] Subscribers(){var fields=typeof(GameEvents).GetFields(BindingFlags.Public|BindingFlags.Static);var result=new int[fields.Length];for(int i=0;i<fields.Length;i++){var channel=fields[i].GetValue(null);result[i]=(int)channel.GetType().GetProperty("SubscriberCount").GetValue(channel);}return result;}
        [UnityTest] public IEnumerator RestartTenTimesReturnsEveryChannelToBaseline(){var baseline=Subscribers();for(int i=0;i<10;i++){flow.Pause();flow.RestartRun();yield return WaitReady();var counts=Subscribers();Assert.AreEqual(baseline.Length,counts.Length);for(int j=0;j<counts.Length;j++)Assert.AreEqual(baseline[j],counts[j],"Subscriber leak after Ward restart");}}
        [UnityTest] public IEnumerator PauseDuringWardLoadCannotStrandRun(){flow.Pause();flow.RestartRun();yield return null;flow.Core.Focus(false);float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline)yield return null;Assert.AreEqual(GameState.Paused,flow.Core.State);flow.Resume();yield return WaitReady();Assert.AreEqual(GameState.Playing,flow.Core.State);}
        [UnityTest] public IEnumerator RequiredPlugReturnsAfterFiveSeconds(){var props=Components<PhysicsProp>();PhysicsProp plug=null;foreach(var prop in props)if(prop.requiredItem)plug=prop;Assert.IsNotNull(plug);var home=plug.transform.position;plug.transform.position=home-Vector3.up*3;yield return new WaitForSeconds(5.3f);Assert.Less(Vector3.Distance(home,plug.transform.position),.2f);}
        /// <summary>FIX-A-8: a pause while the view is under the opaque fade must show the pause menu, not a black screen.</summary>
        [UnityTest] public IEnumerator PauseUnderOpaqueFadeShowsMenuAboveFader()
        {
            var ui=UnityEngine.Object.FindFirstObjectByType<PatientZero.UI.WorldUi>();var fader=UnityEngine.Object.FindFirstObjectByType<PatientZero.Player.ScreenFader>();
            Assert.IsNotNull(ui);Assert.IsNotNull(fader);
            flow.Pause();flow.RestartRun();
            float deadline=Time.realtimeSinceStartup+5;while(fader.overlay.alpha<.99f && Time.realtimeSinceStartup<deadline)yield return null;
            flow.Core.Focus(false);yield return null;
            Assert.AreEqual(GameState.Paused,flow.Core.State);Assert.GreaterOrEqual(fader.overlay.alpha,.99f,"view is not under the opaque fade");
            Assert.IsTrue(ui.pauseMenu.activeInHierarchy,"pause menu is not shown while paused");
            var menu=ui.pauseMenu.GetComponent<Canvas>();var fade=fader.overlay.GetComponent<Canvas>();
            Assert.IsNotNull(menu,"pause menu has no canvas of its own, so it sorts with the HUD under the fader");
            Assert.Greater(menu.sortingOrder,fade.sortingOrder,"pause menu is drawn under the full-view fader: black screen");
            flow.Resume();yield return WaitReady();Assert.AreEqual(GameState.Playing,flow.Core.State);
        }
        /// <summary>A fresh install starts at calibration; confirming persists; reset brings the screen back.</summary>
        [UnityTest] public IEnumerator FreshInstallStartsAtCalibrationAndResetRestoresIt()
        {
            SettingsProvider.ResetCalibration();
            yield return LoadAndStart();
            Assert.AreEqual(GameState.Calibration,flow.Core.State);
            var ui=UnityEngine.Object.FindFirstObjectByType<PatientZero.UI.WorldUi>();Assert.IsNotNull(ui);Assert.IsTrue(ui.calibration.activeInHierarchy,"calibration screen hidden");
            flow.ConfirmCalibration();yield return WaitReady();
            Assert.AreEqual(GameState.Playing,flow.Core.State);Assert.AreEqual(1,PlayerPrefs.GetInt(SettingsProvider.CalibrationKey,0));
            SettingsProvider.ResetCalibration();Assert.IsFalse(PlayerPrefs.HasKey(SettingsProvider.CalibrationKey));
        }
        /// <summary>The 120-second countdown reaching zero never fails the run; a restart afterwards starts a fresh clock.</summary>
        [UnityTest] public IEnumerator RestartAfterCountdownExpiresStartsFreshClock()
        {
            Assert.IsTrue(Services<IExperienceDirector>.TryGet(out var director));
            float deadline=Time.realtimeSinceStartup+90;
            while(director.ExperienceTime<121 && Time.realtimeSinceStartup<deadline){if(flow.Core.State==GameState.Playing)Time.timeScale=20;yield return null;}
            Time.timeScale=1;
            Assert.GreaterOrEqual(director.ExperienceTime,121f,"clock did not pass 120 s");
            Assert.AreEqual(0,PatientZero.Flow.Logic.CountdownDisplay.Remaining(director.ExperienceTime));
            float waitIdle=Time.realtimeSinceStartup+20;while((flow.Core.IsTransitioning || flow.Core.State!=GameState.Playing) && Time.realtimeSinceStartup<waitIdle)yield return null;
            flow.Pause();Assert.IsTrue(flow.Core.State==GameState.Paused);
            flow.RestartRun();yield return WaitReady();
            Assert.AreEqual(GameState.Playing,flow.Core.State);Assert.AreEqual(ActId.Awakening,flow.Core.CurrentAct);
            Assert.Less(director.ExperienceTime,10f,"restart kept the old clock");
            Assert.Greater(PatientZero.Flow.Logic.CountdownDisplay.Remaining(director.ExperienceTime),110);
        }
        [UnityTest,Timeout(300000)] public IEnumerator AutoplayObjectivesReachCompleteWithoutBypassingDirectorGate()
        {
            float deadline=Time.realtimeSinceStartup+200;ActId previous=ActId.None;bool acted=false;
            while(flow.Core.State!=GameState.Complete && Time.realtimeSinceStartup<deadline)
            {
                if(flow.Core.CurrentAct!=previous){previous=flow.Core.CurrentAct;acted=false;}
                if(!acted && flow.Core.State==GameState.Playing && !flow.Core.IsTransitioning)
                {
                    acted=true;var puzzles=Components<PhysicalPuzzle>();
                    if(previous==ActId.Awakening){foreach(var p in puzzles)if(p.mechanic==PhysicalPuzzle.Mechanic.Plank){var point=p.transform.position;p.BeginGrip(point);p.SampleGrip(point+p.transform.TransformDirection(p.axis)*.081f,.1f);p.EndGrip();}}
                    if(previous==ActId.Machinery){foreach(var p in puzzles)if(p.mechanic==PhysicalPuzzle.Mechanic.Valve){var center=p.transform.position;p.BeginGrip(center+Vector3.right*.2f);for(int step=1;step<=6;step++)p.SampleGrip(center+Quaternion.AngleAxis(step*90,p.transform.TransformDirection(p.axis))*Vector3.right*.2f,1);p.EndGrip();}var socket=Components<CableSocket>()[0];socket.plug.transform.SetPositionAndRotation(socket.transform.position,socket.transform.rotation);Assert.IsTrue(socket.TryConnect());}
                    if(previous==ActId.Darkness){var query=Services<IPlayerQuery>.Instance as PatientZero.Player.PlayerRig;Assert.IsNotNull(query);query.origin.MoveCameraToWorldLocation(WardLayout.A3_End.Position+Vector3.up*(query.HeadPosition.y-query.FloorPosition.y));}
                    if(previous==ActId.Exit){foreach(var p in puzzles)if(p.mechanic==PhysicalPuzzle.Mechanic.Lever){var point=p.transform.position;p.BeginGrip(point);p.SampleGrip(point-Vector3.up*.05f,.1f);yield return new WaitForSeconds(2.1f);p.EndGrip();}}
                }
                yield return null;
            }
            Assert.AreEqual(GameState.Complete,flow.Core.State,"If stalled in Awakening: approved director-to-door gate is still missing. No bypass is allowed.");
        }
    }
}
