public static class TrapMapper
{
	public static TrapDefinition ToDomain(TrapResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			DiscoverDc = resource.DiscoverDc,
			DisarmDc = resource.DisarmDc,
			Damage = resource.Damage,
			Effect = resource.Effect
		};
}
