# JadedBelles Unity Save System

The save-system module is a reusable local persistence utility, not a global scene manager. Each game supplies its own DTO, schema version, migration chain, and validation; the module owns serialization and native file I/O.

This integration is opt-in. No existing game save is moved, converted, uploaded, or overwritten merely by installing the updated package.

## Install and wire

1. Install this utility package in Unity 2022.3 or newer. The manifest declares `com.unity.nuget.newtonsoft-json` 3.2.1; do not also drop an unrelated Newtonsoft DLL into the project. Unity's 3.2 package wraps Newtonsoft.Json 13.0.2 ([Unity package documentation](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)).
2. If your game's scripts use assembly definitions, add a reference to `JadedBelles.Util.SaveSystem`. If you use Override References and Newtonsoft attributes or `JObject` directly, also include `Newtonsoft.Json.dll`.
3. Open this package in Package Manager and import **Save System Basics**.
4. Add `SaveSystemBasicsSample` to one GameObject in a test scene. Assign a player Transform if you want position persistence.
5. Add a UI Button and connect On Click to `SaveSystemBasicsSample.HandleSavePressed`.
6. Enter Play Mode, move the player, click Save, leave Play Mode, then enter it again. Inspect the loaded position and Console. The sample reports failures instead of treating them as successful saves.
7. For a real game, replace the sample schema and DTOs with that game's models. Capture a detached snapshot on the main thread and await saving it before claiming completion.

The generic runtime does not contain the sample's `PlayerState`, `WorldData`, item IDs, coin rules, or world-entity references. Those types remain in the sample, ready to adapt inside a consuming game.

## Minimal game-owned contract

```csharp
using System;
using JadedBelles.Util.SaveSystem;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public sealed class MyProgress
{
    [JsonProperty(Required = Required.Always)]
    public int HighestUnlockedLevel = 1;
}

// Run setup on Unity's main thread.
var definition = new SaveDefinition<MyProgress>(
    schemaId: "my-game.progress",
    currentVersion: 1,
    createDefault: () => new MyProgress(),
    validate: data =>
    {
        if (data.HighestUnlockedLevel < 1)
            throw new InvalidSaveException("Invalid unlocked level.");
    });

var saves = UnitySaveSystem.Create("my-game", "guest", "progress", definition);
var result = await saves.LoadAsync();
if (!result.Success)
{
    // Show a recovery/retry UI; do not enable autosave.
    return;
}

var liveState = result.Data;
// Later, capture a fresh snapshot; never pass mutable live collections to the worker.
var snapshot = new MyProgress { HighestUnlockedLevel = liveState.HighestUnlockedLevel };
try
{
    await saves.SaveAsync(snapshot);
    // Display successful save only here.
}
catch (Exception error)
{
    UnityEngine.Debug.LogException(error);
    // Retain live state; show failure/retry, not success.
}
```

`CreateDefault`, validation, and migration callbacks run on worker threads. They must be pure managed-data operations: no scene objects, Unity APIs, UI updates, or mutable global game state.

Validation should throw `InvalidSaveException` only when the payload is genuinely invalid. Unexpected callback errors propagate instead of being silently interpreted as corruption; a missing migration throws `SaveCompatibilityException` and blocks fallback.

## File layout and identity

The Unity factory uses `Application.persistentDataPath/Saves/{product}/{profile}/{slot}.json`; Unity documents the persistent directory and its platform-specific locations ([Unity path documentation](https://docs.unity3d.com/2023.1/Documentation/ScriptReference/Application-persistentDataPath.html)).

Use a stable product identifier and a stable, non-secret internal account/profile identifier. Use `guest` only for guest saves; never use access tokens or raw email addresses as directory names.

Product, profile, and slot segments allow 1 to 64 ASCII letters, digits, underscores, or hyphens, excluding Windows device names. This blocks ordinary path traversal and keeps generated slot names portable; the low-level `SaveManager` directory is still a trusted caller-controlled absolute path.

Keep one manager instance per directory/slot in your app. Its semaphore does not coordinate other instances or processes, and there is no cross-process lock or multiwriter conflict resolution.

When switching accounts, stop new captures, await outstanding saves, discard old live state, then create and load the new profile's manager. Guest-to-account transfer and conflict resolution must be explicit game/API policies, never an automatic copy based only on login.

## Save contract and migrations

The envelope stores:

```json
{
  "format": "jadedbelles.save",
  "schemaId": "my-game.progress",
  "schemaVersion": 1,
  "savedAtUtc": "2026-09-18T16:00:00.0000000Z",
  "data": { "HighestUnlockedLevel": 1 }
}
```

`schemaId` identifies a stable data contract, not a C# type name. The timestamp is diagnostic metadata, not a cloud revision or conflict-resolution mechanism.

Pass migrations as `Dictionary<int, Action<JObject>>`. Entry 1 upgrades v1 to v2, entry 2 upgrades v2 to v3, and so on; the codec runs every required step in order before converting to the current DTO.

The sample demonstrates a v1 `Currency` field becoming v2 `Coins`, with `QuestProgress` introduced. Migrations only change the in-memory JSON tree; the original remains untouched until the next explicitly requested save.

Different format/schema IDs, newer versions, and missing migration steps fail closed without loading an older backup. This prevents a wrong game or older build from overwriting data it cannot understand.

The package envelope adds format and schema identity beyond the original standalone template. Existing standalone-template files or a game's pre-existing PlayerPrefs/cloud JSON need an explicit importer; copying files into this module's directory is not a migration strategy.

## Complex data without Unity serialization pitfalls

- **DTO boundary:** Use plain C# data objects and explicitly opt in saved members. Do not serialize GameObjects, Transforms, ScriptableObjects, callbacks, services, or scene graphs.
- **Flat graphs:** Save entities once in dictionaries and represent relationships using stable IDs. Circular logical relationships then do not create recursive serialized object references.
- **Scene reconstruction:** Load and validate data, resolve catalog IDs, create scene entities, then reconnect references. Resolve this on the main thread, not in a migration callback.
- **Positions:** Save plain X/Y/Z DTOs; convert to Unity vectors in Unity-facing code.
- **Snapshot ownership:** Deep-copy all nested objects and collections, not just their outer container. The worker owns the snapshot until the returned task completes.

The codec uses `ReferenceLoopHandling.Error` so accidental object loops fail instead of quietly dropping data; Newtonsoft documents that `Ignore` skips looping objects ([Newtonsoft settings](https://www.newtonsoft.com/json/help/html/SerializationSettings.htm)).

It also sets `TypeNameHandling.None`, following Microsoft's recommendation for untrusted JSON, and an explicit read-depth limit ([Microsoft CA2326](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2326), [Newtonsoft MaxDepth](https://www.newtonsoft.com/json/help/html/MaxDepth.htm)).

The defaults are 16 MiB and 48 levels of JSON nesting. The depth limit applies to reading; it does not make arbitrarily deep recursive DTOs safe to construct or serialize, so flatten the model rather than increasing the limit indefinitely.

## Async work and durability

Validation, serialization, parsing, file reads, and publication execute on worker threads. File content uses async writes followed by a worker-side durable file flush; a same-directory replacement publishes the completed file.

The commit sequence is:

1. Validate, encode, size-check, and decode the detached snapshot.
2. Write and flush a uniquely named temporary file.
3. Read and validate the existing primary.
4. If valid, publish it as the previous-generation backup through a backup temporary file.
5. If corrupt after an earlier successful recovery, preserve a `.corrupt-*` evidence copy and leave the good backup unchanged.
6. Replace or create the primary. Never delete the primary first.
7. Best-effort cleanup of temporary files.

Cancellation before publication throws and retains the committed primary; backup preparation may already have happened. Once primary publication starts, there is no cancellation point, so a committed save is reported as successful.

Serialization and I/O are off the frame loop, but main-thread snapshot capture and GC still need profiling. Debounce/coalesce autosaves and retain one app-level save owner; the queue serializes operations but cannot decide which independently captured business snapshot is newest.

No portable implementation can promise that every device/filesystem survives every power interruption. Test forced termination and replacement behavior on shipping platforms; unsupported replacement fails without a destructive delete-then-move fallback.

## Load and failure policy

| Condition | Result |
|---|---|
| Both generations absent | Validated defaults with `NewGame`; writes enabled |
| Primary valid | `Loaded` |
| Primary missing/corrupt and backup valid | `RecoveredBackup`, with corruption diagnostics when applicable |
| No valid generation but existing corruption | `Failed`, no default state, writes disabled |
| Wrong format/schema or unsupported version | `Failed`, no downgrade |
| Access denied or read I/O error | `Failed`, not treated as corruption |
| Invalid outgoing snapshot or write failure | Exception; report failure and offer retry |
| Unexpected game callback error | Exception; fix the callback rather than falling back |

The first save has no previous generation. Backup recovery may lose the latest generation's progress; notify the player instead of hiding that recovery occurred.

There is intentionally no automatic reset/delete operation. If users choose a reset, use a separately confirmed game workflow that archives existing files, including corrupt evidence, before creating a fresh slot.

Corrupt evidence and orphan temporary files need a game-owned retention policy. Orphan `.tmp` files are not automatic load candidates.

## Shared API and existing systems

Keep cloud synchronization behind the JadedBellesWebsite API and shared accounts. This utility introduces no second backend, credentials store, server schema, or cloud dependency.

Local currency and entitlement fields are not authoritative financial records; the server remains authoritative for purchases and wallet state. The utility does not implement encryption, signatures, tamper resistance, checksums, cloud revision checks, or offline merge logic.

The existing `ICharacterSaveProvider` and character customization module are unchanged. A game can implement that interface as an adapter to its chosen save model, but this change does not silently replace the current provider.

## Platform and lifecycle limits

- **Native players:** Intended for native desktop/mobile filesystem and thread support. Compile and verify in the actual Unity Editor/player before shipping.
- **WebGL:** The manager explicitly refuses construction in WebGL players; add an IndexedDB-aware adapter and tested browser lifecycle policy instead of implying native I/O works there. Unity documents WebGL's IDBFS location and tvOS's unsupported persistent path ([Unity platform notes](https://docs.unity3d.com/2023.1/Documentation/ScriptReference/Application-persistentDataPath.html)).
- **IL2CPP:** Validate DTO reflection in a release build with production managed stripping. Preserve the game-owned DTOs with appropriate linker rules if required.
- **Suspend/quit:** Save at meaningful active-session checkpoints. Do not depend on an asynchronous quit callback completing.

## Tests

The Unity package contains an Editor-only test assembly. In the consuming project's `Packages/manifest.json`, add the package name to the existing top-level `testables` array, without deleting other entries:

```json
{
  "testables": ["com.jadedbelles.util"]
}
```

With Unity Test Framework installed, run the `JadedBelles.Util.SaveSystem.Tests` EditMode tests. The sample import is not required for those core tests.

For independent verification, run from the utility repository:

```sh
dotnet test Tests~/SaveSystem.Tests.csproj --configuration Release
```

The standalone harness builds the exact core sources against .NET Standard 2.1 and runs the same NUnit tests plus sample DTO checks on .NET 8. The GitHub Actions workflow runs this command on Linux and Windows; it does not claim to build or launch Unity.

See [SaveSystemVerification.md](SaveSystemVerification.md) for what was actually run before delivery.
