# JadedBelles Util

Reusable Unity utilities extracted from Xandria Gem Jam and other JadedBelles titles. Consumed as a Unity Package Manager (UPM) package via git URL.

## Requirements

- Unity 2022.3 or newer
- TextMeshPro (built-in on modern Unity)

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

## Roadmap

Slated for extraction from the Match3 codebase as they mature and prove reusable:

- `AudioManager` — namespaced audio clip lookup
- `MatchJuice` / `ScreenShaker` — screen shake, hitstop, pitched pops (Juice module)
- `HUDCounter` — animated numeric counter UI component
- `CloudSaveManager` glue — thin wrapper around JadedBelles API save endpoints
- Object pooling helpers

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
