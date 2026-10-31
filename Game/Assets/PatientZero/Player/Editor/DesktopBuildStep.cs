// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Interaction;
using UnityEngine;
namespace PatientZero.Player.Editor
{
    public static class DesktopBuildStep
    {
        /// <summary>Composer connects desktop mouse adapter to the same head camera as the XRI rig.</summary>
        [SceneBuildStep(BuildScene.Bootstrap,420)]
        public static void Build(Transform root){var desktop=root.gameObject.AddComponent<DesktopInteraction>();var rig=UnityEngine.Object.FindFirstObjectByType<PlayerRig>();if(!rig || !rig.origin || !rig.origin.Camera)throw new System.InvalidOperationException("Player rig camera missing before desktop adapter step.");desktop.view=rig.origin.Camera;}
    }
}
