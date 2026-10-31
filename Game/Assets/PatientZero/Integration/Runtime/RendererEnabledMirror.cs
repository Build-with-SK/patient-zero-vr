// Claude-owned integration (art pass, 2026-10-31; E-20).
using UnityEngine;

namespace PatientZero.Integration
{
    /// <summary>
    /// Integration E-20: the zombie model has two skinned meshes (body and eyelids). Seat B's shell shows and hides a
    /// zombie through one renderer (ZombieAnimationController.Visual), so the other renderers copy its enabled state.
    /// </summary>
    public sealed class RendererEnabledMirror : MonoBehaviour
    {
        public Renderer source;
        public Renderer[] followers = new Renderer[0];
        private bool known, shown;

        private void LateUpdate()
        {
            bool on = source && source.enabled;
            if (known && on == shown) return;
            known = true;
            shown = on;
            for (int i = 0; i < followers.Length; i++)
            {
                if (followers[i]) followers[i].enabled = on;
            }
        }
    }
}
