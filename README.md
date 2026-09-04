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

ScriptableObject-based input abstraction and mobile input widgets. Gameplay code holds an `InputReader` reference and subscribes to strongly typed events; the concrete subclass is the only piece that touches Unity's `InputSystem`, so rebinds and platform tweaks stay contained.

- `InputReader` — abstract SO base with `Enable() / Disable()` virtuals and `MoveEvent`, `PrimaryPressed`, `PrimaryReleased`, `PointerPositionChanged` events. Subclasses call the protected `Raise*` helpers.
- `MobileJoystick` — on-screen virtual joystick MonoBehaviour. Attach to a Canvas element with a background `RectTransform` and a knob `RectTransform`; exposes `Value` as `Vector2` in `[-1, 1]`. Implements `IPointerDownHandler / IDragHandler / IPointerUpHandler`, so the scene needs an `EventSystem` and a `GraphicRaycaster` on the parent canvas.

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

### `JadedBelles.Util.Auth`

Session-persistence helper for the JadedBelles JWT API. Wraps `PlayerPrefs` with Base64 obfuscation and a per-title key prefix so multiple JadedBelles games installed on the same device do not clobber each other's session.

- `TokenStore` — instance-based store with `SaveTokens`, `GetAccessToken`, `GetRefreshToken`, `Clear`, `HasSession`, plus the resolved `AccessTokenKey` / `RefreshTokenKey` for diagnostics. The default `KeyPrefix` is `"jadedbelles."`. Pass a title-unique prefix to isolate per-game sessions, or pass an empty string to preserve the legacy pre-v0.4.0 key names (`jb_access_token` / `jb_refresh_token`) during migration.

Minimal example:

```csharp
using JadedBelles.Util.Auth;

var tokens = new TokenStore("match3.");
tokens.SaveTokens(accessJwt, refreshJwt);
if (tokens.HasSession())
    Debug.Log($"Logged in — access key = {tokens.AccessTokenKey}");
```

### `JadedBelles.Util.Pooling`

Hand-rolled generic component pool for hot-loop allocations (projectiles, hit VFX, collectable pickups, one-shot audio sources). Intentionally distinct from Unity's built-in `UnityEngine.Pool.ObjectPool<T>`: this variant is constrained to `Component`, calls `SetActive` on the owning `GameObject` around `Get` / `Release`, and no-ops on double-release.

- `ObjectPool<T> where T : Component` — `Get()`, `Release(item)`, `AvailableCount`. Constructor takes a `Func<T>` factory closure (typically wrapping `Object.Instantiate(prefab, parent)`).

Minimal example:

```csharp
using JadedBelles.Util.Pooling;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    [SerializeField] private Projectile prefab;
    private ObjectPool<Projectile> _pool;

    private void Awake() => _pool = new ObjectPool<Projectile>(() => Instantiate(prefab, transform));

    public Projectile Fire() => _pool.Get();
    public void Recycle(Projectile p) => _pool.Release(p);
}
```

### `JadedBelles.Util.Color`

Canonical JadedBelles brand color palette, extracted from the `ColorChanger` helper that had been copy-pasted verbatim across four repos.

- `Palette` — the canonical static color set: `Grey`, `Green`, `Blue`, `Gold`, `Purple`, `Red`. Use these for status messages, UI accents, and anywhere brand colors need to stay in sync across titles.
- `ColorChanger` — legacy alias whose fields forward to `Palette`. Preserved so existing call sites like `LoginMessage.color = ColorChanger.Green;` migrate with a namespace change only.

Minimal example:

```csharp
using JadedBelles.Util.Color;
using TMPro;
using UnityEngine;

public class ToastMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void ShowSuccess(string msg) { label.text = msg; label.color = Palette.Green; }
    public void ShowError(string msg)   { label.text = msg; label.color = Palette.Red; }
}
```

### `JadedBelles.Util.UnityExtensions`

Idiomatic Unity extension methods that every project rewrites eventually. Bundled together so a single `using` unlocks both.

- `GameObjectExtensions.GetOrAdd<T>(this GameObject)` — returns the existing `T` component if attached, otherwise adds and returns a fresh one.
- `GameObjectExtensions.OrNull<T>(this T)` — bypasses Unity's fake-null so `??` and `?.` behave correctly on destroyed `UnityEngine.Object` references.

Minimal example:

```csharp
using JadedBelles.Util.UnityExtensions;
using UnityEngine;

public class AudioBinder : MonoBehaviour
{
    private AudioSource _source;

    private void Awake()
    {
        _source = gameObject.GetOrAdd<AudioSource>();
        var maybe = someOtherReference.OrNull() ?? _source; // real null-coalesce, no fake-null trap
    }
}
```

### `JadedBelles.Util.Services`

One-shot helper for Unity Gaming Services (UGS): calls `UnityServices.InitializeAsync()` and, if the player isn't already signed in, `AuthenticationService.Instance.SignInAnonymouslyAsync()`. Every JadedBelles title that talks to UGS (Leaderboards, Cloud Save, Relay, Authentication) runs the same fifteen-line try/await/log block; this module consolidates it.

- `UnityServicesInitializer.InitializeAndSignInAsync()` — `Task`-returning entry point. Idempotent: no-ops once `IsInitialized` is true and the player is still signed in.
- `UnityServicesInitializer.InitializeAndSignIn()` — coroutine-friendly wrapper you can `yield return` from any `MonoBehaviour`.
- `UnityServicesInitializer.IsInitialized` / `IsSignedIn` — status flags for gating downstream UGS calls.

**Requires `com.unity.services.core` (>= 1.0.0) and `com.unity.services.authentication` (>= 2.0.0).** The file is guarded by the `JADEDBELLES_UGS_CORE` and `JADEDBELLES_UGS_AUTH` version-defines declared in the Runtime asmdef, so it compiles away cleanly on projects that don't have those UGS packages installed — no compile error, the type just isn't visible.

Minimal example:

```csharp
using JadedBelles.Util.Services;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private async void Start()
    {
        await UnityServicesInitializer.InitializeAndSignInAsync();
        if (UnityServicesInitializer.IsInitialized)
            Debug.Log("UGS ready — safe to call Leaderboards / CloudSave / Relay.");
    }
}
```

### `JadedBelles.Util.UITK`

Fluent extension helpers on `VisualElement` for UI Toolkit (UITK). Lifted verbatim from the canonical `StroTheGoatUtils.cs` so every UITK-using JadedBelles project can share one authoritative implementation instead of a copy-paste-then-drift lineage.

- `VisualElementsExtensions.CreateChild(...)` — create a plain `VisualElement` child with USS classes and return it for chaining.
- `VisualElementsExtensions.CreateChild<T>(...)` — typed variant that returns the concrete element type.
- `VisualElementsExtensions.AddTo<T>(...)` — attach the caller to a parent and return the caller.
- `VisualElementsExtensions.AddClass<T>(...)` — add every non-null / non-empty USS class in a `params` list.
- `VisualElementsExtensions.WithManipulators<T>(...)` — attach an `IManipulator` and return the element.

**Requires `com.unity.modules.uielements` (>= 1.0.0).** The UI Elements module is a built-in Unity module that is auto-referenced on modern Unity, so in practice you have this already; the `JADEDBELLES_UITK` version-define is belt-and-braces so the file compiles away cleanly on exotic project configurations that strip built-in modules.

Minimal example:

```csharp
using JadedBelles.Util.UITK;
using UnityEngine.UIElements;

var root = new VisualElement();
var row = root.CreateChild("row", "header");
var label = row.CreateChild<Label>("row__label");
label.text = "Score";
```

### `JadedBelles.Util.EditorTools` (Editor-only)

Small collection of `GUIStyle` and label helpers for custom inspectors, editor windows, and scene overlays. Ships in a separate `JadedBelles.Util.Editor` assembly whose asmdef declares `"includePlatforms": ["Editor"]`, so **every `.cs` file under `Editor/` is Editor-only automatically** — no `#if UNITY_EDITOR` guards needed. Consumers get the module for free once they reference the package; player builds never see it.

- `EditorUtils.CreateLabelAndConfigure(label, guiStyle, color)` — render a `GUILayout.Label` using a temporary clone of the style with its text color overridden.
- `EditorUtils.CenteredStyle(fontSize)` — bold, center-aligned `GUIStyle` at the requested font size on top of `EditorStyles.boldLabel`.
- `EditorUtils.AddSpaceToGUI(int)` — `GUILayout.Space` wrapper that reads more clearly inside long custom-editor layouts.
- `PrefabPlacerWindow` — editor window (open from `Tools > JadedBelles > Prefab Placer`) that scatters copies of a prefab across a rectangular area around an origin transform, then captures placements into a `PrefabLayoutData` asset. Placed-prefab tag is a serialized inspector field (defaults to `"PlacedPrefab"`).
- `PrefabLayoutData` — ScriptableObject asset storing captured placements. Create via `Assets > Create > JadedBelles > Prefab Layout Data`.
- `SceneSwitcherOverlay` + `SceneSwitcherToolbar` — Scene View overlay hosting a dropdown that lists every scene enabled in Build Settings and switches to the picked one. Enable from the Scene View's Overlays menu as `Scene Switcher`; prompts to save before switching.

Minimal example:

```csharp
using JadedBelles.Util.EditorTools;
using UnityEditor;
using UnityEngine;

public sealed class ExampleWindow : EditorWindow
{
    private void OnGUI()
    {
        EditorUtils.CreateLabelAndConfigure("Status", EditorUtils.CenteredStyle(14), Color.green);
        EditorUtils.AddSpaceToGUI(12);
    }
}
```

### `JadedBelles.Util.CharacterCustomization`

Data-driven character outfit customization: cycle hats, hair, shoes, wings, or any slot the game defines. Slots are identified by strings, not a hardcoded enum, so a fashion game and a robot builder can share this module without recompiling it. Save I/O is behind an interface so the module doesn't care whether outfits live in cloud save, PlayerPrefs, or a backend API.

- `BodyPart` — one entry per slot: a string `slotId` plus the swappable GameObjects.
- `CharacterCustomization` — MonoBehaviour on the character root that syncs visible parts to the saved outfit and exposes `GetNextBodyPart` / `GetLastBodyPart` / `PersistCurrentSelectionAsync`.
- `CharacterOutfitManager` — cross-scene singleton (built on `SingletonBehaviour<T>`) that owns the outfit dictionary and the event bus.
- `CharacterUIController` — UGUI plumbing: wires next/previous/save buttons to the rig; return scene is a serialized field.
- `ICharacterSaveProvider` — implement once per game to plug outfit persistence into any save backend.
- `Character Customization` sample under `Samples~/CharacterCustomization/` — the original `CharacterOutfitManager.prefab` as a wiring reference.

Minimal setup:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using JadedBelles.Util.CharacterCustomization;
using UnityEngine;

// 1. Wire the manager once at boot, plug in your save system:
public sealed class MySaveProvider : ICharacterSaveProvider
{
    public Task<Dictionary<string, int>> LoadOutfitAsync() => Task.FromResult(new Dictionary<string, int>());
    public Task SaveOutfitAsync(Dictionary<string, int> outfit) => Task.CompletedTask;
}

public sealed class Boot : MonoBehaviour
{
    private void Start()
    {
        CharacterOutfitManager.Instance.SaveProvider = new MySaveProvider();
    }
}

// 2. Put CharacterCustomization on the character root and fill BodyPart[] in the inspector:
//    slotId="Hat",   bodyParts=[Hat01, Hat02, Hat03]
//    slotId="Hair",  bodyParts=[Hair01, Hair02]
//    slotId="Shoes", bodyParts=[Shoes01, Shoes02, Shoes03]

// 3. On the UI screen, add CharacterUIController and wire the BodyPartButton[] entries:
//    button=NextHatButton,  slotId="Hat",  next=true
//    button=PrevHatButton,  slotId="Hat",  next=false
//    button=NextHairButton, slotId="Hair", next=true ...
// Set returnSceneName in the inspector to whatever scene the Save button should load.
```

## Roadmap

Slated for extraction from the Match3 codebase as they mature and prove reusable:

- `HUDCounter` — animated numeric counter UI component
- `CloudSaveManager` glue — thin wrapper around JadedBelles API save endpoints
- Grid-neighborhood offsets (`NeighborOffsets`, `ForEachNeighborInBounds`) — after Match3's PowerUp code stabilizes
- `NetworkBootstrap` adoption of `UnityServicesInitializer` inside `mobile-arena-fighter` — deferred; NetworkBootstrap currently emits three intermediate `Status(...)` callbacks around the init/sign-in dance that the extracted helper collapses to a single call. Adoption is a small refactor in the consumer, not a package change.

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
