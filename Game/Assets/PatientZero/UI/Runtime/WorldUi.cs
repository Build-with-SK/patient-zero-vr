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
        /// <summary>E-22: the caught panel's two lines, rewritten while a zombie has the player (the struggle, then death).</summary>
        public Text caughtTitle,caughtLine;
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
        private void OnEnable(){GameEvents.CaptionRequested.Subscribe(Caption);GameEvents.AssistChanged.Subscribe(Assist);GameEvents.StateChanged.Subscribe(State);GameEvents.SettingsChanged.Subscribe(Settings);GameEvents.ActStarted.Subscribe(Act);GameEvents.ObjectiveCompleted.Subscribe(Completed);}
        private void OnDisable(){GameEvents.CaptionRequested.Unsubscribe(Caption);GameEvents.AssistChanged.Unsubscribe(Assist);GameEvents.StateChanged.Unsubscribe(State);GameEvents.SettingsChanged.Unsubscribe(Settings);GameEvents.ActStarted.Unsubscribe(Act);GameEvents.ObjectiveCompleted.Unsubscribe(Completed);}
        // E-22: once the bar is pushed the door opens onto the zombie; the "push the bar" line must not stay on screen through it.
        private void Completed(ObjectiveCompleted c){if(hint && c.Objective==ObjectiveId.Act1_DoorUnbarred)hint.text="";}
        private void Caption(CaptionRequested c){if(settings && ((c.Kind==CaptionKind.Speech && !settings.Current.Subtitles) || (c.Kind==CaptionKind.Sound && !settings.Current.Subtitles && !settings.Current.SoundCueVisuals)))return;captions.Add(c.Text,Time.time,c.DurationSeconds,c.IsPositional,c.WorldPosition);}
        // E-16: each act opens with its objective, so the goal is clear before any assist is needed.
        private void Act(ActStarted a){if(!hint)return;switch(a.Act){case ActId.Awakening:hint.text="Get out of the room: grab the bar on the door and push it open.";break;case ActId.Machinery:hint.text="Restore power: vent the pressure valve, then plug in the cable.";break;case ActId.Darkness:hint.text="The lights are gone. Follow the chirp to the exit.";break;case ActId.Exit:hint.text="Pull down and hold the emergency release, then take the stairs down.";break;
                // v1.3 (Claude integration E-19): the lower floors.
                case ActId.Laboratory:hint.text="Floor 2. Put the spare fuse from Lab A into the fuse box, then pull the stair door override. Lab B is not empty: keep quiet.";break;
                case ActId.Lobby:hint.text="Ground floor. Prime the generator in Security, then pull the lockdown release by the main doors. Something walks the lobby.";break;}
#if PZ_DESKTOP_MOUSE
            // E-18: on Windows each objective says which mouse action does it.
            switch(a.Act){case ActId.Awakening:hint.text="Get out: aim the + at the bar on the door, hold the left mouse button and drag to push the door open.";break;case ActId.Machinery:hint.text="Restore power: hold left mouse on the valve wheel and drag sideways to turn it. Then pick up the cable plug and carry it to the socket.";break;case ActId.Exit:hint.text="Aim at the emergency release, hold left mouse, drag down and keep holding. Then go through the door and down the stairs.";break;
                case ActId.Laboratory:hint.text="Floor 2: hold left mouse on the fuse in Lab A (north door) and carry it to the fuse box by the stair door. Then hold the override lever down. Walk slowly past Lab B.";break;
                case ActId.Lobby:hint.text="Ground floor: in Security (north) hold left mouse on the generator valve and drag sideways. Then hold the lockdown lever down by the main doors. Keep away from the guard.";break;}
#endif
        }
        private void Assist(AssistChanged a){if(!hint)return;switch(a.Objective){case ObjectiveId.Act1_DoorUnbarred:hint.text="Grab the bar on the door and push.";break;case ObjectiveId.Act2_ValveVented:hint.text="Turn the pressure valve. Re-grip freely.";break;case ObjectiveId.Act2_CablePlugged:hint.text="Carry the plug into the socket.";break;case ObjectiveId.Act3_BeaconReached:hint.text="Follow the chirp; teleport between anchors.";break;case ObjectiveId.Act4_ReleasePulled:hint.text="Pull down and hold the emergency release.";break;
                case ObjectiveId.Act4_StairsReached:hint.text="The door is open: go through it and down the stairs.";break;
                case ObjectiveId.Act5_FuseInserted:hint.text="The spare fuse is on the bench in Lab A. Carry it to the fuse box.";break;
                case ObjectiveId.Act5_OverridePulled:hint.text="Pull down and hold the stair door override.";break;
                case ObjectiveId.Act5_StairsReached:hint.text="The stair door is open: take the stairs down.";break;
                case ObjectiveId.Act6_GeneratorPrimed:hint.text="Turn the generator valve in the Security office.";break;
                case ObjectiveId.Act6_LockdownReleased:hint.text="Pull down and hold the lockdown release by the main doors.";break;}}
        private void State(GameStateChanged s)
        {
            if(calibration)calibration.SetActive(s.To==GameState.Calibration);if(pauseMenu)pauseMenu.SetActive(s.To==GameState.Paused);if(completeMenu)completeMenu.SetActive(s.To==GameState.Complete);
            // E-22: the panel's lines are written as it opens, so the first frame of a grab never shows the last one's text.
            if(caught){caught.SetActive(s.To==GameState.Caught);caughtShown=-1;CaughtText();}if(crosshair)crosshair.gameObject.SetActive(FlatScreen && s.To==GameState.Playing);if(credits && s.To!=GameState.Calibration && s.To!=GameState.Complete)credits.SetActive(false);
            if(s.To==GameState.Calibration && title)title.text="PATIENT ZERO — THE LAST 120 SECONDS";
            if(s.To==GameState.Ending && s.From!=GameState.Paused){ending=true;endingTime=0;}
            if(s.To==GameState.Playing){ending=false;if(title)title.text="PATIENT ZERO";}
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
            if(rig && rig.HandsNotVisible && hint)hint.text="Hands not visible — bring them into view.";
            if(!ending || !flow || flow.Core.State!=GameState.Ending)return;endingTime+=Time.deltaTime;
            if(endingTime>=10 && endingTime-Time.deltaTime<10 && fader){fader.SetWhite(true);fader.FadeTo(1,1);}
            if(endingTime>=11 && endingTime-Time.deltaTime<11 && fader){fader.SetWhite(false);fader.FadeTo(1,.5f);}
        }
        /// <summary>
        /// E-22: while a zombie has the player the panel says how to break free and how far they have got; once the struggle is
        /// lost (or on a second grab) it says they died and go back to bed. Rewritten only when what it shows changes.
        /// </summary>
        private void CaughtText()
        {
            if(!flow || !caughtTitle || !caughtLine || flow.Core.State!=GameState.Caught){caughtShown=-1;return;}
            int shown=flow.Core.Struggling?1+Mathf.RoundToInt(flow.Core.StruggleProgress*GameFlowCore.StrugglePresses):0;
            if(shown==caughtShown)return;caughtShown=shown;
            if(shown==0){caughtTitle.text="YOU DIED";caughtLine.text="Back to your bed.";return;}
            // The count sits in the title: after the instruction it wrapped onto a second line the panel cuts off.
            caughtTitle.text="IT HAS YOU   "+(shown-1)+" / "+GameFlowCore.StrugglePresses;
            caughtLine.text=StruggleHow;
        }
        private static string StruggleHow => UnityEngine.XR.XRSettings.isDeviceActive?"Shove it off: push hard with your hands!":
#if PZ_TOUCH
            "Tap fast to break free!";
#else
            "Press SPACE or click fast to break free!";
#endif
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
