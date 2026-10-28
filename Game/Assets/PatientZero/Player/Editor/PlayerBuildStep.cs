// UNVERIFIED IN EDITOR — canonical project pin/packages/composer pending.
using System;
using PatientZero.Core;
using PatientZero.Flow;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
namespace PatientZero.Player.Editor
{
    public static class PlayerBuildStep
    {
        /// <summary>Uses imported Unity Starter Assets rig, refuses to fabricate a hand/controller rig.</summary>
        [SceneBuildStep(BuildScene.Bootstrap,410)]
        public static void Build(Transform root)
        {
            var prefab=FindRig();if(!prefab)throw new InvalidOperationException("Import approved XRI >=3.5 Starter Assets and Hands Interaction Demo, including a rig with XRInputModalityManager and both hand/controller groups, before building scenes.");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);var origin=instance.GetComponentInChildren<XROrigin>(true);var manager=instance.GetComponentInChildren<XRInputModalityManager>(true);
            if(!origin || !origin.Camera || !manager || !manager.leftHand || !manager.rightHand || !manager.leftController || !manager.rightController)throw new InvalidOperationException("Imported rig is missing required hand/controller references.");
            var player=instance.AddComponent<PlayerRig>();player.origin=origin;player.head=origin.Camera.transform;player.heightRoot=origin.CameraFloorOffsetObject.transform;
            var bridge=instance.AddComponent<XriTrackingBridge>();bridge.rig=player;bridge.modalityManager=manager;bridge.flow=UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            bridge.leftHandPose=Pose(manager.leftHand.transform,"Palm");bridge.rightHandPose=Pose(manager.rightHand.transform,"Palm");bridge.leftControllerPose=manager.leftController.transform;bridge.rightControllerPose=manager.rightController.transform;player.leftPose=bridge.leftControllerPose;player.rightPose=bridge.rightControllerPose;
            var motion=instance.AddComponent<MotionSettings>();motion.snap=instance.GetComponentInChildren<SnapTurnProvider>(true);motion.smoothTurn=instance.GetComponentInChildren<ContinuousTurnProvider>(true);motion.smoothMove=instance.GetComponentInChildren<ContinuousMoveProvider>(true);
            foreach(var behaviour in instance.GetComponentsInChildren<Behaviour>(true))if(behaviour.GetType().Name=="TunnelingVignetteController")motion.vignette=behaviour;
            var desktop=instance.AddComponent<DesktopLocomotion>();desktop.rig=player;desktop.flow=bridge.flow;desktop.cameraView=origin.Camera;desktop.capsule=origin.GetComponent<CharacterController>();
            var canvasObject=new GameObject("Full view fader",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));canvasObject.transform.SetParent(origin.Camera.transform,false);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=origin.Camera;canvas.planeDistance=.06f;canvas.sortingOrder=32000;var group=canvasObject.GetComponent<CanvasGroup>();group.alpha=1;group.blocksRaycasts=false;
            var imageObject=new GameObject("Fade",typeof(RectTransform),typeof(Image));imageObject.transform.SetParent(canvasObject.transform,false);var rect=imageObject.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;var image=imageObject.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;var fader=instance.AddComponent<ScreenFader>();fader.overlay=group;fader.image=image;
            var reveal=instance.AddComponent<InfectionHands>();var poses=new[]{bridge.leftHandPose,bridge.rightHandPose,bridge.leftControllerPose,bridge.rightControllerPose};reveal.veins=new Renderer[poses.Length*3];int index=0;foreach(var pose in poses)for(int i=0;i<3;i++){var vein=GameObject.CreatePrimitive(PrimitiveType.Capsule);vein.name="Patient Zero vein overlay";vein.transform.SetParent(pose,false);vein.transform.localScale=new Vector3(.003f,.025f,.003f);vein.transform.localPosition=new Vector3((i-1)*.012f,.005f,.015f);UnityEngine.Object.DestroyImmediate(vein.GetComponent<Collider>());var material=VeinMaterial();vein.GetComponent<Renderer>().sharedMaterial=material;reveal.veins[index++]=vein.GetComponent<Renderer>();}
        }
        private static Material VeinMaterial(){const string folder="Assets/PatientZero/Player/Generated";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/PatientZero/Player","Generated");string path=folder+"/Veins.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}return m;}
        private static GameObject FindRig(){foreach(var guid in AssetDatabase.FindAssets("t:Prefab")){string path=AssetDatabase.GUIDToAssetPath(guid);if(!path.StartsWith("Assets/Samples/XR Interaction Toolkit/",StringComparison.Ordinal))continue;var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var m=prefab.GetComponentInChildren<XRInputModalityManager>(true);if(m && m.leftHand && m.rightHand && m.leftController && m.rightController && prefab.GetComponentInChildren<XROrigin>(true))return prefab;}return null;}
        private static Transform Pose(Transform parent,string name){foreach(var t in parent.GetComponentsInChildren<Transform>(true))if(t.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0)return t;throw new InvalidOperationException("Imported hand rig requires a Palm transform for pose query.");}
    }
}
