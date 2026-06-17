using System;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowTiming
    {
        public enum UpdateMode
        {
            Update,
            FixedUpdate,
            EndOfFrame,
            UnscaledUpdate
        }

        [Header("TimeScale")]
        public UpdateMode TimeMode = UpdateMode.Update;

        [Header("Execution")]
        [Tooltip("if false, the next action will start immediately without waiting for this one to finish")]
        public bool WaitForCompletion = true;

        [Header("Delays")]
        [Tooltip("the initial delay to apply before playing the delay (in seconds)")]
        public float InitialDelay = 0f;

        [Header("Repeat")]
        [Tooltip("the repeat mode, whether the feedback should be played once, multiple times, or forever")]
        public int NumberOfRepeats = 0;
        [Tooltip("if this is true, the feedback will be repeated forever")]
        public bool RepeatForever = false;
        [Tooltip("the delay (in seconds) between two firings of this feedback. This doesn't include the duration of the feedback.")]
        public float DelayBetweenRepeats = 1f;
    }
}