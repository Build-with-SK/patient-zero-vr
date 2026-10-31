// UNVERIFIED IN EDITOR — pinned Unity 6000.3.27f1 is not installed locally.
using PatientZero.Core;
using PatientZero.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace PatientZero.Interaction
{
    /// <summary>Mouse adapter for the same puzzle and rigidbody APIs used by XRI; opt-in desktop only.</summary>
    /// <remarks>Integration E-15: reads FlatInput (keyboard and mouse on Windows, touch on phone and iPad). E-17 (contract v1.2,
    /// D-1b): the torch switch (F or LIGHT) and its pose, raised as HandheldLightChanged every frame while it is on; Atmosphere
    /// owns the light itself. E-18: a gripped plank follows the length of the drag in any direction
    /// (owner report 2026-10-31: dragging down, towards yourself, pushed the plank and the door never opened).</remarks>
    public sealed class DesktopInteraction : MonoBehaviour
    {
        public Camera view;public bool desktopSimulation=true;private PhysicalPuzzle puzzle;private PhysicsProp prop;private Vector3 gripPoint;private float angle;private bool live;private bool torch;
        private void OnEnable(){GameEvents.StateChanged.Subscribe(State);}
        private void OnDisable(){GameEvents.StateChanged.Unsubscribe(State);Release(false);}
        private void State(GameStateChanged state){live=GameStateRules.IsSimulationLive(state.To);if(!live)Release(false);}
        private void Update()
        {
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
            if(!desktopSimulation || XRSettings.isDeviceActive || !live || !view)return;
            FlatInput.Poll();
            if(FlatInput.GrabPressed)Begin();
            if(puzzle && FlatInput.GrabHeld)MovePuzzle(FlatInput.Drag);
            if(FlatInput.GrabReleased)Release(false);
            if(FlatInput.Throw)Release(true);
            FlatInput.PuzzleGripped=puzzle;
            if(FlatInput.Flashlight)torch=!torch;
            if(torch || FlatInput.Flashlight)GameEvents.HandheldLightChanged.Raise(new HandheldLightChanged(torch,view.transform.TransformPoint(new Vector3(.12f,-.1f,.1f)),view.transform.forward));
#endif
}
        private void Begin(){if(!Physics.Raycast(view.transform.position,view.transform.forward,out var hit,1.5f))return;puzzle=hit.collider.GetComponentInParent<PhysicalPuzzle>();if(puzzle){gripPoint=hit.point;angle=0;puzzle.BeginGrip(gripPoint);return;}prop=hit.collider.GetComponentInParent<PhysicsProp>();if(prop)prop.DesktopGrab();}
        private void MovePuzzle(Vector2 delta){if(puzzle.mechanic==PhysicalPuzzle.Mechanic.Valve){angle+=delta.x;var radial=Quaternion.AngleAxis(angle,puzzle.transform.TransformDirection(puzzle.axis))*(gripPoint-puzzle.transform.position);puzzle.SampleGrip(puzzle.transform.position+radial,Time.deltaTime);}else{gripPoint+=puzzle.mechanic==PhysicalPuzzle.Mechanic.Plank?puzzle.transform.TransformDirection(puzzle.axis)*delta.magnitude*.002f:Vector3.down*(-delta.y*.002f);puzzle.SampleGrip(gripPoint,Time.deltaTime);}}
        private void FixedUpdate(){
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
if(prop && prop.Held && !prop.Body.isKinematic){Vector3 target=view.transform.position+view.transform.forward*.65f;prop.Body.linearVelocity=Vector3.ClampMagnitude((target-prop.Body.position)*12,5);}
#endif
}
        private void Release(bool throwing){if(puzzle)puzzle.EndGrip();puzzle=null;
#if PZ_DESKTOP_MOUSE || PZ_TOUCH
            FlatInput.PuzzleGripped=false;
#endif
if(prop)prop.DesktopRelease(throwing?view.transform.forward*5:Vector3.zero);prop=null;}
    }
}
