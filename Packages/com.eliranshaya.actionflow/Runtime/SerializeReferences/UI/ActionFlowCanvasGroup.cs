using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowCanvasGroup : ActionFlow
    {
        #region Variables

        public enum CanvasGroupAnimationMode
        {
            SetAlpha,
            LerpAlpha,
            SetInteractable,
        }

        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[] { "LerpAlpha" })]
        public DurationMode DurationMode;

        [InspectorGroup("Canvas Group", 12, false)]
        [Tooltip("The CanvasGroup component to animate")]
        public CanvasGroup TargetCanvasGroup;

        [Tooltip("The animation mode to apply")]
        public CanvasGroupAnimationMode AnimationMode = CanvasGroupAnimationMode.LerpAlpha;

        [Condition("AnimationMode", "SetAlpha")]
        [Tooltip("The alpha value to set immediately (0 = transparent, 1 = opaque)")]
        [Range(0f, 1f)] public float AlphaValue = 1f;

        [Condition("AnimationMode", "LerpAlpha")]
        [Tooltip("The alpha value to start from")]
        [Range(0f, 1f)] public float AlphaFrom = 0f;

        [Condition("AnimationMode", "LerpAlpha")]
        [Tooltip("The alpha value to end at")]
        [Range(0f, 1f)] public float AlphaTo = 1f;

        [Condition("AnimationMode", "LerpAlpha")]
        [Tooltip("Easing curve for the alpha animation")]
        public AnimationCurve AlphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Condition("AnimationMode", "SetInteractable")]
        [Tooltip("The interactable state to set")]
        public bool Interactable = true;

        [Condition("AnimationMode", "SetInteractable")]
        [Tooltip("Also sets blocksRaycasts to match the interactable value")]
        public bool AlsoSetBlocksRaycasts = true;

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Set or animate alpha and interactable state on a CanvasGroup component";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (TargetCanvasGroup == null)
                return "⚠ Target CanvasGroup is not assigned.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetCanvasGroup == null) yield break;

            switch (AnimationMode)
            {
                case CanvasGroupAnimationMode.SetAlpha: yield return DoSetAlpha(); break;
                case CanvasGroupAnimationMode.LerpAlpha: yield return DoLerpAlpha(); break;
                case CanvasGroupAnimationMode.SetInteractable: yield return DoSetInteractable(); break;
            }
        }

        private IEnumerator DoSetAlpha()
        {
            TargetCanvasGroup.alpha = AlphaValue;
            yield break;
        }

        private IEnumerator DoLerpAlpha()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            TargetCanvasGroup.alpha = AlphaFrom;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = AlphaCurve.Evaluate(t);
                TargetCanvasGroup.alpha = Mathf.LerpUnclamped(AlphaFrom, AlphaTo, curvedT);
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            TargetCanvasGroup.alpha = AlphaTo;
        }

        private IEnumerator DoSetInteractable()
        {
            TargetCanvasGroup.interactable = Interactable;
            if (AlsoSetBlocksRaycasts) TargetCanvasGroup.blocksRaycasts = Interactable;
            yield break;
        }
    }
}