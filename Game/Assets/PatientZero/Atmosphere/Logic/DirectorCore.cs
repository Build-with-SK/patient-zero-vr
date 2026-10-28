using System;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Atmosphere.Logic
{
    // Commands are local to B's director/shell, not a second cross-seat contract or event bus.
    public enum DirectorCommandKind { Cue, Lighting, Act, Assist, Zombie, Reveal, State, Silence, Tension, StopZombieLoops, LocalLightPulse, Gate }
    public enum ZombieDirection { Hide, WindowApproach, LeaveWindow, Dormant, Investigate, Patrol, Hold, Stun, Restraint, Snap, Passive, Kneel }
    public readonly struct DirectorCommand
    {
        public readonly DirectorCommandKind Kind;
        public readonly CueId Cue;
        public readonly Vector3 Position;
        public readonly float Value;
        public readonly int Id;
        public readonly int Detail;
        public DirectorCommand(DirectorCommandKind kind, int id = 0, int detail = 0, float value = 0, Vector3 position = default, CueId cue = CueId.None)
        { Kind = kind; Id = id; Detail = detail; Value = value; Position = position; Cue = cue; }
    }
    /// <summary>Retained by B's shell across Ward reloads; reset on full restart/subsystem registration.</summary>
    public sealed class DirectorMemory
    {
        public readonly int[] Catches = new int[5];
        public bool WindowScareUsed, StrapScareUsed;
        public int Attempt;
        public float ExperienceTime;
        public void Reset() { Array.Clear(Catches, 0, Catches.Length); WindowScareUsed = StrapScareUsed = false; Attempt = 0; ExperienceTime = 0; }
    }
    /// <summary>Objective-driven approved four-act timeline. No clock may fail the player.</summary>
    public sealed class DirectorCore
    {
        private readonly DirectorMemory memory;
        private readonly DirectorCommand[] commands = new DirectorCommand[128];
        private readonly float[] progress = new float[8], lastProgress = new float[8];
        private readonly int[] hint = new int[8];
        private readonly bool[] complete = new bool[8];
        private readonly ObjectiveId[] objectives = { ObjectiveId.Act1_DoorUnbarred, ObjectiveId.Act1_DoorOpened,
            ObjectiveId.Act2_ValveVented, ObjectiveId.Act2_CablePlugged, ObjectiveId.Act2_PowerRestored,
            ObjectiveId.Act3_BeaconReached, ObjectiveId.Act4_ReleasePulled, ObjectiveId.Act4_ExitOpened };
        private int read, write;
        private float actTime, endingTime, scareAt, surgeAt = -1, lastHard = -100, lastAlert = -100, lastAmbient = -100, nextBeacon;
        private bool pass, windowArrived, windowScare, lure, z1Clear, investigate, crossing, snapped, ending, pa, drone, kneel, done;
        private Vector3 z1Position;
        private bool walkerRemoved, actOneGateReleased;
        public ActId Act { get; private set; }
        public float ExperienceTime => memory.ExperienceTime;
        public float ActTime => actTime;
        public float Tension { get; private set; }
        public bool CanOpenAct1Door => windowScare && lure && z1Clear && complete[0];
        public bool InvestigationStarted => investigate;
        public bool AdaptiveAssist => memory.Catches[(int)Act] >= 2;
        public int HardScares => (memory.WindowScareUsed ? 1 : 0) + (memory.StrapScareUsed ? 1 : 0);
        private ExperienceSettings settings = ExperienceSettings.Defaults;
        public ExperienceSettings Settings
        {
            get => settings;
            set
            {
                bool wasForced = settings.PuzzleAssistAlwaysOn;
                settings = value.Sanitized();
                if (Act == ActId.None || wasForced == settings.PuzzleAssistAlwaysOn) return;
                for (int i = 0; i < objectives.Length; i++)
                    if (InAct(objectives[i]) && !complete[i])
                    { lastProgress[i] = actTime; Assist(i, AssistFloor); }
            }
        }
        private AssistLevel AssistFloor => AdaptiveAssist || Settings.PuzzleAssistAlwaysOn ? AssistLevel.Assist : AssistLevel.None;
        public bool IsGateReleased(GateId gate) => gate == GateId.Act1Door && Act == ActId.Awakening && actOneGateReleased;
        public bool TrackingAvailable { get; private set; } = true;
        public DirectorCore(DirectorMemory memory) { this.memory = memory ?? throw new ArgumentNullException(nameof(memory)); }
        public void StartRun(ActId act, bool restore)
        {
            if (act < ActId.Awakening || act > ActId.Exit) throw new ArgumentOutOfRangeException(nameof(act));
            if (!restore && act == ActId.Awakening) memory.Reset();
            memory.Attempt++; read = write = 0; Tension = 0; lastHard = lastAlert = lastAmbient = -100;
            EnterAct(act, restore);
        }
        private void EnterAct(ActId act, bool restore)
        {
            Act = act; actTime = 0; surgeAt = -1; nextBeacon = 3;
            pass = windowArrived = windowScare = lure = z1Clear = investigate = crossing = snapped = ending = pa = drone = kneel = done = false;
            walkerRemoved = actOneGateReleased = false;
            Array.Clear(progress, 0, progress.Length); Array.Clear(lastProgress, 0, lastProgress.Length);
            Array.Clear(hint, 0, hint.Length); Array.Clear(complete, 0, complete.Length);
            Emit(new DirectorCommand(DirectorCommandKind.Act, (int)act, restore ? 1 : 0, memory.Attempt));
            for (int id = 1; id <= 4; id++) Zombie(id, ZombieDirection.Hide);
            switch (act)
            {
                case ActId.Awakening:
                    Light(LightingState.Awakening); Cue(CueId.HeartbeatWake, WardLayout.A1_Start.Position);
                    z1Position = WardLayout.Z1_WalkFrom.Position; break;
                case ActId.Machinery: Light(LightingState.WardDim); Zombie(2, ZombieDirection.Dormant); break;
                case ActId.Darkness: Light(LightingState.Blackout); Zombie(3, ZombieDirection.Hold); break;
                case ActId.Exit: Light(LightingState.EmergencyRed); Zombie(4, ZombieDirection.Restraint); break;
            }
            if (AdaptiveAssist || Settings.PuzzleAssistAlwaysOn)
                for (int i = 0; i < objectives.Length; i++) if (InAct(objectives[i])) Assist(i, AssistLevel.Assist);
        }
        public void Progress(ObjectiveId objective, float amount)
        {
            int i = Index(objective);
            if (i < 0 || !InAct(objective) || complete[i] || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= progress[i]) return;
            progress[i] = Math.Min(1, amount); lastProgress[i] = actTime;
            Assist(i, AssistFloor);
            if (objective == ObjectiveId.Act1_DoorUnbarred && amount > 0) StartPass();
            if (objective == ObjectiveId.Act4_ReleasePulled && amount >= .6f) Snap();
        }
        public void Complete(ObjectiveId objective)
        {
            int i = Index(objective); if (i < 0 || !InAct(objective) || complete[i]) return;
            Progress(objective, 1); complete[i] = true;
            if (objective == ObjectiveId.Act4_ReleasePulled) Light(LightingState.ExitDaylight);
            EvaluateGates();
        }
        public void ReportZombiePosition(int id, Vector3 position)
        {
            if (id != ZombieIds.Walker || Act != ActId.Awakening) return;
            z1Position = position;
            if (pass && !windowArrived && !lure && WardLayout.HorizontalDistance(position, WardLayout.A1_DoorWindow.Position) <= .5f)
            { windowArrived = true; Cue(CueId.ZombieGlassTap, WardLayout.A1_DoorWindow.Position); }
            if (lure && WardLayout.HorizontalDistance(position, WardLayout.A1_DoorWindow.Position) >= 4) z1Clear = true;
        }
        public void Alert(int id, AlertLevel level, Vector3 position, Vector3 player)
        {
            lastAlert = ExperienceTime;
            if (level >= AlertLevel.Investigating && WardLayout.HorizontalDistance(position, player) <= 6) Tension = Math.Min(1, Tension + .3f);
            if (Act == ActId.Machinery && id == ZombieIds.Orderly && level >= AlertLevel.Suspicious) investigate = true;
        }
        public void Noise(in NoiseEvent noise)
        { if (noise.Origin == NoiseOrigin.Player && noise.RadiusMeters >= 4) Tension = Math.Min(1, Tension + .1f); }
        public void Caught() { if (!ending && Act != ActId.None) memory.Catches[(int)Act]++; }
        public void Tracking(InputModality modality, bool left, bool right) => TrackingAvailable = modality != InputModality.Hands || left || right;
        public void Tick(float delta, bool live, Vector3 player)
        {
            if (!live || delta < 0 || float.IsNaN(delta) || float.IsInfinity(delta) || Act == ActId.None) return;
            actTime += delta; memory.ExperienceTime += delta; Tension = Math.Max(0, Tension - delta * .05f);
            if (!TrackingAvailable)
                for (int i = 0; i < lastProgress.Length; i++) lastProgress[i] += delta;
            if (ending) { TickEnding(delta); return; }
            switch (Act)
            {
                case ActId.Awakening:
                    if (!pa && actTime >= 3) { pa = true; Cue(CueId.PaGarbledBreach, WardLayout.A1_DoorWindow.Position); }
                    if (!pass && actTime >= 16) StartPass();
                    if (complete[0] && windowArrived && !windowScare) WindowScare();
                    if (windowScare && !lure && actTime >= scareAt + 2)
                    { lure = true; Cue(CueId.DistantCrashLure, WardLayout.Z1_WalkTo.Position); Zombie(1, ZombieDirection.LeaveWindow, WardLayout.Z1_WalkTo.Position); }
                    if (lure && !walkerRemoved && WardLayout.HorizontalDistance(z1Position, WardLayout.Z1_WalkTo.Position) <= .35f)
                    { walkerRemoved = true; Zombie(1, ZombieDirection.Hide); }
                    break;
                case ActId.Machinery:
                    if (!investigate && actTime >= 12)
                    { investigate = true; Cue(CueId.DistantCrashLure, WardLayout.Z1_WalkFrom.Position + Vector3.up); Zombie(2, ZombieDirection.Investigate); }
                    if (surgeAt >= 0 && actTime >= surgeAt + .6f && !pa)
                    { pa = true; Cue(CueId.BreakerOverload, WardLayout.A2_Socket.Position); Light(LightingState.Blackout); }
                    if (surgeAt >= 0 && actTime >= surgeAt + 2.5f) { EnterAct(ActId.Darkness, false); return; }
                    break;
                case ActId.Darkness:
                    if (!pa && actTime >= 1) { pa = true; Cue(CueId.PaCarrierWarning, WardLayout.A3_Beacon.Position); }
                    if (actTime >= nextBeacon) { nextBeacon = actTime + 3; Cue(CueId.BeaconChirp, WardLayout.A3_Beacon.Position); }
                    if (!crossing && (actTime >= 10 || WardLayout.HorizontalDistance(player, WardLayout.A3_Mid1.Position) <= .75f))
                    { crossing = true; Zombie(3, ZombieDirection.Patrol); }
                    break;
                case ActId.Exit: if (!snapped && actTime >= 15) Snap(); break;
            }
            Hints(); EvaluateGates();
            Emit(new DirectorCommand(DirectorCommandKind.Tension, value: Tension));
        }
        public bool TryAmbient(Vector3 position, Vector3 player, bool behindWall)
        {
            float distance = Vector3.Distance(position, player);
            if (ending || !behindWall || distance < 8 || distance > 15 || ExperienceTime - lastAmbient < 12 ||
                ExperienceTime - lastHard < 6 || ExperienceTime - lastAlert < 6 || Tension >= .4f) return false;
            lastAmbient = ExperienceTime; Cue(CueId.DistantInfected, position); return true;
        }
        private void StartPass()
        { if (pass || Act != ActId.Awakening) return; pass = true; Zombie(1, ZombieDirection.WindowApproach, WardLayout.A1_DoorWindow.Position); }
        private void WindowScare()
        {
            windowScare = true; scareAt = actTime;
            bool hard = Settings.Scares == ScareIntensity.Standard && !memory.WindowScareUsed;
            if (hard)
            {
                memory.WindowScareUsed = true; Peak();
                if (!Settings.ReduceFlicker) Emit(new DirectorCommand(DirectorCommandKind.LocalLightPulse));
            }
            Cue(hard ? CueId.WindowSlam : CueId.WindowBreath, WardLayout.A1_DoorWindow.Position);
        }
        private void Snap()
        {
            if (snapped || Act != ActId.Exit) return; snapped = true;
            bool hard = Settings.Scares == ScareIntensity.Standard && !memory.StrapScareUsed;
            if (hard) { memory.StrapScareUsed = true; Peak(); }
            Cue(CueId.RestraintSnap, WardLayout.Z4_Gurney.Position, hard ? 1 : .355f);
            if (hard) Cue(CueId.ZombieLunge, WardLayout.Z4_Gurney.Position);
            Zombie(4, ZombieDirection.Snap, value: hard ? 1 : 0);
        }
        private void Peak()
        { lastHard = ExperienceTime; Emit(new DirectorCommand(DirectorCommandKind.Silence, value: 3)); }
        private void EvaluateGates()
        {
            if (Act == ActId.Awakening && CanOpenAct1Door && !actOneGateReleased)
            {
                actOneGateReleased = true;
                Emit(new DirectorCommand(DirectorCommandKind.Gate, (int)GateId.Act1Door));
            }
            if (Act == ActId.Awakening && complete[1] && actOneGateReleased) EnterAct(ActId.Machinery, false);
            else if (Act == ActId.Machinery && complete[4] && complete[2] && complete[3] && investigate && surgeAt < 0)
            {
                surgeAt = actTime; Cue(CueId.PowerThrum, WardLayout.A2_Socket.Position); Light(LightingState.Surge);
                for (int id = 1; id <= 4; id++) Zombie(id, ZombieDirection.Stun, value: 2.5f);
            }
            else if (Act == ActId.Darkness && complete[5]) EnterAct(ActId.Exit, false);
            else if (Act == ActId.Exit && complete[6] && complete[7] && !ending)
            {
                ending = true; endingTime = 0; pa = drone = false;
                for (int id = 1; id <= 4; id++) Zombie(id, ZombieDirection.Passive);
                Emit(new DirectorCommand(DirectorCommandKind.StopZombieLoops, value: 2));
                Emit(new DirectorCommand(DirectorCommandKind.State, (int)GameState.Ending));
            }
        }
        private void TickEnding(float delta)
        {
            endingTime += delta;
            if (!kneel && endingTime >= 1) { kneel = true; Zombie(4, ZombieDirection.Kneel); }
            if (!pa && endingTime >= 2) { pa = true; Cue(CueId.PaPatientZero, WardLayout.A4_ExitDoor.Position); }
            if (endingTime >= 3 && endingTime <= 9 + delta)
                Emit(new DirectorCommand(DirectorCommandKind.Reveal, value: Math.Max(0, Math.Min(1, (endingTime - 3) / 6))));
            if (!drone && endingTime >= 6) { drone = true; Cue(CueId.RevealDrone, WardLayout.A4_Start.Position); }
            if (!done && endingTime >= 12) { done = true; Emit(new DirectorCommand(DirectorCommandKind.State, (int)GameState.Complete)); }
        }
        private void Hints()
        {
            for (int i = 0; i < objectives.Length; i++)
            {
                if (!InAct(objectives[i]) || complete[i] || objectives[i] == ObjectiveId.Act1_DoorOpened ||
                    objectives[i] == ObjectiveId.Act2_PowerRestored || objectives[i] == ObjectiveId.Act4_ExitOpened) continue;
                float elapsed = actTime - lastProgress[i] - (Act == ActId.Darkness ? 2 : 0);
                int wanted = elapsed >= 30 ? 3 : elapsed >= 20 ? 2 : elapsed >= 10 ? 1 : 0;
                if (wanted <= hint[i]) continue;
                for (int level = hint[i] + 1; level <= wanted; level++) Assist(i, (AssistLevel)level);
                if (Act == ActId.Darkness && wanted == 3) Zombie(3, ZombieDirection.Hold, WardLayout.Z3_PatrolNorth.Position);
            }
        }
        private void Assist(int i, AssistLevel level)
        { hint[i] = (int)level; Emit(new DirectorCommand(DirectorCommandKind.Assist, (int)objectives[i], (int)level)); }
        private bool InAct(ObjectiveId objective) => (int)objective / 100 == (int)Act;
        private int Index(ObjectiveId objective) { for (int i = 0; i < objectives.Length; i++) if (objectives[i] == objective) return i; return -1; }
        private void Cue(CueId cue, Vector3 position, float gain = 1) => Emit(new DirectorCommand(DirectorCommandKind.Cue, value: gain, position: position, cue: cue));
        private void Light(LightingState state) => Emit(new DirectorCommand(DirectorCommandKind.Lighting, (int)state));
        private void Zombie(int id, ZombieDirection direction, Vector3 position = default, float value = 0) =>
            Emit(new DirectorCommand(DirectorCommandKind.Zombie, id, (int)direction, value, position));
        private void Emit(DirectorCommand command)
        {
            if (write - read == commands.Length) throw new InvalidOperationException("Director commands must be drained each tick/event");
            commands[write++ % commands.Length] = command;
        }
        public bool TryRead(out DirectorCommand command)
        {
            if (read == write) { read = write = 0; command = default; return false; }
            command = commands[read++ % commands.Length]; return true;
        }
    }
}
