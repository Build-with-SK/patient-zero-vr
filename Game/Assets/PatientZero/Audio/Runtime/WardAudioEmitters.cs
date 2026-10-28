// UNVERIFIED IN EDITOR
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Audio
{
    /// <summary>Ward-owned loops placed by approved layout ids; no independent noise emission.</summary>
    public sealed class WardAudioEmitters : MonoBehaviour
    {
        private readonly CueHandle[] loops = new CueHandle[8];
        private int count;
        private CueHandle tension;
        private void OnEnable() { GameEvents.ActStarted.Subscribe(ActStarted); GameEvents.StateChanged.Subscribe(StateChanged); }
        private void OnDisable() { GameEvents.ActStarted.Unsubscribe(ActStarted); GameEvents.StateChanged.Unsubscribe(StateChanged); StopLoops(0); }
        private void ActStarted(ActStarted e)
        {
            StopLoops(0);
            Loop(CueId.RoomTone, WardLayout.ActStart(e.Act).Position);
            tension = Cues.Play(CueId.TensionDrone, WardLayout.ActStart(e.Act).Position, NoiseOrigin.Ambient, .0001f);
            Cues.SetIntensity(tension, 0);
            switch (e.Act)
            {
                case ActId.Awakening:
                    Loop(CueId.FluorescentBuzz, WardLayout.A1_DoorWindow.Position); Loop(CueId.Ventilation, WardLayout.A1_Tray.Position); break;
                case ActId.Machinery:
                    Loop(CueId.MonitorBeep, WardLayout.A2_Monitor.Position); Loop(CueId.Ventilation, WardLayout.A2_Socket.Position); break;
                case ActId.Darkness:
                    Loop(CueId.BlackoutTone, WardLayout.A3_Start.Position); Loop(CueId.WaterDrip, WardLayout.A3_Drip.Position); break;
                case ActId.Exit:
                    Loop(CueId.AlarmKlaxon, WardLayout.A4_ExitDoor.Position); Loop(CueId.RestraintStrain, WardLayout.Z4_Gurney.Position); break;
            }
        }
        private void StateChanged(GameStateChanged e) { if (e.To == GameState.Ending) StopLoops(2); }
        private void Loop(CueId cue, Vector3 position) { if (count < loops.Length) loops[count++] = Cues.Play(cue, position, NoiseOrigin.Ambient); }
        /// <summary>Director sets the music bed from its tested tension value.</summary>
        public void SetTension(float amount) => Cues.SetIntensity(tension, amount);
        private void StopLoops(float fade)
        { for (int i = 0; i < count; i++) Cues.Stop(loops[i], fade); count = 0; Cues.Stop(tension, fade); tension = default; }
    }
}
