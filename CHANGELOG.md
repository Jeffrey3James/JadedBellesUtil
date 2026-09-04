# Changelog

All notable changes to `com.jadedbelles.util` are documented here. This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
