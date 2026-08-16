using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowCameraZoom : ActionFlow
    {
        #region Variables

        public enum CameraZoomMode
        {
            FieldOfView,
            OrthographicSize,
        }

        [InspectorGroup("Duration", 12, false)]
        public DurationMode DurationMode;

        [InspectorGroup("Camera", 12, false)]
        [Tooltip("The camera to zoom")]
        public Camera TargetCamera;

        [Tooltip("The zoom mode to use — FieldOfView for perspective, OrthographicSize for orthographic")]
        public CameraZoomMode ZoomMode = CameraZoomMode.FieldOfView;

        [Condition("ZoomMode", "FieldOfView")]
        [Tooltip("The field of view to start from")]
        public float FromFOV = 60f;

        [Condition("ZoomMode", "FieldOfView")]
        [Tooltip("The field of view to zoom to")]
        public float ToFOV = 40f;

        [Condition("ZoomMode", "OrthographicSize")]
        [Tooltip("The orthographic size to start from")]
        public float FromSize = 5f;

        [Condition("ZoomMode", "OrthographicSize")]
        [Tooltip("The orthographic size to zoom to")]
        public float ToSize = 3f;

        [Tooltip("Curve controlling the easing of the zoom")]
        public AnimationCurve ZoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Zoom a camera by animating its Field of View or Orthographic Size";

        public override string GetCategory() => "Camera";

        public override string GetWarningMessage()
        {
            if (TargetCamera == null)
                return "⚠ Target Camera is not assigned. Please assign a Camera component.";

            if (ZoomMode == CameraZoomMode.FieldOfView && TargetCamera != null && TargetCamera.orthographic)
                return "⚠ ZoomMode is set to FieldOfView but the Camera is Orthographic. Consider switching to OrthographicSize.";

            if (ZoomMode == CameraZoomMode.OrthographicSize && TargetCamera != null && !TargetCamera.orthographic)
                return "⚠ ZoomMode is set to OrthographicSize but the Camera is Perspective. Consider switching to FieldOfView.";

            return null;
        }
#endif

        protected override async UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetCamera == null) return;

            float duration = DurationMode.GetDuration();

            Action<float> applyZoom = BuildZoomDelegate();

            if (duration <= 0f)
            {
                applyZoom.Invoke(1f);
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                applyZoom.Invoke(ZoomCurve.Evaluate(t));

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            applyZoom.Invoke(ZoomCurve.Evaluate(1f));
        }

        private Action<float> BuildZoomDelegate()
        {
            if (ZoomMode == CameraZoomMode.FieldOfView)
            {
                return t => TargetCamera.fieldOfView = Mathf.LerpUnclamped(FromFOV, ToFOV, t);
            }
            else
            {
                return t => TargetCamera.orthographicSize = Mathf.LerpUnclamped(FromSize, ToSize, t);
            }
        }
    }
}