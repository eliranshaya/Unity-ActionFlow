using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowRigidBody : ActionFlow
    {
        #region Variables

        public enum RigidbodyDimension
        {
            Rigidbody3D,
            Rigidbody2D,
        }

        [InspectorGroup("Rigidbody", 12, false)]
        [Tooltip("Whether to target a 3D or 2D Rigidbody")]
        public RigidbodyDimension Dimension = RigidbodyDimension.Rigidbody3D;

        [Condition("Dimension", "Rigidbody3D")]
        [Tooltip("The 3D Rigidbody to control")]
        public Rigidbody TargetRigidbody;

        [Condition("Dimension", "Rigidbody2D")]
        [Tooltip("The 2D Rigidbody to control")]
        public Rigidbody2D TargetRigidbody2D;

        [InspectorGroup("Kinematic", 12, false)]
        [Tooltip("If true, changes the isKinematic state")]
        public bool ChangeKinematic = false;

        [Condition("ChangeKinematic", true)]
        [Tooltip("The isKinematic value to set")]
        public bool IsKinematic = false;

        [InspectorGroup("Gravity", 12, false)]
        [Tooltip("If true, changes the gravity state")]
        public bool ChangeGravity = false;

        [Condition("ChangeGravity", true)]
        [Tooltip("The useGravity value to set (3D) or gravityScale to zero/restore (2D)")]
        public bool UseGravity = true;

        [InspectorGroup("Velocity", 12, false)]
        [Tooltip("If true, changes the velocity of the rigidbody")]
        public bool ChangeVelocity = false;

        [Condition("ChangeVelocity", "Dimension", "Rigidbody3D")]
        [Tooltip("The velocity to set on the 3D Rigidbody")]
        public Vector3 Velocity3D = Vector3.zero;

        [Condition("ChangeVelocity", "Dimension", "Rigidbody2D")]
        [Tooltip("The velocity to set on the 2D Rigidbody")]
        public Vector2 Velocity2D = Vector2.zero;

        [InspectorGroup("Force", 12, false)]
        [Tooltip("If true, adds a force to the rigidbody")]
        public bool AddForce = false;

        [Condition("AddForce", "Dimension", "Rigidbody3D")]
        [Tooltip("The force to add to the 3D Rigidbody")]
        public Vector3 Force3D = Vector3.zero;

        [Condition("AddForce", "Dimension", "Rigidbody2D")]
        [Tooltip("The force to add to the 2D Rigidbody")]
        public Vector2 Force2D = Vector2.zero;

        [Condition("AddForce", true)]
        [Tooltip("The force mode to use when adding force")]
        public ForceMode ForceMode = ForceMode.Force;

        [InspectorGroup("Constraints", 12, false)]
        [Tooltip("If true, changes the freeze position constraints")]
        public bool ChangeFreezePosition = false;

        [Condition("ChangeFreezePosition", "Dimension", "Rigidbody3D")]
        [Tooltip("Freeze X position on the 3D Rigidbody")]
        public bool FreezePositionX3D = false;

        [Condition("ChangeFreezePosition", "Dimension", "Rigidbody3D")]
        [Tooltip("Freeze Y position on the 3D Rigidbody")]
        public bool FreezePositionY3D = false;

        [Condition("ChangeFreezePosition", "Dimension", "Rigidbody3D")]
        [Tooltip("Freeze Z position on the 3D Rigidbody")]
        public bool FreezePositionZ3D = false;

        [Condition("ChangeFreezePosition", "Dimension", "Rigidbody2D")]
        [Tooltip("Freeze X position on the 2D Rigidbody")]
        public bool FreezePositionX2D = false;

        [Condition("ChangeFreezePosition", "Dimension", "Rigidbody2D")]
        [Tooltip("Freeze Y position on the 2D Rigidbody")]
        public bool FreezePositionY2D = false;

        [Tooltip("If true, changes the freeze rotation constraints")]
        public bool ChangeFreezeRotation = false;

        [Condition("ChangeFreezeRotation", "Dimension", "Rigidbody3D")]
        [Tooltip("Freeze X rotation on the 3D Rigidbody")]
        public bool FreezeRotationX3D = false;

        [Condition("ChangeFreezeRotation", "Dimension", "Rigidbody3D")]
        [Tooltip("Freeze Y rotation on the 3D Rigidbody")]
        public bool FreezeRotationY3D = false;

        [Condition("ChangeFreezeRotation", "Dimension", "Rigidbody3D")]
        [Tooltip("Freeze Z rotation on the 3D Rigidbody")]
        public bool FreezeRotationZ3D = false;

        [Condition("ChangeFreezeRotation", "Dimension", "Rigidbody2D")]
        [Tooltip("Freeze rotation on the 2D Rigidbody")]
        public bool FreezeRotation2D = false;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Control a 3D or 2D Rigidbody — change kinematic, gravity, velocity, force, and constraints";

        public override string GetCategory() => "Physics";

        public override string GetWarningMessage()
        {
            if (Dimension == RigidbodyDimension.Rigidbody3D && TargetRigidbody == null)
                return "⚠ Target Rigidbody is not assigned. Please assign a Rigidbody component.";

            if (Dimension == RigidbodyDimension.Rigidbody2D && TargetRigidbody2D == null)
                return "⚠ Target Rigidbody2D is not assigned. Please assign a Rigidbody2D component.";

            if (!ChangeKinematic && !ChangeGravity && !ChangeVelocity && !AddForce && !ChangeFreezePosition && !ChangeFreezeRotation)
                return "⚠ No operations are enabled. Please enable at least one group to take effect.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (Dimension == RigidbodyDimension.Rigidbody3D)
            {
                if (TargetRigidbody == null) return UniTask.CompletedTask;
                Apply3D();
            }
            else
            {
                if (TargetRigidbody2D == null) return UniTask.CompletedTask;
                Apply2D();
            }

            return UniTask.CompletedTask;
        }

        private void Apply3D()
        {
            if (ChangeKinematic)
                TargetRigidbody.isKinematic = IsKinematic;

            if (ChangeGravity)
                TargetRigidbody.useGravity = UseGravity;

            if (ChangeVelocity)
#if UNITY_6000_0_OR_NEWER
                TargetRigidbody.linearVelocity = Velocity3D;
#else
                TargetRigidbody.velocity = Velocity3D;
#endif

            if (AddForce)
                TargetRigidbody.AddForce(Force3D, ForceMode);

            if (ChangeFreezePosition || ChangeFreezeRotation)
            {
                RigidbodyConstraints constraints = RigidbodyConstraints.None;

                if (ChangeFreezePosition)
                {
                    if (FreezePositionX3D) constraints |= RigidbodyConstraints.FreezePositionX;
                    if (FreezePositionY3D) constraints |= RigidbodyConstraints.FreezePositionY;
                    if (FreezePositionZ3D) constraints |= RigidbodyConstraints.FreezePositionZ;
                }

                if (ChangeFreezeRotation)
                {
                    if (FreezeRotationX3D) constraints |= RigidbodyConstraints.FreezeRotationX;
                    if (FreezeRotationY3D) constraints |= RigidbodyConstraints.FreezeRotationY;
                    if (FreezeRotationZ3D) constraints |= RigidbodyConstraints.FreezeRotationZ;
                }

                TargetRigidbody.constraints = constraints;
            }
        }

        private void Apply2D()
        {
#if UNITY_6000_0_OR_NEWER

            if (ChangeKinematic)
                TargetRigidbody2D.bodyType = IsKinematic ? RigidbodyType2D.Kinematic :  RigidbodyType2D.Dynamic;
#else
            if (ChangeKinematic)
                TargetRigidbody2D.isKinematic = IsKinematic;
#endif

            if (ChangeGravity)
                TargetRigidbody2D.gravityScale = UseGravity ? 1f : 0f;

            if (ChangeVelocity)
#if UNITY_6000_0_OR_NEWER
                TargetRigidbody2D.linearVelocity = Velocity2D;
#else
                TargetRigidbody2D.velocity = Velocity2D;
#endif

            if (AddForce)
                TargetRigidbody2D.AddForce(Force2D, (ForceMode2D)ForceMode);

            if (ChangeFreezePosition || ChangeFreezeRotation)
            {
                RigidbodyConstraints2D constraints = RigidbodyConstraints2D.None;

                if (ChangeFreezePosition)
                {
                    if (FreezePositionX2D) constraints |= RigidbodyConstraints2D.FreezePositionX;
                    if (FreezePositionY2D) constraints |= RigidbodyConstraints2D.FreezePositionY;
                }

                if (ChangeFreezeRotation)
                {
                    if (FreezeRotation2D) constraints |= RigidbodyConstraints2D.FreezeRotation;
                }

                TargetRigidbody2D.constraints = constraints;
            }
        }
    }
}