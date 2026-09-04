# Changelog

All notable changes to `com.jadedbelles.util` are documented here. This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-09-04

### Added
- Initial package scaffold (`package.json`, `Runtime` asmdef, `README`, `CHANGELOG`, `LICENSE`).
- `JadedBelles.Util.GridSystem` module extracted from Xandria Gem Jam:
  - `GridSystem2D<T>` — generic grid with `VerticalGrid` (X-Y plane) and `HorizontalGrid` (X-Z plane) factories, `SetValue`, `GetValue`, `GetXY`, `GetWorldPositionCenter`, `IsValid`, `OnValueChangeEvent`, `Width`, `Height`, `CellSize`, `Origin`.
  - `GridCell<TValue, TShape>` — two-parameter generic cell replacing the game-specific `GridObj`. Callers plug in their own value and shape types.
  - `GridObject<T>` — legacy single-value cell wrapper, kept for parity with old code.
- `Grid System Basics` sample under `Samples~/GridSystemBasics/`.
