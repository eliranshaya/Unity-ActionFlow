using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowAnimationParameter : ActionFlow
    {
        #region Variables

        public enum AnimatorParameterType
        {
            Bool,
            Int,
            Float,
            Trigger,
        }

        [InspectorGroup("Animator", 12, false)]
        [Tooltip("The Animator to control")]
        public Animator TargetAnimator;

        [Tooltip("The name of the parameter to set")]
        public string ParameterName = "";

        [Tooltip("The type of the parameter")]
        public AnimatorParameterType ParameterType = AnimatorParameterType.Bool;

        [Condition("ParameterType", "Bool")]
        [Tooltip("The bool value to set")]
        public bool BoolValue = true;

        [Condition("ParameterType", "Int")]
        [Tooltip("The int value to set")]
        public int IntValue = 0;

        [Condition("ParameterType", "Float")]
        [Tooltip("The float value to set")]
        public float FloatValue = 0f;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Set a parameter on an Animator — Bool, Int, Float, or Trigger";

        public override string GetCategory() => "Animation";

        public override string GetWarningMessage()
        {
            if (TargetAnimator == null)
                return "⚠ Target Animator is not assigned. Please assign an Animator component.";

            if (string.IsNullOrEmpty(ParameterName))
                return "⚠ Parameter Name is empty. Please enter the name of the animator parameter.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetAnimator == null) yield break;
            if (string.IsNullOrEmpty(ParameterName)) yield break;

            Action applyParameter = BuildParameterDelegate();
            applyParameter?.Invoke();
        }

        private Action BuildParameterDelegate()
        {
            switch (ParameterType)
            {
                case AnimatorParameterType.Bool:
                    return () => TargetAnimator.SetBool(ParameterName, BoolValue);

                case AnimatorParameterType.Int:
                    return () => TargetAnimator.SetInteger(ParameterName, IntValue);

                case AnimatorParameterType.Float:
                    return () => TargetAnimator.SetFloat(ParameterName, FloatValue);

                case AnimatorParameterType.Trigger:
                    return () => TargetAnimator.SetTrigger(ParameterName);

                default:
                    return null;
            }
        }
    }
}