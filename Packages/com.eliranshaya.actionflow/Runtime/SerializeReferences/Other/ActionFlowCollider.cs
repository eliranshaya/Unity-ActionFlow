using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowCollider : ActionFlow
    {
        #region Variables

        public enum ColliderDimension
        {
            Collider3D,
            Collider2D,
        }

        public enum ColliderAction
        {
            Enable,
            Disable,
            Toggle,
        }

        [InspectorGroup("Collider", 12, false)]
        [Tooltip("Whether to target a 3D or 2D collider")]
        public ColliderDimension Dimension = ColliderDimension.Collider3D;

        [Condition("Dimension", "Collider3D")]
        [Tooltip("The 3D Collider to control")]
        public Collider TargetCollider;

        [Condition("Dimension", "Collider2D")]
        [Tooltip("The 2D Collider to control")]
        public Collider2D TargetCollider2D;

        [InspectorGroup("Enabled", 12, false)]
        [Tooltip("If true, changes the enabled state of the collider")]
        public bool ChangeEnabled = true;

        [Condition("ChangeEnabled", true)]
        [Tooltip("The action to perform on the collider")]
        public ColliderAction Action = ColliderAction.Enable;

        [InspectorGroup("Is Trigger", 12, false)]
        [Tooltip("If true, changes the isTrigger state of the collider")]
        public bool ChangeIsTrigger = false;

        [Condition("ChangeIsTrigger", true)]
        [Tooltip("The isTrigger value to set")]
        public bool IsTrigger = false;

        [InspectorGroup("Material", 12, false)]
        [Tooltip("If true, changes the physics material of the collider")]
        public bool ChangeMaterial = false;

        [Condition("ChangeMaterial", "Dimension", "Collider3D")]
        [Tooltip("The 3D physics material to assign")]
#if UNITY_6000_0_OR_NEWER
        public PhysicsMaterial Material3D;
#else
        public PhysicMaterial Material3D;
#endif

        [Condition("ChangeMaterial", "Dimension", "Collider2D")]
        [Tooltip("The 2D physics material to assign")]
        public PhysicsMaterial2D Material2D;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Enable, Disable, or Toggle a 3D or 2D Collider — also supports changing isTrigger and physics material";

        public override string GetCategory() => "Physics";

        public override string GetWarningMessage()
        {
            if (Dimension == ColliderDimension.Collider3D && TargetCollider == null)
                return "⚠ Target Collider is not assigned. Please assign a Collider component.";

            if (Dimension == ColliderDimension.Collider2D && TargetCollider2D == null)
                return "⚠ Target Collider2D is not assigned. Please assign a Collider2D component.";

            if (ChangeMaterial && Dimension == ColliderDimension.Collider3D && Material3D == null)
                return "⚠ Physics Material is not assigned. Please assign a PhysicMaterial.";

            if (ChangeMaterial && Dimension == ColliderDimension.Collider2D && Material2D == null)
                return "⚠ Physics Material 2D is not assigned. Please assign a PhysicsMaterial2D.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (Dimension == ColliderDimension.Collider3D)
            {
                if (TargetCollider == null) return UniTask.CompletedTask;

                if (ChangeEnabled)
                    TargetCollider.enabled = Action switch
                    {
                        ColliderAction.Enable => true,
                        ColliderAction.Disable => false,
                        ColliderAction.Toggle => !TargetCollider.enabled,
                        _ => TargetCollider.enabled
                    };

                if (ChangeIsTrigger)
                    TargetCollider.isTrigger = IsTrigger;

                if (ChangeMaterial && Material3D != null)
                    TargetCollider.material = Material3D;
            }
            else
            {
                if (TargetCollider2D == null) return UniTask.CompletedTask;

                if (ChangeEnabled)
                    TargetCollider2D.enabled = Action switch
                    {
                        ColliderAction.Enable => true,
                        ColliderAction.Disable => false,
                        ColliderAction.Toggle => !TargetCollider2D.enabled,
                        _ => TargetCollider2D.enabled
                    };

                if (ChangeIsTrigger)
                    TargetCollider2D.isTrigger = IsTrigger;

                if (ChangeMaterial && Material2D != null)
                    TargetCollider2D.sharedMaterial = Material2D;
            }

            return UniTask.CompletedTask;
        }
    }
}