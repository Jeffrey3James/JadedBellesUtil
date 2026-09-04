# JadedBelles Util

Reusable Unity utilities extracted from Xandria Gem Jam and other JadedBelles titles. Consumed as a Unity Package Manager (UPM) package via git URL.

## Requirements

- Unity 2022.3 or newer
- TextMeshPro (built-in on modern Unity)
- Unity Input System (`com.unity.inputsystem`) — required by the `Input` module. Add it via `Window → Package Manager → Unity Registry → Input System` if your project doesn't already have it.

## Install

In your consuming Unity project, open `Packages/manifest.json` and add:

```json
{
  "dependencies": {
    "com.jadedbelles.util": "https://github.com/Jeffrey3James/JadedBellesUtil.git"
  }
}
```

For a pinned version, append `#v0.1.0` (or any release tag) to the URL.

Since this repo is private, Unity will need git credentials with read access — the simplest setup is a GitHub personal access token in the git credential helper on your dev machine.

Alternatively: `Window → Package Manager → + → Add package from git URL...` and paste the same URL.

## Modules

### `JadedBelles.Util.GridSystem`

A generic 2D grid usable in both 2D and 3D scenes.

- `GridSystem2D<T>` — the grid itself. `VerticalGrid(...)` for standard 2D on the X-Y plane, `HorizontalGrid(...)` for top-down 3D on the X-Z plane. Public API: `SetValue(x, y, T)`, `GetValue(x, y)`, `GetXY(worldPos)`, `GetWorldPositionCenter(x, y)`, `OnValueChangeEvent`, `Width`, `Height`, `CellSize`, `Origin`, `IsValid(x, y)`.
- `GridCell<TValue, TShape>` — a cell that holds a primary value plus an optional shape/metadata payload. Both type params must be reference types. Use `TShape = object` when you don't need a shape payload.
- `GridObject<T>` — single-value cell wrapper, provided for compatibility with older JadedBelles code. New code should prefer `GridCell`.

Minimal example:

```csharp
using JadedBelles.Util.GridSystem;
using UnityEngine;

var grid = GridSystem2D<GridCell<Transform, object>>.VerticalGrid(
    width: 8, height: 8, cellSize: 1f, origin: Vector3.zero, debug: true);

for (int x = 0; x < grid.Width; x++)
    for (int y = 0; y < grid.Height; y++)
    {
        var cell = new GridCell<Transform, object>(grid, x, y);
        grid.SetValue(x, y, cell);
    }

grid.OnValueChangeEvent += (x, y, cell) => Debug.Log($"Set ({x},{y})");
```

Or install the `Grid System Basics` sample via Package Manager for a runnable version.

### `JadedBelles.Util.Events`

ScriptableObject-based event channels extracted from Match3. Decouples producers from consumers by routing signals through an asset — great for cross-scene / cross-system communication without wiring `[SerializeField]` references everywhere.

- `EventChannel<T>` — abstract generic channel. Subclass with `[CreateAssetMenu]` in your game project to expose it in Unity's asset menu.
- `EventChannel` — no-payload variant for signal-only events (uses the internal `Empty` sentinel).
- `EventListener<T>` / `EventListener` — MonoBehaviour listeners that bind to a channel asset and forward raised values to an inspector-configured `UnityEvent<T>`.

Minimal example:

```csharp
using JadedBelles.Util.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/ScoreChangedEventChannel")]
public class ScoreChangedEventChannel : EventChannel<int> { }

public class ScoreProducer : MonoBehaviour
{
    [SerializeField] private ScoreChangedEventChannel scoreChanged;
    public void Award(int amount) => scoreChanged.Invoke(amount);
}
```

### `JadedBelles.Util.StateMachine`

A lightweight, transition-based finite state machine. No Unity dependency in the core, so states are trivially unit-testable; drop the `StateMachineComponent` MonoBehaviour on a GameObject to drive it from Unity's Update loop.

- `IState` — `OnEnter / Update / FixedUpdate / OnExit` contract.
- `StateMachine` — plain-C# machine with `SetState`, `AddTransition(from, to, condition)`, `AddAnyTransition(to, condition)`, `Tick`, `FixedTick`, and an `OnStateChanged` event.
- `StateMachineComponent` — abstract MonoBehaviour wrapper that owns a `StateMachine` and forwards Update / FixedUpdate.

Minimal example:

```csharp
using JadedBelles.Util.StateMachine;
using UnityEngine;

public class EnemyBrain : StateMachineComponent
{
    private void Start()
    {
        var idle = new IdleState();
        var chase = new ChaseState(this);

        Machine.AddTransition(idle, chase, () => PlayerInSight());
        Machine.AddTransition(chase, idle, () => !PlayerInSight());

        Machine.SetState(idle);
    }

    private bool PlayerInSight() => /* ... */ false;
}
```

### `JadedBelles.Util.Input`

ScriptableObject-based input abstraction. Gameplay code holds an `InputReader` reference and subscribes to strongly typed events; the concrete subclass is the only piece that touches Unity's `InputSystem`, so rebinds and platform tweaks stay contained.

- `InputReader` — abstract SO base with `Enable() / Disable()` virtuals and `MoveEvent`, `PrimaryPressed`, `PrimaryReleased`, `PointerPositionChanged` events. Subclasses call the protected `Raise*` helpers.

Usage:

1. In your game project, generate a `PlayerInputActions` C# class from an Input Actions asset (`.inputactions` → *Generate C# Class*).
2. Create a `[CreateAssetMenu]` subclass of `InputReader` that implements the generated `IPlayerActions` interface and calls `RaiseMove / RaisePrimaryPressed / …` from each callback.
3. Create the SO asset in your project and inject it into consuming MonoBehaviours via `[SerializeField]`.

See the doc comment on `InputReader` for a full subclass example.

### `JadedBelles.Util.Singletons`

MonoBehaviour base class that eats the copy-paste `Awake` singleton dance. Subclass with the CRTP form and override `OnSingletonAwake` for one-time setup — the base handles duplicate destruction and `DontDestroyOnLoad` (opt out via `PersistAcrossScenes`).

- `SingletonBehaviour<T>` — provides `Instance`, `PersistAcrossScenes`, `OnSingletonAwake`, and clears `Instance` on destroy.

Minimal example:

```csharp
using JadedBelles.Util.Singletons;
using UnityEngine;

public class AudioManager : SingletonBehaviour<AudioManager>
{
    [SerializeField] private AudioSource source;

    protected override void OnSingletonAwake()
    {
        if (source == null) source = GetComponent<AudioSource>();
    }

    public void Play(AudioClip clip) => source.PlayOneShot(clip);
}
```

### `JadedBelles.Util.Timers`

Plain-C# tickable timers extracted from `StroTheGoatUtils`. Zero Unity dependency in the core — feed them a `deltaTime` each frame and hook the `OnTimerStart` / `OnTimerStop` events.

- `Timer` — abstract base with `StartTimer / StopTimer / Pause / Resume / Reset / Tick`, plus `OnTimerStart`, `OnTimerStop`, `ForceTimerEnd` action hooks.
- `CountdownTimer` — counts down from an initial duration, fires `OnTimerStop` at zero, exposes `IsFinished`.
- `StopwatchTimer` — counts up from zero, exposes `GetTime()`.

Minimal example:

```csharp
using JadedBelles.Util.Timers;
using UnityEngine;

public class RoundClock : MonoBehaviour
{
    private CountdownTimer _timer;

    private void Start()
    {
        _timer = new CountdownTimer(60f);
        _timer.OnTimerStop = () => Debug.Log("Round over");
        _timer.StartTimer();
    }

    private void Update() => _timer.Tick(Time.deltaTime);
}
```

### `JadedBelles.Util.Time`

Unix-timestamp / countdown-string helpers. No `UnityEngine` dependency — pure `System.DateTimeOffset` / `TimeSpan` math.

- `TimeUtils` — `UnixNow`, `MinutesBetween`, `FormatCountdown`, `DurationBetween`, `ParseUnixString`.
- `TimeSnapshot` — struct holding a `DateTime` + Unix timestamp pair for logging or wire serialization.

Minimal example:

```csharp
using JadedBelles.Util.Time;

long start = TimeUtils.UnixNow;
string label = TimeUtils.FormatCountdown(start, intervalDurationSeconds: 300);
// label == "05:00" right at the start; "04:59" a second later, etc.
```

### `JadedBelles.Util.RandomUtil`

Dice-rolling helpers on top of `UnityEngine.Random`. Namespaced as `RandomUtil` (not `Random`) so you never have to disambiguate against `UnityEngine.Random` at the call site.

- `Dice` — `RollDice(sides)`, `RollMultipleDiceOfSameType`, `AddAllDiceOfSameType`, and the mixed-pool variants.

Minimal example:

```csharp
using JadedBelles.Util.RandomUtil;

int damage = Dice.AddAllDiceOfSameType(numberOfDice: 3, sides: 6); // 3d6
```

### `JadedBelles.Util.Coroutines`

Small helpers for the async/coroutine seams every Unity project rewrites eventually.

- `CoroutineUtils.AwaitTask(Task)` — yield-until-completed bridge for `System.Threading.Tasks.Task`; logs faults via `Debug.LogException`.
- `WaitExtensions.DelayerWithContinuance(delay, continue, callback)` — wait, invoke callback, wait some more.

Minimal example:

```csharp
using JadedBelles.Util.Coroutines;
using System.Threading.Tasks;
using UnityEngine;

public class SaveWatcher : MonoBehaviour
{
    public void Save(Task apiCall) => StartCoroutine(CoroutineUtils.AwaitTask(apiCall));
}
```

### `JadedBelles.Util.Juice`

Game-feel helpers that made it out of Match3's `Juice/` folder unchanged.

- `ScreenShaker` — Perlin-noise `transform.localPosition` shake. Overlapping calls merge to the max amplitude and max remaining duration, so aftershocks never dampen the main hit. Runs on unscaled time so it survives hitstop.
- `HitstopController` — plain object (not a MonoBehaviour) that pauses `Time.timeScale` for N ms and restores it. Overlapping requests extend the freeze if longer, ignore it if shorter. Construct with a host MonoBehaviour that will run the coroutine.

Minimal example:

```csharp
using JadedBelles.Util.Juice;
using UnityEngine;

public class HitFX : MonoBehaviour
{
    [SerializeField] private ScreenShaker shaker;
    private HitstopController _hitstop;

    private void Awake() => _hitstop = new HitstopController(this);

    public void OnBigHit()
    {
        shaker.Shake(amp: 0.4f, dur: 0.25f);
        _hitstop.Begin(ms: 80);
    }
}
```

### `JadedBelles.Util.UI`

Drop-in UI components with zero game coupling.

- `BreathingImage` — pulses `transform.localScale`, optional alpha, and optional Z-rotation on a smoothstep breath curve, on unscaled time so it keeps breathing when the game is paused. Great for loading screens and idle NPCs.
- `LoadingBar` — constant-speed, non-outrunning progress bar backed by a filled `Image` and an optional `TMP_Text` percentage label. Cannot pass the last reported progress value; guarantees the final sprint is visible.

Minimal example:

```csharp
using JadedBelles.Util.UI;
using UnityEngine;

public class LoadingController : MonoBehaviour
{
    [SerializeField] private LoadingBar bar;

    private void Update()
    {
        // Feed progress from your loader; the bar animates smoothly, never jumps.
        bar.ReportProgress(MyLoader.NormalizedProgress);
        if (MyLoader.Done) bar.ReportComplete();
    }
}
```

## Roadmap

Slated for extraction from the Match3 codebase as they mature and prove reusable:

- `HUDCounter` — animated numeric counter UI component
- `CloudSaveManager` glue — thin wrapper around JadedBelles API save endpoints
- Object pooling helpers
- Grid-neighborhood offsets (`NeighborOffsets`, `ForEachNeighborInBounds`) — after Match3's PowerUp code stabilizes

## Contributing

Extraction pattern:

1. Copy the source file(s) into `Runtime/<ModuleName>/`.
2. Change the namespace to `JadedBelles.Util.<ModuleName>`.
3. Strip game-specific type references; introduce generic parameters where needed.
4. Add a section to this README.
5. Bump the minor version in `package.json` and add a `CHANGELOG.md` entry.
6. Update consuming projects (e.g. Match3) to pull the new version.

## License

Proprietary — internal JadedBelles use.
