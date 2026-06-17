using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowBreak : ActionFlow
    {
        [InspectorGroup("Break Settings", 12, false)]
        [Tooltip("A custom label to identify this break point in the console")]
        public string BreakLabel = "Break Point";

        [Tooltip("If true, the break will only trigger in the Unity Editor (ignored in builds)")]
        public bool EditorOnly = true;

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "This action flow will force a break, pausing the editor";
        }

        public override string GetCategory()
        {
            return "Debug";
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
#if UNITY_EDITOR
            if (EditorOnly || !Application.isPlaying)
            {
                Debug.Log($"<color=yellow>⏸ Break Point Hit: {BreakLabel}</color>");
                Debug.Break();
            }
#else
            if (!EditorOnly)
            {
                Debug.Log($"<color=yellow>⏸ Break Point (Runtime): {BreakLabel}</color>");
            }
#endif
            yield break;
        }
    }
}