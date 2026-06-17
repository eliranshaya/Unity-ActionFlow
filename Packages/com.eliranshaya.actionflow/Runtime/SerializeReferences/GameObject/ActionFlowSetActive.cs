using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowSetActive : ActionFlow
    {
        public enum SetActiveState
        {
            Active,
            Inactive,
            Toggle
        }

        [Serializable]
        public class SetActiveHelper
        {
            [Tooltip("The GameObject to set active/inactive")]
            public GameObject GameObject;

            [Tooltip("Active: enable the GameObject\nInactive: disable the GameObject\nToggle: switch current state")]
            public SetActiveState SetActiveState;
        }

        [InspectorGroup("Set Active", 12, false)]
        [Tooltip("List of GameObjects and their target active states")]
        public SetActiveHelper[] SetActiveHelpers = new SetActiveHelper[1];

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription()
        {
            return "Enable, disable, or toggle the active state of GameObjects";
        }

        public override string GetCategory()
        {
            return "GameObject";
        }

        public override string GetWarningMessage()
        {
            if (SetActiveHelpers == null || SetActiveHelpers.Length == 0)
                return "⚠ No Set Active Helpers assigned. Please add at least one entry.";

            bool hasNullGameObject = false;
            foreach (var helper in SetActiveHelpers)
            {
                if (helper != null && helper.GameObject == null)
                {
                    hasNullGameObject = true;
                    break;
                }
            }

            if (hasNullGameObject)
                return "⚠ One or more entries are missing a GameObject reference.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (SetActiveHelpers == null || SetActiveHelpers.Length == 0)
            {
                yield break;
            }

            foreach (var helper in SetActiveHelpers)
            {
                if (helper == null || helper.GameObject == null)
                {
                    continue;
                }

                switch (helper.SetActiveState)
                {
                    case SetActiveState.Active:
                        helper.GameObject.SetActive(true);
                        break;

                    case SetActiveState.Inactive:
                        helper.GameObject.SetActive(false);
                        break;

                    case SetActiveState.Toggle:
                        helper.GameObject.SetActive(!helper.GameObject.activeSelf);
                        break;
                }
            }

            yield break;
        }
    }
}