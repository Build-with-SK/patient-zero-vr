using System;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Audio.Logic
{
    /// <summary>Port of NoiseBus guards at 72be6d8; filtering policy only, never a second event bus.</summary>
    public sealed class NoiseGate
    {
        private readonly int[] keys;
        private readonly float[] times;
        private readonly bool[] used;
        private bool dispatching;
        public NoiseGate(int capacity = 128)
        { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); keys = new int[capacity]; times = new float[capacity]; used = new bool[capacity]; }
        public static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        public static bool Finite(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);
        public bool TryEnter(in NoiseEvent noise, int sourceKey, float now)
        {
            if (dispatching || !Finite(noise.Position) || !Finite(noise.Time) || !Finite(now) ||
                !Finite(noise.RadiusMeters) || noise.RadiusMeters <= 0 || noise.Time < 0 ||
                noise.Time > now || now - noise.Time > 1) return false;
            int slot = -1;
            for (int i = 0; i < keys.Length; i++)
            {
                if (used[i] && keys[i] == sourceKey)
                { if (noise.Time - times[i] < .15f) return false; slot = i; break; }
                if (!used[i] || now - times[i] >= .15f) slot = i;
            }
            if (slot < 0) return false;
            keys[slot] = sourceKey; times[slot] = noise.Time; used[slot] = true; dispatching = true;
            return true;
        }
        public void Exit() => dispatching = false;
    }
}
