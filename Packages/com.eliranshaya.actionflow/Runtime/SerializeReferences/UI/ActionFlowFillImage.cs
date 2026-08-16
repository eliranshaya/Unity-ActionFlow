using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    [Serializable]
    public class ActionFlowFillImage : ActionFlow
    {
        #region Variables

        public enum FillImageAnimationMode
        {
            SetFill,
            LerpFill,
        }

        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[] { "LerpFill" })]
        public DurationMode DurationMode;

        [InspectorGroup("Fill Image", 12, false)]
        [Tooltip("The Image component to animate the fill on")]
        public Image TargetImage;

        [Tooltip("The animation mode to apply")]
        public FillImageAnimationMode AnimationMode = FillImageAnimationMode.LerpFill;

        [Condition("AnimationMode", "SetFill")]
        [Tooltip("The fill amount to set immediately (0 = empty, 1 = full)")]
        [Range(0f, 1f)] public float FillValue = 1f;

        [Condition("AnimationMode", "LerpFill")]
        [Tooltip("The fill amount to start from")]
        [Range(0f, 1f)] public float FillFrom = 0f;

        [Condition("AnimationMode", "LerpFill")]
        [Tooltip("The fill amount to end at")]
        [Range(0f, 1f)] public float FillTo = 1f;

        [Condition("AnimationMode", "LerpFill")]
        [Tooltip("Easing curve for the fill animation")]
        public AnimationCurve FillCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("If true, restores the original fill amount when the action finishes")]
        public bool RestoreOnComplete = false;

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Animate the fillAmount of a UI Image component";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (TargetImage == null)
                return "⚠ Target Image is not assigned.";

            if (TargetImage != null && TargetImage.type != Image.Type.Filled)
                return "⚠ Image Type is not set to 'Filled'. Change it in the Image component.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetImage == null) return UniTask.CompletedTask;

            switch (AnimationMode)
            {
                case FillImageAnimationMode.SetFill: DoSetFill(); break;
                case FillImageAnimationMode.LerpFill: return DoLerpFill(cancellationToken);
            }

            return UniTask.CompletedTask;
        }

        private void DoSetFill()
        {
            float original = RestoreOnComplete ? TargetImage.fillAmount : 0f;
            TargetImage.fillAmount = FillValue;
            if (RestoreOnComplete) TargetImage.fillAmount = original;
        }

        private async UniTask DoLerpFill(CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            float original = RestoreOnComplete ? TargetImage.fillAmount : 0f;

            TargetImage.fillAmount = FillFrom;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = FillCurve.Evaluate(t);

                TargetImage.fillAmount = Mathf.LerpUnclamped(FillFrom, FillTo, curvedT);

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            TargetImage.fillAmount = RestoreOnComplete ? original : FillTo;
        }
    }
}