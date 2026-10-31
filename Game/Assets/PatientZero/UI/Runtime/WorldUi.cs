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
        private int lastClock=-1; private bool showSettings,ending; private float endingTime;
        private void Awake(){for(int i=0;i<clocks.Length;i++)clocks[i]=(i/60).ToString("00")+":"+(i%60).ToString("00");
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
        private void OnEnable(){GameEvents.CaptionRequested.Subscribe(Caption);GameEvents.AssistChanged.Subscribe(Assist);GameEvents.StateChanged.Subscribe(State);GameEvents.SettingsChanged.Subscribe(Settings);GameEvents.ActStarted.Subscribe(Act);}
        private void OnDisable(){GameEvents.CaptionRequested.Unsubscribe(Caption);GameEvents.AssistChanged.Unsubscribe(Assist);GameEvents.StateChanged.Unsubscribe(State);GameEvents.SettingsChanged.Unsubscribe(Settings);GameEvents.ActStarted.Unsubscribe(Act);}
        private void Caption(CaptionRequested c){if(settings && ((c.Kind==CaptionKind.Speech && !settings.Current.Subtitles) || (c.Kind==CaptionKind.Sound && !settings.Current.Subtitles && !settings.Current.SoundCueVisuals)))return;captions.Add(c.Text,Time.time,c.DurationSeconds,c.IsPositional,c.WorldPosition);}
        // E-16: each act opens with its objective, so the goal is clear before any assist is needed.
        private void Act(ActStarted a){if(!hint)return;switch(a.Act){case ActId.Awakening:hint.text="Get out of the room: pull the planks off the door.";break;case ActId.Machinery:hint.text="Restore power: vent the pressure valve, then plug in the cable.";break;case ActId.Darkness:hint.text="The lights are gone. Follow the chirp to the exit.";break;case ActId.Exit:hint.text="Pull down and hold the emergency release.";break;}
#if PZ_DESKTOP_MOUSE
            // E-18: on Windows each objective says which mouse action does it.
            switch(a.Act){case ActId.Awakening:hint.text="Get out: aim the + at a plank on the door, hold the left mouse button and drag. Pull off all 3 planks to open the door.";break;case ActId.Machinery:hint.text="Restore power: hold left mouse on the valve wheel and drag sideways to turn it. Then pick up the cable plug and carry it to the socket.";break;case ActId.Exit:hint.text="Aim at the emergency release, hold left mouse, drag down and keep holding.";break;}
#endif
        }
        private void Assist(AssistChanged a){if(!hint)return;switch(a.Objective){case ObjectiveId.Act1_DoorUnbarred:hint.text="Grab and pull the loose planks.";break;case ObjectiveId.Act2_ValveVented:hint.text="Turn the pressure valve. Re-grip freely.";break;case ObjectiveId.Act2_CablePlugged:hint.text="Carry the plug into the socket.";break;case ObjectiveId.Act3_BeaconReached:hint.text="Follow the chirp; teleport between anchors.";break;default:hint.text="Pull down and hold the emergency release.";break;}}
        private void State(GameStateChanged s)
        {
            if(calibration)calibration.SetActive(s.To==GameState.Calibration);if(pauseMenu)pauseMenu.SetActive(s.To==GameState.Paused);if(completeMenu)completeMenu.SetActive(s.To==GameState.Complete);
            if(caught)caught.SetActive(s.To==GameState.Caught);if(crosshair)crosshair.gameObject.SetActive(FlatScreen && s.To==GameState.Playing);if(credits && s.To!=GameState.Calibration && s.To!=GameState.Complete)credits.SetActive(false);
            if(s.To==GameState.Calibration && title)title.text="PATIENT ZERO — THE LAST 120 SECONDS";
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
