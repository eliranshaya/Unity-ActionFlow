using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowRectPosition : ActionFlow
    {
        public enum ModeType
        {
            Absolute,
            Additive,
            ToDestination
        }

        [InspectorGroup("Duration", 12, false)]
        public DurationMode DurationMode;

        [InspectorGroup("Position Mode", 12, false)]
        [Tooltip("the mode this feedback should operate on\n" +
                 "Absolute : follows the curve between start and end values\n" +
                 "Additive : adds the additive position multiplied by the curve to the current position\n" +
                 "ToDestination : animates from current position to the destination position")]
        public ModeType Mode = ModeType.Absolute;

        [Condition("Mode", "Absolute")]
        [Tooltip("the starting anchoredPosition value (for Absolute mode)")]
        public Vector2 StartPosition = Vector2.zero;

        [Condition("Mode", "Absolute")]
        [Tooltip("the ending anchoredPosition value (for Absolute mode)")]
        public Vector2 EndPosition = new Vector2(100f, 100f);

        [Condition("Mode", "Additive")]
        [Tooltip("the position to add (multiplied by the curve) when in Additive mode")]
        public Vector2 AdditivePosition = new Vector2(100f, 100f);

        [Condition("Mode", "ToDestination")]
        [Tooltip("the destination position to animate to (for ToDestination mode)")]
        public Vector2 DestinationPosition = new Vector2(100f, 100f);

        [Tooltip("the RectTransform to animate")]
        public RectTransform AnimatePositionTarget;

        [InspectorGroup("Axis Control", 14, true)]
        [Tooltip("if this is true, the AnimateX curve only will be used, and applied to all axes")]
        public bool UniformPositioning = false;

        [Tooltip("if this is true, should animate the X position value")]
        public bool AnimateX = true;

        [Tooltip("the x position animation definition")]
        public AnimationCurve AnimatePositionX = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformPositioning", false)]
        [Tooltip("if this is true, should animate the Y position value")]
        public bool AnimateY = true;

        [Condition("UniformPositioning", false)]
        [Tooltip("the y position animation definition")]
        public AnimationCurve AnimatePositionY = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        private Vector2 _initialPosition;

#if UNITY_EDITOR
        public override float GetDuration()
        {
            return Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
        }

        public override string GetDescription()
        {
            return "Animate the anchoredPosition of a RectTransform using animation curves with uniform or per-axis control in different modes (Absolute, Additive, ToDestination)";
        }

        public override string GetCategory()
        {
            return "UI";
        }

        public override string GetWarningMessage()
        {
            if (AnimatePositionTarget == null)
                return "⚠ Animate Position Target is not assigned. Please assign a RectTransform to animate.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (AnimatePositionTarget == null)
            {
                return UniTask.CompletedTask;
            }

            _initialPosition = AnimatePositionTarget.anchoredPosition;

            switch (Mode)
            {
                case ModeType.Absolute:
                    return AnimateAbsolute(cancellationToken);
                case ModeType.Additive:
                    return AnimateAdditive(cancellationToken);
                case ModeType.ToDestination:
                    return AnimateToDestination(cancellationToken);
                default:
                    return AnimateAbsolute(cancellationToken);
            }
        }

        private async UniTask AnimateAbsolute(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                AnimatePositionTarget.anchoredPosition = EndPosition;
                return;
            }

            float elapsed = 0f;
            Vector2 newPosition = StartPosition;

            Action<float> applyPosition = null;

            if (UniformPositioning)
            {
                applyPosition += (percent) =>
                {
                    float curveValue = AnimatePositionX.Evaluate(percent);
                    newPosition.x = Mathf.LerpUnclamped(StartPosition.x, EndPosition.x, curveValue);
                    newPosition.y = Mathf.LerpUnclamped(StartPosition.y, EndPosition.y, curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionX.Evaluate(percent);
                        newPosition.x = Mathf.LerpUnclamped(StartPosition.x, EndPosition.x, curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionY.Evaluate(percent);
                        newPosition.y = Mathf.LerpUnclamped(StartPosition.y, EndPosition.y, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);

                applyPosition?.Invoke(percent);
                AnimatePositionTarget.anchoredPosition = newPosition;

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyPosition?.Invoke(1f);
            AnimatePositionTarget.anchoredPosition = newPosition;
        }

        private async UniTask AnimateAdditive(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                return;
            }

            float elapsed = 0f;
            Vector2 newPosition = _initialPosition;

            Action<float> applyPosition = null;

            if (UniformPositioning)
            {
                applyPosition += (percent) =>
                {
                    float curveValue = AnimatePositionX.Evaluate(percent);
                    newPosition.x = _initialPosition.x + (AdditivePosition.x * curveValue);
                    newPosition.y = _initialPosition.y + (AdditivePosition.x * curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionX.Evaluate(percent);
                        newPosition.x = _initialPosition.x + (AdditivePosition.x * curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionY.Evaluate(percent);
                        newPosition.y = _initialPosition.y + (AdditivePosition.y * curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                newPosition = _initialPosition;

                float percent = Mathf.Clamp01(elapsed / duration);

                applyPosition?.Invoke(percent);
                AnimatePositionTarget.anchoredPosition = newPosition;

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            newPosition = _initialPosition;
            applyPosition?.Invoke(1f);
            AnimatePositionTarget.anchoredPosition = newPosition;
        }

        private async UniTask AnimateToDestination(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                AnimatePositionTarget.anchoredPosition = DestinationPosition;
                return;
            }

            float elapsed = 0f;
            Vector2 newPosition = _initialPosition;

            Action<float> applyPosition = null;

            if (UniformPositioning)
            {
                applyPosition += (percent) =>
                {
                    float curveValue = AnimatePositionX.Evaluate(percent);
                    newPosition.x = Mathf.LerpUnclamped(_initialPosition.x, DestinationPosition.x, curveValue);
                    newPosition.y = Mathf.LerpUnclamped(_initialPosition.y, DestinationPosition.y, curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionX.Evaluate(percent);
                        newPosition.x = Mathf.LerpUnclamped(_initialPosition.x, DestinationPosition.x, curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionY.Evaluate(percent);
                        newPosition.y = Mathf.LerpUnclamped(_initialPosition.y, DestinationPosition.y, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);

                applyPosition?.Invoke(percent);
                AnimatePositionTarget.anchoredPosition = newPosition;

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyPosition?.Invoke(1f);
            AnimatePositionTarget.anchoredPosition = newPosition;
        }
    }
}