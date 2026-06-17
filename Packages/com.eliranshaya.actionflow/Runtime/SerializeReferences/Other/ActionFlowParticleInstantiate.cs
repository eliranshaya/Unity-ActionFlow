using System;
using System.Collections;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class ActionFlowParticleInstantiate : ActionFlow
    {
        #region Variables

        [InspectorGroup("Particle", 12, false)]
        [Tooltip("The ParticleSystem prefab to instantiate")]
        public ParticleSystem ParticlePrefab;

        [Tooltip("The parent transform to instantiate the particle under. If null, nothing will be instantiated")]
        public Transform Parent;

        [Tooltip("If true, resets the local position to zero after instantiating under the parent")]
        public bool ResetLocalPosition = true;

        [InspectorGroup("Settings", 12, false)]
        [Tooltip("If true, plays the particle system immediately after instantiating")]
        public bool PlayOnInstantiate = true;

        [Tooltip("If true, automatically destroys the particle GameObject after it has finished playing")]
        public bool AutoDestroy = true;

        [Condition("AutoDestroy", true)]
        [Tooltip("Extra delay in seconds after the particle finishes before destroying it")]
        public float DestroyDelay = 0f;

        #endregion

#if UNITY_EDITOR
        public override float GetDuration() => 0f;

        public override string GetDescription() =>
            "Instantiate a ParticleSystem prefab under a parent transform";

        public override string GetCategory() => "ParticleSystem";

        public override string GetWarningMessage()
        {
            if (ParticlePrefab == null && Parent == null)
                return "⚠ Particle Prefab and Parent are not assigned. Please assign both.";

            if (ParticlePrefab == null)
                return "⚠ Particle Prefab is not assigned. Please assign a ParticleSystem prefab.";

            if (Parent == null)
                return "⚠ Parent is not assigned. The particle will not instantiate at runtime.";

            return null;
        }
#endif

        protected override IEnumerator CustomExecutionCoroutine()
        {
            if (Parent == null || ParticlePrefab == null)
            {
                yield break;
            }

            var instance = UnityEngine.Object.Instantiate(ParticlePrefab, Parent);
            if (ResetLocalPosition)
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }

            if (PlayOnInstantiate)
            {
                instance.Play();
            }

            if (AutoDestroy)
            {
                float totalDuration = instance.main.duration;
                if (instance.main.loop)
                {
                    totalDuration = DestroyDelay;
                }
                else
                {
                    totalDuration += DestroyDelay;
                }

                float elapsed = 0f;
                while (elapsed < totalDuration || instance.IsAlive(true))
                {
                    elapsed += DeltaTime();
                    yield return YieldInstruction;
                }

                if (instance != null)
                    UnityEngine.Object.Destroy(instance.gameObject);
            }
        }
    }
}