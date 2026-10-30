// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Player
{
    /// <summary>Full-view compositor UI fade; GameFlow and director call via Core service.</summary>
    public sealed class ScreenFader : MonoBehaviour,IScreenFader
    {
        public CanvasGroup overlay;public UnityEngine.UI.Image image;
        private float from,target,duration,elapsed;
        public bool IsFading {get;private set;}
        private void OnEnable(){if(!overlay){Debug.LogError("Fader overlay missing",this);enabled=false;return;}Services<IScreenFader>.Register(this);}
        private void OnDisable(){Services<IScreenFader>.Unregister(this);}
        public void FadeTo(float alpha01,float seconds){from=overlay.alpha;target=Mathf.Clamp01(alpha01);duration=Mathf.Max(.15f,seconds);elapsed=0;IsFading=true;}
        public void SetWhite(bool white){if(image)image.color=white?Color.white:Color.black;}
        private void Update(){if(!IsFading)return;elapsed+=Time.unscaledDeltaTime;overlay.alpha=Mathf.Lerp(from,target,elapsed/duration);if(elapsed>=duration)IsFading=false;}
    }
}
