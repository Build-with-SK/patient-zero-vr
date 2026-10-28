// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using PatientZero.Flow.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PatientZero.Flow
{
    /// <summary>Bootstrap executor for pure flow commands; UI and platform events supply explicit inputs.</summary>
    public sealed class GameFlow : MonoBehaviour
    {
        public SettingsProvider settings;
        [Tooltip("Composer must place Ward in build settings.")] public string wardScene="Ward";
        private readonly GameFlowCore core=new GameFlowCore();private AsyncOperation sceneOperation;private bool unloading,waitingFade,waitingDirector;private FlowCommand startCommand;
        public GameFlowCore Core => core;
        public string BlockingDependency {get;private set;}
        private void OnEnable(){GameEvents.StateRequested.Subscribe(OnRequest);GameEvents.PlayerCaught.Subscribe(OnCaught);GameEvents.ActStarted.Subscribe(OnAct);}
        private void Start(){if(!settings){BlockingDependency="SettingsProvider missing";Debug.LogError(BlockingDependency,this);return;}core.Start(settings.CalibrationConfirmed);Drain();}
        private void OnDisable(){GameEvents.StateRequested.Unsubscribe(OnRequest);GameEvents.PlayerCaught.Unsubscribe(OnCaught);GameEvents.ActStarted.Unsubscribe(OnAct);Time.timeScale=1;AudioListener.pause=false;}
        public void ConfirmCalibration(){if(!settings)return;settings.ConfirmCalibration();core.Request(GameState.Playing);Drain();}
        public void Pause(){core.Pause();Drain();}
        public void Resume(){core.Resume();Drain();}
        public void RestartAct(){core.Restart(true);Drain();}
        public void RestartRun(){core.Restart(false);Drain();}
        private void OnRequest(GameStateRequest request){core.Request(request.Requested,request.Reason);Drain();}
        private void OnCaught(PlayerCaught caught){if(core.Catch())Cues.Play(CueId.CaughtHeartbeat,caught.ZombiePosition,NoiseOrigin.Ambient);Drain();}
        private void OnAct(ActStarted act){core.SetAct(act.Act);}
        private void OnApplicationFocus(bool focused){core.Focus(focused);Drain();}
        private void OnApplicationPause(bool paused){if(paused){core.Focus(false);Drain();}}
        private void Update()
        {
            core.Tick(Time.unscaledDeltaTime);
            if(sceneOperation!=null && sceneOperation.isDone){sceneOperation=null;if(unloading)core.WardUnloaded();else core.WardLoaded();}
            if(waitingDirector)TryStartDirector();
            if(waitingFade && !waitingDirector && Services<IScreenFader>.TryGet(out var fader) && !fader.IsFading){waitingFade=false;core.FadeDone();}
            Drain();
        }
        private readonly FlowCommand[] pending=new FlowCommand[32];private bool draining;
        private void Drain()
        {
            if(draining)return;draining=true;
            try { while(core.CommandCount>0){int count=core.CommandCount;for(int i=0;i<count;i++)pending[i]=core.CommandAt(i);core.ClearCommands();for(int i=0;i<count;i++)Execute(pending[i]);} }
            finally {draining=false;}
        }
        private void Execute(FlowCommand command)
        {
            switch(command.Kind)
            {
                case FlowCommandKind.TimeScale:Time.timeScale=command.Value;break;
                case FlowCommandKind.AudioPause:AudioListener.pause=command.Value>0;break;
                case FlowCommandKind.StateChanged:GameEvents.StateChanged.Raise(new GameStateChanged(command.From,command.To,command.Reason));break;
                case FlowCommandKind.Rejected:Debug.LogWarning("[PatientZero] Illegal or premature state request refused.",this);break;
                case FlowCommandKind.Fade:if(waitingDirector){startFade=command;break;}BeginFade(command);break;
                case FlowCommandKind.UnloadWard:unloading=true;sceneOperation=SceneManager.UnloadSceneAsync(wardScene);if(sceneOperation==null)Block("Ward unload failed");break;
                case FlowCommandKind.LoadWard:unloading=false;if(!Application.CanStreamedLevelBeLoaded(wardScene)){Block("Ward missing from build settings");break;}sceneOperation=SceneManager.LoadSceneAsync(wardScene,LoadSceneMode.Additive);break;
                case FlowCommandKind.StartRun:startCommand=command;waitingDirector=true;TryStartDirector();break;
            }
        }
        private FlowCommand startFade;
        private void TryStartDirector(){if(!Services<IExperienceDirector>.TryGet(out var director)){BlockingDependency="IExperienceDirector missing; waiting under fade";return;}if(core.State==GameState.Paused)return;waitingDirector=false;BlockingDependency=null;director.StartRun(startCommand.Act,startCommand.Restore);if(startFade.Kind==FlowCommandKind.Fade){BeginFade(startFade);startFade=default;}}
        private void BeginFade(FlowCommand command){if(!Services<IScreenFader>.TryGet(out var fader)){Block("IScreenFader missing; scene transition held");return;}fader.FadeTo(command.Value,command.Seconds);waitingFade=true;}
        private void Block(string message){BlockingDependency=message;Debug.LogError(message,this);core.Pause();}
    }
}
