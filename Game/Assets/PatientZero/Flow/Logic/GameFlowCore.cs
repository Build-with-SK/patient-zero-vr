using System;
using PatientZero.Core;
namespace PatientZero.Flow.Logic
{
    public enum FlowCommandKind { TimeScale,AudioPause,Fade,UnloadWard,LoadWard,StartRun,StateChanged,Rejected }
    public readonly struct FlowCommand
    {
        public readonly FlowCommandKind Kind; public readonly float Value,Seconds; public readonly ActId Act; public readonly bool Restore;public readonly GameState From,To;public readonly FailReason Reason;
        public FlowCommand(FlowCommandKind kind,float value=0,float seconds=0,ActId act=ActId.None,bool restore=false,GameState from=GameState.Boot,GameState to=GameState.Boot,FailReason reason=FailReason.None){Kind=kind;Value=value;Seconds=seconds;Act=act;Restore=restore;From=from;To=to;Reason=reason;}
    }
    /// <summary>Deterministic flow commands; shell drains after each input, acknowledges async scene/fade completion.</summary>
    public sealed class GameFlowCore
    {
        private enum Stage { Idle,FadeOut,CaughtWait,Unload,Load,AwaitResume,FadeIn }
        private readonly FlowCommand[] commands=new FlowCommand[32]; private Stage stage;
        private GameState pausedFrom=GameState.Playing;private ActId reloadAct;private bool restore,loaded,completeToPlaying,focusLostDuringLoad;private float caughtRemaining;
        public GameState State {get;private set;}=GameState.Boot;
        public ActId CurrentAct {get;private set;}=ActId.Awakening;
        public bool IsTransitioning => stage!=Stage.Idle;
        public int CommandCount {get;private set;}
        public FlowCommand CommandAt(int index) => commands[index];
        public void ClearCommands(){CommandCount=0;}
        public void SetAct(ActId act){if(act>=ActId.Awakening && act<=ActId.Exit)CurrentAct=act;}
        public void Start(bool calibrated){Request(calibrated?GameState.Playing:GameState.Calibration);}
        public bool Request(GameState next,FailReason reason=FailReason.None)
        {
            if(!GameStateRules.CanTransition(State,next)){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}
            if(next==GameState.Caught)return Catch();
            if(next==GameState.Paused){Pause();return true;}
            if(State==GameState.Paused){if(next!=pausedFrom){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}Resume();return true;}
            if(State==GameState.Caught && next==GameState.Playing && stage!=Stage.Idle){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}
            if(State==GameState.Complete && next==GameState.Playing)return Restart(false);
            if(stage!=Stage.Idle){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}
            Transition(next,reason);
            if(next==GameState.Calibration)Emit(new FlowCommand(FlowCommandKind.Fade,0,.15f));
            if(next==GameState.Playing && !loaded)BeginReload(ActId.Awakening,false,false);
            return true;
        }
        public void Pause(){if(!GameStateRules.CanPauseFrom(State))return;pausedFrom=State;Transition(GameState.Paused);}
        public void Resume(){if(State!=GameState.Paused)return;Transition(pausedFrom);focusLostDuringLoad=false;if(stage==Stage.AwaitResume)FinishLoad();}
        public void Focus(bool focused){if(focused)return;focusLostDuringLoad=IsTransitioning;Pause();}
        public bool Catch()
        {
            if(State!=GameState.Playing || IsTransitioning)return false;
            Transition(GameState.Caught,FailReason.CaughtByZombie);caughtRemaining=2.5f;
            BeginReload(CurrentAct,true,false);return true;
        }
        public bool Restart(bool actOnly)
        {
            if((State!=GameState.Paused && State!=GameState.Complete) || IsTransitioning)return false;
            BeginReload(actOnly?CurrentAct:ActId.Awakening,actOnly,true);return true;
        }
        private void BeginReload(ActId act,bool isRestore,bool transitionOnLoad)
        {
            reloadAct=act;restore=isRestore;completeToPlaying=transitionOnLoad;focusLostDuringLoad=false;stage=Stage.FadeOut;
            Emit(new FlowCommand(FlowCommandKind.Fade,1,State==GameState.Caught?0.4f:0.15f));
        }
        public void FadeDone()
        {
            if(stage==Stage.FadeIn){stage=Stage.Idle;return;}
            if(stage!=Stage.FadeOut)return;
            if(State==GameState.Caught && caughtRemaining>0){stage=Stage.CaughtWait;return;}Unload();
        }
        public void Tick(float unscaledSeconds)
        {
            if(unscaledSeconds<0 || float.IsNaN(unscaledSeconds) || float.IsInfinity(unscaledSeconds))throw new ArgumentOutOfRangeException(nameof(unscaledSeconds));
            if(State==GameState.Caught)caughtRemaining=Math.Max(0,caughtRemaining-unscaledSeconds);
            if(stage==Stage.CaughtWait && caughtRemaining<=0)Unload();
        }
        private void Unload(){if(loaded){stage=Stage.Unload;Emit(new FlowCommand(FlowCommandKind.UnloadWard));}else Load();}
        public void WardUnloaded(){if(stage!=Stage.Unload)return;loaded=false;Load();}
        private void Load(){stage=Stage.Load;Emit(new FlowCommand(FlowCommandKind.LoadWard));}
        public void WardLoaded(){if(stage!=Stage.Load)return;loaded=true;if(focusLostDuringLoad && (State==GameState.Caught || State==GameState.Complete)){Transition(GameState.Playing);Pause();stage=Stage.AwaitResume;return;}if(State==GameState.Paused && (!completeToPlaying || focusLostDuringLoad)){stage=Stage.AwaitResume;return;}FinishLoad();}
        private void FinishLoad()
        {
            CurrentAct=reloadAct;stage=Stage.FadeIn;
            if(State==GameState.Caught || (State==GameState.Paused && completeToPlaying) || State==GameState.Complete)Transition(GameState.Playing);
            Emit(new FlowCommand(FlowCommandKind.StartRun,act:reloadAct,restore:restore));
            Emit(new FlowCommand(FlowCommandKind.Fade,0,restore?1f:4f));
        }
        private void Transition(GameState next,FailReason reason=FailReason.None)
        {
            var from=State;if(!GameStateRules.CanTransition(from,next))throw new InvalidOperationException("Illegal internal flow transition");State=next;
            bool freeze=!GameStateRules.IsSimulationLive(next);
            Emit(new FlowCommand(FlowCommandKind.TimeScale,freeze?0:1));Emit(new FlowCommand(FlowCommandKind.AudioPause,next==GameState.Paused?1:0));
            Emit(new FlowCommand(FlowCommandKind.StateChanged,from:from,to:next,reason:reason));
        }
        private void Emit(FlowCommand command){if(CommandCount==commands.Length)throw new InvalidOperationException("Shell did not drain flow commands");commands[CommandCount++]=command;}
    }
}
