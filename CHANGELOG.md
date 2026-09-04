# Changelog

All notable changes to `com.jadedbelles.util` are documented here. This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-09-04

### Added
- `JadedBelles.Util.Singletons` module:
  - `SingletonBehaviour<T>` — CRTP MonoBehaviour base that consolidates the duplicated "if (Instance == null) { Instance = this; DontDestroyOnLoad; } else { Destroy; }" `Awake` boilerplate from 7+ Match3 managers. `PersistAcrossScenes` override opts out of `DontDestroyOnLoad` for per-scene singletons; `OnSingletonAwake` gives subclasses a safe one-time init hook that only fires on the surviving instance.
- `JadedBelles.Util.Timers` module (extracted from `StroTheGoatUtils`):
  - `Timer` — plain-C# base with `StartTimer / StopTimer / Pause / Resume / Reset / Tick`, plus `OnTimerStart`, `OnTimerStop`, `ForceTimerEnd` action hooks.
  - `CountdownTimer` — counts down to zero, exposes `IsFinished`, `Reset(newTime)` overload.
  - `StopwatchTimer` — counts up from zero, exposes `GetTime()`.
- `JadedBelles.Util.Time` module (extracted from `StroTheGoatUtils`):
  - `TimeUtils` — `UnixNow`, `MinutesBetween`, `FormatCountdown`, `DurationBetween`, `ParseUnixString`.
  - `TimeSnapshot` struct — `DateTime` + Unix timestamp pair with `ToString()`.
- `JadedBelles.Util.RandomUtil` module (extracted from `StroTheGoatUtils`):
  - `Dice` — `RollDice`, `RollMultipleDiceOfSameType`, `AddAllDiceOfSameType`, and mixed-pool variants. Namespaced as `RandomUtil` to avoid clashing with `UnityEngine.Random`.
- `JadedBelles.Util.Coroutines` module (extracted from `StroTheGoatUtils`):
  - `CoroutineUtils.AwaitTask(Task)` — yield-until-completed bridge with `Debug.LogException` on fault.
  - `WaitExtensions.DelayerWithContinuance(delay, continue, callback)` — wait, invoke callback, wait some more.
- `JadedBelles.Util.Juice` module (extracted from Match3's `Juice/` folder):
  - `ScreenShaker` — Perlin-noise `transform.localPosition` shake with max-merge overlap semantics; runs on unscaled time so it survives hitstop; snaps back to rest on disable.
  - `HitstopController` — plain-object `Time.timeScale` freezer. Promoted from `internal` to `public` for reuse.
- `JadedBelles.Util.UI` module (extracted from Match3's `UI/` folder):
  - `BreathingImage` — smoothstep breath curve on scale + optional alpha / rotation sway, on unscaled time. Includes `ResetToRest` and `PulseNow`.
  - `LoadingBar` — constant-speed, non-outrunning filled-`Image` progress bar with optional `TMP_Text` percentage label. Exposes `IsVisuallyComplete`, `DisplayedProgress`, `TargetProgress`, `ReportProgress`, `ReportComplete`, `SetImmediate`, `ResetBar`.

### Notes
- Every candidate in the extraction survey ships in this release. `EventChannel<T>` (survey candidate 8) already landed in 0.2.0, so this wave covers the remaining 9 candidates.
- No breaking changes to any 0.2.0 public API. Modules land as additive namespaces; existing consumers continue to build against 0.2.0 unchanged.
- Extracted files preserve original behavior verbatim — the only edits are namespace renames, `using`-directive adjustments, and (for `HitstopController`) visibility promotion.

## [0.2.0] - 2026-09-04

### Added
- `JadedBelles.Util.Events` module extracted from Match3:
  - `EventChannel<T>` — abstract generic ScriptableObject event channel with `Invoke`, `Register`, `Deregister`.
  - `EventChannel` — no-payload variant backed by the `Empty` sentinel struct.
  - `EventListener<T>` — MonoBehaviour listener that binds to a channel asset and forwards raised values to an inspector-configured `UnityEvent<T>`. Non-generic `EventListener` shipped for signal-only channels.
- `JadedBelles.Util.StateMachine` module (fresh authoring):
  - `IState` interface with `OnEnter / Update / FixedUpdate / OnExit`.
  - `StateMachine` plain-C# class with `SetState`, `AddTransition`, `AddAnyTransition`, `Tick`, `FixedTick`, and an `OnStateChanged` event. Any-state transitions evaluated before per-state transitions.
  - `StateMachineComponent` MonoBehaviour wrapper that drives `Tick / FixedTick` from Unity's loop.
- `JadedBelles.Util.Input` module (fresh authoring):
  - `InputReader` abstract ScriptableObject with `Enable / Disable` virtuals and `MoveEvent`, `PrimaryPressed`, `PrimaryReleased`, `PointerPositionChanged` events. Subclasses implement the generated `PlayerInputActions` callbacks and fan them out via the protected `Raise*` helpers.
- Runtime asmdef now references `Unity.InputSystem` alongside `Unity.TextMeshPro`.

### Changed
- README gains three new module sections (Events, StateMachine, Input) and an Input System requirement note.

## [0.1.0] - 2026-09-04

### Added
- Initial package scaffold (`package.json`, `Runtime` asmdef, `README`, `CHANGELOG`, `LICENSE`).
- `JadedBelles.Util.GridSystem` module extracted from Xandria Gem Jam:
  - `GridSystem2D<T>` — generic grid with `VerticalGrid` (X-Y plane) and `HorizontalGrid` (X-Z plane) factories, `SetValue`, `GetValue`, `GetXY`, `GetWorldPositionCenter`, `IsValid`, `OnValueChangeEvent`, `Width`, `Height`, `CellSize`, `Origin`.
  - `GridCell<TValue, TShape>` — two-parameter generic cell replacing the game-specific `GridObj`. Callers plug in their own value and shape types.
  - `GridObject<T>` — legacy single-value cell wrapper, kept for parity with old code.
- `Grid System Basics` sample under `Samples~/GridSystemBasics/`.
