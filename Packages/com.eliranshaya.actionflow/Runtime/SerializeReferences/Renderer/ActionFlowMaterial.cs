using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowMaterial : ActionFlow
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
            AddFloat, //can you implement AddFloat?
        }

        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[] { "LerpFloat", "LerpColor", "LerpVector", "AddFloat" })]
        public DurationMode DurationMode;

        [InspectorGroup("Material", 12, false)]
        [Tooltip("The Renderer whose material will be animated")]
        public Material Material;

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

        [Condition("AnimationMode", "AddFloat")]
        public float FloatAdd = 1f;

        #endregion

        private int _propertyId;
        private Material _materialInstance;

        public override float GetDuration() => (Timing != null && Timing.WaitForCompletion) ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Animate or set material properties (float, color, int, vector) on a Renderer";

        public override string GetCategory() => "Renderer";

        public override string GetWarningMessage()
        {
            if (Material == null)
                return "⚠ Material is not assigned.";

            if (string.IsNullOrEmpty(PropertyName))
                return "⚠ Property Name is empty. Enter a shader property name like '_Color'.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (Material == null) yield break;

            _propertyId = Shader.PropertyToID(PropertyName);

            switch (AnimationMode)
            {
                case MaterialAnimationMode.SetFloat: yield return DoSetFloat(Material); break;
                case MaterialAnimationMode.SetColor: yield return DoSetColor(Material); break;
                case MaterialAnimationMode.SetInt: yield return DoSetInt(Material); break;
                case MaterialAnimationMode.SetVector: yield return DoSetVector(Material); break;
                case MaterialAnimationMode.LerpFloat: yield return DoLerpFloat(Material); break;
                case MaterialAnimationMode.LerpColor: yield return DoLerpColor(Material); break;
                case MaterialAnimationMode.LerpVector: yield return DoLerpVector(Material); break;
                case MaterialAnimationMode.AddFloat: yield return DoAddFloat(Material); break;
            }
        }

        private IEnumerator DoSetFloat(Material mat)
        {
            float original = RestoreOnComplete ? mat.GetFloat(_propertyId) : 0f;
            mat.SetFloat(_propertyId, FloatValue);

            if (RestoreOnComplete) mat.SetFloat(_propertyId, original);

            yield break;
        }

        private IEnumerator DoSetColor(Material mat)
        {
            Color original = RestoreOnComplete ? mat.GetColor(_propertyId) : Color.white;
            mat.SetColor(_propertyId, ColorValue);

            if (RestoreOnComplete) mat.SetColor(_propertyId, original);

            yield break;
        }

        private IEnumerator DoSetInt(Material mat)
        {
            int original = RestoreOnComplete ? mat.GetInt(_propertyId) : 0;
            mat.SetInt(_propertyId, IntValue);

            if (RestoreOnComplete) mat.SetInt(_propertyId, original);

            yield break;
        }

        private IEnumerator DoSetVector(Material mat)
        {
            Vector4 original = RestoreOnComplete ? mat.GetVector(_propertyId) : Vector4.zero;
            mat.SetVector(_propertyId, VectorValue);

            if (RestoreOnComplete) mat.SetVector(_propertyId, original);

            yield break;
        }

        private IEnumerator DoLerpFloat(Material mat)
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
                yield return YieldInstruction;
            }

            mat.SetFloat(_propertyId, RestoreOnComplete ? original : FloatTo);
        }

        private IEnumerator DoLerpColor(Material mat)
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
                yield return YieldInstruction;
            }

            mat.SetColor(_propertyId, RestoreOnComplete ? original : ColorTo);
        }

        private IEnumerator DoLerpVector(Material mat)
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
                yield return YieldInstruction;
            }

            mat.SetVector(_propertyId, RestoreOnComplete ? original : VectorTo);
        }

        private IEnumerator DoAddFloat(Material mat)
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;
            float original = RestoreOnComplete ? mat.GetFloat(_propertyId) : 0f;

            while (elapsed < duration)
            {
                float current = mat.GetFloat(_propertyId);
                mat.SetFloat(_propertyId, current + FloatAdd * DeltaTime());

                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            if (RestoreOnComplete) mat.SetFloat(_propertyId, original);
        }
    }
}