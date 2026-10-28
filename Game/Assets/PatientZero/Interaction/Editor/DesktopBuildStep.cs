// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using PatientZero.Core;
using UnityEngine;
namespace PatientZero.Interaction.Editor
{
    public static class DesktopBuildStep
    {
        /// <summary>Composer connects desktop mouse adapter to the same head camera as the XRI rig.</summary>
        [SceneBuildStep(BuildScene.Bootstrap,420)]
        public static void Build(Transform root){var desktop=root.gameObject.AddComponent<DesktopInteraction>();desktop.view=UnityEngine.Object.FindFirstObjectByType<Camera>();}
    }
}
