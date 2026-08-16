using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowMeshRenderer : ActionFlow
    {
        #region Variables

        public enum MaterialAnimationMode
        {
            SetFloat,
            SetColor,
            SetInt,
            SetVector,
            LerpFloat,
            LerpColor,
            LerpVector,
        }

        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[] { "LerpFloat", "LerpColor", "LerpVector" })]
        public DurationMode DurationMode;

        [InspectorGroup("Material", 12, false)]
        [Tooltip("The Renderer whose material will be animated")]
        public Renderer TargetRenderer;
        [Tooltip("Index of the material to animate (0 = first, 1 = second, etc.)")]
        public int MaterialIndex = 0;
        [Tooltip("If true, operates on a cloned instance of the material (won't affect shared material)")]
        public bool UseInstancedMaterial = true;

        [Tooltip("The animation mode to apply to the material")]
        public MaterialAnimationMode AnimationMode = MaterialAnimationMode.SetFloat;

        [Tooltip("The shader property name (e.g. '_Color', '_Metallic', '_EmissionColor')")]
        public string PropertyName = "_Color";

        [Condition("AnimationMode", "SetFloat")]
        [Tooltip("The float value to set immediately")]
        public float FloatValue = 1f;

        [Condition("AnimationMode", "LerpFloat")]
        [Tooltip("Start value for float lerp")]
        public float FloatFrom = 0f;

        [Condition("AnimationMode", "LerpFloat")]
        [Tooltip("End value for float lerp")]
        public float FloatTo = 1f;

        [Condition("AnimationMode", "LerpFloat")]
        [Tooltip("Optional easing curve for float lerp")]
        public AnimationCurve FloatCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Condition("AnimationMode", "SetInt")]
        [Tooltip("The integer value to set immediately")]
        public int IntValue = 1;

        [Condition("AnimationMode", "SetColor")]
        [Tooltip("The color value to set immediately")]
        public Color ColorValue = Color.white;

        [Condition("AnimationMode", "LerpColor")]
        [Tooltip("Start color for color lerp")]
        public Color ColorFrom = Color.black;

        [Condition("AnimationMode", "LerpColor")]
        [Tooltip("End color for color lerp")]
        public Color ColorTo = Color.white;

        [Condition("AnimationMode", "LerpColor")]
        [Tooltip("Optional easing curve for color lerp")]
        public AnimationCurve ColorCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Condition("AnimationMode", "SetVector")]
        [Tooltip("The Vector4 value to set immediately")]
        public Vector4 VectorValue = Vector4.one;

        [Condition("AnimationMode", "LerpVector")]
        [Tooltip("Start Vector4 for lerp")]
        public Vector4 VectorFrom = Vector4.zero;

        [Condition("AnimationMode", "LerpVector")]
        [Tooltip("End Vector4 for lerp")]
        public Vector4 VectorTo = Vector4.one;

        [Condition("AnimationMode", "LerpVector")]
        [Tooltip("Optional easing curve for vector lerp")]
        public AnimationCurve VectorCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("If true, restores the original material property value when the action finishes")]
        public bool RestoreOnComplete = false;

        #endregion

        private int _propertyId;
        private Material _materialInstance;

        private Material GetMaterial()
        {
            if (TargetRenderer == null) return null;

            if (UseInstancedMaterial)
            {
                if (_materialInstance == null)
                {
                    Material[] instances = TargetRenderer.materials;
                    if (MaterialIndex < 0 || MaterialIndex >= instances.Length) return null;
                    _materialInstance = instances[MaterialIndex];
                }

                return _materialInstance;
            }
            else
            {
                Material[] shared = TargetRenderer.sharedMaterials;
                if (MaterialIndex < 0 || MaterialIndex >= shared.Length) return null;
                return shared[MaterialIndex];
            }
        }

        public override float GetDuration() => (Timing != null && Timing.WaitForCompletion) ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Animate or set material properties (float, color, int, vector) on a Renderer";

        public override string GetCategory() => "Renderer";

        public override string GetWarningMessage()
        {
            if (TargetRenderer == null)
                return "⚠ Target Renderer is not assigned.";

            if (string.IsNullOrEmpty(PropertyName))
                return "⚠ Property Name is empty. Enter a shader property name like '_Color'.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (TargetRenderer == null) return UniTask.CompletedTask;

            _propertyId = Shader.PropertyToID(PropertyName);

            Material mat = GetMaterial();
            if (mat == null) return UniTask.CompletedTask;

            switch (AnimationMode)
            {
                case MaterialAnimationMode.SetFloat: DoSetFloat(mat); break;
                case MaterialAnimationMode.SetColor: DoSetColor(mat); break;
                case MaterialAnimationMode.SetInt: DoSetInt(mat); break;
                case MaterialAnimationMode.SetVector: DoSetVector(mat); break;
                case MaterialAnimationMode.LerpFloat: return DoLerpFloat(mat, cancellationToken);
                case MaterialAnimationMode.LerpColor: return DoLerpColor(mat, cancellationToken);
                case MaterialAnimationMode.LerpVector: return DoLerpVector(mat, cancellationToken);
            }

            return UniTask.CompletedTask;
        }

        private void DoSetFloat(Material mat)
        {
            float original = RestoreOnComplete ? mat.GetFloat(_propertyId) : 0f;
            mat.SetFloat(_propertyId, FloatValue);

            if (RestoreOnComplete) mat.SetFloat(_propertyId, original);
        }

        private void DoSetColor(Material mat)
        {
            Color original = RestoreOnComplete ? mat.GetColor(_propertyId) : Color.white;
            mat.SetColor(_propertyId, ColorValue);

            if (RestoreOnComplete) mat.SetColor(_propertyId, original);
        }

        private void DoSetInt(Material mat)
        {
            int original = RestoreOnComplete ? mat.GetInt(_propertyId) : 0;
            mat.SetInt(_propertyId, IntValue);

            if (RestoreOnComplete) mat.SetInt(_propertyId, original);
        }

        private void DoSetVector(Material mat)
        {
            Vector4 original = RestoreOnComplete ? mat.GetVector(_propertyId) : Vector4.zero;
            mat.SetVector(_propertyId, VectorValue);

            if (RestoreOnComplete) mat.SetVector(_propertyId, original);
        }

        private async UniTask DoLerpFloat(Material mat, CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            float original = RestoreOnComplete ? mat.GetFloat(_propertyId) : 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = FloatCurve.Evaluate(t);

                mat.SetFloat(_propertyId, Mathf.LerpUnclamped(FloatFrom, FloatTo, curvedT));

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            mat.SetFloat(_propertyId, RestoreOnComplete ? original : FloatTo);
        }

        private async UniTask DoLerpColor(Material mat, CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            Color original = RestoreOnComplete ? mat.GetColor(_propertyId) : Color.white;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = ColorCurve.Evaluate(t);

                mat.SetColor(_propertyId, Color.LerpUnclamped(ColorFrom, ColorTo, curvedT));

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            mat.SetColor(_propertyId, RestoreOnComplete ? original : ColorTo);
        }

        private async UniTask DoLerpVector(Material mat, CancellationToken cancellationToken)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            Vector4 original = RestoreOnComplete ? mat.GetVector(_propertyId) : Vector4.zero;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = VectorCurve.Evaluate(t);

                mat.SetVector(_propertyId, Vector4.LerpUnclamped(VectorFrom, VectorTo, curvedT));

                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }

            mat.SetVector(_propertyId, RestoreOnComplete ? original : VectorTo);
        }
    }
}