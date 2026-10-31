using PatientZero.Core;

namespace PatientZero.Audio.Logic
{
    public readonly struct VoiceAdmission
    {
        public readonly bool Accepted, Spatialized;
        public readonly int Slot, Handle;
        public readonly CueId Stolen;
        public VoiceAdmission(bool accepted, bool spatialized, int slot, int handle, CueId stolen)
        { Accepted = accepted; Spatialized = spatialized; Slot = slot; Handle = handle; Stolen = stolen; }
    }

    /// <summary>Fixed voice pool policy. Reports every rejection/steal; protected UI/Voice are never stolen.</summary>
    public sealed class VoiceBudget
    {
        private struct Slot { public bool Active, Spatialized; public int Handle; public long Order; public CueDefinition Cue; }
        private readonly Slot[] slots = new Slot[24];
        private int nextHandle;
        private long order;
        public int ActiveCount { get; private set; }
        public int SpatialCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int StolenCount { get; private set; }
        public VoiceAdmission Start(CueDefinition cue, bool wantsSpatializer)
        {
            int slot = -1;
            for (int i = 0; i < slots.Length; i++) if (!slots[i].Active) { slot = i; break; }
            CueId stolen = CueId.None;
            if (slot < 0)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].Cue.Bus == MixerBus.UI || slots[i].Cue.Bus == MixerBus.Voice) continue;
                    if (slot < 0 || slots[i].Cue.Priority > slots[slot].Cue.Priority ||
                        slots[i].Cue.Priority == slots[slot].Cue.Priority && slots[i].Order < slots[slot].Order) slot = i;
                }
                if (slot < 0 || cue.Priority > slots[slot].Cue.Priority)
                { RejectedCount++; return new VoiceAdmission(false, false, -1, 0, CueId.None); }
                stolen = slots[slot].Cue.Id; ReleaseSlot(slot); StolenCount++;
            }
            bool spatial = wantsSpatializer && cue.World && SpatialCount < 12;
            // Beyond the HRTF cap keep 3D panning, not a 2D sound or an unreported lost cue.
            do { nextHandle = nextHandle == int.MaxValue ? 1 : nextHandle + 1; } while (Find(nextHandle) >= 0);
            slots[slot] = new Slot { Active = true, Cue = cue, Handle = nextHandle, Order = order++, Spatialized = spatial };
            ActiveCount++; if (spatial) SpatialCount++;
            return new VoiceAdmission(true, spatial, slot, nextHandle, stolen);
        }
        public int Find(int handle)
        {
            if (handle == 0) return -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i].Active && slots[i].Handle == handle) return i;
            return -1;
        }
        public void Release(int handle) { int slot = Find(handle); if (slot >= 0) ReleaseSlot(slot); }
        private void ReleaseSlot(int slot)
        { if (slots[slot].Spatialized) SpatialCount--; slots[slot].Active = false; ActiveCount--; }
    }
}
