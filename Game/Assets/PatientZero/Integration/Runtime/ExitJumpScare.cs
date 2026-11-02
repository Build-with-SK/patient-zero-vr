// Claude-owned integration (horror pass, 2026-11-02; E-21).
using PatientZero.Core;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.XR;

namespace PatientZero.Integration
{
    /// <summary>
    /// The last scare. As the main doors open and freedom is one step away, a zombie that was waiting outside throws itself
    /// at the player's face: a stinger, a scream at arm's length, the flat-screen view shakes, and the picture cuts to black.
    /// (No controller rumble: only AI and Atmosphere may raise haptics.) When it comes back the zombie is gone and the director's ending (the PA at 2 s, the reveal at
    /// 3 s) plays as before. Skipped with reduced scares.
    /// </summary>
    public sealed class ExitJumpScare : MonoBehaviour
    {
        [Tooltip("Pivot on the floor between the zombie's feet; its forward is the way the model faces. Hidden until the scare.")]
        public Transform zombie;
        [Tooltip("Where it comes from when the player is facing the open doors.")]
        public Vector3 doorway;
        public float delay = .25f, lunge = .45f, hop = .35f, black = .75f, fadeBack = .8f;
        [Tooltip("How far in front of the player's feet the leap ends.")]
        public float stopAt = 1.05f;

        private enum Phase { Idle, Waiting, Lunging, Black, Done }
        private Phase phase;
        private float t;
        private bool reduced;
        private Vector3 from, to;
        private Transform view;
        private Vector3 viewBase;

        private void Awake() => Hide();

        private void OnEnable()
        {
            GameEvents.StateChanged.Subscribe(OnState);
            GameEvents.ActStarted.Subscribe(OnAct);
            GameEvents.SettingsChanged.Subscribe(OnSettings);
            if (Services<ISettingsProvider>.TryGet(out ISettingsProvider settings)) reduced = settings.Current.Scares == ScareIntensity.Reduced;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged.Unsubscribe(OnState);
            GameEvents.ActStarted.Unsubscribe(OnAct);
            GameEvents.SettingsChanged.Unsubscribe(OnSettings);
            Finish(false);
        }

        private void OnSettings(SettingsChanged e) => reduced = e.Settings.Scares == ScareIntensity.Reduced;

        private void OnAct(ActStarted e)
        {
            Finish(false);
            phase = Phase.Idle;
        }

        private void OnState(GameStateChanged e)
        {
            if (e.To == GameState.Ending && phase == Phase.Idle && !reduced)
            {
                phase = Phase.Waiting;
                t = 0;
            }
            else if (e.To == GameState.Complete || e.To == GameState.Calibration) Finish(true);
        }

        private void Update()
        {
            if (phase == Phase.Idle || phase == Phase.Done) return;
            t += Time.deltaTime;
            switch (phase)
            {
                case Phase.Waiting:
                    if (t >= delay) Leap();
                    break;
                case Phase.Lunging:
                    float k = Mathf.Clamp01(t / lunge);
                    float e = 1 - (1 - k) * (1 - k) * (1 - k);
                    zombie.position = Vector3.Lerp(from, to, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * hop);
                    if (k >= 1) CutToBlack();
                    break;
                case Phase.Black:
                    if (t >= black) Finish(true);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (phase != Phase.Lunging || !view) return;
            // Flat screens only: the head is the player's in a headset.
            float k = Mathf.Clamp01(t / lunge);
            float amount = .012f + .03f * k * k;
            view.localPosition = viewBase + new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount), 0);
        }

        private void Leap()
        {
            if (!zombie || !Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) { phase = Phase.Done; return; }
            Vector3 head = player.HeadPosition, feet = player.FloorPosition;
            Vector3 forward = player.HeadForward;
            forward.y = 0;
            forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;

            // From the open doorway when the player is looking out through it; otherwise from just ahead, out of the dark.
            Vector3 toDoor = doorway - feet;
            toDoor.y = 0;
            float distance = toDoor.magnitude;
            bool fromDoor = distance >= 1.5f && distance <= 6f && Vector3.Dot(toDoor / distance, forward) > .5f;
            from = fromDoor ? new Vector3(doorway.x, feet.y, doorway.z) : feet + forward * 2.4f;
            // It stops with its face about 0.7 m from the eyes: the attack pose leans its head ahead of its feet, and any
            // closer puts the camera inside the model, which shows nothing at all.
            to = feet + forward * stopAt;
            zombie.position = from;
            zombie.rotation = Quaternion.LookRotation(-forward, Vector3.up);
            zombie.gameObject.SetActive(true);

            Cues.Play(CueId.JumpScareStinger, head, NoiseOrigin.Scripted);
            Cues.PlayAttached(CueId.JumpScareScream, zombie, NoiseOrigin.Scripted);

            view = null;
            if (!XRSettings.isDeviceActive && player is PlayerRig rig && rig.origin && rig.origin.Camera)
            {
                view = rig.origin.Camera.transform;
                viewBase = view.localPosition;
            }

            phase = Phase.Lunging;
            t = 0;
        }

        private void CutToBlack()
        {
            if (Services<IScreenFader>.TryGet(out IScreenFader fader)) fader.FadeTo(1, .06f);
            RestoreView();
            phase = Phase.Black;
            t = 0;
        }

        /// <summary>Puts everything back. With <paramref name="fadeIn"/>, lifts the black it put up.</summary>
        private void Finish(bool fadeIn)
        {
            bool wasBlack = phase == Phase.Black;
            RestoreView();
            Hide();
            if (phase != Phase.Idle) phase = Phase.Done;
            if (fadeIn && wasBlack && Services<IScreenFader>.TryGet(out IScreenFader fader)) fader.FadeTo(0, fadeBack);
        }

        private void RestoreView()
        {
            if (view) view.localPosition = viewBase;
            view = null;
        }

        private void Hide()
        {
            if (zombie) zombie.gameObject.SetActive(false);
        }
    }
}
