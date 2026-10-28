using System;
using UnityEngine;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Integrates hold/decay with five-percent event throttling; invoked by lever shell.</summary>
    public sealed class LeverLogic
    {
        private float heldSeconds,lastPublished; private bool assisted;
        public float Progress => Mathf.Clamp01(heldSeconds/(assisted?1f:2f));
        public bool Complete { get; private set; }
        public void SetAssist(bool enabled) { assisted=enabled; }
        public bool Tick(bool held,float seconds)
        {
            if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
            if(Complete)return false;
            float requirement=assisted?1f:2f;
            heldSeconds=Mathf.Clamp(heldSeconds+(held?seconds:-seconds*requirement),0,requirement);
            Complete=Progress>=1;
            if(Math.Abs(Progress-lastPublished)+1e-6f<0.05f && !Complete)return false;
            if(Progress==lastPublished)return false;
            lastPublished=Progress;return true;
        }
    }
}
