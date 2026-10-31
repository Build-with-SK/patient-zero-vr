using System;
using UnityEngine;
namespace PatientZero.UI.Logic
{
    public enum CaptionDirection { Ahead,Left,Right,Behind }
    public readonly struct CaptionLine
    {
        public readonly string Text;public readonly float Until;public readonly bool Positional;public readonly Vector3 Position;
        public CaptionLine(string text,float until,bool positional,Vector3 position){Text=text;Until=until;Positional=positional;Position=position;}
    }
    /// <summary>Two fixed caption slots; menu shell feeds unscaled or simulation time explicitly.</summary>
    public sealed class CaptionQueue
    {
        private readonly CaptionLine[] lines=new CaptionLine[2];private readonly float[] last=new float[2];
        public int Count {get;private set;}
        public CaptionLine this[int i] => i>=0 && i<Count?lines[i]:throw new ArgumentOutOfRangeException(nameof(i));
        public void Expire(float now){for(int i=Count-1;i>=0;i--)if(lines[i].Until<=now)Remove(i);}
        public void Add(string text,float now,float duration,bool positional,Vector3 position)
        {
            Expire(now);if(string.IsNullOrEmpty(text) || duration<=0)return;
            for(int i=0;i<Count;i++)if(lines[i].Text==text && now-last[i]<=1f){lines[i]=new CaptionLine(text,Math.Max(lines[i].Until,now+duration),positional,position);last[i]=now;return;}
            if(Count==2)Remove(0);lines[Count]=new CaptionLine(text,now+duration,positional,position);last[Count]=now;Count++;
        }
        private void Remove(int index){for(int i=index;i<Count-1;i++){lines[i]=lines[i+1];last[i]=last[i+1];}Count--;}
        public static CaptionDirection Direction(Vector3 head,Vector3 forward,Vector3 sound)
        {
            var delta=Vector3.ProjectOnPlane(sound-head,Vector3.up).normalized;var facing=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
            float dot=Vector3.Dot(facing,delta);if(dot>=0.7071067f)return CaptionDirection.Ahead;if(dot<=-0.7071067f)return CaptionDirection.Behind;
            return Vector3.Dot(Vector3.Cross(Vector3.up,facing),delta)>=0?CaptionDirection.Right:CaptionDirection.Left;
        }
    }
}
