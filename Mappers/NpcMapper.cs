public static class NpcMapper
{
	public static NpcDefinition ToDomain(NpcResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			DiscoverDc = resource.DiscoverDc,
		};
}
