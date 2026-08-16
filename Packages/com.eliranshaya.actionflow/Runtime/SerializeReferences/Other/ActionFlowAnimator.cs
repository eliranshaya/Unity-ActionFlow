using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowAnimator : ActionFlow
    {
        public enum AnimatorAction
        {
            Enable,
            Disable,
            SetSpeed,
            SetUpdateMode,
            ApplyRootMotion,
            Rebind,
        }

        #region Variables

        [InspectorGroup("Animator", 12, false)]
        [Tooltip("The Animator to control")]
        public Animator TargetAnimator;

        [Tooltip("The action to perform on the Animator")]
        public AnimatorAction Action = AnimatorAction.Enable;

        [Condition("Action", "SetSpeed")]
        [Tooltip("The speed to set on the Animator")]
        public float Speed = 1f;

        [Condition("Action", "SetUpdateMode")]
        [Tooltip("The update mode to set on the Animator")]
        public AnimatorUpdateMode UpdateMode = AnimatorUpdateMode.Normal;

        [Condition("Action", "ApplyRootMotion")]
        [Tooltip("Whether to apply root motion")]
        public bool ApplyRootMotion = true;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() => "Control an Animator component — enable, disable, set speed, update mode, root motion, or rebind";

        public override string GetCategory() => "Animation";

        public override string GetWarningMessage()
        {
            if (TargetAnimator == null)
                return "⚠ Target Animator is not assigned. Please assign an Animator component.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetAnimator == null) return UniTask.CompletedTask;

            switch (Action)
            {
                case AnimatorAction.Enable:
                    TargetAnimator.enabled = true;
                    break;

                case AnimatorAction.Disable:
                    TargetAnimator.enabled = false;
                    break;

                case AnimatorAction.SetSpeed:
                    TargetAnimator.speed = Speed;
                    break;

                case AnimatorAction.SetUpdateMode:
                    TargetAnimator.updateMode = UpdateMode;
                    break;

                case AnimatorAction.ApplyRootMotion:
                    TargetAnimator.applyRootMotion = ApplyRootMotion;
                    break;

                case AnimatorAction.Rebind:
                    TargetAnimator.Rebind();
                    break;
            }

            return UniTask.CompletedTask;
        }
    }
}