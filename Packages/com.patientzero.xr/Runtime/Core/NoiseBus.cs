using System;

namespace PatientZero.Core
{
    // Single game-thread bus. Fixed capacities and no allocations during dispatch.
    public sealed class NoiseBus
    {
        private readonly INoiseListener[] listeners;
        private readonly int[] sources;
        private readonly double[] lastTimes;
        private readonly bool[] occupied;
        private bool dispatching;
        public readonly double Cooldown;
        public readonly double MaxAge;
        public int Rejected { get; private set; }
        public int Published { get; private set; }
        public NoiseBus(int listenerCapacity = 16, int sourceCapacity = 128, double cooldown = .15, double maxAge = 1)
        {
            if (listenerCapacity < 1 || sourceCapacity < 1 || cooldown < 0 || maxAge <= 0 ||
                double.IsNaN(cooldown) || double.IsInfinity(cooldown) || double.IsNaN(maxAge) || double.IsInfinity(maxAge))
                throw new ArgumentOutOfRangeException();
            listeners = new INoiseListener[listenerCapacity]; sources = new int[sourceCapacity];
            lastTimes = new double[sourceCapacity]; occupied = new bool[sourceCapacity];
            Cooldown = cooldown; MaxAge = maxAge;
        }
        public bool Register(INoiseListener listener)
        {
            if (listener == null) return false;
            for (int i = 0; i < listeners.Length; i++) if (ReferenceEquals(listeners[i], listener)) return true;
            for (int i = 0; i < listeners.Length; i++) if (listeners[i] == null) { listeners[i] = listener; return true; }
            return false;
        }
        public void Unregister(INoiseListener listener)
        {
            for (int i = 0; i < listeners.Length; i++) if (ReferenceEquals(listeners[i], listener)) listeners[i] = null;
        }
        public bool Publish(in NoiseEvent noise, double now)
        {
            if (dispatching || !noise.IsValid || double.IsNaN(now) || double.IsInfinity(now) ||
                noise.Timestamp > now || now - noise.Timestamp > MaxAge) return Reject();
            int slot = -1;
            for (int i = 0; i < sources.Length; i++)
            {
                if (occupied[i] && sources[i] == noise.SourceId)
                {
                    if (noise.Timestamp - lastTimes[i] < Cooldown) return Reject();
                    slot = i; break;
                }
                if (!occupied[i] || now - lastTimes[i] >= Cooldown) slot = i;
            }
            // Do not evict an unexpired cooldown: a flood cannot bypass throttling.
            if (slot < 0) return Reject();
            sources[slot] = noise.SourceId; lastTimes[slot] = noise.Timestamp; occupied[slot] = true;
            Published++;
            dispatching = true;
            try
            {
                for (int i = 0; i < listeners.Length; i++) listeners[i]?.Hear(in noise);
            }
            finally { dispatching = false; }
            return true;
        }
        private bool Reject() { Rejected++; return false; }
    }
}
