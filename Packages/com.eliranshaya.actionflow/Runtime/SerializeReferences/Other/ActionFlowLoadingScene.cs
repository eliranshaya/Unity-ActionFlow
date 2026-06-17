using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    [Serializable]
    public class ActionFlowLoadingScene : ActionFlow
    {
        #region Variables

        public enum LoadSceneMode
        {
            Single,
            Additive,
        }

        public enum SceneReferenceMode
        {
            ByName,
            ByIndex,
        }

        [InspectorGroup("Scene", 12, false)]
        [Tooltip("Whether to reference the scene by name or by build index")]
        public SceneReferenceMode ReferenceMode = SceneReferenceMode.ByName;

        [Condition("ReferenceMode", "ByName")]
        [Tooltip("The name of the scene to load")]
        public string SceneName = "";

        [Condition("ReferenceMode", "ByIndex")]
        [Tooltip("The build index of the scene to load")]
        public int SceneIndex = 0;

        [Tooltip("How the scene should be loaded")]
        public LoadSceneMode LoadMode = LoadSceneMode.Single;

        [InspectorGroup("Loading", 12, false)]
        [Tooltip("If true, the scene will be loaded asynchronously")]
        public bool Async = true;

        [Condition("Async", true)]
        [Tooltip("If true, waits until the scene is fully loaded before completing")]
        public bool WaitUntilLoaded = true;

        [Condition("Async", true)]
        [Tooltip("If true, the scene will not activate automatically — you control when it activates")]
        public bool ManualActivation = false;

        [Condition("ManualActivation", true)]
        [Tooltip("Delay in seconds before activating the scene after it has finished loading")]
        public float ActivationDelay = 0f;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Load a Unity scene by name or build index, with support for async loading and manual activation";

        public override string GetCategory() => "Scene";

        public override string GetWarningMessage()
        {
            if (ReferenceMode == SceneReferenceMode.ByName && string.IsNullOrEmpty(SceneName))
                return "⚠ Scene Name is empty. Please enter a valid scene name.";

            if (ReferenceMode == SceneReferenceMode.ByIndex && SceneIndex < 0)
                return "⚠ Scene Index is invalid. Please enter a valid build index.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            UnityEngine.SceneManagement.LoadSceneMode unityLoadMode = LoadMode == LoadSceneMode.Additive
                ? UnityEngine.SceneManagement.LoadSceneMode.Additive
                : UnityEngine.SceneManagement.LoadSceneMode.Single;

            if (!Async)
            {
                if (ReferenceMode == SceneReferenceMode.ByName)
                {
                    SceneManager.LoadScene(SceneName, unityLoadMode);
                }
                else
                {
                    SceneManager.LoadScene(SceneIndex, unityLoadMode);
                }

                yield break;
            }

            AsyncOperation operation = ReferenceMode == SceneReferenceMode.ByName
                ? SceneManager.LoadSceneAsync(SceneName, unityLoadMode)
                : SceneManager.LoadSceneAsync(SceneIndex, unityLoadMode);

            if (operation == null)
            {
                yield break;
            }

            if (ManualActivation)
            {
                operation.allowSceneActivation = false;

                while (operation.progress < 0.9f)
                {
                    yield return YieldInstruction;
                }

                if (ActivationDelay > 0f)
                {
                    yield return new WaitForSeconds(ActivationDelay);
                }

                operation.allowSceneActivation = true;
            }

            if (WaitUntilLoaded)
            {
                while (!operation.isDone)
                {
                    yield return YieldInstruction;
                }
            }
        }
    }
}