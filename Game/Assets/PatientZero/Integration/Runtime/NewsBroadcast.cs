// Claude-owned integration (horror pass, 2026-11-02; E-21).
using PatientZero.Core;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.XR;

namespace PatientZero.Integration
{
    /// <summary>
    /// The television bolted to the ceiling of Isolation Room B-7. When a new run starts it carries an emergency news
    /// broadcast about the outbreak, and the player cannot walk away until the bulletin is over: on a flat screen the view
    /// turns to the set and stays there; in a headset the body is turned to face it once and movement and turning are off,
    /// while the head stays free (never move a headset view for the player). A restored act does not replay it.
    /// The anchor's words are captioned by the cue player from the recording's transcript (shown when subtitles are on); the
    /// set shows its own headlines and ticker.
    /// </summary>
    public sealed class NewsBroadcast : MonoBehaviour
    {
        [System.Serializable]
        public struct Card
        {
            public float At;
            public string Headline;
            public string Detail;
        }

        [Tooltip("Centre of the picture. Its forward axis points from the viewer into the screen.")]
        public Transform screen;
        public Renderer picture;
        public TextMesh headline, detail, ticker;
        public Renderer liveDot;
        [Tooltip("The set's fixed captions (BREAKING NEWS, ALERT): shown only while the bulletin plays.")]
        public GameObject[] labels = new GameObject[0];
        [Tooltip("The bulletin's length in seconds (the recording's length, set at compose time).")]
        public float duration = 64f;
        public Card[] cards = new Card[0];
        public string[] tickerLines = new string[0];
        public float tickerSeconds = 4.5f;

        /// <summary>True while the bulletin holds the player in place.</summary>
        public bool Holding => holding;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private const float TurnRate = 3.5f, ReleaseSeconds = .6f, DuckedLevel = .3f;
        private static readonly string[] HeldProviders =
            { "TeleportationProvider", "ContinuousMoveProvider", "DynamicMoveProvider", "SnapTurnProvider", "ContinuousTurnProvider", "ClimbProvider", "GrabMoveProvider", "TwoHandedGrabMoveProvider" };

        private MaterialPropertyBlock block;
        private Color pictureColour = Color.white;
        private bool pending, holding, releasing, reduceFlicker, ducked, flat;
        private float time, releaseTime, duckUntil, flicker = 1, flickerTarget = 1, nextFlicker;
        private int card = -1, tick = -1;
        private CueHandle voice;
        private PlayerRig rig;
        private DesktopLocomotion desktop;
        private Transform view;
        private Quaternion viewRestore;
        private float pitch;
        private Behaviour[] heldMovement = System.Array.Empty<Behaviour>();

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (picture && picture.sharedMaterial && picture.sharedMaterial.HasProperty(BaseColor)) pictureColour = picture.sharedMaterial.GetColor(BaseColor);
            Show(-1, -1);
        }

        private void OnEnable()
        {
            GameEvents.ActStarted.Subscribe(OnAct);
            GameEvents.CaptionRequested.Subscribe(OnCaption);
            GameEvents.SettingsChanged.Subscribe(OnSettings);
            GameEvents.StateChanged.Subscribe(OnState);
            if (Services<ISettingsProvider>.TryGet(out ISettingsProvider settings)) reduceFlicker = settings.Current.ReduceFlicker;
        }

        private void OnDisable()
        {
            GameEvents.ActStarted.Unsubscribe(OnAct);
            GameEvents.CaptionRequested.Unsubscribe(OnCaption);
            GameEvents.SettingsChanged.Unsubscribe(OnSettings);
            GameEvents.StateChanged.Unsubscribe(OnState);
            if (holding || releasing) Release(true);
        }

        private void OnSettings(SettingsChanged e) => reduceFlicker = e.Settings.ReduceFlicker;

        private void OnState(GameStateChanged e)
        {
            // Leaving the run (menu, results) ends the bulletin at once; pause only stops its clock.
            if (e.To == GameState.Calibration || e.To == GameState.Complete) Stop();
        }

        private void OnAct(ActStarted e)
        {
            if (e.Act == ActId.Awakening && !e.IsRestore)
            {
                // The audio player releases every voice when an act starts, so the bulletin starts on the next frame.
                Stop();
                pending = true;
                return;
            }

            Stop();
        }

        /// <summary>Another voice (the PA) talks over the bulletin, and the set is turned down under it.</summary>
        private void OnCaption(CaptionRequested e)
        {
            if (!holding || e.Kind != CaptionKind.Speech) return;
            if (e.IsPositional && (e.WorldPosition - screen.position).sqrMagnitude < .04f) return; // the anchor's own words
            duckUntil = time + Mathf.Max(1.5f, e.DurationSeconds);
            if (!ducked && voice.IsValid) Cues.SetIntensity(voice, DuckedLevel);
            ducked = true;
        }

        private void Update()
        {
            if (pending)
            {
                pending = false;
                Begin();
            }

            if (!holding) return;
            float dt = Time.deltaTime;
            time += dt;

            if (ducked && time >= duckUntil)
            {
                ducked = false;
                if (voice.IsValid) Cues.SetIntensity(voice, 1);
            }

            int nextCard = card;
            while (nextCard + 1 < cards.Length && time >= cards[nextCard + 1].At) nextCard++;
            int nextTick = tickerLines.Length == 0 ? -1 : (int)(time / tickerSeconds) % tickerLines.Length;
            if (nextCard != card || nextTick != tick) Show(nextCard, nextTick);

            if (liveDot) liveDot.enabled = reduceFlicker || Mathf.Repeat(time, 1.2f) < .8f;
            Flicker(dt);

            if (time >= duration) Release(false);
        }

        private void LateUpdate()
        {
            if (!flat || !view || !rig || !rig.origin) return;
            float dt = Time.deltaTime;
            if (holding)
            {
                Vector3 to = screen.position - view.position;
                float targetYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float targetPitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
                float k = 1 - Mathf.Exp(-dt * TurnRate);
                Transform body = rig.origin.transform;
                body.rotation = Quaternion.Euler(0, Mathf.LerpAngle(body.eulerAngles.y, targetYaw, k), 0);
                pitch = Mathf.LerpAngle(pitch, targetPitch, k);
                view.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            else if (releasing)
            {
                // Hand the view back at the pitch it had, so the next mouse movement continues from there without a jump.
                releaseTime += dt;
                float k = Mathf.SmoothStep(0, 1, releaseTime / ReleaseSeconds);
                view.localRotation = Quaternion.Slerp(Quaternion.Euler(pitch, 0, 0), viewRestore, k);
                if (releaseTime >= ReleaseSeconds) Unlock();
            }
        }

        private void Begin()
        {
            time = 0;
            card = tick = -1;
            ducked = false;
            holding = true;
            releasing = false;
            Show(-1, -1);
            voice = Cues.Play(CueId.NewsBroadcast, screen.position, NoiseOrigin.Ambient);

            rig = Services<IPlayerQuery>.TryGet(out IPlayerQuery player) ? player as PlayerRig : null;
            if (!rig || !rig.origin) return;
            flat = !XRSettings.isDeviceActive;
            if (flat)
            {
                desktop = rig.GetComponent<DesktopLocomotion>();
                view = rig.origin.Camera ? rig.origin.Camera.transform : null;
                if (!view) return;
                viewRestore = view.localRotation;
                pitch = view.localEulerAngles.x;
                if (desktop) desktop.viewLocked = true;
                return;
            }

            // Headset: turn the body to the set once, as PlayerRig does when it places the player, then hold movement.
            Vector3 facing = screen.position - rig.HeadPosition;
            facing.y = 0;
            if (facing.sqrMagnitude > .01f) rig.origin.MatchOriginUpCameraForward(Vector3.up, facing.normalized);
            var held = new System.Collections.Generic.List<Behaviour>();
            foreach (Behaviour behaviour in rig.GetComponentsInChildren<Behaviour>(true))
            {
                if (!behaviour || !behaviour.enabled) continue;
                string type = behaviour.GetType().Name;
                foreach (string name in HeldProviders)
                {
                    if (type != name) continue;
                    behaviour.enabled = false;
                    held.Add(behaviour);
                    break;
                }
            }

            heldMovement = held.ToArray();
        }

        /// <summary>The bulletin is over (or was cut short): give the player back their view and their legs.</summary>
        private void Release(bool immediately)
        {
            if (holding && voice.IsValid && time < duration - .25f) Cues.Stop(voice, .3f);
            voice = default;
            holding = false;
            Show(-1, -1); // the set goes dark when the bulletin ends
            foreach (Behaviour behaviour in heldMovement)
            {
                if (behaviour) behaviour.enabled = true;
            }

            heldMovement = System.Array.Empty<Behaviour>();
            if (flat && view && !immediately)
            {
                releasing = true;
                releaseTime = 0;
                return;
            }

            if (flat && view) view.localRotation = viewRestore;
            Unlock();
        }

        private void Unlock()
        {
            releasing = false;
            if (desktop) desktop.viewLocked = false;
            desktop = null;
            view = null;
        }

        private void Stop()
        {
            pending = false;
            if (holding || releasing) Release(true);
            Show(-1, -1);
        }

        private void Show(int nextCard, int nextTick)
        {
            card = nextCard;
            tick = nextTick;
            bool on = holding;
            if (picture) picture.enabled = on;
            if (headline) { headline.gameObject.SetActive(on && card >= 0); if (card >= 0) headline.text = cards[card].Headline; }
            if (detail) { detail.gameObject.SetActive(on && card >= 0); if (card >= 0) detail.text = cards[card].Detail; }
            if (ticker) { ticker.gameObject.SetActive(on && tick >= 0); if (tick >= 0) ticker.text = tickerLines[tick]; }
            if (liveDot) liveDot.enabled = on;
            foreach (GameObject label in labels)
            {
                if (label) label.SetActive(on);
            }

            if (!on) SetPicture(0);
        }

        /// <summary>A failing set: the picture dips and rolls now and then. Off with "reduce flicker".</summary>
        private void Flicker(float dt)
        {
            if (reduceFlicker) { flicker = flickerTarget = 1; }
            else
            {
                nextFlicker -= dt;
                if (nextFlicker <= 0)
                {
                    bool dip = Random.value < .22f;
                    flickerTarget = dip ? Random.Range(.25f, .6f) : Random.Range(.88f, 1f);
                    nextFlicker = dip ? Random.Range(.05f, .12f) : Random.Range(.25f, 1.4f);
                }

                flicker = Mathf.MoveTowards(flicker, flickerTarget, dt * 9f);
            }

            SetPicture(flicker);
        }

        private void SetPicture(float level)
        {
            if (!picture) return;
            picture.GetPropertyBlock(block);
            block.SetColor(BaseColor, new Color(pictureColour.r * level, pictureColour.g * level, pictureColour.b * level, 1));
            picture.SetPropertyBlock(block);
        }
    }
}
