// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using PatientZero.Flow;
using PatientZero.Flow.Logic;
using PatientZero.Player;
using PatientZero.UI.Logic;
using UnityEngine;
using UnityEngine.UI;
namespace PatientZero.UI
{
    /// <summary>Readable view-relative UI; XRI UI input drives menus for either hand/controller.</summary>
    public sealed class WorldUi : MonoBehaviour
    {
        public GameFlow flow; public SettingsProvider settings; public PlayerRig rig; public ScreenFader fader;
        public Text countdown,hint,captionTop,captionBottom,title,topArrow,bottomArrow;
        public GameObject calibration,pauseMenu,completeMenu,credits;
        private readonly CaptionQueue captions=new CaptionQueue();
        private readonly string[] clocks=new string[121];
        private int lastClock=-1; private bool showSettings,ending; private float endingTime;
        private void Awake(){for(int i=0;i<clocks.Length;i++)clocks[i]=(i/60).ToString("00")+":"+(i%60).ToString("00");}
        private void OnEnable(){GameEvents.CaptionRequested.Subscribe(Caption);GameEvents.AssistChanged.Subscribe(Assist);GameEvents.StateChanged.Subscribe(State);GameEvents.SettingsChanged.Subscribe(Settings);}
        private void OnDisable(){GameEvents.CaptionRequested.Unsubscribe(Caption);GameEvents.AssistChanged.Unsubscribe(Assist);GameEvents.StateChanged.Unsubscribe(State);GameEvents.SettingsChanged.Unsubscribe(Settings);}
        private void Caption(CaptionRequested c){if(settings && ((c.Kind==CaptionKind.Speech && !settings.Current.Subtitles) || (c.Kind==CaptionKind.Sound && !settings.Current.Subtitles && !settings.Current.SoundCueVisuals)))return;captions.Add(c.Text,Time.time,c.DurationSeconds,c.IsPositional,c.WorldPosition);}
        private void Assist(AssistChanged a){if(!hint)return;switch(a.Objective){case ObjectiveId.Act1_DoorUnbarred:hint.text="Grab and pull the loose planks.";break;case ObjectiveId.Act2_ValveVented:hint.text="Turn the pressure valve. Re-grip freely.";break;case ObjectiveId.Act2_CablePlugged:hint.text="Carry the plug into the socket.";break;case ObjectiveId.Act3_BeaconReached:hint.text="Follow the chirp; teleport between anchors.";break;default:hint.text="Pull down and hold the emergency release.";break;}}
        private void State(GameStateChanged s)
        {
            if(calibration)calibration.SetActive(s.To==GameState.Calibration);if(pauseMenu)pauseMenu.SetActive(s.To==GameState.Paused);if(completeMenu)completeMenu.SetActive(s.To==GameState.Complete);
            if(s.To==GameState.Ending && s.From!=GameState.Paused){ending=true;endingTime=0;}
            if(s.To==GameState.Playing){ending=false;if(title)title.text="PATIENT ZERO";}
            if(s.To==GameState.Complete){if(title)title.text="PATIENT ZERO — YOU WERE THE CARRIER";if(fader)fader.FadeTo(0,.15f);}
        }
        private void Settings(SettingsChanged s){if(captionTop)captionTop.fontSize=Mathf.RoundToInt(56*s.Settings.SubtitleScale);if(captionBottom)captionBottom.fontSize=Mathf.RoundToInt(56*s.Settings.SubtitleScale);}
        private void Update()
        {
            if(Services<IExperienceDirector>.TryGet(out var director)){int value=CountdownDisplay.Remaining(director.ExperienceTime);if(value!=lastClock){lastClock=value;if(countdown)countdown.text=clocks[value];}}
            captions.Expire(Time.time);DisplayCaptions();
            if(rig && rig.HandsNotVisible && hint)hint.text="Hands not visible — bring them into view.";
            if(!ending || !flow || flow.Core.State!=GameState.Ending)return;endingTime+=Time.deltaTime;
            if(endingTime>=10 && endingTime-Time.deltaTime<10 && fader){fader.SetWhite(true);fader.FadeTo(1,1);}
            if(endingTime>=11 && endingTime-Time.deltaTime<11 && fader){fader.SetWhite(false);fader.FadeTo(1,.5f);}
        }
        private void DisplayCaptions(){if(captionTop)captionTop.text=captions.Count>0?captions[0].Text:"";if(captionBottom)captionBottom.text=captions.Count>1?captions[1].Text:"";if(Services<IPlayerQuery>.TryGet(out var p)){Arrow(topArrow,0,p);Arrow(bottomArrow,1,p);}}
        private void Arrow(Text arrow,int index,IPlayerQuery p){if(!arrow)return;if(index>=captions.Count || !captions[index].Positional){arrow.text="";return;}switch(CaptionQueue.Direction(p.HeadPosition,p.HeadForward,captions[index].Position)){case CaptionDirection.Ahead:arrow.text="AHEAD";break;case CaptionDirection.Left:arrow.text="LEFT";break;case CaptionDirection.Right:arrow.text="RIGHT";break;default:arrow.text="BEHIND";break;}}
        public void Confirm(){if(flow)flow.ConfirmCalibration();}
        public void Resume(){if(flow)flow.Resume();}
        public void RestartAct(){if(flow)flow.RestartAct();}
        public void RestartRun(){if(flow)flow.RestartRun();}
        public void Credits(){if(credits)credits.SetActive(!credits.activeSelf);}
        public void Quit(){Application.Quit();}
        public void SettingsMenu(){showSettings=!showSettings;if(calibration)calibration.SetActive(showSettings);}
        public void Seated(bool value){if(!settings)return;var s=settings.Current;s.SeatedMode=value;settings.Apply(s);}
        public void Height(float value){if(!settings)return;var s=settings.Current;s.HeightOffsetMeters=value;settings.Apply(s);}
        public void Brightness(float value){if(!settings)return;var s=settings.Current;s.DarknessBrightness01=value;settings.Apply(s);}
        public void AssistAlways(bool value){if(!settings)return;var s=settings.Current;s.PuzzleAssistAlwaysOn=value;settings.Apply(s);}
        public void SoundVisuals(bool value){if(!settings)return;var s=settings.Current;s.SoundCueVisuals=value;settings.Apply(s);}
        public void ReduceFlicker(bool value){if(!settings)return;var s=settings.Current;s.ReduceFlicker=value;settings.Apply(s);}
        public void OneHanded(bool value){if(!settings)return;var s=settings.Current;s.OneHandedMode=value;settings.Apply(s);}
        public void Subtitles(bool value){if(!settings)return;var s=settings.Current;s.Subtitles=value;settings.Apply(s);}
        public void Vignette(bool value){if(!settings)return;var s=settings.Current;s.ComfortVignette=value;settings.Apply(s);}
        public void SubtitleScale(float value){if(!settings)return;var s=settings.Current;s.SubtitleScale=value;settings.Apply(s);}
        public void Master(float value){if(!settings)return;var s=settings.Current;s.MasterVolume01=value;settings.Apply(s);}
        public void Effects(float value){if(!settings)return;var s=settings.Current;s.EffectsVolume01=value;settings.Apply(s);}
        public void Voice(float value){if(!settings)return;var s=settings.Current;s.VoiceVolume01=value;settings.Apply(s);}
        public void Haptics(float value){if(!settings)return;var s=settings.Current;s.HapticsStrength01=value;settings.Apply(s);}
        public void ReducedScares(bool value){if(!settings)return;var s=settings.Current;s.Scares=value?ScareIntensity.Reduced:ScareIntensity.Standard;settings.Apply(s);}
        public void SmoothLocomotion(bool value){if(!settings)return;var s=settings.Current;s.Locomotion=value?LocomotionMode.TeleportAndSmooth:LocomotionMode.TeleportOnly;settings.Apply(s);}
        public void SmoothTurning(bool value){if(!settings)return;var s=settings.Current;s.Turn=value?TurnMode.Smooth:TurnMode.Snap;settings.Apply(s);}
        public void LeftHanded(bool value){if(!settings)return;var s=settings.Current;s.DominantHand=value?HandSide.Left:HandSide.Right;settings.Apply(s);}
        public void SnapDegrees(float value){if(!settings)return;var s=settings.Current;s.SnapTurnDegrees=Mathf.RoundToInt(value);settings.Apply(s);}
    }
}
