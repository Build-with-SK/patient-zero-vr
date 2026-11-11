// Claude-owned integration (horror pass, 2026-11-02; E-21).
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// The television bolted to the ceiling of Isolation Room B-7. When a new run starts it carries an emergency news
    /// broadcast about the outbreak. E-24 (owner, 2026-11-11, as decided on 2026-11-03: "no lock"): it no longer holds the
    /// player; it plays, about 17 s, and they may look away or walk off. A restored act does not replay it.
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

        /// <summary>True while the bulletin plays.</summary>
        public bool Playing => holding;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private const float DuckedLevel = .3f;

        private MaterialPropertyBlock block;
        private Color pictureColour = Color.white;
        private bool pending, holding, reduceFlicker, ducked;
        private float time, duckUntil, flicker = 1, flickerTarget = 1, nextFlicker;
        private int card = -1, tick = -1;
        private CueHandle voice;

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
            if (holding) Release();
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

            if (time >= duration) Release();
        }

        private void Begin()
        {
            time = 0;
            card = tick = -1;
            ducked = false;
            holding = true;
            Show(-1, -1);
            voice = Cues.Play(CueId.NewsBroadcast, screen.position, NoiseOrigin.Ambient);
        }

        /// <summary>The bulletin is over, or was cut short: the set goes dark.</summary>
        private void Release()
        {
            if (holding && voice.IsValid && time < duration - .25f) Cues.Stop(voice, .3f);
            voice = default;
            holding = false;
            Show(-1, -1);
        }

        private void Stop()
        {
            pending = false;
            if (holding) Release();
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
