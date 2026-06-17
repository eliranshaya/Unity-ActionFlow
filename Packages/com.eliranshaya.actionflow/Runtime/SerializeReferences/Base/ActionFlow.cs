using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    public enum TransformSpace
    {
        World,
        Local
    }
    public enum DurationType
    {
        Absolute,
        RandomBetweenValues
    }
    [Serializable]
    public class DurationMode
    {
        [Tooltip("Determines whether to use a fixed or random duration")]
        public DurationType DurationType = DurationType.Absolute;

        [Condition("DurationType", "Absolute")]
        [Tooltip("The duration of the animation (in seconds)")]
        public float Duration = 1f;

        [Condition("DurationType", "RandomBetweenValues")]
        [Tooltip("The minimum duration when using Random mode (in seconds)")]
        public float MinDuration = 0.5f;

        [Condition("DurationType", "RandomBetweenValues")]
        [Tooltip("The maximum duration when using Random mode (in seconds)")]
        public float MaxDuration = 2f;

        public float GetDurationUI() => DurationType == DurationType.Absolute ? Duration : (MinDuration + MaxDuration) / 2;
        public float GetDuration() => DurationType == DurationType.Absolute ? Duration : UnityEngine.Random.Range(MinDuration, MaxDuration);
    }
    [Serializable]
    public class ActionFlowOverride
    {
        public int Index;
        public Action<ActionFlow> Apply;
    }
    [Serializable]
    public abstract class ActionFlow : ISerializationCallbackReceiver
    {
        [Tooltip("whether or not this feedback is active")]
        public bool Active = true;

        [Tooltip("the name of this feedback to display in the inspector")]
        public string Label;

        [Tooltip("the chance of this feedback in percentages, 0% to 100%")]
        [Range(0, 100)] public float Chance = 100;

        [Tooltip("a number of timing related values")]
        public ActionFlowTiming Timing;

        protected Func<float> DeltaTime { get; private set; }
        protected YieldInstruction YieldInstruction { get; private set; }

        protected ActionFlow()
        {
#if UNITY_EDITOR
            InitializeLabel();
#endif
        }

        public void OnBeforeSerialize()
        {
            // This is called before Unity serializes the object
        }

        public void OnAfterDeserialize()
        {
            // This is called after Unity deserializes the object
            // Check if label is empty or was never set
            if (string.IsNullOrEmpty(Label))
            {
                InitializeLabel();
            }
        }

        private void InitializeLabel()
        {
            string typeName = GetType().Name;
            if (typeName.StartsWith("ActionFlow"))
            {
                typeName = typeName.Substring("ActionFlow".Length);
            }

            Label = NicifyTypeName(typeName);
        }

        private string NicifyTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return "";

            string result = "";
            for (int i = 0; i < typeName.Length; i++)
            {
                if (i > 0 && char.IsUpper(typeName[i]) && !char.IsUpper(typeName[i - 1]))
                {
                    result += " ";
                }

                result += typeName[i];
            }

            return result;
        }

        #region Editor:

        public virtual float GetDuration() => 0;
#if UNITY_EDITOR

        /// <summary>
        /// Override this in derived classes to provide a description that will be shown in the inspector
        /// </summary>
        public virtual string GetDescription()
        {
            return string.Empty;
        }

        /// <summary>
        /// Override this in derived classes to specify the category in the Add ActionFlow menu
        /// </summary>
        public virtual string GetCategory()
        {
            return "Other";
        }

        public virtual string GetWarningMessage()
        {
            return null;
        }
#endif

        #endregion

        private void InitializeUpdateModeSettings()
        {
            if (Timing == null)
            {
                DeltaTime = () => Time.deltaTime;
                YieldInstruction = null;
                return;
            }

            switch (Timing.TimeMode)
            {
                case ActionFlowTiming.UpdateMode.EndOfFrame:
                    DeltaTime = () => Time.deltaTime;
                    YieldInstruction = new WaitForEndOfFrame();
                    break;

                case ActionFlowTiming.UpdateMode.FixedUpdate:
                    DeltaTime = () => Time.fixedDeltaTime;
                    YieldInstruction = new WaitForFixedUpdate();
                    break;

                case ActionFlowTiming.UpdateMode.UnscaledUpdate:
                    DeltaTime = () => Time.unscaledDeltaTime;
                    YieldInstruction = null;
                    break;

                default:
                    DeltaTime = () => Time.deltaTime;
                    YieldInstruction = null;
                    break;
            }
        }

        protected IEnumerator WaitForDuration(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += DeltaTime();
                yield return YieldInstruction;
            }
        }

        private IEnumerator HandleTimingAndRepeats()
        {
            if (Timing == null)
            {
                yield return CustomExecutionCoroutine();
                yield break;
            }

            if (Timing.InitialDelay > 0f)
            {
                yield return WaitForDuration(Timing.InitialDelay);
            }

            if (Timing.RepeatForever)
            {
                while (true)
                {
                    yield return CustomExecutionCoroutine();

                    if (Timing.DelayBetweenRepeats > 0f)
                    {
                        yield return WaitForDuration(Timing.DelayBetweenRepeats);
                    }
                }
            }
            else if (Timing.NumberOfRepeats > 0)
            {
                for (int i = 0; i <= Timing.NumberOfRepeats; i++)
                {
                    yield return CustomExecutionCoroutine();

                    if (i < Timing.NumberOfRepeats && Timing.DelayBetweenRepeats > 0f)
                    {
                        yield return WaitForDuration(Timing.DelayBetweenRepeats);
                    }
                }
            }
            else
            {
                yield return CustomExecutionCoroutine();
            }
        }

        public virtual IEnumerator ExecutionCoroutine()
        {
            if (!Active)
            {
                yield break;
            }

            if (Chance < 100f)
            {
                float random = UnityEngine.Random.Range(0f, 100f);
                if (random > Chance)
                {
                    yield break;
                }
            }

            InitializeUpdateModeSettings();

            yield return HandleTimingAndRepeats();
        }

        protected abstract IEnumerator CustomExecutionCoroutine();
    }
}