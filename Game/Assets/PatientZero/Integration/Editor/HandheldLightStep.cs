// Claude-owned integration (owner final-release mission, 2026-10-30; E-17).
using PatientZero.Atmosphere;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.Integration.Editor
{
    /// <summary>Adds the torch owner (seat B's HandheldLight, contract v1.2) to Ward. It makes no light until the player switches it on.</summary>
    public static class HandheldLightStep
    {
        [SceneBuildStep(BuildScene.Ward, 960)]
        public static void Build(Transform root)
        {
            root.gameObject.AddComponent<HandheldLight>();
        }
    }
}
