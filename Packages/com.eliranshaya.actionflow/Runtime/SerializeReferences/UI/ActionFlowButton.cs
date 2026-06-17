using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    [Serializable]
    public class ActionFlowButton : ActionFlow
    {
        public enum ButtonState
        {
            Enable,
            Disable,
            Toggle
        }

        [Serializable]
        public class ButtonHelper
        {
            [Tooltip("The Button component to enable/disable")]
            public Button Button;

            [Tooltip("Enable: make the button interactable\nDisable: make the button non-interactable\nToggle: switch current interactable state")]
            public ButtonState ButtonState;
        }

        [InspectorGroup("Button Control", 12, false)]
        [Tooltip("List of Buttons and their target interactable states")]
        public ButtonHelper[] ButtonHelpers = new ButtonHelper[1];

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() => "Enable, disable, or toggle the interactable state of UI buttons";

        public override string GetCategory() => "UI";

        public override string GetWarningMessage()
        {
            if (ButtonHelpers == null || ButtonHelpers.Length == 0)
                return "⚠ Button Helpers array is empty. Please assign at least one Button.";

            for (int i = 0; i < ButtonHelpers.Length; i++)
            {
                if (ButtonHelpers[i] == null || ButtonHelpers[i].Button == null)
                    return $"⚠ Button at index {i} is not assigned. Please assign a Button component.";
            }

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (ButtonHelpers == null || ButtonHelpers.Length == 0)
            {
                yield break;
            }

            foreach (var helper in ButtonHelpers)
            {
                if (helper == null || helper.Button == null)
                {
                    continue;
                }

                switch (helper.ButtonState)
                {
                    case ButtonState.Enable:
                        helper.Button.interactable = true;
                        break;

                    case ButtonState.Disable:
                        helper.Button.interactable = false;
                        break;

                    case ButtonState.Toggle:
                        helper.Button.interactable = !helper.Button.interactable;
                        break;
                }
            }

            yield break;
        }
    }
}