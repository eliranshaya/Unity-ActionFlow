using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    [Serializable]
    public class ActionFlowImageRaycast : ActionFlow
    {
        #region Variables

        [InspectorGroup("Image Raycast", 12, false)]
        [Tooltip("The Image component to change raycast target on")]
        public Image TargetImage;

        [Tooltip("The raycast target value to set")]
        public bool RaycastTarget = true;

        #endregion

#if UNITY_EDITOR
        public override float GetDuration() => 0;

        public override string GetDescription() =>
            "Enable or disable the raycast target on a UI Image component";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (TargetImage == null)
                return "⚠ Target Image is not assigned.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetImage == null) yield break;

            bool original = TargetImage.raycastTarget;
            TargetImage.raycastTarget = RaycastTarget;

            yield break;
        }
    }
}