// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace PatientZero.Interaction
{
    /// <summary>Mouse adapter for the same puzzle and rigidbody APIs used by XRI; opt-in desktop only.</summary>
    public sealed class DesktopInteraction : MonoBehaviour
    {
        public Camera view;public bool desktopSimulation=true;private PhysicalPuzzle puzzle;private PhysicsProp prop;private Vector3 gripPoint;private float angle;private bool live;
        private void OnEnable(){GameEvents.StateChanged.Subscribe(State);}
        private void OnDisable(){GameEvents.StateChanged.Unsubscribe(State);Release(false);}
        private void State(GameStateChanged state){live=GameStateRules.IsSimulationLive(state.To);if(!live)Release(false);}
        private void Update()
        {
#if PZ_DESKTOP_MOUSE

            if(!desktopSimulation || XRSettings.isDeviceActive || !live || !view)return;var m=Mouse.current;var k=Keyboard.current;if(m==null || k==null)return;
            if(m.leftButton.wasPressedThisFrame)Begin();
            if(puzzle && m.leftButton.isPressed)MovePuzzle(m.delta.ReadValue());
            if(m.leftButton.wasReleasedThisFrame)Release(false);
            if(k.qKey.wasPressedThisFrame)Release(true);
        
#endif
}
        private void Begin(){if(!Physics.Raycast(view.transform.position,view.transform.forward,out var hit,1.5f))return;puzzle=hit.collider.GetComponentInParent<PhysicalPuzzle>();if(puzzle){gripPoint=hit.point;angle=0;puzzle.BeginGrip(gripPoint);return;}prop=hit.collider.GetComponentInParent<PhysicsProp>();if(prop)prop.DesktopGrab();}
        private void MovePuzzle(Vector2 delta){if(puzzle.mechanic==PhysicalPuzzle.Mechanic.Valve){angle+=delta.x;var radial=Quaternion.AngleAxis(angle,puzzle.transform.TransformDirection(puzzle.axis))*(gripPoint-puzzle.transform.position);puzzle.SampleGrip(puzzle.transform.position+radial,Time.deltaTime);}else{gripPoint+=puzzle.mechanic==PhysicalPuzzle.Mechanic.Plank?puzzle.transform.TransformDirection(puzzle.axis)*delta.y*.002f:Vector3.down*(-delta.y*.002f);puzzle.SampleGrip(gripPoint,Time.deltaTime);}}
        private void FixedUpdate(){
#if PZ_DESKTOP_MOUSE
if(prop && prop.Held && !prop.Body.isKinematic){Vector3 target=view.transform.position+view.transform.forward*.65f;prop.Body.linearVelocity=Vector3.ClampMagnitude((target-prop.Body.position)*12,5);}
#endif
}
        private void Release(bool throwing){if(puzzle)puzzle.EndGrip();puzzle=null;if(prop)prop.DesktopRelease(throwing?view.transform.forward*5:Vector3.zero);prop=null;}
    }
}
