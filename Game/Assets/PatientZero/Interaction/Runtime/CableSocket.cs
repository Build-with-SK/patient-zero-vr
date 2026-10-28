// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using UnityEngine;
namespace PatientZero.Interaction
{
    /// <summary>Socket snap logic with assist and a kinematic cable visual; never a physics joint chain.</summary>
    public sealed class CableSocket : MonoBehaviour
    {
        public PhysicsProp plug;public ObjectivePublisher objectives;public LineRenderer cable;private Vector3 hook;private bool assist,complete;
        private void Awake(){if(plug)hook=plug.transform.position;}
        private void OnEnable(){GameEvents.AssistChanged.Subscribe(Assist);GameEvents.ActStarted.Subscribe(Act);GameEvents.SettingsChanged.Subscribe(Settings);}
        private void OnDisable(){GameEvents.AssistChanged.Unsubscribe(Assist);GameEvents.ActStarted.Unsubscribe(Act);GameEvents.SettingsChanged.Unsubscribe(Settings);}
        private void Assist(AssistChanged a){if(a.Objective==ObjectiveId.Act2_CablePlugged)assist=a.Level==AssistLevel.Assist;}
        private void Settings(SettingsChanged s){if(s.Settings.PuzzleAssistAlwaysOn)assist=true;}
        private void Act(ActStarted act){complete=act.Act>ActId.Machinery;if(complete && plug){plug.CancelSelection();plug.Body.isKinematic=true;plug.transform.SetPositionAndRotation(transform.position,transform.rotation);}assist=Services<ISettingsProvider>.TryGet(out var s) && s.Current.PuzzleAssistAlwaysOn;}
        public bool TryConnect(){if(complete || !plug || !objectives || !objectives.Live || objectives.Act!=ActId.Machinery)return false;if(!SocketLogic.CanSnap(Vector3.Distance(plug.transform.position,transform.position),Quaternion.Angle(plug.transform.rotation,transform.rotation),assist))return false;complete=true;plug.CancelSelection();plug.Body.isKinematic=true;plug.transform.SetPositionAndRotation(transform.position,transform.rotation);objectives.Complete(ObjectiveId.Act2_CablePlugged);Cues.Play(CueId.PlugSpark,transform.position,NoiseOrigin.Player);return true;}
        private void Update(){if(!complete)TryConnect();if(!cable || !plug)return;cable.SetPosition(0,hook);cable.SetPosition(1,(hook+plug.transform.position)*.5f+Vector3.down*.15f);cable.SetPosition(2,plug.transform.position);}
    }
}
