#nullable enable
using Godot.Collections;

public static class CharacterBackgroundMapper
{
	public static CharacterBackgroundDefinition ToDomain(CharacterBackgroundResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description,
			StartingCoinPurse = CoinPurseMapper.ToDomain(resource.StartingCoinPurse),
			AbilityGrants = LevelAbilityGrantMapper.ToDomainList(resource.AbilityGrants),
			StartingEquipment = ItemResourceListMapper.ToDefinitionIds(resource.StartingEquipment),
			ProficiencyGrants = ProficiencyGrantMapper.ToDomainList(resource.ProficiencyGrants),
		};
}
