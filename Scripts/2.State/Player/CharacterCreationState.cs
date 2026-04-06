#nullable enable
using System;

public sealed class CharacterCreationState
{
	public string Name { get; set; } = "Testy McTestface";
	public CharacterSex Sex { get; set; } = CharacterSex.Male;
	public CharacterClassDefinition? SelectedClass { get; set; }
	public CharacterRaceDefinition? SelectedRace { get; set; }
	public CharacterBackgroundDefinition? SelectedBackground { get; set; }

	public AbilityScores RolledAbilityScores { get; set; } = new();
	public AbilityScores FinalAbilityScores { get; set; } = new();

	public CharacterCreationState Clone() =>
		new()
		{
			Name = Name,
			Sex = Sex,
			SelectedClass = SelectedClass,
			SelectedRace = SelectedRace,
			SelectedBackground = SelectedBackground,
			RolledAbilityScores = AbilityScoresCopy.From(RolledAbilityScores),
			FinalAbilityScores = AbilityScoresCopy.From(FinalAbilityScores),
		};
}
