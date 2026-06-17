using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public enum ActionFlowType
    {
        PlayOnStart,
        PlayOnEnable,
        PlayOnActionCallback,
    }

    [Serializable]
    public class ActionFlowSettings
    {
        public ActionFlowType ActionFlowType = ActionFlowType.PlayOnStart;

        [Header("Repeat")]
        [Tooltip("Number of times to repeat all action flows (0 = play once, 1 = play twice, etc.)")]
        public int NumberOfRepeats = 0;
        [Tooltip("If true, all action flows will repeat forever in a loop")]
        public bool RepeatForever = false;
        [Tooltip("Delay in seconds between each complete repeat cycle of all action flows")]
        public float DelayBetweenRepeats = 1f;
    }

    public class ActionFlowComponent : MonoBehaviour
    {
        [SerializeField] private ActionFlowSettings _actionFlowSettings;
        public ActionFlowSettings ActionFlowSettings => _actionFlowSettings;

        [SerializeReference] public ActionFlow[] ActionFlows;

        [SerializeField] private bool _resetBeforeExecution = true;
        [SerializeField] private Transform[] _resetTargets;

        private Action _onStartCallback;
        private Action _onEnableCallback;

        private Coroutine _coroutine;
        private readonly List<Coroutine> _activeCoroutines = new List<Coroutine>();
        private bool _isExecuting = false;

        private Vector3[] _cachedLocalPositions;
        private Quaternion[] _cachedLocalRotations;
        private Vector3[] _cachedLocalScales;

        [SerializeField] private bool _calculateTotalDuration = false;
        private float _totalDuration;
        public float TotalDuration
        {
            get
            {
                if (!_calculateTotalDuration)
                {
                    _calculateTotalDuration = true;
                    _totalDuration = CalculateTotalDuration();
                }

                return _totalDuration;
            }
        }

        private void Awake()
        {
            CacheResetPose();

            if (_calculateTotalDuration)
            {
                _totalDuration = CalculateTotalDuration();
            }

            switch (_actionFlowSettings.ActionFlowType)
            {
                case ActionFlowType.PlayOnStart:
                    _onStartCallback += ExecuteFromCallback;
                    break;
                case ActionFlowType.PlayOnEnable:
                    _onEnableCallback += ExecuteFromCallback;
                    break;
                case ActionFlowType.PlayOnActionCallback:
                    break;
            }
        }

        private void Start()
        {
            _onStartCallback?.Invoke();
        }

        private void OnEnable()
        {
            _onEnableCallback?.Invoke();
        }

        private void OnDestroy()
        {
            switch (_actionFlowSettings.ActionFlowType)
            {
                case ActionFlowType.PlayOnStart:
                    _onStartCallback -= ExecuteFromCallback;
                    break;
                case ActionFlowType.PlayOnEnable:
                    _onEnableCallback -= ExecuteFromCallback;
                    break;
                case ActionFlowType.PlayOnActionCallback:
                    break;
            }

            StopExecution();
        }

        public void OverrideSettings(ActionFlowSettings settings)
        {
            _actionFlowSettings = settings;
        }

        private void ExecuteFromCallback()
        {
            StartExecution();
        }

        //for the unity editor
        public void StartExecutionCall()
        {
            StartExecution();
        }

        public void StartExecution(Action onComplete = null, params ActionFlowOverride[] overrides)
        {
            StopExecution();

            if (_resetBeforeExecution)
            {
                RestoreCachedPose();
            }

            if (overrides != null)
            {
                foreach (var o in overrides)
                {
                    if (o.Index >= 0 && o.Index < ActionFlows.Length)
                    {
                        o.Apply?.Invoke(ActionFlows[o.Index]);
                    }
                }
            }

            _coroutine = StartCoroutine(CallbackCoroutine(onComplete));
        }

        public void CacheResetPose()
        {
            int targetCount = _resetTargets != null ? _resetTargets.Length : 0;

            _cachedLocalPositions = new Vector3[targetCount];
            _cachedLocalRotations = new Quaternion[targetCount];
            _cachedLocalScales = new Vector3[targetCount];

            for (int i = 0; i < targetCount; i++)
            {
                Transform resetTarget = _resetTargets[i];
                if (resetTarget == null)
                {
                    continue;
                }

                _cachedLocalPositions[i] = resetTarget.localPosition;
                _cachedLocalRotations[i] = resetTarget.localRotation;
                _cachedLocalScales[i] = resetTarget.localScale;
            }
        }

        public void RestoreCachedPose()
        {
            if (_resetTargets == null ||
                _cachedLocalPositions == null ||
                _cachedLocalRotations == null ||
                _cachedLocalScales == null)
            {
                Debug.LogWarning($"{name}: No reset pose cached. Make sure Reset Targets are assigned before Play Mode starts.");
                return;
            }

            int targetCount = Mathf.Min(
                _resetTargets.Length,
                _cachedLocalPositions.Length,
                _cachedLocalRotations.Length,
                _cachedLocalScales.Length
            );

            for (int i = 0; i < targetCount; i++)
            {
                Transform resetTarget = _resetTargets[i];
                if (resetTarget == null)
                {
                    continue;
                }

                resetTarget.localPosition = _cachedLocalPositions[i];
                resetTarget.localRotation = _cachedLocalRotations[i];
                resetTarget.localScale = _cachedLocalScales[i];
            }
        }

        public void ResetAnimationTargets()
        {
            StopExecution();
            RestoreCachedPose();
        }

        public void OverrideActionFlow(params ActionFlowOverride[] overrides)
        {
            if (overrides != null)
            {
                foreach (var o in overrides)
                {
                    if (o.Index >= 0 && o.Index < ActionFlows.Length)
                    {
                        o.Apply?.Invoke(ActionFlows[o.Index]);
                    }
                }
            }
        }

        public void StopExecution()
        {
            _isExecuting = false;

            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }

            //TODO Maybe add a StopCallback to each actionflow
            foreach (var activeCoroutine in _activeCoroutines)
            {
                if (activeCoroutine != null)
                {
                    StopCoroutine(activeCoroutine);
                }
            }

            _activeCoroutines.Clear();
        }

        private IEnumerator CallbackCoroutine(Action onComplete = null)
        {
            _isExecuting = true;

            int executionCount = _actionFlowSettings.RepeatForever ? -1 : _actionFlowSettings.NumberOfRepeats + 1;
            int currentExecution = 0;

            while (_isExecuting && (executionCount < 0 || currentExecution < executionCount))
            {
                _activeCoroutines.Clear();

                foreach (var actionFlow in ActionFlows)
                {
                    if (!_isExecuting)
                    {
                        yield break;
                    }

                    Coroutine actionCoroutine = StartCoroutine(actionFlow.ExecutionCoroutine());
                    _activeCoroutines.Add(actionCoroutine);

                    bool shouldWait = actionFlow.Timing != null && actionFlow.Timing.WaitForCompletion;
                    if (shouldWait)
                    {
                        yield return actionCoroutine;
                        _activeCoroutines.Remove(actionCoroutine);
                    }
                }

                yield return null;

                currentExecution++;

                if (_isExecuting && (executionCount < 0 || currentExecution < executionCount))
                {
                    if (_actionFlowSettings.DelayBetweenRepeats > 0f)
                    {
                        yield return new WaitForSeconds(_actionFlowSettings.DelayBetweenRepeats);
                    }
                }
            }

            _isExecuting = false;
            _activeCoroutines.Clear();
            onComplete?.Invoke();
        }

        private float CalculateTotalDuration()
        {
            if (ActionFlows == null || ActionFlows.Length == 0)
            {
                return 0f;
            }

            float total = 0f;
            foreach (var flow in ActionFlows)
            {
                if (flow == null || !flow.Active)
                {
                    continue;
                }

                if (flow.Timing != null)
                {
                    total += flow.Timing.InitialDelay;
                }

                bool shouldWait = flow.Timing == null || flow.Timing.WaitForCompletion;
                if (shouldWait)
                {
                    total += flow.GetDuration();
                }
            }

            return total;
        }
    }
}
