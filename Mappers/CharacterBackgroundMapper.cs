#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public static class CharacterBackgroundMapper
{
	public static CharacterBackgroundDefinition ToDomain(CharacterBackgroundResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description,
			StartingGold = resource.StartingGold,
			AbilityGrants = LevelAbilityGrantMapper.ToDomainList(resource.AbilityGrants),
			StartingEquipment = ItemResourceListMapper.ToDefinitionIds(resource.StartingEquipment),
			Proficiencies = ToStringList(resource.Proficiencies),
		};

	private static List<string> ToStringList(Array<string>? arr) =>
		arr == null || arr.Count == 0 ? [] : arr.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
}
