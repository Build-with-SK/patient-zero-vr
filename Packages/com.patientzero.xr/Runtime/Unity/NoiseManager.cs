using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    [DefaultExecutionOrder(-500)]
    public sealed class NoiseManager : MonoBehaviour
    {
        [Min(1)] public int MaxActiveZombies = 12;
        [Min(1)] public int MaxNoiseSources = 128;
        [Min(.01f)] public float SourceCooldown = .15f;
        public NoiseBus Bus { get; private set; }
        public int ActiveZombies { get; private set; }
        private INoiseListener[] admitted;
        private void Awake()
        {
            admitted = new INoiseListener[Mathf.Max(1, MaxActiveZombies)];
            Bus = new NoiseBus(admitted.Length + 16, Mathf.Max(1, MaxNoiseSources), SourceCooldown);
        }
        public bool TryAdmitZombie(INoiseListener listener)
        {
            if (listener == null || Bus == null) return false;
            for (int i = 0; i < admitted.Length; i++) if (ReferenceEquals(admitted[i], listener)) return true;
            for (int i = 0; i < admitted.Length; i++)
            {
                if (admitted[i] != null) continue;
                if (!Bus.Register(listener)) return false;
                admitted[i] = listener; ActiveZombies++; return true;
            }
            return false;
        }
        public void ReleaseZombie(INoiseListener listener)
        {
            if (admitted == null || listener == null) return;
            for (int i = 0; i < admitted.Length; i++)
            {
                if (!ReferenceEquals(admitted[i], listener)) continue;
                Bus.Unregister(listener); admitted[i] = null; ActiveZombies--; return;
            }
        }
        public bool Emit(Vector3 position, float intensity, float radius, NoiseCategory category, int sourceId)
        {
            double now = Time.timeAsDouble;
            var noise = new NoiseEvent(ToPoint(position), intensity, radius, now, category, sourceId);
            return Bus != null && Bus.Publish(in noise, now);
        }
        public static WorldPoint ToPoint(Vector3 p) => new WorldPoint(p.x, p.y, p.z);
        public static Vector3 ToVector(WorldPoint p) => new Vector3(p.X, p.Y, p.Z);
    }
}
