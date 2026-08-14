#nullable enable

/// <summary>Dependencies for character creation UI (narrower than full run context).</summary>
public sealed class CharacterCreationDependencies
{
	public CharacterCreationService CharacterCreation { get; }
	public ICharacterClassDefinitionRepository CharacterClasses { get; }
	public ICharacterRaceDefinitionRepository CharacterRaces { get; }
	public ICharacterBackgroundDefinitionRepository CharacterBackgrounds { get; }

	public CharacterCreationDependencies(
		CharacterCreationService characterCreation,
		ICharacterClassDefinitionRepository characterClasses,
		ICharacterRaceDefinitionRepository characterRaces,
		ICharacterBackgroundDefinitionRepository characterBackgrounds)
	{
		CharacterCreation = characterCreation;
		CharacterClasses = characterClasses;
		CharacterRaces = characterRaces;
		CharacterBackgrounds = characterBackgrounds;
	}
}
