using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowParticleSystem : ActionFlow
    {
        #region Variables

        public enum ParticleAction
        {
            Play,
            Stop,
            Pause,
            Resume,
        }

        [InspectorGroup("Particle", 12, false)]
        [Tooltip("The ParticleSystem to control")]
        public ParticleSystem TargetParticle;

        [Tooltip("The action to perform on the ParticleSystem")]
        public ParticleAction Action = ParticleAction.Play;

        [Condition("Action", "Stop")]
        [Tooltip("If true, stops the particle system with children as well")]
        public bool WithChildren = true;

        [Condition("Action", "Stop")]
        [Tooltip("If true, clears all existing particles on stop")]
        public bool ClearOnStop = true;

        [Condition("Action", "Play")]
        [Tooltip("If true, resets the particle system before playing")]
        public bool ResetOnPlay = false;

        #endregion

        public override float GetDuration() => 0f;
#if UNITY_EDITOR

        public override string GetDescription() =>
            "Control a ParticleSystem — Play, Stop, Pause, or Resume";

        public override string GetCategory() => "ParticleSystem";

        public override string GetWarningMessage()
        {
            if (TargetParticle == null)
                return "⚠ Target Particle is not assigned. Please assign a ParticleSystem component.";
            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (TargetParticle == null)
            {
                yield break;
            }

            switch (Action)
            {
                case ParticleAction.Play:
                    if (ResetOnPlay) TargetParticle.Clear();
                    TargetParticle.Play(WithChildren);
                    break;

                case ParticleAction.Stop:
                    TargetParticle.Stop(WithChildren,
                        ClearOnStop ? ParticleSystemStopBehavior.StopEmittingAndClear
                            : ParticleSystemStopBehavior.StopEmitting);
                    break;

                case ParticleAction.Pause:
                    TargetParticle.Pause(WithChildren);
                    break;

                case ParticleAction.Resume:
                    TargetParticle.Play(WithChildren);
                    break;
            }
        }
    }
}