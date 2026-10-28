// UNVERIFIED IN EDITOR — run only after Claude generates Bootstrap/Ward with the approved contract gate.
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
        private GameFlow flow;
        [UnitySetUp] public IEnumerator LoadBootstrap()
        {
            if(!Application.CanStreamedLevelBeLoaded("Bootstrap") || !Application.CanStreamedLevelBeLoaded("Ward"))Assert.Ignore("BLOCKED: Claude must generate approved scenes/build settings first.");
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single);
            flow=UnityEngine.Object.FindFirstObjectByType<GameFlow>();Assert.IsNotNull(flow);
            if(flow.Core.State==GameState.Calibration)flow.ConfirmCalibration();
            yield return WaitReady();
        }
        private IEnumerator WaitReady(){float deadline=Time.realtimeSinceStartup+20;while(flow.Core.IsTransitioning && Time.realtimeSinceStartup<deadline)yield return null;Assert.IsFalse(flow.Core.IsTransitioning,flow.BlockingDependency);}
        private static T[] Components<T>() where T:Component{var roots=SceneManager.GetSceneByName("Ward").GetRootGameObjects();var all=new System.Collections.Generic.List<T>();foreach(var root in roots)all.AddRange(root.GetComponentsInChildren<T>(true));return all.ToArray();}
        private static int[] Subscribers(){var fields=typeof(GameEvents).GetFields(BindingFlags.Public|BindingFlags.Static);var result=new int[fields.Length];for(int i=0;i<fields.Length;i++){var channel=fields[i].GetValue(null);result[i]=(int)channel.GetType().GetProperty("SubscriberCount").GetValue(channel);}return result;}
        [UnityTest] public IEnumerator RestartTenTimesReturnsEveryChannelToBaseline(){var baseline=Subscribers();for(int i=0;i<10;i++){flow.Pause();flow.RestartRun();yield return WaitReady();var counts=Subscribers();Assert.AreEqual(baseline.Length,counts.Length);for(int j=0;j<counts.Length;j++)Assert.AreEqual(baseline[j],counts[j],"Subscriber leak after Ward restart");}}
        [UnityTest] public IEnumerator PauseDuringWardLoadCannotStrandRun(){flow.Pause();flow.RestartRun();yield return null;flow.Core.Focus(false);float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline)yield return null;Assert.AreEqual(GameState.Paused,flow.Core.State);flow.Resume();yield return WaitReady();Assert.AreEqual(GameState.Playing,flow.Core.State);}
        [UnityTest] public IEnumerator RequiredPlugReturnsAfterFiveSeconds(){var props=Components<PhysicsProp>();PhysicsProp plug=null;foreach(var prop in props)if(prop.requiredItem)plug=prop;Assert.IsNotNull(plug);var home=plug.transform.position;plug.transform.position=home-Vector3.up*3;yield return new WaitForSeconds(5.3f);Assert.Less(Vector3.Distance(home,plug.transform.position),.2f);}
        [UnityTest] public IEnumerator AutoplayObjectivesReachCompleteWithoutBypassingDirectorGate()
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
