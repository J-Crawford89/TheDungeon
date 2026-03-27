public static class LoreMapper
{
	public static LoreDefinition ToDomain(LoreResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description
		};
}
