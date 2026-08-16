using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowInstantiate : ActionFlow
    {
        #region Variables

        [InspectorGroup("Instantiate", 12, false)]
        [Tooltip("The GameObject prefab to instantiate")]
        public GameObject Prefab;

        [Tooltip("The parent transform to instantiate the object under. If null, nothing will be instantiated")]
        public Transform Parent;

        [Tooltip("If true, resets the local position and rotation to zero after instantiating under the parent")]
        public bool ResetLocalTransform = true;

        [Condition("ResetLocalTransform", false)]
        [Tooltip("Local position offset to apply after instantiating")]
        public Vector3 LocalPositionOffset = Vector3.zero;

        [Condition("ResetLocalTransform", false)]
        [Tooltip("Local rotation offset to apply after instantiating")]
        public Vector3 LocalRotationOffset = Vector3.zero;

        #endregion

#if UNITY_EDITOR
        public override float GetDuration() => 0f;

        public override string GetDescription() =>
            "Instantiate a GameObject prefab under a parent transform";

        public override string GetCategory() => "GameObject";

        public override string GetWarningMessage()
        {
            if (Prefab == null && Parent == null)
                return "⚠ Prefab and Parent are not assigned. Please assign both.";

            if (Prefab == null)
                return "⚠ Prefab is not assigned. Please assign a GameObject prefab.";

            if (Parent == null)
                return "⚠ Parent is not assigned. The object will not instantiate at runtime.";

            return null;
        }
#endif

        protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
        {
            if (Parent == null || Prefab == null)
            {
                return UniTask.CompletedTask;
            }

            GameObject instance = UnityEngine.Object.Instantiate(Prefab, Parent);
            if (ResetLocalTransform)
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }
            else
            {
                instance.transform.localPosition = LocalPositionOffset;
                instance.transform.localRotation = Quaternion.Euler(LocalRotationOffset);
            }

            return UniTask.CompletedTask;
        }
    }
}