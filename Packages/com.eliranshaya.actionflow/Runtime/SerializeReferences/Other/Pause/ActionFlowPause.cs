using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowPause : ActionFlow
    {
        [InspectorGroup("Pause Settings", 12, false)]
        [Tooltip("if this is true, the pause duration will be randomized between min and max")]
        public bool RandomizePauseDuration = false;

        [Condition("RandomizePauseDuration", false)]
        [Tooltip("the duration of the pause, in seconds")]
        public float PauseDuration = 1f;

        [Condition("RandomizePauseDuration", true)]
        [Tooltip("the minimum pause duration (when randomizing)")]
        public float MinPauseDuration = 0.5f;

        [Condition("RandomizePauseDuration", true)]
        [Tooltip("the maximum pause duration (when randomizing)")]
        public float MaxPauseDuration = 2f;

        [Condition("RandomizePauseDuration", true)]
        [Tooltip("if true, randomize on every play. if false, randomize only once")]
        public bool RandomizeOnEachPlay = true;

        private float _randomizedDuration = -1f;

        public override float GetDuration()
        {
            if (RandomizePauseDuration)
            {
                if (RandomizeOnEachPlay)
                {
                    return (MinPauseDuration + MaxPauseDuration) / 2f;
                }
                else
                {
                    if (_randomizedDuration > 0)
                    {
                        return _randomizedDuration;
                    }

                    return (MinPauseDuration + MaxPauseDuration) / 2f;
                }
            }

            return PauseDuration;
        }
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "This action flow will force a break, pausing the editor";
        }
        public override string GetCategory()
        {
            return "Pause";
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            float actualDuration = PauseDuration;
            if (RandomizePauseDuration)
            {
                if (RandomizeOnEachPlay)
                {
                    actualDuration = UnityEngine.Random.Range(MinPauseDuration, MaxPauseDuration);
                }
                else
                {
                    if (_randomizedDuration < 0)
                    {
                        _randomizedDuration = UnityEngine.Random.Range(MinPauseDuration, MaxPauseDuration);
                    }

                    actualDuration = _randomizedDuration;
                }
            }

            return WaitForDuration(actualDuration, cancellationToken);
        }
    }
}