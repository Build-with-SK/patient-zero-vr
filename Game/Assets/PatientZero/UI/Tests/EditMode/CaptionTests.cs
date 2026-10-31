using NUnit.Framework;
using PatientZero.UI.Logic;
using UnityEngine;
namespace PatientZero.UI.Tests
{
    public class CaptionTests
    {
        [Test] public void TwoLineLimitAndDedupExtendsLifetime(){var q=new CaptionQueue();q.Add("one",0,2,false,Vector3.zero);q.Add("one",.5f,2,false,Vector3.zero);Assert.AreEqual(1,q.Count);q.Expire(2.1f);Assert.AreEqual(1,q.Count);q.Add("two",2.1f,2,false,Vector3.zero);q.Add("three",2.1f,2,false,Vector3.zero);Assert.AreEqual(2,q.Count);Assert.AreEqual("two",q[0].Text);q.Expire(5);Assert.AreEqual(0,q.Count);}
        [Test] public void SameTextAfterOneSecondCanBeSeparate(){var q=new CaptionQueue();q.Add("same",0,5,false,Vector3.zero);q.Add("same",1.1f,5,false,Vector3.zero);Assert.AreEqual(2,q.Count);}
        [Test] public void DirectionsRotateWithHead(){Assert.AreEqual(CaptionDirection.Ahead,CaptionQueue.Direction(Vector3.zero,Vector3.forward,Vector3.forward));Assert.AreEqual(CaptionDirection.Behind,CaptionQueue.Direction(Vector3.zero,Vector3.forward,-Vector3.forward));Assert.AreEqual(CaptionDirection.Left,CaptionQueue.Direction(Vector3.zero,Vector3.forward,-Vector3.right));Assert.AreEqual(CaptionDirection.Right,CaptionQueue.Direction(Vector3.zero,Vector3.forward,Vector3.right));Assert.AreEqual(CaptionDirection.Left,CaptionQueue.Direction(Vector3.zero,Vector3.right,Vector3.forward));}
    }
}
