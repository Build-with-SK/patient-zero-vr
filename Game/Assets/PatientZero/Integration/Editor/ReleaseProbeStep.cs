// Claude-owned release QA (owner final-release mission, 2026-10-30).
using PatientZero.Core;
using PatientZero.Flow;
using UnityEngine;

namespace PatientZero.Integration.Editor
{
    /// <summary>Adds the release probe to Bootstrap. It does nothing unless the built game is started with -pz-autoplay.</summary>
    public static class ReleaseProbeStep
    {
        [SceneBuildStep(BuildScene.Bootstrap, 950)]
        public static void Build(Transform root)
        {
            var flow = Object.FindFirstObjectByType<GameFlow>();
            if (!flow) throw new System.InvalidOperationException("The Flow build step must add GameFlow before the release probe.");
            root.gameObject.AddComponent<ReleaseProbe>().flow = flow;
        }
    }
}
