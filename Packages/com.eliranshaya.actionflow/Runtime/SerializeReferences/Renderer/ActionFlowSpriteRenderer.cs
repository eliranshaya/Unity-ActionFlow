using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowSpriteRenderer : ActionFlow
    {
        #region Variables

        public enum SpriteAnimationMode
        {
            SetColor,
            LerpColor,
            SetAlpha,
            LerpAlpha,
        }

        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[]
        {
            "LerpColor", "LerpAlpha"
        })]
        public DurationMode DurationMode;

        [InspectorGroup("Sprite", 12, false)] [Tooltip("The SpriteRenderer whose color will be animated")] public SpriteRenderer TargetRenderer;

        [Tooltip("The animation mode to apply to the sprite")] public SpriteAnimationMode AnimationMode = SpriteAnimationMode.SetColor;

        [Condition("AnimationMode", "SetColor")] [Tooltip("The color value to set immediately")] public Color ColorValue = Color.white;

        [Condition("AnimationMode", "LerpColor")] [Tooltip("Start color for color lerp")] public Color ColorFrom = Color.white;

        [Condition("AnimationMode", "LerpColor")] [Tooltip("End color for color lerp")] public Color ColorTo = Color.white;

        [Condition("AnimationMode", "LerpColor")] [Tooltip("Optional easing curve for color lerp")] public AnimationCurve ColorCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Condition("AnimationMode", "SetAlpha")] [Tooltip("The alpha value to set immediately (0 = transparent, 1 = opaque)")] [Range(0f, 1f)] public float AlphaValue = 1f;

        [Condition("AnimationMode", "LerpAlpha")] [Tooltip("Start alpha for alpha lerp")] [Range(0f, 1f)] public float AlphaFrom = 0f;

        [Condition("AnimationMode", "LerpAlpha")] [Tooltip("End alpha for alpha lerp")] [Range(0f, 1f)] public float AlphaTo = 1f;

        [Condition("AnimationMode", "LerpAlpha")] [Tooltip("Optional easing curve for alpha lerp")] public AnimationCurve AlphaCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("If true, restores the original color when the action finishes")] public bool RestoreOnComplete = false;

        #endregion

        public override float GetDuration() => (Timing != null && Timing.WaitForCompletion) ? DurationMode.GetDurationUI() : 0;

#if UNITY_EDITOR

        public override string GetDescription() => "Animate or set the color/alpha of a SpriteRenderer";

        public override string GetCategory() => "Renderer";

        public override string GetWarningMessage()
        {
            if (TargetRenderer == null)
                return "⚠ Target SpriteRenderer is not assigned.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetRenderer == null) return UniTask.CompletedTask;

            switch (AnimationMode)
            {
                case SpriteAnimationMode.SetColor: DoSetColor(); break;
                case SpriteAnimationMode.LerpColor: return DoLerpColor(cancellationToken);
                case SpriteAnimationMode.SetAlpha: DoSetAlpha(); break;
                case SpriteAnimationMode.LerpAlpha: return DoLerpAlpha(cancellationToken);
            }

            return UniTask.CompletedTask;
        }

        private void DoSetColor()
        {
            Color original = TargetRenderer.color;
            TargetRenderer.color = ColorValue;

            if (RestoreOnComplete) TargetRenderer.color = original;
        }

        private void DoSetAlpha()
        {
            Color original = TargetRenderer.color;

            Color c = original;
            c.a = AlphaValue;
            TargetRenderer.color = c;

            if (RestoreOnComplete) TargetRenderer.color = original;
        }

        private async UniTask DoLerpColor(CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            Color original = TargetRenderer.color;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = ColorCurve.Evaluate(t);

                TargetRenderer.color = Color.LerpUnclamped(ColorFrom, ColorTo, curvedT);

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            TargetRenderer.color = RestoreOnComplete ? original : Color.LerpUnclamped(ColorFrom, ColorTo, ColorCurve.Evaluate(1f));
        }

        private async UniTask DoLerpAlpha(CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            Color original = TargetRenderer.color;
            Color working = original;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = AlphaCurve.Evaluate(t);

                working.a = Mathf.LerpUnclamped(AlphaFrom, AlphaTo, curvedT);
                TargetRenderer.color = working;

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            if (RestoreOnComplete)
            {
                TargetRenderer.color = original;
            }
            else
            {
                working.a = Mathf.LerpUnclamped(AlphaFrom, AlphaTo, AlphaCurve.Evaluate(1f));
                TargetRenderer.color = working;
            }
        }
    }
}