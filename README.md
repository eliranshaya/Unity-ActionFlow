# ActionFlow

ActionFlow is an editor-driven animation and sequencing tool for Unity. You add an `ActionFlowComponent` to a GameObject and build a list of action flows that run one after another or in parallel, each with its own timing — initial delays and an optional wait-for-completion. Flows can fire automatically on `Start` or `OnEnable`, or be triggered manually from your own code. Built-in repeat options let you loop the whole sequence a set number of times or forever, with a configurable delay between cycles. It can also cache and restore the starting transforms of target objects, so every playthrough begins from a clean pose.

## Requirements

- Unity 2021.3 or newer

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
https://github.com/eliranshaya/Unity-ActionFlow.git?path=/Packages/com.eliranshaya.actionflow#1.0.0
```

You can also add it manually by editing your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eliranshaya.actionflow": "https://github.com/eliranshaya/Unity-ActionFlow.git?path=/Packages/com.eliranshaya.actionflow#1.0.0"
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

## License

MIT
