# Save System Basics

Import this sample through the JadedBelles Util Package Manager page. Add `SaveSystemBasicsSample` to one GameObject, optionally assign a player Transform, and bind a UI Button's On Click to `HandleSavePressed`.

Run the scene, move the player, save, and restart Play Mode to load the position. The sample uses a dedicated `save-sample/guest/slot-1` location and does not touch another game's progress.

`SampleSaveData` demonstrates game-owned `PlayerState` and `WorldData`, dictionary-based entity graphs, opt-in JSON members, an ignored runtime reference, a v1-to-v2 migration, semantic validation, and deep-copy snapshot capture. Its importable assembly references only the save module and Newtonsoft, not the utility package's other modules.

Adapt the DTOs and schema ID inside your game rather than putting game-specific state in the generic utility. The full package guide is `Documentation~/SaveSystem.md`; follow its account-isolation, worker-thread ownership, failure-handling, and platform limitations before integration.
