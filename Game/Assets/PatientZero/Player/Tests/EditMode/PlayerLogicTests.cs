using NUnit.Framework;
using PatientZero.Core;
using PatientZero.Player.Logic;
using UnityEngine;
namespace PatientZero.Player.Tests
{
    public class PlayerLogicTests
    {
        [Test] public void SpeedsSmoothAndIgnoreTeleports(){var s=new SpeedEstimator();Assert.AreEqual(0,s.Sample(Vector3.zero,true,.1f));float speed=s.Sample(Vector3.right*.1f,true,.1f);Assert.Greater(speed,0);Assert.Less(speed,1);Assert.Greater(s.Sample(Vector3.right*.2f,true,.1f),speed);Assert.AreEqual(0,s.Sample(Vector3.right*3,true,.1f));Assert.AreEqual(0,s.Sample(Vector3.zero,false,.1f));Assert.AreEqual(0,s.Sample(Vector3.right,true,.1f));}
        [Test] public void TrackingLossUsesGraceAndSwitchCancelsBoth(){var t=new TrackingRecovery();t.Tick(InputModality.Hands,true,true,true,0);Assert.IsTrue(t.DropLeft);t.Tick(InputModality.Hands,true,true,true,.25f);Assert.IsTrue(t.Ready);t.Tick(InputModality.Hands,false,true,true,1.49f);Assert.IsFalse(t.DropLeft);Assert.IsFalse(t.ShowHandsLost);t.Tick(InputModality.Hands,false,true,true,.01f);Assert.IsTrue(t.DropLeft);Assert.IsTrue(t.ShowHandsLost);t.Tick(InputModality.Controllers,true,true,true,.1f);Assert.IsTrue(t.DropRight);Assert.IsFalse(t.ShowHandsLost);Assert.IsFalse(t.Ready);t.Tick(InputModality.Controllers,true,true,true,.25f);Assert.IsTrue(t.Ready);t.Tick(InputModality.Controllers,true,true,false,.1f);Assert.IsTrue(t.DropLeft);Assert.IsFalse(t.Ready);}
        [Test] public void EveryRequiredPuzzleReachableForSeatedAndStanding(){foreach(bool seated in new[]{false,true}){var s=ExperienceSettings.Defaults;s.SeatedMode=seated;Assert.GreaterOrEqual(ReachRules.CalibratedOffset(seated?1.1f:1.7f,s),0);foreach(var point in WardLayout.Points){if(point.Kind!=LayoutKind.Interactable || !point.Required)continue;bool reachable=false;foreach(var anchor in WardLayout.CriticalPath)if(anchor.Act==point.Act && ReachRules.CanReach(anchor.Position,point.Position))reachable=true;Assert.IsTrue(reachable,point.Id);}}}
    }
}
