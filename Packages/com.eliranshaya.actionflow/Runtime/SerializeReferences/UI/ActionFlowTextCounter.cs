using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowTextCounter : ActionFlow
    {
        #region Variables

        public enum CounterFormat
        {
            Integer,
            OneDecimal,
            TwoDecimals,
        }

        public enum TextAnimationMode
        {
            None,
            FadeIn,
            ColorChange,
            Wave,
            Shake,
            Rainbow,
            Glitch,
            FadeInPerChar,
            SlideInPerChar,
        }

        [InspectorGroup("Duration", 12, false)]
        public DurationMode DurationMode;

        [InspectorGroup("Counter", 12, false)]
        [Tooltip("The TextMeshPro component to display the counter")]
        public TextMeshProUGUI TargetText;

        [Tooltip("The value to count from")]
        public float FromValue = 0f;

        [Tooltip("The value to count to")]
        public float ToValue = 100f;

        [Tooltip("Number format for the displayed value")]
        public CounterFormat Format = CounterFormat.Integer;

        [Tooltip("Optional prefix before the number (e.g. '$')")]
        public string Prefix = "";

        [Tooltip("Optional suffix after the number (e.g. '%')")]
        public string Suffix = "";

        [Tooltip("Curve controlling the easing of the counter (X = time 0-1, Y = value 0-1)")]
        public AnimationCurve CounterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [InspectorGroup("Text Animation", 12, false)]
        [Tooltip("The animation mode to apply on the text while counting")]
        public TextAnimationMode AnimationMode = TextAnimationMode.None;

        [Condition("AnimationMode", "FadeIn")]
        [Range(0f, 1f)] public float FadeFromAlpha = 0f;
        [Condition("AnimationMode", "FadeIn")]
        [Range(0f, 1f)] public float FadeToAlpha = 1f;

        [Condition("AnimationMode", "ColorChange")]
        public Color FromColor = Color.white;
        [Condition("AnimationMode", "ColorChange")]
        public Color ToColor = Color.red;

        [Condition("AnimationMode", "Wave")]
        public float WaveAmplitude = 10f;
        [Condition("AnimationMode", "Wave")]
        public float WaveSpeed = 2f;
        [Condition("AnimationMode", "Wave")]
        public float WaveFrequency = 1f;

        [Condition("AnimationMode", "Shake")]
        public float ShakeIntensity = 3f;

        [Condition("AnimationMode", "Rainbow")]
        public float RainbowSpeed = 1f;
        [Condition("AnimationMode", "Rainbow")]
        public float RainbowFrequency = 0.2f;

        [Condition("AnimationMode", "Glitch")]
        public float GlitchIntensity = 8f;
        [Condition("AnimationMode", "Glitch")]
        [Range(0f, 1f)] public float GlitchChance = 0.1f;
        [Condition("AnimationMode", "Glitch")]
        public bool GlitchColor = true;

        [Condition("AnimationMode", "FadeInPerChar")]
        public float FadePerCharDelay = 0.05f;
        [Condition("AnimationMode", "FadeInPerChar")]
        public float FadePerCharDuration = 0.2f;

        [Condition("AnimationMode", "SlideInPerChar")]
        public float SlidePerCharDelay = 0.05f;
        [Condition("AnimationMode", "SlideInPerChar")]
        public float SlidePerCharDuration = 0.2f;
        [Condition("AnimationMode", "SlideInPerChar")]
        public Vector3 SlidePerCharOffset = new Vector3(0f, -30f, 0f);
        [Condition("AnimationMode", "SlideInPerChar")]
        public AnimationCurve SlidePerCharCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.1f), new Keyframe(1f, 1f));

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Count a number from one value to another with optional text animation effects";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (TargetText == null)
                return "⚠ Target Text is not assigned. Please assign a TextMeshProUGUI component.";
            return null;
        }

        private void MarkDirty()
        {
            if (TargetText == null) return;
            UnityEditor.EditorUtility.SetDirty(TargetText);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(TargetText.rectTransform);
            TargetText.canvas?.rootCanvas?.GetComponent<UnityEngine.UI.Graphic>()?.SetAllDirty();
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetText == null)
            {
                yield break;
            }

            float duration = DurationMode.GetDuration();

            Func<float, string> formatValue = Format switch
            {
                CounterFormat.Integer => v => Mathf.RoundToInt(v).ToString("N0"),
                CounterFormat.OneDecimal => v => v.ToString("F1"),
                CounterFormat.TwoDecimals => v => v.ToString("F2"),
                _ => v => Mathf.RoundToInt(v).ToString("N0")
            };

            Action<float> setText = string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix)
                ? v => TargetText.text = formatValue(v)
                : v => TargetText.text = Prefix + formatValue(v) + Suffix;

            Action<float, float> applyAnimation = BuildAnimationDelegate();

            if (duration <= 0f)
            {
                setText(ToValue);
                yield break;
            }

            float elapsed = 0f;
            setText(FromValue);

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = CounterCurve.Evaluate(t);

                setText(Mathf.LerpUnclamped(FromValue, ToValue, curvedT));
                applyAnimation?.Invoke(elapsed, t);

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            setText(ToValue);
            applyAnimation?.Invoke(duration, 1f);

#if UNITY_EDITOR
            if (!Application.isPlaying) MarkDirty();
#endif
        }

        private Action<float, float> BuildAnimationDelegate()
        {
            switch (AnimationMode)
            {
                case TextAnimationMode.FadeIn:
                    return (elapsed, t) =>
                    {
                        Color c = TargetText.color;
                        c.a = Mathf.Lerp(FadeFromAlpha, FadeToAlpha, t);
                        TargetText.color = c;
                    };

                case TextAnimationMode.ColorChange:
                    return (elapsed, t) =>
                    {
                        TargetText.color = Color.Lerp(FromColor, ToColor, t);
                    };

                case TextAnimationMode.Wave:
                    return (elapsed, t) =>
                    {
                        TargetText.ForceMeshUpdate();
                        var textInfo = TargetText.textInfo;
                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            if (!textInfo.characterInfo[i].isVisible) continue;
                            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                            float offset = Mathf.Sin((elapsed * WaveSpeed) + (i * WaveFrequency)) * WaveAmplitude;
                            for (int v = 0; v < 4; v++)
                                verts[vertexIndex + v] += new Vector3(0, offset, 0);
                        }

                        PushMeshUpdate();
                    };

                case TextAnimationMode.Shake:
                    return (elapsed, t) =>
                    {
                        TargetText.ForceMeshUpdate();
                        var textInfo = TargetText.textInfo;
                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            if (!textInfo.characterInfo[i].isVisible) continue;
                            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                            Vector3 shake = new Vector3(UnityEngine.Random.Range(-ShakeIntensity, ShakeIntensity),
                                UnityEngine.Random.Range(-ShakeIntensity, ShakeIntensity), 0);
                            for (int v = 0; v < 4; v++)
                                verts[vertexIndex + v] += shake;
                        }

                        PushMeshUpdate();
                    };

                case TextAnimationMode.Rainbow:
                    return (elapsed, t) =>
                    {
                        TargetText.ForceMeshUpdate();
                        var textInfo = TargetText.textInfo;
                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            if (!textInfo.characterInfo[i].isVisible) continue;
                            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                            Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                            Color color = Color.HSVToRGB(Mathf.Repeat((elapsed * RainbowSpeed) + (i * RainbowFrequency), 1f), 1f, 1f);
                            for (int v = 0; v < 4; v++)
                                colors[vertexIndex + v] = color;
                        }

                        PushColorUpdate();
                    };

                case TextAnimationMode.Glitch:
                    return (elapsed, t) =>
                    {
                        TargetText.ForceMeshUpdate();
                        var textInfo = TargetText.textInfo;
                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            if (!textInfo.characterInfo[i].isVisible) continue;
                            if (UnityEngine.Random.value >= GlitchChance) continue;
                            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                            Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                            Vector3 glitch = new Vector3(UnityEngine.Random.Range(-GlitchIntensity, GlitchIntensity),
                                UnityEngine.Random.Range(-GlitchIntensity * 0.5f, GlitchIntensity * 0.5f), 0);
                            for (int v = 0; v < 4; v++)
                                verts[vertexIndex + v] += glitch;
                            if (GlitchColor)
                            {
                                Color32 glitchCol = new Color32((byte)UnityEngine.Random.Range(0, 255),
                                    (byte)UnityEngine.Random.Range(0, 255),
                                    (byte)UnityEngine.Random.Range(0, 255), 255);
                                for (int v = 0; v < 4; v++)
                                    colors[vertexIndex + v] = glitchCol;
                            }
                        }

                        PushMeshAndColorUpdate();
                    };

                case TextAnimationMode.FadeInPerChar:
                    return (elapsed, t) =>
                    {
                        TargetText.ForceMeshUpdate();
                        var textInfo = TargetText.textInfo;
                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            if (!textInfo.characterInfo[i].isVisible) continue;
                            float startTime = i * FadePerCharDelay;
                            float charT = Mathf.Clamp01((elapsed - startTime) / FadePerCharDuration);
                            byte alpha = (byte)(charT * 255);
                            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                            Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                            for (int v = 0; v < 4; v++)
                                colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r,
                                    colors[vertexIndex + v].g,
                                    colors[vertexIndex + v].b, alpha);
                        }

                        PushColorUpdate();
                    };

                case TextAnimationMode.SlideInPerChar:
                    return (elapsed, t) =>
                    {
                        TargetText.ForceMeshUpdate();
                        var textInfo = TargetText.textInfo;
                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            if (!textInfo.characterInfo[i].isVisible) continue;
                            float startTime = i * SlidePerCharDelay;
                            float charT = Mathf.Clamp01((elapsed - startTime) / SlidePerCharDuration);
                            float curve = SlidePerCharCurve.Evaluate(charT);
                            byte alpha = (byte)(charT * 255);
                            Vector3 offset = Vector3.LerpUnclamped(SlidePerCharOffset, Vector3.zero, curve);
                            int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                            Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                            for (int v = 0; v < 4; v++)
                            {
                                verts[vertexIndex + v] += offset;
                                colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r,
                                    colors[vertexIndex + v].g,
                                    colors[vertexIndex + v].b, alpha);
                            }
                        }

                        PushMeshAndColorUpdate();
                    };

                default:
                    return null;
            }
        }

        private void PushMeshUpdate()
        {
            var textInfo = TargetText.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        private void PushColorUpdate()
        {
            var textInfo = TargetText.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        private void PushMeshAndColorUpdate()
        {
            var textInfo = TargetText.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}