// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Player;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
namespace PatientZero.Interaction.Editor
{
    public static class InteractionBuildStep
    {
        /// <summary>Composer builds A-owned puzzles and props; B builds geometry, lighting and beats.</summary>
        [SceneBuildStep(BuildScene.Ward,310)]
        public static void Build(Transform root)
        {
            var publisher=root.gameObject.AddComponent<ObjectivePublisher>();
            Plank(root,publisher,WardLayout.A1_PlankLow,0);Plank(root,publisher,WardLayout.A1_PlankMid,1);Plank(root,publisher,WardLayout.A1_PlankHigh,2);
            Constrained(root,publisher,WardLayout.A2_ValveWheel,PhysicalPuzzle.Mechanic.Valve,PrimitiveType.Cylinder,new Vector3(.38f,.04f,.38f));
            Constrained(root,publisher,WardLayout.A4_ReleaseLever,PhysicalPuzzle.Mechanic.Lever,PrimitiveType.Cube,new Vector3(.08f,.35f,.08f));
            var hook=At(root,WardLayout.A2_CableHook);var shelf=Shape(hook.transform,"Plug hook shelf",PrimitiveType.Cube,new Vector3(.18f,.04f,.18f));shelf.transform.localPosition=Vector3.down*.08f;var plug=Prop(hook.transform,"Cable plug",PrimitiveType.Cube,new Vector3(.045f,.045f,.1f),false);plug.requiredItem=true;
            var socket=At(root,WardLayout.A2_Socket);Shape(socket.transform,"Socket",PrimitiveType.Cube,new Vector3(.12f,.15f,.12f));var cable=socket.AddComponent<LineRenderer>();cable.positionCount=3;cable.startWidth=cable.endWidth=.015f;cable.sharedMaterial=Material(Color.black);var puzzle=socket.AddComponent<CableSocket>();puzzle.plug=plug;puzzle.objectives=publisher;puzzle.cable=cable;
            var tray=At(root,WardLayout.A1_Tray);Shape(tray.transform,"Tray",PrimitiveType.Cube,new Vector3(.5f,.04f,.35f));for(int i=0;i<3;i++){var prop=Prop(tray.transform,i==0?"Bottle":i==1?"Kidney dish":"Clipboard",i==0?PrimitiveType.Cylinder:PrimitiveType.Cube,i==0?new Vector3(.06f,.12f,.06f):new Vector3(.12f,.025f,.16f),false);prop.transform.localPosition=new Vector3((i-1)*.16f,.15f,0);}
            var cart=new GameObject("Bottle trolley");cart.transform.SetParent(root,false);cart.transform.position=WardLayout.A3_Cart.Position;var trolley=Prop(cart.transform,"Pushable trolley",PrimitiveType.Cube,new Vector3(.7f,.12f,.5f),true);trolley.GetComponent<Rigidbody>().mass=18;for(int i=0;i<4;i++){var leg=Shape(trolley.transform,"Trolley support",PrimitiveType.Cube,new Vector3(.04f,.65f,.04f));leg.transform.localPosition=new Vector3(i%2==0?-.28f:.28f,-.32f,i<2?-.2f:.2f);}for(int i=0;i<3;i++){var bottle=Prop(cart.transform,"Loose bottle",PrimitiveType.Cylinder,new Vector3(.055f,.11f,.055f),false);bottle.transform.localPosition=new Vector3((i-1)*.16f,.2f,0);}
            Door(root,publisher,WardLayout.A1_DoorWindow,true);Door(root,publisher,WardLayout.A4_ExitDoor,false);root.gameObject.AddComponent<BeaconArrival>().objectives=publisher;
            foreach(var point in WardLayout.CriticalPath){var anchorRoot=At(root,point);var target=anchorRoot.AddComponent<SphereCollider>();target.radius=.18f;target.isTrigger=true;var anchor=anchorRoot.AddComponent<TeleportationAnchor>();anchor.teleportAnchorTransform=anchorRoot.transform;var gate=anchorRoot.AddComponent<AnchorGate>();gate.act=point.Act;gate.target=target;var marker=Shape(anchorRoot.transform,"Aim-only marker",PrimitiveType.Cylinder,new Vector3(.25f,.01f,.25f));Object.DestroyImmediate(marker.GetComponent<Collider>());gate.marker=marker.GetComponent<Renderer>();gate.marker.enabled=false;}
        }
        private static void Plank(Transform root,ObjectivePublisher publisher,LayoutPoint point,int index){var p=Constrained(root,publisher,point,PhysicalPuzzle.Mechanic.Plank,PrimitiveType.Cube,new Vector3(1.1f,.12f,.06f));p.plankIndex=index;p.axis=Vector3.back;}
        private static PhysicalPuzzle Constrained(Transform root,ObjectivePublisher publisher,LayoutPoint point,PhysicalPuzzle.Mechanic mode,PrimitiveType primitive,Vector3 size){var o=At(root,point);var visual=Shape(o.transform,mode.ToString(),primitive,size);if(mode==PhysicalPuzzle.Mechanic.Valve)visual.transform.localRotation=Quaternion.Euler(90,0,0);var xri=o.AddComponent<XRSimpleInteractable>();xri.colliders.Add(visual.GetComponent<Collider>());var p=o.AddComponent<PhysicalPuzzle>();p.mechanic=mode;p.movingPart=visual.transform;p.objectives=publisher;return p;}
        private static void Door(Transform root,ObjectivePublisher publisher,LayoutPoint point,bool isolation){var o=new GameObject(isolation?"Isolation door — pending director gate":"Emergency door");o.transform.SetParent(root,false);o.transform.position=point.Position;o.transform.rotation=Quaternion.Euler(0,isolation?0:90,0);var leaf=Shape(o.transform,"Door leaf",PrimitiveType.Cube,new Vector3(1,2,.08f));leaf.transform.localPosition=Vector3.up*(isolation?-.65f:1);var door=o.AddComponent<ObjectiveDoor>();door.objectives=publisher;door.leaf=leaf.transform;door.isolationDoor=isolation;}
        private static GameObject At(Transform root,LayoutPoint point){var o=new GameObject(point.Id);o.transform.SetParent(root,false);o.transform.position=point.Position;o.transform.rotation=Quaternion.Euler(0,point.YawDegrees,0);o.AddComponent<LayoutTag>().LayoutId=point.Id;return o;}
        private static PhysicsProp Prop(Transform root,string name,PrimitiveType primitive,Vector3 size,bool heavy){var o=new GameObject(name);o.transform.SetParent(root,false);Shape(o.transform,name+" geometry",primitive,size);var body=o.AddComponent<Rigidbody>();body.mass=heavy?12:0.3f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;if(heavy)body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;o.AddComponent<XRGrabInteractable>().throwOnDetach=true;var prop=o.AddComponent<PhysicsProp>();prop.heavy=heavy;return prop;}
        private static GameObject Shape(Transform root,string name,PrimitiveType primitive,Vector3 size){var o=GameObject.CreatePrimitive(primitive);o.name=name;o.transform.SetParent(root,false);o.transform.localScale=size;return o;}
        private static Material Material(Color color){const string folder="Assets/PatientZero/Interaction/Generated";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/PatientZero/Interaction","Generated");string path=folder+"/CableRubber.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.color=color;AssetDatabase.CreateAsset(m,path);}return m;}
    }
}
