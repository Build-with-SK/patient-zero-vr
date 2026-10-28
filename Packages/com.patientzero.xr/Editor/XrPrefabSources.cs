using UnityEngine;

namespace PatientZero.XR.Editor
{
    [CreateAssetMenu(menuName = "Patient Zero/SDK Prefab Sources")]
    public sealed class XrPrefabSources : ScriptableObject
    {
        [Tooltip("SDK comprehensive rig prefab, with hands AND controller fallback already configured.")]
        public GameObject Rig;
        [Tooltip("Select SDK interactor components from the rig prefab asset. Include aggregate interactor groups.")]
        public Behaviour[] LeftHand = new Behaviour[0], RightHand = new Behaviour[0];
        public Behaviour[] LeftController = new Behaviour[0], RightController = new Behaviour[0];
        [Tooltip("SDK prefabs with HandGrabInteractable, controller grab and SDK throw/transformer configuration.")]
        public GameObject Throwable, TwoHanded, Valve, Lever;
        public Transform ValveMovingPart, LeverMovingPart;
        public Vector3 ValveAxis = Vector3.forward, LeverAxis = Vector3.right;
        public float ValveMin = -170, ValveMax = 170, LeverMin = 0, LeverMax = 90;
    }
}
