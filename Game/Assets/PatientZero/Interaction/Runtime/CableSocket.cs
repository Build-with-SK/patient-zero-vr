// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Interaction.Logic;
using UnityEngine;
namespace PatientZero.Interaction
{
    /// <summary>Socket snap logic with assist and a kinematic cable visual; never a physics joint chain.</summary>
    public sealed class CableSocket : MonoBehaviour
    {
        public PhysicsProp plug;public ObjectivePublisher objectives;public LineRenderer cable;private Vector3 hook;private bool assist,complete;
        // v1.3 (E-19): the Floor 2 fuse box is this socket again. The defaults are the ward's cable socket.
        public ActId socketAct=ActId.Machinery;public ObjectiveId socketObjective=ObjectiveId.Act2_CablePlugged;
        private void Awake(){if(plug)hook=plug.transform.position;}
        private void OnEnable(){GameEvents.AssistChanged.Subscribe(Assist);GameEvents.ActStarted.Subscribe(Act);GameEvents.SettingsChanged.Subscribe(Settings);}
        private void OnDisable(){GameEvents.AssistChanged.Unsubscribe(Assist);GameEvents.ActStarted.Unsubscribe(Act);GameEvents.SettingsChanged.Unsubscribe(Settings);}
        private void Assist(AssistChanged a){if(a.Objective==socketObjective)assist=a.Level==AssistLevel.Assist;}
        private void Settings(SettingsChanged s){if(s.Settings.PuzzleAssistAlwaysOn)assist=true;}
        private void Act(ActStarted act){complete=act.Act>socketAct;if(complete && plug){plug.CancelSelection();plug.Body.isKinematic=true;plug.transform.SetPositionAndRotation(transform.position,transform.rotation);}assist=Services<ISettingsProvider>.TryGet(out var s) && s.Current.PuzzleAssistAlwaysOn;}
        public bool TryConnect(){if(complete || !plug || !objectives || !objectives.Live || objectives.Act!=socketAct)return false;float distance=Vector3.Distance(plug.transform.position,transform.position);if(!(Flat?SocketLogic.CanSnapFlat(distance):SocketLogic.CanSnap(distance,Quaternion.Angle(plug.transform.rotation,transform.rotation),assist)))return false;complete=true;plug.CancelSelection();plug.Body.isKinematic=true;plug.transform.SetPositionAndRotation(transform.position,transform.rotation);objectives.Complete(socketObjective);Cues.Play(CueId.PlugSpark,transform.position,NoiseOrigin.Player);return true;}
        // E-19: mouse and touch players carry the plug .65 m ahead with physics; it stops at the socket's collider, never inside it.
        private static bool Flat
        {
            get
            {
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
                return !UnityEngine.XR.XRSettings.isDeviceActive;
#else
                return false;
#endif
            }
        }
        private void Update(){if(!complete)TryConnect();if(!cable || !plug)return;cable.SetPosition(0,hook);cable.SetPosition(1,(hook+plug.transform.position)*.5f+Vector3.down*.15f);cable.SetPosition(2,plug.transform.position);}
    }
}
