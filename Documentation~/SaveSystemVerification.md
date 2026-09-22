# Save-System Integration Verification

Verified on September 18, 2026 against `Jeffrey3James/JadedBellesUtil` base commit `802b364`, with implementation changes on local branch `feat/generic-save-system`. This report describes local checks, not a Unity release certification.

## Executed successfully

- Compiled the exact runtime core against .NET Standard 2.1 with C# 9.
- Ran 40 NUnit cases on .NET 8.0 using SDK 8.0.425 and Newtonsoft.Json 13.0.2 on Linux.
- Compiled the sample DTOs and checked snapshot isolation, persistence round trip, dangling graph references, and non-finite positions.
- Validated package/assembly JSON syntax, new Unity metadata coverage and GUID uniqueness, package sample paths, and whitespace checks.

The 40 cases comprise 37 core NUnit cases also available to Unity EditMode, plus three standalone-only sample-model tests. There are no skipped cases in the recorded local run.

## Behaviors exercised

- Validated defaults when both generations are absent.
- Round-trip dictionaries, logical graph cycles, ignored runtime references, and a second unrelated DTO type.
- Write protection before load and after failed loads.
- Recovery from corrupt or missing primaries, preservation of diagnostic evidence, and blocking when both generations are unusable.
- Different format/schema IDs, newer versions, missing migrations, and changed-on-disk future versions fail closed.
- In-memory v1 migration and ordered v1-to-v2-to-v3 migration.
- Required fields, JSON depth, invalid UTF-8, input/output byte limits, and semantic validation.
- Game callback programming errors propagate instead of causing a silent rollback.
- Pre-cancelled saves and cancellation after backup preparation.
- Injected storage errors after temporary-file flush and after backup preparation.
- Failed first saves publish no partial primary.
- Concurrent requests through one manager retain readable committed generations.
- Slot traversal and reserved device names are rejected.
- Trailing JSON, duplicate properties, CLR type metadata, and accidental reference loops are handled conservatively.

Fault injection tests simulate managed exceptions at commit boundaries, not physical disk damage or actual power loss. They assert primary content, backup content where relevant, and cleanup behavior.

## Not yet verified

- Unity Editor import, assembly resolution, sample component compilation, Play Mode, or Unity Test Runner execution.
- IL2CPP, stripping, mobile devices, frame timing, allocation pressure, or application suspend/termination.
- Real disk-full conditions, OS permission failures, or cancellation inside an in-flight OS write.
- Crash/power-loss behavior during rename/replacement and directory metadata durability.
- Windows execution locally. A GitHub Actions Linux/Windows matrix is included but has not run remotely before branch publication.
- Any existing game's save importer, account switch, cloud synchronization, shared API conflict logic, or live release.

Existing utility modules and their runtime assembly were not modified. The package version remains 0.9.0 with an Unreleased changelog entry; consumers should adopt this branch explicitly for testing, not treat it as a tagged release.
