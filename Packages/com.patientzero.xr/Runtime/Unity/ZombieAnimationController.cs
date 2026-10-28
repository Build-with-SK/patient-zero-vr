using PatientZero.Core;
using UnityEngine;

namespace PatientZero.XR
{
    [RequireComponent(typeof(ZombieStateMachine))]
    public sealed class ZombieAnimationController : MonoBehaviour
    {
        public Animator Animator;
        public SpatialAudioSystem Audio;
        public Transform LeftFoot, RightFoot, Mouth;
        private ZombieStateMachine machine;
        private ZombieNavigation navigation;
        private static readonly int StateId = UnityEngine.Animator.StringToHash("State");
        private static readonly int SpeedId = UnityEngine.Animator.StringToHash("Speed");
        private void Awake()
        {
            machine = GetComponent<ZombieStateMachine>(); navigation = GetComponent<ZombieNavigation>();
        }
        private void OnEnable() => machine.OnStateChanged.AddListener(SetState);
        private void OnDisable() => machine.OnStateChanged.RemoveListener(SetState);
        private void Start() => SetState((int)machine.Brain.State);
        private void SetState(int state) { if (Animator) Animator.SetInteger(StateId, state); }
        private void Update()
        {
            if (Animator && navigation.Agent) Animator.SetFloat(SpeedId, navigation.Agent.velocity.magnitude);
        }
        // Animation events; zombie sounds do not feed hearing and cause feedback loops.
        public void FootLeft() { if (Audio && LeftFoot) Audio.Play(AudioCue.FootLeft, LeftFoot.position); }
        public void FootRight() { if (Audio && RightFoot) Audio.Play(AudioCue.FootRight, RightFoot.position); }
        public void Growl() { if (Audio && Mouth) Audio.Play(AudioCue.Growl, Mouth.position); }
    }
}
