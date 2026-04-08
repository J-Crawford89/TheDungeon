#nullable enable

/// <summary>Dependencies for <see cref="CharacterCreationScreen"/> (narrower than full <see cref="GameRunContext"/>).</summary>
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

	public static CharacterCreationDependencies From(GameRunContext ctx) =>
		new(ctx.CharacterCreation, ctx.CharacterClasses, ctx.CharacterRaces, ctx.CharacterBackgrounds);
}
