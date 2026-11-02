// Claude-owned integration (horror pass, 2026-11-02; E-21).
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// The sound of the hospital falling apart, layered over the director's beats: people sobbing behind closed doors on the
    /// player's floor, screams from elsewhere in the building, and the crowd outside the main doors on the ground floor.
    /// Every sound is a Core cue (Audio wires the recordings), so captions, volume settings, pause and the muffling of sound
    /// behind walls all apply, and none of it is a noise the zombies can hear. Reduced scares: fewer, quieter screams.
    /// </summary>
    public sealed class HorrorSoundscape : MonoBehaviour
    {
        [Tooltip("Rooms with someone crying in them, behind a door. Only those on the player's floor are used.")]
        public Vector3[] cryingRooms = new Vector3[0];
        [Tooltip("Where screams come from. Only points on the player's floor, 9 to 30 m from the player, are used.")]
        public Vector3[] screamPoints = new Vector3[0];
        [Tooltip("The crowd outside the main doors (ground floor).")]
        public Vector3 crowd;
        [Tooltip("While it holds the player, no screams play over it.")]
        public NewsBroadcast news;

        private int[] cryingFloor, screamFloor;
        private int floor;
        private bool live, reduced, actPending;
        private float nextCry, nextScream;
        private int cryRoom = -1, cryRepeats;
        private CueHandle crowdLoop;

        private void Awake()
        {
            cryingFloor = FloorsOf(cryingRooms);
            screamFloor = FloorsOf(screamPoints);
        }

        private static int[] FloorsOf(Vector3[] points)
        {
            var floors = new int[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                for (int r = 0; r < WardLayout.Rooms.Count; r++)
                {
                    if (WardLayout.Rooms[r].Contains(points[p], 0)) floors[p] = WardLayout.RoomFloors[r];
                }
            }

            return floors;
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
            live = GameStateRules.IsSimulationLive(e.To);
            // Paused keeps the loop (the listener pause holds it); a finished or abandoned run lets it go.
            if (e.To == GameState.Complete || e.To == GameState.Calibration) StopCrowd();
        }

        private void StopCrowd()
        {
            if (crowdLoop.IsValid) Cues.Stop(crowdLoop, 1.5f);
            crowdLoop = default;
        }

        // The audio player releases every voice when an act starts, so the act's sounds start on the next frame.
        private void OnAct(ActStarted e)
        {
            floor = WardLayout.FloorOf(e.Act);
            live = true;
            actPending = true;
            cryRoom = -1;
            crowdLoop = default; // every voice was released with the act
            nextCry = Random.Range(3f, 7f);
            nextScream = Random.Range(14f, 26f) * (reduced ? 2f : 1f);
        }

        private void Update()
        {
            if (!live || floor == 0) return;
            if (actPending)
            {
                actPending = false;
                if (floor == 1) crowdLoop = Cues.Play(CueId.CrowdPanic, crowd, NoiseOrigin.Ambient, reduced ? .45f : .7f);
            }

            if (!Services<IPlayerQuery>.TryGet(out IPlayerQuery player)) return;
            Vector3 head = player.HeadPosition;
            float dt = Time.deltaTime;

            nextCry -= dt;
            if (nextCry <= 0)
            {
                nextCry = Random.Range(6.2f, 10f); // the recording is 5.6 s: sobbing almost without a break
                int room = CryingRoom(head);
                if (room >= 0) Cues.Play(CueId.VictimCrying, cryingRooms[room], NoiseOrigin.Ambient, Random.Range(.55f, .9f) * (reduced ? .7f : 1f));
            }

            nextScream -= dt;
            if (nextScream <= 0)
            {
                nextScream = Random.Range(22f, 50f) * (reduced ? 2.2f : 1f);
                if (news && news.Holding) return;
                int point = ScreamPoint(head);
                if (point >= 0) Cues.Play(CueId.DistantScream, screamPoints[point], NoiseOrigin.Ambient, Random.Range(.6f, 1f) * (reduced ? .55f : 1f));
            }
        }

        /// <summary>A person cries in bouts: the same room two or three times, then another room on this floor.</summary>
        private int CryingRoom(Vector3 head)
        {
            if (cryRoom >= 0 && cryRepeats > 0 && cryingFloor[cryRoom] == floor)
            {
                cryRepeats--;
                return cryRoom;
            }

            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < cryingRooms.Length; i++)
            {
                if (cryingFloor[i] != floor || i == cryRoom) continue;
                float score = Vector3.Distance(head, cryingRooms[i]) + Random.Range(0f, 8f);
                if (score < bestScore) { bestScore = score; best = i; }
            }

            if (best < 0 && cryRoom >= 0 && cryingFloor[cryRoom] == floor) best = cryRoom;
            cryRoom = best;
            cryRepeats = Random.Range(1, 3);
            return best;
        }

        private int ScreamPoint(Vector3 head)
        {
            int start = Random.Range(0, Mathf.Max(1, screamPoints.Length));
            for (int k = 0; k < screamPoints.Length; k++)
            {
                int i = (start + k) % screamPoints.Length;
                if (screamFloor[i] != floor) continue;
                float d = Vector3.Distance(head, screamPoints[i]);
                if (d >= 9f && d <= 30f) return i;
            }

            return -1;
        }
    }
}
