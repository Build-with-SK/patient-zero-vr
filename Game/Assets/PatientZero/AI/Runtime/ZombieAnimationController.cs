// UNVERIFIED IN EDITOR
using PatientZero.AI.Logic;
using PatientZero.Core;
using UnityEngine;

namespace PatientZero.AI
{
    /// <summary>Generated mannequin shamble with optional Animator adapter. Never affects the player camera.</summary>
    /// <remarks>E-25 (Claude integration, the revolver): Hit plays the model's HitReact, Die its Death; a dead zombie holds still.</remarks>
    public sealed class ZombieAnimationController : MonoBehaviour
    {
        public Transform Body, Head, LeftLeg, RightLeg, LeftArm, RightArm, FootEmitter;
        public Animator Animator;
        public SkinnedMeshRenderer Visual;
        private Vector3 bodyStart, leftFootStart, rightFootStart, target, restraintTarget;
        private float speed, lungeUntil;
        private bool kneel, dead;
        private AlertLevel level;
        private ZombieMode mode;
        private int motionSeed;
        private float walkSpeed = .9f;
        private bool stunned, offsetsApplied, listening;
        private ZombieMotion motion;
        private Animator boneAnimator;
        private Transform humanHead, chest, shoulder;
        private Quaternion headPose, chestPose, shoulderPose;
        private float animatorSpeed;
        public bool IsHumanoid => Animator != null && Animator.isHuman;
        private static readonly int StateParameter = UnityEngine.Animator.StringToHash("AlertLevel");
        private static readonly int SpeedParameter = UnityEngine.Animator.StringToHash("Speed");
        private static readonly int HitTrigger = UnityEngine.Animator.StringToHash("Hit"), DieTrigger = UnityEngine.Animator.StringToHash("Die");
        private static readonly int LocomotionState = UnityEngine.Animator.StringToHash("Locomotion");
        private void Awake()
        {
            if (Body) bodyStart = Body.localPosition;
            if (LeftLeg) leftFootStart = LeftLeg.localPosition; if (RightLeg) rightFootStart = RightLeg.localPosition;
            if (Animator) Animator.applyRootMotion = false;
        }
        public void InitializeMotion(int zombieId) { motionSeed = zombieId; }
        public void Apply(AlertLevel alert, ZombieMode zombieMode, float motionSpeed, Vector3 lookAt,
            float walkingSpeed = .9f, bool isStunned = false)
        { level = alert; mode = zombieMode; speed = motionSpeed; target = lookAt; walkSpeed = walkingSpeed; stunned = isStunned; }
        // The old Generic mannequin's director-scripted restraint lunge is separate from its look target.
        public void SetRestraintLungeTarget(Vector3 value) { restraintTarget = value; }
        public void SetStunned(bool value) { stunned = value; }
        public void SetVisible(bool visible) { if (Visual) Visual.enabled = visible; else if (Body) Body.gameObject.SetActive(visible); }
        public void ResetPose()
        {
            RestoreOffsets(); ReleaseListening(); motion?.Reset(); stunned = false;
            kneel = false; lungeUntil = 0; speed = 0; level = AlertLevel.Unaware; mode = ZombieMode.Dormant;
            if (dead && Animator) { Animator.ResetTrigger(DieTrigger); Animator.Play(LocomotionState, 0, 0); }
            dead = false;
        }
        public void Hit()
        {
            if (IsHumanoid && !dead) { stunned = true; RestoreOffsets(); ReleaseListening(); }
            if (Animator && !dead) Animator.SetTrigger(HitTrigger);
        }
        public void Die()
        {
            RestoreOffsets(); ReleaseListening();
            dead = true; speed = 0;
            if (Body) { Body.localPosition = bodyStart; Body.localRotation = Quaternion.identity; }
            if (Animator) { Animator.ResetTrigger(HitTrigger); Animator.SetFloat(SpeedParameter, 0); Animator.SetTrigger(DieTrigger); }
        }
        public void Lunge() => lungeUntil = Time.time + .5f;
        public void Kneel() => kneel = true;
        private void Update()
        {
            RestoreOffsets(); // before the Animator evaluates: never accumulate our additive offsets, even on unkeyed bones
            if (dead) return;
            if (IsHumanoid)
            {
                CacheHumanBones();
                bool freeze = ZombieMotion.IsListening(level, mode, stunned, dead, kneel);
                if (freeze && !listening) { animatorSpeed = Animator.speed; listening = true; Animator.speed = 0; }
                else if (!freeze) ReleaseListening();
                Animator.SetInteger(StateParameter, (int)level);
                Animator.SetFloat(SpeedParameter, speed);
                return;
            }
            ReleaseListening();
            float phase = Time.time * (speed > .05f ? 5 : 1.2f), sway = Mathf.Sin(phase);
            if (Body)
            {
                Vector3 offset = Vector3.up * (kneel ? -.5f : .025f * sway);
                if (mode == ZombieMode.Restrained && Time.time < lungeUntil)
                { Vector3 toward = restraintTarget - transform.position; toward.y = 0; offset += toward.normalized * .55f; }
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

        private void CacheHumanBones()
        {
            if (boneAnimator == Animator) return;
            ReleaseListening(); boneAnimator = Animator;
            humanHead = Animator.GetBoneTransform(HumanBodyBones.Head);
            chest = Animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (!chest) chest = Animator.GetBoneTransform(HumanBodyBones.Chest);
            if (!chest) chest = Animator.GetBoneTransform(HumanBodyBones.Spine);
            shoulder = Animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            if (!shoulder) shoulder = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            motion = new ZombieMotion(motionSeed);
        }

        private void LateUpdate()
        {
            if (!IsHumanoid || dead) return;
            CacheHumanBones();
            Vector3 direction = transform.InverseTransformDirection(target - (humanHead ? humanHead.position : transform.position));
            float yaw = 0, pitch = 0;
            if (direction.sqrMagnitude > .0001f)
            {
                yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                pitch = -Mathf.Atan2(direction.y, Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z)) * Mathf.Rad2Deg;
            }
            var pose = motion.Step(Time.deltaTime, level, mode, speed, walkSpeed, yaw, pitch,
                stunned, dead, kneel);
            if (humanHead) headPose = humanHead.localRotation;
            if (chest) chestPose = chest.localRotation;
            if (shoulder) shoulderPose = shoulder.localRotation;
            // Rotate about the actor's axes, not imported bone-local axes (Humanoid rigs vary).
            if (chest) chest.rotation = Quaternion.AngleAxis(pose.Lean, transform.right) * chest.rotation;
            if (shoulder) shoulder.rotation = Quaternion.AngleAxis(pose.ShoulderTwitch, transform.forward) * shoulder.rotation;
            if (humanHead)
            {
                // Cancel the chest lean at the head before adding the eased, clamped look.
                Quaternion look = Quaternion.AngleAxis(pose.Yaw, transform.up) *
                    Quaternion.AngleAxis(Mathf.Clamp(pose.Pitch + pose.HeadTwitch, -ZombieMotion.MaxPitch, ZombieMotion.MaxPitch), transform.right) *
                    Quaternion.AngleAxis(pose.Roll, transform.forward);
                humanHead.rotation = look * Quaternion.AngleAxis(-pose.Lean, transform.right) * humanHead.rotation;
            }
            offsetsApplied = true;
        }

        private void RestoreOffsets()
        {
            if (!offsetsApplied) return;
            if (humanHead) humanHead.localRotation = headPose;
            if (chest) chest.localRotation = chestPose;
            if (shoulder) shoulder.localRotation = shoulderPose;
            offsetsApplied = false;
        }
        private void ReleaseListening()
        {
            if (!listening) return;
            if (boneAnimator) boneAnimator.speed = animatorSpeed;
            listening = false;
        }
        private void OnDisable() { RestoreOffsets(); ReleaseListening(); }
    }
}
