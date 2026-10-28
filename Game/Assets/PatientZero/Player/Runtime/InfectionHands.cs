// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Player
{
    /// <summary>Prebuilt vein overlays follow either tracked or controller hands; director owns reveal timing.</summary>
    public sealed class InfectionHands : MonoBehaviour
    {
        public Renderer[] veins=new Renderer[0];private MaterialPropertyBlock block;
        private void Awake(){block=new MaterialPropertyBlock();}
        private void OnEnable(){GameEvents.InfectionReveal.Subscribe(Reveal);GameEvents.ActStarted.Subscribe(Reset);}
        private void OnDisable(){GameEvents.InfectionReveal.Unsubscribe(Reveal);GameEvents.ActStarted.Unsubscribe(Reset);}
        private void Reset(ActStarted act){Reveal(new InfectionReveal(0));}
        private void Reveal(InfectionReveal reveal){block.SetColor("_BaseColor",Color.Lerp(new Color(.4f,.1f,.1f,0),new Color(.12f,.015f,.07f,1),reveal.Amount01));for(int i=0;i<veins.Length;i++)if(veins[i]){veins[i].enabled=reveal.Amount01>0;veins[i].SetPropertyBlock(block);}}
    }
}
