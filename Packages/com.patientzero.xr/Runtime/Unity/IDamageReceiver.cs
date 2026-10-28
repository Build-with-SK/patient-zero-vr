using UnityEngine;

namespace PatientZero.XR
{
    // Codex 1 implements this on the player's collider root; return true only if accepted.
    public interface IDamageReceiver { bool TryApplyDamage(float amount, Vector3 origin); }
}
