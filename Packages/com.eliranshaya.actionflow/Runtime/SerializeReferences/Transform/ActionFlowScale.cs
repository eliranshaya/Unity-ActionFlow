using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowScale : ActionFlow
    {
        public enum ModeType
        {
            Absolute,
            Additive,
            ToDestination
        }
        [InspectorGroup("Scale Mode", 12, false)]
        [Header("Duration")]
        public DurationMode DurationMode;

        [Header("Scale")]
        [Tooltip("the mode this feedback should operate on\n" +
                 "Absolute : follows the curve between start and end values\n" +
                 "Additive : adds the additive scale multiplied by the curve to the current scale\n" +
                 "ToDestination : animates from current scale to the destination scale")]
        public ModeType Mode = ModeType.Absolute;

        [Condition("Mode", "Absolute")]
        [Tooltip("the starting scale value (for Absolute mode)")]
        public Vector3 StartScale = Vector3.one;

        [Condition("Mode", "Absolute")]
        [Tooltip("the ending scale value (for Absolute mode)")]
        public Vector3 EndScale = Vector3.one * 3f;

        [Condition("Mode", "Additive")]
        [Tooltip("the scale to add (multiplied by the curve) when in Additive mode")]
        public Vector3 AdditiveScale = Vector3.one;

        [Condition("Mode", "ToDestination")]
        [Tooltip("the destination scale to animate to (for ToDestination mode)")]
        public Vector3 DestinationScale = Vector3.one;

        [Tooltip("the object to animate")]
        public Transform AnimateScaleTarget;

        [InspectorGroup("Axis Control", 14, false)]
        [Tooltip("if this is true, the AnimateX curve only will be used, and applied to all axis")]
        public bool UniformScaling = true;

        [Condition("UniformScaling", false)]
        [Tooltip("if this is true, should animate the X scale value")]
        public bool AnimateX = true;

        [Tooltip("the x scale animation definition")]
        public AnimationCurve AnimateScaleX = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformScaling", false)]
        [Tooltip("if this is true, should animate the Y scale value")]
        public bool AnimateY = true;

        [Condition("UniformScaling", false)]
        [Tooltip("the y scale animation definition")]
        public AnimationCurve AnimateScaleY = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformScaling", false)]
        [Tooltip("if this is true, should animate the Z scale value")]
        public bool AnimateZ = true;

        [Condition("UniformScaling", false)]
        [Tooltip("the z scale animation definition")]
        public AnimationCurve AnimateScaleZ = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        private Vector3 _initialScale;

        public override float GetDuration()
        {
            return Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
        }
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "Animate the scale of a transform using animation curves with uniform or per-axis control in different modes (Absolute, Additive, ToDestination)";
        }

        public override string GetCategory()
        {
            return "Transform";
        }

        public override string GetWarningMessage()
        {
            if (AnimateScaleTarget == null)
            {
                return "⚠ Animate Scale Target is not assigned. Please assign a Transform to animate.";
            }

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (AnimateScaleTarget == null)
            {
                return UniTask.CompletedTask;
            }

            _initialScale = AnimateScaleTarget.localScale;

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
                AnimateScaleTarget.localScale = EndScale;
                return;
            }

            float elapsed = 0f;
            Vector3 newScale = StartScale;

            Action<float> applyScale = null;

            if (UniformScaling)
            {
                applyScale += (percent) =>
                {
                    float curveValue = AnimateScaleX.Evaluate(percent);
                    newScale.x = Mathf.LerpUnclamped(StartScale.x, EndScale.x, curveValue);
                    newScale.y = newScale.x;
                    newScale.z = newScale.x;
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleX.Evaluate(percent);
                        newScale.x = Mathf.LerpUnclamped(StartScale.x, EndScale.x, curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleY.Evaluate(percent);
                        newScale.y = Mathf.LerpUnclamped(StartScale.y, EndScale.y, curveValue);
                    };
                }

                if (AnimateZ)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleZ.Evaluate(percent);
                        newScale.z = Mathf.LerpUnclamped(StartScale.z, EndScale.z, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);

                applyScale?.Invoke(percent);
                AnimateScaleTarget.localScale = newScale;

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyScale?.Invoke(1f);
            AnimateScaleTarget.localScale = newScale;
        }

        private async UniTask AnimateAdditive(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                return;
            }

            float elapsed = 0f;
            Vector3 newScale = _initialScale;

            Action<float> applyScale = null;

            if (UniformScaling)
            {
                applyScale += (percent) =>
                {
                    float curveValue = AnimateScaleX.Evaluate(percent);
                    newScale.x = _initialScale.x + (AdditiveScale.x * curveValue);
                    newScale.y = _initialScale.y + (AdditiveScale.x * curveValue);
                    newScale.z = _initialScale.z + (AdditiveScale.x * curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleX.Evaluate(percent);
                        newScale.x = _initialScale.x + (AdditiveScale.x * curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleY.Evaluate(percent);
                        newScale.y = _initialScale.y + (AdditiveScale.y * curveValue);
                    };
                }

                if (AnimateZ)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleZ.Evaluate(percent);
                        newScale.z = _initialScale.z + (AdditiveScale.z * curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                newScale = _initialScale;

                float percent = Mathf.Clamp01(elapsed / duration);

                applyScale?.Invoke(percent);
                AnimateScaleTarget.localScale = newScale;

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            newScale = _initialScale;
            applyScale?.Invoke(1f);
            AnimateScaleTarget.localScale = newScale;
        }

        private async UniTask AnimateToDestination(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                AnimateScaleTarget.localScale = DestinationScale;
                return;
            }

            float elapsed = 0f;
            Vector3 newScale = _initialScale;

            Action<float> applyScale = null;

            if (AnimateX)
            {
                applyScale += (percent) =>
                {
                    float curveValue = AnimateScaleX.Evaluate(percent);
                    newScale.x = Mathf.LerpUnclamped(_initialScale.x, DestinationScale.x, curveValue);
                };
            }

            if (UniformScaling)
            {
                applyScale += (percent) =>
                {
                    newScale.y = newScale.x;
                    newScale.z = newScale.x;
                };
            }
            else
            {
                if (AnimateY)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleY.Evaluate(percent);
                        newScale.y = Mathf.LerpUnclamped(_initialScale.y, DestinationScale.y, curveValue);
                    };
                }

                if (AnimateZ)
                {
                    applyScale += (percent) =>
                    {
                        float curveValue = AnimateScaleZ.Evaluate(percent);
                        newScale.z = Mathf.LerpUnclamped(_initialScale.z, DestinationScale.z, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);
                applyScale?.Invoke(percent);
                AnimateScaleTarget.localScale = newScale;
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyScale?.Invoke(1f);
            AnimateScaleTarget.localScale = newScale;
        }
    }
}