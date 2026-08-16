# ActionFlow

ActionFlow is an editor-driven animation and sequencing tool for Unity. You add an `ActionFlowComponent` to a GameObject and build a list of action flows that run one after another or in parallel, each with its own timing — initial delays and an optional wait-for-completion. Flows can fire automatically on `Start` or `OnEnable`, or be triggered manually from your own code. Built-in repeat options let you loop the whole sequence a set number of times or forever, with a configurable delay between cycles. It can also cache and restore the starting transforms of target objects, so every playthrough begins from a clean pose.

## Requirements

- Unity 2021.3 or newer
- [UniTask](https://github.com/Cysharp/UniTask) — installed automatically, see below

### UniTask

ActionFlow runs on UniTask instead of coroutines, so all sequencing is allocation-light and cancellable.

You do not have to install it yourself. The first time the package is imported, an editor bootstrap
adds the OpenUPM scoped registry and `com.cysharp.unitask` to your project's `Packages/manifest.json`,
then installs it. Until UniTask lands, ActionFlow's own assemblies are skipped rather than compiled
(they carry a `defineConstraints` on `ACTIONFLOW_UNITASK`), so your project keeps compiling and the
console stays clean — the package simply switches on once the dependency is there.

If the automatic install is blocked (offline, restricted registry), run
**Tools → ActionFlow → Install UniTask Dependency** to retry, or add it yourself:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp"]
    }
  ],
  "dependencies": {
    "com.cysharp.unitask": "2.5.10"
  }
}
```

## Installation

Install via Unity's Package Manager using the Git URL.

1. Open your project in Unity.
2. Go to **Window → Package Manager**.
3. Click the **+** button in the top-left and choose **Add package from git URL…**
4. Paste the URL below and click **Add**:

```
https://github.com/eliranshaya/Unity-ActionFlow.git?path=/Packages/com.eliranshaya.actionflow
```

To lock to a specific version, append the tag:

```
https://github.com/eliranshaya/Unity-ActionFlow.git?path=/Packages/com.eliranshaya.actionflow#2.0.0
```

You can also add it manually by editing your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eliranshaya.actionflow": "https://github.com/eliranshaya/Unity-ActionFlow.git?path=/Packages/com.eliranshaya.actionflow#2.0.0"
  }
}
```

## Quick start

1. Add the **Action Flow Component** to a GameObject.
2. Choose how it triggers: `PlayOnStart`, `PlayOnEnable`, or `PlayOnActionCallback` (fired from your own code).
3. Add one or more action flows and configure their timing.
4. (Optional) Assign **Reset Targets** to snapshot and restore their transforms before each run.
5. Enter Play Mode, or call `StartExecution()` from a script when using the callback mode.

```csharp
// Trigger manually from code
var flow = GetComponent<ActionFlowComponent>();
flow.StartExecution(onComplete: () => Debug.Log("Done!"));
```

## Writing a custom action

Derive from `ActionFlow` and implement `CustomExecutionAsync`. Actions that finish immediately should
return `UniTask.CompletedTask` from a non-`async` method — that keeps them free of an async state
machine and of any allocation:

```csharp
[Serializable]
public class ActionFlowMyInstantAction : ActionFlow
{
    public GameObject Target;

    protected override UniTask CustomExecutionAsync(CancellationToken cancellationToken)
    {
        if (Target != null) Target.SetActive(true);
        return UniTask.CompletedTask;
    }
}
```

Actions that run over time await `NextFrame(cancellationToken)`, which resumes on the player-loop
point selected by the action's **Time Mode** (Update, FixedUpdate, EndOfFrame or UnscaledUpdate):

```csharp
protected override async UniTask CustomExecutionAsync(CancellationToken cancellationToken)
{
    float duration = DurationMode.GetDuration();
    float elapsed = 0f;

    while (elapsed < duration)
    {
        float t = Mathf.Clamp01(elapsed / duration);
        // ... apply the animation for this frame ...
        elapsed += DeltaTime();
        await NextFrame(cancellationToken);
    }
}
```

Honour the `CancellationToken` by passing it to every await — that is what makes `StopExecution()`,
`OnDestroy` and re-triggering stop the action cleanly.

## Upgrading from 1.x to 2.0

2.0 replaces coroutines with UniTask. This is a breaking change for anyone who wrote **custom
ActionFlow subclasses**; scenes, prefabs and serialized data are unaffected and need no migration.

| 1.x | 2.0 |
| --- | --- |
| `IEnumerator CustomExecutionCoroutine()` | `UniTask CustomExecutionAsync(CancellationToken)` |
| `IEnumerator ExecutionCoroutine()` | `UniTask ExecutionAsync(CancellationToken)` |
| `yield return YieldInstruction` | `await NextFrame(cancellationToken)` |
| `yield return WaitForDuration(d)` | `await WaitForDuration(d, cancellationToken)` |
| `yield break` | `return` (or `return UniTask.CompletedTask` in a non-`async` method) |
| `protected YieldInstruction YieldInstruction` | `protected PlayerLoopTiming LoopTiming` |

Also in 2.0:

- **The Video category was removed.** `ActionFlowVideoPlayer` no longer ships with the package. Any
  action flow entry using it will be dropped from its component when the scene is re-serialized.
- `EndOfFrame` timing now maps to UniTask's `PlayerLoopTiming.LastPostLateUpdate`.
- Waits inside actions now honour the action's own **Time Mode**, so an action set to
  `UnscaledUpdate` or `FixedUpdate` no longer falls back to scaled `Update` time for its delays.
- Stopping is driven by a `CancellationToken` instead of `StopCoroutine`. As with coroutines,
  cancellation takes effect at the next suspension point.

## License

MIT
