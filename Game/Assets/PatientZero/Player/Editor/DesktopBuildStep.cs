// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using PatientZero.Interaction;
using UnityEngine;
// Moved from Interaction/Editor at integration (exception E-3 = FIX-A-1, docs/collab/INTEGRATION_ROUND2.md):
// Interaction builds Ward 300-399 only; Player owns Bootstrap 400-499.
namespace PatientZero.Player.Editor
{
    public static class DesktopBuildStep
    {
        /// <summary>Composer connects desktop mouse adapter to the same head camera as the XRI rig.</summary>
        [SceneBuildStep(BuildScene.Bootstrap,420)]
        public static void Build(Transform root){var desktop=root.gameObject.AddComponent<DesktopInteraction>();desktop.view=UnityEngine.Object.FindFirstObjectByType<Camera>();}
    }
}
