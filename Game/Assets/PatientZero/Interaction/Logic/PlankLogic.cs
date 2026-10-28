using UnityEngine;
namespace PatientZero.Interaction.Logic
{
    /// <summary>Normal-axis pull and spring-back rules; called by the plank's grab adapter.</summary>
    public sealed class PlankLogic
    {
        public bool Assisted { get; set; }
        public bool Popped { get; private set; }
        public float PullMeters { get; private set; }
        public float Threshold => Assisted ? 0.04f : 0.08f;
        public float Intensity => Mathf.Clamp01(PullMeters/Threshold);
        public bool Pull(float normalDistance) { if(Popped)return false; PullMeters=float.IsNaN(normalDistance)?0:Mathf.Max(0,normalDistance); if(PullMeters+1e-6f<Threshold)return false; Popped=true; return true; }
        public void Release() { if(!Popped)PullMeters=0; }
    }
}
