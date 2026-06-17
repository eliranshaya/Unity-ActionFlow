using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowText : ActionFlow
    {
        #region Varaibles:

        public enum TextAnimationMode
        {
            Typewriter,
            FadeIn,
            ColorChange,
            TypewriterWithPunch,
            Wave,
            Shake,
            Rainbow,
            Glitch,
            FadeInPerChar,
            SlideInPerChar,
            EmptyText,
            SlideInPerCharAndWave,
            SlideInPerCharAndCurve,
            SlideInPerCharBouncyArc,
            SlideInPerCharCartoonArc
        }

        [InspectorGroup("Duration", 12, false)]
        [Condition("AnimationMode", new[]
        {
            "Typewriter",
            "FadeIn",
            "ColorChange",
            "Wave",
            "Shake",
            "Rainbow",
            "Glitch",
            "SlideInPerCharAndWave"
        })]
        public DurationMode DurationMode;

        [InspectorGroup("Text", 12, false)] [Tooltip("The TextMeshPro component to animate")] public TMP_Text TargetText;

        [Condition("AnimationMode", new[]
        {
            "Typewriter",
            "TypewriterWithPunch"
        })]
        [Tooltip("If true, uses the text fields below. If false, uses the current text already in the TextMeshPro component.")]
        public bool ReplaceText = true;

        [Tooltip("The animation mode to use on the text")] public TextAnimationMode AnimationMode = TextAnimationMode.Typewriter;

        [Condition("ReplaceText", "AnimationMode", "Typewriter")] [Tooltip("The full text to reveal character by character")] public string TypewriterText = "Hello World";

        [Condition("AnimationMode", "Typewriter")] [Tooltip("If true, clears the text before starting the typewriter effect")] public bool ClearOnStart = true;

        [Condition("AnimationMode", "FadeIn")] [Tooltip("The alpha value to start from (0 = fully transparent)")] [Range(0f, 1f)] public float FadeFromAlpha = 0f;

        [Condition("AnimationMode", "FadeIn")] [Tooltip("The alpha value to end at (1 = fully opaque)")] [Range(0f, 1f)] public float FadeToAlpha = 1f;

        [Condition("AnimationMode", "ColorChange")] [Tooltip("The color to animate from")] public Color FromColor = Color.white;

        [Condition("AnimationMode", "ColorChange")] [Tooltip("The color to animate to")] public Color ToColor = Color.red;

        [Condition("ReplaceText", "AnimationMode", "TypewriterWithPunch")] [Tooltip("The full text to reveal character by character with a size punch per character")] public string TypewriterPunchText = "Hello World";

        [Condition("AnimationMode", "TypewriterWithPunch")] [Tooltip("If true, clears the text before starting")] public bool PunchClearOnStart = true;

        [Condition("AnimationMode", "TypewriterWithPunch")] [Tooltip("The font size multiplier at the peak of the punch (e.g. 1.5 = 150% of normal size)")] public float PunchSizeMultiplier = 1.5f;

        [Condition("AnimationMode", "TypewriterWithPunch")] [Tooltip("How long each character's punch animation takes in seconds")] public float PunchDurationPerChar = 0.1f;

        [Condition("AnimationMode", "TypewriterWithPunch")] [Tooltip("Delay between each character appearing")] public float DelayBetweenChars = 0.05f;

        [Condition("AnimationMode", new[]
        {
            "Wave",
            "SlideInPerCharAndWave"
        })]
        [Tooltip("Height of the wave in pixels")]
        public float WaveAmplitude = 10f;

        [Condition("AnimationMode", new[]
        {
            "Wave",
            "SlideInPerCharAndWave"
        })]
        [Tooltip("How fast the wave moves")]
        public float WaveSpeed = 2f;

        [Condition("AnimationMode", new[]
        {
            "Wave",
            "SlideInPerCharAndWave"
        })]
        [Tooltip("Distance between each character's wave offset")]
        public float WaveFrequency = 1f;

        [Condition("AnimationMode", "Shake")] [Tooltip("How far each character shakes in pixels")] public float ShakeIntensity = 3f;

        [Condition("AnimationMode", "Rainbow")] [Tooltip("How fast the colors cycle")] public float RainbowSpeed = 1f;

        [Condition("AnimationMode", "Rainbow")] [Tooltip("Offset between each character's color")] public float RainbowFrequency = 0.2f;

        [Condition("AnimationMode", "Glitch")] [Tooltip("How far glitched characters offset in pixels")] public float GlitchIntensity = 8f;

        [Condition("AnimationMode", "Glitch")] [Tooltip("Chance per character per frame to glitch (0-1)")] [Range(0f, 1f)] public float GlitchChance = 0.1f;

        [Condition("AnimationMode", "Glitch")] [Tooltip("Whether to also randomize color on glitched characters")] public bool GlitchColor = true;

        [Condition("AnimationMode", "FadeInPerChar")] [Tooltip("Delay between each character starting to fade in")] public float FadePerCharDelay = 0.05f;

        [Condition("AnimationMode", "FadeInPerChar")] [Tooltip("How long each individual character takes to fade in")] public float FadePerCharDuration = 0.2f;

        [Condition("AnimationMode", new[]
        {
            "SlideInPerChar",
            "SlideInPerCharAndWave",
            "SlideInPerCharAndCurve"
        })]
        [Tooltip("Delay between each character starting to slide in")]
        public float SlidePerCharDelay = 0.05f;

        [Condition("AnimationMode", new[]
        {
            "SlideInPerChar",
            "SlideInPerCharAndWave",
            "SlideInPerCharAndCurve"
        })]
        [Tooltip("How long each individual character takes to slide in")]
        public float SlidePerCharDuration = 0.2f;

        [Condition("AnimationMode", new[]
        {
            "SlideInPerChar",
            "SlideInPerCharAndWave",
            "SlideInPerCharAndCurve"
        })]
        [Tooltip("The offset each character slides in from")]
        public Vector3 SlidePerCharOffset = new Vector3(0f, -30f, 0f);

        [Condition("AnimationMode", new[]
        {
            "SlideInPerChar",
            "SlideInPerCharAndWave",
            "SlideInPerCharAndCurve"
        })]
        [Tooltip("Curve controlling the slide easing")]
        public AnimationCurve SlidePerCharCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.1f), new Keyframe(1f, 1f));

        [Condition("AnimationMode", "SlideInPerCharAndCurve")] [Tooltip("The arch shape of the text. X = position along text (0 = first char, 1 = last char). Y = vertical offset multiplier. Default is a rainbow arc.")]
        public AnimationCurve TextArchCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        [Condition("AnimationMode", "SlideInPerCharAndCurve")] [Tooltip("Maximum height of the arch in pixels (multiplied by the curve value)")] public float ArchHeight = 30f;

        [Condition("AnimationMode", "SlideInPerCharAndCurve")] [Tooltip("If true, each character rotates to follow the slope of the arch (tilts toward the peak)")] public bool RotateAlongArch = true;

        [Condition("AnimationMode", "SlideInPerCharAndCurve")] [Tooltip("Multiplier on the rotation. 1 = natural slope angle, increase for more dramatic tilt, negative to invert.")] public float RotationStrength = 1f;
        // ============================================================
        //  SlideInPerCharCartoonArc — like the "WELL DONE!" video
        // ============================================================

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Delay between each letter starting")] public float CartoonArcDelay = 0.08f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Duration of one letter entrance")] public float CartoonArcDuration = 0.55f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Where letters fly from, relative to final position")] public Vector3 CartoonArcStartOffset = new Vector3(-260f, -40f, 0f);

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Extra height of the flying arc")] public float CartoonArcTravelHeight = 110f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Final curve height of the word")] public float CartoonArcFinalHeight = 28f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Final word arc shape")] public AnimationCurve CartoonArcFinalCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("Starting rotation of each letter")] public float CartoonArcStartRotation = -55f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Tooltip("How much letters rotate along final arc")] public float CartoonArcRotationStrength = 1.15f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] [Range(0f, 1f)] public float CartoonArcStartScale = 0.15f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] public float CartoonArcOvershootScale = 1.22f;

        [Condition("AnimationMode", "SlideInPerCharCartoonArc")] public Vector2 CartoonArcSquash = new Vector2(1.18f, 0.86f);
        // ============================================================
        //  SlideInPerCharBouncyArc — juicy mobile-game style entrance
        // ============================================================

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Delay between each character starting its entrance")] public float BouncyArcDelay = 0.07f;

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("How long each character's full entrance takes (slide + overshoot + settle)")] public float BouncyArcDuration = 0.45f;

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Where each character flies in from, relative to its final position. Default: from off-screen left.")] public Vector3 BouncyArcStartOffset = new Vector3(-200f, 0f, 0f);

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Height of the arc each character travels along (positive = arc upward, then fall down into place)")] public float BouncyArcHeight = 80f;

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Starting rotation of each char in degrees. Default tilts forward, untilts as it lands.")] public float BouncyArcStartRotation = -45f;

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Starting scale of each character (0 = invisible point, 1 = full size)")] [Range(0f, 1f)] public float BouncyArcStartScale = 0f;

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Peak overshoot scale at landing (e.g. 1.3 = grows 30% larger then settles back). Creates the squash-and-stretch impact feel.")] public float BouncyArcOvershootScale = 1.3f;

        [Condition("AnimationMode", "SlideInPerCharBouncyArc")] [Tooltip("Squash factor at impact: X stretches wider, Y squashes shorter. (1,1) = no squash. (1.2, 0.85) = classic juicy squash.")] public Vector2 BouncyArcSquash = new Vector2(1.2f, 0.85f);

        #endregion

        public override float GetDuration() => Timing.WaitForCompletion ? DurationMode.GetDurationUI() : 0;
#if UNITY_EDITOR

        public override string GetDescription() => "Animate a TextMeshPro component with various effects including typewriter, fade, color change, scale punch, and slide in";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (TargetText == null) return "⚠ Text Target is not assigned. Please assign a TextMeshProUGUI component.";

            if (AnimationMode == TextAnimationMode.EmptyText && string.IsNullOrEmpty(TargetText.text)) return "⚠ EmptyText mode requires existing text in the TargetText component.";

            if (AnimationMode == ActionFlowText.TextAnimationMode.Typewriter && string.IsNullOrEmpty(TypewriterText)) return "⚠ Typewriter Text is empty. Please enter the text to reveal.";

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
            if (TargetText == null) yield break;

            switch (AnimationMode)
            {
                case TextAnimationMode.Typewriter:
                    yield return DoTypewriter();
                    break;
                case TextAnimationMode.FadeIn:
                    yield return DoFadeIn();
                    break;
                case TextAnimationMode.ColorChange:
                    yield return DoColorChange();
                    break;
                case TextAnimationMode.TypewriterWithPunch:
                    yield return DoTypewriterWithPunch();
                    break;
                case TextAnimationMode.Wave:
                    yield return DoWave();
                    break;
                case TextAnimationMode.Shake:
                    yield return DoShake();
                    break;
                case TextAnimationMode.Rainbow:
                    yield return DoRainbow();
                    break;
                case TextAnimationMode.Glitch:
                    yield return DoGlitch();
                    break;
                case TextAnimationMode.FadeInPerChar:
                    yield return DoFadeInPerChar();
                    break;
                case TextAnimationMode.SlideInPerChar:
                    yield return DoSlideInPerChar();
                    break;
                case TextAnimationMode.EmptyText:
                    yield return DoEmptyText();
                    break;
                case TextAnimationMode.SlideInPerCharAndWave:
                    yield return DoSlideInPerCharAndWave();
                    break;
                case TextAnimationMode.SlideInPerCharAndCurve:
                    yield return DoSlideInPerCharAndCurve();
                    break;
                case TextAnimationMode.SlideInPerCharBouncyArc:
                    yield return DoSlideInPerCharBouncyArc();
                    break;
                case TextAnimationMode.SlideInPerCharCartoonArc:
                    yield return DoSlideInPerCharCartoonArc();
                    break;
            }
        }

        private IEnumerator DoEmptyText()
        {
            TargetText.text = "";
            yield break;
        }

        private IEnumerator DoTypewriter()
        {
            string full = ReplaceText ? TypewriterText : TargetText.text;

            if (ClearOnStart) TargetText.text = "";

            float duration = DurationMode.GetDuration();
            float delay = duration / Mathf.Max(full.Length, 1);

            object waitFor = new WaitForSeconds(delay);
            if (Timing.TimeMode == ActionFlowTiming.UpdateMode.UnscaledUpdate)
            {
                waitFor = new WaitForSecondsRealtime(delay);
            }

            for (int i = 0; i <= full.Length; i++)
            {
                TargetText.text = full.Substring(0, i);
#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                yield return waitFor;
            }
        }

        private IEnumerator DoFadeIn()
        {
            Color c = TargetText.color;
            float duration = DurationMode.GetDuration();
            if (duration == 0)
            {
                c.a = FadeToAlpha;
                TargetText.color = c;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);

                c.a = Mathf.Lerp(FadeFromAlpha, FadeToAlpha, t);
                TargetText.color = c;
#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif

                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            c.a = FadeToAlpha;
            TargetText.color = c;
#if UNITY_EDITOR
            if (!Application.isPlaying) MarkDirty();
#endif
        }

        private IEnumerator DoColorChange()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                TargetText.color = Color.Lerp(FromColor, ToColor, t);
#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif

                elapsed += DeltaTime();
                yield return YieldInstruction;
            }

            TargetText.color = ToColor;
#if UNITY_EDITOR
            if (!Application.isPlaying) MarkDirty();
#endif
        }

        private IEnumerator DoTypewriterWithPunch()
        {
            string full = ReplaceText ? TypewriterPunchText : TargetText.text;

            if (PunchClearOnStart) TargetText.text = "";

            float originalSize = TargetText.fontSize;
            float punchSize = originalSize * PunchSizeMultiplier;

            float[] charSizes = new float[full.Length];
            bool[] charVisible = new bool[full.Length];

            for (int i = 0; i < full.Length; i++)
            {
                charSizes[i] = 0f;
                charVisible[i] = false;
            }

            TargetText.text = full;
            TargetText.ForceMeshUpdate();

            var activeCoroutines = new System.Collections.Generic.List<IEnumerator>();

            IEnumerator PunchChar(int index)
            {
                charVisible[index] = true;
                charSizes[index] = punchSize;

                float elapsed = 0f;
                while (elapsed < PunchDurationPerChar)
                {
                    float t = Mathf.Clamp01(elapsed / PunchDurationPerChar);
                    charSizes[index] = Mathf.LerpUnclamped(punchSize, originalSize, t);
                    elapsed += DeltaTime();
                    yield return YieldInstruction;
                }

                charSizes[index] = originalSize;
            }

            for (int i = 0; i < full.Length; i++)
            {
                activeCoroutines.Add(PunchChar(i));

                float waitElapsed = 0f;
                while (waitElapsed < DelayBetweenChars)
                {
                    for (int c = activeCoroutines.Count - 1; c >= 0; c--)
                    {
                        if (!activeCoroutines[c].MoveNext()) activeCoroutines.RemoveAt(c);
                    }

                    ApplyCharSizes(charSizes, charVisible);
#if UNITY_EDITOR
                    if (!Application.isPlaying) MarkDirty();
#endif
                    waitElapsed += DeltaTime();
                    yield return YieldInstruction;
                }
            }

            while (activeCoroutines.Count > 0)
            {
                for (int c = activeCoroutines.Count - 1; c >= 0; c--)
                {
                    if (!activeCoroutines[c].MoveNext()) activeCoroutines.RemoveAt(c);
                }

                ApplyCharSizes(charSizes, charVisible);
#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                yield return YieldInstruction;
            }
        }

        private void ApplyCharSizes(float[] charSizes, bool[] charVisible)
        {
            TargetText.ForceMeshUpdate();
            var textInfo = TargetText.textInfo;

            for (int i = 0; i < charSizes.Length && i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

                if (!charVisible[i])
                {
                    Vector3 center = (verts[vertexIndex] + verts[vertexIndex + 2]) / 2f;
                    verts[vertexIndex] = center;
                    verts[vertexIndex + 1] = center;
                    verts[vertexIndex + 2] = center;
                    verts[vertexIndex + 3] = center;
                    continue;
                }

                Vector3 charCenter = (verts[vertexIndex] + verts[vertexIndex + 1] + verts[vertexIndex + 2] + verts[vertexIndex + 3]) / 4f;

                float scale = charSizes[i] / TargetText.fontSize;

                for (int v = 0; v < 4; v++)
                {
                    verts[vertexIndex + v] = charCenter + (verts[vertexIndex + v] - charCenter) * scale;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        private IEnumerator DoWave()
        {
            float wavePeriod = (2f * Mathf.PI) / WaveSpeed;
            float duration = DurationMode.GetDuration();
            duration = Mathf.Round(duration / wavePeriod) * wavePeriod;
            if (duration <= 0f) duration = wavePeriod;

            float elapsed = 0f;
            float absoluteTime = 0f;
            TargetText.ForceMeshUpdate();

            while (elapsed < duration)
            {
                TargetText.ForceMeshUpdate();
                var textInfo = TargetText.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

                    float offset = Mathf.Sin((absoluteTime * WaveSpeed) + (i * WaveFrequency)) * WaveAmplitude;

                    for (int v = 0; v < 4; v++) verts[vertexIndex + v] += new Vector3(0, offset, 0);
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                float delta = DeltaTime();
                elapsed += delta;
                absoluteTime += delta;
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoShake()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                TargetText.ForceMeshUpdate();
                var textInfo = TargetText.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

                    Vector3 shake = new Vector3(UnityEngine.Random.Range(-ShakeIntensity, ShakeIntensity), UnityEngine.Random.Range(-ShakeIntensity, ShakeIntensity), 0);

                    for (int v = 0; v < 4; v++) verts[vertexIndex + v] += shake;
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoRainbow()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                TargetText.ForceMeshUpdate();
                var textInfo = TargetText.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    Color color = Color.HSVToRGB(Mathf.Repeat((elapsed * RainbowSpeed) + (i * RainbowFrequency), 1f), 1f, 1f);

                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                    for (int v = 0; v < 4; v++) colors[vertexIndex + v] = color;
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoGlitch()
        {
            float duration = DurationMode.GetDuration();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                TargetText.ForceMeshUpdate();
                var textInfo = TargetText.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    if (UnityEngine.Random.value < GlitchChance)
                    {
                        Vector3 glitch = new Vector3(UnityEngine.Random.Range(-GlitchIntensity, GlitchIntensity), UnityEngine.Random.Range(-GlitchIntensity * 0.5f, GlitchIntensity * 0.5f), 0);

                        for (int v = 0; v < 4; v++) verts[vertexIndex + v] += glitch;

                        if (GlitchColor)
                        {
                            Color32 glitchCol = new Color32((byte)UnityEngine.Random.Range(0, 255), (byte)UnityEngine.Random.Range(0, 255), (byte)UnityEngine.Random.Range(0, 255), 255);

                            for (int v = 0; v < 4; v++) colors[vertexIndex + v] = glitchCol;
                        }
                    }
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoFadeInPerChar()
        {
            TargetText.ForceMeshUpdate();
            var textInfo = TargetText.textInfo;
            int charCount = textInfo.characterCount;

            float[] charElapsed = new float[charCount];
            bool[] charStarted = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                charElapsed[i] = 0f;
                charStarted[i] = false;
            }

            TargetText.ForceMeshUpdate();
            for (int i = 0; i < charCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;
                int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                Color32[] colors = textInfo.meshInfo[meshIndex].colors32;
                for (int v = 0; v < 4; v++) colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, 0);
            }

            float totalElapsed = 0f;
            float totalDuration = FadePerCharDelay * charCount + FadePerCharDuration;

            while (totalElapsed < totalDuration)
            {
                TargetText.ForceMeshUpdate();
                textInfo = TargetText.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    if (totalElapsed >= i * FadePerCharDelay) charStarted[i] = true;

                    if (!charStarted[i]) continue;

                    charElapsed[i] += DeltaTime();
                    float t = Mathf.Clamp01(charElapsed[i] / FadePerCharDuration);

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    byte alpha = (byte)(t * 255);
                    for (int v = 0; v < 4; v++) colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, alpha);
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                totalElapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoSlideInPerChar()
        {
            TargetText.ForceMeshUpdate();
            var textInfo = TargetText.textInfo;
            int charCount = textInfo.characterCount;

            float[] charElapsed = new float[charCount];
            bool[] charStarted = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                charElapsed[i] = 0f;
                charStarted[i] = false;
            }

            float totalElapsed = 0f;
            float totalDuration = SlidePerCharDelay * charCount + SlidePerCharDuration;

            while (totalElapsed < totalDuration)
            {
                TargetText.ForceMeshUpdate();
                textInfo = TargetText.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    if (totalElapsed >= i * SlidePerCharDelay) charStarted[i] = true;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    if (!charStarted[i])
                    {
                        Vector3 center = (verts[vertexIndex] + verts[vertexIndex + 2]) / 2f;
                        for (int v = 0; v < 4; v++)
                        {
                            verts[vertexIndex + v] = center;
                            colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, 0);
                        }

                        continue;
                    }

                    charElapsed[i] += DeltaTime();
                    float t = Mathf.Clamp01(charElapsed[i] / SlidePerCharDuration);
                    float curve = SlidePerCharCurve.Evaluate(t);
                    byte alpha = (byte)(t * 255);

                    Vector3 offset = Vector3.LerpUnclamped(SlidePerCharOffset, Vector3.zero, curve);

                    for (int v = 0; v < 4; v++)
                    {
                        verts[vertexIndex + v] += offset;
                        colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, alpha);
                    }
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                totalElapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoSlideInPerCharAndWave()
        {
            TargetText.ForceMeshUpdate();
            var textInfo = TargetText.textInfo;
            int charCount = textInfo.characterCount;

            float[] charElapsed = new float[charCount];
            bool[] charStarted = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                charElapsed[i] = 0f;
                charStarted[i] = false;
            }

            float slideTotalDuration = SlidePerCharDelay * charCount + SlidePerCharDuration;

            float wavePeriod = (2f * Mathf.PI) / WaveSpeed;
            float waveDuration = DurationMode.GetDuration();
            waveDuration = Mathf.Round(waveDuration / wavePeriod) * wavePeriod;
            if (waveDuration <= 0f) waveDuration = wavePeriod;

            float totalDuration = slideTotalDuration + waveDuration;
            float totalElapsed = 0f;
            float absoluteTime = 0f;

            while (totalElapsed < totalDuration)
            {
                TargetText.ForceMeshUpdate();
                textInfo = TargetText.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    if (totalElapsed >= i * SlidePerCharDelay) charStarted[i] = true;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    if (!charStarted[i])
                    {
                        Vector3 center = (verts[vertexIndex] + verts[vertexIndex + 2]) / 2f;
                        for (int v = 0; v < 4; v++)
                        {
                            verts[vertexIndex + v] = center;
                            colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, 0);
                        }

                        continue;
                    }

                    charElapsed[i] += DeltaTime();
                    float t = Mathf.Clamp01(charElapsed[i] / SlidePerCharDuration);
                    float curve = SlidePerCharCurve.Evaluate(t);
                    byte alpha = (byte)(t * 255);

                    Vector3 slideOffset = Vector3.LerpUnclamped(SlidePerCharOffset, Vector3.zero, curve);

                    float waveBlend = Mathf.Clamp01(t);
                    float waveOffsetY = Mathf.Sin((absoluteTime * WaveSpeed) + (i * WaveFrequency)) * WaveAmplitude * waveBlend;

                    Vector3 totalOffset = slideOffset + new Vector3(0f, waveOffsetY, 0f);

                    for (int v = 0; v < 4; v++)
                    {
                        verts[vertexIndex + v] += totalOffset;
                        colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, alpha);
                    }
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                float delta = DeltaTime();
                totalElapsed += delta;
                absoluteTime += delta;
                yield return YieldInstruction;
            }
        }

        private IEnumerator DoSlideInPerCharAndCurve()
        {
            TargetText.ForceMeshUpdate();
            var textInfo = TargetText.textInfo;
            int charCount = textInfo.characterCount;

            float[] charElapsed = new float[charCount];
            bool[] charStarted = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                charElapsed[i] = 0f;
                charStarted[i] = false;
            }

            float totalElapsed = 0f;
            float totalDuration = SlidePerCharDelay * charCount + SlidePerCharDuration;

            float archDenom = Mathf.Max(charCount - 1, 1);
            float estimatedCharSpacing = Mathf.Max(TargetText.fontSize * 0.5f, 1f);

            while (totalElapsed < totalDuration)
            {
                TargetText.ForceMeshUpdate();
                textInfo = TargetText.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    if (totalElapsed >= i * SlidePerCharDelay) charStarted[i] = true;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    float archT = i / archDenom;
                    float archY = TextArchCurve.Evaluate(archT) * ArchHeight;
                    Vector3 archOffset = new Vector3(0f, archY, 0f);

                    float archAngleRad = 0f;
                    if (RotateAlongArch)
                    {
                        const float h = 0.01f;
                        float t0 = Mathf.Clamp01(archT - h);
                        float t1 = Mathf.Clamp01(archT + h);
                        float y0 = TextArchCurve.Evaluate(t0) * ArchHeight;
                        float y1 = TextArchCurve.Evaluate(t1) * ArchHeight;
                        float dx = (t1 - t0) * archDenom * estimatedCharSpacing;
                        if (dx > 0.0001f)
                        {
                            float slope = (y1 - y0) / dx;
                            archAngleRad = Mathf.Atan(slope) * RotationStrength;
                        }
                    }

                    if (!charStarted[i])
                    {
                        Vector3 center = (verts[vertexIndex] + verts[vertexIndex + 2]) / 2f;
                        for (int v = 0; v < 4; v++)
                        {
                            verts[vertexIndex + v] = center;
                            colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, 0);
                        }

                        continue;
                    }

                    charElapsed[i] += DeltaTime();
                    float t = Mathf.Clamp01(charElapsed[i] / SlidePerCharDuration);
                    float curve = SlidePerCharCurve.Evaluate(t);
                    byte alpha = (byte)(t * 255);

                    Vector3 slideOffset = Vector3.LerpUnclamped(SlidePerCharOffset, Vector3.zero, curve);
                    Vector3 archAppliedOffset = archOffset * curve;

                    float blendedAngle = archAngleRad * curve;
                    float cosA = Mathf.Cos(blendedAngle);
                    float sinA = Mathf.Sin(blendedAngle);

                    Vector3 charCenter = (verts[vertexIndex] + verts[vertexIndex + 1] + verts[vertexIndex + 2] + verts[vertexIndex + 3]) * 0.25f;

                    for (int v = 0; v < 4; v++)
                    {
                        Vector3 local = verts[vertexIndex + v] - charCenter;
                        float rx = local.x * cosA - local.y * sinA;
                        float ry = local.x * sinA + local.y * cosA;
                        verts[vertexIndex + v] = charCenter + new Vector3(rx, ry, local.z);

                        verts[vertexIndex + v] += slideOffset + archAppliedOffset;

                        colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, alpha);
                    }
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                totalElapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        // ============================================================
        //  Juicy mobile-game style entrance.
        //
        //  Per-character animation breakdown (timeline 0 → 1):
        //
        //   [0.00 → 0.70]  TRAVEL PHASE
        //      • Position lerps from BouncyArcStartOffset → 0
        //      • Y gets an extra arc bump (sin(πt)) so it flies in a curve, not a line
        //      • Rotation lerps from BouncyArcStartRotation → 0
        //      • Scale lerps from BouncyArcStartScale → 1 with overshoot easing
        //      • Alpha fades 0 → 255 quickly in the first ~30%
        //
        //   [0.70 → 0.85]  IMPACT PHASE
        //      • Position is at 0
        //      • Scale punches up to BouncyArcOvershootScale
        //      • Squash applied (X stretch, Y compress) — squash-and-stretch hit
        //
        //   [0.85 → 1.00]  SETTLE PHASE
        //      • Scale eases back to 1.0
        //      • Squash relaxes back to 1,1
        //      • Final resting state
        //
        //  Each char starts at index * BouncyArcDelay, so the chain is sequential.
        // ============================================================

        private IEnumerator DoSlideInPerCharCartoonArc()
        {
            TargetText.ForceMeshUpdate();

            var textInfo = TargetText.textInfo;
            int charCount = textInfo.characterCount;

            int[] visibleOrder = new int[charCount];
            int visibleCount = 0;

            for (int i = 0; i < charCount; i++)
            {
                if (textInfo.characterInfo[i].isVisible) visibleOrder[i] = visibleCount++;
                else visibleOrder[i] = -1;
            }

            float[] charElapsed = new float[charCount];
            bool[] charStarted = new bool[charCount];

            float totalElapsed = 0f;
            float totalDuration = CartoonArcDelay * Mathf.Max(visibleCount - 1, 0) + CartoonArcDuration;

            while (totalElapsed < totalDuration)
            {
                TargetText.ForceMeshUpdate();
                textInfo = TargetText.textInfo;

                float delta = DeltaTime();

                for (int i = 0; i < charCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    int order = visibleOrder[i];

                    if (totalElapsed >= order * CartoonArcDelay) charStarted[i] = true;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    Vector3 charCenter = (verts[vertexIndex] + verts[vertexIndex + 1] + verts[vertexIndex + 2] + verts[vertexIndex + 3]) * 0.25f;

                    // Hide before start
                    if (!charStarted[i])
                    {
                        for (int v = 0; v < 4; v++)
                        {
                            verts[vertexIndex + v] = charCenter;
                            colors[vertexIndex + v].a = 0;
                        }

                        continue;
                    }

                    charElapsed[i] += delta;

                    float t = Mathf.Clamp01(charElapsed[i] / CartoonArcDuration);

                    // 0 → 1 travel motion
                    float travelT = Mathf.Clamp01(t / 0.72f);
                    float travelEase = EaseOutCubic(travelT);

                    // Final word arc
                    float archT = visibleCount <= 1 ? 0.5f : order / (float)(visibleCount - 1);
                    float finalY = CartoonArcFinalCurve.Evaluate(archT) * CartoonArcFinalHeight;

                    // Final rotation follows the arc slope
                    float finalRotDeg = 0f;
                    {
                        const float h = 0.01f;

                        float t0 = Mathf.Clamp01(archT - h);
                        float t1 = Mathf.Clamp01(archT + h);

                        float y0 = CartoonArcFinalCurve.Evaluate(t0) * CartoonArcFinalHeight;
                        float y1 = CartoonArcFinalCurve.Evaluate(t1) * CartoonArcFinalHeight;

                        float slope = (y1 - y0) / Mathf.Max((t1 - t0) * TargetText.fontSize * 4f, 0.001f);
                        finalRotDeg = Mathf.Atan(slope) * Mathf.Rad2Deg * CartoonArcRotationStrength;
                    }

                    // Flying path: from left + arc bump
                    Vector3 startOffset = CartoonArcStartOffset + new Vector3(-order * 8f, 0f, 0f);
                    Vector3 linearOffset = Vector3.LerpUnclamped(startOffset, Vector3.zero, travelEase);

                    float arcBump = Mathf.Sin(travelT * Mathf.PI) * CartoonArcTravelHeight;

                    Vector3 positionOffset = linearOffset + new Vector3(0f, arcBump + finalY, 0f);

                    // Rotation
                    float rotDeg = Mathf.LerpUnclamped(CartoonArcStartRotation, finalRotDeg, travelEase);
                    float rotRad = rotDeg * Mathf.Deg2Rad;

                    // Scale + bounce
                    float scaleX;
                    float scaleY;

                    if (t < 0.72f)
                    {
                        float s = Mathf.LerpUnclamped(CartoonArcStartScale, 1f, travelEase);
                        scaleX = s;
                        scaleY = s;
                    }
                    else if (t < 0.86f)
                    {
                        float impactT = Mathf.InverseLerp(0.72f, 0.86f, t);
                        float squashAmount = 1f - impactT;

                        scaleX = CartoonArcOvershootScale * Mathf.LerpUnclamped(1f, CartoonArcSquash.x, squashAmount);
                        scaleY = CartoonArcOvershootScale * Mathf.LerpUnclamped(1f, CartoonArcSquash.y, squashAmount);
                    }
                    else
                    {
                        float settleT = Mathf.InverseLerp(0.86f, 1f, t);
                        float settleEase = EaseOutCubic(settleT);

                        float s = Mathf.LerpUnclamped(CartoonArcOvershootScale, 1f, settleEase);
                        scaleX = s;
                        scaleY = s;
                    }

                    byte alpha = (byte)(Mathf.Clamp01(t / 0.18f) * 255);

                    float cos = Mathf.Cos(rotRad);
                    float sin = Mathf.Sin(rotRad);

                    for (int v = 0; v < 4; v++)
                    {
                        Vector3 local = verts[vertexIndex + v] - charCenter;

                        // Scale
                        local.x *= scaleX;
                        local.y *= scaleY;

                        // Rotate
                        float rx = local.x * cos - local.y * sin;
                        float ry = local.x * sin + local.y * cos;

                        verts[vertexIndex + v] = charCenter + new Vector3(rx, ry, local.z) + positionOffset;
                        colors[vertexIndex + v].a = alpha;
                    }
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif

                totalElapsed += delta;
                yield return YieldInstruction;
            }
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private IEnumerator DoSlideInPerCharBouncyArc()
        {
            TargetText.ForceMeshUpdate();
            var textInfo = TargetText.textInfo;
            int charCount = textInfo.characterCount;

            float[] charElapsed = new float[charCount];
            bool[] charStarted = new bool[charCount];
            bool[] charDone = new bool[charCount];

            for (int i = 0; i < charCount; i++)
            {
                charElapsed[i] = 0f;
                charStarted[i] = false;
                charDone[i] = false;
            }

            float totalElapsed = 0f;
            float totalDuration = BouncyArcDelay * charCount + BouncyArcDuration;

            while (totalElapsed < totalDuration)
            {
                TargetText.ForceMeshUpdate();
                textInfo = TargetText.textInfo;

                for (int i = 0; i < charCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    if (totalElapsed >= i * BouncyArcDelay) charStarted[i] = true;

                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
                    Color32[] colors = textInfo.meshInfo[meshIndex].colors32;

                    // --- Pre-start: hide character (collapsed + invisible) ---
                    if (!charStarted[i])
                    {
                        Vector3 hideCenter = (verts[vertexIndex] + verts[vertexIndex + 2]) * 0.5f;
                        for (int v = 0; v < 4; v++)
                        {
                            verts[vertexIndex + v] = hideCenter;
                            colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, 0);
                        }

                        continue;
                    }

                    if (!charDone[i]) charElapsed[i] += DeltaTime();

                    float t = Mathf.Clamp01(charElapsed[i] / BouncyArcDuration);
                    if (t >= 1f) charDone[i] = true;

                    // ---------- Phase math ----------
                    // Travel goes 0 → 1 over the first 70% of the timeline.
                    float travelT = Mathf.Clamp01(t / 0.7f);
                    // Ease-out cubic for smooth landing
                    float travelEase = 1f - Mathf.Pow(1f - travelT, 3f);

                    // Position: from start offset to zero, plus an arc bump on Y
                    Vector3 linearOffset = Vector3.LerpUnclamped(BouncyArcStartOffset, Vector3.zero, travelEase);
                    // Sin(πx) gives a hump that's 0 at start, peaks at middle, 0 at end → arc shape
                    float arcBump = Mathf.Sin(travelT * Mathf.PI) * BouncyArcHeight;
                    Vector3 position = linearOffset + new Vector3(0f, arcBump, 0f);

                    // Rotation: from start angle to 0
                    float rotDeg = Mathf.LerpUnclamped(BouncyArcStartRotation, 0f, travelEase);
                    float rotRad = rotDeg * Mathf.Deg2Rad;

                    // Scale: ease from start scale up to overshoot during travel,
                    //        then settle back to 1 during impact+settle.
                    float scaleX, scaleY;
                    if (t < 0.7f)
                    {
                        // Travel: BouncyArcStartScale → BouncyArcOvershootScale
                        float baseScale = Mathf.LerpUnclamped(BouncyArcStartScale, BouncyArcOvershootScale, travelEase);
                        scaleX = baseScale;
                        scaleY = baseScale;
                    }
                    else if (t < 0.85f)
                    {
                        // Impact: hold at overshoot, apply squash (X stretches, Y compresses)
                        float impactT = Mathf.InverseLerp(0.7f, 0.85f, t);
                        // Squash peaks at the start of impact and relaxes toward end
                        float squashAmount = 1f - impactT;
                        scaleX = BouncyArcOvershootScale * Mathf.LerpUnclamped(1f, BouncyArcSquash.x, squashAmount);
                        scaleY = BouncyArcOvershootScale * Mathf.LerpUnclamped(1f, BouncyArcSquash.y, squashAmount);
                    }
                    else
                    {
                        // Settle: ease from overshoot scale → 1.0
                        float settleT = Mathf.InverseLerp(0.85f, 1f, t);
                        // Ease-out so it lands gently
                        float settleEase = 1f - Mathf.Pow(1f - settleT, 2f);
                        float s = Mathf.LerpUnclamped(BouncyArcOvershootScale, 1f, settleEase);
                        scaleX = s;
                        scaleY = s;
                    }

                    // Alpha: fade in quickly during the first 30% of travel
                    float alphaT = Mathf.Clamp01(t / 0.3f);
                    byte alpha = (byte)(alphaT * 255);

                    // ---------- Apply to the 4 verts ----------
                    Vector3 charCenter = (verts[vertexIndex] + verts[vertexIndex + 1] + verts[vertexIndex + 2] + verts[vertexIndex + 3]) * 0.25f;

                    float cosR = Mathf.Cos(rotRad);
                    float sinR = Mathf.Sin(rotRad);

                    for (int v = 0; v < 4; v++)
                    {
                        // 1. Translate to local space (origin = char center)
                        Vector3 local = verts[vertexIndex + v] - charCenter;

                        // 2. Apply scale (with squash)
                        local.x *= scaleX;
                        local.y *= scaleY;

                        // 3. Apply rotation
                        float rx = local.x * cosR - local.y * sinR;
                        float ry = local.x * sinR + local.y * cosR;

                        // 4. Translate back, then apply position offset
                        verts[vertexIndex + v] = charCenter + new Vector3(rx, ry, local.z) + position;

                        colors[vertexIndex + v] = new Color32(colors[vertexIndex + v].r, colors[vertexIndex + v].g, colors[vertexIndex + v].b, alpha);
                    }
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                    TargetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) MarkDirty();
#endif
                totalElapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }
    }
}