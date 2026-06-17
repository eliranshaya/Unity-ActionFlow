using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowAnimationPlay : ActionFlow
    {
        #region Variables

        [InspectorGroup("Animator", 12, false)]
        [Tooltip("The Animator to control")]
        public Animator TargetAnimator;

        [AnimatorState("TargetAnimator")]
        [Tooltip("The name of the animation state to play")]
        public string StateName = "";

        [Tooltip("The layer index on which the state is located (-1 for any layer)")]
        public int LayerIndex = -1;

        [Tooltip("The normalized time at which to start the animation (0 = start, 1 = end)")]
        [Range(0f, 1f)]
        public float NormalizedTime = 0f;

        [InspectorGroup("Wait For Completion", 14, false)]
        [Tooltip("If true, waits until the animation state has finished playing before continuing")]
        public bool WaitForCompletion = false;

        #endregion

        public override float GetDuration()
        {
            if (!WaitForCompletion) return 0f;
            if (TargetAnimator == null) return 0f;
            if (string.IsNullOrEmpty(StateName)) return 0f;

            int stateHash = Animator.StringToHash(StateName);
            foreach (var clip in TargetAnimator.runtimeAnimatorController.animationClips)
            {
                if (Animator.StringToHash(clip.name) == stateHash)
                {
                    return clip.isLooping ? 0f : clip.length * (1f - NormalizedTime);
                }
            }

            return 0f;
        }

#if UNITY_EDITOR

        public override string GetDescription() => "Play an animation state by name on an Animator, with optional wait for completion";

        public override string GetCategory() => "Animation";

        public override string GetWarningMessage()
        {
            if (TargetAnimator == null)
                return "⚠ Target Animator is not assigned. Please assign an Animator component.";

            if (string.IsNullOrEmpty(StateName))
                return "⚠ State Name is empty. Please enter the name of the animation state to play.";

            var controller = TargetAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controller != null)
            {
                int stateHash = Animator.StringToHash(StateName);
                int layerIndex = LayerIndex < 0 ? 0 : LayerIndex;

                foreach (var state in controller.layers[layerIndex].stateMachine.states)
                {
                    if (state.state.nameHash == stateHash && state.state.motion is AnimationClip clip)
                    {
                        if (clip.isLooping && WaitForCompletion)
                            return "⚠ The animation clip is looping — WaitForCompletion will have no effect.";

                        break;
                    }
                }
            }

            return null;
        }

        public string[] GetAnimatorStateNames()
        {
            if (TargetAnimator == null) return Array.Empty<string>();

            var controller = TargetAnimator.runtimeAnimatorController
                as UnityEditor.Animations.AnimatorController;
            if (controller == null) return Array.Empty<string>();

            int layerIndex = LayerIndex < 0 ? 0 : LayerIndex;
            var states = controller.layers[layerIndex].stateMachine.states;

            string[] names = new string[states.Length];
            for (int i = 0; i < states.Length; i++)
                names[i] = states[i].state.name;

            return names;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetAnimator == null || string.IsNullOrEmpty(StateName))
            {
                yield break;
            }

            int stateHash = Animator.StringToHash(StateName);
            TargetAnimator.Play(stateHash, LayerIndex, NormalizedTime);

            if (!WaitForCompletion)
            {
                yield break;
            }

            yield return YieldInstruction;

            int layer = LayerIndex < 0 ? 0 : LayerIndex;
            AnimatorClipInfo[] clipInfos = TargetAnimator.GetCurrentAnimatorClipInfo(layer);

            if (clipInfos.Length > 0)
            {
                AnimationClip clip = clipInfos[0].clip;
                float waitTime = clip.isLooping ? 0f : clip.length * (1f - NormalizedTime);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}