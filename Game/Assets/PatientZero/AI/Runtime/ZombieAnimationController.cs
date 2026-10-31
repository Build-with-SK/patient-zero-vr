// UNVERIFIED IN EDITOR
using PatientZero.AI.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI
{
    /// <summary>Generated mannequin shamble with optional Animator adapter. Never affects the player camera.</summary>
    public sealed class ZombieAnimationController : MonoBehaviour
    {
        public Transform Body, Head, LeftLeg, RightLeg, LeftArm, RightArm, FootEmitter;
        public Animator Animator;
        public SkinnedMeshRenderer Visual;
        private Vector3 bodyStart, leftFootStart, rightFootStart, target;
        private float speed, lungeUntil;
        private bool kneel;
        private AlertLevel level;
        private ZombieMode mode;
        private static readonly int StateParameter = UnityEngine.Animator.StringToHash("AlertLevel");
        private static readonly int SpeedParameter = UnityEngine.Animator.StringToHash("Speed");
        private void Awake()
        {
            if (Body) bodyStart = Body.localPosition;
            if (LeftLeg) leftFootStart = LeftLeg.localPosition; if (RightLeg) rightFootStart = RightLeg.localPosition;
            if (Animator) Animator.applyRootMotion = false;
        }
        public void Apply(AlertLevel alert, ZombieMode zombieMode, float motionSpeed, Vector3 lookAt)
        { level = alert; mode = zombieMode; speed = motionSpeed; target = lookAt; }
        public void SetVisible(bool visible) { if (Visual) Visual.enabled = visible; else if (Body) Body.gameObject.SetActive(visible); }
        public void ResetPose() { kneel = false; lungeUntil = 0; speed = 0; level = AlertLevel.Unaware; mode = ZombieMode.Dormant; }
        public void Lunge() => lungeUntil = Time.time + .5f;
        public void Kneel() => kneel = true;
        private void Update()
        {
            float phase = Time.time * (speed > .05f ? 5 : 1.2f), sway = Mathf.Sin(phase);
            if (Body)
            {
                Vector3 offset = Vector3.up * (kneel ? -.5f : .025f * sway);
                if (mode == ZombieMode.Restrained && Time.time < lungeUntil)
                { Vector3 toward = target - transform.position; toward.y = 0; offset += toward.normalized * .55f; }
                Body.localPosition = bodyStart + offset;
                Body.localRotation = Quaternion.Euler(kneel ? 25 : 4, 0, sway * 3);
            }
            if (Head) Head.localRotation = Quaternion.Euler(level == AlertLevel.Searching ? -10 : 10, Mathf.Sin(phase * .5f) * 15, -8);
            if (LeftLeg) LeftLeg.localPosition = leftFootStart + Vector3.forward * (speed > .05f ? sway * .12f : 0);
            if (RightLeg) RightLeg.localPosition = rightFootStart - Vector3.forward * (speed > .05f ? sway * .12f : 0);
            if (FootEmitter && LeftLeg && RightLeg) FootEmitter.position = sway >= 0 ? LeftLeg.position : RightLeg.position;
            if (LeftArm) LeftArm.localRotation = Quaternion.Euler(15 + sway * 5, 0, 8);
            if (RightArm) RightArm.localRotation = Quaternion.Euler(15 - sway * 5, 0, -8);
            if (Animator) { Animator.SetInteger(StateParameter, (int)level); Animator.SetFloat(SpeedParameter, speed); }
        }
    }
}
