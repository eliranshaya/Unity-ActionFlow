using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowCameraShake : ActionFlow
    {
        #region Variables

        public enum CameraShakeMode
        {
            Positional,
            Rotational,
            Both,
        }

        [InspectorGroup("Duration", 12, false)]
        public DurationMode DurationMode;

        [InspectorGroup("Shake", 12, false)]
        [Tooltip("The camera to shake")]
        public Camera TargetCamera;

        [Tooltip("The shake mode to apply")]
        public CameraShakeMode ShakeMode = CameraShakeMode.Positional;

        [Tooltip("How far the camera moves/rotates during the shake")]
        public float Intensity = 0.3f;

        [Tooltip("How fast the shake oscillates")]
        public float Frequency = 25f;

        [Tooltip("If true, the intensity fades out over the duration")]
        public bool FadeOut = true;

        [Condition("ShakeMode", new[] { "Positional", "Both" })]
        [Tooltip("Shake along the X axis")]
        public bool ShakeX = true;

        [Condition("ShakeMode", new[] { "Positional", "Both" })]
        [Tooltip("Shake along the Y axis")]
        public bool ShakeY = true;

        [Condition("ShakeMode", new[] { "Positional", "Both" })]
        [Tooltip("Shake along the Z axis")]
        public bool ShakeZ = false;

        [Condition("ShakeMode", new[] { "Rotational", "Both" })]
        [Tooltip("Shake rotation on the X axis")]
        public bool ShakeRotX = false;

        [Condition("ShakeMode", new[] { "Rotational", "Both" })]
        [Tooltip("Shake rotation on the Y axis")]
        public bool ShakeRotY = false;

        [Condition("ShakeMode", new[] { "Rotational", "Both" })]
        [Tooltip("Shake rotation on the Z axis")]
        public bool ShakeRotZ = true;

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Shake a camera with positional and/or rotational shake with optional fade out";

        public override string GetCategory() => "Camera";

        public override string GetWarningMessage()
        {
            if (TargetCamera == null)
                return "⚠ Target Camera is not assigned. Please assign a Camera component.";

            return null;
        }
#endif

        protected override async UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetCamera == null)
            {
                return;
            }

            float duration = DurationMode.GetDuration();
            if (duration <= 0f)
            {
                return;
            }

            float elapsed = 0f;

            Vector3 originalLocalPosition = TargetCamera.transform.localPosition;
            Quaternion originalLocalRotation = TargetCamera.transform.localRotation;

            Action<float, float> applyShake = BuildShakeDelegate(originalLocalPosition, originalLocalRotation);

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                applyShake?.Invoke(elapsed, t);

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            TargetCamera.transform.localPosition = originalLocalPosition;
            TargetCamera.transform.localRotation = originalLocalRotation;
        }

        private Action<float, float> BuildShakeDelegate(Vector3 originalLocalPosition, Quaternion originalLocalRotation)
        {
            Action<float, float> applyShake = null;

            if (ShakeMode == CameraShakeMode.Positional || ShakeMode == CameraShakeMode.Both)
            {
                applyShake += (elapsed, t) =>
                {
                    float fadeMultiplier = FadeOut ? (1f - t) : 1f;
                    float currentIntensity = Intensity * fadeMultiplier;
                    float seed = elapsed * Frequency;

                    Vector3 posShake = Vector3.zero;
                    if (ShakeX) posShake.x = (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f * currentIntensity;
                    if (ShakeY) posShake.y = (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f * currentIntensity;
                    if (ShakeZ) posShake.z = (Mathf.PerlinNoise(seed, seed) - 0.5f) * 2f * currentIntensity;

                    TargetCamera.transform.localPosition = originalLocalPosition + posShake;
                };
            }

            if (ShakeMode == CameraShakeMode.Rotational || ShakeMode == CameraShakeMode.Both)
            {
                applyShake += (elapsed, t) =>
                {
                    float fadeMultiplier = FadeOut ? (1f - t) : 1f;
                    float currentIntensity = Intensity * fadeMultiplier;
                    float seed = elapsed * Frequency;

                    Vector3 rotShake = Vector3.zero;
                    if (ShakeRotX) rotShake.x = (Mathf.PerlinNoise(seed + 1f, 0f) - 0.5f) * 2f * currentIntensity;
                    if (ShakeRotY) rotShake.y = (Mathf.PerlinNoise(0f, seed + 1f) - 0.5f) * 2f * currentIntensity;
                    if (ShakeRotZ) rotShake.z = (Mathf.PerlinNoise(seed + 1f, seed + 1f) - 0.5f) * 2f * currentIntensity;

                    TargetCamera.transform.localRotation = originalLocalRotation * Quaternion.Euler(rotShake);
                };
            }

            return applyShake;
        }
    }
}