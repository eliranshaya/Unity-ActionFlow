using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowRotation : ActionFlow
    {
        public enum ModeType
        {
            Absolute,
            Additive,
            ToDestination
        }
        [InspectorGroup("Rotation Mode", 12, false)]
        [Header("Duration")]
        public DurationMode DurationMode;

        [Header("Rotation")]
        [Tooltip("the mode this animation should operate on\n" +
                 "Absolute : rotates between start and end values following the curve\n" +
                 "Additive : adds the additive rotation multiplied by the curve to the initial rotation\n" +
                 "ToDestination : animates from current rotation to the destination rotation")]
        public ModeType Mode = ModeType.Absolute;

        [Tooltip("whether this animation should use local or world rotation")]
        public TransformSpace RotationMode = TransformSpace.World;

        [Condition("Mode", "Absolute")]
        [Tooltip("the starting rotation value in degrees (for Absolute mode)")]
        public Vector3 StartRotation = Vector3.zero;

        [Condition("Mode", "Absolute")]
        [Tooltip("the ending rotation value in degrees (for Absolute mode)")]
        public Vector3 EndRotation = Vector3.zero;

        [Condition("Mode", "Additive")]
        [Tooltip("the rotation to add in degrees (multiplied by the curve) when in Additive mode")]
        public Vector3 AdditiveRotation = Vector3.zero;

        [Condition("Mode", "ToDestination")]
        [Tooltip("the destination rotation in degrees to animate to (for ToDestination mode)")]
        public Vector3 DestinationRotation = Vector3.zero;

        [Tooltip("the transform to animate")]
        public Transform AnimateRotationTarget;

        [InspectorGroup("Axis Control", 14, false)]
        [Tooltip("if true, uses the X curve for all axes with each axis interpolating between its own start and end values")]
        public bool UniformRotationing = false;

        [Condition("UniformRotationing", false)]
        [Tooltip("if true, animates the X rotation value")]
        public bool AnimateX = true;

        [Tooltip("the curve controlling the X rotation animation (or all axes if UniformRotationing is true)")]
        public AnimationCurve AnimateRotationX = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformRotationing", false)]
        [Tooltip("if true, animates the Y rotation value")]
        public bool AnimateY = true;

        [Condition("UniformRotationing", false)]
        [Tooltip("the curve controlling the Y rotation animation")]
        public AnimationCurve AnimateRotationY = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformRotationing", false)]
        [Tooltip("if true, animates the Z rotation value")]
        public bool AnimateZ = true;

        [Condition("UniformRotationing", false)]
        [Tooltip("the curve controlling the Z rotation animation")]
        public AnimationCurve AnimateRotationZ = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        private Vector3 _initialRotation;
        private Action<Vector3> _setRotation;
        private Func<Vector3> _getRotation;

        public override float GetDuration()
        {
            return Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
        }
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "Animate the rotation of a transform using animation curves in local or world space with different modes (Absolute, Additive, ToDestination)";
        }

        public override string GetCategory()
        {
            return "Transform";
        }

        public override string GetWarningMessage()
        {
            if (AnimateRotationTarget == null)
            {
                return "⚠ Animate Rotation Target is not assigned. Please assign a Transform to animate.";
            }

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (AnimateRotationTarget == null)
            {
                return UniTask.CompletedTask;
            }

            if (RotationMode == TransformSpace.World)
            {
                _getRotation = () => AnimateRotationTarget.eulerAngles;
                _setRotation = (rotation) => AnimateRotationTarget.eulerAngles = rotation;
            }
            else
            {
                _getRotation = () => AnimateRotationTarget.localEulerAngles;
                _setRotation = (rotation) => AnimateRotationTarget.localEulerAngles = rotation;
            }

            _initialRotation = _getRotation();

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
                _setRotation(EndRotation);
                return;
            }

            float elapsed = 0f;
            Vector3 newRotation = StartRotation;

            Action<float> applyRotation = null;

            if (UniformRotationing)
            {
                applyRotation += (percent) =>
                {
                    float curveValue = AnimateRotationX.Evaluate(percent);
                    newRotation.x = Mathf.LerpUnclamped(StartRotation.x, EndRotation.x, curveValue);
                    newRotation.y = Mathf.LerpUnclamped(StartRotation.y, EndRotation.y, curveValue);
                    newRotation.z = Mathf.LerpUnclamped(StartRotation.z, EndRotation.z, curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationX.Evaluate(percent);
                        newRotation.x = Mathf.LerpUnclamped(StartRotation.x, EndRotation.x, curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationY.Evaluate(percent);
                        newRotation.y = Mathf.LerpUnclamped(StartRotation.y, EndRotation.y, curveValue);
                    };
                }

                if (AnimateZ)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationZ.Evaluate(percent);
                        newRotation.z = Mathf.LerpUnclamped(StartRotation.z, EndRotation.z, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);

                applyRotation?.Invoke(percent);
                _setRotation(newRotation);

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyRotation?.Invoke(1f);
            _setRotation(newRotation);
        }

        private async UniTask AnimateAdditive(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                return;
            }

            float elapsed = 0f;
            Vector3 newRotation = _initialRotation;

            Action<float> applyRotation = null;

            if (UniformRotationing)
            {
                applyRotation += (percent) =>
                {
                    float curveValue = AnimateRotationX.Evaluate(percent);
                    newRotation.x = _initialRotation.x + (AdditiveRotation.x * curveValue);
                    newRotation.y = _initialRotation.y + (AdditiveRotation.x * curveValue);
                    newRotation.z = _initialRotation.z + (AdditiveRotation.x * curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationX.Evaluate(percent);
                        newRotation.x = _initialRotation.x + (AdditiveRotation.x * curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationY.Evaluate(percent);
                        newRotation.y = _initialRotation.y + (AdditiveRotation.y * curveValue);
                    };
                }

                if (AnimateZ)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationZ.Evaluate(percent);
                        newRotation.z = _initialRotation.z + (AdditiveRotation.z * curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                newRotation = _initialRotation;

                float percent = Mathf.Clamp01(elapsed / duration);
                applyRotation?.Invoke(percent);

                _setRotation(newRotation);
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            newRotation = _initialRotation;
            applyRotation?.Invoke(1f);
            _setRotation(newRotation);
        }

        private async UniTask AnimateToDestination(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                _setRotation(DestinationRotation);
                return;
            }

            float elapsed = 0f;
            Vector3 newRotation = _initialRotation;

            Action<float> applyRotation = null;

            if (UniformRotationing)
            {
                applyRotation += (percent) =>
                {
                    float curveValue = AnimateRotationX.Evaluate(percent);
                    newRotation.x = Mathf.LerpUnclamped(_initialRotation.x, DestinationRotation.x, curveValue);
                    newRotation.y = Mathf.LerpUnclamped(_initialRotation.y, DestinationRotation.y, curveValue);
                    newRotation.z = Mathf.LerpUnclamped(_initialRotation.z, DestinationRotation.z, curveValue);
                };
            }
            else
            {
                if (AnimateX)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationX.Evaluate(percent);
                        newRotation.x = Mathf.LerpUnclamped(_initialRotation.x, DestinationRotation.x, curveValue);
                    };
                }

                if (AnimateY)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationY.Evaluate(percent);
                        newRotation.y = Mathf.LerpUnclamped(_initialRotation.y, DestinationRotation.y, curveValue);
                    };
                }

                if (AnimateZ)
                {
                    applyRotation += (percent) =>
                    {
                        float curveValue = AnimateRotationZ.Evaluate(percent);
                        newRotation.z = Mathf.LerpUnclamped(_initialRotation.z, DestinationRotation.z, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);
                applyRotation?.Invoke(percent);
                _setRotation(newRotation);
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyRotation?.Invoke(1f);
            _setRotation(newRotation);
        }
    }
}