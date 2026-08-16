using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        /// <summary>
        /// The player loop point this action resumes on, the UniTask equivalent of the old YieldInstruction.
        /// </summary>
        protected PlayerLoopTiming LoopTiming { get; private set; } = PlayerLoopTiming.Update;

        /// <summary>
        /// Whether timed waits made by this action ignore Time.timeScale.
        /// </summary>
        protected DelayType DelayMode { get; private set; } = DelayType.DeltaTime;

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
                LoopTiming = PlayerLoopTiming.Update;
                DelayMode = DelayType.DeltaTime;
                return;
            }

            switch (Timing.TimeMode)
            {
                case ActionFlowTiming.UpdateMode.EndOfFrame:
                    DeltaTime = () => Time.deltaTime;
                    LoopTiming = PlayerLoopTiming.LastPostLateUpdate;
                    DelayMode = DelayType.DeltaTime;
                    break;

                case ActionFlowTiming.UpdateMode.FixedUpdate:
                    DeltaTime = () => Time.fixedDeltaTime;
                    LoopTiming = PlayerLoopTiming.FixedUpdate;
                    DelayMode = DelayType.DeltaTime;
                    break;

                case ActionFlowTiming.UpdateMode.UnscaledUpdate:
                    DeltaTime = () => Time.unscaledDeltaTime;
                    LoopTiming = PlayerLoopTiming.Update;
                    DelayMode = DelayType.UnscaledDeltaTime;
                    break;

                default:
                    DeltaTime = () => Time.deltaTime;
                    LoopTiming = PlayerLoopTiming.Update;
                    DelayMode = DelayType.DeltaTime;
                    break;
            }
        }

        /// <summary>
        /// Suspends until the next tick of <see cref="LoopTiming"/>, the replacement for
        /// <c>yield return YieldInstruction</c>. Backed by UniTask's pooled promises, so it does not
        /// allocate per frame the way the old coroutine yield instructions did.
        /// </summary>
        protected UniTask NextFrame(CancellationToken cancellationToken)
        {
            return UniTask.Yield(LoopTiming, cancellationToken);
        }

        protected UniTask WaitForDuration(float duration, CancellationToken cancellationToken)
        {
            return duration <= 0f
                ? UniTask.CompletedTask
                : WaitForDurationAsync(duration, cancellationToken);
        }

        private async UniTask WaitForDurationAsync(float duration, CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += DeltaTime();
                await NextFrame(cancellationToken);
            }
        }

        private UniTask HandleTimingAndRepeats(CancellationToken cancellationToken)
        {
            // Fast path: no timing at all, or timing with nothing to schedule around the action.
            // Skipping the async state machine here keeps the common case allocation free.
            if (Timing == null ||
                (Timing.InitialDelay <= 0f && !Timing.RepeatForever && Timing.NumberOfRepeats <= 0))
            {
                return CustomExecutionAsync(cancellationToken);
            }

            return HandleTimingAndRepeatsAsync(cancellationToken);
        }

        private async UniTask HandleTimingAndRepeatsAsync(CancellationToken cancellationToken)
        {
            if (Timing.InitialDelay > 0f)
            {
                await WaitForDurationAsync(Timing.InitialDelay, cancellationToken);
            }

            if (Timing.RepeatForever)
            {
                // Runs until the cancellation token stops it.
                while (true)
                {
                    await CustomExecutionAsync(cancellationToken);

                    if (Timing.DelayBetweenRepeats > 0f)
                    {
                        await WaitForDurationAsync(Timing.DelayBetweenRepeats, cancellationToken);
                    }
                }
            }
            else if (Timing.NumberOfRepeats > 0)
            {
                for (int i = 0; i <= Timing.NumberOfRepeats; i++)
                {
                    await CustomExecutionAsync(cancellationToken);

                    if (i < Timing.NumberOfRepeats && Timing.DelayBetweenRepeats > 0f)
                    {
                        await WaitForDurationAsync(Timing.DelayBetweenRepeats, cancellationToken);
                    }
                }
            }
            else
            {
                await CustomExecutionAsync(cancellationToken);
            }
        }

        public virtual UniTask ExecutionAsync(CancellationToken cancellationToken)
        {
            if (!Active)
            {
                return UniTask.CompletedTask;
            }

            if (Chance < 100f)
            {
                float random = UnityEngine.Random.Range(0f, 100f);
                if (random > Chance)
                {
                    return UniTask.CompletedTask;
                }
            }

            InitializeUpdateModeSettings();

            return HandleTimingAndRepeats(cancellationToken);
        }

        protected abstract UniTask CustomExecutionAsync(CancellationToken cancellationToken);
    }
}