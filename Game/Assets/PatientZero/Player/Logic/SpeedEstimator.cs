using System;
using UnityEngine;
namespace PatientZero.Player.Logic
{
    /// <summary>Exponential velocity smoothing for a tracked pose; teleport and loss clear history.</summary>
    public sealed class SpeedEstimator
    {
        private Vector3 previous;private bool initialized;
        public float Speed {get;private set;}
        public float Sample(Vector3 position,bool tracked,float seconds,float response=12f)
        {
            if(!tracked || seconds<=0 || float.IsNaN(seconds) || float.IsInfinity(seconds)){initialized=false;Speed=0;return 0;}
            if(!initialized){previous=position;initialized=true;return Speed=0;}
            float distance=Vector3.Distance(previous,position);previous=position;
            if(distance>1f || float.IsNaN(distance) || float.IsInfinity(distance))return Speed=0;
            float alpha=1f-(float)Math.Exp(-Math.Max(0,response)*seconds);
            Speed+=(distance/seconds-Speed)*alpha;return Speed;
        }
    }
}
