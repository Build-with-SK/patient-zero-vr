namespace PatientZero.Environment.Logic
{
    /// <summary>B-local mask policy. The integrator supplies a door layer; this never edits TagManager.</summary>
    public readonly struct WardCollisionPolicy
    {
        public readonly int Hearing, Sight, Movement, Audio;
        public WardCollisionPolicy(int doorLayer)
        {
            // User layers only: reject built-in layers, missing names and out-of-range indices.
            Hearing = doorLayer >= 8 && doorLayer <= 31 ? 1 << doorLayer : 0;
            Sight = Movement = Audio = 1 | Hearing; // Existing greybox geometry is on Default (0).
        }
    }
}
