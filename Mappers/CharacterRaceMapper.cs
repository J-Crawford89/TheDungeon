#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public static class CharacterRaceMapper
{
	public static CharacterRaceDefinition ToDomain(CharacterRaceResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description,
			BaseHp = resource.BaseHp,
			AbilityGrants = LevelAbilityGrantMapper.ToDomainList(resource.AbilityGrants),
			StartingEquipment = ToStringList(resource.StartingEquipment),
			Proficiencies = ToStringList(resource.Proficiencies),
		};

	private static List<string> ToStringList(Array<string>? arr) =>
		arr == null || arr.Count == 0 ? [] : arr.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
}
