// Claude-owned integration (owner's step 1 feedback, 2026-11-11; E-23).
using PatientZero.Core;
using PatientZero.Integration.Logic;
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// A dead body that hangs where there was nothing a moment ago (<see cref="LookAwayRule"/>). Once the player has looked away
    /// it is there, the rope creaks to turn them round, and when they see it a stinger plays (not with reduced scares). It sways
    /// a little. Every new life (the bed) makes the spot empty again. No collider, no AI, no noise a zombie can hear.
    /// </summary>
    public sealed class LookAwayScare : MonoBehaviour
    {
        [Tooltip("The hanged body; its transform is the top of the rope, where it swings from. Hidden until the reveal.")]
        public Transform body;
        [Tooltip("Where the player's eyes must fall: the body's chest.")]
        public Vector3 lookAt;
        [Tooltip("When true the player must first see the spot empty.")]
        public bool seenFirst = true;
        [Tooltip("For the log.")]
        public string label = "";
        public float creakAfter = .7f, swingDegrees = 2.5f, swingSeconds = 3.4f;

        private LookAwayRule rule;
        // Armed by the run's first act (the Ward loads after the game is already Playing, so the state event alone is not enough),
        // disarmed when the run ends; held while paused.
        private bool armed, playing, reduced;
        private string revealedNote, spottedNote;
        private float creakAt = -1, swing;
        private Quaternion rest;

        public LookAwayRule.Phase Phase => rule == null ? LookAwayRule.Phase.Empty : rule.State;

        private void Awake()
        {
            rule = new LookAwayRule(seenFirst);
            revealedNote = "[PZ] look-away scare '" + label + "': the body is there now";
            spottedNote = "[PZ] look-away scare '" + label + "': spotted";
            if (body) rest = body.localRotation;
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.ActStarted.Subscribe(OnAct);
            GameEvents.StateChanged.Subscribe(OnState);
            GameEvents.SettingsChanged.Subscribe(OnSettings);
            if (Services<ISettingsProvider>.TryGet(out ISettingsProvider settings)) reduced = settings.Current.Scares == ScareIntensity.Reduced;
        }

        private void OnDisable()
        {
            GameEvents.ActStarted.Unsubscribe(OnAct);
            GameEvents.StateChanged.Unsubscribe(OnState);
            GameEvents.SettingsChanged.Unsubscribe(OnSettings);
        }

        private void OnSettings(SettingsChanged e) => reduced = e.Settings.Scares == ScareIntensity.Reduced;

        private void OnState(GameStateChanged e)
        {
            playing = e.To == GameState.Playing;
            if (!playing && e.To != GameState.Paused) armed = false;
        }

        private void OnAct(ActStarted e)
        {
            // An act starts only while the game is Playing. Every death sends the player back to bed, so a new Act 1 is a new
            // life: the spot is empty again.
            armed = playing = true;
            if (e.Act != ActId.Awakening || rule == null) return;
            rule.Reset();
            Hide();
        }

        private void Update()
        {
            if (!body || rule == null) return;
            if (creakAt >= 0 && Time.time >= creakAt)
            {
                creakAt = -1;
                Cues.Play(CueId.PlankCreak, lookAt + Vector3.up * .6f, NoiseOrigin.Ambient);
            }

            if (!armed || !playing || !Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) return;
            Vector3 eye = player.HeadPosition, to = lookAt - eye;
            float meters = to.magnitude;
            float degrees = meters > 1e-3f ? Vector3.Angle(player.HeadForward, to) : 0;
            bool clear = !Physics.Linecast(eye, lookAt, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            switch (rule.Tick(Time.deltaTime, degrees, meters, clear))
            {
                case LookAwayEvent.Revealed:
                    body.gameObject.SetActive(true);
                    swing = 0;
                    creakAt = Time.time + creakAfter;
                    Debug.Log(revealedNote);
                    break;
                case LookAwayEvent.Spotted:
                    if (!reduced) Cues.Play(CueId.JumpScareStinger, eye, NoiseOrigin.Ambient, .6f);
                    Debug.Log(spottedNote);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!body || !body.gameObject.activeSelf) return;
            swing += Time.deltaTime;
            float a = Mathf.Sin(swing * 2 * Mathf.PI / swingSeconds), b = Mathf.Sin(swing * 2 * Mathf.PI / (swingSeconds * 1.37f));
            body.localRotation = rest * Quaternion.Euler(a * swingDegrees, b * swingDegrees * 4, b * swingDegrees * .6f);
        }

        private void Hide()
        {
            creakAt = -1;
            if (!body) return;
            body.localRotation = rest;
            body.gameObject.SetActive(false);
        }
    }
}
