using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [SerializeField]
        private ActionFlowSettings _actionFlowSettings;
        public ActionFlowSettings ActionFlowSettings => _actionFlowSettings;

        [SerializeReference]
        public ActionFlow[] ActionFlows;

        private Action _onStartCallback;
        private Action _onEnableCallback;

        private CancellationTokenSource _cts;
        private bool _isExecuting = false;

        [SerializeField]
        private bool _calculateTotalDuration = false;
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

            _cts = new CancellationTokenSource();
            RunAsync(onComplete, _cts.Token).Forget();
        }

        public void ResetAnimationTargets()
        {
            StopExecution();
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

        //TODO Maybe add a StopCallback to each actionflow
        public void StopExecution()
        {
            _isExecuting = false;

            // Cancelling the token stops the driver and every fire-and-forget action it started,
            // which is what the per-coroutine bookkeeping used to do.
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTaskVoid RunAsync(Action onComplete, CancellationToken cancellationToken)
        {
            _isExecuting = true;

            int executionCount = _actionFlowSettings.RepeatForever ? -1 : _actionFlowSettings.NumberOfRepeats + 1;
            int currentExecution = 0;

            try
            {
                while (_isExecuting && (executionCount < 0 || currentExecution < executionCount))
                {
                    ActionFlow[] actionFlows = ActionFlows;

                    for (int i = 0; i < actionFlows.Length; i++)
                    {
                        if (!_isExecuting)
                        {
                            return;
                        }

                        ActionFlow actionFlow = actionFlows[i];
                        if (actionFlow == null)
                        {
                            continue;
                        }

                        bool shouldWait = actionFlow.Timing != null && actionFlow.Timing.WaitForCompletion;
                        if (shouldWait)
                        {
                            await actionFlow.ExecutionAsync(cancellationToken);
                        }
                        else
                        {
                            actionFlow.ExecutionAsync(cancellationToken).Forget();
                        }
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    currentExecution++;

                    if (_isExecuting && (executionCount < 0 || currentExecution < executionCount))
                    {
                        if (_actionFlowSettings.DelayBetweenRepeats > 0f)
                        {
                            await UniTask.Delay(TimeSpan.FromSeconds(_actionFlowSettings.DelayBetweenRepeats),
                                DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _isExecuting = false;
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