#nullable enable
using System.Collections.Generic;

/// <summary>Inputs for <see cref="PlayerAbilityGrantBuilder.EnumerateFutureAbilitiesNotYetGranted"/> — one bag instead of many parallel parameters.</summary>
public sealed class FutureAbilityEnumerationRequest
{
	public required string ClassDefinitionId { get; init; }
	public required string RaceDefinitionId { get; init; }
	public required string BackgroundDefinitionId { get; init; }
	public required ICharacterClassDefinitionRepository CharacterClasses { get; init; }
	public required ICharacterRaceDefinitionRepository CharacterRaces { get; init; }
	public required ICharacterBackgroundDefinitionRepository CharacterBackgrounds { get; init; }
	public required IReadOnlyCollection<string> GrantedAbilityIds { get; init; }
	public required IAbilityDefinitionRepository AbilityDefinitions { get; init; }
}
