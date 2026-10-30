// UNVERIFIED IN EDITOR — requires the actual pinned editor; never counted as a portable pass.
using System.Collections;
using NUnit.Framework;
using PatientZero.Core;
using UnityEngine;
using UnityEngine.TestTools;
namespace PatientZero.Interaction.Tests
{
    public sealed class DoorGatePlayTests
    {
        private sealed class Director : IExperienceDirector
        {
            public ActId CurrentAct {get;private set;}=ActId.Awakening;
            public float ExperienceTime=>0;public bool released;
            public void StartRun(ActId act,bool restore){CurrentAct=act;released=false;GameEvents.ActStarted.Raise(new ActStarted(act,restore,1));}
            public bool IsGateReleased(GateId gate)=>CurrentAct==ActId.Awakening && gate==GateId.Act1Door && released;
        }
        private sealed class Audio : IAudioCuePlayer
        {
            public int unlatches;
            public CueHandle Play(CueId cue,Vector3 position,float intensity01,NoiseOrigin origin){if(cue==CueId.DoorUnlatch)unlatches++;return default;}
            public CueHandle PlayAttached(CueId cue,Transform follow,float intensity01,NoiseOrigin origin)=>Play(cue,Vector3.zero,intensity01,origin);
            public void SetIntensity(CueHandle h,float i){}public void Stop(CueHandle h,float seconds){}
        }
        private GameObject root;private Director director;private Audio audio;private ObjectivePublisher publisher;private ObjectiveDoor door;private int opened;
        [SetUp] public void Setup(){opened=0;CoreStaticReset.ResetStatics();Time.timeScale=1;director=new Director();audio=new Audio();Services<IExperienceDirector>.Register(director);Services<IAudioCuePlayer>.Register(audio);root=new GameObject("Gate test");publisher=root.AddComponent<ObjectivePublisher>();director.StartRun(ActId.Awakening,false);GameEvents.ObjectiveCompleted.Subscribe(Completed);}
        private void Completed(ObjectiveCompleted e){if(e.Objective==ObjectiveId.Act1_DoorOpened)opened++;}
        private void CreateDoor(){var o=new GameObject("Door");o.transform.SetParent(root.transform);o.SetActive(false);door=o.AddComponent<ObjectiveDoor>();door.isolationDoor=true;door.objectives=publisher;var leaf=new GameObject("Leaf");leaf.transform.SetParent(o.transform);door.leaf=leaf.transform;door.openDegrees=3;o.SetActive(true);}
        private void Release(){director.released=true;GameEvents.GateReleased.Raise(new GateReleased(GateId.Act1Door));}
        [UnityTest] public IEnumerator LiveSignalOpensOnceAndRestartCloses(){CreateDoor();Release();Release();yield return new WaitForSeconds(.15f);Assert.IsTrue(door.IsOpen);Assert.AreEqual(1,opened);Assert.AreEqual(1,audio.unlatches);director.StartRun(ActId.Awakening,true);yield return null;Assert.IsFalse(door.IsOpen);Assert.IsFalse(director.IsGateReleased(GateId.Act1Door));Release();yield return new WaitForSeconds(.15f);Assert.IsTrue(door.IsOpen);Assert.AreEqual(2,opened);Assert.AreEqual(2,audio.unlatches);}
        [UnityTest] public IEnumerator LateCreationAndReenableUseRetainedQuery(){Release();CreateDoor();yield return new WaitForSeconds(.15f);Assert.IsTrue(door.IsOpen);door.gameObject.SetActive(false);door.gameObject.SetActive(true);Release();yield return new WaitForSeconds(.15f);Assert.AreEqual(1,opened);Assert.AreEqual(1,audio.unlatches);}
        [UnityTest] public IEnumerator SpuriousSignalCannotOverrideClosedQuery(){CreateDoor();GameEvents.GateReleased.Raise(new GateReleased(GateId.Act1Door));GameEvents.GateReleased.Raise(new GateReleased(GateId.None));yield return new WaitForSeconds(.15f);Assert.IsFalse(door.IsOpen);Assert.AreEqual(0,opened);Assert.AreEqual(0,audio.unlatches);}
        [UnityTest] public IEnumerator MissingDirectorRecoversWhenServiceArrives(){Services<IExperienceDirector>.Unregister(director);CreateDoor();director.released=true;Services<IExperienceDirector>.Register(director);yield return new WaitForSeconds(.15f);Assert.IsTrue(door.IsOpen);}
        [TearDown] public void Cleanup(){GameEvents.ObjectiveCompleted.Unsubscribe(Completed);if(root)Object.DestroyImmediate(root);Services<IExperienceDirector>.Unregister(director);Services<IAudioCuePlayer>.Unregister(audio);CoreStaticReset.ResetStatics();}
    }
}
