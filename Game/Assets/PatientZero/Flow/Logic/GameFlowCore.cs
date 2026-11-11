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
    /// <summary>
    /// Deterministic flow commands; shell drains after each input, acknowledges async scene/fade completion.
    /// Two-minute version (owner-approved plan 2026-11-10; Claude integration E-22): the first grab of each life is a struggle the
    /// player can win (Caught, the world held, then back to Playing); a lost struggle or a second grab kills, and the player
    /// starts again from the bed (the ward reloads into the Awakening act as a restore, so the news does not replay).
    /// </summary>
    public sealed class GameFlowCore
    {
        /// <summary>E-22: the struggle's window, and how many presses (or shoves) win it.</summary>
        public const float StruggleSeconds=3f;public const int StrugglePresses=5;
        private enum Stage { Idle,FadeOut,CaughtWait,Unload,Load,AwaitResume,FadeIn }
        private readonly FlowCommand[] commands=new FlowCommand[32]; private Stage stage;
        private GameState pausedFrom=GameState.Playing;private ActId reloadAct;private bool restore,loaded,completeToPlaying,focusLostDuringLoad;private float caughtRemaining;
        private int presses;private float struggleRemaining;private bool struggleFocused=true;
        /// <summary>E-22: a zombie has the player and the struggle is on (state Caught, world held).</summary>
        public bool Struggling {get;private set;}
        /// <summary>E-22: this life's one struggle is spent. A restart from the bed gives it back.</summary>
        public bool StruggleUsed {get;private set;}
        public float StruggleProgress => Struggling?Math.Min(1f,presses/(float)StrugglePresses):0f;
        public float StruggleRemaining => Struggling?struggleRemaining:0f;
        public GameState State {get;private set;}=GameState.Boot;
        public ActId CurrentAct {get;private set;}=ActId.Awakening;
        public bool IsTransitioning => stage!=Stage.Idle;
        public int CommandCount {get;private set;}
        public FlowCommand CommandAt(int index) => commands[index];
        public void ClearCommands(){CommandCount=0;}
        public void SetAct(ActId act){if(act>=ActId.Awakening && act<=ActId.Lobby)CurrentAct=act;} // v1.3: through the lobby (E-19)
        public void Start(bool calibrated){Request(calibrated?GameState.Playing:GameState.Calibration);}
        public bool Request(GameState next,FailReason reason=FailReason.None)
        {
            if(!GameStateRules.CanTransition(State,next)){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}
            if(next==GameState.Caught)return Catch();
            if(next==GameState.Paused){Pause();return true;}
            if(State==GameState.Paused){if(next!=pausedFrom){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}Resume();return true;}
            // Only a won struggle (Struggle) or the reload leaves Caught: no request can skip either (E-22).
            if(State==GameState.Caught && next==GameState.Playing){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}
            if(State==GameState.Complete && next==GameState.Playing)return Restart(false);
            if(stage!=Stage.Idle){Emit(new FlowCommand(FlowCommandKind.Rejected,from:State,to:next));return false;}
            Transition(next,reason);
            if(next==GameState.Calibration)Emit(new FlowCommand(FlowCommandKind.Fade,0,.15f));
            if(next==GameState.Playing && !loaded)BeginReload(ActId.Awakening,false,false);
            return true;
        }
        public void Pause(){if(!GameStateRules.CanPauseFrom(State))return;pausedFrom=State;Transition(GameState.Paused);}
        public void Resume(){if(State!=GameState.Paused)return;Transition(pausedFrom);focusLostDuringLoad=false;if(stage==Stage.AwaitResume)FinishLoad();}
        /// <remarks>E-22: a struggle cannot be paused (Caught never pauses), so losing focus holds its clock instead.</remarks>
        public void Focus(bool focused){struggleFocused=focused;if(focused)return;focusLostDuringLoad=IsTransitioning;Pause();}
        /// <summary>
        /// A zombie has grabbed the player. E-22: the first grab of a life starts the struggle; a later one kills, and the run
        /// starts again from the bed. Returns false when no grab can happen now (already caught, paused, loading).
        /// </summary>
        public bool Catch()
        {
            if(State!=GameState.Playing || IsTransitioning)return false;
            Transition(GameState.Caught,FailReason.CaughtByZombie);
            if(!StruggleUsed){StruggleUsed=Struggling=true;presses=0;struggleRemaining=StruggleSeconds;return true;}
            Die();return true;
        }
        /// <summary>E-22: one press of the struggle input (or one shove in a headset). True when it broke the player free.</summary>
        public bool Struggle()
        {
            if(!Struggling || State!=GameState.Caught)return false;
            if(++presses<StrugglePresses)return false;
            Struggling=false;Transition(GameState.Playing);return true;
        }
        private void Die(){Struggling=false;caughtRemaining=2.5f;BeginReload(ActId.Awakening,true,false);}
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
            if(Struggling && struggleFocused){struggleRemaining=Math.Max(0,struggleRemaining-unscaledSeconds);if(struggleRemaining<=0)Die();}
            else if(State==GameState.Caught)caughtRemaining=Math.Max(0,caughtRemaining-unscaledSeconds);
            if(stage==Stage.CaughtWait && caughtRemaining<=0)Unload();
        }
        private void Unload(){if(loaded){stage=Stage.Unload;Emit(new FlowCommand(FlowCommandKind.UnloadWard));}else Load();}
        public void WardUnloaded(){if(stage!=Stage.Unload)return;loaded=false;Load();}
        private void Load(){stage=Stage.Load;Emit(new FlowCommand(FlowCommandKind.LoadWard));}
        public void WardLoaded(){if(stage!=Stage.Load)return;loaded=true;if(focusLostDuringLoad && (State==GameState.Caught || State==GameState.Complete)){Transition(GameState.Playing);Pause();stage=Stage.AwaitResume;return;}if(State==GameState.Paused && (!completeToPlaying || focusLostDuringLoad)){stage=Stage.AwaitResume;return;}FinishLoad();}
        private void FinishLoad()
        {
            CurrentAct=reloadAct;stage=Stage.FadeIn;
            if(reloadAct==ActId.Awakening)StruggleUsed=false; // a new life from the bed (E-22)
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
