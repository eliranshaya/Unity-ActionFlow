using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowDestroy : ActionFlow
    {
        #region Variables

        [InspectorGroup("Destroy", 12, false)]
        [Tooltip("The GameObjects to destroy")]
        public GameObject[] Targets;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Destroy an array of GameObjects with an optional delay";

        public override string GetCategory() => "GameObject";

        public override string GetWarningMessage()
        {
            if (Targets == null || Targets.Length == 0)
                return "⚠ Targets array is empty. Please assign at least one GameObject to destroy.";

            for (int i = 0; i < Targets.Length; i++)
            {
                if (Targets[i] == null)
                    return $"⚠ Target at index {i} is null. Please assign a valid GameObject.";
            }

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (Targets == null || Targets.Length == 0)
            {
                yield break;
            }

            foreach (var target in Targets)
            {
                if (target == null)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(target);
            }
        }
    }
}