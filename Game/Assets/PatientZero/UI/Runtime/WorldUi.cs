// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
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
        public GameObject calibration,pauseMenu,completeMenu,credits,caught;public Text crosshair;
        /// <summary>E-22: the caught panel's two lines. E-23: shown only for a death; the struggle has no words on screen.</summary>
        public Text caughtTitle,caughtLine;
        /// <summary>E-23 (owner, 2026-11-11): nothing tells the player what to do while they play. The controls are on the main menu and the pause menu only.</summary>
        public const string HandsWarning="Hands not visible — bring them into view.";
        private int caughtShown=-1;
        /// <summary>Integration E-16: flat-screen builds (Windows keyboard and mouse, phone and iPad touch).</summary>
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
        public const bool FlatScreen=true;
#else
        public const bool FlatScreen=false;
#endif
        public const string DesktopControls="WASD move · mouse looks · aim the + and hold left mouse to grab, drag to pull or turn · Q throw · F flashlight · Esc pause";
        public const string TouchControlsHint="Left thumb moves · right thumb looks · tap to grab or drop · drag while holding · THROW · JUMP · LIGHT · II pauses";
        private readonly CaptionQueue captions=new CaptionQueue();
        private readonly string[] clocks=new string[121];
        public const string FloorTwoLabel="FLOOR 2 · LABS",GroundFloorLabel="GROUND FLOOR";
        private const float ClockX=390,ClockWidth=300; // right edge 540: inside a 4:3 screen's 550 canvas units
        private int lastClock=int.MinValue; private bool showSettings,ending; private float endingTime; // v1.3: -1 and -2 now mean the lower floors
        private void Awake(){for(int i=0;i<clocks.Length;i++)clocks[i]=(i/60).ToString("00")+":"+(i%60).ToString("00");
            // v1.3 (Claude integration E-19): a hint or floor name shrinks to fit rather than being cut at two lines, and the
            // clock moves up to the title row so it no longer sits on the hint's right edge.
            if(hint){hint.resizeTextForBestFit=true;hint.resizeTextMinSize=24;hint.resizeTextMaxSize=hint.fontSize;}
            if(countdown){countdown.resizeTextForBestFit=true;countdown.resizeTextMinSize=24;countdown.resizeTextMaxSize=countdown.fontSize;
                var clock=countdown.rectTransform;clock.anchoredPosition=new Vector2(ClockX,title?title.rectTransform.anchoredPosition.y:clock.anchoredPosition.y);clock.sizeDelta=new Vector2(ClockWidth,clock.sizeDelta.y);}
#if PZ_DESKTOP_MOUSE
            if(hint)hint.text=DesktopControls;
#elif PZ_TOUCH
            if(hint)hint.text=TouchControlsHint;
#endif
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
            // Integration E-18: without a headset the UI draws over the view. A world-space canvas 1.4 m ahead was hidden by
            // any wall nearer than that (the results screen in the 1.0.0 Windows build), so flat screens use an overlay.
            if(!UnityEngine.XR.XRSettings.isDeviceActive){var canvas=GetComponent<Canvas>();if(canvas){canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1100,800);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;}}
#endif
        }
        private void OnEnable(){GameEvents.CaptionRequested.Subscribe(Caption);GameEvents.StateChanged.Subscribe(State);GameEvents.SettingsChanged.Subscribe(Settings);}
        private void OnDisable(){GameEvents.CaptionRequested.Unsubscribe(Caption);GameEvents.StateChanged.Unsubscribe(State);GameEvents.SettingsChanged.Unsubscribe(Settings);}
        // E-23 (owner, 2026-11-11): sound captions ("[snarl: it heard you]") told the player what was happening; they now need
        // "Sound direction visuals" (off by default). Speech subtitles still follow "Subtitles".
        private void Caption(CaptionRequested c){if(settings && ((c.Kind==CaptionKind.Speech && !settings.Current.Subtitles) || (c.Kind==CaptionKind.Sound && !settings.Current.SoundCueVisuals)))return;captions.Add(c.Text,Time.time,c.DurationSeconds,c.IsPositional,c.WorldPosition);}
        // E-23 (owner, 2026-11-11): no objective or assist lines while playing (they were E-16 and E-18). The controls show on the
        // main menu and the pause menu, and nowhere else.
        private static string Controls =>
#if PZ_DESKTOP_MOUSE
            DesktopControls;
#elif PZ_TOUCH
            TouchControlsHint;
#else
            "Grab with pinch or grip.";
#endif
        private void State(GameStateChanged s)
        {
            if(calibration)calibration.SetActive(s.To==GameState.Calibration);if(pauseMenu)pauseMenu.SetActive(s.To==GameState.Paused);if(completeMenu)completeMenu.SetActive(s.To==GameState.Complete);
            if(hint)hint.text=s.To==GameState.Calibration || s.To==GameState.Paused?Controls:"";
            // E-22: the panel's lines are written as it opens, so the first frame of a grab never shows the last one's text.
            if(caught){caughtShown=-2;CaughtText();}if(crosshair)crosshair.gameObject.SetActive(FlatScreen && s.To==GameState.Playing);if(credits && s.To!=GameState.Calibration && s.To!=GameState.Complete)credits.SetActive(false);
            if(s.To==GameState.Calibration && title)title.text="PATIENT ZERO — THE LAST 120 SECONDS";
            if(s.To==GameState.Ending && s.From!=GameState.Paused){ending=true;endingTime=0;}
            // E-23 (owner, 2026-11-11): nothing in front of the screen during play; the pause menu keeps the name.
            if(s.To==GameState.Playing){ending=false;if(title)title.text="";}
            if(s.To==GameState.Paused && title)title.text="PATIENT ZERO";
            if(s.To==GameState.Complete){if(title)title.text="PATIENT ZERO — YOU WERE THE CARRIER";if(fader)fader.FadeTo(0,.15f);}
        }
        private void Settings(SettingsChanged s){if(captionTop)captionTop.fontSize=Mathf.RoundToInt(56*s.Settings.SubtitleScale);if(captionBottom)captionBottom.fontSize=Mathf.RoundToInt(56*s.Settings.SubtitleScale);}
        private void Update()
        {
            if(Services<IExperienceDirector>.TryGet(out var director))
            {
                // v1.3 (E-19): the ward keeps its clock; the lower floors show where the player is.
                int floor=WardLayout.FloorOf(director.CurrentAct),value=floor==1||floor==2?-floor:CountdownDisplay.Remaining(director.ExperienceTime);
                if(value!=lastClock){lastClock=value;if(countdown)countdown.text=value==-2?FloorTwoLabel:value==-1?GroundFloorLabel:clocks[value];}
            }
            captions.Expire(Time.time);DisplayCaptions();CaughtText();
            // A headset safety warning, not an instruction: it clears as soon as the hands are seen again.
            if(rig && hint){if(rig.HandsNotVisible)hint.text=HandsWarning;else if(hint.text==HandsWarning)hint.text="";}
            if(!ending || !flow || flow.Core.State!=GameState.Ending)return;endingTime+=Time.deltaTime;
            if(endingTime>=10 && endingTime-Time.deltaTime<10 && fader){fader.SetWhite(true);fader.FadeTo(1,1);}
            if(endingTime>=11 && endingTime-Time.deltaTime<11 && fader){fader.SetWhite(false);fader.FadeTo(1,.5f);}
        }
        /// <summary>
        /// E-23 (owner, 2026-11-11): while a zombie has the player nothing is written on screen; the zombie is in their face and
        /// any key, click, tap or hard shove fights it off (each press jolts the view: PlayerBody). Only a death shows the panel,
        /// which says so. Rewritten only when what it shows changes.
        /// </summary>
        private void CaughtText()
        {
            if(!caught)return;
            int shown=!flow || flow.Core.State!=GameState.Caught?-1:flow.Core.Struggling?1:0;
            if(shown==caughtShown)return;caughtShown=shown;
            caught.SetActive(shown==0);
            if(shown!=0)return;
            if(caughtTitle)caughtTitle.text="YOU DIED";if(caughtLine)caughtLine.text="";
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
