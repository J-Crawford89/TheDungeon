#nullable enable
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
			AbilityGrants = LevelAbilityGrantMapper.ToDomainList(resource.AbilityGrants),
			StartingEquipment = ItemResourceListMapper.ToDefinitionIds(resource.StartingEquipment),
			ProficiencyGrants = ProficiencyGrantMapper.ToDomainList(resource.ProficiencyGrants),
		};
}
