using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowPosition : ActionFlow
    {
        public enum ModeType
        {
            Absolute,
            Additive,
            ToDestination
        }
        [InspectorGroup("Position Mode", 12, false)]
        [Header("Duration")]
        public DurationMode DurationMode;

        [Header("Position")]
        [Tooltip("the mode this animation should operate on\n" +
                 "Absolute : moves between start and end values following the curve\n" +
                 "Additive : adds the additive position multiplied by the curve to the initial position\n" +
                 "ToDestination : animates from current position to the destination position")]
        public ModeType Mode = ModeType.Absolute;

        [Tooltip("whether this animation should use local or world position")]
        public TransformSpace PositionMode = TransformSpace.World;

        [Condition("Mode", "Absolute")]
        [Tooltip("the starting position value (for Absolute mode)")]
        public Vector3 StartPosition = Vector3.zero;

        [Condition("Mode", "Absolute")]
        [Tooltip("the ending position value (for Absolute mode)")]
        public Vector3 EndPosition = Vector3.zero;

        [Condition("Mode", "Additive")]
        [Tooltip("the position to add (multiplied by the curve) when in Additive mode")]
        public Vector3 AdditivePosition = Vector3.zero;

        [Condition("Mode", "ToDestination")]
        [Tooltip("the destination position to animate to (for ToDestination mode)")]
        public Vector3 DestinationPosition = Vector3.zero;

        [Tooltip("the transform to animate")]
        public Transform AnimatePositionTarget;

        [InspectorGroup("Axis Control", 14, false)]
        [Tooltip("if true, uses the X curve for all axes with each axis interpolating between its own start and end values")]
        public bool UniformPositioning = false;

        [Condition("UniformPositioning", false)]
        [Tooltip("if true, animates the X position value")]
        public bool AnimateX = true;

        [Tooltip("the curve controlling the X position animation (or all axes if UniformPositioning is true)")]
        public AnimationCurve AnimatePositionX = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformPositioning", false)]
        [Tooltip("if true, animates the Y position value")]
        public bool AnimateY = true;

        [Condition("UniformPositioning", false)]
        [Tooltip("the curve controlling the Y position animation")]
        public AnimationCurve AnimatePositionY = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Condition("UniformPositioning", false)]
        [Tooltip("if true, animates the Z position value")]
        public bool AnimateZ = true;

        [Condition("UniformPositioning", false)]
        [Tooltip("the curve controlling the Z position animation")]
        public AnimationCurve AnimatePositionZ = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        private Vector3 _initialPosition;
        private Action<Vector3> _setPosition;
        private Func<Vector3> _getPosition;

        public override float GetDuration()
        {
            return Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
        }
        
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "Animate the position of a transform using animation curves and different modes (Absolute, Additive, ToDestination)";
        }

        public override string GetCategory()
        {
            return "Transform";
        }

        public override string GetWarningMessage()
        {
            if (AnimatePositionTarget == null)
            {
                return "⚠ Animate Position Target is not assigned. Please assign a Transform to animate.";
            }

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (AnimatePositionTarget == null)
            {
                return UniTask.CompletedTask;
            }

            if (PositionMode == TransformSpace.World)
            {
                _getPosition = () => AnimatePositionTarget.position;
                _setPosition = (position) => AnimatePositionTarget.position = position;
            }
            else
            {
                _getPosition = () => AnimatePositionTarget.localPosition;
                _setPosition = (position) => AnimatePositionTarget.localPosition = position;
            }

            _initialPosition = _getPosition();

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
                _setPosition(EndPosition);
                return;
            }

            float elapsed = 0f;
            Vector3 newPosition = StartPosition;

            Action<float> applyPosition = null;

            if (UniformPositioning)
            {
                applyPosition += (percent) =>
                {
                    float curveValue = AnimatePositionX.Evaluate(percent);
                    newPosition.x = Mathf.LerpUnclamped(StartPosition.x, EndPosition.x, curveValue);
                    newPosition.y = Mathf.LerpUnclamped(StartPosition.y, EndPosition.y, curveValue);
                    newPosition.z = Mathf.LerpUnclamped(StartPosition.z, EndPosition.z, curveValue);
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

                if (AnimateZ)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionZ.Evaluate(percent);
                        newPosition.z = Mathf.LerpUnclamped(StartPosition.z, EndPosition.z, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);

                applyPosition?.Invoke(percent);
                _setPosition(newPosition);

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyPosition?.Invoke(1f);
            _setPosition(newPosition);
        }

        private async UniTask AnimateAdditive(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                return;
            }

            float elapsed = 0f;
            Vector3 newPosition = _initialPosition;

            Action<float> applyPosition = null;

            if (UniformPositioning)
            {
                applyPosition += (percent) =>
                {
                    float curveValue = AnimatePositionX.Evaluate(percent);
                    newPosition.x = _initialPosition.x + (AdditivePosition.x * curveValue);
                    newPosition.y = _initialPosition.y + (AdditivePosition.x * curveValue);
                    newPosition.z = _initialPosition.z + (AdditivePosition.x * curveValue);
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

                if (AnimateZ)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionZ.Evaluate(percent);
                        newPosition.z = _initialPosition.z + (AdditivePosition.z * curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                newPosition = _initialPosition;

                float percent = Mathf.Clamp01(elapsed / duration);

                applyPosition?.Invoke(percent);
                _setPosition(newPosition);

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            newPosition = _initialPosition;
            applyPosition?.Invoke(1f);
            _setPosition(newPosition);
        }

        private async UniTask AnimateToDestination(CancellationToken cancellationToken)
        {
            var duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                _setPosition(DestinationPosition);
                return;
            }

            float elapsed = 0f;
            Vector3 newPosition = _initialPosition;

            Action<float> applyPosition = null;

            if (UniformPositioning)
            {
                applyPosition += (percent) =>
                {
                    float curveValue = AnimatePositionX.Evaluate(percent);
                    newPosition.x = Mathf.LerpUnclamped(_initialPosition.x, DestinationPosition.x, curveValue);
                    newPosition.y = Mathf.LerpUnclamped(_initialPosition.y, DestinationPosition.y, curveValue);
                    newPosition.z = Mathf.LerpUnclamped(_initialPosition.z, DestinationPosition.z, curveValue);
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

                if (AnimateZ)
                {
                    applyPosition += (percent) =>
                    {
                        float curveValue = AnimatePositionZ.Evaluate(percent);
                        newPosition.z = Mathf.LerpUnclamped(_initialPosition.z, DestinationPosition.z, curveValue);
                    };
                }
            }

            while (elapsed < duration)
            {
                float percent = Mathf.Clamp01(elapsed / duration);
                applyPosition?.Invoke(percent);
                _setPosition(newPosition);
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyPosition?.Invoke(1f);
            _setPosition(newPosition);
        }
    }
}