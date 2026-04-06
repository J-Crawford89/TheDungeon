#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public static class CharacterClassMapper
{
	public static CharacterClassDefinition ToDomain(CharacterClassResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description,
			BaseHp = resource.BaseHp,
			Abilities = MapAbilityStrings(resource.Abilities),
			StartingEquipment = ToStringList(resource.StartingEquipment),
			Proficiencies = ToStringList(resource.Proficiencies),
		};

	private static List<AbilityDefinition> MapAbilityStrings(Array<string>? abilities)
	{
		if (abilities == null || abilities.Count == 0)
			return [];
		var list = new List<AbilityDefinition>();
		foreach (var s in abilities)
		{
			if (string.IsNullOrWhiteSpace(s))
				continue;
			list.Add(new AbilityDefinition { Id = s, Name = s, Definition = string.Empty });
		}
		return list;
	}

	private static List<string> ToStringList(Array<string>? arr) =>
		arr == null || arr.Count == 0 ? [] : arr.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();
}
