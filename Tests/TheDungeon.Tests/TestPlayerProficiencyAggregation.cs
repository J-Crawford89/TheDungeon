using System.Collections.Generic;

internal static class TestPlayerProficiencyAggregation
{
	public static PlayerProficiencyAggregationService CreateEmpty() =>
		new(new EmptyRaces(), new EmptyClasses(), new EmptyBackgrounds());

	private sealed class EmptyRaces : ICharacterRaceDefinitionRepository
	{
		public IReadOnlyList<CharacterRaceDefinition> All => [];
	}

	private sealed class EmptyClasses : ICharacterClassDefinitionRepository
	{
		public IReadOnlyList<CharacterClassDefinition> All => [];
	}

	private sealed class EmptyBackgrounds : ICharacterBackgroundDefinitionRepository
	{
		public IReadOnlyList<CharacterBackgroundDefinition> All => [];
	}
}
