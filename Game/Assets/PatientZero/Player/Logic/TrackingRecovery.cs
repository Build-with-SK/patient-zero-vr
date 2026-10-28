using System;
using PatientZero.Core;
namespace PatientZero.Player.Logic
{
    /// <summary>Stable tracking gate for rig adapters; switch/focus loss cancels grabs and requires re-grip.</summary>
    public sealed class TrackingRecovery
    {
        private InputModality modality;private float settled,leftLost,rightLost;
        public bool Ready {get;private set;}
        public bool ShowHandsLost => modality==InputModality.Hands && (leftLost>=1.5f || rightLost>=1.5f);
        public bool DropLeft {get;private set;} public bool DropRight {get;private set;}
        public void Tick(InputModality next,bool left,bool right,bool focus,float seconds)
        {
            if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
            bool cancel=next!=modality || !focus;
            DropLeft=DropRight=cancel;modality=next;
            leftLost=left?0:leftLost+seconds;rightLost=right?0:rightLost+seconds;
            if(next==InputModality.Hands){DropLeft|=leftLost>=1.5f;DropRight|=rightLost>=1.5f;}
            if(cancel || next==InputModality.None || (!left && !right)){settled=0;Ready=false;return;}
            settled+=seconds;Ready=settled>=0.25f;
        }
    }
}
