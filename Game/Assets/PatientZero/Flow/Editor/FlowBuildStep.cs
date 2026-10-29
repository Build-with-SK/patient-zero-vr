// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Flow.Editor
{
    public static class FlowBuildStep
    {
        /// <summary>Composer creates persistent Bootstrap settings and flow executor.</summary>
        [SceneBuildStep(BuildScene.Bootstrap,400)]
        public static void Build(Transform root){var settings=root.gameObject.AddComponent<SettingsProvider>();var flow=root.gameObject.AddComponent<GameFlow>();flow.settings=settings;}
    }
}
