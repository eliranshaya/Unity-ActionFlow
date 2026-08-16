using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    [Serializable]
    public class ActionFlowImageColor : ActionFlow
    {
        public enum ModeType
        {
            Instant,
            Lerp,
            ToDestination,
        }

        #region Variables

        [InspectorGroup("Image", 12, false)]
        [Tooltip("The Image to control")]
        public Image TargetImage;

        [Tooltip("The mode this action should operate on\n" +
                 "Instant : immediately sets the color\n" +
                 "Lerp : animates between two colors over time\n" +
                 "ToDestination : animates from the current color to a destination color")]
        public ModeType Mode = ModeType.Instant;

        [Condition("Mode", "Instant")]
        [Tooltip("The color to set instantly")]
        public Color InstantColor = Color.white;

        [Condition("Mode", "Lerp")]
        [Tooltip("The color to start from")]
        public Color FromColor = Color.white;

        [Condition("Mode", "Lerp")]
        [Tooltip("The color to animate to")]
        public Color ToColor = Color.black;

        [Condition("Mode", "ToDestination")]
        [Tooltip("The destination color to animate to from the current color")]
        public Color DestinationColor = Color.white;

        [Condition("Mode", new[] { "Lerp", "ToDestination" })]
        [Header("Duration")]
        public DurationMode DurationMode;

        [Condition("Mode", new[] { "Lerp", "ToDestination" })]
        [Tooltip("The curve controlling the color animation")]
        public AnimationCurve Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        #endregion

        public override float GetDuration()
        {
            if (Mode == ModeType.Instant) return 0f;
            return Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0f;
        }
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Control the color of a UI Image — set instantly, lerp between two colors, or animate from current to a destination color";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (TargetImage == null)
                return "⚠ Target Image is not assigned. Please assign a UI Image component.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetImage == null) return UniTask.CompletedTask;

            switch (Mode)
            {
                case ModeType.Instant:
                    TargetImage.color = InstantColor;
                    break;

                case ModeType.Lerp:
                    return AnimateLerp(cancellationToken);

                case ModeType.ToDestination:
                    return AnimateToDestination(cancellationToken);
            }

            return UniTask.CompletedTask;
        }

        private async UniTask AnimateLerp(CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                TargetImage.color = ToColor;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);
                float curveValue = Curve.Evaluate(percent);
                TargetImage.color = Color.LerpUnclamped(FromColor, ToColor, curveValue);
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            TargetImage.color = Color.LerpUnclamped(FromColor, ToColor, Curve.Evaluate(1f));
        }

        private async UniTask AnimateToDestination(CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            Color initialColor = TargetImage.color;

            if (duration <= 0f)
            {
                TargetImage.color = DestinationColor;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);
                float curveValue = Curve.Evaluate(percent);
                TargetImage.color = Color.LerpUnclamped(initialColor, DestinationColor, curveValue);
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            TargetImage.color = Color.LerpUnclamped(initialColor, DestinationColor, Curve.Evaluate(1f));
        }
    }
}