# TheDungeon Testing Policy

This document defines the current test-scope boundary for automated tests in this repository.

## Current Unit-Test Scope

The default automated harness is xUnit under `Tests/TheDungeon.Tests`.

In scope for routine feature work:

- Pure C# logic in `Scripts/0.Core/`
- Pure C# logic in `Scripts/2.State/`
- Pure C# logic in `Scripts/3.Game/`

These areas should ship with both happy-path and fail-path tests when changed.

## Temporary Out-of-Scope Areas

The following are intentionally deferred in the current xUnit-only workflow:

- `Mappers/`
- `Repositories/` behavior that depends on Godot runtime/resource semantics
- `Resources/` data/resource wiring behavior

This is a deliberate, temporary boundary to avoid brittle failures in non-Godot-hosted test execution.

When code changes touch those areas, note in PR/review context that coverage is deferred by this policy.

## Revisit Trigger

Revisit this policy when a stable Godot-hosted integration test harness is introduced.

At that point, add mapper/repository/resource coverage in that harness rather than forcing it into the xUnit runtime.

## Former backlog (now covered in xUnit)

The following former “must-have” targets are exercised under `Tests/TheDungeon.Tests/` (non-exhaustive):

| Code | Tests |
|------|--------|
| [`PlayerVitalsService`](Scripts/3.Game/Services/PlayerVitalsService.cs) | `PlayerVitalsServiceTests.cs` |
| [`CombatAbilityCooldowns`](Scripts/2.State/Combat/CombatAbilityCooldowns.cs) | `CombatAbilityCooldownsTests.cs` |
| [`CharacterNameValidator`](Scripts/3.Game/CharacterNameValidator.cs) | `CharacterNameValidatorTests.cs` |
| [`CharacterCreationService`](Scripts/3.Game/Services/CharacterCreationService.cs) | `CharacterCreationServiceRollTests.cs`, `CharacterCreationServiceApplyTests.cs` |
| [`GameSessionState` logging](Scripts/2.State/GameSessionState.cs) | `GameSessionStateLogTests.cs`, `GameSessionStateResetTests.cs` |
| [`DefendCombatAbilityHandler`](Scripts/3.Game/Combat/DefendCombatAbilityHandler.cs) | `DefendCombatAbilityHandlerTests.cs` |

Additional combat/UI/dungeon coverage includes `CombatTurnLoopTests.cs`, `CombatMonsterTurnTests.cs`, `CombatUiPresenterTests.cs`, `HandBuiltDungeonFloorTests.cs`, and `NarrativeServiceContractTests.cs`.

## Feature-Test Checklist (Default)

For each new feature or logic change:

- Add at least one happy-path test.
- Add at least one fail/edge-path test.
- Prefer deterministic seams (fixed random/stubs) over probabilistic assertions.
- If change is in temporary out-of-scope folders, explicitly record deferred coverage in PR notes.
