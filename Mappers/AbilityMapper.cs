#nullable enable

public static class AbilityMapper
{
	public static AbilityDefinition ToDomain(AbilityResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description,
		};
}
